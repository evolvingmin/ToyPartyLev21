/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SeawispHunter.MinibufferConsole {

public enum Matchers {
  PrefixMatcher,
  SubstringMatcher,
  WordPrefixMatcher
};

/*
   Tab completion uses an IMatcher to determine if the input _matches_
   an element in the ICompleter.
 */
public interface IMatcher {
  /**
     Should the matching be case sensitive?
   */
  bool caseSensitive { get; set; }
  /**
     Return all strings from source that _match_ the input string.
   */
  IEnumerable<string> Match(string input, IEnumerable<string> source);
}

public static class Matcher {
  internal static IMatcher defaultMatcher
    = new PrefixMatcher();
  //= new ContainsMatcher();

  public static IEnumerable<string> Match(string input,
                                          IEnumerable<string> source) {
    return defaultMatcher.Match(input, source);
  }

  public static IMatcher MatcherFromEnum(Matchers autoCompleteMatcher) {
    switch (autoCompleteMatcher) {
      case Matchers.PrefixMatcher:
        return new PrefixMatcher();
      case Matchers.SubstringMatcher:
        return new SubstringMatcher();
      case Matchers.WordPrefixMatcher:
        return new WordPrefixMatcher();
      default:
        throw new MinibufferException("No matcher set.");
    }
  }
}

public class PrefixMatcher : IMatcher {
  private bool _caseSensitive = false;
  public bool caseSensitive {
    get { return _caseSensitive; }
    set { _caseSensitive = value; }
  }
  public virtual IEnumerable<string> Match(string input,
                                           IEnumerable<string> source) {
      return source
        .Where(x => x.StartsWith(input, ! caseSensitive, null));
  }
}

public class WordPrefixMatcher : PrefixMatcher {

  public override IEnumerable<string> Match(string input,
                                            IEnumerable<string> source) {
    return source
      .Where(x => {
          if (x.StartsWith(input, ! caseSensitive, null))
            return true;
          string[] words;
          if (Minibuffer.instance.commandCase == ChangeCase.Case.KebabCase)
            words = x.Split(new [] {'-', '*', '_'},
                            StringSplitOptions.RemoveEmptyEntries);
          else
            words = Regex.Split(x, @"(?<!^)(?=[A-Z])");
          return words.Any(w => w.StartsWith(input, ! caseSensitive, null));
        });
  }
}


public class SubstringMatcher : PrefixMatcher {

  public override IEnumerable<string> Match(string input,
                                            IEnumerable<string> source) {
    if (! caseSensitive)
      return source
        .Where(x => x.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0);
    else
      return source
        .Where(x => x.Contains(input));
  }
}

}
