/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

public class ColorCoercer : ICoercer {

  public Type defaultType { get { return typeof(UnityEngine.Color); } }
  public string message {
    get { return "0xff0000"; }
  }

  public object Coerce(string input, Type desiredType) {
    if (desiredType != defaultType)
      return null;
    // 0xffffff
    var m
      = Regex.Match(input,
                    @"(?:#|0x|)(?<rgb>[0-9a-fA-F]{6})(?<a>[0-9a-fA-F]{2})?",
                    RegexOptions.IgnoreCase);
    if (m.Success) {
      var r = Convert.ToInt32(m.Groups["rgb"].Value.Substring(0, 2), 16);
      var g = Convert.ToInt32(m.Groups["rgb"].Value.Substring(2, 2), 16);
      var b = Convert.ToInt32(m.Groups["rgb"].Value.Substring(4, 2), 16);
      var a = 255;
      if (m.Groups["a"].Success)
        a = Convert.ToInt32(m.Groups["a"].Value, 16);
      return new UnityEngine.Color(r/255.0f, g/255.0f, b/255.0f, a/255.0f);
    }

    // shorthand 0xfff
    m
      = Regex.Match(input,
                    @"(?:#|0x|)(?<rgb>[0-9a-fA-F]{3})(?<a>[0-9a-fA-F]{1})?",
                    RegexOptions.IgnoreCase);
    if (m.Success) {
      var r = Convert.ToInt32(m.Groups["rgb"].Value.Substring(0, 1), 16);
      var g = Convert.ToInt32(m.Groups["rgb"].Value.Substring(1, 1), 16);
      var b = Convert.ToInt32(m.Groups["rgb"].Value.Substring(2, 1), 16);
      var a = 15;
      if (m.Groups["a"].Success)
        a = Convert.ToInt32(m.Groups["a"].Value, 16);
      return new UnityEngine.Color(r/15.0f, g/15.0f, b/15.0f, a/15.0f);
    }

    return null;
  }
}

}
