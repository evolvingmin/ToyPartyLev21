/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SeawispHunter.MinibufferConsole.Extensions;
using RSG;

namespace SeawispHunter.MinibufferConsole {

/**
   ![Variable commands in the inspector](inspector/variable-commands.png)
   Edit variables and toggle booleans.

 */
[Group("core", tag = "built-in")]
public class VariableCommands : MonoBehaviour {
  private Minibuffer m;

  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
    Minibuffer.With(minibuffer => this.m = minibuffer);
  }

  // [Command("toggle-booleans",
  //          description = "Toggle boolean variables. "
  //          + "Hit 'q' to quit, '1' to turn all on, '0' to turn all off, and '*' to invert all booleans."
  //          // keyBinding = "C-t"
  //          )]
  /**
     Toggle the boolean Variables of a given object.
   */
  public void ToggleBooleans(object ofObject = null, Action onQuit = null) {
    // var m = Minibuffer.instance;
    var charToVar = new Dictionary<char, VariableInfo>();
    Action myOnQuit = () => {
      m.Message("Quit");
      if (onQuit != null)
        onQuit();
    };
    var bools = m.variables
      .Where(kv => ofObject == null
             ? true
             : kv.Value.DeclaringType == ofObject.GetType())
      .Where(kv => kv.Value.VariableType == typeof(bool))
      .SelectMany(kv => ofObject == null ?
                  BoundVariableInfo.GetBoundVariables(kv.Value)
                  : new[] { kv.Value.ToBoundVariable(ofObject) });
    var dupes = bools
      .GroupBy(vi => m.CanonizeVariable(vi.Name))
      .Where(g => g.Count() > 1);
    if (dupes.Any()) {
      foreach(var group in dupes)
        Debug.LogError("Variable '{0}' defined in multiple fields: "
                       .Formatted(group.Key)
                       + group.Select(vi =>
                                      vi.ToString())
                       .OxfordAnd());
      Debug.LogWarning("Will only use one of the available variable "
                       + "definitions, undefined which one.");
      bools = bools.DistinctBy(vi => m.CanonizeVariable(vi.Name));
    }
    var reservedLetters = new [] {'q', '*', '1', '0'};
    var letterBag = new LetterBag();
    foreach(var r in reservedLetters)
      letterBag.chars.Add(r);
    var nameAndVars = new Dictionary<string, VariableInfo>();
    foreach (var kv in bools
             .ToDictionary(bvi => m.CanonizeVariable(bvi.Name))
             .OrderBy(kv => kv.Key)) {
      var name = kv.Key;
      var vi = kv.Value;
      char c;
      var nameWithShortcut = letterBag.NextLetter(name, out c);
      charToVar[c] = vi;
      nameAndVars[nameWithShortcut] = vi;
    }
    System.Func<string> stateOfBools = () =>
      string.Join("\n", nameAndVars
                  .Select(kv => kv.Key
                          + ((bool) kv.Value.GetValue(null) ? " on" : " off"))
                  .ToArray())
      ;
    if (letterBag.chars.Count == 1) {
      m.MessageInline(" [No boolean variables found]");
      myOnQuit();
      return;
    }
    // Put 'q' at the back.
    ToggleBooleansHelper(letterBag.chars
                         .Skip(reservedLetters.Count())
                         .Concat(reservedLetters)
                         .ToList(),
                         stateOfBools, charToVar, myOnQuit);
  }

  [Command("toggle-booleans",
           description = "Toggle boolean variables. "
           + "Hit 'ctrl-2 to toggle, 'ctrl-shift-2' to toggle all, 'ctrl-1' to turn on, 'ctrl-shift-1' to turn all on, 'ctrl-0' to turn off, 'ctrl-shift-0' to turn all off.",
           keyBinding = "C-t"
           )]
  public void ToggleBooleans2(object ofObject = null, Action onQuit = null) {
    // var dict = new Dictionary<string, bool>();
    // var pi = new PromptInfo("Set toggles: ");
    // pi.completerEntity = new ListCompleter<string>(toggles).ToEntity();
    // IPromise<object> p = FillType(prompt, t);
    var bools = m.variables
      .Where(kv => ofObject == null
             ? true
             : kv.Value.DeclaringType == ofObject.GetType())
      .Where(kv => kv.Value.VariableType == typeof(bool))
      .SelectMany(kv => ofObject == null ?
                  BoundVariableInfo.GetBoundVariables(kv.Value)
                  : new[] { kv.Value.ToBoundVariable(ofObject) });
    var dupes = bools
      .GroupBy(vi => m.CanonizeVariable(vi.Name))
      .Where(g => g.Count() > 1);
    if (dupes.Any()) {
      foreach(var group in dupes)
        Debug.LogError("Variable '{0}' defined in multiple fields: "
                       .Formatted(group.Key)
                       + group.Select(vi =>
                                      vi.ToString())
                       .OxfordAnd());
      Debug.LogWarning("Will only use one of the available variable "
                       + "definitions, undefined which one.");
      bools = bools.DistinctBy(vi => m.CanonizeVariable(vi.Name));
    }
    var prompt = new PromptInfo("Toggle boolean variables: ");
    var boolDict = bools.ToDictionary(v => v.Name);
    prompt.completerEntity = new DictCompleter<VariableInfo>(boolDict).ToEntity();
    m.Read<VariableInfo>(prompt)
      .Done(input => {
          m.Message("Done");
          Debug.Log("Got here");
          if (onQuit != null)
            onQuit();
        });
    m.editState.isValidInput = _ => true; // All input is valid.
    m.editState.toggleGetter = name => (bool) boolDict[name].GetValue(null);
    m.editState.toggleSetter = (name, value) => boolDict[name].SetValue(null, value);
    m.TabComplete();
    //     = new ListCoercer(ce.coercer, dict.Where(kv => kv.Value).Select(kv => kv.Key));
    // m.editState.prompt.desiredType = ce.coercer.defaultType;
    // m.editState.prompt.completerEntity = ce;
    // return "Done";
  }

