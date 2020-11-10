/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using Tiny.Twitter;
using SeawispHunter.MinibufferConsole;
using SeawispHunter.MinibufferConsole.Extensions;
using RSG;
using uGIF;
using MimeTypes;

namespace SeawispHunter.MinibufferConsole {
/**
   ![Twitter commands in the inspector](inspector/twitter-commands.png)
   Tweet a message, screenshot, or GIF from within your game.

   This is mainly meant for a developer to showcase their new features or bugs.
   If one wants to use this functionality as a part of their game proper where
   the players can tweet, please [register an app with
   twitter](http://apps.twitter.com/apps/new) and set the consumerKey and
   consumerSecret.
 */
[Group(tag = "built-in")]
public class TwitterCommands : MonoBehaviour {
  private const string minibufferConsumerKey = "ExFOkYkf1zYSEG5tahFLnkDfD";
  private const string minibufferConsumerSecret = "Cc26ZL0RBtLkITYk5xCpVUwTHjhn7GK7LEMdfTPk0nMZpqrBd2";
  [Header("Consumer key and secret for Twitter app")]
  public string consumerKey;
  public string consumerSecret;
  public string screenshotDirectory = "$temp/twitter-screenshots/";
  private TinyTwitter tinyTwitter;
  private AccessInfo access;
  private CaptureToGIF captureToGIF;

  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
    captureToGIF = GetComponent<CaptureToGIF>();
  }

  void OnDestroy() {
    Minibuffer.Unregister(this);
  }

