/*
  Copyright (c) 2017 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace SeawispHunter.MinibufferConsole {

[CustomEditor(typeof(Note))]
public class NoteEditor : Editor {
  public override void OnInspectorGUI() {
    serializedObject.Update();
    var notes = serializedObject.FindProperty("editNotes");
    Texture icon = (Texture) notes.FindPropertyRelative("image").objectReferenceValue;
    var text = notes.FindPropertyRelative("text").stringValue;
    var link = notes.FindPropertyRelative("link").stringValue;

    if (icon != null) {
      var rect = GUILayoutUtility.GetRect(new GUIContent(icon),
                                          GUIStyle.none,
                                          GUILayout.ExpandWidth(true),
                                          GUILayout.ExpandHeight(true));
      // http://answers.unity3d.com/answers/633353/view.html
      var borderSize = 5;
      var borders = new RectOffset(borderSize - 10,
                                   borderSize,
                                   borderSize,
                                   borderSize);

      GUI.DrawTexture(borders.Remove(rect), icon);
      // GUI.DrawTexture(rect, icon);
    }

    if (! string.IsNullOrEmpty(text)) {
      // EditorGUILayout.HelpBox(text, MessageType.None);
      var style = new GUIStyle(EditorStyles.label);
      style.wordWrap = true;
      GUILayout.Label(text, style);
    }

    if (! string.IsNullOrEmpty(link)) {
      string domain = link;
      var match = Regex.Match(link, @"^(?:https?://)?(?:[^@]+@)?(?:www\.)?([^:/]+)", RegexOptions.IgnoreCase);
      if (match.Success) {
        domain = match.Groups[1].Value;
      }
      MakeLink(domain, link, link, EditorStyles.label);
    }

    EditorGUILayout.PropertyField(notes, true);
    serializedObject.ApplyModifiedProperties();
  }

  public static void MakeLink(string label, string url, string tooltip, GUIStyle style) {
    var linkStyle = new GUIStyle(style);
    // orange
    // linkStyle.normal.textColor = new Color(225/255f, 145/255f, 51/255f);
    // blue
    linkStyle.normal.textColor = new Color(1/255f, 150/255f, 214/255f);
    linkStyle.fontStyle = FontStyle.Bold;
    var link = new GUIContent(label, tooltip);
    var linkRect = GUILayoutUtility.GetRect(link,
                                            linkStyle);
    if (GUI.Button(linkRect, link, linkStyle)) {
      Application.OpenURL(url);
    }
  }

}
}
