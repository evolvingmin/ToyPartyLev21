/* QuadrupusCommands.cs

   A more advanced example of integrating Minibuffer. Used in the demo scenes.

   See HelloCommands.cs for simpler, step-by-step instructions how to add commands.
 */
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;
// using MoreLinq;

/* STEP 1. Reference the MinibufferConsole namespace. */
using SeawispHunter.MinibufferConsole;

namespace SeawispHunter.MinibufferConsole {

/**
   An example of Minibuffer commands and variables with a "quadrupus", a
   four-legged robot. Used in the scenes:

   - 0 Quadrapus
   - 1 Snake Fight

   It show cases how to add commands, variables, and key bindings to Minibuffer
   including some advanced features like dynamically generated commands and
   variables. Notes are also provided on instances where one of Minibuffer's
   components like MiniAction or MiniToggler could be used instead of the coded
   command.

   \see HelloCommands for simpler, step-by-step instructions how to add commands.
 */
public class QuadrapusCommands : MonoBehaviour {

  /* These four fields should be set up in the inspector. */
  public GameObject quadrapus;
  public Rigidbody quadrapusRootBody;
  public Transform cubePrefab;
  public Text _quote;

  /* Expose a field as a _variable_ in Minibuffer. */
  [Variable]
  public int score = 0;

  /* Expose a property as a _variable_ in Minibuffer. */
  [Variable]
  public string quote {
    get { return _quote.text; }
    set { _quote.text = value; }
  }

  /* This will show all the commands, variables, and key bindings known at
     compile-time in the inspector.  It does not need to be instantiated. */
  public MinibufferListing minibufferExtensions;

  void Start () {
/* STEP 2. Register all commands, variables, and key bindings with Minibuffer. */
    Minibuffer.Register(this);

    /* If you want to setup some other things at the start, using
       Minibuffer.With() is recommended. */

    // Minibuffer.With(minibuffer => {
    //     Keymap keymap = minibuffer.GetKeymap("user", true);
    //     keymap["C-d"]         = "cube-drop";
    //     /* We could setup some key bindings here, but this can also be done in
    //        the [Command("cube-drop", keyBinding = "C-d"] tag.  See below. */
    //
    //     /* Add a new variable at runtime. This variable would actually read-only. */
    //     minibuffer
    //     .RegisterVariable<string>(new Variable("dynamic-var"),
    //                               () => "my var",
    //                               (s) => { Minibuffer.instance.Message("Set " + s + " but not really."); });
    //   });
  }

/* STEP 3. Add [Command] attributes to any public method. */
  [Command(description = "Cheat code with ad hoc completions.")]
  public static string GoodCheatCode(/* Add custom ad hoc completions. */
                                     [Prompt(completions = new []
                                         { "GodMode", "KonamiCode" })]
                                     string str) {
    // ... your cheat implementation here ...
    return str + " activated.";
  }

  // [Command]
  // public static void CheckRead() {
  //   Minibuffer.instance.Read<string>("WTF: ")
  //     .Then(s => {
  //         Minibuffer.instance.Message("Got " + s);
  //         });
  // }

  // [Command]
  // public static void CheckRead2() {
  //   // This doesn't work.
  //   Minibuffer.instance.Read<PromptResult>("WTF: ")
  //     .Done(s => {
  //         Minibuffer.instance.Message("Got " + s.str);
  //       });
  // }

  // [Command]
  // public static void CheckRead3(PromptResult s) {
  //   // This does work.
  //   Minibuffer.instance.Message("Got " + s.str);
  // }

  // [Command]
  // public static void CheckRead4() {
  //   // This doesn't work.
  //   Minibuffer.instance.Read<PromptResult<string>>("WTF: ")
  //     .Done(s => {
  //         Minibuffer.instance.Message("Got " + s.obj);
  //       });
  // }

  // [Command]
  // public static void CheckRead5(PromptResult<string> s) {
  //   Minibuffer.instance.Message("Got " + s.obj);
  // }

  // [Command]
  // public static void CheckRead6() {
  //   // This doesn't work.
  //   Minibuffer.instance.Read<PromptResult<int>>("WTF: ")
  //     .Done(s => {
  //         Minibuffer.instance.Message("Got " + s.obj);
  //       });
  // }

  /* Or instead of custom completions, just use an enumeration. */
  public enum Cheat { GodMode, KonamiCode };

  /* Command methods may be static or not. */
  [Command(description = "Cheat code with completions from enumeration.")]
  public string BetterCheatCode(Cheat c) {
    // ... your cheat implementation here ...
    return c + " activated.";
  }

