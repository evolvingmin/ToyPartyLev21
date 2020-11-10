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
using UnityEngine.Assertions;

namespace SeawispHunter.MinibufferConsole {

/*
  Ad Hoc Interface Trick
  ---------------------

  FieldInfo and PropertyInfo have quite a bit in common.  I wanted to
  treat them in common.  I started using a Union[1] class which is a
  type-safe tagged union. I extended it with the few properties and
  methods I need, and it seemed like a neat trick to make similar
  concrete classes into an ad hoc interface.

  [1]: http://stackoverflow.com/questions/3151702/discriminated-union-in-c-sharp

 */

public interface VariableInfo : ITaggable {
//  Variable variable { get; }
  string Name { get; }
  Type DeclaringType { get; }
  Type VariableType { get; }
  bool IsStatic { get; }
  void SetValue(Object instance, Object value);
  Object GetValue(Object instance);

  VariableInfo ToBoundVariable(object instance);
  string definedIn { get; }
  string description { get; }
  string briefDescription { get; }
  string group { get; }
}

public abstract class BaseVariableInfo {

  private Variable _variable;
  public BaseVariableInfo(Variable v) {
    _variable = v;
  }

  public Variable variable {
    get { return _variable; }
  }

  public string Name { get { return variable.name; } }

  public string description {
    get { return variable.description; }
  }

  private string _briefDescription;
  /**
     The brief description will be generated from the first sentence of the
     description if available; otherwise it's null.
  */
  public string briefDescription {
    get {
      if (_briefDescription == null)
        _briefDescription = variable.briefDescription
          ?? (variable.description != null ? CommandInfo.GenerateBriefDescription(variable.description) : null);
      return _briefDescription;
    }
  }

  private string[] _tags = null;
  public string[] tags {
    get {
      if (_tags == null)
        _tags = TagUtil.Coalesce(variable.tags, variable.tag);
      return _tags;
    }
  }
}

public class DynamicVariableInfo<T> : BaseVariableInfo, VariableInfo {
  Type t;
  Func<T> getFunc;
  Action<T> setFunc;
  public DynamicVariableInfo(Variable v, Func<T> getFunc, Action<T> setFunc) : base(v) {
    t = typeof(T);
    this.getFunc = getFunc;
    this.setFunc = setFunc;
  }

  public Type DeclaringType { get; set; }
  public Type VariableType { get { return t; } }
  public bool IsStatic { get { return true; } }
  public void SetValue(Object _, Object value) {
    setFunc((T) value);
  }
  public Object GetValue(Object _) {
    return getFunc();
  }

  private string _group;
  public string group {
    get {
      if (_group == null) {
        if (variable.group != null) {
          _group = variable.group;
        } else if (DeclaringType != null) {
          _group = Minibuffer.instance.GetGroup(DeclaringType, true).name;
          // .GetGroup(Minibuffer.instance.CanonizeCommand).name;
        } else {
          _group = Minibuffer.instance.anonymousGroup.name;
        }
      }
      Assert.IsNotNull(_group);
      return _group;
    }
  }
  public VariableInfo ToBoundVariable(object instance) {
    throw new MinibufferException("Variable is already bound.");
  }

  public override string ToString() {
    return "DynamicVariableInfo {0}".Formatted(variable.name);
  }

  public string definedIn {
    get { return variable.definedIn
        ?? (DeclaringType != null
            ? "class {0}".Formatted(DeclaringType.PrettyName())
            : "unknown"); }
  }
}

/*
  You may have dynamic variables that have multiple instances. That's when you
  use this class.
 */
public class DynamicVariableInfo<X,T> : BaseVariableInfo, VariableInfo {
  Type t;
  Func<X,T> getFunc;
  Action<X,T> setFunc;
  public DynamicVariableInfo(Variable v, Func<X,T> getFunc, Action<X,T> setFunc) : base(v) {
    t = typeof(T);
    this.getFunc = getFunc;
    this.setFunc = setFunc;
  }

  public Type DeclaringType { get { return typeof(X); } }
  public Type VariableType { get { return t; } }
  public bool IsStatic { get { return false; } } // We require an object.
  public void SetValue(Object x, Object value) {
    setFunc((X) x, (T) value);
  }
  public Object GetValue(Object x) {
    return getFunc((X) x);
  }

  private string _group;
  public string group {
    get {
      if (_group == null) {
        if (variable.group != null) {
          _group = variable.group;
        } else if (DeclaringType != null) {
          _group = Minibuffer.instance.GetGroup(DeclaringType, true).name;
          // .GetGroup(Minibuffer.instance.CanonizeCommand).name;
        } else {
          _group = Minibuffer.instance.anonymousGroup.name;
        }
      }
      Assert.IsNotNull(_group);
      return _group;
    }
  }
  public VariableInfo ToBoundVariable(object instance) {
    return new DynamicVariableInfo<T>(variable,
                                      () => getFunc((X)instance),
                                      (value) => setFunc((X)instance, value));
  }

