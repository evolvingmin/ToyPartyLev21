/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/**
   Have a list of some type T?  Use `ListCompleter<T>`.
 */
public class ListCompleter<T> : ICompleter, ICoercer {

  public Type defaultType { get { return typeof(T); } }

  protected IEnumerable<T> list;
  /**
     Requires a list.
   */
  public ListCompleter(IEnumerable<T> list) {
    this.list = list;
  }

  /**
    Return a list of matching strings for the given input.
   */
  public IEnumerable<string> Complete(string input) {
    return Matcher.Match(input, list.Select(x => x.ToString()));
  }

  /**
    If there is some kind of object associated with the input string,
    return it.
   */
  public object Coerce(string input, Type type) {
    if (! type.IsAssignableFrom(typeof(T)))
      throw new CoercionException(typeof(T), type);
    return list
      .Where(y => y.ToString() == input)
      .FirstOrDefault();
  }
}

/**
   Have a simple list of strings? Use `ListCompleter`.
 */
public class ListCompleter : ListCompleter<string> {

  /**
     Requires a list of strings.
   */
  public ListCompleter(IEnumerable<string> list) : base(list) {}

  /**
     Requires a list of strings.
   */
  public ListCompleter(params string[] list) : base(list) {}
}


}
