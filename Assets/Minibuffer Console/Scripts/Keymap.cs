/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
#if ! SH_MINIBUFFER_NOLIBS
#define FULL_SERIALIZER
#endif
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
#if FULL_SERIALIZER
using SeawispHunter.FullSerializer;
#endif

using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/**
   Map of key sequences to command names.

   To bind a key to a command, use the Command field \link
   SeawispHunter.MinibufferConsole.Command.keyBinding keyBinding\endlink.


   ```cs
   [Command("cube-drop",
            description = "Drop a cube into the scene",
            keyBinding = "C-d")]
   public void CubeDrop() {...}
   ```

   Or you can use %Minibuffer's \link
   SeawispHunter.MinibufferConsole.Minibuffer.GetKeymap GetKeymap\endlink to
   reference or create a new keymap.  Adding new bindings can be done
   as follows:

   ```cs
   Keymap keymap = minibuffer.GetKeymap("user", true);
   keymap["C-d"]         = "cube-drop";
   keymap["C-c b"]       = "randomize-background-color";
   ```

   Keymaps can be enabled or disabled and they have a priority
   number. If two enabled keymaps have the same key binding, the one
   with the highest priority number resolves to its command.  A
   disabled keymap is not considered when resolving key bindings.

*/
/*
# Default Key Bindings

## "unity" keymap  priority 50

key sequence   | command
---            | -------
<kbd>C-c c</kbd>          | capture-screenshot
<kbd>s-p</kbd>            | noop
<kbd>C-c d</kbd>          | show-console
<kbd>C-x l</kbd>          | switch-to-scene

## "user" keymap  priority 40
key sequence            | command
---            | -------
<kbd>C-d</kbd>            | cube-drop
<kbd>C-c b</kbd>          | randomize-background-color

## "minibuffer" keymap priority 30
key sequence            | command
---            | -------
<kbd>tab</kbd>            | minibuffer-complete
<kbd>S-tab</kbd>          | minibuffer-complete
<kbd>return</kbd>         | minibuffer-exit
<kbd>C-return</kbd>       | minibuffer-exit
<kbd>C-m</kbd>            | minibuffer-exit
<kbd>C-n</kbd>            | next-element
<kbd>C-p</kbd>            | previous-element

## "compat" keymap  priority 20
key sequence            | command
---            | -------
<kbd>:</kbd>              | execute-extended-command
<kbd>escape</kbd>         | keyboard-quit
<kbd>s-c</kbd>            | kill-ring-save
<kbd>s-x</kbd>            | kill-selection
<kbd>s-a</kbd>            | mark-whole-buffer
<kbd>home</kbd>           | move-beginning-of-line
<kbd>C-leftarrow</kbd>    | move-beginning-of-line
<kbd>end</kbd>            | move-end-of-line
<kbd>C-rightarrow</kbd>   | move-end-of-line
<kbd>s-v</kbd>            | yank

## "editing" keymap priority 10
key sequence   | command
---            | -------
<kbd>backspace</kbd>      | backspace
<kbd>C-b</kbd>            | backward-char
<kbd>M-backspace</kbd>    | backward-kill-word
<kbd>M-b</kbd>            | backward-word
<kbd>C-f</kbd>            | forward-char
<kbd>M-f</kbd>            | forward-word
<kbd>C-k</kbd>            | kill-line
<kbd>M-w</kbd>            | kill-ring-save
<kbd>C-x h</kbd>          | mark-whole-buffer
<kbd>C-a</kbd>            | move-beginning-of-line
<kbd>C-e</kbd>            | move-end-of-line
<kbd>M-n</kbd>            | next-history-element
<kbd>C-n</kbd>            | next-line
<kbd>M-p</kbd>            | previous-history-element
<kbd>C-p</kbd>            | previous-line
<kbd>?</kbd>              | self-insert-command
<kbd>:</kbd>              | self-insert-command
<kbd>.</kbd>              | self-insert-command
<kbd>C-space</kbd>        | set-mark-command
<kbd>C-y</kbd>            | yank

## "help" keymap  priority 5
key sequence            | command
---            | -------
<kbd>C-h b</kbd>          | describe-bindings
<kbd>C-h c</kbd>          | describe-command
<kbd>C-h f</kbd>          | describe-command
<kbd>C-h k</kbd>          | describe-key
<kbd>C-h l</kbd>          | describe-license
<kbd>C-h v</kbd>          | describe-variable
<kbd>C-h C-h</kbd>        | help-for-help
<kbd>f1</kbd>             | help-for-help
<kbd>?</kbd>              | help-for-help
<kbd>C-h t</kbd>          | help-with-tutorial

## "core" keymap  priority 0
key sequence            | command
---            | -------
<kbd>M-<</kbd>            | beginning-of-buffer
<kbd>M-></kbd>            | end-of-buffer
<kbd>M-x</kbd>            | execute-extended-command
<kbd>C--</kbd>            | font-size-decrease
<kbd>C-=</kbd>            | font-size-increase
<kbd>C-g</kbd>            | keyboard-quit
<kbd>C-x C-b</kbd>        | list-buffers
<kbd>.</kbd>              | repeat-last-command
<kbd>C-x C-s</kbd>        | save-buffer
<kbd>C-x b</kbd>          | switch-to-buffer
<kbd>C-t</kbd>            | toggle-completer-booleans
<kbd>C-u</kbd>            | universal-argument
<kbd>C-x 1</kbd>          | window-fullsize
<kbd>C-x 2</kbd>          | window-halfsize
<kbd>C-x 0</kbd>          | window-hide
<kbd>C-v</kbd>            | window-scroll-down
<kbd>M-v</kbd>            | window-scroll-up
<kbd>C-x 3</kbd>          | window-split-right
 */
