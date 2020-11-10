/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {

/*
  Make the gameObject attached to this behaviour a long-lived singleton.
  It will stay alive, and any other instances of itself will be destoyed.
 */
// http://answers.unity3d.com/questions/576969/create-a-persistent-gameobject-using-singleton.html
public class DontDestroySingleton : MonoBehaviour {
  public static DontDestroySingleton instance;

  void Awake () {
    if (enabled) {
      if(instance == null) {
        instance = this;
        DontDestroyOnLoad(gameObject);
      } else {
        DestroyImmediate(gameObject);
      }
    }
  }

  void Start() { }

  void OnDestroy() {
    if (instance == this)
      instance = null;
  }
}
}
