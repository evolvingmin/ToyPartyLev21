/*
  Copyright (c) 2017 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Linq;
using System.Collections;
using SeawispHunter.MinibufferConsole;
using SeawispHunter.MinibufferConsole.Extensions;

/**
   ![Secret key in the inspector](inspector/secret-key.png)
   Toggle %Minibuffer with a Secret Command Line Argument.

   Suppose a developer wants to include %Minibuffer in their game but they'd
   prefer players not accidentally bump into it. They could disable Minibuffer, but
   then their testers wouldn't have access to it unless they want to provide two
   separate builds. %SecretArgument lets one include %Minibuffer with some
   obscurity.

   Instead a developer could provide one build of their game and enable this
   SecretArgument script; it's called 'Secret Argument' in the '%Minibuffer Console'
   prefab. Select the argument, say, "-minibuffer" and only tell
   their testers about the secret argument that activates %Minibuffer.

   - macOS
   ```
   $ ./game.app/Contents/MacOS/game -minibuffer
   ```

   - Windows
   ```
   > game.exe -minibuffer
   ```


   Or if obfuscation is not important, one might add a 'Developer Mode' in their
   settings that activates %Minibuffer.
 */
public class SecretArgument : MonoBehaviour {
  public enum TargetState {
    Activate,
    Deactivate,
    LeaveAsIs
  };
  [Header("The command line argument for the game.", order = 0)]
  // [Header("By default it is: -minibuffer", order = 1)]
  public string argument = "-minibuffer";
  public GameObject target;
  public TargetState setInitialTargetState = TargetState.LeaveAsIs;

  IEnumerator Start() {
    if (target == null) {
        Debug.LogWarning("No target selected for secret argument; disabling.");
        this.enabled = false;
        yield break;
    }

    // Let's yield so that if there was any setup in the target object, it
    // happens. If we don't do this, sometimes it sets up; sometimes it doesn't.
    // Why introduce that kind of random behavior if we don't have to.
    yield return null;

    switch (setInitialTargetState) {
      case TargetState.LeaveAsIs:
        break;
      case TargetState.Activate:
        target.SetActive(true);
        break;
      case TargetState.Deactivate:
        target.SetActive(false);
        break;
    }
    var args = System.Environment.GetCommandLineArgs();
    for (int i = 0; i < args.Length; i++) {
      if (args[i] == argument) {
        if (i + 1 < args.Length) {
          if (args[i] == "on") {
            Debug.Log("Secret argument detected; turning " + target.name + " on.");
            target.SetActive(true);
            yield break;
          } else if (args[i] == "off") {
            Debug.Log("Secret argument detected; turning " + target.name + " off.");
            target.SetActive(false);
            yield break;
          }
        }
        Toggle();
        Debug.Log("Secret argument detected; toggling " + target.name + " to " + (target.activeSelf ? "on" : "off") + " state.");
      }
    }
    // if (args.Contains(argument)) {
    //   Toggle();
    //   Debug.Log("Secret argument detected; toggling " + target.name + " to " + (target.activeSelf ? "on" : "off") + " state.");
    // }
  }

  void Toggle() {
    target.SetActive(! target.activeSelf);
  }
}
