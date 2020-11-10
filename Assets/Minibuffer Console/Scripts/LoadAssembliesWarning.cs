/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SeawispHunter.MinibufferConsole {

public class LoadAssembliesWarning : MonoBehaviour {

  [Header("Minibuffer does not support live code reloading.")]
  public bool quitOnCompile = false;
  // [Header("Restrict code reloading while playing?")]
  // public bool lockAssembliesWhilePlaying = false;

  private static Queue<Action> onWarning = new Queue<Action>();

  public static void OnWarning(Action action) {
    onWarning.Enqueue(action);
  }

  // void OnEnable() {
  //   if (lockAssembliesWhilePlaying)
  //     Lock();
  // }

  // void OnDisable() {
  //   if (lockAssembliesWhilePlaying)
  //     Unlock();
  // }

  // void OnDestroy() {
  //   if (lockAssembliesWhilePlaying)
  //     Unlock();
  // }

  // void Lock() {
  //   #if UNITY_EDITOR
  //   EditorApplication.LockReloadAssemblies();
  //   #endif
  // }

  // void Unlock() {
  //   #if UNITY_EDITOR
  //   EditorApplication.UnlockReloadAssemblies();
  //   #endif
  // }

  #if UNITY_EDITOR
  private bool warningShown = false;
  void OnDrawGizmos() {
    if (! warningShown
        && UnityEditor.EditorApplication.isCompiling &&
        (UnityEditor.EditorApplication.isPlaying
         || UnityEditor.EditorApplication.isPaused)) {
      /**
         I actually went to significant pains to make Minibuffer work
         with live code reloading early in its development; however, I
         couldn't justify the pain.  In general I don't expect live
         code reloading to work in Unity.  Nothing in the asset store
         broadcasts that even as a relevant feature worth supporting,
         so I ditched support for it.
       */
      Debug.Log("Warning: Compiled during play; Minibuffer does not support live code reloading.");
      warningShown = true;
      while (onWarning.Any()) {
        var action = onWarning.Dequeue();
        action();
      }

      UnityEditor.EditorApplication.Beep();
      if (quitOnCompile) {
        Debug.Log("Warning: Compiled during play; quitting.");
        UnityEditor.EditorApplication.isPlaying = false;
      }
    }
  }
  #endif

}

}
