/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SeawispHunter.MinibufferConsole {

public class PathName {
  private static PathName _instance;
  public static PathName instance {
    get {
      if (_instance == null)
        _instance = new PathName();
      return _instance;
    }
  }
  public Dictionary<string, string> abbrevs = new Dictionary<string,string>();
  public PathName() {

    abbrevs["~"] = (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
      ? Environment.GetEnvironmentVariable("HOME")
      : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
    //! [file-abbreviations]
    abbrevs["$assets"]    = UnityEngine.Application.dataPath;
    abbrevs["$data"]      = UnityEngine.Application.persistentDataPath;
    abbrevs["$temp"]      = UnityEngine.Application.temporaryCachePath;
    abbrevs["$streaming"] = UnityEngine.Application.streamingAssetsPath;
    //! [file-abbreviations]
    //abbrevs["$tmp"]       = UnityEngine.Application.temporaryCachePath;
  }

  public string Expand(string str) {
    foreach(var kv in abbrevs) {
      str = str.Replace(kv.Key, kv.Value);
    }
    return str;
  }

  /*
    Return the smallest path given the current abbreviations.
   */
  public string Compress(string str) {
    return abbrevs
      .Select(kv => str.Replace(kv.Value, kv.Key))
      .OrderBy(s => s.Length)
      .FirstOrDefault();
  }
}

}
