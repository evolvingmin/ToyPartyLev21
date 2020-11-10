/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SeawispHunter.MinibufferConsole {

/**
   Have objects with specialized names? Use this completer.

   The completion name will be the key and the coercion will be the
   value from the dictionary.
 */
public class DictCompleter<ValueT> : ICompleter, ICoercer {

  public bool stringCoerceable = false;

  protected Dictionary<string, ValueT> dict;
  public Func<KeyValuePair<string, ValueT>, bool> filter;
  public Type defaultType { get { return typeof(ValueT); } }

  public DictCompleter(Dictionary<string, ValueT> dict) {
    this.dict = dict;
  }

  /*
    Return a list of matching strings for the given input.
   */
  public virtual IEnumerable<string> Complete(string input) {
    if (filter == null)
      return Matcher.Match(input, dict.Select(x => x.Key));
    else
      return Matcher.Match(input, dict.Where(filter).Select(x => x.Key));
  }

  /*
    If there is some kind of object associated with the input string,
    return it.
   */
  public virtual object Coerce(string input, System.Type type) {
    //UnityEngine.Debug.Log("desired type " + type);
    if (stringCoerceable && type == typeof(string))
      return input;
    if (! type.IsAssignableFrom(typeof(ValueT)))
      throw new CoercionException(typeof(ValueT), type);
    ValueT value;
    if (dict.TryGetValue(input, out value))
      return value;
    else
      return null;
  }
}

/*
   Have a simple list of strings? This is your completer.
*/
public class DictCompleter : DictCompleter<string> {

  /**
     Requires a list of strings.
  */
  public DictCompleter(Dictionary<string, string> list) : base(list) {
  }
}
}
