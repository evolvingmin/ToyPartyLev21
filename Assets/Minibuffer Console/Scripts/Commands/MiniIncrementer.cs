/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using RSG;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/**
  ![MiniIncrementer in the inspector](inspector/mini-incrementer.png)
  An incrementer for an integer variables.

  <kbd>C-=</kbd> to increment. If no variable is selected, it'll prompt you for one.
  <kbd>C--</kbd> to decrement. You may select another variable with <kbd>M-x select-variable</kbd>.

  The incrementer may be specialized on one variable, in which case the
  incrementVariable is fixed and cannot be changed. No select-variable will be
  exposed.

  <kbd>C-=</kbd> to increment by 1.

  <kbd>C-u 10 C-=</kbd> to increment by 10.

  <kbd>C-u C-=</kbd> to increment by 4.

  <kbd>C-u C-u C-=</kbd> to increment by 16.
 */
[Group(tag = "built-in")]
public class MiniIncrementer : MonoBehaviour {
  [Header("Use this for one variable only?")]
  public bool specialize = false;
  public bool stickyIncrements = false;
  public string incrementVariable;
  // XXX Is there a good way to make these key bindings into the appropriate format?
  // I can standardize on "standard" but then it's weird for everything else.
  public string incrementKeyBinding = "ctrl-=";
  public string decrementKeyBinding = "ctrl--";
  public int delta = 1;
  [System.Serializable]
  public struct AdvancedOptions {
    public string[] commandTags;
    public string incrementPrefix;
    public string decrementPrefix;
    public string group;
    public string keymap;
  }
  public AdvancedOptions advancedOptions
    = new AdvancedOptions { commandTags = null,
                            incrementPrefix = "Increment",
                            decrementPrefix = "Decrement",
                            group = "mini-incrementer",
                            keymap = "mini-incrementer" };

  private Minibuffer m;
  void Start() {
    if (delta == 0)
      Debug.LogWarning("Delta is set to 0 meaning increment will not change its value.");
    if (! specialize) {
      Minibuffer.Register(this);
      Minibuffer.With(m => this.m = m);
    } else {
      if (incrementVariable.IsZull()) {
        Debug.LogWarning("For a specialized incrementer the increment variable "
                         + "must not be null; disabling.");
        enabled = false;
        return;
      }
      // This is a special purpose incrementer.
      Minibuffer.With(m => {
          this.m = m;
          var desc = "This command made available by MiniIncrementer class.";
          m.RegisterCommand(new Command("{0}-{1}" .Formatted(advancedOptions.incrementPrefix,
                                                             incrementVariable))
                            { keyBinding = incrementKeyBinding,
                              keymap = advancedOptions.keymap,
                              group = advancedOptions.group,
                              description = "Increment variable '{0}'. {1}"
                              .Formatted(incrementVariable, desc),
                              definedIn = "class MiniIncrementer",
                              signature = "void Increment([UniversalArgument] int? amount)",
                              tags = advancedOptions.commandTags
                            },
                            () => Increment(m.currentPrefixArg.HasValue
                                            ? m.currentPrefixArg.Value : delta));
          m.RegisterCommand(new Command("{0}-{1}".Formatted(advancedOptions.decrementPrefix,
                                                            incrementVariable))
                            { keyBinding = decrementKeyBinding,
                              keymap = advancedOptions.keymap,
                              group = advancedOptions.group,
                              description = "Decrement variable '{0}'. {1}"
                              .Formatted(incrementVariable, desc),
                              definedIn = "class MiniIncrementer",
                              signature = "void Decrement([UniversalArgument] int? amount)",
                              tags = advancedOptions.commandTags
                            },
                            () => Decrement(m.currentPrefixArg.HasValue
                                            ? m.currentPrefixArg.Value : delta));
        });
    }
  }

  void OnDestroy() {
    if (! specialize) {
      Minibuffer.Unregister(this);
    } else {
      if (incrementVariable.IsZull()) {
        return;
      }
      // This is a special purpose incrementer.
      Minibuffer.With(m => {
          m.UnregisterCommand("{0}-{1}".Formatted(advancedOptions.incrementPrefix,
                                                  incrementVariable));
          m.UnregisterCommand("{0}-{1}".Formatted(advancedOptions.decrementPrefix,
                                                  incrementVariable));
        });
    }
  }

  [Command("select-variable",
           description = "Select an integer variable for M-x increment and M-x decrement to use.")]
  public string SelectVariable([Prompt("Select integer variable: ",
                                     completer = "variable-int")]
                             string variableName) {
    incrementVariable = variableName;
    return string.Format("Variable '{0}' set as increment variable. C-= to increment. C-- to decrement.",
                         incrementVariable);
  }

  [Command("increment",
           description = "Increment the current integer variable by 1 (C-u to change). "
           + "Will ask for a variable if one is not already selected.  See also SelectVariable().",
           keyBinding = "C-=")]
  public void Increment([UniversalArgument] int? amount, string variableName = null) {
    if (variableName == null)
      variableName = incrementVariable;
    if (variableName == null
        || ! m.variables.ContainsKey(variableName)) {
      m.Read(new Prompt("Select integer variable: ") {
                      completer = "variable-int"})
        .Then(name => {
            incrementVariable = name;
            Increment(amount);
          });
    } else {
      if (! amount.HasValue) {
        amount = delta;
      } else {
        if (stickyIncrements)
          delta = amount.Value > 0 ? amount.Value : -amount.Value;
      }
      var vi = m.variables[variableName];
      IPromise<object> instanceP;
      if (vi.IsStatic) {
        instanceP = Promise<object>.Resolved(null);
      } else {
        instanceP = m.interpreter.FillInstance(vi.DeclaringType);
      }
      instanceP.Done(instance => {
          vi.SetValue(instance, (int) vi.GetValue(instance) + amount.Value);
          m.Echo("{0} {1} = {2}",
               vi.VariableType.PrettyName(),
               vi.Name,
               TextUtils.QuoteIfString(vi.GetValue(instance)));
        });
    }
  }
  [Command("decrement",
           description = "Decrement the current integer variable by 1 (C-u to change). "
           + "Will ask for a variable if one is not already selected.  See also SelectVariable().",
           keyBinding = "C--")]
  public void Decrement([UniversalArgument] int? amount, string variableName = null) {
    Increment(amount.HasValue ? -amount.Value : -delta, variableName);
  }
}
}
