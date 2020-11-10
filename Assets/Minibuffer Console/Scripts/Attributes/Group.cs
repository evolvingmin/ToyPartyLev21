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

/**
   Name a group for the following commands and variables.

   Commands and variables are grouped typically by the class in which they're
   defined. The group name is typically derived from the class name. However, if
   you want override that group name, you may use this attribute.

   For instance, the class HelpCommands places its commands and variables in
   the group "help" rather than the default "help-commands".

   ```
   [Group("help")]
   public class HelpCommands : MonoBehaviour { ... }
   ```

   Individual commands may opt-in to a group that are not where they're defined.
   For instance the command "self-insert-command" is defined in %Minibuffer, but
   it makes more logical sense as part of the "editing" group, so the command
   places itself there.

   ```
   [Command("self-insert-command",
            description = "Insert the character that provoked this command.",
            group = "editing")]
   public void SelfInsertCommand() { ... }
   ```
*/
/** \ingroup MIT */
[AttributeUsage(AttributeTargets.Class)]
public class Group : Attribute {

  /** If no name is given, it'll be derived from the class name. */
  public Group() {}

  /** Give group name. */
  public Group(string name) {
    this.name = name;
  }
  /** Name of the group. */
  public string name;
  /** May set the description. */
  public string description;

  /*
    Let's allow some meta data.
  */
  public string tag;

  /*
    Let's allow some meta data.
  */
  public string[] tags;
}

}
