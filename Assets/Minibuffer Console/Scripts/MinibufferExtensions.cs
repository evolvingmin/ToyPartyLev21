/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using RSG;
using SeawispHunter.MinibufferConsole;

namespace SeawispHunter.MinibufferConsole.Extensions {

public static class MinibufferExtensions {

  public static void AddAll<TKey,TVal>(this IDictionary<TKey,TVal> dictA,
                                       IDictionary<TKey,TVal> dictB) {
    //IDictionary<TKey,TVal> output = new Dictionary<TKey,TVal>(dictA);

    foreach (var pair in dictB) {
      if (dictA.ContainsKey(pair.Key)
          // && dictA[pair.Key] != pair.Value
          ) {
        //dictA.Remove(pair.Key);
        UnityEngine.Debug.LogWarning("Overwriting key '{0}' for '{1}' with '{2}'."
                                     .Formatted(pair.Key.ToString(),
                                                dictA[pair.Key].ToString(),
                                                pair.Value.ToString()));
      }
      dictA[pair.Key] = pair.Value;
    }
  }

  public static string OxfordComma(this IEnumerable<String> collection,
                                   string andSlashOr,
                                   string ifNone = "") {
    var output = String.Empty;

    var list = collection.ToList();
    if (list.Count == 0) {
      output = ifNone;
    } else if (list.Count == 1) {
      output = list[0];
    } if (list.Count == 2) {
      output = list[0] + " " + andSlashOr + " " + list[1];
    } else if (list.Count > 1) {
      var delimited = String.Join(", ", list.Take(list.Count - 1).ToArray());

      output = String.Concat(delimited, ", ", andSlashOr, " ",
                             list.LastOrDefault());
    }

    return output;
  }

  public static string OxfordAnd(this IEnumerable<String> collection,
                                 string ifNone = "") {
    return collection.OxfordComma("and", ifNone);
  }

  public static string OxfordOr(this IEnumerable<String> collection,
                                string ifNone = "") {
    return collection.OxfordComma("or", ifNone);
  }

  /*
    Is string Null or Zero Length?  IsZull?

    Unity will set any public string (or serialized string) that
    inherits from a UnityEngine.Object by default to "" not null.
   */
  public static bool IsZull(this string s) {
    return s == null || s.Length == 0;
  }

  public static string PrettyName(this Type type) {
    if (type.GetGenericArguments().Length == 0) {
      if (type.IsNumericType()) {
        return type.NumericTypeAsString();
      } else {
        return type.Name;
      }
    }
    var genericArguments = type.GetGenericArguments();
    var typeDef = type.Name;
    if (typeDef.Contains("`")) {
      var unmangledName = typeDef.Substring(0, typeDef.IndexOf("`"));
      return unmangledName
        + "<" + string.Join(",", genericArguments
                                   .Select(t => t.PrettyName())
                                   .ToArray())
        + ">";
    } else {
      return typeDef;
    }
  }

  public static string PrettySignature(this MethodInfo mi,
                                       bool includeClassName = false) {
    var ps = mi.GetParameters()
      .Select(p => String.Format("{0} {1}",
                                 p.ParameterType.PrettyName(),
                                 p.Name));

    return String.Format("{4}{0} {3}{1}({2})", mi.ReturnType.PrettyName(),
                         mi.Name,
                         string.Join(", ", ps.ToArray()),
                         includeClassName
                           ? mi.DeclaringType.PrettyName() + "."
                           : "",
                         mi.IsStatic ? "static " : "");
  }

  public static string NumericTypeAsString(this Type t) {
    if (t.IsEnum)
      return null;
    switch (Type.GetTypeCode(t)) {
      case TypeCode.Byte:
        return "byte";
      case TypeCode.SByte:
        return "sbyte";
      case TypeCode.UInt16:
        return "ushort";
      case TypeCode.UInt32:
        return "uint";
      case TypeCode.UInt64:
        return "ulong";
      case TypeCode.Int16:
        return "short";
      case TypeCode.Int32:
        return "int";
      case TypeCode.Int64:
        return "long";
      case TypeCode.Decimal:
        return "decimal";
      case TypeCode.Double:
        return "double";
      case TypeCode.Single:
        return "float";
      default:
        return null;
    }
  }

  public static CompleterEntity ToEntity(this ICompleter completer) {
    return new CompleterEntity(completer);
  }

  // public static CompleterEntity ToEntity(this ICoercer coercer) {
  //   return new CompleterEntity(coercer);
  // }

