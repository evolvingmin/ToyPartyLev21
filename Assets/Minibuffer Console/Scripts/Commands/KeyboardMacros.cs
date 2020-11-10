/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
#if ! SH_MINIBUFFER_NOLIBS
#define FULL_SERIALIZER
#endif
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using SeawispHunter.MinibufferConsole.Extensions;
#if FULL_SERIALIZER
using SeawispHunter.FullSerializer;
#endif

namespace SeawispHunter.MinibufferConsole {

#if FULL_SERIALIZER
[fsObject("1", Converter = typeof(KeyboardMacroConverter))]
#endif
public class KeyboardMacro {
  public List<KeyChord> keys = new List<KeyChord>();
  public List<float>  timing = new List<float>();

  public void Clear() {
    keys.Clear();
    timing.Clear();
  }
}

/**
   ![KeyboardMacros in the inspector](inspector/keyboard-macros.png)
   Record your key strokes and play them back.
 */
[Group(tag = "built-in")]
public class KeyboardMacros : MonoBehaviour {

#if FULL_SERIALIZER
  [fsIgnore]
#endif
  // private KeyboardMacro lastMacro = new KeyboardMacro();
  private KeyboardMacro lastMacro {
    get { return macros["*last-macro*"]; }
    set { macros["*last-macro*"] = value; }
  }
  public Dictionary<string, KeyboardMacro> macros
    = new Dictionary<string, KeyboardMacro>();
#if FULL_SERIALIZER
  [fsIgnore]
#endif
  private bool _isRecording = false;
  public bool isRecording {
    get { return _isRecording; }
  }

  private bool _isPlaying = false;
  public bool isPlaying {
    get { return _isPlaying; }
  }

  private bool _isPausing = false;
  public bool isPausing {
    get { return _isPausing; }
  }
  public float speed = 1;
  private int step = 0;
#if FULL_SERIALIZER
  [fsIgnore]
#endif
  [Variable("temporal-macros", description = "Play macros at the same speed they were recorded?")]
  [Header("Play macros back at same speed they were recorded?")]
  public bool temporalMacros = false;
  #if FULL_SERIALIZER
  [fsIgnore]
  #endif

  [Header("Load macros from last session?")]
  public bool loadMacros = true;
  [Header("Save macros for next session?")]
  public bool saveMacros = true;

  public MinibufferListing MinibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
    Minibuffer.With((m) => {
        if (loadMacros)
          Load();
        m.completers["KeyboardMacro"] = new DictCompleter<KeyboardMacro>(macros).ToEntity();

        LoadAssembliesWarning.OnWarning(() => {
            if (saveMacros) {
              Debug.Log("Saving macros before reload; turning off persistence.");
              Save();
              saveMacros = false;
            }
          });
      });

  }

  [Command(description = "Start keyboard macro recording.",
           keyBinding = "C-x (")]
  public void StartMacro() {
    if (_isRecording) {
      Minibuffer.instance.MessageAlert("Already recording a macro.");
      return;
    }
    lastMacro = new KeyboardMacro();
    float lastAdd = Time.time;
    bool lastWasQuit = false;
    _isRecording = true;
    Func<KeyChord, bool> fn = null;
    fn = k => {
      // Does quiting really kill a keyboard macro?
      if (Minibuffer.instance.IsKeyboardQuit(k.ToString())) {
        if (! lastWasQuit) {
          Minibuffer.instance.MessageAlert("Quit; still recording macro. Quit again to stop recording macro.");
          lastWasQuit = true;
        } else {
          EndMacro();
          return true; // Let's eat this quit.
        }
      } else {
        lastWasQuit = false;
      }

      if (_isRecording) {
        lastMacro.keys.Add(k);
        lastMacro.timing.Add(Time.time - lastAdd);
        lastAdd = Time.time;
        Minibuffer.instance.ReadKeyChord(fn);
      }
      return false; // Let minibuffer handle it.
    };
    Minibuffer.instance.Message("Begin recording macro.");
    Minibuffer.instance.ReadKeyChord(fn);
  }

