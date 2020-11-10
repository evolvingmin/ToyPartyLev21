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

/** \defgroup MIT MIT Licensed Code


    This group of code--the attributes and stub--is licensed differently than
    Minibuffer.

    ![MIT License logo&dagger;](MIT-License-transparent.png)

    Minibuffer is licensed under Unity Asset Store End User License Agreement.
    In addition to that license, the classes \link
    SeawispHunter.MinibufferConsole.Command Command\endlink, \link
    SeawispHunter.MinibufferConsole.Variable Variable\endlink, \link
    SeawispHunter.MinibufferConsole.Prompt Prompt\endlink, \link
    SeawispHunter.MinibufferConsole.Group Group\endlink, \link
    SeawispHunter.MinibufferConsole.UniversalArgument UniversalArgument\endlink, \link
    SeawispHunter.MinibufferConsole.MinibufferListing MinibufferListing\endlink, \link
    SeawispHunter.MinibufferConsole.Current Current\endlink, and \link
    SeawispHunter.MinibufferConsole.MinibufferStub MinibufferStub\endlink are available
    under the MIT License.

    These classes may be of use for asset developers. The motivation for this
    additional license is two fold:

    1) An asset developer might want to make their own assets
    "minibuffer-able"--i.e., expose commands and variables for their users that
    have Minibuffer--without having to litter \#ifdefs everywhere.&Dagger; This
    license allows asset developers to include the attribute markup in their
    asset code and the attributes. Users without Minibuffer will not notice a
    difference, but users with Minibuffer will have new commands, variables, and
    key bindings that enhance the developer's asset. (Many assets don't expose
    any kind of developer UI at runtime because games are so idiosyncratic, and
    because UIs are a lot of work, but %Minibuffer could reduce those
    impediments.)

    2) Another developer may wish to write tools that adopt %Minibuffer's
    philosophy of UI-by-decoration but with a UI that dramatically differs from
    %Minibuffer. %Minibuffer is very comfortable for
    [Emacs](https://www.gnu.org/software/emacs/) users, but perhaps someone else
    would prefer a Command Line Interface (CLI) or a more
    [Vim](http://www.vim.org)-like experience. Or perhaps they require a less
    keyboard-centric UI to serve developers and testers on mobile platforms.
    This license allows another developer to follow that end, but hopefully the
    commands, variables, prompts, and other attribute markup may be shared
    freely even if the UI differs considerably; thus all these UI-by-decoration
    developer tools may share in a commons.

    <small>&dagger; [MIT License logo](http://excaliburzero.deviantart.com/art/MIT-License-Logo-595847140) by Excalibur Zero licensed under the [CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/) </small>

    <small>&Dagger; Some \#ifdefs are unavoidable but the goal is to avoid it in the majority of cases. </small>

    # License Text

    Copyright (c) 2016 Shane Celis, [\@shanecelis][1]

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

    [1]: https://twitter.com/shanecelis
*/


