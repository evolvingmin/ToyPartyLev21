
/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Linq;
using System.Collections.Generic;

namespace SeawispHunter.MinibufferConsole {
/**
   Coerce a completion string to a desired type.
*/
public interface ICoercer {

  /**
     What is the default or preferred type for this Coercer.  This
     does not mean that the Coerce method cannot produce a radically
     different type.
  */
  Type defaultType { get; }

  /**
     Coerce or lookup an input string to a particular type.

     \note Not strictly parsing or coercision because it can do
     lookups.
  */
  object Coerce(string input, Type desiredType);

  //string message { get; }
}

public class PassThruCoercer : ICoercer {
  Type t;

  public PassThruCoercer(Type t) {
    this.t = t;
  }

  public Type defaultType {
    get { return t; }
  }

  public object Coerce(string input, Type desiredType) {
    if (! desiredType.IsAssignableFrom(t)) {
      throw new CoercionException(t, desiredType);
    } else {
      return input;
    }
  }
}

public class ListCoercer : ICoercer {
  ICoercer innerCoercer;
  IEnumerable<string> list;

  public Type defaultType {
    get {
      Type generic = typeof(IEnumerable<>);
      return generic.MakeGenericType(new Type[] { innerCoercer.defaultType });
    }
  }

  public ListCoercer(ICoercer innerCoercer,
                     IEnumerable<string> list) {
    this.innerCoercer = innerCoercer;
    this.list = list;
  }

  public object Coerce(string input, Type desiredType) {
    return list.Select(x => innerCoercer.Coerce(x, desiredType));
  }

}


public interface Lookup<T> {
  T Lookup(string name);
}

/*
  I can have a graph of these coercers, and if there is a path from any Tin to
  the desired Tout then you're golden, right?

  What about loops?  Loops are probably bad.

  string -> GameObject -> string -> Transform

  A shortest path would be good.
 */
public interface ICoercer<in Tin, out Tout> {
  Tout Coerce(Tin input);
}

// public class CoercerAdapter<T> : ICoercer {
//   ICoercer<T> innerCoercer;
//   Type defaultType => typeof(T);

//   public object Coerce(string input, Type desiredType) {
//     if (desiredType == typeof(T)) {
//       innerCoercer.Coerce(input);
//     } else {
//       // Have to use reflection here. :/
//       // innerCorcer.Coerce
//     }
//   }
// }

}
