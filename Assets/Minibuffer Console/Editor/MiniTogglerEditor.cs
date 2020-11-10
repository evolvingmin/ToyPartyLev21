/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

[CustomEditor(typeof(MiniToggler)), CanEditMultipleObjects]
public class MiniTogglerEditor : Editor {

  public override void OnInspectorGUI() {
    serializedObject.Update();
    MiniToggler ma = (MiniToggler) serializedObject.targetObject;
    var objName = (ma.customTarget && ma.target != null) ? ma.target.name : ma.name;

    var hasCommand = serializedObject.FindProperty("commandOrVariable").intValue
      == (int) MiniToggler.CommandOrVariable.Command
      || serializedObject.FindProperty("commandOrVariable").intValue
      == (int) MiniToggler.CommandOrVariable.Both;

    var hasVariable = serializedObject.FindProperty("commandOrVariable").intValue
      == (int) MiniToggler.CommandOrVariable.Variable
      || serializedObject.FindProperty("commandOrVariable").intValue
      == (int) MiniToggler.CommandOrVariable.Both;
    var advancedOptions = serializedObject.FindProperty("advancedOptions");
    if (hasCommand) {
      var cname = (ma.customName && ! ma.commandName.IsZull())
        ? ma.commandName
        : advancedOptions.FindPropertyRelative("prefix").stringValue + " " + objName;
      cname = MinibufferListingsDrawer.CanonizeCommand(cname);
      if (serializedObject.targetObjects.Length == 1) {
        EditorGUILayout.LabelField("Minibuffer Command", cname);
      } else {
        EditorGUILayout.LabelField("Minibuffer Command", "-");
      }
    }

    if (hasVariable) {
      var vname = (ma.customName && ! ma.variableName.IsZull())
        ? ma.variableName
        : objName;
      vname = MinibufferListingsDrawer.CanonizeVariable(vname);
      if (serializedObject.targetObjects.Length == 1) {
        EditorGUILayout.LabelField("Minibuffer Variable", vname);
      } else {
        EditorGUILayout.LabelField("Minibuffer Variable", "-");
      }
    }

    EditorGUILayout.PropertyField(serializedObject.FindProperty("commandOrVariable"), new GUIContent("Command, variable, or both?"));
    var customName = serializedObject.FindProperty("customName");
    EditorGUILayout.PropertyField(customName, new GUIContent("Custom name?"));
    if (customName.boolValue && hasCommand)
      EditorGUILayout.PropertyField(serializedObject.FindProperty("commandName"));

    if (customName.boolValue && hasVariable)
      EditorGUILayout.PropertyField(serializedObject.FindProperty("variableName"));
    var bindKey = serializedObject.FindProperty("bindKey");
    if (hasCommand) {
      EditorGUILayout.PropertyField(bindKey, new GUIContent("Bind key?"));
      if (bindKey.boolValue)
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keyBinding"));
    }
    // EditorGUILayout.LabelField("Use a custom target if the GameObject starts "
    //                            + "as inactive otherwise self is the target.",
    //                            EditorStyles.wordWrappedLabel);
    var targetChoice = serializedObject.FindProperty("targetChoice");

    EditorGUILayout.PropertyField(targetChoice);
    if (targetChoice.intValue == (int) MiniToggler.Target.GameObject)
      EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
    else if (targetChoice.intValue == (int) MiniToggler.Target.ActionPair) {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("_state"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("onAction"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("offAction"));
    }
    EditorGUILayout.PropertyField(serializedObject.FindProperty("targetState"));

    EditorGUILayout.PropertyField(serializedObject.FindProperty("advancedOptions"), true);
    if (! ma.gameObject.activeSelf) {
      EditorGUILayout.HelpBox("Warning: If this GameObject starts as deactivated, "
                              + "it will not register its commands.",
                              MessageType.Warning);
    }
    serializedObject.ApplyModifiedProperties();
  }
}
}