  public static bool IsNumericType(this Type t) {
    if (t.IsEnum)
      return false;
    switch (Type.GetTypeCode(t)) {
      case TypeCode.Byte:
      case TypeCode.SByte:
      case TypeCode.UInt16:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.Int64:
      case TypeCode.Decimal:
      case TypeCode.Double:
      case TypeCode.Single:
        return true;
      default:
        return false;
    }
  }

  public static void Reject<T>(this Promise<T> promise, string msgFormat,
                               params object[] args) {
    promise.Reject(new MinibufferException(string.Format(msgFormat, args)));
  }

  public static bool IsPending(this Promise promise) {
    return promise.CurState == PromiseState.Pending;
  }

  public static IPromise RunInThread(this MonoBehaviour mb, Action action) {
    var p = new Promise();
    // Although these threads are encapsulated as a promise, it's troublesome to
    // actually resolve them within their own thread context. Therefore, we
    // still use the promise but it is never directly chained with our other
    // promises.
    mb.StartCoroutine(new ThreadedPromise(action)
                      .WaitForPromiseAndThen(() => p.Resolve()));
    return p;
  }

  // Consider using a custom yield instruction:
  // https://docs.unity3d.com/ScriptReference/CustomYieldInstruction.html
  public static IEnumerator WaitForPromiseAndThen(this Promise promise, Action then) {
    while (promise.CurState == PromiseState.Pending)
      yield return null;
    then();
  }

  public static IPromise<T> RunInThread<T>(this MonoBehaviour mb, Func<T> action) {
    var p = new Promise<T>();
    var thread = new ThreadedPromise<T>(action);
    mb.StartCoroutine(thread
                      .WaitForPromiseAndThen(() => { p.Resolve(thread.result); }));
    return p;
  }


  public static IEnumerator WaitForPromise(this Promise promise) {
    while (promise.CurState == PromiseState.Pending)
      yield return null;
  }

  public static IEnumerator WaitForPromise(this IPromise promise) {
    var p = new Promise();
    promise.Then(() => p.Resolve(), ex => p.Reject(ex));
    return p.WaitForPromise();
  }

  public static IEnumerator WaitForPromise<T>(this Promise<T> promise) {
    while (promise.CurState == PromiseState.Pending)
      yield return null;
  }

  public static IEnumerator WaitForPromiseAndThen<T>(this Promise<T> promise, Action then) {
    while (promise.CurState == PromiseState.Pending)
      yield return null;
    then();
  }

  public static IEnumerator WaitForPromise<T>(this IPromise<T> promise) {
    var p = new Promise();
    promise.Then(x => p.Resolve(), ex => p.Reject(ex));
    return p.WaitForPromise();
  }


  public static string Formatted(this string formatString,
                                 params object[] args) {
    return String.Format(formatString, args);
  }

  public static bool Implements(this Type source, Type theInterface) {
    return theInterface.IsAssignableFrom(source);
  }

  /**
     Limit the size of a list.
   */
  public static void LimitSize<T>(this List<T> list, int size) {
    if (list.Count > size)
      list.RemoveRange(size, list.Count - size);
  }

  public static Type CreateDelegateType(this MethodInfo method, bool prependInstanceParameter) {
    if (method.IsGenericMethod) {
      throw new ArgumentException("The provided method must not be generic.", "method");
    }
    var parameters = method.GetParameters()
      .Select(p => p.ParameterType);
    if (prependInstanceParameter && ! method.IsStatic)
      parameters = parameters.Prepend(method.DeclaringType);
    // Debug.Log(method + "'s parameters " + string.Join(", ", parameters.Select(z => z.ToString()).ToArray()));
    if (method.ReturnType == typeof(void)) {
      return Expression
               .GetActionType(parameters.ToArray<Type>());
    } else {
      return Expression
               .GetFuncType(parameters
                            .Append(method.ReturnType)
                            .ToArray<Type>());
    }
  }

