/* HelloCommands.cs

   Step-by-step example how to add commands to Minibuffer.
*/

/* STEP 1. Reference the MinibufferConsole namespace. */
using SeawispHunter.MinibufferConsole;

/** Adds two simple commands: alt-x hello-world and alt-x hello. */
public class HelloCommands : UnityEngine.MonoBehaviour {

  void Start() {
/* STEP 2. Register with Minibuffer. */
    Minibuffer.Register(this);
  }

/* STEP 3. Add [Command] to any public methods. */
  [Command]
  public static string HelloWorld() {
    return "Hello, World!";
  }

  /* Command methods may be static or not. */
  [Command]
  public string Hello([Prompt(history = "name")] string name) {
    return "Hello, " + name + "!";
  }

/* STEP 4. Unregister if necessary. */
  void OnDestroy() {
    Minibuffer.Unregister(this);
  }

  /* Include this line to show the commands in the inspector. */
  public MinibufferListing minibufferExtensions;
}
