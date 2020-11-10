/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis

  * * *

  Licensed under the MIT License

  Permission is hereby granted, free of charge, to any person
  obtaining a copy of this software and associated documentation files
  (the "Software"), to deal in the Software without restriction,
  including without limitation the rights to use, copy, modify, merge,
  publish, distribute, sublicense, and/or sell copies of the Software,
  and to permit persons to whom the Software is furnished to do so,
  subject to the following conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
  BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
  ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
  CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

/**
   Comment the following line to enable the MinibufferStub to be used with the
   Minibuffer name. (This should not be commented when Minibuffer is actually
   present.)
*/
#define SH_MINIBUFFER           // Comment this line to enable MinibufferStub.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using RSG;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

#if ! SH_MINIBUFFER
/* If there's no Minibuffer, let's use our stub. */
public class Minibuffer : MinibufferStub {}
#endif


/**
  This stub may be of use for asset developers.

  This class does not stub all of Minibuffer's methods but it includes stubs for
  the most used setup methods. The motivation for this class is that one might
  want to make their own assets "minibuffer-able"--i.e., expose commands and
  variables for those that have Minibuffer--without having to litter \#ifdefs
  everywhere.

  That's also why the license for the attributes and this stub is different than
  %Minibuffer proper. %Minibuffer proper is licensed under Unity Asset Store End
  User License Agreement. The classes Command, Variable, Prompt,
  UniversalArgument, MinibufferListing, and MinibufferStub are licensed under
  the MIT License. (See the \link MIT MIT license page\endlink for more
  details.) Should someone else wish to write tools that adopt %Minibuffer's
  philosophy of UI-by-decoration but with a UI that is less like Emacs and more
  a CLI, or more [Vim](http://www.vim.org)-like experience, etc., I encourage
  them to use these attributes.

  If one needs functionality not exposed by this stub, I suggest doing the
  following:

  ```
  #if SH_MINIBUFFER
  // The 'visible' property is not exposed in the Minibuffer stub.
  if (Minibuffer.instance.visible) {
    Minibuffer.instance.Message("Minibuffer is visible!");
  }
  #endif
  ```

  The only thing this stub actually _does_ is redirect messages to
  `Debug.Log()`.  All `Action`s or `Func`s provided to stub methods will
  never be executed.

  Using this Class
  ----------------

  To use this stub as a drop-in replacement for the Minibuffer class, comment
  out the following line in `MinibufferStub.cs`:

```
#define SH_MINIBUFFER           // Comment this line to enable MinibufferStub.
```

That will expose MinibufferStub as the Minibuffer class.

```
#if ! SH_MINIBUFFER
// If there's no Minibuffer, let's use our stub. 
public class Minibuffer : MinibufferStub {}
#endif
```

Note the stub does not inherit from MonoBehaviour.

 */
/** \ingroup MIT */
public class MinibufferStub /*: MonoBehaviour */ {

  private static MinibufferStub _instance;

  /**
     Returns an instance of the MinibufferStub class. Will always work unlike
     Minibuffer, we don't need to wait for Unity to instantiate it.
   */
  public static MinibufferStub instance {
    get {
      if (_instance == null) {
        Debug.Log("Instantiating MinibufferStub. "
          + "See http://seawisphunter.com/minibuffer to enable Minibuffer.");
        _instance = new MinibufferStub();
      }
      return _instance;
    }
  }

  /**
     Does nothing.
   */
  public static void OnStart(Action<MinibufferStub> action) {}

  /**
     Does nothing.
  */
  public static void With(Action<MinibufferStub> action) {}

  /**
     Prints message using `Debug.Log()` with prefix "Message: ".
   */
  public void Message(string msgFormat, params object[] args) {
    // Message should be shown unless the minibuffer is being used.
    Message(string.Format(msgFormat, args));
  }

  public void Message(string msg) {
    // Message should be shown unless the minibuffer is being used.
    Debug.Log("Message: " + msg);
  }

  /**
     Prints message using `Debug.Log()` with prefix "MessageAlert: ".
   */
  public void MessageAlert(string msgFormat, params object[] args) {
    MessageAlert(string.Format(msgFormat, args));
  }

  public void MessageAlert(string msg) {
    Debug.Log("MessageAlert: " + msg);
  }

  /**
     Does nothing.
  */
  public static void Register(object o) {}

  /**
     Does nothing.
  */
  public static void Register(Type t) {}

  /**
     Does nothing.
  */
  public void RegisterObject(object o) {}

  /**
     Does nothing.
  */
  public void RegisterType(Type t) {}


  /**
     Does nothing.
  */
  public static void Unregister(object o) {}

  /**
     Does nothing.
  */
  public void UnregisterObject(object o) {}

  /**
     Does nothing.
  */
  public void RegisterVariable<T>(Variable variable,
                                  Func<T> getFunc,
                                  Action<T> setFunc) {}

  /**
     Does nothing.
   */
  public void UnregisterVariable(string variable) {}

  /**
     Does nothing.
   */
  public void RegisterCommand(Command command,
                              Delegate action) {}

  public void RegisterCommand(Command command,
                              Action action) {}

  public void RegisterCommand<T>(Command command,
                                 Action<T> action) {}

  public void RegisterCommand<T1, T2>(Command command,
                                      Action<T1, T2> action) {}

  public void RegisterCommand<T1, T2, T3>(Command command,
                                          Action<T1, T2, T3> action) {}

  /**
     Does nothing.
   */
  public void UnregisterCommand(string commandName) {}

  /**
     Print content using Debug.Log with the bufferName used as a prefix.
  */
  public void ToBuffer(string bufferName, string content) {
    Debug.Log(bufferName + ": " + content);
  }

  /**
     Print content using Debug.Log with the bufferName used as a prefix.
   */
  public void MessageOrBuffer(string bufferName, string msg) {
    ToBuffer(bufferName, msg);
  }
}

} // end namespace SeawispHunter.MinibufferConsole
