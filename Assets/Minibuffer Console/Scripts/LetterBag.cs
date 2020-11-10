/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System.Collections.Generic;
using System.Linq;
using SeawispHunter.MinibufferConsole.Extensions;

public class LetterBag {
  public List<char> chars = new List<char>();

  /*
    Given a string s, find a character c not already taken, and return
    s with its selected character emphasized, e.g., 
    `NextLetter("Save Buffer", out c) -> "S(a)ve Buffer" && c = 'a'`.
   */
  public string NextLetter(string s, out char c) {
    int i = 0;
    c = '?';
    var fromSet = "abcdefghijklmnopqrstuvwxyz"
                + "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    // While this character is unacceptable...
    while (i < s.Length
           && (char.IsWhiteSpace(s[i])
               || ! fromSet.Contains(s[i])
               || chars.Contains(c = char.ToLower(s[i]))))
      i++;
    if (i < s.Length) {
      chars.Add(c);
      return s.Substring(0, i)
        + "(" + c + ")"
        + s.Substring(i + 1, s.Length - (i + 1));
    } else {
      /* Grab any char available. */
      i = 0;
      while (chars.Contains(c = fromSet[i]))
        i++;
      return "({0}) {1}".Formatted(c, s);
    }
  }
}
