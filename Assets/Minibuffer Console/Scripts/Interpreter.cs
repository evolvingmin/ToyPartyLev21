/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RSG;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/*
  Minibuffer essentially has a kind of user driven eval/apply
  interpreter when it evaluates a command.

define eval(expr, environment):
   if is_literal(expr): return literal_value(expr)
   if is_symbol(expr):  return lookup_symbol(expr, environment)
   ;; other similar cases here
   ;; remaining (and commonest) case: function application
   function  = extract_function(expr)
   arguments = extract_arguments(expr)
   apply(eval(function, environment), eval_list(arguments, environment))

 define apply(function, arguments):
   if is_primitive(function): return apply_primitive(function, arguments)
   environment = augment(function_environment(function),
                         formal_args(function), arguments)
   return eval(function_body(function), environment)

 def eval_list(items, environment):
   return map( { x -> eval(x, environment) }, items)

Commands are essentially our primitive functions.  Instances and
parameters can be thought of as symbol lookups or determined by user
selection.

  http://c2.com/cgi/wiki?EvalApply
*/
public class Interpreter {
  private Minibuffer minibuffer;
  public struct Applied {
    public MethodInfo methodInfo;
    public object instance;
    public object[] arguments;
  }
  internal Applied lastApplied;

  public Interpreter(Minibuffer minibuffer) {
    this.minibuffer = minibuffer;
  }

  // public abstract object LookupSymbol(string name);

  // public abstract object LookupByType(Type t);

  public IPromise<object> Apply(Delegate delegate_,
                                string[] parameterNames = null,
                                Prompt[] prompts = null) {
    return Apply(delegate_.Method, delegate_.Target, parameterNames, prompts);
  }

  public IPromise<object> Apply(MethodInfo methodInfo,
                                object givenInstance = null,
                                string[] parameterNames = null,
                                Prompt[] prompts = null) {
    bool makePromises = false;
    //bool makePromises = true;
    object instance = null;
    var nearPromises = new List<Func<IPromise<object>>>();
    if (givenInstance != null) {
      instance = givenInstance;
      nearPromises.Add(() => Promise<object>.Resolved(instance));
    } else if (methodInfo.IsStatic) {
      instance = null;
      nearPromises.Add(() => Promise<object>.Resolved(null));
    } else {
      var p = FillInstance(methodInfo.DeclaringType, false);
      if (p == null) {
        makePromises = true;
        nearPromises.Add(() => FillInstance(methodInfo.DeclaringType));
      } else {
        // The promise should be resolved already.
        p.Then(o => instance = o);
        nearPromises.Add(() => p);
      }
    }

    ParameterInfo[] theParams = methodInfo.GetParameters();
    object[] parameters = new object[theParams.Length];
    int i = 0;
    if (methodInfo.IsStatic && givenInstance != null) {
      // We have a first parameter.
      parameters[0] = givenInstance;
      nearPromises.Add(() => Promise<object>.Resolved(givenInstance));
      i = 1;
    }
    // Do we need to make promises in general?  Yes, if we have
    // non-optional parameters.
    makePromises = makePromises || theParams.Where(x => !x.IsOptional).Any();
    for (; i < parameters.Length; ++i) {
      bool hasPromptValue = (prompts != null && prompts[i] != null && prompts[i].defaultValue != null);
      if (hasPromptValue || theParams[i].IsOptional) {
        var value = hasPromptValue ? prompts[i].defaultValue
                                   : theParams[i].DefaultValue;
        if (makePromises)
          nearPromises.Add(() => Promise<object>.Resolved(value));
        else
          parameters[i] = value;
      } else {
        var myParam = theParams[i];
        var myParamName = parameterNames == null ? null : parameterNames[i];
        var myPrompt = prompts == null ? null : prompts[i];
        nearPromises.Add(() => FillParam(myPrompt, myParam, myParamName));
      }
    }
    if (makePromises) {
      // XXX I have two execution paths. I should just have one.
      // They do the same thing. I'm just optimizing.
      // Debug.Log("making big promise with " + nearPromises.Count + " promises");
      return Promise
        .Sequence<object>(nearPromises)
        .Then((objects) =>
            {
              var q = objects.ToQueue();
              var inst = q.Dequeue();
              var args = q.ToArray();
              //print("invoking " + command);
              try {
                lastApplied.methodInfo = methodInfo;
                lastApplied.instance = inst;
                lastApplied.arguments = args;
                object result = methodInfo.Invoke(inst,
                                                  args);
                return result; //object
              } catch (TargetInvocationException tie) {
                Debug.Log("Command invocation exception thrown.");
                Debug.LogException(tie.InnerException);
                throw tie;
              } catch (Exception ex) {
                throw ex;
              }
            })
        .Catch(exception => {
            if (exception is AbortException) {
              Debug.Log("Minibuffer aborted.");
            } else {
              Debug.Log("Command invocation exception thrown.");
              Debug.Log("An exception occured while getting params! "
                        + exception.Message);
              Debug.LogException(exception);
              minibuffer.Message("Error: {0}", exception.Message);
            }
          });
    } else {
      // Debug.Log("running now ");
      try {
        lastApplied.methodInfo = methodInfo;
        lastApplied.instance = instance;
        lastApplied.arguments = parameters;
        var result = methodInfo.Invoke(instance,
                                       parameters);
        //print("invoking " + command);

        //minibuffer.PostCommand(command);
        return Promise<object>.Resolved(result);
      } catch (TargetInvocationException tie) {
        Debug.Log("Command invocation exception thrown.");
        Debug.LogException(tie.InnerException);
        return Promise<object>.Rejected(tie.InnerException);
      }
    }
  }

