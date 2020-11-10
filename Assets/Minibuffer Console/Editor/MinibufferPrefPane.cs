/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

// https://docs.unity3d.com/ScriptReference/PreferenceItem.html
public class MinibufferPrefPane : MonoBehaviour
{
  // Have we loaded the prefs yet
  private static bool prefsLoaded = false;
  private static ChangeCase.Case commandCase;
  private static ChangeCase.Case variableCase;
  private static Minibuffer.Notation keyNotation;
  private static Texture icon;

  [PreferenceItem("Minibuffer")]
  public static void PreferencesGUI()
  {
    // Load the preferences.
    if (! prefsLoaded) {
      commandCase = GetCasePref("minibuffer.command-case");
      variableCase = GetCasePref("minibuffer.variable-case");

      keyNotation = (Minibuffer.Notation)
        System.Enum.Parse(typeof(Minibuffer.Notation),
                          EditorPrefs.GetString("minibuffer.key-notation",
                                                Minibuffer.Notation.Standard.ToString()));
      prefsLoaded = true;
    }

    // Preferences GUI
    ShowHeader();

    EditorGUILayout.HelpBox("These preferences control how MinibufferListings are shown in the inspector.",
                            MessageType.Info);

    EditorGUILayout.LabelField("Do you prefer M-x or alt-x?", EditorStyles.boldLabel);
    // keyNotation = (Minibuffer.Notation) EditorGUILayout.EnumPopup("Key Notation", keyNotation);
    keyNotation = (Minibuffer.Notation) MinibufferPrefPane.EnumPopup("Key Notation",
                                                                     keyNotation, x => x.GetDescription());

    EditorGUILayout.Space();

    EditorGUILayout.LabelField("PascalCase, camelCase, kebab-case, or as is?", EditorStyles.boldLabel);
    // commandCase = (ChangeCase.Case) EditorGUILayout.EnumPopup("Command Case", commandCase);
    commandCase = (ChangeCase.Case) MinibufferPrefPane.EnumPopup("Command Case", commandCase, x => x.GetDescription());
    // variableCase = (ChangeCase.Case) EditorGUILayout.EnumPopup("Variable Case", variableCase);
    variableCase = (ChangeCase.Case) MinibufferPrefPane.EnumPopup("Variable Case", variableCase, x => x.GetDescription());

    EditorGUILayout.Space();

    EditorGUILayout.LabelField(MinibufferVersion.ShowVersion(false), EditorStyles.wordWrappedLabel);

    // Save the preferences.
    if (GUI.changed) {
      EditorPrefs.SetString("minibuffer.command-case", commandCase.ToString());
      EditorPrefs.SetString("minibuffer.variable-case", variableCase.ToString());
      EditorPrefs.SetString("minibuffer.key-notation", keyNotation.ToString());
    }
  }

  public static void ShowHeader() {
    // It'd be nice to just stick these textures into something rather than
    // loading them like this. But that would probably require a
    // ScriptableObject or something.

    // Consider using GUIDToAssetPath
    // black GUID: 3820a82d3a79a41aaa2f0858f5b874af
    // white GUID: d4f6720b8692b40c7a6d971d33247987
    if (icon == null) {
      icon = (Texture) AssetDatabase
        .LoadAssetAtPath(string.Format("Assets/Minibuffer Console/Textures/MinibufferIcon{0}Text.png",
                                       EditorGUIUtility.isProSkin ? "White" : "Black"),
                         typeof(Texture));
      if (icon == null) {
        // Make this work even if the directory structure is changed.
        icon = (Texture) AssetDatabase
          .LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(EditorGUIUtility.isProSkin
                                                         ? "d4f6720b8692b40c7a6d971d33247987"
                                                         : "3820a82d3a79a41aaa2f0858f5b874af"),
                           typeof(Texture));
      }
    }
    var rect = GUILayoutUtility.GetRect(new GUIContent(icon),
                                        GUIStyle.none,
                                        GUILayout.ExpandWidth(true),
                                        GUILayout.ExpandHeight(false));
    // http://answers.unity3d.com/answers/633353/view.html
    GUI.DrawTexture(rect, icon);
    if (GUI.Button(rect, "", GUIStyle.none)) {
      Application.OpenURL("http://seawisphunter.com");
    }
    var rightAlign = new GUIStyle(EditorStyles.label);
    rightAlign.alignment = TextAnchor.MiddleRight;
    EditorGUILayout.BeginHorizontal();
    NoteEditor.MakeLink("@shanecelis", "http://twitter.com/shanecelis", "Go to twitter.", EditorStyles.label);
    NoteEditor.MakeLink("http://seawisphunter.com", "http://seawisphunter.com", "Go to website.", rightAlign);
    EditorGUILayout.EndHorizontal();
  }

  public static T EnumPopup<T>(string label, T selected, Func<T, string> display) where T : struct {
    var map = new BidirectionalMap<T, string>();
    foreach(var v in Enum.GetValues(typeof(T)))
      map[(T)v] = display((T) v);

    var displayOptions = map.forward.Values.ToArray();
    var index = Array.IndexOf(displayOptions, map[selected]);
    var chosenIndex = EditorGUILayout.Popup(label, index, displayOptions);
    return map.reverse[displayOptions[chosenIndex]];
  }

  private static ChangeCase.Case GetCasePref(string name) {
    return (ChangeCase.Case)
      System.Enum.Parse(typeof(ChangeCase.Case),
                        EditorPrefs.GetString(name, ChangeCase.Case.KebabCase.ToString()));
  }

}

// A bidirectional map.

public class BidirectionalMap<T1, T2> {
  public Dictionary<T1, T2> forward = new Dictionary<T1, T2>();
  public Dictionary<T2, T1> reverse = new Dictionary<T2, T1>();
  public BidirectionalMap() {}
  public T2 this[T1 t1] {
    get { return forward[t1]; }
    set { Add(t1, value); }
  }
  public void Add(T1 t1, T2 t2) {
    forward.Add(t1, t2);
    reverse.Add(t2, t1);
  }
}
}
