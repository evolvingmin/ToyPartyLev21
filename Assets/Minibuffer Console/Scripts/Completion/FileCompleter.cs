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
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/**
   A filename and directory completer.

   There are a few abbreviations.

```
    abbrevs["~"]          = Environment.GetEnvironmentVariable("HOME");
    abbrevs["$assets"]    = UnityEngine.Application.dataPath;
    abbrevs["$data"]      = UnityEngine.Application.persistentDataPath;
    abbrevs["$temp"]      = UnityEngine.Application.temporaryCachePath;
    abbrevs["$streaming"] = UnityEngine.Application.streamingAssetsPath;
```

The command <kbd>M-x open-path</kbd> is a good demonstration of it.
 */
public class FileCompleter : ICompleter, ICoercer {
  public Type defaultType { get { return typeof(string); } }

  /**
     Show directories only? By default no.
   */
  public bool directoriesOnly { get; set; }
  /**
     Show dotfiles? By default no.
   */
  public bool includeDotfiles { get; set; }
  /*
    The include pattern, by default it's "*".
   */
  public string pattern { get; set; }
  /*
    An exclude pattern, by default it's null.
   */
  public string exclude { get; set; }

  public PathName pathName { get; set; }

  private char slash {
    get { return System.IO.Path.DirectorySeparatorChar; }
  }

  public FileCompleter() {
    pattern = "*";
    exclude = null;
    directoriesOnly = false;
    includeDotfiles = false;
    if (pathName == null)
      pathName = PathName.instance;
  }

  /*
    Return a list of matching strings for the given input.
   */
  public IEnumerable<string> Complete(string input) {
    string filename;
    string dirname;
    if (input.Length == 0)
      input = slash.ToString();
    if (pattern == null)
      pattern = "*";
    input = pathName.Expand(input);

    // Add the '/'s after we do all the processing with Directory.Exists
    filename = Path.GetFileName(input);
    try {
      dirname = Path.GetDirectoryName(input);
      dirname = dirname ?? slash.ToString();
    } catch (DirectoryNotFoundException) {
      dirname = slash.ToString();
    }
    Assert.IsNotNull(dirname);
    Assert.IsNotNull(pattern);
    IEnumerable<string> files;
    if (!Directory.Exists(dirname))
      return Enumerable.Empty<string>();
    files = Directory
      .GetDirectories(dirname, pattern)
      .Append(dirname);
    if (! directoriesOnly)
      files = files.Concat(Directory.GetFiles(dirname, pattern));
    if (exclude != null)
      files = files.Where(z => ! z.Contains(exclude));
    if (! includeDotfiles)
      files = files.Where(y => {
          var p = Path.GetFileName(y);
          return ! (p.Length > 0 && p[0] == '.');
        });
    return files
      .Where(y => Path.GetFileName(y).StartsWith(filename))
      .Select(z => Directory.Exists(z) ? z + slash : z)
      .Select(x => pathName.Compress(x));
  }

  /*
    If there is some kind of object associated with the input string,
    return it.
   */
  public object Coerce(string input, Type type) {
    if (! type.IsAssignableFrom(typeof(System.IO.FileInfo))
        && ! type.IsAssignableFrom(typeof(string)))
      throw new CoercionException("Cannot convert from type " + type.PrettyName() + " to the types FileInfo or string.");
    if (input != null) {
      if (type == typeof(string))
        return pathName.Expand(input);
      else
        return new System.IO.FileInfo(pathName.Expand(input));
    } else
      return null;
  }
}

}
