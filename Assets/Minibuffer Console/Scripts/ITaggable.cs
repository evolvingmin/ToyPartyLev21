/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System.Linq;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

public interface ITaggable {
  string[] tags { get; }
}

public static class TagUtil {

  /*
    For convenience many ITaggable objects often have tag and tags members.
    This method will combine them or return an empty array (but never null).
   */
  public static string[] Coalesce(string[] tags, string tag) {
    if (tags != null && tag != null)
      return tags.Append(tag).ToArray();
    else if (tags != null)
      return tags;
    else if (tag != null)
      return new [] { tag };
    else
      return new string[] {};
  }
}
}
