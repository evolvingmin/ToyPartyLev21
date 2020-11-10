/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
#if ! SH_MINIBUFFER_NOLIBS
#define FULL_SERIALIZER
#endif
using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
#if FULL_SERIALIZER
using SeawispHunter.FullSerializer;
#endif
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/*
   KeyChord captures the key pressed and any modifiers.
*/
#if FULL_SERIALIZER
[fsObject("1", Converter = typeof(KeyChordConverter))]
#endif
[Serializable]
public class KeyChord : ISerializationCallbackReceiver {
  /** Was meta key pressed? */
  [NonSerialized]
  public bool meta;
  /** Was control key pressed? */
  [NonSerialized]
  public bool control;
  /** Was shift key pressed? */
  [NonSerialized]
  public bool shift;
  /** Was super key pressed? */
  [NonSerialized]
  public bool super;
  /** Was hyper key pressed?

      \note The hyper key is not by default tied to any existing modifier.
   */
  [NonSerialized]
  public bool hyper;

  [SerializeField]
  private string serialization;

  public char character { get; private set; }
  public string keyName { get; private set; }
  private static string[] kbdExclusions;
  private static Dictionary<string, char> nameToChar;
  private static Dictionary<char, char> shiftedChar;
  private static Dictionary<string, string> standardModifiers;
  public static HashSet<string> modifierKeys;
  public static bool standardNotation = false;

  private string toString;
  public bool hasKeyName {
    get { return !(keyName == null || keyName.Length == 0); }
  }

  public bool hasCharacter {
    get { return ! hasKeyName; }
  }

  internal KeyChord() {
    character = '\0';
    keyName = null;
  }

  public override bool Equals(object obj) {
    if (obj == null || GetType() != obj.GetType())
      return false;
    KeyChord kc = (KeyChord) obj;
    return meta == kc.meta
      && control == kc.control
      && shift == kc.shift
      && super == kc.super
      && hyper == kc.hyper
      && character == kc.character
      && keyName == kc.keyName;
  }

  public override int GetHashCode() {
    int hash = 0;
    if (meta)
      hash |= 1;
    if (control)
      hash |= 2;
    if (super)
      hash |= 4;
    if (hyper)
      hash |= 8;
    if (keyName == null)
      return hash ^ (int) character;
    else
      return hash ^ keyName.GetHashCode();

    // return ToString().GetHashCode();
  }

  /**
     Provide the character the key represents.
   */
  public KeyChord(char c) {
    character = c;
    keyName = null;
  }

  /**
     Provide the name of the key like "return". Do not use this on "C-f" because
     there is no key "C-f". Instead use FromString().
   */
  public KeyChord(string keyName) {
    character = '\0';
    if (IsKeyCode(keyName))
      this.keyName = keyName;
    else
      throw new MinibufferException("KeyChord error: No such key code '{0}'."
                                    .Formatted(keyName));
  }

  private string kbd(string mod) {
    if (! standardNotation)
      return mod;
    else {
      string standardModifier;
      if (standardModifiers.TryGetValue(mod, out standardModifier))
        return standardModifier;
      else
        return mod;
    }

  }

  public override string ToString() {
    if (toString == null) {
      var s = new System.Text.StringBuilder();
      if (control) {
        s.Append(kbd("C-")); // control
      }
      if (hyper) {
        s.Append(kbd("H-")); // hyper
      }
      if (meta) {
        s.Append(kbd("M-")); // meta
      }
      if (shift &&
          ((hasCharacter && ! shiftedChar.ContainsValue(character)) //char.IsUpper(character))
           || !hasCharacter)) {
        s.Append(kbd("S-")); // shift
      }
      if (super) {
        s.Append(kbd("s-")); // super
      }
      if (hasKeyName) {
        s.Append(keyName);
      } else {
        s.Append(character);
      }
      toString = s.ToString();
    }
    return toString;
  }