  /*

   */
  internal IPromise<object> FillInstance(System.Type type,
                                         bool fillType = true) {
    var name = type.PrettyName();
    var ce = minibuffer.GetCompleterEntity(type);
    var completer = ce.completer;
    List<string> list;
    if (minibuffer.instances.ContainsKey(name)) {
      // This is like a lookup_symbol.
      return Promise<object>.Resolved(minibuffer.instances[name]);
    } else if (completer != null
               //&& completer as ICoercer != null
               && (list = completer.Complete("").ToList()).Count == 1
               && ce.coercer != null) {
      // If there's just one completion, provide that.
      //Debug.Log("FillInstance for " + name + " auto resolved.");
      return Promise<object>.Resolved(ce.coercer.Coerce(list[0], type));
    } else if (type == typeof(Minibuffer)) {
      // This is like a lookup_by_type.
      return Promise<object>.Resolved(this);
    } else if (type == typeof(InputField)) {
      return Promise<object>.Resolved(minibuffer.gui.input);
    // } else if (type == typeof(TappedInputField)) {
    //     // Might want to have a focus that actually moves around.
    //   return Promise<object>.Resolved(minibuffer.gui.minibufferPrompt);
    } else if (typeof(IBuffer).IsAssignableFrom(type)) {
      return Promise<object>.Resolved(minibuffer.gui.main.buffer);
    } else if (type == typeof(Window)) {
      // This is like a primitive_function.
      return Promise<object>.Resolved(minibuffer.showAutocomplete
                                      ? minibuffer.gui.autocomplete
                                      : minibuffer.gui.main);
    } else {
      var prompt = new PromptInfo();
      prompt.prompt = "Instance " + name + ": ";

      return fillType ? FillType(prompt, type) : null;
    }
  }

  public IPromise<object> FillParam(Prompt promptSettings, ParameterInfo paramInfo, string paramName = null) {
    PromptInfo prompt;
    object [] attrs;
    if (promptSettings == null) {
      attrs = paramInfo.GetCustomAttributes (typeof(Prompt), false);
      prompt = PromptInfo.CopyOrNew((Prompt) attrs.FirstOrDefault());
    } else {
      prompt = new PromptInfo(promptSettings);
    }
    if (prompt.prompt == null) {
      prompt.prompt = paramInfo.ParameterType.PrettyName() + " "
        + (paramName ?? paramInfo.Name) + ": ";
    }
    attrs = paramInfo.GetCustomAttributes(typeof(UniversalArgument), false);
    if (attrs.Any()) {
      if (paramInfo.ParameterType == typeof(int?)) {
        return Promise<object>.Resolved(minibuffer.currentPrefixArg);
      } else if (paramInfo.ParameterType == typeof(int)) {
        return Promise<object>.Resolved(minibuffer.currentPrefixArg.HasValue
                                        ? minibuffer.currentPrefixArg
                                        : 1); // default value of 0 or 1?
      } else if (paramInfo.ParameterType == typeof(bool)) {
        return Promise<object>.Resolved(minibuffer.currentPrefixArg.HasValue);
      } else {
        return Promise<object>
          .Rejected(new MinibufferException(
            "UniversalArgument can only be int, int?, or bool not "
            + paramInfo.ParameterType.PrettyName()));
      }
    }
    attrs = paramInfo.GetCustomAttributes(typeof(Current), false);
    if (attrs.Any()) {
      var currentAttr = (Current) attrs.First();
      ICurrentProvider provider = null;
      foreach(var cp in Minibuffer.instance.currentProviders) {
        if (cp.CanProvideType(paramInfo.ParameterType)) {
          if (provider == null) {
            provider = cp;
          } else {
            return Promise<object>
              .Rejected(new MinibufferException(
                 "Ambiguity. Multiple ICurrentProviders can provide type"
                 + paramInfo.ParameterType.PrettyName()
                 + ". In this case both "
                 + cp.canonicalType.PrettyName()
                 + " and "
                 + provider.canonicalType.PrettyName() + "."));
          }
        }
      }

      if (provider != null) {
        var obj = provider.CurrentObject();
        if (obj != null
            || (obj == null && currentAttr.acceptNull))
          return Promise<object>.Resolved(obj);
        else {
          return Promise<object>.Rejected(new MinibufferException(
            "The parameter [Current] {0} {1} can not be null."
            .Formatted(paramInfo.ParameterType.PrettyName(),
                       paramInfo.Name)));
        }
      } else {
        return Promise<object>
          .Rejected(new MinibufferException(
             "No ICurrentProvider found for type "
             + paramInfo.ParameterType.PrettyName()
             + "; these are the registered types: "
             + String.Join(", ",
                           Minibuffer.instance.currentProviders
                             .Select(c => c.canonicalType.PrettyName())
                             .ToArray()) + "."));
      }
    }
    return FillType(prompt, paramInfo.ParameterType);
  }

