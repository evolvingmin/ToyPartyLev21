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
/*
  Add a header image to Minibuffer Editor window.  If it's clicked, it
  goes to the website.
 */
[CustomEditor(typeof(Minibuffer))]
public class MinibufferEditor : Editor {

  private void OnEnable() {
  }

  public override void OnInspectorGUI() {

    serializedObject.Update();
    MinibufferPrefPane.ShowHeader();

    EditorGUILayout.PropertyField(serializedObject.FindProperty("_visible"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("_fontSize"));

    EditorGUILayout.LabelField("Tab completion settings", EditorStyles.boldLabel);
    var indent = EditorGUI.indentLevel;
    EditorGUI.indentLevel = 1;
    EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoCompleteMatcher"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("_caseSensitive"));

    // EditorGUILayout.BeginHorizontal();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("showAnnotations"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("showGroups"));
    // EditorGUILayout.EndHorizontal();
    EditorGUILayout.Space();

    EditorGUI.indentLevel = 0;


    EditorGUILayout.LabelField("Conventions", EditorStyles.boldLabel);
    EditorGUI.indentLevel = 1;
    var keyNotation = serializedObject.FindProperty("keyNotation");
    EditorGUILayout.LabelField("Do you prefer alt-x or M-x?", EditorStyles.boldLabel);
    // keyNotation = (Minibuffer.Notation) EditorGUILayout.EnumPopup("Key Notation", keyNotation);
    keyNotation.intValue = (int) MinibufferPrefPane.EnumPopup<Minibuffer.Notation>("Key Notation",
                                                                                   (Minibuffer.Notation)
                                                                                   keyNotation.intValue,
                                                                                   x => x.GetDescription());

    EditorGUILayout.Space();

    EditorGUILayout.LabelField("PascalCase, camelCase, kebab-case, or as is?", EditorStyles.boldLabel);
    var commandCase = serializedObject.FindProperty("commandCase");
    // commandCase = (ChangeCase.Case) EditorGUILayout.EnumPopup("Command Case", commandCase);
    commandCase.intValue = (int) MinibufferPrefPane.EnumPopup("Command Case",
                                                              (ChangeCase.Case) commandCase.intValue,
                                                              x => x.GetDescription());
    // variableCase = (ChangeCase.Case) EditorGUILayout.EnumPopup("Variable Case", variableCase);
    var variableCase = serializedObject.FindProperty("variableCase");
    variableCase.intValue = (int) MinibufferPrefPane.EnumPopup("Variable Case",
                                                               (ChangeCase.Case) variableCase.intValue,
                                                               x => x.GetDescription());

    EditorGUI.indentLevel = indent;
    // EditorGUILayout.ObjectField("ha", null, typeof(MinibufferListing), false);

    EditorGUILayout.PropertyField(serializedObject.FindProperty("persistHistory"));
    // EditorGUILayout.PropertyField(serializedObject.FindProperty("messageSettings"), true);
    // EditorGUILayout.PropertyField(serializedObject.FindProperty("showOnStart"));

    EditorGUILayout.PropertyField(serializedObject.FindProperty("GUIElements"), true);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("advancedOptions"), true);
    EditorGUILayout.PropertyField(serializedObject.FindProperty("minibufferBuiltins"), true);

    // if (GUILayout.Button("Set Case Preferences for Editor")) {
    //   Minibuffer m = (Minibuffer) target;
    //   EditorPrefs.SetString("minibuffer.command-case", m.commandCase.ToString());
    //   EditorPrefs.SetString("minibuffer.variable-case", m.variableCase.ToString());
    // }
    serializedObject.ApplyModifiedProperties();
  }
}
}
