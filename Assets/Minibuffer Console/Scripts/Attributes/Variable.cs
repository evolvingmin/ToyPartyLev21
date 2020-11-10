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
using System.Text.RegularExpressions;

namespace SeawispHunter.MinibufferConsole {

/**
   \defgroup variable Variables

   These are "variables" exposed to %Minibuffer's <kbd>M-x describe-variable</kbd> and
   <kbd>M-x edit-variable</kbd> commands.

   \see Variable
 */
/**
   Mark a field or property as a variable to %Minibuffer.

   ```

   // We can expose fields as variables in minibuffer.
   [Variable]
   public int myGold = 0;

   // We can expose properties as variables in minibuffer.
   [Variable]
   public string quote {
     get { return _quote.text; }
     set { _quote.text = value; }
   }
   private Text _quote;
   ```

   Type <kbd>M-x edit-variable quote</kbd> to edit the variable. Type <kbd>M-x
   describe-variable quote</kbd> to see information on the variable.
*/
/** \ingroup MIT */
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class Variable : Attribute {
  /** Use the field or property's name. */
  public Variable() {}

  /** Name the variable */
  public Variable(string name) {
    this.name = name;
  }

  /** Name of the variable */
  public string name;
  /** Description of the variable */
  public string description;

  /** The brief description will be generated from the first sentence of the
      description if available; otherwise it's null. */
  public string briefDescription;  /*
    I'd like to perhaps allow for string, int, or float types to be
    persisted in the PlayerPrefs.  However, that introduces a lot of
    complexity.  For instance, how are different instances' values
    saved (if allowed)?  How are different instances' values setup
    upon instantiation?  For these reasons maybe it's best to only
    allow it on static variables or ScriptableObjects.
   */
  //public bool persist { get; set; }

  /** Where was this variable defined? Use "class X" if not given. */
  public string definedIn;

  public string group;

  /*
    Add a meta datum.
  */
  public string tag;

  /*
    Add some meta data.
  */
  public string[] tags;
}

}
