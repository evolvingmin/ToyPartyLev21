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
using System;

namespace SeawispHunter.MinibufferConsole {

[AttributeUsage(AttributeTargets.Parameter)]
/**
   Requests the current object of particular type.

   For instance, it can provide the current buffer to an IBuffer parameter.

```
    [Command("copy-buffer-to-clipboard",
             description = "Copy the contents of the current buffer to the clipboard.")]
    public static void CopyToClipboard([Current] IBuffer b) {
      GUIUtility.systemCopyBuffer = b.content;
    }
```

   Or the current InputField if any are selected.

```
[Command("forward-char",
         description = "Move cursor forward one character")]
public void ForwardChar([Current] InputField inputField) {
  inputField.caretPosition += 1;
}
```

   \see ICurrentProvider for instructions on how to implement your own.
 */
/** \ingroup MIT */
public class Current : Attribute {
  public bool acceptNull = false;
}

}