[System.Serializable]
[Group(tag = "built-in")]
#if FULL_SERIALIZER
[fsObject("v1")]
#endif
public class Keymap {
  public string name;
  [SerializeField]
  #if FULL_SERIALIZER
  [fsIgnore]
  #endif
  private bool _enabled = true;
  private int _priority = 0;
  public delegate void KeymapChangeHandler();
  public /*event*/ KeymapChangeHandler keymapChange;
  internal Dictionary<string,string> map = new Dictionary<string,string>();
  protected Dictionary<Regex,string> regexes = new Dictionary<Regex,string>();
  // We could but do we really want to?
  // public Dictionary<string,System.Delegate> delegateMap
  //   = new Dictionary<string,System.Delegate>();

  static Keymap() {
    Minibuffer.Register(typeof(Keymap));
  }

  /**
     Keymaps may be disabled.  This is the principle means of
     activating different "modes" in minibuffer.
   */
  public bool enabled {
    get { return _enabled; }
    set {
      if (value != _enabled) {
        // State change.
        if (keymapChange != null)
          keymapChange();
      }
      _enabled = value;
    }
  }

  /**
     Keymaps are sorted by priority.  The higher the priority the
     greater its precedence.
   */
  public int priority {
    get { return _priority; }
    set {
      if (value != _priority) {
        // State change.
        if (keymapChange != null)
          keymapChange();
      }
      _priority = value;
    }
  }

