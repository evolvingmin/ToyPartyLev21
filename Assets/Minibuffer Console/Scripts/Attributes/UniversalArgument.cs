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
   Mark an int, bool, or int? parameter to receive the universal argument.

   Many commands can benefit from having a boolean flag or integer count.
   Although a command may prompt the user for a boolean or integer directly,
   %UniversalArgument allows the user to provide this argument only when they
   choose to and with a convenient "universal" key binding.

   For clarity, <kbd>M-x display-universal-argument</kbd> shows the numerical
   and boolean value of its universal argument. If no argument is given, the
   count is 1, and the boolean value is false. If an argument is given, the
   count is 4, and the boolean value is true. If the argument is given a
   numerical count of 10, the count is 10 and the boolean value is true. If two
   <kbd>C-u</kbd>s are given, the numerical value is 16 and the boolean value is
   true. You can run these commands and variations to see for yourself.

   <kbd>M-x display-universal-argument</kbd>

   <samp>1 False</samp>

   <kbd>C-u M-x display-universal-argument</kbd>

   <samp>4 True</samp>

   <kbd>C-u 1 0 M-x display-universal-argument</kbd>

   <samp>10 True</samp>

   <kbd>C-u C-u M-x display-universal-argument</kbd>

   <samp>16 True</samp>

    ```
    [Command]
    public string DisplayUniversalArgument([UniversalArgument]
                                           int prefix,
                                           [UniversalArgument]
                                           bool boolPrefix) {
        return string.Format("{0} {1}", prefix, boolPrefix);
    }
    ```
 */
/** \ingroup MIT */
public class UniversalArgument : Attribute {}

}
