/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

public static class EnumFlags {

  public static int FlagCount<T>(this T flags)
    where T : struct, IConvertible {
      Type type = typeof(T);
      if (type.IsEnum) {
        var values = Enum.GetValues(type);
        int state = Convert.ToInt32(flags);
        int count = 0;
        foreach (var enumValue in values) {
          int value = (int) enumValue;
          if ((state & value) == value)
            count++;
        }
        return count;
      } else {
        throw new System.Exception("Type " + type + " is not an Enum.");
      }
    }

  public static IEnumerable<T> GetFlags<T>(this T flags)
    where T : struct, IConvertible {
    Type type = typeof(T);
    if (type.IsEnum) {
      var values = Enum.GetValues(type);
      int state = Convert.ToInt32(flags);
      var list = new List<T>();
      foreach (var enumValue in values) {
        int value = (int) enumValue;
        if ((state & value) == value)
          list.Add((T) Enum.ToObject(type, value));
      }
      return list;
    } else {
      throw new System.Exception("Type " + type + " is not an Enum.");
    }
  }

  public static string ToNames<T>(this T flags)
    where T : struct, IConvertible {
    var matchedNames = flags.GetFlags().Select(x => x.ToString());
    return "(" + string.Join("|", matchedNames.ToArray()) + ")";
  }
}
}
