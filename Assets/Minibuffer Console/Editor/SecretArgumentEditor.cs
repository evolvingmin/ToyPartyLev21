/*
  Copyright (c) 2017 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SeawispHunter.MinibufferConsole {

[CustomEditor(typeof(SecretArgument))]
public class SecretArgumentEditor : Editor {
  public override void OnInspectorGUI() {
    serializedObject.Update();
    var argumentName = serializedObject.FindProperty("argument").stringValue;

    var style = new GUIStyle(EditorStyles.label);
    style.wordWrap = true;
    GUILayout.Space(10);
    GUILayout.Label(string.Format("Turn Minibuffer Console on or off from the command line.\n"
                                          + "$ game.exe {0}; # Toggles Minibuffer.\n"
                                          + "$ game.exe {0} on; # Turns on Minibuffer.\n"
                                          + "$ game.exe {0} off; # Turns off Minibuffer.\n",
                                          argumentName),
                    style);

    EditorGUILayout.PropertyField(serializedObject.FindProperty("argument"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("setInitialTargetState"));
    // DrawDefaultInspector ();
    serializedObject.ApplyModifiedProperties();
  }

}
}