  /** Expose methods as commands in Minibuffer. */
  [Command("quadrapus-detach",
           description = "Drop the quadrapus from its hook")]
  public string Detach() {
    quadrapusRootBody.isKinematic = false;
    /* For convenience, if we return a string from a command, it'll be
       reported as a message. */
    return "Detached.";
    /* This is equivalent to:

    Minibuffer.instance.Message("Detached.");
    */

    /* Note that quadrapus-detach's functionality could be easily replicated by
       a MiniAction component with no code. */
  }

  /* The quadrapus has two scripts attached to each body part, we toggle them on
     and off. */
  [Command("quadrapus-twitch",
           description = "Toggle quadrapus twitching")]
  public void Twitch() {
    quadrapus.GetComponentsInChildren<Twitchy>().Each(x => x.enabled = !x.enabled);
    quadrapus.GetComponentsInChildren<Snakey>().Each(x => x.enabled = false);

    /* Each() is an extension method that takes an action. It is purely for
       convenience. It is equivalent to this code:

    foreach(var x in quadrapus.GetComponentsInChildren<Snakey>()) {
      x.enabled = false;
    }
    */
  }

  [Command("quadrapus-snake",
           description = "Toggle sinusoidal movement")]
  public void Snake() {
    quadrapus.GetComponentsInChildren<Snakey>().Each(x => x.enabled = !x.enabled);
    quadrapus.GetComponentsInChildren<Twitchy>().Each(x => x.enabled = false);
  }

  [Command(description = "Set material of quadrapus. Shows preview in editor.")]
  public string QuadrapusMaterial(Material m) {
    foreach(var renderer in quadrapus
            .GetComponentsInChildren<MeshRenderer>())
      renderer.material = m;
    return "Changed material to " + m.name + ".";
  }

  [Command(description = "Set color of quadrapus")]
  // requireMatch is false so that coercion may be used.
  public string QuadrapusColor([Prompt(requireMatch = false)]
                               Color c) {
    var renderers = quadrapus.GetComponentsInChildren<MeshRenderer>();
    foreach(var renderer in renderers) {
      renderer.material.SetColor("_Color", c);
    }
    return "Changed color to " + c + ".";
  }

  /* Minibuffer comes with its own built-in game! Can you guess the number? */
  [Command("guess-a-number",
           description = "A guessing game")]
  public string GuessANumber(int guess) {
    if (guess > 42)
      return "Too high.";
    else if (guess < 42)
      return "Too low.";
    else
      return "You got it! 42, the answer to life, the universe and everything!";
  }

  [Command("cube-drop",
           description = "Drop a cube into the scene",
           /* Either standard or Emacs key sequence notation can be used to set
              key bindings. */
           // keyBinding = "C-d",
           keyBinding = "ctrl-d")]
  public IEnumerator CubeDrop([UniversalArgument]
                              int count) {
    for (int i = 0; i < count; i++) {
      var pos = UnityEngine.Random.insideUnitSphere;
      Instantiate(cubePrefab,
                  quadrapusRootBody.transform.position + pos * 5f + Vector3.up * 5f,
                  Quaternion.identity);
      yield return null;
    }
    if (count == 1 && ! seenCubeDropTip) {
      Minibuffer.instance.Message("Try C-u 1000 M-x cube-drop for some #Cubefetti!");
      seenCubeDropTip = true;
    }
  }
  private bool seenCubeDropTip = false;

  [Command(description = "Go to seawisphunter.com")]
  /** This is used by the Logo Button, but we can easily make it an
      interactive command too. */
  public void GotoSeawispHunter() {
    Application.OpenURL("http://seawisphunter.com");
  }

  [Command(description = "Go to Unity Asset Store page for Minibuffer")]
  /** This is used by the Logo Button, but we can easily make it an
      interactive command too. */
  public void GotoAssetStore() {
    Application.OpenURL("http://u3d.as/B8H");
  }

  [Command(description = "Randomly pick a cheerful color",
           keyBinding = "ctrl-c b")]
  public void RandomizeBackgroundColor() {
    Camera.main.backgroundColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
  }

  [Command(description = "Dynamically add two commands")]
  public string CreateDynamicCommands() {
    var minibuffer = Minibuffer.instance;
    minibuffer
      .RegisterCommand("my-dynamic-command", () => {
          minibuffer.Message("Make commands on the fly!");
        });

    minibuffer
      .RegisterCommand("my-dynamic-command-with-args", (string s) => {
          minibuffer.Message("They can take arguments too!  Like '{0}'.", s);
        });
    return "Added my-dynamic-command and my-dynamic-command-with-args.";
  }