  private void ToggleBooleansHelper(List<char> chars,
                                    System.Func<string> stateOfBools,
                                    Dictionary<char, VariableInfo> charToVar,
                                    Action onQuit) {
    // var m = Minibuffer.instance;
    m.ReadValidChar(chars, "Toggle booleans \n"
                    + stateOfBools()
                    + "\n")
      .Then(c => {
          switch(c) {
          case 'q':
            onQuit();
            return;
          case '1':
            foreach(var bvi in charToVar.Values) {
              bvi.SetValue(null, true);
            }
            break;
          case '0':
            foreach(var bvi in charToVar.Values) {
              bvi.SetValue(null, false);
            }
            break;
          case '*':
            foreach(var bvi in charToVar.Values) {
              bvi.SetValue(null, ! (bool)bvi.GetValue(null));
            }
            break;
          default:
          {
            var bvi = charToVar[c];
            bvi.SetValue(null, ! (bool)bvi.GetValue(null));
            break;
          }
          }
          ToggleBooleansHelper(chars, stateOfBools, charToVar, onQuit);
        })
      .Catch(ex => onQuit());
  }

  [Command("edit-variable",
           description = "Edit a variable.",
           keyBinding = "C-x v",
           group = "core")]
  public void EditVariable([Prompt("Edit variable: ",
                                   completer    = "variable")]
                           string name) {
    if (name != null && m.variables.ContainsKey(name)) {
      var vi = m.variables[name];
      IPromise<object> instanceP;
      if (vi.IsStatic) {
        instanceP = Promise<object>.Resolved(null);
      } else {
        instanceP = m.interpreter.FillInstance(vi.DeclaringType);
      }
      instanceP.Done(instance => {
          var prompt
          = string.Format("{0} {1} = {2}; Set new value: ",
                          vi.VariableType.PrettyName(),
                          vi.Name,
                          TextUtils.QuoteIfString(vi.GetValue(instance)));
          m.interpreter.FillType(new PromptInfo(new Prompt(prompt)), vi.VariableType)
          .Then((obj) => {
              vi.SetValue(instance, obj);
              m.Message("{0} {1} = {2}",
                        vi.VariableType.PrettyName(),
                        vi.Name,
                        TextUtils.QuoteIfString(vi.GetValue(instance)));
            })
          .Catch(ex => {
              m.Message("Variable not changed. {0} {1} = {2}",
                        vi.VariableType.PrettyName(),
                        vi.Name,
                        TextUtils.QuoteIfString(vi.GetValue(instance)));
            });
        });
    } else {
      m.Message("No such variable '{0}'.", name);
    }
  }

  [Command("toggle-completer-booleans",
           keyBinding = "C-t",
           group = "minibuffer",
           keymap = "minibuffer",
           description = "Toggle the booleans of the current completer")]
  public void ToggleFilters7(Action onQuit = null) {
    if (m == null)
      m = Minibuffer.instance;
    var c = m.editState.prompt.completerEntity.completer;
    if (c == null) {
      m.MessageAlert("No completer to toggle booleans");
    } else {
      var wereEditing = m.editing;
      Minibuffer.EditState saved = m.SaveEditState();
      m.ClearEditState();
      if (onQuit == null && wereEditing) {
        onQuit = () => {
          if (wereEditing) {
            m.editing = true;
            m.RestoreEditState(saved);
            if (m.showAutocomplete)
              m.TabComplete();
          }
        };
      }
      m.editing = false;
      ToggleBooleans2(c, onQuit);
    }
  }

}

} // namespace