  internal IPromise<object> FillField(FieldInfo fieldInfo) {
    // XXX Since when do fields have any Prompt attributes?
    object[] attrs = fieldInfo.GetCustomAttributes(typeof(Prompt), false);
    PromptInfo prompt = PromptInfo.CopyOrNew((Prompt) attrs.FirstOrDefault());
    if (prompt.prompt == null)
      prompt.prompt = fieldInfo.FieldType.PrettyName() + " "
        + fieldInfo.Name + ": ";

    return FillType(prompt, fieldInfo.FieldType);
  }

  public PromptInfo PromptForParameter(Prompt promptSettings, ParameterInfo paramInfo, string paramName = null) {
    PromptInfo prompt;
    object [] attrs;
    if (promptSettings == null) {
      attrs = paramInfo.GetCustomAttributes (typeof(Prompt), false);
      prompt = PromptInfo.CopyOrNew((Prompt) attrs.FirstOrDefault());
    } else {
      prompt = new PromptInfo(promptSettings);
    }
    if (prompt.prompt == null) {
      prompt.prompt = paramInfo.ParameterType.PrettyName() + " "
        + (paramName ?? paramInfo.Name) + ": ";
    }
    attrs = paramInfo.GetCustomAttributes(typeof(UniversalArgument), false);
    if (attrs.Any()) {
      if (paramInfo.ParameterType == typeof(int?)) {
        prompt.defaultValue = minibuffer.currentPrefixArg;
      } else if (paramInfo.ParameterType == typeof(int)) {
        prompt.defaultValue = minibuffer.currentPrefixArg.HasValue
          ? minibuffer.currentPrefixArg
          : 1;
      } else if (paramInfo.ParameterType == typeof(bool)) {
        prompt.defaultValue = minibuffer.currentPrefixArg.HasValue;
      } else {
        throw new MinibufferException(
            "UniversalArgument can only be int, int?, or bool not "
            + paramInfo.ParameterType.PrettyName());
      }
    }
    attrs = paramInfo.GetCustomAttributes(typeof(Current), false);
    if (attrs.Any()) {
      var currentAttr = (Current) attrs.First();
      ICurrentProvider provider = null;
      foreach(var cp in Minibuffer.instance.currentProviders) {
        if (cp.CanProvideType(paramInfo.ParameterType)) {
          if (provider == null) {
            provider = cp;
          } else {
           throw new MinibufferException(
                 "Ambiguity. Multiple ICurrentProviders can provide type"
                 + paramInfo.ParameterType.PrettyName()
                 + ". In this case both "
                 + cp.canonicalType.PrettyName()
                 + " and "
                 + provider.canonicalType.PrettyName() + ".");
          }
        }
      }

      if (provider != null) {
        var obj = provider.CurrentObject();
        if (obj != null
            || (obj == null && currentAttr.acceptNull))
          prompt.defaultValue = obj;
        else {
          throw new MinibufferException(
            "The parameter [Current] {0} {1} can not be null."
            .Formatted(paramInfo.ParameterType.PrettyName(),
                       paramInfo.Name));
        }
      } else {
        throw new MinibufferException(
             "No ICurrentProvider found for type "
             + paramInfo.ParameterType.PrettyName()
             + "; these are the registered types: "
             + String.Join(", ",
                           Minibuffer.instance.currentProviders
                             .Select(c => c.canonicalType.PrettyName())
                             .ToArray()) + ".");
      }
    }
    return PromptForType(prompt, paramInfo.ParameterType);
  }

