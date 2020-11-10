/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
#define MANUAL_SCENENAMES

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RSG;

using System.Reflection;
using System;
using SeawispHunter.MinibufferConsole.Extensions;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace SeawispHunter.MinibufferConsole {

[Group("unity", tag = "built-in")]
/**
   ![Unity commands in the inspector](inspector/unity-commands.png)
   [Unity](http://unity3d.com) specific commands.

   These are a general grab bag of Unity specific commands. The following
   features are given:

   1. Switch to a scene.
   2. Log console messages to `*Console*`.
   3. Capture a screenshot.
   4. Describe a GameObject's hierarchy, or an entire scene.
   5. Activate or deactivate a GameObject.
   6. Reload the current scene.
*/
//[HelpURL("http://example.com/docs/MyComponent.html")]
public class UnityCommands : MonoBehaviour {
  public string screenshotDirectory = "~/Desktop";
  private AppendableBuffer console;
  private Minibuffer minibuffer;
  [Header("Hide Minibuffer on Screenshot?")]
  [Variable("hide-minibuffer-on-screenshot",
            description = "Toggle whether screenshot includes minibuffer")]
  public bool hideMinibufferOnScreenshot = false;
  //public RawImage assetPreview;

  #if MANUAL_SCENENAMES || !UNITY_5_4_OR_NEWER
  [Header("List of scenes that switch-to-scene command uses.")]
  /* sceneNames need to be filled in at editor-time. */
  public List<string> sceneNames;
  #endif

  [Header("Capture Unity's Console logging to a buffer?")]
  public bool consoleLogging = false;
  public bool showStacktrace = true;

  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
    Minibuffer.With(minibuffer => {
        this.minibuffer = minibuffer;

        #if !MANUAL_SCENENAMES && UNITY_5_4_OR_NEWER
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        List<string> sceneNames = Enumerable
        .Range(0, sceneCount)
        .Select(i => SceneManager.GetSceneByBuildIndex(i).name)
        .Where(s => s != null)
        .ToList();
        //minibuffer.ToBuffer("*sceneNames", HelpCommands.FormatTable(sceneNames.Select(x => new [] { x }).ToArray()));
        #endif
        minibuffer.completers["Scene"] = new ListCompleter<string>(sceneNames).ToEntity();
        console = new RLEBuffer("console") { autoScrollDown = true };
        minibuffer.buffers.Add(console);
        if (! consoleLogging) {
          // console.AppendLine("To activate this buffer, type 'M-x toggle-console-logging'.");
        } else {
          Application.logMessageReceived += HandleConsoleLog;
        }

        var completers = minibuffer.completers;
        // These commented classes can't be added this way.
        //completers["AudioMixer"]   = new ResourceCompleter<AudioMixer>();
        //completers["Model"]        = new ResourceCompleter<Model>();
        //completers["Prefab"]       = new ResourceCompleter<Prefab>();
        //completers["Scene"]        = new ResourceCompleter<Scene>();
        //completers["Script"]       = new ResourceCompleter<Script>();
        completers["AnimationClip"]  = new ResourceCompleter<AnimationClip>().ToEntity();
        completers["AudioClip"]      = new ResourceCompleter<AudioClip>().ToEntity();
        completers["Font"]           = new ResourceCompleter<Font>() { showHidden = true }.ToEntity();
        completers["GUISkin"]        = new ResourceCompleter<GUISkin>().ToEntity();
        completers["Material"]       = new ResourceCompleter<Material>().ToEntity();
        completers["Mesh"]           = new ResourceCompleter<Mesh>().ToEntity();
        completers["PhysicMaterial"] = new ResourceCompleter<PhysicMaterial>().ToEntity();
        completers["Shader"]         = new ResourceCompleter<Shader>().ToEntity();
        completers["Sprite"]         = new ResourceCompleter<Sprite>().ToEntity();
        completers["Texture"]        = new ResourceCompleter<Texture>().ToEntity();
        completers["GameObject"]     = new ResourceCompleter<GameObject>().ToEntity();
        completers["Component"]      = new ResourceCompleter<Component>().ToEntity();

        // There are multiple completers possible. One could do color by
        // name or by rgb.  How should this be managed?
        var dict        = new Dictionary<string, Color>();
        dict["black"]   = Color.black;
        dict["blue"]    = Color.blue;
        dict["clear"]   = Color.clear;
        dict["cyan"]    = Color.cyan;
        dict["gray"]    = Color.gray;
        dict["green"]   = Color.green;
        dict["grey"]    = Color.grey;
        dict["magenta"] = Color.magenta;
        dict["red"]     = Color.red;
        dict["white"]   = Color.white;
        dict["yellow"]  = Color.yellow;
        dict["orange"]  = new Color(246f/255f, 171f/255f, 73f/255f);
        var colorCompleter = new CompleterEntity(new DictCompleter<Color>(dict));
        colorCompleter.coercer = new CombineCoercers(new ColorCoercer(),
                                                     colorCompleter.coercer);
        minibuffer.completers["Color"]          = colorCompleter;

        var keymap = minibuffer.GetKeymap("unity", true);
        // If you're using Minibuffer in the Unity Editor, you'll
        // probably be hitting Command-p to play quite a lot, so it's
        // nice to just noop this keychord.  It'll squelch the message
        // "No command bound to s-p."
        keymap["s-p"]         = "noop";

        //MethodInfo m = (new Func<int, string>((i) => "whatever")).Method;
        minibuffer.elementSelectionChange = ShowPreviewForCurrentLine;
      });
  }

  [Command("switch-to-scene",
           description = "Load a scene.",
           keyBinding = "C-x s")]
  public IEnumerator SwitchToScene([Prompt("Scene: ",
                                           completer = "Scene")]
                                   string scene) {
    if (scene != null) {
      //AsyncOperation operation = SceneManager.LoadSceneAsync(scene.buildIndex);
      AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
      var minibuffer = Minibuffer.instance;
      if (operation == null) {
        Debug.LogError(string.Format("Unable to load scene '{0}'.", scene));
        yield break;
      }
      while(! operation.isDone) {
        yield return null; //operation.isDone;
        minibuffer.Echo(string.Format("Loading scene '{0}' progress {1}...", scene, operation.progress));
      }
      minibuffer.Message("Loaded scene '{0}'.", scene);
    }
  }

  void HandleConsoleLog(string logString, string stackTrace, LogType type) {
    console.AppendLine(type + ": " + logString);
    if (showStacktrace
        && (type != LogType.Log
            && type != LogType.Warning)
        && ! stackTrace.IsZull()) {
      console.AppendLine("```\n{0}```".Formatted(stackTrace));
    }
  }

  [Command("show-console",
           description = "Show Unity's Console log.",
           keyBinding = "C-c d")]
  public void ShowConsole(bool interactive = true) {
    minibuffer.gui.main.buffer = console;
    minibuffer.gui.main.visible = true;
    if (interactive && ! consoleLogging)
      minibuffer
        .ReadYesOrNo("Do you want to turn console logging on?")
        .Then(yes => {
            if (yes && ! consoleLogging)
              ToggleConsoleLogging();
            else
              minibuffer.Message("Console logging is turned off. To turn it on type M-x toggle-console-logging.");
            });
  }

  [Command("toggle-console-logging",
           description = "Toggle Unity's Console logging.")]
  public void ToggleConsoleLogging() {
    if (consoleLogging) {
      // Turn console off.
      Minibuffer.instance.Message("Console logging turned off.");
      Application.logMessageReceived -= HandleConsoleLog;
    } else {
      // Turn console on.
      Application.logMessageReceived -= HandleConsoleLog;
      Application.logMessageReceived += HandleConsoleLog;
      Minibuffer.instance.Message("Console logging turned on.");
      ShowConsole(false);
    }
    consoleLogging = !consoleLogging;
  }

  const int MAX_SCREENSHOT = 100;

  public static string GenerateNewFilename(string pathFormat) {
    int i = 0;
    var filename = string.Format(pathFormat, i);
    for (; i < MAX_SCREENSHOT && File.Exists(filename); i++) {
      filename = string.Format(pathFormat, i);
    }
    return filename;
  }

  public static string lastScreenshotFilename;
  #if !UNITY_WEBGL
  [Command("capture-screenshot",
           description = "Save screenshot to selected directory",
           keyBinding = "C-c c")]
  public IEnumerator CaptureScreenshot(bool quiet = false) {
    bool errored = false;
    while (screenshotDirectory.IsZull() && ! errored) {
      // No directory set. Ask for one the first time.
      var p = Minibuffer.instance
        .Read("Screenshot directory: ", // prompt
              "~",                      // input
              "screenshot-directory",   // history
              "directory"               // completer
              )
        .Then((path) => {
            screenshotDirectory = (string) path;
            // Then capture the screenshot.
            // StartCoroutine(CaptureScreenshot());
          })
        .Catch(ex => errored = true); // Don't care.
      yield return p.WaitForPromise();
    }
    var dir = PathName.instance.Expand(screenshotDirectory);
    Directory.CreateDirectory(dir);
    var filename = GenerateNewFilename(Path.Combine(dir,
                                       "screenshot-{0}.png"));
    lastScreenshotFilename = filename;
    if (hideMinibufferOnScreenshot) {
      // XXX Hide minibuffer while taking screenshot? Yeah, eventually.
      Minibuffer.instance.visibleNoAnim = false;
      yield return null;
      yield return new WaitForEndOfFrame();
    }

#if UNITY_2017_1_OR_NEWER
    ScreenCapture.CaptureScreenshot(filename);
#else
    Application.CaptureScreenshot(filename);
#endif
    Minibuffer.instance.visibleNoAnim = true;
    if (! quiet)
      Minibuffer.instance.Message("Captured screenshot to {0}.",
                                  PathName.instance.Compress(filename));
  }
  #endif

  #if UNITY_EDITOR
  /* Asset preview section */
  public void ShowPreviewForCurrentLine(string line ) {
    //print(string.Format("preview line '{0}'", line));
    var coercer = minibuffer.editState.prompt.completerEntity.coercer;
    if (coercer != null && line != null) {
      // I'm not sure which one is the right one here.
      object o = coercer.Coerce(line, coercer.defaultType); /* minibuffer.editState.prompt.desiredType);*/
      ShowPreviewFor(o);
    }
  }

  public IPromise<Texture2D> GetAssetPreview(UnityEngine.Object obj) {
    var texture = AssetPreview.GetAssetPreview(obj);
    if (texture != null)
      return Promise<Texture2D>.Resolved(texture);
    else {
      var p = new Promise<Texture2D>();
      StartCoroutine(WaitForAssetPreview(obj, p));
      return p;
    }
  }

  public IEnumerator WaitForAssetPreview(UnityEngine.Object obj,
                                         Promise<Texture2D> promise) {
    var texture = AssetPreview.GetAssetPreview(obj);
    yield return null;
    while (texture == null
           && AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID())) {
      yield return null;
      texture = AssetPreview.GetAssetPreview(obj);
    }
    if (texture != null)
      promise.Resolve(texture);
    else {
      promise.Resolve(null);
      // Throwing an exception here seems heavy handed. It's expected that most
      // things won't have a preview.
      //promise.Reject(new MinibufferException("No preview for " + obj));
    }
  }

  public void ShowPreviewFor(object o) {
    if (o == null || o.GetType() == typeof(string))
      return;
    if (o is UnityEngine.Object) {
      GetAssetPreview((UnityEngine.Object) o)
        .Catch(ex => {
            //Debug.LogException(ex);
            print("Caught: " + ex.Message);
          })
        .Then(texture => {
            if (texture != null) {
              minibuffer.gui.assetPreview.texture = texture;
              minibuffer.gui.assetPreview.enabled = texture != null;
            }
            //print("Preview set.");
          });
    } else {
      minibuffer.gui.assetPreview.enabled = false;
      //print("No preview available.");
    }
  }

  // XXX This is just to test stuff.
  [Command("preview-material")]
  private void PreviewMaterial(Material m) {
    var texture = AssetPreview.GetAssetPreview(m);
    minibuffer.gui.assetPreview.texture = texture;
    minibuffer.Message("Set preview.");
  }
  #else
  public void ShowPreviewForCurrentLine(string line) { }
  #endif

  [Command("describe-scene",
           description = "Show game object hierarchy of scene.  Use C-u to show components.",
           keyBinding = "C-h s")]
  public void DescribeScene([UniversalArgument] bool includeComponents) {
    var writer = new StringWriter();
    // List<GameObject> rootObjects = new List<GameObject>();
    Scene scene = SceneManager.GetActiveScene();
    var rootObjects = scene.GetRootGameObjects();
    writer.WriteLine("## GameObject Hierarchy", scene.name);
    writer.WriteLine();
    writer.WriteLine("Minibuffer and other objects that are marked with `DontDestroyOnLoad` will not be shown.");
    writer.WriteLine();
    if (! includeComponents) {
      writer.WriteLine("Note: Use `C-u` to show components.");
      writer.WriteLine();
    } else {
      writer.WriteLine("Note: Components are shown as _ComponentName_.");
      writer.WriteLine();
    }

    foreach (var root in rootObjects) {
      ListGameObjects(root, writer, "", includeComponents);
    }
    Minibuffer.instance.ToBuffer("{0}.scene".Formatted(scene.name),
                                 writer.ToString());
  }

  [Command("describe-game-object",
           description = "Show a game object's components and children.",
           keyBinding = "C-h o")]
  public void DescribeGameObject([Prompt("Describe GameObject: ",
                                         completions = new [] { "*scene*" },
                                         completer = "GameObject",
                                         requireCoerce = false)]
                                 PromptResult<GameObject> pr) {
    var writer = new StringWriter();
    var go = pr.obj;
    if (go != null) {
      ListGameObjects(go, writer, "", true);
      Minibuffer.instance.ToBuffer(go.name, writer.ToString());
    } else if (pr.str == "*scene*") {
      DescribeScene(true);
    }
  }

  private void ListGameObjects(GameObject go, StringWriter writer,
                               string indent, bool includeComponents) {
    writer.WriteLine("{0}* {1}", indent, go.name);
    if (includeComponents)
      foreach (Component c in go.GetComponents<Component>())
        ListComponents(c, writer, "  " + indent);
    foreach (Transform child in go.transform) {
      ListGameObjects(child.gameObject, writer, "  " + indent, includeComponents);
    }
  }

  private void ListComponents(Component c, StringWriter writer, string indent) {
    writer.WriteLine("{0}- _{1}_", indent, c == null ? "(null)" : c.GetType().PrettyName());
  }

  [Command("toggle-game-object",
           description = "Toggle a game object to (in)active.")]
  public string ToggleGameObject([Prompt("Toggle GameObject: ",
                                         history = "GameObject",
                                         requireMatch = true)]
                                 GameObject go) {
    var activateP = ! go.activeSelf;
    go.SetActive(activateP);
    return "{0} {1}.".Formatted(activateP ? "Activated" : "Deactivated", go.name);
  }

  [Command(description = "Delete the player preferences. Use with caution.")]
  public void PlayerPrefsDelete() {
    minibuffer.ReadYesOrNo("Delete all player preferences stored by PlayerPrefs?")
      .Then(yes => {
          if (yes) {
            PlayerPrefs.DeleteAll();
            minibuffer.Message("Player preferences deleted.");
          } else {
            minibuffer.Message("Player preferences untouched.");
          }
        });
  }

  [Command("scene-reload",
           description = "Reload the current scene",
           keyBinding = "C-x r")]
  public IEnumerator SceneReload() {
    var scene = SceneManager.GetActiveScene();
    var name = scene.name;
    AsyncOperation operation = SceneManager.LoadSceneAsync(scene.buildIndex);
    var minibuffer = Minibuffer.instance;
    while(! operation.isDone) {
      yield return null;
      minibuffer.Echo(string.Format("Loading scene '{0}' progress {1}...", name, operation.progress));
    }
    minibuffer.Message("Loaded scene '{0}'.", name);
    // Coulda been simple but no.
    //Application.LoadLevel(Application.loadedLevel);
  }

  #if UNITY_5_4_OR_NEWER
  [Command("describe-build-scenes",
           description = "Show scenes in the build settings.",
           keyBinding = "C-h S")]
  public void DescribeBuildScenes() {
    var count = SceneManager.sceneCountInBuildSettings;
    if (count == 0) {
      Minibuffer.instance.Message("No build scenes available.");
      return;
    }

    string[][] table = Enumerable
      .Range(0, count)
      .Select(i => SceneManager.GetSceneByBuildIndex(i))
      .Select(s => new [] { s.name, s.isLoaded ? "YES" : "NO", s.isDirty ? "YES" : "NO", s.rootCount.ToString() })
      .Prepend(new [] {"Name", "Loaded?", "Dirty?", "root count"})
      .ToArray();

    minibuffer.ToBuffer("*build-scenes*",
@"## Build Scenes ({0})

{1}" .Formatted(count,
                HelpCommands.FormatTable(table, true)));
  }


  [Command("describe-loaded-scenes",
           description = "Show the scenes currently loaded.")]
  public void DescribeLoadedScenes() {
    var count = SceneManager.sceneCount;
    if (count == 0) {
      Minibuffer.instance.Message("No loaded scenes available.");
      return;
    }

    string[][] table = Enumerable
      .Range(0, count)
      .Select(i => SceneManager.GetSceneAt(i))
      .Select(s => new [] { s.name, s.isLoaded ? "YES" : "NO", s.isDirty ? "YES" : "NO", s.rootCount.ToString() })
      .Prepend(new [] {"Name", "Loaded?", "Dirty?", "root count"})
      .ToArray();

    minibuffer.ToBuffer("*loaded-scenes*", "## Loaded Scenes ({0})\n\n{1}"
                        .Formatted(count,
                                   HelpCommands.FormatTable(table, true)));
  }
  #endif

  [Command(description = "Pause or resume the game using Time.timeScale.",
           keyBinding = "s-\\")]
  public string TogglePause() {
    if (Time.timeScale == 0f) {
      Time.timeScale = 1f;
      return "Resume.";
    } else {
      Time.timeScale = 0f;
      return "Pause.";
    }
  }

  [Command(description = "Quit the game.",
           keyBinding = "C-x C-c")]
  public void GameQuit() {
    minibuffer.Message("Quitting game...");
    #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    // #elif UNITY_WEBPLAYER
    // Application.OpenURL(webplayerQuitURL);
    #else
    Application.Quit();
    #endif
  }
}
}
