/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SeawispHunter.MinibufferConsole;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/**
   ![MiniAction in the inspector](inspector/mini-action.png)
   Add a command that runs an action.

   Here are some potential general uses:

   - Trigger an animation.
   - Enable or disable a game object.
   - Enable or disable a component.
   - Run a method on a component.

   Here are some fun specific uses:

   - Play [*badum tish*](http://www.badum-tish.com) when your joke falls flat at the game expo.
   - Activate [your disco ball](https://twitter.com/punchesbears/status/747997312396435456). "What? Your game doesn't have one? You need one."

   Actions are fully specified in the inspector. By default the command is
   "do-<game-object-name>". One can also set a key binding.

   \see MiniToggler
*/
public class MiniAction : MonoBehaviour {

  public UnityEvent action;
  public bool customName;
  public string commandName;

  public bool bindKey;
  public string keyBinding;

  [System.Serializable]
  public struct AdvancedOptions {
    public string[] commandTags;
    public string prefix;
    public string group;
    public string keymap;
    public string description;
    public bool repeatOnUniversalArgument;
  }
  public AdvancedOptions advancedOptions
    = new AdvancedOptions { commandTags = null,
                            prefix = "Do",
                            group = "mini-action",
                            keymap = "mini-action",
    description = "",
    repeatOnUniversalArgument = false };

  private string cname;
  void Start() {
    Minibuffer.With(minibuffer => {
          cname = (customName && ! commandName.IsZull())
          ? commandName
          : advancedOptions.prefix + " " + gameObject.name;
          cname = minibuffer.CanonizeCommand(cname);
          // var actions = GetComponents<MiniAction>().ToList();
          // if (actions.Count() > 1) {
          //   int index = actions.FindIndex(x => x == this);
          //   cname += "-" + (index + 1);
          // }
          var n = cname;
          for (int i = 0; i < 10 && minibuffer.commands.ContainsKey(n); i++)
            n = string.Format("{0}-{1}", cname, i + 1);
          if (n != cname) {
            customName = true;
            commandName = n;
            cname = n;
          }
          var c = new Command(cname) {
            description = advancedOptions.description.IsZull()
              ? "Do " + gameObject.name + " action"
              : advancedOptions.description,
            keymap = advancedOptions.keymap,
            keyBinding = (bindKey && ! keyBinding.IsZull()) ? keyBinding : null,
            group = advancedOptions.group,
            tags = advancedOptions.commandTags
          };
          // Awesome. I wasn't sure this was possible before.
          if (! advancedOptions.repeatOnUniversalArgument)
            minibuffer.RegisterCommand(c, () => { action.Invoke(); });
          else
            minibuffer.RegisterCommand(c, (System.Action<int>) DoAction);
        });
  }

  public void DoAction([UniversalArgument] int count) {
    for (int i = 0; i < count; i++)
      action.Invoke();
  }

  void OnDestroy() {
    Minibuffer.With(minibuffer =>
        { if (cname != null)
            minibuffer.UnregisterCommand(cname);
        });
  }
}
}