  /**
     Parse a key chord from a string, e.g., "C-f".
  */
  public static KeyChord FromString(string input, KeyChord kc = null) {
    if (input.IsZull())
      throw new ArgumentException("input is zull");
    // if (standardNotation) {
      // Should I always run this?
      foreach (var kv in standardModifiers)
        input = input.Replace(kv.Value, kv.Key);
    // }
    Match match = Regex.Match(input, "^(?<mods>([CMSsH]-)*)(?<key>.*)$");
    if (kc == null) {
      kc = new KeyChord();
    }
    if (match.Success) {
      var key = match.Groups["key"].Value;
      if (key.Length == 1)
        // character
        kc = new KeyChord(key[0]);
      else {
        if (IsKeyCode(key))
          kc = new KeyChord(key);
        else
          throw new MinibufferException("KeyChord error: No such key code '{1}' in '{0}'.".Formatted(input, key));
      }
    } else
      kc = new KeyChord(input);

    var mods = match.Groups["mods"].Value;
    foreach(var c in mods) {
      switch (c) {
        case 'C':
          kc.control = true;
          break;
        case 'M':
          kc.meta = true;
          break;
        case 's':
          kc.super = true;
          break;
        case 'S':
          kc.shift = true;
          break;
        case 'H':
          kc.hyper = true;
          break;
        case '-':
          break;
      }
    }
    return kc;
  }

