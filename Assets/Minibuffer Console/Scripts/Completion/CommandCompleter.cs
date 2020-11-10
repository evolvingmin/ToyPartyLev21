/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/** \cond HIDE */
[Group(tag = "built-in")]
public class CommandCompleter : DictCompleter<CommandInfo>, IAnnotater, ICompletionGrouper {
  // [Variable("no-instances", description = "Show commands that can't be run")]
  public bool noInstances = true;
  /* Toggles group visibility */
  public Dictionary<string, bool> groupVisible;
  public string[] hideTags = { };

  public CommandCompleter(Dictionary<string, CommandInfo> commands)
    : base(commands) {
    Minibuffer.instance.instances["CommandCompleter"] = this;
    // No commands or variables found warning.
    //Minibuffer.instance.RegisterObject(this);
    stringCoerceable = true;
    groupVisible = new Dictionary<string, bool>();
    // Minibuffer.instance.groupCreated += groupInfo => RegisterGroup(groupInfo.name);
  }


  internal void RegisterGroup(string name) {
    var g = Minibuffer.instance.GetGroup(name, true);
    // UnityEngine.Debug.Log("Registering group with completer " + name + " is " + g + ".");
    groupVisible[name] = g != null ? ! g.tags.Contains("hide-group") : true; // true;
    // Minibuffer.instance
    // .RegisterVariable<bool>(new Variable(name)
    //                           { description = "Show group '{0}'".Formatted(name)},
    //                         () => groupVisible[name],
    //                         (value) => groupVisible[name] = value,
    //                         this.GetType());
    Minibuffer.instance
      .RegisterVariable<CommandCompleter, bool>(new Variable(name)
                                                  { description = "Show group '{0}'".Formatted(name)},
                                                (x) => x.groupVisible[name],
                                                (x, value) => x.groupVisible[name] = value);
  }

  public string Annotate(string completion) {
    CommandInfo ci;
    if (dict.TryGetValue(completion, out ci)) {
      return ci.briefDescription ?? "";
    } else {
      return "";
    }
  }

  public string Group(string completion) {
    CommandInfo ci;
    if (dict.TryGetValue(completion, out ci)) {
      return ci.group;
    } else {
      return "";
    }
  }

  public override IEnumerable<string> Complete(string input) {
    if (groupVisible.Values.All(x => x) && hideTags.Length == 0) {
      // We're not filtering based on anything.
      // XXX Assumes that filters should always be set to true
      // in their default non-filtering state.
      return base.Complete(input);
    } else {
      var rejects = new HashSet<string>(groupVisible
                                        .Where(kv => ! kv.Value)
                                        .Select(kv => kv.Key));
      var source = dict
        .Where(kv => ! rejects.Contains(kv.Value.group));
      if (! noInstances)
        source = source.
          Where(kv => kv.Value.instanceExists);
      if (hideTags.Length != 0)
        source = source
          .Where(kv =>
                 ! hideTags.Any(tag => kv.Value.tags.Contains(tag)));
      return Matcher.Match(input, source.Select(x => x.Key));
    }
  }

  public override object Coerce(string input, Type type) {
    CommandInfo ci;
    if (dict.TryGetValue(input, out ci)) {
      if (type == typeof(string))
        return input;
      else if (type.IsAssignableFrom(typeof(CommandInfo)))
        return ci;
      else if (type == typeof(MethodInfo))
        return ci.methodInfo;
      else
        throw new CoercionException(defaultType, type);
    } else
        return null;
  }
}

/** \endcond HIDE */
}