  public string definedIn {
    get { return variable.definedIn
        ?? (DeclaringType != null
            ? "class {0}".Formatted(DeclaringType.PrettyName())
            : "unknown"); }
  }
}

public class MemberInfo : Union<FieldInfo,PropertyInfo>, VariableInfo {
  public Variable variable { get; private set; }

  public MemberInfo(Variable variable, FieldInfo fi) : base(fi) {
    this.variable = variable;
  }
  public MemberInfo(Variable variable, PropertyInfo pi) : base(pi) {
    this.variable = variable;
  }

  public Type DeclaringType {
    get { return Match(fi => fi.DeclaringType, pi => pi.DeclaringType); }
  }

  public Type VariableType {
     get { return Match(fi => fi.FieldType, pi => pi.PropertyType); }
  }

  public string Name {
    get {
      return variable.name == null
        ? Match(fi => fi.Name, pi => pi.Name) : variable.name;
    }
  }

  public virtual bool IsStatic {
    get { return Match(fi => fi.IsStatic, pi => false); }
  }

  public virtual void SetValue(Object instance, Object value) {
    /*
      Trying to return void causes the Unity Editor to crash.
      Returning an object that is null is a workaround.

    Match<void>(fi => fi.SetValue(instance, value),
                pi => pi.SetValue(instance, value, null));
    */
    Match<Object>(fi => {
        fi.SetValue(instance, value);
        return null; },
      pi => {
        pi.SetValue(instance, value, null);
        return null; });
  }

  public virtual Object GetValue(Object instance) {
    return Match<Object>(fi => fi.GetValue(instance),
                         pi => pi.GetValue(instance, null));
  }

  public override string ToString() {
    // float class.fieldname
    return string.Format("{0} {1}.{2}",
                         VariableType.PrettyName(),
                         DeclaringType.PrettyName(),
                         Match(fi => fi.Name, pi => pi.Name));
  }

  public virtual VariableInfo ToBoundVariable(object instance) {
    return Match<BoundVariableInfo>(fi => new BoundVariableInfo(variable, fi, instance),
                                    pi => new BoundVariableInfo(variable, pi, instance));
  }

  public string definedIn {
    get { return variable.definedIn
        ?? (DeclaringType != null
            ? "class {0}".Formatted(DeclaringType.PrettyName())
            : "unknown"); }
  }

  public string description {
    get { return variable.description; }
  }

  private string _group;
  public string group {
    get {
      if (_group == null) {
        if (variable.group != null) {
          _group = variable.group;
        } else if (DeclaringType != null) {
          _group = Minibuffer.instance.GetGroup(DeclaringType, true).name;
          // .GetGroup(Minibuffer.instance.CanonizeCommand).name;
        } else {
          _group = Minibuffer.instance.anonymousGroup.name;
        }
      }
      Assert.IsNotNull(_group);
      return _group;
    }
  }
  private string _briefDescription;
  /**
     The brief description will be generated from the first sentence of the
     description if available; otherwise it's null.
  */
  public string briefDescription {
    get {
      if (_briefDescription == null)
        _briefDescription = variable.briefDescription
          ?? (variable.description != null ? CommandInfo.GenerateBriefDescription(variable.description) : null);
      return _briefDescription;
    }
  }


  private string[] _tags = null;
  public string[] tags {
    get {
      if (_tags == null) {
        if (variable.tags != null)
          _tags = variable.tags;
        else if (variable.tag != null)
          _tags = new [] { variable.tag };
        else
          _tags = new string[] {};
      }
      return _tags;
    }
  }
}

public class BoundVariableInfo : MemberInfo {
  private object instance;
  public BoundVariableInfo(Variable v, FieldInfo fi, object instance) : base(v, fi) {
    this.instance = instance;
  }
  public BoundVariableInfo(Variable v, PropertyInfo pi, object instance) : base(v, pi) {
    this.instance = instance;
  }

  public override bool IsStatic {
    get { return true; }
  }

  public override void SetValue(Object _, Object value) {
    SetValue(value);
  }

  public override Object GetValue(Object _) {
    return GetValue();
  }

  public Object GetValue() {
    return base.GetValue(this.instance);
    //return GetValue(null);
  }

  public void SetValue(Object value) {
    //SetValue(null, value);
    base.SetValue(this.instance, value);
  }

  public static IEnumerable<VariableInfo>
    GetBoundVariables(VariableInfo vi) {
    if (vi.IsStatic) // We don't bind to instances.
      return vi.ToEnumerable();
    var ce = Minibuffer.instance.GetCompleterEntity(vi.DeclaringType);
    var completer = ce.completer;
    if (completer != null && ce.coercer != null) {
      return completer
        .Complete("")
        .Select(x => ce.coercer.Coerce(x, vi.DeclaringType))
        .Select(y => vi.ToBoundVariable(y));
    } else {
      return Enumerable.Empty<VariableInfo>();
    }
  }

  public override VariableInfo ToBoundVariable(object instance) {
    if (this.instance == instance)
      return this;
    else
      return base.ToBoundVariable(instance);
  }

  public override string ToString() {
    return "BoundVariableInfo {0} {1}".Formatted(variable.name, instance);
  }
}

}