  /**
     Search for a method based on generic arguments in addition to name and
     parameters. Most useful for disambiguating methods with the same name.

     If the elements of genericArgs are not null, it will return the specialized
     generic method. Otherwise it will return the generic method definition.
     Finally, if genericArgs has no elements, it will return a non-generic
     method.  If no method is found, it will return null.

     Any nulls in the parameterTypes will be treated as a wildcard for any type.

     e.g.  Suppose I had the following methods in a class Minibuffer:

     IPromise<string> Read(Prompt p) { ... }
     IPromise<T> Read<T>(Prompt p) { ... }
     IPromise<T> Read<T>(Prompt p, Action<T> a) { ... }

     And I wanted to get the first generic one:

     Minibuffer.GetMethodGeneric("Read", new [] {typeof(int)}, new [] {typeof(Prompt)}) ->
          IPromise`1 Read[Int64](SeawispHunter.MinibufferConsole.Prompt)

     Minibuffer.GetMethodGeneric("Read", new Type[] {null}, new [] {typeof(Prompt)}) ->
          IPromise`1 Read[T](SeawispHunter.MinibufferConsole.Prompt)

     Or I wanted to get the second generic one:

     Minibuffer.GetMethodGeneric("Read", new Type[] {null}, new [] {typeof(Prompt), null}) ->
          IPromise`1 Read[T](SeawispHunter.MinibufferConsole.Prompt, System.Action[T])

     Or I could get the non-generic one:

     Minibuffer.GetMethodGeneric("Read", new Type[] {}, new [] {typeof(Prompt)}) ->
          IPromise`1 Read(SeawispHunter.MinibufferConsole.Prompt)
  */

  public static MethodInfo GetMethodGeneric(this Type t,
                                            string name,
                                            Type[] genericArgs,
                                            Type[] parameterTypes) {
    var myMethod
      = t
      .GetMethods()
      // I can count the number of times I've had to actually XOR on one hand.
      .Where(m => (genericArgs.Length == 0 ^ m.IsGenericMethod)
                  && m.Name == name)
      .Where(m => {
            var paramTypes = m.GetParameters().Select(p => p.ParameterType);
            var genParams = m.GetGenericArguments();
            Func<Type, Type, bool> f = (typeInput, typeMatch) => typeMatch == null ? true : typeMatch == typeInput;
            return genParams.Length == genericArgs.Length
            // No Zip in Unity.  Bleh.
            // && Enumerable
            // .Zip(paramTypes,
            //      parameterTypes,
            //      (typeInput, typeMatch) => typeMatch == null ? true : typeMatch == typeInput)
            // .All(x => x);
            // && Enumerable.SequenceEqual(paramTypes.Select(p => p.ParameterType),
            //                             parameterTypes);
            && Enumerable
                 .SequenceEqual(paramTypes,
                                parameterTypes,
                                new FuncEqualityComparer<Type>(f));
          })
      .SingleOrDefault();
    if (myMethod != null
        && myMethod.ContainsGenericParameters
        && genericArgs.All(p => p != null)) {
      return myMethod.MakeGenericMethod(genericArgs);
    }
    return myMethod;
  }

  /**
     Need to wait a tick before doing something? Don't want to pollute your
     class with a method just to write the coroutine? Do this instead!

     ```cs
       behaviour.DoNextTick(() => {
         Debug.Log("I waited!");
       })
     ```
  */
  public static void DoNextTick(this UnityEngine.MonoBehaviour mb, Action action) {
    mb.StartCoroutine(YieldOnceCoroutine(null, action));
  }

  public static void DoAfter(this UnityEngine.MonoBehaviour mb, object firstYield, Action action) {
    mb.StartCoroutine(YieldOnceCoroutine(firstYield, action));
  }

  private static IEnumerator YieldOnceCoroutine(object firstYield, Action action) {
    yield return firstYield;
    action();
  }

  public static IEnumerable<T> ToEnumerable<T>(this T item) {
    yield return item;
  }

  public static void Deselect(this InputField inputField) {
    inputField.selectionAnchorPosition = inputField.selectionFocusPosition;
  }
  public static void Deselect(this TappedInputField inputField) {
    inputField.selectionAnchorPosition = inputField.selectionFocusPosition;
  }

  public static void SetLineIndex(this InputField inputField, int lineIndex) {
    // Debug.Log("SetLineIndex to " + lineIndex);
    var charIndex = inputField.positionForLineIndex(lineIndex);
    // Debug.Log("SetLineIndex to " + lineIndex + " goes to charIndex " + charIndex);
    inputField.caretPosition = charIndex;
  }

  public static int GetLineIndex(this InputField inputField) {
    return inputField.lineIndexForPosition(inputField.caretPosition);
  }

  public static int lineIndexForPosition(this InputField inputField, int charPosition) {
    return LineIndexForPosition(inputField.text, charPosition);
    // var generator = inputField.textComponent.cachedTextGenerator;

    // for (int i = 0; i < generator.lineCount - 1; ++i) {
    //   if (generator.lines[i + 1].startCharIdx > charPosition)
    //     return i;
    // }

    // return generator.lineCount - 1;
  }

  public static int positionForLineIndex(this InputField inputField, int lineIndex) {
    // var generator = inputField.textComponent.cachedTextGenerator;

    // if (false && lineIndex >= 0 && lineIndex < generator.lineCount)
    //   return generator.lines[lineIndex].startCharIdx;
    // else {
    // Do it the expensive way.
    return PositionForLineIndex(inputField.text, lineIndex);
    // Ha ha! I found you!
    // return 0;
    // }
  }

