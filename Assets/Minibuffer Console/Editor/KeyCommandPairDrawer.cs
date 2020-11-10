/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEditor;
using UnityEngine;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

[CustomPropertyDrawer (typeof (KeymapModifier.KeyCommandPair))]
public class KeyCommandPairDrawer : PropertyDrawer {


  public override void OnGUI (Rect position,
                              SerializedProperty property,
                              GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
    var left = position;
    left.width /= 4f;
    var right = position;
    right.x += left.width;
    right.width -= left.width;

    EditorGUI.PropertyField(left, property.FindPropertyRelative("keyBinding"), GUIContent.none);
    EditorGUI.PropertyField(right, property.FindPropertyRelative("command"), GUIContent.none);

    EditorGUI.EndProperty ();
  }
}
}
