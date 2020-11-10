/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Collections;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/**
  ![KeymapModifier in the inspector](inspector/keymap-modifier.png)
  Add or remove a key binding.

  Make it easy for the user to modify or create a keymap in the inspector.
 */
public class KeymapModifier : MonoBehaviour {

  [Header("Modify or create the keymap named?")]
  public string keymap = "user";
  public bool createKeymapIfNecessary = true;

  [System.Serializable]
  public struct KeyCommandPair {
    public string keyBinding;
    public string command;
  }
  [Header("List of key bindings to add")]
  [Tooltip("Key binding first then command")]
  public KeyCommandPair[] addKeyBindings;

  [Header("List of key bindings to remove")]
  [Tooltip("Only the key binding is required")]
  public string[] removeKeyBindings;

  // Use this for initialization
  IEnumerator Start () {
    // Let Minibuffer setup its commands and keymaps so we don't get spurious
    // errors.
    yield return null;
    yield return null;
    Minibuffer.With(m => {
        var km = m.GetKeymap(keymap, createKeymapIfNecessary);
        if (km == null) {
          Debug.LogWarning("No such keymap '{0}'; disabling."
                           .Formatted(keymap));
          enabled = false;
          return;
        }

        foreach(var pair in addKeyBindings)
          km[pair.keyBinding] = pair.command;

        foreach(var keyBinding in removeKeyBindings) {
          if (! km.Remove(keyBinding))
            Debug.LogWarning("Unable to remove key binding {0} from keymap {1}"
                             .Formatted(keyBinding, keymap));
        }
      });
  }
}
}
