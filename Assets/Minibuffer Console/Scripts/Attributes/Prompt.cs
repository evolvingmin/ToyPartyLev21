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
   %Prompt the user for an argument with completer, default
   input, history, etc.


   Consider the following example command.

   ```
   [Command]
   public string HelloA(string str) {
   return "Hello, " + str + "!";
   }
   ```

   By default the command <kbd>M-x hello-a</kbd> will ask the user to provide a
   <samp>String str: </samp>.  This might be confusing for the user.

   <kbd>M-x hello-a</kbd><br>
   <samp>String str: </samp><kbd>umm?</kbd><br>
   <samp>Hello, umm?!</samp>

   Now obviously, the name of the parameter "str" could be changed to, say,
   "name" which would help. However, sometimes one wants to control the entire
   prompting, which is what the %Prompt attribute provides.

   ## Set the _prompt_

   It is used to decorate a parameter in a Command method.  In its
   simplest usage, it defines how to _prompt_ the user for input.

   ```
   [Command]
   public string HelloB([Prompt("Name: ")]
                        string str) {
     return "Hello, " + str + "!";
   }
   ```

   <kbd>M-x hello-b</kbd><br>
   <samp>Name: </samp><kbd>Mary</kbd><br>
   <samp>Hello, Mary!</samp>

   ## Default Input


   ```
   [Command]
   public string HelloC([Prompt("Name: ",
                                input = "Susan")]
                        string str) {
    return "Hello, " + str + "!";
   }
   ```

   <kbd>M-x hello-c</kbd><br>
   <samp>Name: Susan</samp><br>
   <kbd>return</kbd><br>
   <samp>Hello, Susan!</samp>

   ## History

   History allows the user to quickly look up previous responses.

   ```
   [Command]
   public string HelloD([Prompt("Name: ", history = "first-name")] string str) {
   return "Hello, " + str + "!";
   }
   ```

   <kbd>M-x hello-d</kbd><br>
   <samp>Name: </samp><br>
   <kbd>M-uparrow</kbd><br>
   <samp>Name: Bob</samp><br>
   <kbd>M-uparrow</kbd><br>
   <samp>Name: Jordan</samp><br>
   <kbd>M-downarrow</kbd><br>
   <samp>Name: Bob</samp><br>
   <kbd>return</kbd><br>
   <samp>Hello, Bob!</samp>

   ## Tab completion

   %Minibuffer offers extensive tab completion. The %Prompt attribute is the
    main means of choosing which completion to offer.

   ### Ad hoc completion

   One can provide a list of completions.

   ```
   [Command]
   public string TweetScreenshot([Prompt("To twitter account: ",
                                         completions = new []
                                           { "@shanecelis",
                                             "@stupidmassive",
                                             "@unormal" })]
                                         string str) {
     Minibuffer.instance.ExecuteCommand("capture-screenshot");
     // ... Magic twitter code not included ...
     return "Tweeted screenshot to " + str + ".";
   }
   ```
   <kbd>M-x tweet-screenshot</kbd><br>
   <samp>To twitter account: </samp><br>
   <kbd>tab</kbd><br>
   <samp>->@@shanecelis<-</samp><br>
   <samp>@@stupidmassive</samp><br>
   <samp>@@unormal</samp><br>
   <kbd>downarrow</kbd><br>
   <samp>@@shanecelis</samp><br>
   <samp>->@@stupidmassive<-</samp><br>
   <samp>@@unormal</samp><br>
   <kbd>return</kbd><br>
   <samp>To twitter account: @@stupidmassive</samp><br>
   <kbd>return</kbd><br>
   <samp>Tweeted screenshot to @@stupidmassive.</samp>

   ### Require match?

   If `requireMatch` is set to true, the user input must match one of the
   completions, and it will not let the user continue. The user may always quit
   with <kbd>C-g</kbd> or <kbd>escape</kbd>.

   <kbd>M-x tweet-screenshot</kbd><br>
   <samp>To twitter account: </samp><br>
   <kbd>@@curtisaube return</kbd><br>
   <samp>To twitter account: @@curtisaube [No match]</samp><br>
   <kbd>return</kbd><br>
   <samp>To twitter account: @@curtisaube [No match]</samp><br>
   <kbd>C-g</kbd><br>
   <samp>Quit.</samp>

   If `requireMatch` is set to false, the user input does not have to match anything.

   <kbd>M-x tweet-screenshot</kbd><br>
   <samp>To twitter account: </samp><br>
   <kbd>@@curtisaube</kbd><br>
   <samp>Tweeted screenshot to @@curtisaube.</samp>

   If `requireMatch` is not set, then it will try to choose a smart default: if
   there's a completer, it will default to true.

   ### Completer

   %Minibuffer has many builtin completers: "buffer", "command", "directory",
   "file", "variable", AnimationClip, AudioClip, Color, Component, Font,
   GameObject, GUISkin, Material, Mesh, PhysicMaterial, Scene, Shader, Sprite,
   and Texture. And one can write their own using ICompleter. Type <kbd>M-x
   describe-completers</kbd> to see all available completers.

   Suppose one wanted to select a directory for screenshots.

   ```
   [Command]
   public string ScreenshotDirectory([Prompt("Screenshot directory: ",
                                             completer = "directory",
                                             input = "~/")]
                                     string dirName) {
     this.screenshotDirectory = dirName;
     return "Screenshot directory is " + dirName;
   }
   ```

   <kbd>M-x screenshot-directory</kbd><br>
   <samp>Screenshot directory: ~/</samp><br>
   <kbd>D tab</kbd><br>
   <samp>->~/Desktop<-</samp><br>
   <samp>~/Documents</samp><br>
   <samp>~/Downloads</samp><br>
   <kbd>return</kbd><br>
   <samp>Screenshot directory is /Users/shane/Desktop</samp>

   ## Coercion

   In addition to tab completion %Minibuffer offers type coercion and look
   up. So if a command needs, say, an integer, it does not need to do any string
   handling of its own. Suppose we had a command that plays a guess a number game.

   ```
   [Command("quadrapus-color",
            description = "Set color of quadrapus")]
   public string SetColor([Prompt(requireMatch = false)]
                          Color c) {
     quadrapus
       .GetComponentsInChildren<MeshRenderer>()
       .Each(x => x.material.SetColor("_Color", c));
     return "Changed color to " + c + ".";
   }
   ```

   ```
   [Command("guess-a-number")]
   public string GuessANumber(int guess) {
     if (guess > 42)
       return "Too high.";
     else if (guess < 42)
       return "Too low.";
     else
       return "You got it! 42, the answer to life, the universe and everything!";
   }
   ```

   <kbd>M-x guess-a-number</kbd><br>
   <samp>int guess: </samp><br>
   <kbd>blah return</kbd><br>
   <samp>int guess: blah [Cannot coerce to int]</samp><br>
   <kbd>C-a C-k 10 return</kbd><br>
   <samp>Too low.</samp>

   One does not actually need to specify a `Prompt` because it can infer one
   from the parameter type and name. However, it's instructive to see what
   the equivalent prompt is.

   ```
   [Command("guess-a-number")]
   public string GuessANumber([Prompt("int guess: ",
                                      requireCoerce = true,
                                      completer = "int",
                                      desiredType = typeof(int),
                              int guess) {
     ...
   }
   ```
   In general, the `desiredType` property never needs to be set manually.

   ### Require Coercion?

   The property `requireCoerce` is like `requireMatch`. It requires that the
   input provided by the user can be coerced or looked up to the `desiredType`.
   If the parameter type is not `string` then a coercion is often mandatory, and
   the default for `requireCoerce` reflects that.

   ## Ignore Parameters

   One can mark certain parameters to be _ignored_ so the user is never prompted
   for them. The method will instead receive a `null` if it's an object or the
   default value if it's a struct or enum. Optional parameters are by default
   _ignored_ so these two code samples behave the same as commands:

   ```
   [Command]
   public void DontAsk1([Prompt(ignore = true)] string what) { ... }
   ```

   ```
   [Command]
   public void DontAsk2(string what = null) { ... }
   ```

   ## Default Value

   One can provide a default value for a parameter.

   Even though `defaultValue` is an `object`, there are restrictions on what can
   be specified as attributes. This field has more practical uses when coupled
   with the Minibuffer.Read<T>(Prompt) method. And one can get a similar effect by using optional
   parameters anyhow. For instance these two code samples behave the same as
   commands:

   ```
   [Command]
   public void SomethingAbout1([Prompt(defaultValue = "Mary")] string what) { ... }
   ```

   ```
   [Command]
   public void SomethingAbout2(string what = "Mary") { ... }
   ```
 */
