/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeawispHunter.MinibufferConsole {

public class FixLightingOnLevelLoad : MonoBehaviour {
  void Start() {
#if UNITY_5_4_OR_NEWER
    SceneManager.sceneLoaded += SceneLoaded;
#endif
  }

#if UNITY_5_4_OR_NEWER
  void SceneLoaded(Scene scene, LoadSceneMode mode) {
#else
  void OnLevelWasLoaded(int level) {
#endif
#if UNITY_EDITOR && UNITY_5_4_OR_NEWER
    if (enabled && UnityEditor.Lightmapping.giWorkflowMode == UnityEditor.Lightmapping.GIWorkflowMode.Iterative) {
      Debug.Log("Fixing lighting on level change in editor.");
      DynamicGI.UpdateEnvironment();
    }
#endif
  }
}
}
