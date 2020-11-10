/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

public class MinibufferException : Exception {
  public MinibufferException() : base() { }
  public MinibufferException(string message) : base(message) { }
  public MinibufferException(string message, Exception inner)
    : base(message, inner) { }
}

public class MinibufferInUseException : MinibufferException {
  public MinibufferInUseException() : base() { }
  public MinibufferInUseException(string message) : base(message) { }
  public MinibufferInUseException(string message, Exception inner)
    : base(message, inner) { }
}

public class AbortException : MinibufferException {
  public AbortException() : base() { }
  public AbortException(string message) : base(message) { }
  public AbortException(string message, Exception inner)
    : base(message, inner) { }
}

public class CoercionException : MinibufferException {
  public CoercionException(Type fromType, Type toType)
    //: base(string.Format("Cannot convert from type {0} to type {1}.", fromType.PrettyName(), toType.PrettyName())) { }
  : base(string.Format("Cannot convert from type {0} to type {1}.", fromType, toType)) { }

  public CoercionException(string message) : base(message) { }

  public CoercionException(string message, Exception inner)
    : base(message, inner) { }
}

}
