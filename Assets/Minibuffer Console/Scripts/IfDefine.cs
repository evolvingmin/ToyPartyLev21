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
   ![IfDefine in the inspector](inspector/secret-key.png)
   Disable %Minibuffer with a compiler define.

   Suppose a developer wants to include %Minibuffer in their game but they'd
   prefer players not accidentally bump into it. They could disable Minibuffer by
   editing the scene or prefab, and producing two different builds. However, if
   instead one wants to not have to change the scene or source code, one can
   define a preprocessor directive, then they can use this script.

   When this script is enabled, it looks for the directive `ENABLE_MINIBUFFER`
   and will set its target object to active. (The target object is presumably
   'Minibuffer'.)  If it sees the directive `DISABLE_MINIBUFFER`, the target
   object is set to inactive.

   This solution even though it doesn't require any asset, scene, or source code
   changes does unfortunately require two builds. See the following classes for
   other options that do not require two builds.

   \seealso SecretArgument SecretKey
 */
public class IfDefine : MonoBehaviour {
  [Header("If ENABLE_MINIBUFFER is defined, target is activated.", order = 0)]
  [Header("If DISABLE_MINIBUFFER is defined, target is deactivated.", order = 1)]
  public GameObject target;

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
    yield return null;

#if ENABLE_MINIBUFFER
    target.SetActive(true);
    Debug.Log("Compiler define ENABLE_MINIBUFFER detected; turning " + target.name + " on.");
#endif

#if DISABLE_MINIBUFFER
    target.SetActive(false);
    Debug.Log("Compiler define DISABLE_MINIBUFFER detected; turning " + target.name + " off.");
#endif
  }
}
