/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SeawispHunter.MinibufferConsole {

public class NumberCoercer : ICoercer {
  protected Type t;

  public Type defaultType { get { return t; } }

  public NumberCoercer(Type t) {
    this.t = t;
  }

  /*
    Return a list of matching strings for the given input.
   */
  public IEnumerable<string> Complete(string input) {
    return Enumerable.Empty<string>();
  }

  /*
    If there is some kind of object associated with the input string,
    return it.
   */
  public object Coerce(string input, Type type) {
    Assert.IsNotNull(type);
    if (! type.IsAssignableFrom(t)) {
      throw new CoercionException(t, type);
    }
    try {
      object r = Convert.ChangeType(input, t);
      return r;
    } catch (Exception) {
      return null;
    }
  }
}

}