  public static int GetLineCount(this InputField inputField) {
    return inputField.textComponent.cachedTextGenerator.lineCount;
  }

  public static void ScrollEvent(this InputField inputField) {
    // Debug.Log("ScrollEvent");
    typeof(InputField).GetMethod("MarkGeometryAsDirty", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inputField, null);
  }

  public static Vector2 CharSize(this Text textComponent, bool scaled = true) {
    // Assumes the font is fixed width.
    var settings = textComponent.GetGenerationSettings(new Vector2(1000f, 1000f));
    var charWidth = textComponent.cachedTextGenerator.GetPreferredWidth("M", settings);
    var charHeight = textComponent.cachedTextGenerator.GetPreferredHeight("M", settings);
    var charSize = new Vector2(charWidth, charHeight);
    if (scaled) {
      var canvas = textComponent.GetComponentInParent<Canvas>();
      charSize = charSize / canvas.scaleFactor;
    }
    return charSize;
  }

  public static int LineCount(this string s) {
    int len = s.Length;
    int c = 0;
    for (int i = 0; i < len;  i++) {
      if (s[i] == '\n')
        c++;
    }
    return c + 1;
  }

  public static int PositionForLineIndex(this string s, int lineIndex) {
    int len = s.Length;
    int c = 0;
    for (int i = 0; i < len;  i++) {
      if (c == lineIndex)
        return i;
      if (s[i] == '\n')
        c++;
    }
    return -1;
  }

  public static int LineIndexForPosition(this string s, int charPosition) {
    int len = s.Length;
    int c = 0;
    for (int i = 0; i < len && i <= charPosition;  i++) {
      if (s[i] == '\n')
        c++;
    }
    return c;
  }

  public static int MaxLineWidth(this string s) {
    int len = s.Length;
    int c = 0;
    int maxWidth = 0;
    for (int i = 0; i < len;  i++) {
      c++;
      if (s[i] == '\n') {
        if (c > maxWidth)
          maxWidth = c;
        c = 0;
      }
    }
    return maxWidth != 0 ? maxWidth : c;
  }

  public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T> {
    // compareTo < 0 if val precedes min in the sort order
    if (val.CompareTo(min) < 0) return min;
    else if(val.CompareTo(max) > 0) return max;
    else return val;
  }

  public static T Min<T>(this T val, T min) where T : IComparable<T> {
    // compareTo < 0 if val precedes min in the sort order
    if (val.CompareTo(min) > 0) return min;
    else return val;
  }

  public static T Max<T>(this T val, T max) where T : IComparable<T> {
    if (val.CompareTo(max) < 0) return max;
    else return val;
  }

  public static string GetDescription<T>(this T enumerationValue) where T : struct {
    Type type = enumerationValue.GetType();
    if (! type.IsEnum)
      throw new ArgumentException("Must be an Enum type", "enumerationValue");
    System.Reflection.MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
    if (memberInfo != null && memberInfo.Length > 0) {
      object[] attrs = memberInfo[0].GetCustomAttributes(typeof(Description), false);
      if (attrs != null && attrs.Length > 0)
        return ((Description)attrs[0]).text;
    }
    return enumerationValue.ToString();
  }

  public static bool IsZero(this float a) {
    return Mathf.Approximately(a, 0f);
  }

  public static IEnumerable<Transform> OrderByHierarchy(this IEnumerable<Transform> ts) {
    return ts.OrderBy(t => t.GetSiblingIndex());
  }

  public static IEnumerable<GameObject> OrderByHierarchy(this IEnumerable<GameObject> gs) {
    return gs.OrderBy(g => g.transform.GetSiblingIndex());
  }

  public static IEnumerable<T> OrderByHierarchy<T>(this IEnumerable<T> cs)
    where T : Component {
    return cs.OrderBy(c => c.transform.GetSiblingIndex());
  }
}

public class FuncEqualityComparer<T> : IEqualityComparer<T> {
  readonly Func<T, T, bool> comparer;
  readonly Func<T, int> hash;
  public FuncEqualityComparer(Func<T, T, bool> comparer) : this( comparer, t => t.GetHashCode()) {}
  public FuncEqualityComparer(Func<T, T, bool> comparer, Func<T, int> hash) {
    this.comparer = comparer;
    this.hash = hash;
  }
  public bool Equals(T x, T y) { return comparer( x, y ); }
  public int GetHashCode(T obj) { return hash( obj ); }
}
}