  [Command(description = "Stop a keyboard macro recording.",
           keyBinding = "C-x )")]
  public void EndMacro(bool showWarning = true) {
    if (! _isRecording) {
      if (showWarning)
        Minibuffer.instance.MessageAlert("Not recording a macro.");
    } else {
      _isRecording = false;
      // Pop off the last keyboard that activated this command.
      IEnumerable<KeyChord> ks = KeyChord.Parse(Minibuffer.instance.lastKeySequence);
      // Debug.Log("lastKeySequence " + Minibuffer.instance.lastKeySequence);
      // Debug.Log("lastMacro.keys " + string.Join(", ", lastMacro.keys.Select(x => x.ToString()).ToArray()));
      // Debug.Log("ks " + string.Join(", ", ks.Select(x => x.ToString()).ToArray()));
      // Debug.Log("ks.Count " + ks.Count());
      lastMacro.keys.RemoveRange(lastMacro.keys.Count - ks.Count(), ks.Count());
      lastMacro.timing.RemoveRange(lastMacro.timing.Count - ks.Count(), ks.Count());

      // Debug.Log("after lastMacro.keys " + string.Join(", ", lastMacro.keys.Select(x => x.ToString()).ToArray()));
      Minibuffer.instance.Message("Finished recording macro.");
    }
  }

  [Command(description = "Stop a keyboard macro recording and execute it. "
                       + "If next key is 'e', play the same macro again.",
           keyBinding = "C-x e")]
  public void EndAndCallMacro(KeyboardMacro macro = null) {
    // Stop recording.
    EndMacro(false);
    if (macro == null)
      macro = lastMacro;
    // if (! temporalMacros) {
    //   // XXX no real _isPlaying possible here.
    //   macro.keys.Each(k =>
    //                   Minibuffer.instance.keyQueue.Enqueue(k));
    //   // How to watch for e?
    // } else {
    StartCoroutine(PlayMacro(macro, temporalMacros));
  }

  // XXX Can't make this 'C-x \' because the running macro will interrupt it.
  // Would need to have two streams of inputs to do that: An interactive one and
  // a non-interactive one.
  [Command(keyBinding = "C-\\")]
  /*public*/ string PauseMacro() {
    if (! _isPlaying)
      return "No macro playing currently.";
    _isPausing = ! _isPausing;
    if (_isPausing)
      return "Pausing macro.";
    else
      return "Resuming macro.";
  }

  [Command(keyBinding = "C-backspace", tag = "no-echo")]
  /*public*/ void StepMacro() {
    if (! _isPlaying)
      Minibuffer.instance.Message("No macro playing currently.");
    if (! _isPausing)
      Minibuffer.instance.Message("Macro must be paused to single step.");
    step++;

  }

  [Command]
  /*public*/ IEnumerator RetimeMacro(KeyboardMacro macro, string newMacroName) {
    macro = lastMacro;
    var newMacro = new KeyboardMacro();
    float lastAdd = Time.time;
    var stop = false;
    _isPlaying = true;
    // HACK
    // Minibuffer.instance.visible = false;
    for (int i = 0; i < macro.keys.Count ; i++) {
      Minibuffer.instance.keyQueue.Enqueue(macro.keys[i]);
      yield return null;
      newMacro.keys.Add(macro.keys[i]);
      newMacro.timing.Add(Time.time - lastAdd);
      lastAdd = Time.time;
      var p = Minibuffer.instance.ReadKeyChord()
        .Then(keyChord => {
            if (Minibuffer.instance.IsKeyboardQuit(keyChord.ToString())) {
              stop = true;
            }
          });
      yield return p.WaitForPromise();
      if (stop)
        break;
    }
    _isPlaying = false;
    macros[newMacroName] = newMacro;
    Minibuffer.instance.Message("New macro saved as '{0}'.", newMacroName);
  }

