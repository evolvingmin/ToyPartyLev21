/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

[CustomEditor(typeof(MiniInfo))]
public class MiniInfoEditor : Editor {

  public override void OnInspectorGUI() {
    serializedObject.Update();
    MiniInfo ma = (MiniInfo) serializedObject.targetObject;
    var objName = ma.name;

    var advancedOptions = serializedObject.FindProperty("advancedOptions");
    var cname = (ma.customName && ! ma.commandName.IsZull())
      ? ma.commandName
      : advancedOptions.FindPropertyRelative("prefix").stringValue + " " + objName;
    cname = MinibufferListingsDrawer.CanonizeCommand(cname);
    EditorGUILayout.LabelField("Minibuffer Command", cname);

    var customName = serializedObject.FindProperty("customName");
    EditorGUILayout.PropertyField(customName, new GUIContent("Custom name?"));
    if (customName.boolValue)
      EditorGUILayout.PropertyField(serializedObject.FindProperty("commandName"));

    var bindKey = serializedObject.FindProperty("bindKey");
    EditorGUILayout.PropertyField(bindKey, new GUIContent("Bind key?"));
    if (bindKey.boolValue)
      EditorGUILayout.PropertyField(serializedObject.FindProperty("keyBinding"));

    var useTextAsset = serializedObject.FindProperty("useTextAsset");
    EditorGUILayout.PropertyField(useTextAsset, new GUIContent("Use text asset?"));
    if (useTextAsset.boolValue)
      EditorGUILayout.PropertyField(serializedObject.FindProperty("textAsset"));
    else
      EditorGUILayout.PropertyField(serializedObject.FindProperty("info"));
    // EditorGUILayout.PropertyField(serializedObject.FindProperty("text"));

    EditorGUILayout.PropertyField(advancedOptions, true);
    serializedObject.ApplyModifiedProperties();
  }
}
}