  /**
     Get or set the command for the given key sequence.

     E.g.
     ```
     userKeymap["C-c g"] = "god-mode";
     ```
   */
  public string this[string key] {
    get {
      string result;
      key = KeyChord.Canonize(key);
      if (! map.TryGetValue(key, out result)) {
        result = regexes
          .Where(kv => kv.Key.IsMatch(key))
          .Select(kv => kv.Value)
          .FirstOrDefault();
      }
      return result;
    }
    set {
      bool isRegex = Regex.IsMatch(key, "^/.*/$");
      bool deleting = value == null;
      var command = deleting ? null : Minibuffer.instance.CanonizeCommand(value);
      if (command != null) {
        Assert.IsNotNull(Minibuffer.instance);
        Assert.IsNotNull(Minibuffer.instance.commands);
        if (! Minibuffer.instance.commands.ContainsKey(command)) {
          Debug.LogError("{0} is bound to non-existent command {1}."
                         .Formatted(key,
                                    command));
        }
      }
      if (deleting) {
        if (! isRegex) {
          string k = key;
          try {
            k = KeyChord.Canonize(key);
          } catch (MinibufferException) {
            Debug.LogWarning("Key binding '{0}' not recognized.".Formatted(key));
          }
          if (map.ContainsKey(k))
            Remove(k);
          else
            Debug.LogWarning("Trying to remove key binding '{0}' that is not there."
                             .Formatted(k));
        } else {
          var r = new Regex(Regex.Replace(key, "^/(.*)/$", "$1"));
          if (regexes.ContainsKey(r))
            regexes.Remove(r);
          else
            Debug.LogWarning("Trying to remove key binding '{0}' that is not there."
                             .Formatted(key));
        }
        return;
      }
      // Add the key binding.
      if (! isRegex) {
        var k = KeyChord.Canonize(key);
        if (map.ContainsKey(k)) {
          Debug.Log("Warning: overwriting key binding '{0}' for '{1}' with '{2}'."
                    .Formatted(k, map[k], command));
        }
        map[k] = command;
      } else {
        // This is a regex.
        regexes[new Regex(Regex.Replace(key, "^/(.*)/$", "$1"))] = command;
      }

      // if (enabled && keymapChange != null)
      //   keymapChange();
    }
  }

  public void CanonizeCommands() {
    foreach(var key in map.Keys.ToList()) {
      //this[key] = map[key];
      map[key] = Minibuffer.instance.CanonizeCommand(map[key]);
    }
  }

  public bool ContainsKey(string key) {
    var ckey = KeyChord.Canonize(key);
    return map.ContainsKey(ckey)
      || regexes.Keys.Any(r => r.IsMatch(ckey));
  }

  public bool Remove(string key) {
    return map.Remove(KeyChord.Canonize(key));
  }

  public override string ToString() {
    return name + " priority " + priority + " with " + map.Count + " key bindings";
  }

  public static Keymap Load(string name) {
    Debug.Log("LoadKeymap " + name);
    #if !UNITY_WEBPLAYER
    var filename =
      PathName.instance.Expand(string.Format("$data/minibuffer/{0}", name));
    if (System.IO.File.Exists(filename)) {
      // There's a keymap.
      Debug.Log("Loading file " + filename);
      var k = (Keymap)
        StringSerializationAPI.Deserialize(typeof(Keymap),
                                           System.IO.File.ReadAllText(filename));
      k.CanonizeCommands();
      return k;
    } else {
      // Check for a resource.
      Debug.Log("Loading text asset " + name);

      var ta = Resources.Load<TextAsset>(name.Replace(".json", ""));
      if (ta != null) {
        var k = (Keymap) StringSerializationAPI.Deserialize(typeof(Keymap),
                                                            ta.text);
        k.CanonizeCommands();
        return k;
      } else {
        Debug.Log("Error: couldn't deserialize " + name + "\n");
        return null;
      }
    }
    #else
    return null;
    #endif
  }