  [Command("twitter-authorize",
           description = "Authorize through twitter.")]
  public IEnumerator PermitTweets([Current] Minibuffer m) {
    SetupConsumerKey();
    if (consumerKey.IsZull() || consumerSecret.IsZull()) {
      m.Message("This game needs to register itself as a twitter app first. Opening web page...");
      Application.OpenURL("http://apps.twitter.com/apps/new");
      yield break;
    }

    this.RunInThread<OAuthInfo>(() => TinyTwitter.RequestToken(consumerKey, consumerSecret))
      .Then(oauth => {
          Debug.Log("request thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
          Application.OpenURL(string.Format("https://api.twitter.com/oauth/authenticate?oauth_token={0}", oauth.AccessToken));
          return oauth;
        })
      .Then(oauth =>
            m.Read("Enter pin: ")
              .Then(pin =>
                    this.RunInThread<AccessInfo>(
                      () => TinyTwitter.AccessToken(consumerKey, consumerSecret, oauth != null ? oauth.AccessToken : null, pin))))
      .Then(access => {
          this.access = access;
          tinyTwitter = new TinyTwitter(this.access.oauth);
          SaveAccessInfo(access);
          Debug.Log("access thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
          m.Message("Permission granted for @{0}.", access.user.screenName);
        })
      .Done();
  }

  private void SetupConsumerKey() {
  if (consumerKey.IsZull() || consumerSecret.IsZull()) {
    /* Consider generating your own consumer key and secret if you'll be
       doing a lot of tweeting in your game. */
    Debug.LogWarning("Using Minibuffer twitter app consumer key and secret.");
    consumerKey = minibufferConsumerKey;
    consumerSecret = minibufferConsumerSecret;
  }
  }

  private IEnumerator SetupIfNeeded() {
    if (access == null) {
      SetupConsumerKey();
      access = LoadAccessInfo();
      if (access != null) {
        tinyTwitter = new TinyTwitter(access.oauth);
      } else {
        Debug.Log("No twitter account stored in preferences.");
        yield return StartCoroutine(PermitTweets(Minibuffer.instance));
      }
    }
  }

  private bool IsSetup() {
    return access != null;
  }

  [Command(description = "What account is authorized?")]
  public string TwitterWhoAmI() {
    if (access == null)
      access = LoadAccessInfo();
    if (access != null)
      return "@" + access.user.screenName;
    else
      return "Not twitter account setup. Run M-x twitter-authorize to set one up.";
  }

  [Command(description = "Tweet a message.")]
  public IEnumerator Tweet([Prompt("Message: ")]
                           string message) {

    if (! IsSetup())
      yield return SetupIfNeeded();
    this.RunInThread(() => tinyTwitter.UpdateStatus(message))
      .Then(() => Minibuffer.instance.Message("@{0}: {1}", access.user.screenName, message))
      .Catch(ex => Minibuffer.instance.Message("Error sending tweet: {0}", ex.Message))
      .Done();
  }

  [Command(description = "Tweet a screenshot.")]
  public IEnumerator TweetScreenshot([Prompt("Screenshot message: ")]
                                     string message,
                                     [UniversalArgument]
                                     bool useLastScreenshot) {
    if (! IsSetup())
      yield return SetupIfNeeded();

    string filename;
    if (useLastScreenshot) {
      Minibuffer.instance.Message("Using last screenshot.");
      filename = UnityCommands.lastScreenshotFilename;
    } else {
      var dir = PathName.instance.Expand(screenshotDirectory);
      Directory.CreateDirectory(dir);
      filename = UnityCommands.GenerateNewFilename(Path.Combine(dir,"screenshot-{0}.png"));

      yield return null;
      yield return new WaitForEndOfFrame();
#if UNITY_2017_1_OR_NEWER
      ScreenCapture.CaptureScreenshot(filename);
#else
      Application.CaptureScreenshot(filename);
#endif
      // I want to wait till the file exists.  I'm assuming waiting one frame will do.
      // If it's more complicated then that, I ought to have a promise that waits
      // for the file to show up.
      yield return null;
    }
    Minibuffer.instance.Message("@{0}: {1}", access.user.screenName, message);
    this.RunInThread<string>(() => tinyTwitter.UploadMedia(new FileStream(filename,
                                                                          FileMode.Open,
                                                                          FileAccess.Read),
                                                           "image/png"))
      .Then(mediaId => this.RunInThread(() => { tinyTwitter.UpdateStatusWithMedia(message, mediaId); }))
      .Then(() => Minibuffer.instance.Message("Image posted. @{0}: {1}", access.user.screenName, message))
      .Catch(ex => Minibuffer.instance.Message("Error sending tweet: {0}", ex.Message))
      .Done();
  }

  [Command(description = "Tweet a GIF.  If used with C-u, tweet the last recorded GIF.")]
  public IEnumerator TweetGIF([Prompt("GIF message: ")]
                              string message,
                              [UniversalArgument]
                              bool useLastGIF) {
    if (! IsSetup())
      yield return SetupIfNeeded();
    string gifFilename = null;
    if (useLastGIF) {
      if (! captureToGIF.lastGifFilename.IsZull()) {
        Minibuffer.instance.Message("Using last GIF.");
        gifFilename = captureToGIF.lastGifFilename;
      } else {
        Minibuffer.instance.Message("No such previous GIF recorded.");
        yield break;
      }
    }
    if (gifFilename == null && ! captureToGIF.capturing) {
      // We're not capturing already. Start capturing.
      Minibuffer.instance.WithCommandResult(captureToGIF.GIFRecord());
    }

    (gifFilename == null
      ? captureToGIF.gifFile
      : Promise<string>.Resolved(gifFilename))
      .Then(filename =>
            StartCoroutine(TweetWithMedia(message, filename)))
      .Done();

    if (gifFilename == null)
      Minibuffer.instance.Message("Will post tweet when GIF is complete.");
  }

  [Command(description = "Tweet a message with a media file.")]
  public IEnumerator TweetWithMedia([Prompt("Message: ")]
                                    string message,
                                    [Prompt("Media: ",
                                            completer = "file")]
                                    string filename) {
    if (! IsSetup())
      yield return SetupIfNeeded();
    var ext = Path.GetExtension(filename);
    this.RunInThread<string>(() => tinyTwitter.UploadMedia(new FileStream(filename,
                                                                          FileMode.Open,
                                                                          FileAccess.Read),
                                                           MimeTypeMap.GetMimeType(ext)))
      .Then(mediaId => this.RunInThread(() => { tinyTwitter.UpdateStatusWithMedia(message, mediaId); }))
      .Then(() => Minibuffer.instance.Message("Media posted. @{0}: {1}", access.user.screenName, message))
      .Catch(ex => Minibuffer.instance.Message("Error sending tweet: {0}", ex.Message))
      .Done();
  }

  AccessInfo LoadAccessInfo() {
    SetupConsumerKey();
    if (consumerKey.IsZull()
        || consumerSecret.IsZull())
      return null;
    var accessInfo =
      new AccessInfo {
      user = new User {
        id         = Convert.ToInt64(PlayerPrefs.GetString("minibuffer.twitter.user-id", "-1")),
        screenName = PlayerPrefs.GetString("minibuffer.twitter.screen-name")
      },
      oauth = new OAuthInfo {
        ConsumerKey    = consumerKey,
        ConsumerSecret = consumerSecret,
        AccessToken    = PlayerPrefs.GetString("minibuffer.twitter.access-token"),
        AccessSecret   = PlayerPrefs.GetString("minibuffer.twitter.access-secret")
      }
    };
    if (accessInfo.user.screenName.IsZull()
        || accessInfo.user.id == -1
        || accessInfo.oauth.AccessToken.IsZull()
        || accessInfo.oauth.AccessSecret.IsZull()) {
      accessInfo = null;
    }
    return accessInfo;
  }

  void SaveAccessInfo(AccessInfo accessInfo) {
    PlayerPrefs.SetString("minibuffer.twitter.user-id", accessInfo.user.id.ToString());
    PlayerPrefs.SetString("minibuffer.twitter.screen-name", accessInfo.user.screenName);
    PlayerPrefs.SetString("minibuffer.twitter.access-token", accessInfo.oauth.AccessToken);
    PlayerPrefs.SetString("minibuffer.twitter.access-secret", accessInfo.oauth.AccessSecret);
  }

  [Command("twitter-preferences-delete", description = "Delete twitter authorization information.")]
  public void DeleteAccessInfo() {
    PlayerPrefs.DeleteKey("minibuffer.twitter.user-id");
    PlayerPrefs.DeleteKey("minibuffer.twitter.screen-name");
    PlayerPrefs.DeleteKey("minibuffer.twitter.access-token");
    PlayerPrefs.DeleteKey("minibuffer.twitter.access-secret");
    access = null;
    tinyTwitter = null;
  }
}
}
