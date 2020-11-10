/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;
using System.Threading;
using RSG;

namespace SeawispHunter.MinibufferConsole {

// https://gamedev.stackexchange.com/questions/50864/mixing-threads-and-coroutines-in-unity3d-mobile
// Hmm... have to be careful where the promises will run.  Will it be on the main thread?
// Could be.  It could happen anywhere actually.  Best to just sequester these promises.
public class ThreadedPromise : Promise {
  public ThreadedPromise(Action action) : base() {
    var thread = new Thread(() => {
        try {
          action();
          Resolve();
        } catch (Exception e) {
          Reject(e);
        }
      });
    thread.Start();
  }
}

public class ThreadedPromise<T> : Promise<T> {
  public T result;
  public ThreadedPromise(Func<T> action) : base() {
    var thread = new Thread(() => {
        try {
          result = action();
          Resolve(result);
          // Debug.Log("resolve thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
        } catch (Exception e) {
          // Debug.Log("reject thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
          Reject(e);
        }
      });
    thread.Start();
  }
}

}
