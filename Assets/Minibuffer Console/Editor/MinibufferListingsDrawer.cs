/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEditor;
using UnityEngine;
using System.Linq;
using SeawispHunter.MinibufferConsole;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
// http://catlikecoding.com/unity/tutorials/editor/custom-data-old/
// https://docs.unity3d.com/ScriptReference/PropertyDrawer.html
[CustomPropertyDrawer (typeof (MinibufferListing))]
public class MinibufferListingsDrawer : PropertyDrawer {

  public static string CanonizeCommand(string s) {
    return Canonize("minibuffer.command-case", s);
  }

  public static string CanonizeVariable(string s) {
    return Canonize("minibuffer.variable-case", s);
  }

  public static string Canonize(string key, string s) {
    return ChangeCase
      .Convert(s,
               EditorPrefs.HasKey(key)
               ? (ChangeCase.Case)
               System.Enum.Parse(typeof(ChangeCase.Case),
                                 EditorPrefs.GetString(key))
               : ChangeCase.Case.KebabCase);
  }

  // Draw the property inside the given rect
  public override void OnGUI (Rect position,
                              SerializedProperty property,
                              GUIContent label) {
    // http://answers.unity3d.com/questions/542901/property-drawer-and-children.html
    position.height = 16f;
    // Caveat: all those known at compile-time that is.
    label = new GUIContent(label);
    label.tooltip = "Listing of Minibuffer commands, variables, and key bindings for this script";
    EditorGUI.BeginProperty(position, label, property);
    property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
    if (property.isExpanded) {
      //position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);
      var type = property.serializedObject.targetObject.GetType();
      //Debug.Log("type " + type);
      var p = position;
      var commands = Minibuffer
        .FindCommands(new [] { type }, CanonizeCommand, null, null)
        .OrderBy(kv => kv.Key);
      var indent = EditorGUI.indentLevel;

      if (commands.Any()) {
        p.y += 16f;
        EditorGUI.LabelField(p,
                             "Commands",
                             //"Commands ({0})".Formatted(commands.Count),
                             EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        foreach (var kv in commands) {
          p.y += 16f;
          EditorGUI.LabelField(p, kv.Key, kv.Value.briefDescription);
        }
      }
      var variables = Minibuffer
        .FindVariables(new [] { type }, CanonizeVariable)
        .OrderBy(kv => kv.Key);
      if (variables.Any()) {
        p.y += 16f;
        EditorGUI.indentLevel = 0;
        EditorGUI.LabelField(p,
                             "Variables",
                             //"Variables ({0})".Formatted(variables.Count),
                             EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        foreach (var kv in variables) {
          p.y += 16f;
          EditorGUI.LabelField(p, kv.Key, kv.Value.briefDescription);
        }
        EditorGUI.indentLevel = indent;
      }

      var keyBindings = commands
        .Where(kv => kv.Value.keyBindings.Any())
        .OrderBy(kv => kv.Key);
      if (keyBindings.Any()) {
        p.y += 16f;
        EditorGUI.indentLevel = 0;
        EditorGUI.LabelField(p,
                             "Key Bindings",
                             //"Key Bindings ({0})".Formatted(keyBindings.Count),
                             EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        Minibuffer.Notation keyNotation = (Minibuffer.Notation)
          System.Enum.Parse(typeof(Minibuffer.Notation),
                            EditorPrefs.GetString("minibuffer.key-notation",
                                                  Minibuffer.Notation.Standard.ToString()));

        KeyChord.standardNotation = keyNotation == Minibuffer.Notation.Standard;
        foreach (var kv in keyBindings) {
          p.y += 16f;
          EditorGUI.LabelField(p,
                               kv.Value.keyBindings
                                 .Select(kb => KeyChord.Canonize(kb))
                                 .OxfordOr(),
                               kv.Key);
        }
      }
      if (! commands.Any() && ! variables.Any())
        EditorGUI.LabelField(p,
                             "No commands or variables found",
                             EditorStyles.boldLabel);
    }
    EditorGUI.EndProperty ();
  }

  private static int PlusHeader(int count) {
    return count == 0 ? 0 : count + 1;
  }

  public override float GetPropertyHeight (SerializedProperty property,
                                           GUIContent label) {
    if (! property.isExpanded)
      return 16f;
    else {
      var type = property.serializedObject.targetObject.GetType();
      var commands = Minibuffer.FindCommands(new [] { type }, x => x, null, null);
      var ccount = commands.Count;
      var vcount = Minibuffer.FindVariables(new [] { type }, x => x).Count;
      var kcount = commands
        .Where(kv => kv.Value.keyBindings.Any()).Count();
      return
        (PlusHeader(ccount)
         + PlusHeader(vcount)
         + PlusHeader(kcount)
         + (ccount == 0 && vcount == 0 ? 1 : 0)
         + 1) * 16f;
    }
  }
}
}