  public static void LoadDefaultKeymap(string name, Keymap keymap) {
    switch (name) {
      case "core":
        var coreKeymap = keymap;
        /*
          What is the minimal set of commands for an Emacs-like system
          that's emphatically not a text editor?
        */
        // help commands are very important, but they're in the
        // HelpCommands file.
        // Moved to the class that defines them.
        coreKeymap["/.*(C|ctrl)-g$/"]    = "keyboard-quit";
        break;
      case "window":
        var windowKeymap = keymap;
        windowKeymap["C-v"]         = "window-scroll-down";
        // windowKeymap["space"]       = "window-scroll-down";
        windowKeymap["M-v"]         = "window-scroll-up";
        windowKeymap["S-space"]     = "window-scroll-up";
        windowKeymap["M-<"]         = "window-scroll-to-top";
        windowKeymap["M->"]         = "window-scroll-to-bottom";
        windowKeymap["C-x <"]       = "window-scroll-left";
        windowKeymap["C-x >"]       = "window-scroll-right";
        windowKeymap["pagedown"]    = "window-scroll-down";
        windowKeymap["pageup"]      = "window-scroll-up";

        windowKeymap["C-x 0"]       = "window-hide";
        windowKeymap["C-x 1"]       = "window-fullsize";
        windowKeymap["C-x 2"]       = "window-halfsize";
        windowKeymap["C-x 3"]       = "window-split-right";
        windowKeymap["C-x w"]       = "toggle-text-wrap";
        break;
      case "buffer":
        // var bufferKeymap = keymap;
        // Moved to the class that defines them.
        break;
      case "editing":
        var editingKeymap = keymap;
        // [ -~] is all printable characters.
        // http://www.catonmat.net/blog/my-favorite-regex/
        // But it doesn't work for space ' ' which must instead be
        // 'space'.
        editingKeymap["/^[ -~]$/"] = "self-insert-command";
        break;
      case "compat":
        // var compatKeymap = keymap;
        /*
          For vim-ers, non-Emacs users, and all around normal people.
        */
        // compatKeymap["escape"]       = "keyboard-quit";
        // compatKeymap[":"]            = "execute-extended-command";

        // compatKeymap["M-uparrow"]    = "history-previous";
        // compatKeymap["M-downarrow"]  = "history-next";
        // compatKeymap["pagedown"]     = "window-scroll-down";
        // compatKeymap["pageup"]       = "window-scroll-up";
        break;
      case "winos":
        var winosKeymap = keymap;
        /*
          This is for Windows, but it breaks some very important
          Emacs/Minibuffer key bindings.

          XXX Emacs proper when using CUA-mode does these commands only if a
          region is selected. Can we do that easily? Maybe stick this into its
          own keymap that only activates if a region is selected. That seems
          reasonable.
        */
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // winosKeymap["C-c"]          = "kill-ring-save";
        // winosKeymap["C-x"]          = "kill-selection";
        // winosKeymap["C-v"]          = "yank";
        #endif
        winosKeymap["C-rightarrow"] = "forward-word";
        winosKeymap["C-leftarrow"]  = "backward-word";
        winosKeymap["C-backspace"]  = "backward-kill-word";
        winosKeymap["C-delete"]     = "kill-word";
        winosKeymap["home"]         = "window-scroll-to-top";
        winosKeymap["end"]          = "window-scroll-to-bottom";
        winosKeymap["C-a"]          = "mark-whole-buffer";
        winosKeymap["C-s"]          = "save-buffer";
        break;
      case "macos":
        // MacOS key bindings
        var macosKeymap = keymap;
        macosKeymap["M-rightarrow"] = "forward-word";
        macosKeymap["M-leftarrow"]  = "backward-word";
        // macosKeymap["M-backspace"]  = "backward-kill-word";
        macosKeymap["M-delete"]     = "kill-word";
        macosKeymap["home"]         = "window-scroll-to-top";
        macosKeymap["end"]          = "window-scroll-to-bottom";
        macosKeymap["s-a"]          = "mark-whole-buffer";
        macosKeymap["s-s"]          = "save-buffer";
        //macosKeymap["s-c"]          = "kill-ring-save";
        macosKeymap["s-c"]          = "copy-buffer-or-prompt";
        macosKeymap["s-x"]          = "kill-selection";
        macosKeymap["s-v"]          = "yank";
        break;
      case "minibuffer":
        var minibufferKeymap = keymap;
        minibufferKeymap["C-m"]         = "minibuffer-exit";
        minibufferKeymap["S-tab"]       = "minibuffer-complete";
        // XXX This is slightly wonky because sometimes the scrollbar
        // gets the up and down arrow keys.  I could turn off the scrollbar
        // interactivity but then the mouse doesn't work on it.
        minibufferKeymap["downarrow"]   = "next-element";
        minibufferKeymap["uparrow"]     = "previous-element";

        // These are compat-minibuffer commands.
        minibufferKeymap["home"]         = "move-beginning-of-line";
        minibufferKeymap["end"]          = "move-end-of-line";
        minibufferKeymap["rightarrow"]   = "forward-char";
        minibufferKeymap["leftarrow"]    = "backward-char";
        // We don't use uparrow and down arrow for history because
        // then the completion popup window wouldn't work.
        // compatKeymap["uparrow"]      = "history-previous";
        // compatKeymap["downarrow"]    = "history-next";
        break;
      case "user":
        // We don't do anything with the user keymap.
        break;
      default:
        string message = "No such default keymap \"{0}\".".Formatted(name);
        throw new MinibufferException(message);
    }
  }

