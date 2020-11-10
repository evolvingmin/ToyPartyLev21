using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SeawispHunter.MinibufferConsole.Extensions {
/*
  Some helpful Linq extensions I've used with Unity.
*/
public static class LinqExtensions {

  public static Queue<T> ToQueue<T>(this IEnumerable<T> source) {
    if (source == null)
      throw new ArgumentNullException("source");
    return new Queue<T>(source);
  }

  public static Stack<T> ToStack<T>(this IEnumerable<T> source) {
    if (source == null)
      throw new ArgumentNullException("source");
    return new Stack<T>(source);
  }

  public static bool IsEmpty<T>(this IEnumerable<T> source) {
    return ! source.Any();
  }

  public static void Each<T>(this IEnumerable<T> items, Action<T> action) {
    foreach (var i in items)
      action(i);
  }

#if NET_2_0 || NET_2_0_SUBSET

  public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T element) {
    if (source == null)
      throw new ArgumentNullException("source");
    foreach (var e in source)
      yield return e;
    yield return element;
  }

#if ! NET_4_6
  public static IEnumerable<T> Prepend<T>(this IEnumerable<T> tail, T head) {
    if (tail == null)
      throw new ArgumentNullException("tail");
    yield return head;
    foreach (var e in tail)
      yield return e;
  }

  public static IEnumerable<TResult> Zip<TA, TB, TResult>(
                                                          this IEnumerable<TA> seqA, IEnumerable<TB> seqB,
                                                          Func<TA, TB, TResult> func) {
    if (seqA == null) throw new ArgumentNullException("seqA");
    if (seqB == null) throw new ArgumentNullException("seqB");

    using (var iteratorA = seqA.GetEnumerator())
      using (var iteratorB = seqB.GetEnumerator()) {
        while (iteratorA.MoveNext() && iteratorB.MoveNext()) {
          yield return func(iteratorA.Current, iteratorB.Current);
        }
      }
  }
  #endif
#endif

  public static IEnumerable<DictionaryEntry> CastDict(this IEnumerable dictionary) {
    foreach (DictionaryEntry entry in dictionary) {
      yield return entry;
    }
  }

  // https://code.msdn.microsoft.com/LINQ-to-DataSets-Custom-41738490
  public static IEnumerable<T2> Combine<T,T2>(this IEnumerable<T> first,
                                              IEnumerable<T> second,
                                              System.Func<T,T,T2> func) {
    using (IEnumerator<T> e1 = first.GetEnumerator(),
           e2 = second.GetEnumerator()) {
      while (e1.MoveNext() && e2.MoveNext()) {
        yield return func(e1.Current, e2.Current);
      }
    }
  }

  /*
    Transform a series based on a window of its data.  Note: the first
    argument to func is not guaranteed to be windowSize.

    s.Window(3, f) =>
    {f({s[0]}), f({s[0], s[1]}), f({s[0], s[1], s[2]}),
     f({s[1], s[2], s[3]}), ... }

    e.g.\ Simple Moving Average with a window of size 5:
          series.Window(5, sample => sample.Average());
   */
  public static IEnumerable<T2> Window<T,T2>(this IEnumerable<T> series,
                                             int windowSize,
                                             System.Func<IEnumerable<T>,T2> func) {
    using (IEnumerator<T> e1 = series.GetEnumerator()) {
      Queue<T> q = new Queue<T>();
      //for (int i = 0; i < windowSize - 1 && e1.MoveNext(); i++)
      //q.Enqueue(e1.Current);
      while (e1.MoveNext()) {
        q.Enqueue(e1.Current);
        yield return func(q);
        if (q.Count >= windowSize)
          q.Dequeue();
      }
    }
  }

  public static T Random<T>(this IEnumerable<T> enumerable) {
    int index;
    return RandomWithIndex(enumerable, out index);
  }

  public static T RandomWithIndex<T>(this IEnumerable<T> enumerable, out int index) {
    //var r = new Random();
    var list = enumerable as IList<T> ?? enumerable.ToList();
    return list.ElementAt(index = UnityEngine.Random.Range(0, list.Count));
  }

  // Reservoir Sampling
  // https://en.wikipedia.org/wiki/Reservoir_sampling
  /*
    Return an IEnumerable with k random items from the given
    enumerable with n items.

    It's O(n) and doesn't need to get the count n first.

    Note: The if the enumerable only has k items, the result will not
    be shuffled.
  */
  public static IEnumerable<T> Random<T>(this IEnumerable<T> enumerable, int k) {
    var list = new List<T>(k);
    using (var e = enumerable.GetEnumerator()) {
      for (int i = 0; i < k && e.MoveNext(); i++)
        list[i] = e.Current;
      for (int j, i = k + 1; e.MoveNext(); i++) {
        j = UnityEngine.Random.Range(0, i);
        if (j < k)
          list[j] = e.Current;
      }
      return list;
    }
  }

  // Fisher-Yates-Durstenfeld shuffle

  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng = null) {
    if (rng == null)
      rng = new Random();
    var buffer = source.ToList();
    for (int i = 0; i < buffer.Count; i++) {
      int j = rng.Next(i, buffer.Count);
      yield return buffer[j];
      buffer[j] = buffer[i];
    }
  }

  public static IEnumerable<TSource> DistinctBy<TSource, TKey>
    (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
    HashSet<TKey> seenKeys = new HashSet<TKey>();
    foreach (TSource element in source) {
      if (seenKeys.Add(keySelector(element))) {
        yield return element;
      }
    }
  }

}
}
