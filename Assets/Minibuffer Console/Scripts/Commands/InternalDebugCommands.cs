/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using UnityEngine.UI;
using RSG;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/*
  These commands are for exercising Minibuffer to test it.
 */
[Group("debug", tag = "built-in")]
public class InternalDebugCommands : MonoBehaviour {

  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
    Minibuffer.With(minibuffer => {
        minibuffer.RegisterCommand(new Command("test-prompts") {
            prompts = new [] { new Prompt("X: ") }
          },
          (string x) => minibuffer.Message("Got " + x));
      });

  }

  [Command]
  public void TestCompleter() {
    if (! enabled)
      return;
    var m = Minibuffer.instance;
    m.Read(new PromptInfo("Completer: ") {
        history = "completer",
        completerEntity = new ListCompleter(m.completers.Keys).ToEntity(),
        requireMatch = false
      })
      .Then((name) => {
          return m.Read<PromptResult>(new Prompt(name + ": ") {
              completer = name == "null" ? null : name });
          // return m.MinibufferEdit(name + ": ",
          //                         "",
          //                         null,
          //                         );
        })
      .Then((PromptResult pr) => {
          var s = pr.str;
          var o = pr.obj;
          if (o != null)
            m.Message("Result object " + o);
          else
            m.Message("Result string " + s);
        })
      .Catch(ex => {
          m.Message("Got exception " + ex);
          Debug.LogException(ex);
        }) ;
  }

  [Command]
  public void TestCompleterForType() {
    if (! enabled)
      return;
    var m = Minibuffer.instance;
    m.Read<PromptResult>(new PromptInfo("Completer: ") {
        history = "completer",
        completerEntity = m.GetCompleterEntity(typeof(MiniToggler), true),
        requireMatch = false
      })
      .Then((PromptResult pr) => {
          var s = pr.str;
          var o = pr.obj;
          if (o != null)
            m.Message("Result object " + o);
          else
            m.Message("Result string " + s);
        })
      .Catch(ex => {
          m.Message("Got exception " + ex);
          Debug.LogException(ex);
        }) ;
  }


  [Command]
  public void TestCompleterGeneric() {
    if (! enabled)
      return;
    var m = Minibuffer.instance;
    m.Read(new PromptInfo("Completer: ") {
        history = "completer",
        completerEntity = new ListCompleter(m.completers.Keys).ToEntity(),
        requireMatch = false
      })
      .Then((name) => {
          return m.Read<PromptResult<object>>(new Prompt(name + ": ") {
              completer = name == "null" ? null : name });
          // return m.MinibufferEdit(name + ": ",
          //                         "",
          //                         null,
          //                         );
        })
      .Then((PromptResult<object> pr) => {
          var s = pr.str;
          var o = pr.obj;
          if (o != null)
            m.Message("Result object " + o);
          else
            m.Message("Result string " + s);
        })
      .Catch(ex => {
          m.Message("Got exception " + ex);
          Debug.LogException(ex);
        }) ;
  }

  [Command]
  public string TestExpression() {
    return typeof(object).IsAssignableFrom(typeof(CommandInfo)).ToString();
  }
}
}
