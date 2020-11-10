
/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/**
   Provide tab completions for an input string.
*/
public interface ICompleter {
  /**
     Return a list of matching strings for the given input. They do not need to
     be in any particular order.
  */
  IEnumerable<string> Complete(string input);
}

public interface ICompleter<T> {

  /** Return a list of objects matching the given input. The order is important. */
  IEnumerable<T> Complete(string input);
  /** How do we match against it? */
  string MatchCompletion(T obj);
  /** How do we display it? */
  string DisplayCompletion(T obj);
}

public class AnnotaterCompleter<T> : ICompleter<T> {

  ICompleter<T> innerCompleter;
  IAnnotater annotater;
  int lastMaxLength;
  public AnnotaterCompleter(ICompleter<T> innerCompleter,
                            IAnnotater annotater) {
    this.innerCompleter = innerCompleter;
    this.annotater = annotater;
  }

  public IEnumerable<T> Complete(string input) {
    var matches = innerCompleter.Complete(input).ToList();
    lastMaxLength
      = matches
      .Select(x => innerCompleter.DisplayCompletion(x))
      .Aggregate(0, (accum, str) => Math.Max(accum, str.Length));
    return matches;
  }

  public string MatchCompletion(T obj) {
    return innerCompleter.MatchCompletion(obj);
  }

  public string DisplayCompletion(T obj) {
    return string.Format("{0,-" + lastMaxLength + "} {1}",
                         innerCompleter.DisplayCompletion(obj),
                         annotater.Annotate(innerCompleter.MatchCompletion(obj)));
  }
}

/** How can I make a group be toggleable to hide/show itself? */
public class GrouperCompleter<T> : ICompleter<T> {

  ICompletionGrouper innerGrouper;
  ICompleter<T> innerCompleter;
  // IGrouping<string, T> lastGroups;
  Dictionary<T, string> firstEntryForGroup = new Dictionary<T, string>();
  int i = 0;
  List<string> groupNames = new List<string>();

  public GrouperCompleter(ICompleter<T> innerCompleter,
                          ICompletionGrouper innerGrouper) {
    this.innerCompleter = innerCompleter;
    this.innerGrouper = innerGrouper;
  }

  public IEnumerable<T> Complete(string input) {
    firstEntryForGroup.Clear();
    i = 0;
    groupNames.Clear();
    var matches = innerCompleter.Complete(input);
    return matches
      .GroupBy(v => innerGrouper.Group(innerCompleter.MatchCompletion(v)))
      .OrderBy(g => g.Key)
      .SelectMany(g =>
                            //x => innerCompleter.MatchCompletion(x)).Prepend(null)
                  { var entries = g.OrderBy(x => innerCompleter.MatchCompletion(x)).ToList();
                    groupNames.Add(g.Key);
                    firstEntryForGroup[entries.First()] = g.Key;
                    return entries.Prepend<T>(default(T));
                  });


    // editState.candidates = matches
    //   .GroupBy(v => grouper.Group(v))
    //   .OrderBy(g => g.Key)
    //   .SelectMany(g =>
    //               g.OrderBy(x => x)
    //               .Select(x => x)
    //               .Prepend(null))
    //   .ToList();
  }

  public string MatchCompletion(T obj) {
    return obj != null ? innerCompleter.MatchCompletion(obj) : null;
  }
  /** How do we display it? */
  public string DisplayCompletion(T obj) {
    if (obj == null) {
      i = i % groupNames.Count;
      return groupNames[i++];
    }
    var name = innerCompleter.DisplayCompletion(obj);
    // string groupName;
    // if (false && firstEntryForGroup.TryGetValue(obj, out groupName)) {
    //   // We don't match group names.  SHOULD WE!?
    //   return groupName + "\n  " + name;
    // } else {
    return "  " + name;
    // }
  }
}

public class CompleterAdapter<T> : ICompleter {
  ICompleter<T> innerCompleter;

  public IEnumerable<string> Complete(string input) {
    return innerCompleter.Complete(input)
      .Select(x => innerCompleter.DisplayCompletion(x));
  }
}

public class CompleterAdapter2 : ICompleter<string> {
  ICompleter innerCompleter;

  public CompleterAdapter2(ICompleter innerCompleter) {
    this.innerCompleter = innerCompleter;
  }

  public IEnumerable<string> Complete(string input) {
    return innerCompleter.Complete(input).OrderBy(x => x);
  }

  /* And this inability to discriminate for display and match purposes is why we
     need the new ICompleter<T>. */
  public string DisplayCompletion(string obj) {
    return obj;
  }

  public string MatchCompletion(string obj) {
    return obj;
  }
}


public class ToggleCompleter<T> : ICompleter<T> {

  ICompleter<T> innerCompleter;
  Func<T, bool> onSelector;

  public ToggleCompleter(ICompleter<T> innerCompleter, Func<T, bool> onSelector) {
    this.innerCompleter = innerCompleter;
    this.onSelector = onSelector;
  }

  public IEnumerable<T> Complete(string input) {
    return innerCompleter.Complete(input);
  }

  /* And this inability to discriminate for display and match purposes is why we
     need the new ICompleter<T>. */
  public string DisplayCompletion(T obj) {
    return (onSelector(obj) ? "[X] " : "[ ] ") + innerCompleter.DisplayCompletion(obj);
  }

  public string MatchCompletion(T obj) {
    return innerCompleter.MatchCompletion(obj);
  }
}

}