  // [Command(keyBinding = "f")]
  public void TestReadSet() {
    // var dict = new Dictionary<object, bool>();
    // dict["Somebody"] = true;
    // dict["Set"] = true;
    // dict["Us"] = true;
    // dict["Up"] = false;
    // dict["the"] = true;
    // dict["Bomb"] = true;

    Minibuffer.instance.ReadSet("Read it: ", new [] { "Someone", "Set", "Us", "Up", "The", "Bomb" })
      .Then(result => {
          Minibuffer.instance.Message("Got " + string.Join(" ", result.ToArray()));
        });
  }

  // [Command(keyBinding = "T")]
  // public string TestReadGameObject(GameObject objs) {
  //   return "Got " + objs.name;
  // }

  // // [Command(keyBinding = "G")]
  // public string TestReadGameObjects(GameObject[] objs) {
  //   return "Got " + string.Join(", ", objs.Select(x => x.name).ToArray());
  // }

  /**

   */



  // /** Accepts a set of colors, then recolors all the quadrapus body parts using
  //     that coloring cycle. */
  // [Command]
  // public string QuadrapusRecolor(Color[] colors) {
  //   // A parameter of IEnumerable<Color> or List<Color> would work the same.
  //   if (! colors.Any()) {
  //     return "Must select a color.";
  //   }
  //   quadrapus
  //     .GetComponentsInChildren<Renderer>()
  //     .OrderByHierarchy()
  //     .Zip(MoreEnumerable.Repeat(colors),
  //          (r, c) => r.material.color = c)
  //     .ToList();
  //   return "Done.";
  // }

  [Command(description = "Recolor using a set of colors (demo toggles).")]
  public string RecolorGameObjects(GameObject[] objs, Color[] colors) {
    if (! colors.Any()) {
      return "Must select a color.";
    }
    objs
      .Select(go => go.GetComponent<Renderer>())
      .OrderByHierarchy()
      .Zip(colors,
           // MoreEnumerable.Repeat(colors),
           (r, c) => r.material.color = c)
      .ToList();
    return "Done.";
  }

  [Command(description = "Recolor quadrapus using a set of colors (demo toggles).")]
  public string QuadrapusRecolor(Color[] colors) {
    return RecolorGameObjects(quadrapus
                              .GetComponentsInChildren<Renderer>()
                              .Select(t => t.gameObject)
                              .ToArray(), colors);
  }

  // [Command(keyBinding = "n")]
  // public string TestReadNumbers(IEnumerable<int> objs) {
  //   return "Got " + string.Join(", ", objs.Select(x => x.ToString()).ToArray());
  // }

  /* This command is implemented in UnityCommands. If there was a particular
     game object, however, that you wanted to toggle then the MiniToggler
     component might be easier. */
  // [Command]
  // public void ToggleGameObject(GameObject go) {
  //   go.SetActive(! go.activeSelf);
  // }

  /* This functionality actually works now, but it's in TwitterCommands. */
  // [Command]
  // public string TweetScreenshot([Prompt("To twitter account: ",
  //                                       completions = new []
  //                                           { "@shanecelis",
  //                                             "@stupidmassive",
  //                                             "@unormal" })]
  //                               string str) {
  //   Minibuffer.instance.ExecuteCommand("capture-screenshot");
  //   // ... Magic twitter code goes here ...
  //   return "Tweeted screenshot to " + str + "."; // Not really.
  //                                                // Good idea though!
  // }

/* STEP 4. Unregister if necessary. */
  void OnDestroy() {
    /* Unregister from Minibuffer. It's undefined what happens if you run
       commands whose objects have been destroyed. */
    Minibuffer.Unregister(this);

    /* If you've added some things at runtime, you may want to have a block like
       this: */
    // Minibuffer.With(m => {
    //     m.UnregisterCommand("my-dynamic-command");
    //     m.UnregisterCommand("my-dynamic-command-with-args");
    //     // m.UnregisterVariable("dynamic-var");
    //   });
  }


  /*
    In order to work in an Ahead Of Time (AOT) Compilation, you have to specify
    which types you will be instantiating. Most types are already covered.
    However, if you get an error like this

    ExecutionEngineException: Attempting to call method
    'SeawispHunter.MinibufferConsole.Interpreter::Fill' for which no ahead of
    time (AOT) code was generated.

    you'll want to add your custom types like we do here for the enum Cheat:
  */
  public static void _AotWorkAround() {
    Interpreter._AotWorkAround<Cheat>();
    throw new System.InvalidOperationException
      ("This method is used for AOT code generation only. Do not call at runtime.");
  }

}
}
