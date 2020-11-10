/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEditor;
using UnityEngine;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

[CustomPropertyDrawer (typeof (KeyChord))]
public class KeyChordDrawer : PropertyDrawer {


  public override void OnGUI (Rect position,
                              SerializedProperty property,
                              GUIContent label) {
    EditorGUI.BeginProperty (position, label, property);


    EditorGUI.PropertyField(position, property.FindPropertyRelative("serialization"), label);

    EditorGUI.EndProperty ();
  }
}
}
