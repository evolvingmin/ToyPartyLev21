/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using SeawispHunter.MinibufferConsole;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/**
   ![MiniToggler options in the inspector](inspector/mini-toggler.png)
   Add command to toggle a state or game object on or off.

   %MiniToggler has three target modes:

   - __Self__: Toggle the game object this component is attached to.

   - __Game Object__: Toggle a different game object.

   - __ActionPair__: Specify the "on" and "off" states via two actions.

   By default the command is "toggle-<game-object-name>". One can also set a key
   binding.

   By default the variable is "<game-object-name>". Since the variable is a
   boolean, use <kbd>C-t</kbd> or <kbd>M-x toggle-booleans</kbd> to toggle it
   and others. Otherwise use <kbd>M-x edit-variable</kbd> or <kbd>M-x
   describe-variable</kbd>.

   \see SeawispHunter.MinibufferConsole.MiniAction
*/
public class MiniToggler : MonoBehaviour {

  public enum CommandOrVariable {
    Command,
    Variable,
    Both
  };

  public CommandOrVariable commandOrVariable = CommandOrVariable.Command;

  public bool customName;
  public string commandName;
  public string variableName;

  public bool bindKey;
  public string keyBinding;

  public enum Target {
    Self,
    GameObject,
    ActionPair
  };
  public enum TargetState {
    On,
    Off,
    LeaveAsIs
  };
  public Target targetChoice = Target.Self;
  public TargetState targetState = TargetState.LeaveAsIs;

  [System.Serializable]
  public struct AdvancedOptions {
    public string[] commandTags;
    public string prefix;
    public string group;
    public string keymap;
    public string description;
  }
  public AdvancedOptions advancedOptions
    = new AdvancedOptions { commandTags = null, prefix = "Toggle",
    group = "mini-toggler", keymap = "mini-toggler", description = "" };

  public bool customTarget;
  public GameObject target;

  public bool _state;
  public UnityEvent onAction;
  public UnityEvent offAction;

  private string cname;
  private string vname;

  public bool state {
    get {
      switch (targetChoice) {
        case Target.Self:
          return gameObject.activeSelf;
        case Target.GameObject:
          return target.activeSelf;
        case Target.ActionPair:
          return _state;
        default:
          throw new MinibufferException("Unexpected targetChoice " + targetChoice);
      }
    }
    set {
      switch (targetChoice) {
        case Target.Self:
          gameObject.SetActive(value);
          break;
        case Target.GameObject:
          target.SetActive(value);
          break;
        case Target.ActionPair:
          if (value)
            onAction.Invoke();
          else
            offAction.Invoke();
          _state = value;
          break;
      }
    }
  }
  /*
    \todo Problem: Don't have access to this.name if the gameObject is
    initially inactive.  Workaround: Provide a custom command name.
  */
  void Start() {
    var _target = customTarget ? target : gameObject;
    var desc = advancedOptions.description.IsZull()
      ? (targetChoice != Target.ActionPair
         ? "Turn " + _target.name + " on or off"
         : "Turn on or off")
      : advancedOptions.description;

    Minibuffer.With(minibuffer => {
        if (commandOrVariable == CommandOrVariable.Command
            || commandOrVariable == CommandOrVariable.Both) {

          cname = (customName && ! commandName.IsZull())
          ? commandName
          : advancedOptions.prefix + " " + _target.name;
          var c = new Command(cname) {
            description = desc,
            keymap = advancedOptions.keymap,
            keyBinding = (bindKey && ! keyBinding.IsZull()) ? keyBinding : null,
            group = advancedOptions.group,
            tags = advancedOptions.commandTags
          };
          minibuffer.RegisterCommand(c, () => Toggle());
        }
        if (commandOrVariable == CommandOrVariable.Variable
            || commandOrVariable == CommandOrVariable.Both) {

          vname = (customName && ! variableName.IsZull())
          ? variableName
          : _target.name;
          var v = new Variable(vname) {
            description = desc,
          };
          minibuffer.RegisterVariable(v,
                                      () => state,
                                      (value) => { state = value; },
                                      typeof(MiniToggler));
        }
      });
    switch (targetState) {
      case TargetState.On:
        state = true;
        break;
      case TargetState.Off:
        state = false;
        break;
    }
  }

  public void Toggle() {
    state = !state;
  }

  void OnDestroy() {
    Minibuffer.With(minibuffer =>
        { if (cname != null)
            minibuffer.UnregisterCommand(cname);
          if (vname != null)
            minibuffer.UnregisterVariable(vname);
        });
  }
}
}
