/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/*
  Run this list of commands when this behaviour starts.  If this
  is a WebGL build will look for a 'playback' GET parameter.
*/
[Group(tag = "built-in")]
public class RunCommands : MonoBehaviour {

  [Header("List of commands to run when this script starts.")]
  public string[] commands;

  [Header("List of key chords to replay when this script starts.")]
  [TextArea]
  public string keySequence;
  public float delay = 0.2f;

  public MinibufferListing minibufferExtensions;

  private IEnumerator Start() {
    Minibuffer.Register(this);
    yield return null;
    PostStart();
  }

  /*
    This runs after everything else has run their Start().
   */
  void PostStart() {
    if (enabled) {
      foreach(var command in commands) {
        Minibuffer.instance.ExecuteCommand(command);
      }

      if (! keySequence.IsZull()) {
        StartCoroutine(PlayerPiano(keySequence));
      }
    }

    #if UNITY_WEBGL
    // print("url " + Application.absoluteURL);
    var qs = ParseQueryString(Application.absoluteURL);
    if (qs.ContainsKey("playback")) {
      StartCoroutine(PlayerPiano(qs["playback"]));
    }
    #endif
  }

  [Command("player-piano",
           description = "The given string will be 'played' back as if typed")]
  public IEnumerator PlayerPiano(string input) {
    var wait = new WaitForSeconds(delay);
    var m = Minibuffer.instance;
    foreach(var str in input.Split(' ')) {
      if (Regex.IsMatch(str, "^[MSCsH]-.")
          || KeyChord.IsKeyCode(str)) {
        m.keyQueue.Enqueue(KeyChord.FromString(str));
        //print("sending mod " + str);
        //KbdEvent(str);
        //Event(Event.KeyboardEvent
        yield return wait;
      } else {
        // This is a string that's just supposed to be typed.
        foreach(char c in str) {
          //print("sending " + c);
          //KbdEvent(c.ToString());
          m.keyQueue.Enqueue(new KeyChord(c));
          yield return wait;
        }
      }
    }
  }

  #if UNITY_WEBGL

  public static Dictionary<string, string> ParseQueryString(string url) {
    Dictionary<string, string> qDict = new Dictionary<string, string>();
    if (url.Contains("?"))
      foreach (string qPair in url.Substring(url.IndexOf('?') + 1).Split('&')) {
        string[] qVal = qPair.Split('=');
        qDict.Add(qVal[0], WWW.UnEscapeURL(qVal[1]));
      }
    return qDict;
  }
  #endif
}
}
