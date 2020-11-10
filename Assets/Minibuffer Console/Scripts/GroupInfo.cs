/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Linq;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

public class GroupInfo : ITaggable {
  private readonly Group group;

  public GroupInfo(Group group) {
    this.group = group;
  }

  public string name {
    get { return group.name; }
  }

  public string description {
    get { return group.description; }
  }

  private string[] _tags = null;
  public string[] tags {
    get {
      if (_tags == null)
        _tags = TagUtil.Coalesce(group.tags, group.tag);
      return _tags;
    }
  }

  public override string ToString() {
    return "GroupInfo " + name;
  }
}

}
