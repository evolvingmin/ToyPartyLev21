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

public static class TextUtils {

  public static object QuoteIfString(object o) {
    if (o == null)
      return "null";
    else if (o.GetType() == typeof(string))
      return string.Format("\"{0}\"", o);
    else
      return o;
  }
  public static IEnumerable<string> PadRight(IEnumerable<string> a) {
    var aMax = a.Aggregate(0, (accum, str) => Math.Max(accum, str.Length));
    return a.Select(aWord => string.Format("{0,-" + aMax + "}", aWord));
  }

  public static IEnumerable<string> PadLeft(IEnumerable<string> a) {
    var aMax = a.Aggregate(0, (accum, str) => Math.Max(accum, str.Length));
    return a.Select(aWord => string.Format("{0," + aMax + "}", aWord));
  }

  public static IEnumerable<string> JoinColumns(string delim,
                                          IEnumerable<string> a,
                                          IEnumerable<string> b) {
    return a.Zip(b, (aWord, bWord) => aWord + delim + bWord);
  }

  public static IEnumerable<string> TwoColumn(IEnumerable<string> a, IEnumerable<string> b) {
    // var aMax = a.Aggregate(0, (accum, str) => Math.Max(accum, str.Length));
    // var bMax = b.Aggregate(0, (accum, str) => Math.Max(accum, str.Length));
    // return a.Zip(b,
    //              (aWord, bWord) =>
    //              string.Format("{0,-" + aMax + "} "
    //                            + "{1," + bMax + "}",
    //                            aWord, bWord));
    return JoinColumns(" ", PadRight(a), PadLeft(b));
  }

  // public static IEnumerable<string> Columnize(IEnumerable<string> a, int columnCount) {
  //   //var b = a.GroupBy((str, i) => i % columnCount);
  //   var columns
  //     = a.Select((s, i) => new { str = s,
  //                                idx = i })
  //     .GroupBy(x => x.idx % columnCount);


  //   return null;
  //   // return a.Zip(b,
  //   //              (aWord, bWord) =>
  //   //              string.Format("{0,-" + aMax + "} "
  //   //                            + "{1," + bMax + "}",
  //   //                            aWord, bWord));
  // }
}
}