  public Dictionary<string,string> ToDict() {
    var dict = map.ToDictionary(kv => kv.Key, kv => kv.Value);
    regexes.Each(kv => { dict["/" + kv.Key.ToString() + "/"] = kv.Value; });
    return dict;
  }

  public int Count {
    get { return map.Count + regexes.Count; }
  }

  public bool Any() {
    return map.Any() || regexes.Any();
  }

  [Command("bind-key",
           description = "Bind a key to a command")]
  /**
     To do this programmatically, one can do the following.
     ```cs
     GetKeymap("user")["C-c f"] = "foo-command";
     ```
   */
  public static void BindKey(List<string> keyAccum = null) {
    Minibuffer m = Minibuffer.instance;
    if (keyAccum == null) {
      keyAccum = new List<string>();
    }
    var keyseq = string.Join(" ", keyAccum.ToArray());
    m.Echo("Bind key (C-g to end): " + keyseq);
    m.ReadKeyChord()
      .Done(k => {
          var key = k.ToString();
          if (m.IsKeyboardQuit(key)
              || key == "return") {
            if (! keyseq.IsZull()) {
              // Get command.
              m.Read(string.Format("Bind key {0} to command: ",
                                 keyseq),
                   "",
                   "command",
                   "command",
                   true)
                .Then(command => {
                    if (command != null) {
                      var user = m.GetKeymap("user");
                      user[keyseq] = command;
                      m.Message("{0} is now bound to {1}.",
                                command,
                                keyseq);
                    } else {
                      m.Message("No key bindings changed.");
                    }
                  });
            } else {
              // We got nothing to bind.  Do nothing.
              m.Message("No key sequence given. Cancelling.");
            }
          } else {
              keyAccum.Add(key);
              BindKey(keyAccum);
          }
        });
  }

  [Command("unbind-key",
           description = "Unbind a key from a command")]
  public static void UnbindKey([Prompt("Unbind key: ",
                                filler = "keybinding")]
                        string keyBinding) {
    Minibuffer m = Minibuffer.instance;
    var command = m.Lookup(keyBinding);
    if (command != null) {
      // Found it.
      m.ReadYesOrNo("{0} runs the command {1}. Remove key binding?"
                    .Formatted(keyBinding,
                               command))
        .Then(yes => {
            if (yes) {
              // XXX Should check all maps.
              var user = m.GetKeymap("user");
              if (user.Remove(keyBinding))
                m.Message("{0} is now unbound.", keyBinding);
              else
                m.Message("Error: Unable to unbind {0}.", keyBinding);
            } else {
              m.Message("Key bindings unchanged.");
            }
          });
    } else {
      // There's nothing.
      m.Message(keyBinding + " is not bound to any command.");
    }
  }
}
}
