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
   ![MiniInfo in the inspector](inspector/mini-info-smaller.png)
   Add a command that shows a buffer of text.

   For static text that one wants accessible in their game, add %MiniInfo and
   add the text.

   ### Potential Uses
   - Game description
   - Game instructions
   - Release notes
   - High-minded philosophical gamedev treatise
   - Test instructions
   - License text

   If the text is not static, consider using the Minibuffer.ToBuffer() method in
   a custom command like so:

   ```
   [Command]
   public void DescribeGameState() {
     Minibuffer.instance.ToBuffer("game-state",
       string.Format("Score: {0}\n"
                   + "Round: {1}",
                   score,
                   round));
   }
   ```


   By default the command is "describe-<game-object-name>". One can also set a
   key binding.
*/
public class MiniInfo : MonoBehaviour {

  public bool customName;
  public string commandName;

  public bool bindKey;
  // public enum InfoType {
  //   String,
  //   TextAsset
  // }
  // public InfoType textType = InfoType.String;
  public string keyBinding;

  [System.Serializable]
  public struct AdvancedOptions {
    public string[] commandTags;
    public string prefix;
    public string group;
    public string keymap;
    public string description;
    public string bufferName;
  }
  public AdvancedOptions advancedOptions
    = new AdvancedOptions { commandTags = null, prefix = "Describe",
    group = "mini-info", keymap = "mini-info", bufferName = "*info*" };
  public bool useTextAsset = false;
  [TextArea(10,20)]
  //[TextArea]
  public string info;
  public TextAsset textAsset;
  // public TextAsset text;
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
            description = "Describe " + gameObject.name,
            keymap = advancedOptions.keymap,
            keyBinding = (bindKey && ! keyBinding.IsZull()) ? keyBinding : null,
            group = advancedOptions.group,
            tags = advancedOptions.commandTags
          };
          minibuffer.RegisterCommand(c, () => { minibuffer.ToBuffer(advancedOptions.bufferName ?? "*info*", useTextAsset ? textAsset.text : info); });
        });
  }

  void OnDestroy() {
    Minibuffer.With(minibuffer =>
        { if (cname != null)
            minibuffer.UnregisterCommand(cname);
        });
  }
}
}
