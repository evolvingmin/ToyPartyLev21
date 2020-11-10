/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RSG;

namespace SeawispHunter.MinibufferConsole {

// Maybe Decorate?  I want to add a string that's not part of the completion.

public interface ICompletionGrouper {
  /**
    Group of a completion.
  */
  string Group(string completion);
}

public interface ICache {
  void ResetCache();
}

public class CombineCompleters : ICompleter {
  private IEnumerable<ICompleter> completers;
  public CombineCompleters(IEnumerable<ICompleter> completers) {
    this.completers = completers;
  }

  public CombineCompleters(params ICompleter[] completers)
    : this((IEnumerable<ICompleter>)completers) { }

  public IEnumerable<string> Complete(string input) {
    return completers.SelectMany(c => c.Complete(input));
  }

}

public class CombineCoercers : ICoercer {
  public Type defaultType { get { return coercers.First().defaultType; } }

  private IEnumerable<ICoercer> coercers;
  public CombineCoercers(IEnumerable<ICoercer> coercers) {
    if (coercers.Any(x => x == null))
      throw new NullReferenceException();
    this.coercers = coercers;
  }

  public CombineCoercers(params ICoercer[] coercers)
    : this((IEnumerable<ICoercer>)coercers) { }

  public object Coerce(string input, Type type) {
    return coercers
      .Select(c => {
          try {
            return c.Coerce(input, type);
          } catch (CoercionException) {
            return null;
          }
        })
      .Where(x => x != null)
      .FirstOrDefault();
  }

}



/*
  An entity object with which to hang all the completion interfaces
  on.  Usage: pass an object which implements any of the interfaces.
  Or mix and match different aspects of a set of coercers however
  you like.
  */
public struct CompleterEntity {

  //public IFiller filler; // prompt, typeof(T), T ->  T
  public ICompleter completer; // string -> string[]
  public ICoercer coercer; // string -> T
  public IAnnotater annotater; // string -> string
  public ICache cache; // Action
  public ICompletionGrouper grouper; // string -> string

  public CompleterEntity(object o) {
    completer = (o as ICompleter);
    coercer = (o as ICoercer);
    annotater = (o as IAnnotater);
    cache = (o as ICache);
    grouper = (o as ICompletionGrouper);
  }

  public void Clear() {
    this.completer = null;
    this.coercer = null;
    this.annotater = null;
    this.cache = null;
    this.grouper = null;
  }

  public bool Any() {
    return this.completer != null
      || this.coercer != null
      || this.annotater != null
      || this.cache != null
      || this.grouper != null;
   }

  public bool Equals(CompleterEntity o) {
    return this.completer == o.completer
      && this.coercer == o.coercer
      && this.annotater == o.annotater
      && this.cache == o.cache
      && this.grouper == o.grouper;
   }

  public CompleterEntity Combine(CompleterEntity ce) {
    if (Equals(ce))
      return this;
    var result = this;
    if (result.completer != null && ce.completer != null)
      result.completer = new CombineCompleters(this.completer, ce.completer);
    if (result.coercer != null && ce.coercer != null)
      result.coercer = new CombineCoercers(this.coercer, ce.coercer);
    if (result.annotater == null)
      result.annotater = ce.annotater;
    if (result.cache == null)
      result.cache = ce.cache;
    if (result.grouper == null)
      result.grouper = ce.grouper;
    return result;
  }

  public override string ToString() {

    return "CompleterEntity {"
      + "  completer = " + completer + ",\n"
      + "  coercer = " + coercer + ",\n"
      + " }";
  }
}

/*
  This isn't used anywhere yet.  Just thinking about it.

public class CompleterEntity {
  public IFiller filler; // prompt, typeof(T), T ->  T
  public ICompleter completer; // string -> string[]
  public ICoercer coercer; // string -> T
  public IAnnotater annotater; // string -> string
}



public interface IFiller {
  IPromise<object> FillObject(Prompt prompt, Type t, object arg);
}

public class CommandFiller : IFiller {
  public IPromise<object> FillObject(Prompt prompt, Type t, object arg) {
    return Minibuffer.instance.ExecuteCommand((string) arg);
  }
}

*/
public interface IFiller<T> {
  IPromise<T> Fill(Prompt prompt);
}

public class CommandFiller<T> : IFiller<T> {
  string commandName;
  public CommandFiller(string commandName) {
    this.commandName = commandName;
  }

  public IPromise<T> Fill(Prompt prompt) {
    return Minibuffer.instance.ExecuteCommand(commandName)
      .Then(obj => (T) obj);
  }
}

// public class PromptResultFiller<T> : IFiller<PromptResult<T>> {
//   public IPromise<PromptResult<T>> Fill(Prompt prompt) {
//     return Minibuffer.instance.ExecuteCommand(commandName)
//       .Then(obj => (T) obj);
//   }
// }
}
