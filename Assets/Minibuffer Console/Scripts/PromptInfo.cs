/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Linq;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/*
   %PromptInfo wraps a %Prompt object and includes smart default handling and a
    few runtime properties.
 */
public class PromptInfo {

  private Prompt given;
  private Prompt edited;

  static Type[] passThruTypes = new [] { typeof(string), typeof(object), typeof(PromptResult), typeof(PromptResult<>) };
  /*
     Make a copy of another prompt.
   */
  public PromptInfo(Prompt other) {
    if (other == null)
      throw new NullReferenceException("other");
    given = other;
    edited = new Prompt();
  }

  public PromptInfo() {
    given = new Prompt();
    edited = new Prompt();
  }

  public PromptInfo(string prompt) {
    given = new Prompt(prompt);
    edited = new Prompt();
  }

  public PromptInfo(PromptInfo other) {
    given = other.given;
    edited = new Prompt(other.edited);
    completerEntity = other.completerEntity;
  }

  public static PromptInfo CopyOrNew(Prompt p) {
    return (p == null) ? new PromptInfo() : new PromptInfo(p);
  }

  /** The user prompt, e.g.\ "Your Name: "*/
  public string prompt {
    get { return edited.prompt ?? given.prompt; }
    set { edited.prompt = value; }
  }
  /** The default input by default it's "", e.g.\ "Shane" */
  public string input {
    get { return edited.input ?? given.input; }
    set { edited.input = value; }
  }

  /** The name of the command history to use.  New names will create
      new histories. */
  public string history {
    // We need a little bit of smarts here so that we don't need a
    // history everywhere.  We'd prefer smart defaults but
    // want it to listen when we tell it something.
    get {
      return
        edited.history
        ?? given.history
        ?? completer
        ?? (desiredType != null
            && desiredType != typeof(string)
            ? desiredType.PrettyName()
            : null);
    }
    set {
      edited.history = value;
    }
  }

  /** The name of the completer to use. May be inferred based on the type of
      argument. */
  public string completer { get {
      return edited.completer
        ?? given.completer
        ?? (desiredType != null
            && ! IsPassThru(desiredType)
            ? desiredType.PrettyName()
            : null);
    }
    set { edited.completer = value; }
  }

  /** Is the given type one that can be passed thru without any real coercion?
      The types `string`s and `object`s are an example of pass thru types. */
  public static bool IsPassThru(Type t) {
    return passThruTypes.Contains(! t.IsGenericType
                                  ? t
                                  : t.GetGenericTypeDefinition());
  }

  // XXX I don't want to talk about it yet.
  public string filler {
    get { return edited.filler ?? given.filler; }
    set { edited.filler = value; }
  }
  /** Is a match with the completer required? */
  public bool requireMatch {
    // We need a little bit of smarts here so that we don't need a
    // requireMatch everywhere.  We'd prefer smart defaults but
    // want it to listen when we tell it something.
    get {
      if (edited._requireMatch.HasValue)
        return edited._requireMatch.Value;
      else if (given._requireMatch.HasValue)
        return given._requireMatch.Value;
      else
        return completer != null || completerEntity.Any()
          || desiredType == null || desiredType != typeof(string);
    }
    set {
      edited._requireMatch = value;
    }
  }

  internal bool requireCoerceSet;
  /** Must the result coerce? */
  public bool requireCoerce {
    // We need a little bit of smarts here so that we don't need a
    // requireCoerce everywhere.  We'd prefer smart defaults but
    // want it to listen when we tell it something.
    get {
      if (edited._requireCoerce.HasValue)
        return edited._requireCoerce.Value;
      else if (given._requireCoerce.HasValue)
          return given._requireCoerce.Value;
      else
        return desiredType != null && desiredType != typeof(string);
    }
    set { edited._requireCoerce = value; }
  }
  /** Ad Hoc list of completions that will be put into a ListCompleter. */
  public string[] completions {
    get { return edited.completions ?? given.completions; }
    set { edited.completions = value; }
  }

  // Just return null.
  public bool ignore {
    get { return edited.ignore ? true : given.ignore; }
    set { edited.ignore = value; }
  }

  public object defaultValue {
    get { return edited.defaultValue ?? given.defaultValue; }
    set { edited.defaultValue = value; }
  }

  public Type desiredType {
    get { return edited.desiredType ?? given.desiredType; }
    set { edited.desiredType = value; }
  }

  public CompleterEntity completerEntity { get; set; }

  public override string ToString() {
    return "PromptInfo {"
      + "  completer = " + completer + ",\n"
      + "  completerEntity = " + completerEntity + ",\n"
      + " }";
  }
}
}