namespace SeawispHunter.MinibufferConsole {

/**
   \defgroup command Commands

   These are %Minibuffer "commands" that may be executed by typing <kbd>M-x command-name</kbd> or keys may be bound to them.

   \see Command
*/
/**
Mark a method as an interactive command.

### Mark a public method with the %Command attribute.

The method `HelloX` requires a string and returns a string. The `[%Command]`
attribute marks it as an interactive command to %Minibuffer by the name
'hello-x', which can be executed by typing <kbd>M-x hello-x</kbd>. %Minibuffer
will then prompt the user <samp>String name: </samp>. Suppose the user responds
<kbd>Mary</kbd>. The method will return a string "Hello, Mary", and %Minibuffer
will show this as a message <samp>Hello, Mary</samp>.


```
  [Command]
  public string HelloX(string name) {
    return "Hello, " + name;
  }
```

![Command case setting](files/command-case.png)
\note The naming convention for commands may be set to PascalCase, camelCase, and
kebab-case, or as-is. So the `HelloX` command name would be "HelloX", "helloX", or "hello-x" respectively. See \link
SeawispHunter.MinibufferConsole.Minibuffer.CanonizeCommand CanonizeCommand\endlink for more details.

### Registering with %Minibuffer

Classes do have to \link SeawispHunter.MinibufferConsole.Minibuffer.Register
register\endlink with %Minibuffer.
```
  void Start() {
    Minibuffer.Register(this);
  }
```

### Specify the command name

Type <kbd>M-x hi</kbd> to run the following command.

```
  [Command("hi")]
  public string HelloY(string name) {
    return "Hello, " + name;
  }
```

### Provide a description

Explain what a command does in its description that is used in various help
commands like <kbd>M-x describe-command</kbd> and <kbd>M-x
describe-bindings</kbd>.

```
  [Command("hi-there", description = "Say hello to X.") ]
  public string HelloW(string name) {
    return "Hello, " + name;
  }
```

See its description by typing <kbd>M-x describe-command hi-there</kbd>.

<pre><samp>hi-there is not bound to any keys.
Description: "Say hello to X."
It is defined in class ExampleCommands.
String HelloW(String str)</samp></pre>

### Set a key binding

Bind a key to the command. Run the command <kbd>M-x hello-z</kbd> or by typing
<kbd>C-h w</kbd>.

```
  [Command(keyBinding = "C-h w")]
  public string HelloZ(string name) {
    return "Hello, " + name;
  }
```

  See listing of all builtin \ref command, or type <kbd>C-h c *all*</kbd>.

## Prompts

The default prompt for each command argument is "<type> <argument-name>: ". This
can be changed by adding a Prompt attribute.

```
  [Command]
  public string HelloA([Prompt("First name: ", history = "first-name")] string name) {
    return "Hello, " + name;
  }
```

See the Prompt class for more details.

## Special Return Types

%Minibuffer commands that return a `String` or `IEnumerator` are handled
differently for convenience.

### String Return Type

A command that returns a `String` is output as a message.

```
[Command]
public string HelloX(string name) {
  return "Hello, " + name;
}
```
It is equivalent to the following code.

```
[Command]
public void HelloX2(string name) {
  Minibuffer.instance.Message("Hello, " + name);
}
```

### IEnumerator Return Type

A command that returns an `IEnumerator` is run as a coroutine.

```
[Command]
public IEnumerator HelloWaitForIt(string name) {
  Minibuffer.instance.Message("Hello, ");
  yield new WaitForSeconds(1f);
  Minibuffer.instance.Message("Wait for it...");
  yield new WaitForSeconds(1f);
  Minibuffer.instance.Message(name);
}
```

It is equivalent to the following code.

```
[Command]
public void HelloWaitForIt2(string name) {
  StartCoroutine(HelloWaitForIt2Helper(name));
}

private IEnumerator HelloWaitForIt2Helper(string name) {
  Minibuffer.instance.Message("Hello, ");
  yield new WaitForSeconds(1f);
  Minibuffer.instance.Message("Wait for it...");
  yield new WaitForSeconds(1f);
  Minibuffer.instance.Message(name);
}
```

 */
[AttributeUsage(AttributeTargets.Method)]
/** \ingroup MIT */
public class Command : Attribute {

  /** The command name will be derived from the method's name. */
  public Command() {}

  /** Command with given name */
  public Command(string name) {
    this.name = name;
  }

  /** Name of the command. If not given, will use the method's name. */
  public string name;

  /** %Command description or null. */
  public string description;

  /** The brief description will be generated from the first sentence of the
      description if available; otherwise it's null. */
  public string briefDescription;
  /** Bind a key sequence to this command. */
  public string keyBinding;
  /** Bind multiple key sequences to this command. */
  public string[] keyBindings;
  /** The keymap to place any key bindings in. */
  public string keymap;
  /** Place command in given group. By default it will go into the group of its
      class. */
  public string group;

  /*
    Where this command is defined, e.g., "class X".
   */
  public string definedIn;

  /*
    The signature of the method.  Will be generated automatically if not set.
  */
  public string signature;

  /*
    The names of the parameters.  Will be generated automatically if not set.
  */
  public string[] parameterNames;

  /*
    The prompts for each parameter.  Will be generated automatically if not set.
  */
  public Prompt[] prompts;

  /*
    Add a meta datum.
  */
  public string tag;

  /*
    Add some meta data.
  */
  public string[] tags;

  /*
     Implicit conversion from string to Command object.  This is
     mainly so Minibuffer.RegisterCommand, which has five variations
     currently, doesn't require twice as many methods.
  */
  public static implicit operator Command(string commandName) {
    return new Command(commandName);
  }
}
}