/** \ingroup MIT */
[AttributeUsage(AttributeTargets.Parameter)]
public class Prompt : Attribute {

  /** Create a prompt. */
  public Prompt() {}

  /** Ask the user for something with the given prompt. */
  public Prompt(string prompt) {
    this.prompt = prompt;
  }

  /* Copy a prompt. */
  public Prompt(Prompt other) {
    prompt = other.prompt;
    input = other.input;
    history = other.history;
    completer = other.completer;
    filler = other.filler;
    _requireMatch = other._requireMatch;
    _requireCoerce = other._requireCoerce;
    completions = other.completions;
    ignore = other.ignore;
    defaultValue = other.defaultValue;
    desiredType = other.desiredType;
  }

  /** The user prompt, e.g.\ "Your profession: "*/
  public string prompt;
  /** The default input if any, e.g.\ "Wizard" */
  public string input;
  /** The name of the command history to use, e.g.\ "profession-choice". New
      names will create new histories. */
  public string history;
  /** The name of the completer to use, e.g.\ "profession". May be inferred based on the type of
      argument. */
  public string completer;
  // XXX I don't want to talk about it yet.
  public string filler;
  internal bool? _requireMatch;
  /** Is a match with the completer required? */
  public bool requireMatch {
    get { if (_requireMatch.HasValue)
        return _requireMatch.Value;
      else
        return false;
    }
    set {
      _requireMatch = value;
    }
  }
  internal bool? _requireCoerce;
  /** Must the result coerce to the desiredType? (Not applicable if desiredType
      is a string.) */
  public bool requireCoerce {
    get { if (_requireCoerce.HasValue)
        return _requireCoerce.Value;
      else
        return false;
    }
    set {
      _requireCoerce = value;
    }
  }
  /** An ad hoc list of completions, e.g.\ `new [] { "*random*", "Wizard",
      "Thief", "Warrior" }`. If `completion` and `completions` are both provided,
      the completers will be "concatenated" together. This allows one to provide
      some options that may not belong in the completer. For instance, M-x
      describe-variable uses this to add an '*all*' option.

      \code{.cs}
      [Command("describe-variable",
               description = "Show type, class, and description for a variable.")]
      public void DescribeVariable([Prompt("Describe variable: ",
                                           completions  = new [] { "*all*" },
                                           completer    = "variable")]
                                           string name) {
        if (name == "*all*") {
        ...
      \endcode

      In some cases, you may wish to add an option that won't be coercable to
      the `desiredType`. For instance, <kbd>M-x describe-game-object</kbd> adds a
      "*scene*" completion to essentially describe all the GameObjects in the
      scene instead of one GameObject. So "*scene*" will not coerce to a
      GameObject. In this case, we use PromptResult<GameObject> which holds two
      values: the string input given by the user, and what it was coerced to by
      the completer. This shows one example of how construct such commands.

      \code{.cs}
      [Command("describe-game-object",
               description = "Show a game object's components and children.",
               keyBinding = "C-h o")]
      public void DescribeGameObject([Prompt("Describe GameObject: ",
                                             completions = new [] { "*scene*" },
                                             completer = "GameObject",
                                             requireCoerce = false)]
                                     PromptResult<GameObject> pr) {
        string input = pr.str;
        GameObject go = pr.obj;
        if (go != null) {
          // Got a game object.
          ...
        } else if (input == "*scene*") {
          // Show scene.
          ...
        }
        ...
      \endcode
  */
  public string[] completions;
  /** If ignore is true, do not ask user for the argument and supply null by
      default. */
  public bool ignore;

  /** If defaultValue is not null, do not ask the user for the argument and
      supply this value instead. */
  public object defaultValue;

  /** What type should the input be coerced to? The desiredType is often
      determined by the parameter type rather than setting it by hand. */
  public Type desiredType;
}

}
