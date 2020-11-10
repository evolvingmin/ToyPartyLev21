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

public class EnumCompleter : ICompleter, ICoercer {
  protected Type t;
  private ListCompleter list;
  public  Type defaultType { get { return t; } }

  public EnumCompleter(Type t) {
    Assert.IsTrue(t.IsEnum);
    list = new ListCompleter(Enum.GetNames(t));
    this.t = t;
  }

  /*
    Return a list of matching strings for the given input.
   */
  public IEnumerable<string> Complete(string input) {
    return list.Complete(input);
  }

  /*
    If there is some kind of object associated with the input string,
    return it.
   */
  public object Coerce(string input, Type type) {
    Assert.IsNotNull(type);
    if (type == typeof(string))
      return input;
    else if (type.IsEnum) {
      try {
        return Enum.Parse(type, input, true);
      } catch (Exception) {
       return null;
      }
    } else {
      try {
        return Convert.ChangeType(input, t);
      } catch (Exception) {
        return null;
      }
    }
  }
}

}