  private IEnumerator PlayMacro(KeyboardMacro macro, bool temporal) {
    if (temporal && macro.keys.Count != macro.timing.Count) {
      Debug.LogError("Macro keys and timing arrays are not the same size. Cannot play macro");
      yield break;
    }

    _isPlaying = true;
    for (int i = 0; i < macro.keys.Count
                    && ((temporal && i < macro.timing.Count)
                        || ! temporal); i++) {
      // print ("enqueing " + macro.keys[i]);
      yield return temporal ? new WaitForSeconds(macro.timing[i] / speed) : null;
      Minibuffer.instance.keyQueue.Enqueue(macro.keys[i]);
      while (isPausing) {
        if (step > 0) {
          step--;
          break;
        }
        yield return null;
      }
    }
    _isPlaying = false;
    yield return null;
    // This is called immediately eating the last thing put into the
    // queue.
    Minibuffer.instance.ReadKeyChord(k => {
        if (k.ToString() == "e") {
          // Redo Macro.
          StartCoroutine(PlayMacro(macro, temporal));
          return true;
        } else {
          return false;
        }
      });
  }

  [Command(description = "Name the last macro as a new command.")]
  public void NameLastMacro([Prompt("Name for last macro: ")]
                            string name) {
    // Save the current values.
    macros[name] = lastMacro;
    CreateMacroCommand(name, lastMacro);
    lastMacro = new KeyboardMacro();
  }

  private void CreateMacroCommand(string name, KeyboardMacro macro) {
    Minibuffer.instance
      .RegisterCommand(new Command(name) {
          description = "Keyboard macro, recorded over "
            + macro.timing.Sum() + " seconds." },
        () => {
          EndAndCallMacro(macro);
        });
  }

  [Command(description = "Show macros in a buffer.")]
  public void ShowMacros() {
    var b = Minibuffer.instance.GetBuffer("keyboard-macros.json", true);
    b.content = StringSerializationAPI.Serialize(typeof(Dictionary<string, KeyboardMacro>), macros);
    Minibuffer.instance.Display(b);
  }

  public void Save() {
    StringSerializationAPI
      .Save<Dictionary<string, KeyboardMacro>>
        ("$data/keyboard-macros.json", macros);

  }

  public void Load() {
    macros = StringSerializationAPI
      .Load<Dictionary<string, KeyboardMacro>>("$data/keyboard-macros.json");
    if (macros != null) {
      macros
        .Where(kv => kv.Key != "*last-macro*")
        .Each(kv =>
                  CreateMacroCommand(kv.Key, kv.Value));
    } else {
      macros = new Dictionary<string, KeyboardMacro>();
    }
  }

  //[Command]
  public void LoadMacros() {
    var b = Minibuffer.instance.GetBuffer("keyboard-macros.json", true);
    macros = (Dictionary<string, KeyboardMacro>)
      StringSerializationAPI.Deserialize(typeof(Dictionary<string, KeyboardMacro>),
                                         b.content);
  }

  public void OnDisable() {
    if (saveMacros) {
      Save();
      saveMacros = false;
    }
  }
}

#if FULL_SERIALIZER
public class KeyboardMacroConverter : fsDirectConverter {
  public override Type ModelType { get { return typeof(KeyboardMacro); } }

  public override object CreateInstance(fsData data, Type storageType) {
    return new KeyboardMacro();
  }

  public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType) {
    var keyboardMacro = (KeyboardMacro) instance;
    var list = new List<fsData>();
    for (int i = 0; i < keyboardMacro.keys.Count || i < keyboardMacro.timing.Count; i++) {
      if (i < keyboardMacro.timing.Count)
        list.Add(new fsData(System.Math.Round(keyboardMacro.timing[i], 2)));
      if (i < keyboardMacro.keys.Count)
        list.Add(new fsData(keyboardMacro.keys[i].ToString()));
      }
    serialized = new fsData(list);
    return fsResult.Success;
  }

  public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType) {
    if (data.IsList == false)
      return fsResult.Fail("Expected list in " + data);
    var list = data.AsList;
    // instance = new KeyboardMacro();
    // for (int i = 0; i < list.Count; i++) {
    //   if (list[i].IsString)
    //     instance.keys.Add(Keychord.FromString(list[i].AsString));
    //   else if (list[i].IsDouble)
    //     instance.timing.Add((float) list[i].AsDouble);
    // }
    instance = new KeyboardMacro()
      { keys = list
               .Where(x => x.IsString)
               .Select(x => KeyChord.FromString(x.AsString)).ToList(),
        timing = list
                 .Where(x => x.IsDouble)
                 .Select(x => (float) x.AsDouble).ToList() };
    return fsResult.Success;
  }
}
#endif
}
