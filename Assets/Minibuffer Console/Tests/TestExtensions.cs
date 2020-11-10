/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using NUnit.Framework;
//using UnityTest;
using System.Linq;
using System.Collections.Generic;
namespace SeawispHunter.MinibufferConsole {
public static class TestExtensions {
  public static KeyChord AssertEqual(this KeyChord kc, string expected) {
    Assert.AreEqual(expected, kc.ToString());
    return kc;
  }

  public static int Count(this ICompleter c) {
    return c.Complete("").ToList().Count;
  }
}
}
