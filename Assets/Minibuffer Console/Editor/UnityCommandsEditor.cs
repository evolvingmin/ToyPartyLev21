/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
#define MANUAL_SCENENAMES
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SeawispHunter.MinibufferConsole {

#if MANUAL_SCENENAMES || ! UNITY_5_4_OR_NEWER
[CustomEditor(typeof(UnityCommands))]
public class UnityCommandsEditor : Editor {
  private SerializedProperty sceneNamesProp;
  private void OnEnable() {
    sceneNamesProp = serializedObject.FindProperty("sceneNames");
  }
  public override void OnInspectorGUI() {
    serializedObject.Update();
    DrawDefaultInspector ();
    if (GUILayout.Button("Update Scene Names")) {
      UpdateSceneNames();
      //EditorUtility.SetDirty(target);
    }
    serializedObject.ApplyModifiedProperties();
  }

  // http://forum.unity3d.com/threads/custom-inspector-initializing-array.144346/
  private void UpdateSceneNames() {
    Undo.RecordObject(target, "Changed scene names.");
    var sceneNames = EditorBuildSettings.scenes
      .Where(s => s.enabled)
      .Select(s => s.path)
      .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
      .ToList();
    //sceneNamesProp.ClearArray();
    sceneNamesProp.arraySize = sceneNames.Count;
    for (int i = 0; i < sceneNames.Count; i++) {
      // if (i >= sceneNamesProp.arraySize)
      //   sceneNamesProp.InsertArrayElementAtIndex(i);
      var value = sceneNamesProp.GetArrayElementAtIndex(i);
      value.stringValue = sceneNames[i];
    }
  }
}
#endif
}