  private KeyChord SuperToCtrlIfWindows() {
    #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    if (super) {
      super = false;
      control = true;
    }
    #endif
    return this;
  }

  // public static string CtrlIfWin(string keychords) {
  //   return string.Join(" ",
  //                      keyChordSequence.Split(' ')
  //                      .Select(k => KeyChord.FromString(k).SuperToCtrlIfWindows().ToString())
  //                      .ToArray());
  // }

  public static IEnumerable<KeyChord> Parse(string keyChordSequence) {
    return keyChordSequence.Split(' ')
      .Select(k => KeyChord.FromString(k).Canonical());
  }

  /**
     Canonize a key chord sequence.

     E.g., Canonize("S-f M-S-x S-g") -> "F M-X G"
   */
  public static string Canonize(string keyChordSequence) {
    return string.Join(" ",
                       Parse(keyChordSequence)
                       .Select(k => k.ToString())
                       .ToArray());
  }

  /**
     Provide a canonical representation of the KeyChord.

     For instance the key chords representing shift-f-key like {"S-f", "S-F",
     "F"} are represented as simply "F".
  */
  public KeyChord Canonical() {
    // Let's normalize from {"S-f", "S-F", "F"} to "F".
    var kc = this;

    if (kc.hasKeyName
        && ! kbdExclusions.Contains(kc.keyName)
        && nameToChar.ContainsKey(kc.keyName)) {
        var kcp = new KeyChord(nameToChar[kc.keyName])
          { control = kc.control,
            meta = kc.meta,
            super = kc.super,
            hyper = kc.hyper,
            shift = kc.shift };
        kc = kcp;
    }
    if (kc.hasCharacter
        && (shiftedChar.ContainsKey(kc.character)
            || shiftedChar.ContainsValue(kc.character))//char.IsLetter(kc.character)
        && (kc.shift
            || shiftedChar.ContainsValue(kc.character))) { //char.IsUpper(kc.character))) {
      if (shiftedChar.ContainsKey(kc.character)) {
        var kcp = new KeyChord(shiftedChar[kc.character]); //char.ToUpper(kc.character));
        kcp.control = kc.control;
        kcp.meta = kc.meta;
        kcp.super = kc.super;
        kcp.hyper = kc.hyper;
        kc = kcp;
      }
      kc.shift = true;
    }

    return kc;
  }

  /**
     Convert an Event to a KeyChord.
   */
  public static KeyChord FromEvent(Event e) {
    // if (! e.isKey)
    //   return null;

    var keyName = e.keyCode.ToString().ToLower();
    KeyChord kc;
    bool shiftFudge = false;
    if (! kbdExclusions.Contains(keyName)
        && nameToChar.ContainsKey(keyName)) {
      kc = new KeyChord(nameToChar[keyName]);
      // This feels pretty hacky.  I'm just trying to account for
      // shifted characters.
      if (shiftedChar.ContainsKey(kc.character) &&
          e.character == shiftedChar[kc.character]) {
      // if (e.character != '\0' && e.character < 256) {
        kc = new KeyChord(e.character);
        //shiftFudge = true;
      }
    } else {
      if (keyName.Length == 1)
        kc = new KeyChord(keyName[0]);
      else
        kc = new KeyChord(keyName);
    }
    kc.shift = (e.shift
                //|| ((e.modifiers & EventModifiers.Shift) == EventModifiers.Shift)
                || shiftFudge
                || char.IsUpper(e.character));
    kc.control = e.control;
    kc.meta = e.alt;
    kc.super = e.command;
    return kc;
  }

  /**
    Return true if we can force to character.
   */
  public bool ForceCharacter(out char c) {
    if (hasCharacter) {
      c = character;
      return true;
    } else if (nameToChar.ContainsKey(keyName)) {
      c = nameToChar[keyName];
      return true;
    }
    c = '\0';
    return false;
  }

  /**
     Return true if the string represents a KeyCode enum value, false
     otherwise.

     e.g., IsKeyCode("return") -> true
     IsKeyCode("blah") -> false
   */
  public static bool IsKeyCode(string s) {
    try {
      System.Enum.Parse(typeof(KeyCode), s, true);
      return true;
    } catch (System.ArgumentException) {
      return false;
    }
  }

  static KeyChord() {
    if (kbdExclusions == null)
      kbdExclusions = new string[] { "none", "backspace", "delete", "tab", "clear", "return", "pause", "escape", "space" };
    if (nameToChar == null) {
    nameToChar = new Dictionary<string, char>();
        /*
    nameToChar["none"]           = ' ';
    nameToChar["backspace"]      = ' ';
    nameToChar["delete"]         = ' ';
    */
    nameToChar["tab"]            = '\t';
//  nameToChar["clear"]          = ' ';
    nameToChar["return"]         = '\n';
    /*
    nameToChar["pause"]          = ' ';
    nameToChar["escape"]         = ' ';
    */
    nameToChar["space"]          = ' ';
    nameToChar["keypad0"]        = '0';
    nameToChar["keypad1"]        = '1';
    nameToChar["keypad2"]        = '2';
    nameToChar["keypad3"]        = '3';
    nameToChar["keypad4"]        = '4';
    nameToChar["keypad5"]        = '5';
    nameToChar["keypad6"]        = '6';
    nameToChar["keypad7"]        = '7';
    nameToChar["keypad8"]        = '8';
    nameToChar["keypad9"]        = '9';
    nameToChar["keypadperiod"]   = '.';
    nameToChar["keypaddivide"]   = '/';
    nameToChar["keypadmultiply"] = '*';
    nameToChar["keypadminus"]    = '-';
    nameToChar["keypadplus"]     = '+';
    nameToChar["keypadenter"]    = '\n';
    nameToChar["keypadequals"]   = '=';
    /*
    nameToChar["uparrow"]        = ' ';
    nameToChar["downarrow"]      = ' ';
    nameToChar["rightarrow"]     = ' ';
    nameToChar["leftarrow"]      = ' ';
    nameToChar["insert"]         = ' ';
    nameToChar["home"]           = ' ';
    nameToChar["end"]            = ' ';
    nameToChar["pageup"]         = ' ';
    nameToChar["pagedown"]       = ' ';
    nameToChar["f1"]             = ' ';
    nameToChar["f2"]             = ' ';
    nameToChar["f3"]             = ' ';
    nameToChar["f4"]             = ' ';
    nameToChar["f5"]             = ' ';
    nameToChar["f6"]             = ' ';
    nameToChar["f7"]             = ' ';
    nameToChar["f8"]             = ' ';
    nameToChar["f9"]             = ' ';
    nameToChar["f10"]            = ' ';
    nameToChar["f11"]            = ' ';
    nameToChar["f12"]            = ' ';
    nameToChar["f13"]            = ' ';
    nameToChar["f14"]            = ' ';
    nameToChar["f15"]            = ' ';
    */
    nameToChar["alpha0"]         = '0';
    nameToChar["alpha1"]         = '1';
    nameToChar["alpha2"]         = '2';
    nameToChar["alpha3"]         = '3';
    nameToChar["alpha4"]         = '4';
    nameToChar["alpha5"]         = '5';
    nameToChar["alpha6"]         = '6';
    nameToChar["alpha7"]         = '7';
    nameToChar["alpha8"]         = '8';
    nameToChar["alpha9"]         = '9';
    nameToChar["exclaim"]        = '!';
    nameToChar["doublequote"]    = '"';
    nameToChar["hash"]           = '#';
    nameToChar["dollar"]         = '$';
    nameToChar["ampersand"]      = '&';
    nameToChar["quote"]          = '\'';
    nameToChar["leftparen"]      = '(';
    nameToChar["rightparen"]     = ')';
    nameToChar["asterisk"]       = '*';
    nameToChar["plus"]           = '+';
    nameToChar["comma"]          = ',';
    nameToChar["minus"]          = '-';
    nameToChar["period"]         = '.';
    nameToChar["slash"]          = '/';
    nameToChar["colon"]          = ':';
    nameToChar["semicolon"]      = ';';
    nameToChar["less"]           = '<';
    nameToChar["equals"]         = '=';
    nameToChar["greater"]        = '>';
    nameToChar["question"]       = '?';
    nameToChar["at"]             = '@';
    nameToChar["leftbracket"]    = '[';
    nameToChar["backslash"]      = '\\';
    nameToChar["rightbracket"]   = ']';
    nameToChar["caret"]          = '^';
    nameToChar["underscore"]     = '_';
    nameToChar["backquote"]      = '`';
    /*
    nameToChar["a"]              = ' ';
    nameToChar["b"]              = ' ';
    nameToChar["c"]              = ' ';
    nameToChar["d"]              = ' ';
    nameToChar["e"]              = ' ';
    nameToChar["f"]              = ' ';
    nameToChar["g"]              = ' ';
    nameToChar["h"]              = ' ';
    nameToChar["i"]              = ' ';
    nameToChar["j"]              = ' ';
    nameToChar["k"]              = ' ';
    nameToChar["l"]              = ' ';
    nameToChar["m"]              = ' ';
    nameToChar["n"]              = ' ';
    nameToChar["o"]              = ' ';
    nameToChar["p"]              = ' ';
    nameToChar["q"]              = ' ';
    nameToChar["r"]              = ' ';
    nameToChar["s"]              = ' ';
    nameToChar["t"]              = ' ';
    nameToChar["u"]              = ' ';
    nameToChar["v"]              = ' ';
    nameToChar["w"]              = ' ';
    nameToChar["x"]              = ' ';
    nameToChar["y"]              = ' ';
    nameToChar["z"]              = ' ';
    */
    /*
    nameToChar["numlock"]        = ' ';
    nameToChar["capslock"]       = ' ';
    nameToChar["scrolllock"]     = ' ';
    nameToChar["rightshift"]     = ' ';
    nameToChar["leftshift"]      = ' ';
    nameToChar["rightcontrol"]   = ' ';
    nameToChar["leftcontrol"]    = ' ';
    nameToChar["rightalt"]       = ' ';
    nameToChar["leftalt"]        = ' ';
    nameToChar["leftcommand"]    = ' ';
    nameToChar["leftapple"]      = ' ';
    nameToChar["leftwindows"]    = ' ';
    nameToChar["rightcommand"]   = ' ';
    nameToChar["rightapple"]     = ' ';
    nameToChar["rightwindows"]   = ' ';
    nameToChar["altgr"]          = ' ';
    nameToChar["help"]           = ' ';
    nameToChar["print"]          = ' ';
    nameToChar["sysreq"]         = ' ';
    nameToChar["break"]          = ' ';
    nameToChar["menu"]           = ' ';
    */
    }
    if (shiftedChar == null) {
    shiftedChar = new Dictionary<char, char>();
    shiftedChar['1']             = '!';
    shiftedChar['2']             = '@';
    shiftedChar['3']             = '#';
    shiftedChar['4']             = '$';
    shiftedChar['5']             = '%';
    shiftedChar['6']             = '^';
    shiftedChar['7']             = '&';
    shiftedChar['8']             = '*';
    shiftedChar['9']             = '(';
    shiftedChar['0']             = ')';
    shiftedChar['-']             = '_';
    shiftedChar['=']             = '+';
    shiftedChar['[']             = '{';
    shiftedChar[']']             = '}';
    shiftedChar['\\']            = '|';
    shiftedChar[';']             = ':';
    shiftedChar['\'']            = '"';
    shiftedChar[',']             = '<';
    shiftedChar['.']             = '>';
    shiftedChar['/']             = '?';
    shiftedChar['`']             = '~';
    shiftedChar['a']             = 'A';
    shiftedChar['b']             = 'B';
    shiftedChar['c']             = 'C';
    shiftedChar['d']             = 'D';
    shiftedChar['e']             = 'E';
    shiftedChar['f']             = 'F';
    shiftedChar['g']             = 'G';
    shiftedChar['h']             = 'H';
    shiftedChar['i']             = 'I';
    shiftedChar['j']             = 'J';
    shiftedChar['k']             = 'K';
    shiftedChar['l']             = 'L';
    shiftedChar['m']             = 'M';
    shiftedChar['n']             = 'N';
    shiftedChar['o']             = 'O';
    shiftedChar['p']             = 'P';
    shiftedChar['q']             = 'Q';
    shiftedChar['r']             = 'R';
    shiftedChar['s']             = 'S';
    shiftedChar['t']             = 'T';
    shiftedChar['u']             = 'U';
    shiftedChar['v']             = 'V';
    shiftedChar['w']             = 'W';
    shiftedChar['x']             = 'X';
    shiftedChar['y']             = 'Y';
    shiftedChar['z']             = 'Z';
    }

    if (modifierKeys == null) {
      var s = new HashSet<string>();
      s.Add("rightshift");
      s.Add("leftshift");
      s.Add("rightcontrol");
      s.Add("leftcontrol");
      s.Add("rightalt");
      s.Add("leftalt");
      s.Add("leftcommand");
      s.Add("leftapple");
      s.Add("leftwindows");
      s.Add("rightcommand");
      s.Add("rightapple");
      s.Add("rightwindows");
      modifierKeys = s;
    }
    if (standardModifiers == null) {
      standardModifiers = new Dictionary<string, string>();
      standardModifiers["C-"] = "ctrl-";
      standardModifiers["M-"] = "alt-";
      standardModifiers["S-"] = "shift-";
      #if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
      standardModifiers["s-"] = "win-";
      #elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
      standardModifiers["s-"] = "cmd-";
      #else
      standardModifiers["s-"] = "super-";
      #endif
    }
  }

  public void OnBeforeSerialize() {
    //Debug.Log("OnBeforeSerialize");
    serialization = ToString();
  }

  public void OnAfterDeserialize() {
    //Debug.Log("OnAfterDeserialize");
    if (! serialization.IsZull()) {
      FromString(serialization, this);
    }
  }

    public static bool IsModifierKey(KeyCode kc) {
        switch (kc) {
            case KeyCode.RightControl:
            case KeyCode.RightAlt:
            case KeyCode.RightCommand:
            // case KeyCode.RightApple:
            case KeyCode.RightWindows:
            case KeyCode.RightShift:
            case KeyCode.LeftControl:
            case KeyCode.LeftAlt:
            case KeyCode.LeftCommand:
            // case KeyCode.LeftApple:
            case KeyCode.LeftWindows:
            case KeyCode.LeftShift:
                return true;
            default:
                return false;
        }
    }

}

#if FULL_SERIALIZER
public class KeyChordConverter : fsDirectConverter {
    public override Type ModelType { get { return typeof(KeyChord); } }

    public override object CreateInstance(fsData data, Type storageType) {
        return new KeyChord();
    }

    public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType) {
      serialized = new fsData(((KeyChord)instance).ToString());
      return fsResult.Success;
    }

    public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType) {
      if (! data.IsString) return fsResult.Fail("Expected string in " + data);
      if (data.IsNull) return fsResult.Fail("Expected non-null in " + data);

      instance = KeyChord.FromString(data.AsString);
      return fsResult.Success;
    }
}
#endif

}