  /*
    This is used for CLI prompts, I think.

    The things that are not supported would require some kind of syntax.
  */
  public PromptInfo PromptForType(PromptInfo prompt, Type t) {
    // Debug.Log("PromptForType");
    Type promptResultType = null;
    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PromptResult<>)) {
      promptResultType = t;
      t = promptResultType.GetGenericArguments()[0];
    } else if (t == typeof(PromptResult)) {
      promptResultType = t;
      t = typeof(object);
    }
    prompt.desiredType = t;
    if (prompt.ignore) {
      // Remove nulls.
      return null;
    } else if (prompt.filler == "keybinding") {
      throw new MinibufferException("keybinding not supported.");
    } else if (t == typeof(bool)) {
      prompt.completions = new [] { "true", "false" };
      // XXX Need a coercer.
      // Is this prompt good now?
      return prompt;
    } else if (t.IsArray) {
      // Handle arrays
      // [1, 2, 3]
      throw new MinibufferException("Arrays not supported.");
    } else if (t.IsGenericType
               && t.GetGenericTypeDefinition() == typeof(List<>)) {
      // Handle lists
      // [1, 2, 3]
      throw new MinibufferException("Lists not supported.");
    } else if (t == typeof(Vector3)) {
      // XXX Should have a better way of dealing with generic constructors.
      // (1, 2, 3)
      throw new MinibufferException("Vector3 not supported.");
    } else if (t == typeof(Vector2)) {
      throw new MinibufferException("Vector2 not supported.");
    } else if (t == typeof(Vector4)) {
      // ExecuteCommand can turn any command into a "filler".

      // Eval("MakeVector4")
      throw new MinibufferException("Vector4 not supported.");
    } else if (t == typeof(Quaternion)) {
      throw new MinibufferException("Quaternion not supported.");
    } else if (t == typeof(Matrix4x4)) {
      throw new MinibufferException("Quaternion not supported.");
    // } else if (t == typeof(InputField) || t == typeof(TappedInputField)) {
    //   return Promise<object>.Resolved(minibuffer.gui.input);
    } else {
      // Handle whatever
      // var promise = new Promise<object>();
      string msg;
      if (! ResolveCompleters(t, prompt, out msg)) {
        throw new MinibufferException(msg);
      }
    }
    return prompt;
  }

  public IPromise<PromptResult<T>> ConvertPromptResult<T>(IPromise<PromptResult> p) {
    var t = typeof(T);
    // var p = minibuffer.MinibufferEdit(prompt);
    return p.Then((PromptResult promptResult) => {
        string input = promptResult.str;
        object obj = promptResult.obj;
        // We were asked for a PromptResult<T>, give them back
        // that.
        var pr = new PromptResult<T>();
        pr.str = input;
        Assert.IsNotNull(t);
        // Assert.IsNotNull(obj);
        if (obj != null
            && t.IsAssignableFrom(obj.GetType()))
          pr.obj = (T) obj;
        // else if (t == typeof(string))
        //   pr.obj = (T) (object) input;
        // pr.obj = (T) (obj != null && t.IsAssignableFrom(obj.GetType())
        //               ? obj
        //               : null);
        // Debug.Log("input = " + input + " obj " + obj);
        if (obj != null && pr.obj == null) {
          // if (t.IsAssignableFrom(obj.GetType()) || t == typeof(string)) {
          //   return pr;
          // } else {
            var msg = string.Format("Unable to assign expected type {0} to given type {1}.",
                                    t.PrettyName(),
                                    obj.GetType().PrettyName());
            throw new MinibufferException(msg);
          // }
        } else {
          return pr;
        }
      });
  }

  /*
    We might have to do a bunch of dirty reflection to do this, but that doesn't
    mean we have to resort to having a bunch of untyped objects internally.
    Bleh. F--- that.
   */
  public IPromise<object> FillType(PromptInfo prompt, Type t) {
    var genericMethod = this.GetType().GetMethod("Fill");
    var method = genericMethod.MakeGenericMethod(t);
    object promise = method.Invoke(this, new object[] { prompt });

    var genericMethod2 = this.GetType().GetMethod("Convert");
    var method2 = genericMethod2.MakeGenericMethod(t);
    return (IPromise<object>) method2.Invoke(this, new object[] { promise });
    // object promise;
    // if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PromptResult<>)) {
    //   var innerType = t.GetGenericArguments()[0];
    //   // promise = FillPromptResult<t>(prompt);
    //   var genericMethod = this.GetType().GetMethod("FillPromptResultT");
    //   var method = genericMethod.MakeGenericMethod(innerType);
    //   promise = method.Invoke(this, new object[] { prompt });
    // } else if (t == typeof(PromptResult)) {
    //   promise = minibuffer.MinibufferEdit(prompt);
    // } else {
    //   // XXX It's a lot of Reflection here. Ugh.
    //   var genericMethod = this.GetType().GetMethod("Fill");
    //   var method = genericMethod.MakeGenericMethod(t);
    //   // object pvar = method.Invoke(this, new object[] { prompt });
    //   promise = method.Invoke(this, new object[] { prompt });
    // }
    // var genericMethod2 = this.GetType().GetMethod("Convert");
    // var method2 = genericMethod2.MakeGenericMethod(t);
    // return (IPromise<object>) method2.Invoke(this, new object[] { promise });

    // var genericPromise = typeof(IPromise<>);
    // var promiseType = genericPromise.MakeGenericType(t);
    // promiseType.GetMethod("Then", new Type[] { });
  }

  [Preserve]
  public IPromise<object> Convert<T>(IPromise<T> promise) {
    return promise.Then((T x) => (object) x);
  }

  public bool ResolveCompleters(Type t, PromptInfo prompt, out string msg) {
    msg = null;
    // var t = typeof(T);
    if (prompt.completerEntity.Any()) {
      // This takes precedence.
    } else if (prompt.completions != null && prompt.completer != null) {
      // Combine our completers.
      CompleterEntity ce;
      if (minibuffer.completers.TryGetValue(prompt.completer, out ce)) {
        prompt.completerEntity
          = new ListCompleter(prompt.completions).ToEntity().Combine(ce);
      } else {
        prompt.completerEntity
          = new ListCompleter(prompt.completions).ToEntity();
        minibuffer.MessageAlert("No such completer '{0}'.", prompt.completer);
      }
    } else if (prompt.completions != null) {
      prompt.completerEntity
        = new ListCompleter(prompt.completions).ToEntity();
    } else if (prompt.completer != null) {
      CompleterEntity ce;
      if (minibuffer.completers.TryGetValue(prompt.completer, out ce)) {
        prompt.completerEntity = ce;
      } else {
        // Maybe it's a dynamically generated completer.
        prompt.completerEntity = minibuffer.GetCompleterEntity(t, true);
        if (! prompt.completerEntity.Any()) {
          msg = "No such completer '{0}' for type {1}.".Formatted(prompt.completer, t.PrettyName());
          return false;
        }
      }
    }
    if (! prompt.completerEntity.Any()) {
      prompt.completerEntity = minibuffer.GetCompleterEntity(t);
    }
    if (prompt.history == null)
      prompt.history = t.PrettyName();
    if (t.IsNumericType()) {
      prompt.requireCoerce = true;
      prompt.requireMatch = false;
    }

    if (! PromptInfo.IsPassThru(t)) {
      if (prompt.requireCoerce && prompt.completerEntity.coercer == null) {
        msg = string.Format("No coercer for type {0}.", t.PrettyName());
        return false;
      }
    } else {
      // We want a string, if there's no coercer then we want a
      // coercer that just passes the string through.
      // Debug.Log("checking for type");
      if (prompt.completerEntity.coercer == null) {
        // Debug.Log("setting pass thru");
        var ce = prompt.completerEntity;
        ce.coercer = new PassThruCoercer(t);
        prompt.completerEntity = ce;
      }
    }
    prompt.desiredType = t;
    return true;
  }

  [Preserve]
  public IPromise<PromptResult> FillPromptResult(PromptInfo prompt, Type t) {
    string msg;
    if (! ResolveCompleters(t, prompt, out msg))
      return Promise<PromptResult>.Rejected(new MinibufferException(msg));
    return minibuffer.MinibufferEdit(prompt);
  }

  public object FillPromptResultGeneric(PromptInfo prompt, Type t) {
    var genericMethod = this.GetType().GetMethod("FillPromptResultGenericT");
    var method = genericMethod.MakeGenericMethod(t);
    return method.Invoke(this, new object[] { prompt });
  }

  [Preserve]
  public IPromise<PromptResult<T>> FillPromptResultGenericT<T>(PromptInfo prompt) {
    IPromise<PromptResult> p = FillPromptResult(prompt, typeof(T));
    return ConvertPromptResult<T>(p);
  }

  bool IsToggleable(Type t, PromptInfo prompt) {
    string msg;
    var promptCopy = new PromptInfo(prompt);
    bool toggleable
      = ResolveCompleters(t, promptCopy, out msg)
      && promptCopy.completerEntity.completer != null;
    return toggleable;
  }

  [Preserve]
  public IPromise<T> Fill<T>(PromptInfo prompt) {
    Type t = typeof(T);
    // Debug.Log("Fill<" + t.PrettyName() + ">()");
    // Type promptResultType = null;
    // if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PromptResult<>)) {
    //   promptResultType = t;
    //   t = promptResultType.GetGenericArguments()[0];
    // } else if (t == typeof(PromptResult)) {
    //   promptResultType = t;
    //   t = typeof(object);
    // }

    // Dictionary<Type, IFiller> dict = null;
    // return dict[typeof(T)].Fill(prompt);
    prompt.desiredType = t;
    if (prompt.ignore) {
      return Promise<T>.Resolved(default(T));
    } else if (prompt.filler == "keybinding") {
      return (IPromise<T>) FillKeyBinding(prompt);
    } else if (t == typeof(bool)) {
      // Handle booleans.
      return (IPromise<T>) minibuffer.ReadTrueOrFalse(prompt.prompt);
    } else if (t.IsArray) {
      // Handle arrays.
      var promptp = new PromptInfo(prompt);
      if (promptp.prompt == null)
        promptp.prompt = t.PrettyName() + ": ";
      bool toggleable = IsToggleable(t.GetElementType(), prompt);
      return (IPromise<T>) FillArray(promptp, t.GetElementType(), toggleable);
    } else if (t.IsGenericType
               && t.GetGenericTypeDefinition() == typeof(List<>)) {
      // Handle lists.
      var promptp = new PromptInfo(prompt);
      if (promptp.prompt == null)
        promptp.prompt = t.PrettyName() + ": ";
      bool toggleable = IsToggleable(t.GetGenericArguments()[0], prompt);
      return (IPromise<T>) FillList(promptp, t.GetGenericArguments()[0], toggleable);
    } else if (t.IsGenericType
               && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
      // Handle IEnumerable.
      var promptp = new PromptInfo(prompt);
      if (promptp.prompt == null)
        promptp.prompt = t.PrettyName() + ": ";
      bool toggleable = IsToggleable(t.GetGenericArguments()[0], prompt);
      return (IPromise<T>) FillCollection(promptp, t.GetGenericArguments()[0], toggleable);
    } else if (t == typeof(Vector3)) {
      // XXX Should have a better way of dealing with generic constructors.
      return (IPromise<T>) FillVector3(prompt);
    } else if (t == typeof(Vector2)) {
      return (IPromise<T>) FillVector2(prompt);
    } else if (t == typeof(Vector4)) {
      // ExecuteCommand can turn any command into a "filler".
      // Eval("MakeVector4")
      return minibuffer.ExecuteCommand<T>("MakeVector4");
    } else if (t == typeof(Quaternion)) {
      return minibuffer.ExecuteCommand<T>("MakeQuaternion");
    } else if (t == typeof(Matrix4x4)) {
      return (IPromise<T>) FillMatrix4x4(prompt);
    // } else if (t == typeof(InputField) || t == typeof(TappedInputField)) {
    //   return Promise<object>.Resolved(minibuffer.gui.input);
    } else {
      // Handle whatever.
      if (t == typeof(PromptResult)) {
        prompt.requireCoerce = false;
        return (IPromise<T>) FillPromptResult(prompt, typeof(object));
      } else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PromptResult<>)) {
        var promisedType = t.GetGenericArguments()[0];
        return (IPromise<T>) FillPromptResultGeneric(prompt, promisedType);
      } else {
        var p = FillPromptResultGenericT<T>(prompt);
        return p.Then((PromptResult<T> pr) => pr.obj);
      }


    }
  }

  /*
    Fill a List (not a List<T>) with objects of the given Type T. This
    can be then used to create Arrays (T[]) or Lists (List<T>) or
    other composite types like Vector3.
   */
  [Preserve]
  public IPromise<IEnumerable<T>>
    FillObjectList<T>(PromptInfo prompt, int? elementCount = null) {
    var promise = new Promise<IEnumerable<T>>();
    var elementType = typeof(T);
    if (prompt.prompt == null)
      prompt.prompt = elementType.PrettyName();
    IPromise<int> promisedCount;
    if (! elementCount.HasValue) {
      promisedCount = minibuffer.Read<int>(new PromptInfo(prompt)
          { prompt = "Count of " + prompt.prompt });
    } else
      promisedCount = Promise<int>.Resolved((int) elementCount);
    promisedCount
      .Then(count => {
          // We got a count.
          var promises = new List<Func<IPromise<T>>>();
          for (int i = 0; i < count; i++) {
            var j = i;
            promises.Add(() => Fill<T>(new PromptInfo(prompt) {
                  prompt = string.Format("Item {0} of {1} for {2}",
                                         j, count, prompt.prompt)
                }));
          }
          Promise
          .Sequence<T>(promises)
          .Then(objects => promise.Resolve(objects))
          .Catch(ex => promise.Reject(ex));
        })
      .Catch(ex => promise.Reject(ex));
    return promise;
  }

  // https://github.com/modesttree/Zenject/issues/219
  // https://docs.unity3d.com/560/Documentation/Manual/ScriptingRestrictions.html
  /*
    In order to work in an Ahead Of Time (AOT) Compilation, we have to specify which
    types we will be instantiating.
   */
  private static void _AotWorkAround() {
    // C# types
    _AotWorkAround<int>();
    _AotWorkAround<bool>();
    _AotWorkAround<float>();
    _AotWorkAround<double>();
    _AotWorkAround<object>();
    _AotWorkAround<string>();

    _AotWorkAround<int?>();
    _AotWorkAround<bool?>();
    _AotWorkAround<float?>();
    _AotWorkAround<double?>();

    // Minibuffer types
    _AotWorkAround<Current>();
    _AotWorkAround<CommandInfo>();
    _AotWorkAround<IBuffer>();
    _AotWorkAround<PromptResult>();
    _AotWorkAround<VariableInfo>();
    _AotWorkAround<TappedInputField>();
    _AotWorkAround<Minibuffer.KeyBindingHint>();
    _AotWorkAround<Minibuffer.Notation>();
    _AotWorkAround<Matchers>();
    _AotWorkAround<ChangeCase.Case>();

    // Unity types
    _AotWorkAround<AnimationClip>();
    _AotWorkAround<AudioClip>();
    _AotWorkAround<Component>();
    _AotWorkAround<Color>();
    _AotWorkAround<Font>();
    _AotWorkAround<GameObject>();
    _AotWorkAround<GUISkin>();
    _AotWorkAround<Material>();
    _AotWorkAround<Mesh>();
    _AotWorkAround<PhysicMaterial>();
    _AotWorkAround<Scene>();
    _AotWorkAround<Shader>();
    _AotWorkAround<Sprite>();
    _AotWorkAround<Texture>();
    _AotWorkAround<Transform>();
    _AotWorkAround<Quaternion>();
    _AotWorkAround<Vector2>();
    _AotWorkAround<Vector3>();
    _AotWorkAround<Vector4>();
    _AotWorkAround<Matrix4x4>();
    _AotWorkAround<InputField>();
  }

  public static void _AotWorkAround<T>() {
    // Turn off warning, "CS0219: The variable `p1' is assigned but its value is never used."
    #pragma warning disable 0219

    var prompt = new PromptInfo();
    var i = new Interpreter(null);
    {
      var p1 = i.FillCollectionT<T>(prompt, true);
      var p2 = i.FillArrayT<T>(prompt, true);
      var p3 = i.FillListT<T>(prompt, true);
      var p4 = i.FillObjectList<T>(prompt);
      var p5 = i.FillIEnumerableT<T>(prompt);
      var p6 = i.Fill<T>(prompt);
      var p7 = i.FillPromptResultGenericT<T>(prompt);
      var p8 = i.Convert<T>(Promise<T>.Resolved(default(T)));
      var p9 = i.ConvertPromptResult<T>(null);
      var p10 = new Promise<T>().Then((T x) => x);
    }

    {
      var p0 = new PromptResult<T>();
      var p1 = i.FillCollectionT<PromptResult<T>>(prompt, true);
      var p2 = i.FillArrayT<PromptResult<T>>(prompt, true);
      var p3 = i.FillListT<PromptResult<T>>(prompt, true);
      var p4 = i.FillObjectList<PromptResult<T>>(prompt);
      var p5 = i.FillIEnumerableT<PromptResult<T>>(prompt);
      var p6 = i.Fill<PromptResult<T>>(prompt);
      var p7 = i.FillPromptResultGenericT<PromptResult<T>>(prompt);
      var p8 = i.Convert<PromptResult<T>>(Promise<PromptResult<T>>.Resolved(default(PromptResult<T>)));
      var p9 = i.ConvertPromptResult<PromptResult<T>>(null);
      var p10 = new Promise<PromptResult<T>>().Then((PromptResult<T> x) => x);
    }
    throw new InvalidOperationException
      ("This method is used for AOT code "
       + "generation only. Do not call at runtime.");
    #pragma warning restore 0219
  }

  [Preserve]
  public IPromise<IEnumerable<T>> FillCollectionT<T>(PromptInfo prompt, bool toggleable) {
    return toggleable
      ? FillIEnumerableT<T>(prompt)
      : FillObjectList<T>(prompt);
  }

  public object FillCollection(PromptInfo prompt, Type t, bool toggleable) {
    var genericMethod = this.GetType().GetMethod("FillCollectionT");
    var method = genericMethod.MakeGenericMethod(t);
    return method.Invoke(this, new object[] { prompt, toggleable });
  }

  [Preserve]
  public IPromise<T[]> FillArrayT<T>(PromptInfo prompt, bool toggleable) {
    return (toggleable ? FillIEnumerableT<T>(prompt) : FillObjectList<T>(prompt))
      .Then(objects => objects.ToArray());
  }

  public object FillIEnumerable(PromptInfo prompt, Type t, bool toggleable) {
    var genericMethod = this.GetType().GetMethod("FillIEnumerableT");
    var method = genericMethod.MakeGenericMethod(t);
    return method.Invoke(this, new object[] { prompt });
  }

  [Preserve]
  public IPromise<IEnumerable<T>> FillIEnumerableT<T>(PromptInfo prompt) {
    if (prompt.prompt == null)
      prompt.prompt = typeof(T).PrettyName();
    return minibuffer.ReadSet<T>(prompt);
    // var dict = new Dictionary<string, bool>();
    // ICoercer coercer = null;
    // var p = Fill<T>(prompt)
    //   .Then(_ => {
    //   return (IEnumerable<T>)
    //           dict
    //           .Where(kv => kv.Value)
    //           .Select(kv => kv.Key)
    //           .Select(name => coercer.Coerce(name, typeof(T)))
    //           .Cast<T>()
    //           .ToList();
    //     });
    // minibuffer.editState.isValidInput = _ => true; // All input is valid.
    // minibuffer.editState.toggleGetter
    //   = name => { bool value;
    //               return dict.TryGetValue(name, out value) ? value : false; };
    // minibuffer.editState.toggleSetter = (name, value) => dict[name] = value;
    // minibuffer.TabComplete();
    // var ce = minibuffer.editState.prompt.completerEntity;
    // coercer = ce.coercer;
    // return p;
  }

  public object FillArray(PromptInfo prompt, Type t, bool toggleable) {
    var genericMethod = this.GetType().GetMethod("FillArrayT");
    var method = genericMethod.MakeGenericMethod(t);
    return method.Invoke(this, new object[] { prompt, toggleable });

    // var elementType = t;
    // return FillObjectList(prompt, t)
    //   .Then((objects) => {
    //       var list = objects.ToList();
    //       var a = Array.CreateInstance(elementType, list.Count);
    //       Array arr = (Array) a;
    //       for (int i = 0; i < list.Count; i++)
    //         arr.SetValue(list[i], i);
    //       return (object) a;
    //     });
  }

  [Preserve]
  public IPromise<List<T>> FillListT<T>(PromptInfo prompt, bool toggleable) {
    return (toggleable ? FillIEnumerableT<T>(prompt) : FillObjectList<T>(prompt))
      .Then(objects => objects.ToList());
  }

  public object FillList(PromptInfo prompt, Type t, bool toggleable) {
    var genericMethod = this.GetType().GetMethod("FillListT");
    var method = genericMethod.MakeGenericMethod(t);
    return method.Invoke(this, new object[] { prompt, toggleable });
  }

  public IPromise<Vector3> FillVector3(PromptInfo prompt) {
    // XXX Prompt is totally ignored here.
    return minibuffer.ExecuteCommand<Vector3>("MakeVector3");
    // return FillObjectList(prompt, typeof(float), 3)
    //   .Then(objects =>
    //       {
    //         var list = objects.ToList();
    //         return new Vector3((float) list[0], (float) list[1], (float) list[2]);
    //       });
  }

  public IPromise<Matrix4x4> FillMatrix4x4(PromptInfo prompt) {
    return FillObjectList<float>(prompt, 16)
      .Then(objects =>
          {
            var list = objects.ToList();
            var m = Matrix4x4.zero;
            for (int i = 0; i < 16; i++)
              m[i] = list[i];
            return m;
          });
  }

  public IPromise<Vector2> FillVector2(PromptInfo prompt) {
    return minibuffer.ExecuteCommand<Vector2>("MakeVector2");
    // return FillObjectList(prompt, typeof(float), 2)
    //   .Then(objects =>
    //       {
    //         var list = objects.ToList();
    //         return new Vector2((float) list[0], (float) list[1]);
    //       });
  }

  internal IPromise<object> FillProperty(PropertyInfo propertyInfo) {
    object[] attrs = propertyInfo.GetCustomAttributes(typeof(Prompt), false);
    PromptInfo prompt = PromptInfo.CopyOrNew((Prompt) attrs.FirstOrDefault());
    if (prompt.prompt == null)
      prompt.prompt
        = propertyInfo.PropertyType.PrettyName() + " "
        + propertyInfo.Name + ": ";

    return FillType(prompt, propertyInfo.PropertyType);
  }

  //[Filler("keybinding")]
  /**
    Returns an existing keybinding from the user.
   */
  public IPromise<string> FillKeyBinding(PromptInfo prompt,
                                         List<string> keyAccum = null,
                                         Promise<string> promise = null) {
    if (keyAccum == null)
      keyAccum = new List<string>();
    if (promise == null)
      promise = new Promise<string>();
    minibuffer.visible = true;
    minibuffer.Message(prompt.prompt + String.Join(" ", keyAccum.ToArray()));
    minibuffer.ReadKeyChord()
      .Then(k => {
          var key = k.ToString();
          keyAccum.Add(key);
          key = String.Join(" ", keyAccum.ToArray());
          var command = minibuffer.Lookup(key);
          if (command != null) {
            promise.Resolve(key);
          } else if (minibuffer.prefixes.Contains(key)) {
            // There's more.
            FillKeyBinding(prompt, keyAccum, promise);
          } else {
            // There's nothing.
            if (prompt.requireMatch)
              promise.Reject(new MinibufferException(key + " is not bound to any command."));
            else
              promise.Resolve(key);
          }
        })
      .Catch(ex => promise.Reject(ex));
    return promise;
  }

}
}
