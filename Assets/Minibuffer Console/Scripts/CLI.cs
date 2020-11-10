/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/


using UnityEngine;

using UnityEngine.Assertions;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using SeawispHunter.MinibufferConsole.Extensions;
using Sprache;

namespace SeawispHunter.MinibufferConsole {

public class ShortFlags {
  public PosAwareStr flags;
}

public class LongFlag {
  public PosAwareStr flag;
}

public class CLIElement : Union<PosAwareStr, ShortFlags, LongFlag> {
  public CLIElement(PosAwareStr a) : base(a) { }
  public CLIElement(ShortFlags a) : base(a) { }
  public CLIElement(LongFlag a) : base(a) { }

  public PosAwareStr posAwareStr {
    get { return Match(pas => pas, sf => sf.flags, lf => lf.flag); }
  }

  public bool isFlag {
    get { return Match(pas => false, sf => true, lf => true); }
  }

  public string Value {
    get { return posAwareStr.Value; }
  }
}

public class CompoundMessage {
  public List<string> messages = new List<string>();
  public void Add(string msg) {
    messages.Add(msg);
  }

  public void Add(string msgFormat, params object[] args) {
    messages.Add(string.Format(msgFormat, args));
  }

  public override string ToString() {
    // lower case the first letter in every message after the first.
    return string.Join("; ", messages.ToArray()).FirstLetterToUpperCase();
  }
}

/*
  Run this list of commands when this behaviour starts.  If this
  is a WebGL build will look for a 'playback' GET parameter.
*/
[Group(tag = "built-in")]
public class CLI : MonoBehaviour {

  // https://www.thomaslevesque.com/2017/02/23/easy-text-parsing-in-c-with-sprache/
  private static readonly Parser<char> _quotedText =
    Parse.AnyChar.Except(Parse.Char('"'));

  private static readonly Parser<char> escapedChar =
    from _ in Parse.Char('\\')
    from c in Parse.AnyChar
    select c;

  private static readonly Parser<string> quotedString =
    from open in Parse.Char('"')
    from text in escapedChar.Or(_quotedText).Many().Text()
    from close in Parse.Char('"')
    select text;

  public static readonly Parser<PosAwareStr> quotedText =
    quotedString.MyToken();

  public static readonly Parser<PosAwareStr> bareWord = Parse.CharExcept(' ').AtLeastOnce().Text().MyToken();
  public static readonly Parser<PosAwareStr> argument = quotedText.Or(bareWord);

  public static readonly Parser<PosAwareStr> shortFlags =
    from dash in Parse.Char('-')
    from flags in Parse.LetterOrDigit.AtLeastOnce().Text().MyToken()
    select flags;

  public static readonly Parser<PosAwareStr> longFlag =
    from dash in Parse.String("--")
    from flag in bareWord
    select flag;

  public static readonly Parser<ShortFlags> shortFlagsT =
    from flags in shortFlags
    select new ShortFlags { flags = flags };

  public static readonly Parser<LongFlag> longFlagT =
    from flag in longFlag
    select new LongFlag { flag = flag };

  public static readonly Parser<IEnumerable<PosAwareStr>> commandLine =
    (from command in argument // Identifier
     from arguments in argument.Many()
     from trailing in Parse.WhiteSpace.Many()
     select arguments.Prepend(command));

  public static readonly Parser<IEnumerable<CLIElement>> commandLineT =
    (from command in argument.Wrapped() // Identifier
     from arguments in longFlagT.Wrapped().Or(shortFlagsT.Wrapped()).Or(argument.Wrapped()).Many()
     from trailing in Parse.WhiteSpace.Many()
     select arguments.Prepend(command));

  // public static readonly Parser<IEnumerable<PosAwareStr>> RichCommandLine =
  //   (from command in Argument
  //    from arguments in Argument.Many()
  //    select arguments.Prepend(new CLIElement(command)));

  private Keymap cliKeymap;
  private Minibuffer minibuffer;
  public MinibufferListing minibufferExtensions;
  /* Nomenclature

     Input looks like this:

     $ command arg1 arg2 ...

     In order to unify the handling, "terms" means the following:

     $ term0 term1 term2 term3 ...
   */
  private Func<List<PromptInfo>, List<PromptInfo>> termFilter = Identity;
  private List<PromptInfo> termPromptsUnfiltered;
  private List<PromptInfo> termPrompts;
  private List<PosAwareStr> parsedCmd;
  private List<string> termNames = new List<string>();
  private int termIndex;
  private PosAwareStr selection;
  // private string currentCommand;
  private CompoundMessage stdout = new CompoundMessage();
  private CompoundMessage stderr;//= new CompoundMessage();

  private PromptInfo commandPrompt = new PromptInfo(new Prompt("$ ") {
    completer = "command-no-bind-only",
    history = "command"
  });


  void Start() {
    stderr = stdout;
    Minibuffer.Register(this);
    Minibuffer.With(m => {
        minibuffer = m;
        cliKeymap = m.GetKeymap("cli", true);
        cliKeymap.priority = 1100;
        cliKeymap.enabled = false;
      });
  }

  public bool IsValidInput(string commandLineInput) {
    // No errors?
    // Debug.Log("is valid input: " + commandLineInput);
    var cmd = commandLineInput; //minibuffer.gui.input.text;
    int caret = minibuffer.gui.input.caretPosition;
    int errors = 0;
    if (! TryParse(cmd, caret)) {
      // Error.
      // minibuffer.MessageInline(" [Error: Unable to parse command line]");
      // return false;
      errors++;
    }

    // if (termPrompts.Count != parsedCmd.Count) {
    //   if (parsedCmd.Count > termPrompts.Count) {
    //     minibuffer.MessageInline(" [too many arguments]");
    //   } else if (parsedCmd.Count < termPrompts.Count) {
    //     minibuffer.MessageInline(" [too few arguments]");
    //   } else {
    //   }
    //   errors++;
    //   // return false;
    // }
    // Debug.Log(PrintParseResults(parsedCmd) + "\ncaret: " + caret);
    var input = selection != null ? selection.Value : "";
    if ((minibuffer.editState.prompt.requireMatch || input.IsZull())
        && ! minibuffer.CompleterHasMatch(input)) {
      if (minibuffer.gui.autocomplete.window.activeInHierarchy) {
        minibuffer.UseElement();
        errors++;
        // return false;
      }
    }
    bool ok = TabComplete(commandLineInput, true) == 0;
    // if (ok) {
    //   // XXX Yuck!
    //   minibuffer.editState.prompt.requireMatch = false;
    //   minibuffer.editState.prompt.requireCoerce = false;
    // } else {
    //   // Should I show the errors?
    // }
    ok = ok && errors == 0;
    // if (ok)
    //   minibuffer.editState.prompt.history = "cli";
    return ok;
  }

  /*
    GOOD
    $ quadrapus-material f| return

    no go.

    BAD
    $ quadrapus-material | return

    It's let thru.
    */

  /*
    Returns true if the command is available.
  */
  private bool TermPromptsFor(string command, out List<PromptInfo> prompts) {
    CommandInfo ci;
    if (minibuffer.commands.TryGetValue(command, out ci)) {
      prompts = ci.GeneratePrompts()
        .Where(p => p.defaultValue == null)
        .Prepend(commandPrompt)
        .ToList();

      termNames = new List<string>();
      var args = termNames;
      args.Add("command-name");
      var parameters = ci.methodInfo.GetParameters();
      for (int i = 0; i < parameters.Length; i++) {
        object[] universalAttrs;
        object[] currentAttrs;
        universalAttrs = parameters[i].GetCustomAttributes(typeof(UniversalArgument), false);
        currentAttrs = parameters[i].GetCustomAttributes(typeof(Current), false);
        if (universalAttrs.Length != 0) {
          var paramInfo = parameters[i];
          if (paramInfo.ParameterType == typeof(int?)
              || paramInfo.ParameterType == typeof(int)) {
            // universalInteger = true;
          } else if (paramInfo.ParameterType == typeof(bool)) {
            // universalBool = true;
          } else {
            throw new MinibufferException("UniversalArgument can only be int, int?, or bool not "
                                          + paramInfo.ParameterType.PrettyName());
          }
        } else if (! parameters[i].IsOptional
                   && currentAttrs.Length == 0) {
          args.Add(parameters[i].ParameterType.PrettyName() + " " + parameters[i].Name);
        }
      }
      return true;
    } else {
      prompts = new List<PromptInfo>() { commandPrompt };
      return false;
    }
  }

  // M-n sometimes produces the small tilde, hex code cb9c, unicode hex 2dc, decimal 732
  [Command(description = "Retrieve next argument from history",
           keyBindings = new [] { "M-N", "M-S-downarrow" },
           tag = "bind-only")]
  /* public*/ void HistoryNextArgument() {
    HistoryMove(true);
  }

  [Command(description = "Retrieve prior argument from history",
           keyBindings = new [] { "M-P", "M-S-uparrow" },
           tag = "bind-only" )]
  /*public*/ void HistoryPreviousArgument() {
    HistoryMove(false);
  }

  private void HistoryMove(bool next) {

    var cmd = minibuffer.input;
    int caret = minibuffer.gui.input.caretPosition;
    // int errors = 0;
    if (! TryParse(cmd, caret)) {
      // Unknown command, possibly blank.
      // minibuffer.MessageInline(" [Cannot parse for history]");
      // return;
      // termIndex = 0;
    }
    var input = selection != null ? selection.Value : "";
    // var originalHistory = minibuffer.GetHistory(minibuffer.editState.prompt.history);
    PromptInfo prompt;
    if (termIndex < termPrompts.Count)
      prompt = termPrompts[termIndex];
    else
      prompt = termPrompts[0];
    var history = minibuffer.GetHistory(prompt.history);
    if (minibuffer.editState.history != history) {
      minibuffer.editState.TeardownHistory(null);
      minibuffer.editState.SetupHistory(history);
    }

    if (minibuffer.editState.history != null) {
      var queuing = next
        ? minibuffer.editState.prevHistory
        : minibuffer.editState.nextHistory;
      var dequeuing = ! next
        ? minibuffer.editState.prevHistory
        : minibuffer.editState.nextHistory;
      if (dequeuing.Count > 0) {
        // queuing.Enqueue(termIndex != 0 ? input : cmd);
        queuing.Enqueue(input);
        // var initialTermIndex = termIndex;
        // minibuffer.editState.replaceInput(minibuffer.editState.prevHistory.Dequeue());
        ReplaceInput(dequeuing.Dequeue(), false);
        // if (initialTermIndex == 0) {
        //   // Set cursor to beginning.
        //   minibuffer.gui.input.caretPosition = 0;
        // }
      } else {
        minibuffer.MessageInline(" [No {0} argument history]", next ? "further" : "prior");
      }
    } else {
      minibuffer.MessageInline(" [No argument history]");
    }
    // minibuffer.editState.TeardownHistory(null);
    // minibuffer.editState.SetupHistory(originalHistory);
  }

  // private void UpdateTermPrompts() {

  //   string cmd = minibuffer.gui.input.text;
  // }

  private int caretOnWhichTerm(List<PosAwareStr> parsedCmd, int caret/*Position*/) {
    PosAwareStr selection;
    int termIndex;
    // Which argument are we on?
    // Debug.Log("caret " + caret);
    var caretOnArguments = parsedCmd
      .Where(r => caret >= r.Pos.Column - 1
               && caret <= r.Pos.Column - 1 + r.Length);
    if (caretOnArguments.Any()) {
      // Debug.Log("inside args");
      selection = caretOnArguments.First();
      termIndex = parsedCmd.IndexOf(selection);
    } else {
      var caretAfterArguments = parsedCmd
        // .Where(r => caret > r.Pos.Column - 1 + r.Length);
        .OrderBy(r => caret - r.Pos.Column - 1 + r.Length);
      var prevSelection = caretAfterArguments.FirstOrDefault();
      if (prevSelection != null) {
        // Debug.Log("after arg: " + prevSelection.Value);
        termIndex = parsedCmd.IndexOf(prevSelection) + 1;
      } else {
        // Debug.Log("no prev selection found.");
        termIndex = 0;
      }
    }
    // Debug.Log("termIndex = " + termIndex);
    return termIndex;
  }

  private void ReplaceInput(string match) {
    ReplaceInput(match, true);
  }
  /*
    $ two-hello bye | tab

    Not working on second argument.
    */
  private void ReplaceInput(string match, bool addSpaceIfNeeded) {
    // We should reparse. Things could have changed.
    var cmd = minibuffer.gui.input.text;
    int caret = minibuffer.gui.input.caretPosition;
    // Debug.Log(PrintParseResults(parsedCmd) + "\ncaret: " + caret);
    if (cmd.IsZull() || ! TryParse(cmd, caret)) {
      // Hmmm... parse error.  What do?
      if (termIndex != 0)
        stderr.Add("unknown command");
      // It's probably on the first argument.
      minibuffer.gui.input.text = match;
      minibuffer.gui.input.MoveTextEnd(false);
      minibuffer.gui.input.Deselect();
      return;
    }
    var command = parsedCmd[0].Value;
    if (! TermPromptsFor(termIndex == 0 ? match : command, out termPromptsUnfiltered)) {
      // Error getting term prompts.
    }
    termPrompts = termFilter(termPromptsUnfiltered);
    bool addSpaceMaybe = (addSpaceIfNeeded //termIndex < termPrompts.Count - 1
                          && minibuffer.CompleterHasMatch(match));
    bool termComplete = minibuffer.CompleterHasMatch(match);
    match = QuoteIfNeeded(match);
    // Debug.Log("addSpaceMaybe " + addSpaceMaybe);
    if (selection != null) {
      if (addSpaceMaybe) {
        // What's the character after the match?
        var lastIndex = selection.Pos.Column - 1 + selection.Length;
        if (lastIndex >= minibuffer.gui.input.text.Length
            || (lastIndex < minibuffer.gui.input.text.Length
                && minibuffer.gui.input.text[lastIndex] != ' '))
          match += " ";
      }
      if (selection.Value != match) {
        minibuffer.gui.input.text
          = minibuffer.gui.input.text.Replace(selection.Pos.Column - 1, selection.Length, match);
        minibuffer.gui.input.caretPosition = selection.Pos.Column - 1 + match.Length;
      }
    } else {
      minibuffer.gui.input.text += match;
      if (addSpaceMaybe)
        minibuffer.gui.input.text += " ";
      minibuffer.gui.input.MoveTextEnd(false);
      minibuffer.gui.input.Deselect();
    }

    if (termComplete) {
      // We finished a term.  We should probably reparse.  Otherwise we'll complete wrong.
    }
  }

  /*
    Returns true if the command is known.
  */
  public bool TryParse(string cmd, int caret) {

    parsedCmd = ParseCommandLineWithoutFlags(cmd).ToList();
    // Debug.Log(PrintParseResults(parsedCmd) + "\ncaret: " + caret);
    termIndex = caretOnWhichTerm(parsedCmd, caret);
    if (termIndex >= 0 && termIndex < parsedCmd.Count)
      selection = parsedCmd[termIndex];
    else
      selection = null;
    // Debug.Log("term index " + termIndex);
    var command = parsedCmd.Count > 0 ? parsedCmd[0].Value : "";
    var knownCommand = TermPromptsFor(command, out termPromptsUnfiltered);
    termPrompts = termFilter(termPromptsUnfiltered);
    if (termIndex < termPrompts.Count) {
      minibuffer.editState.prompt = PreserveHistory(termPrompts[termIndex]);
    } else {
      minibuffer.editState.prompt = PreserveHistory(new PromptInfo());
    }
    return knownCommand;
    // return true;
  }

  private PromptInfo PreserveHistory(PromptInfo pi) {
    // var pip = new PromptInfo(pi);
    // pip.history = "cli";
    // return pip;
    return pi;
  }

  [Command(keyBinding = "C-h p", keymap = "core")]
  /*public*/ void ShowPrompt() {
    minibuffer.ToBuffer("Prompt", minibuffer.editState.prompt.ToString());
  }

  [Command("cli-complete",
           keyBinding = "tab",
           description = "Tab complete for the CLI",
           tag = "bind-only")]
  public int TabComplete(string cmd = null, bool quietOnSuccess = false) {
    int caret = minibuffer.gui.input.caretPosition;
    if (cmd == null)
      cmd = minibuffer.gui.input.text;
    int errors = 0;

    if (cmd.IsZull()) {
      minibuffer.editState.prompt = PreserveHistory(commandPrompt);
      // minibuffer.editState.prompt = commandPrompt;
      // minibuffer.MessageInline("doing regular completer");
      minibuffer.TabComplete("");
      errors++;
      return errors;
    }

    bool knownCommand = TryParse(cmd, caret);

    if (! knownCommand) {
      if (termIndex != 0)
        stderr.Add("unknown command");
      errors++;
    }


    // int argCount = termPrompts.Count - 1;
    // if (termIndex == 0) {
    //   if (termPrompts.Count != parsedCmd.Count) {

    //     if (argCount != 0) {
    //       stderr.Add("requires {0} arguments", argCount);
    //     } else {
    //       stdout.Add("no arguments required");
    //     }
    //     errors++;
    //   }
    // }

    // if (termPrompts.Count != parsedCmd.Count) {
    //   errors++;
    // }
    if (termIndex < termPrompts.Count) {
      if (termIndex < termNames.Count && termIndex != 0)
        stdout.Add(termNames[termIndex]);
      // if (termIndex != 0) {
      //   // XXX Maybe add this to the end.
      //   stdout.Add("argument {0} of {1}", termIndex, termPrompts.Count - 1);
      // }
    } else {
    }
    if (parsedCmd.Count > termPrompts.Count) {
      stderr.Add("too many arguments");
      errors++;
    } else if (parsedCmd.Count < termPrompts.Count) {
      stderr.Add("too few arguments");
      errors++;
    } else {
      // Just right.
      // stdout.Add("argument {0} of {1}", termIndex, termPrompts.Count - 1);
    }
    // else {
    //   if (termIndex > termPrompts.Count) {
    //     stderr.Add("too many arguments");
    //     errors++;
    //   } else 
    //   if (termIndex == termPrompts.Count && errors == 0) {
    //     stdout.Add("ready");
    //     // errors++;
    //   }
    // }

    if (termIndex == termPrompts.Count && errors == 0) {
      stdout.Add("ready");
      // errors++;
    }
    string inputCopy;
    if (selection != null) {
      inputCopy = selection.Value;
    } else if (minibuffer.editState.prompt != null
               && minibuffer.editState.prompt.input != null) {
      // Let's complete the default input.
      minibuffer.editState.replaceInput(minibuffer.editState.prompt.input);
      stderr.messages.Clear();
      return errors;
    } else {
      inputCopy = "";
    }

    // default input?
    var matches = minibuffer.TabComplete(inputCopy);
    var editState = minibuffer.editState;
    if (editState.prompt.completerEntity.completer != null) {
      if (matches.Count > 4) {
        // Humans can count to four natively.
        stdout.Add("{0} matches", matches.Count);
      }

      if (matches.Count == 1) {
        minibuffer.showAutocomplete = false;
        if (inputCopy == matches[0])
          stdout.Add("sole completion");
        editState.replaceInput(matches[0]);
      } else if (matches.Count == 0) {
        minibuffer.showAutocomplete = false;
        stderr.Add("no match");
        // We should distinguish between matches and coercions.
        if (editState.prompt.requireCoerce && ! minibuffer.CanCoerce(inputCopy)) {
          stderr.Add("cannot coerce");
          errors++;
        } else if (editState.prompt.desiredType != typeof(string)) {
          stdout.Add("can coerce to {0}",
                      editState.prompt.desiredType.PrettyName());
        }
      } else {
        if (matches.Contains(inputCopy))
          stdout.Add("complete but not unique");
      }

      if (editState.prompt.requireMatch
          && ! minibuffer.CompleterHasMatch(inputCopy))
        errors++;

    } else if (editState.prompt.requireCoerce
               && editState.prompt.completerEntity.coercer != null) {
      //var coercer = editState.completerEntity.coercer;
      if (/*editState.requireCoerce && */! minibuffer.CanCoerce(inputCopy)) {
        stderr.Add("cannot coerce to {0}",
                    editState.prompt.desiredType.PrettyName());
        errors++;
      }
      // Showing an inline message for something that's ok seems like it's over
      // doing it.

      // else if (editState.desiredType != typeof(stdout.Add("can coerce to {0}",
      // editState.desiredType.PrettyName());
    } else {
      minibuffer.showAutocomplete = false;
      if (termIndex < termPrompts.Count)
        stdout.Add("no completer");
    }
    // } else {
    //   Debug.Log("No term index");
    //   // We're probably on a space.
    // }
    if (quietOnSuccess && errors == 0) {
      stdout.messages.Clear();
    } else {
      FlushOutput();
    }
    return errors;
    // minibuffer.MessageInline(" [{0}]", selection.Value);
  }

  private void FlushOutput() {
    var msgs = stderr.messages; //stderr.messages.Concat(stdout.messages);
    if (msgs.Any()) {
      minibuffer.MessageInline(" [{0}]", string.Join("; ", msgs.ToArray()));
      stderr.messages.Clear();
      stdout.messages.Clear();
    }
  }

  internal static string QuoteIfNeeded(string str) {
    if (str.Contains(" ") && !(str.Length > 2 && str.First() == '"' && str.Last() == '"'))
      return "\"" + str.Replace("\"", "\\\"") + "\"";
    else
      return str;
  }

  [Command("cli",
           description = "A command line interface for Minibuffer.",
           keyBinding = "$",
           keymap = "core")]
  public void Shell() {
    cliKeymap.enabled = true;
    // minibuffer.gui.main.visible = true;
    minibuffer.editState.replaceInput = ReplaceInput;
    minibuffer.editState.isValidInput = IsValidInput;
    minibuffer
      .Read<PromptResult>(PreserveHistory(commandPrompt))
      .Then(pr => {
          var commandLine = pr.str;
          // Debug.Log("command line: " + commandLine);
          cliKeymap.enabled = false;
          _ShellWithFlags(commandLine);
        })
      .Catch(ex => {
          if (ex is AbortException) {
            // Aborts are normal.
          } else {
            Debug.LogException(ex);
          }
          cliKeymap.enabled = false;
        });
  }

  private static List<PromptInfo> Identity(List<PromptInfo> prompts) {
    return prompts;
  }

  private static List<PromptInfo> AcceptNoArguments(List<PromptInfo> prompts) {
    return new List<PromptInfo>() { prompts[0] };
  }

  private IEnumerable<PosAwareStr> ParseCommandLineWithoutFlags(string cmd) {
    // var parsedCmd = commandLineT.Parse(cmd).ToList();
    // return parsedCmd
    //   .Where(x => x.Match(pas => true,
    //                       sf => false,
    //                       lf => false))
    //   .Select(x => x.posAwareStr);
      if (cmd.IsZull()) {
        return Enumerable.Empty<PosAwareStr>();
      }

      var parsedCmd = commandLineT.Parse(cmd).ToList();
      // Debug.Log("parsed: " + string.Join(", ", parsedCmd.Select(r => r.Value).ToArray()));
      var properArguments = new List<CLIElement>();
      if (parsedCmd.Count == 0)
        return properArguments.Select(x => x.posAwareStr);
      termFilter = Identity;
      // properArguments.Add(parsedCmd[0]);
      // Let's handle flags.
      // -u number
      // -U (boolean toggle)
      for (int i = 0; i < parsedCmd.Count; i++) {
        if (parsedCmd[i].isFlag) {
          var shortFlags = parsedCmd[i].Match(pas => null,
                                              sf => sf.flags,
                                              lf => null);
          if (shortFlags != null) {
            foreach(char c in shortFlags.Value) {
              switch (c) {
                case 'u':
                  // we take a number (and therefore an argument).
                  int num;
                  if (i + 1 >= parsedCmd.Count) {
                    stderr.Add("-u expects an integer argument");
                    continue;
                  } else if (int.TryParse(parsedCmd[i + 1].Value, out num)) {
                    // prefixArgument = num;
                    i++;
                  } else {
                    stderr.Add(string.Format("-u expects an integer argument not '{0}'", parsedCmd[i + 1].Value));
                    continue;
                  }
                  break;
                case 'U':
                  // We're a boolean toggle.
                  // prefixArgument = 1;
                  break;
                case 'h':
                  // Also add on its command line usage.
                  termFilter = AcceptNoArguments;
                  break;
              }
            }
          }

          var longFlag = parsedCmd[i].Match(pas => null,
                                            sf => null,
                                            lf => lf.flag);
          if (longFlag != null) {
            switch (longFlag.Value) {
              case "help":
                // Also add on its command line usage.
                termFilter = AcceptNoArguments;
                break;
            }
          }
          continue;
        }
        properArguments.Add(parsedCmd[i]);
      }
      return properArguments.Select(x => x.posAwareStr);
  }

  public void _ShellWithFlags([Prompt("$ ", completer = "command")] string cmd) {
    try {
      var parsedCmd = commandLineT.Parse(cmd).ToList();
      // Debug.Log("parsed: " + string.Join(", ", parsedCmd.Select(r => r.Value).ToArray()));
      var command = parsedCmd[0].Value;
      CommandInfo ci;
      int? prefixArgument = null;
      var properTerms = new List<CLIElement>();
      // .Where(r => ! r.isFlag)
      // .ToList();
      // Let's handle flags.
      // -u number
      // -U (boolean toggle)
      for (int i = 0; i < parsedCmd.Count; i++) {
        if (parsedCmd[i].isFlag) {
          var shortFlags = parsedCmd[i].Match(pas => null,
                                              sf => sf.flags,
                                              lf => null);
          if (shortFlags != null) {
            foreach(char c in shortFlags.Value) {
              switch (c) {
                case 'u':
                  // we take a number (and therefore an argument).
                  int num;
                  if (i + 1 >= parsedCmd.Count) {
                    minibuffer.Message("-u expects an integer argument");
                    return;
                  } else if (int.TryParse(parsedCmd[i + 1].Value, out num)) {
                    prefixArgument = num;
                    i++;
                  } else {
                    minibuffer.Message("-u expects an integer argument not '{0}'", parsedCmd[i + 1].Value);
                    return;
                  }
                  break;
                case 'U':
                  // We're a boolean toggle.
                  prefixArgument = 1;
                  break;
                case 'h':
                  // Also add on its command line usage.
                  DescribeCommand(command);
                  return;
              }
            }
          }

          var longFlag = parsedCmd[i].Match(pas => null,
                                            sf => null,
                                            lf => lf.flag);
          if (longFlag != null) {
            switch (longFlag.Value) {
              case "help":
                // Also add on its command line usage.
                DescribeCommand(command);
                return;
            }
          }
          continue;
        } else {
          properTerms.Add(parsedCmd[i]);
        }
      }
      // Debug.Log("no flags: " + string.Join(", ", properTerms.Select(r => r.Value).ToArray()));
      var argumentCount = properTerms.Count - 1;
      minibuffer.currentPrefixArg = prefixArgument;
      if (minibuffer.commands.TryGetValue(command, out ci)) {
        var prompts = ci.GeneratePrompts();
        var requiredPrompts
          = prompts
          .Where(p => p.defaultValue == null)
          .ToArray();
        if (argumentCount > requiredPrompts.Length) {
          minibuffer.Message("Too many arguments.");
          return;
        } else if (argumentCount < requiredPrompts.Length) {
          minibuffer.Message("Too few arguments.");
          return;
        }
        var parameters = ci.methodInfo.GetParameters();
        System.Object[] arguments = new System.Object[parameters.Length];
        // Debug.Log("arguments length " + arguments.Length);
        Assert.IsTrue(arguments.Length == prompts.Length);
        // Assert.IsTrue(properTerms.Count == prompts.Length + 1);
        for (int i = 0, j = 1; i < prompts.Length; i++) {
          if (prompts[i].defaultValue != null) {
            arguments[i] = prompts[i].defaultValue;
            // Debug.Log(string.Format("argument {0} has default value '{1}'", i, arguments[i]));
          } else if (j < parsedCmd.Count && parameters[i].ParameterType == typeof(string)) {
            arguments[i] = properTerms[j].Value;
            // Add it to the history.
            var history = minibuffer.GetHistory(prompts[i].history);
            history.Add(properTerms[j].Value);
            j++;
          } else if (j < parsedCmd.Count && prompts[i].completerEntity.coercer != null) {
            arguments[i] = prompts[i].completerEntity.coercer.Coerce(properTerms[j].Value, parameters[i].ParameterType);
            var history = minibuffer.GetHistory(prompts[i].history);
            history.Add(properTerms[j].Value);
            j++;
          } else {
            // We don't have a parsed thing to get what we need.
          }
        }
        // Debug.Log("delegate target" + ci.delegate_.Target);
        var result = ci.methodInfo.Invoke(ci.delegate_.Target,
                                          arguments);
        if (ci.methodInfo.ReturnType == typeof(IEnumerator)) {
          StartCoroutine((IEnumerator) result);
        } else if (ci.methodInfo.ReturnType == typeof(string)) {
          minibuffer.Message((string) result);
        }
      } else {
        minibuffer.Message("No such command: {0}", command);
      }
    } catch (System.Exception e) {
      Debug.LogError(e);
      minibuffer.Message("Error: {0}", e.Message);
    } finally {
      minibuffer.currentPrefixArg = null;
    }
  }

  public void DescribeCommand(string command) {
    HelpCommands.DescribeCommand(command, Usage(command) + "\n\n");
  }

  public string Usage(string command) {
    try {
      CommandInfo ci;
      bool universalInteger = false;
      bool universalBool = false;
      List<string> args = new List<string>();
      if (minibuffer.commands.TryGetValue(command, out ci)) {
        var parameters = ci.methodInfo.GetParameters();
        for (int i = 0; i < parameters.Length; i++) {
          object[] universalAttrs;
          object[] currentAttrs;
          universalAttrs = parameters[i].GetCustomAttributes(typeof(UniversalArgument), false);
          currentAttrs = parameters[i].GetCustomAttributes(typeof(Current), false);
          if (universalAttrs.Length != 0) {
            var paramInfo = parameters[i];
            if (paramInfo.ParameterType == typeof(int?)
                || paramInfo.ParameterType == typeof(int)) {
              universalInteger = true;
            } else if (paramInfo.ParameterType == typeof(bool)) {
              universalBool = true;
            } else {
              throw new MinibufferException("UniversalArgument can only be int, int?, or bool not "
                                            + paramInfo.ParameterType.PrettyName());
            }
          } else if (! parameters[i].IsOptional
                     && currentAttrs.Length == 0) {
            args.Add(parameters[i].ParameterType.PrettyName() + " " + parameters[i].Name);
          }
        }
        return string.Format("usage: {0} [-h{1}]{2} {3}",
                             command,
                             universalBool ? "U" : "",
                             universalInteger ? " [-u count]" : "",
                             string.Join(" ",
                                         args
                                         .Select(s => "<" + s + ">")
                                         .ToArray()));
      } else {
        return "No usage for command " + command;
      }
    } catch (System.Exception e) {
      Debug.LogError(e);
      return string.Format("Error making usage for {0}: {1}", command, e.Message);
    }
  }

  // public void _Shell([Prompt("$ ", completer = "command")] string cmd) {
  //   try {
  //     var parsedCmd = commandLine.Parse(cmd).ToList();
  //     // Debug.Log("parsed: " + string.Join(", ", parsedCmd.Select(r => r.Value).ToArray()));
  //     var command = parsedCmd[0].Value;
  //     CommandInfo ci;
  //     var argumentCount = parsedCmd.Count - 1;
  //     if (minibuffer.commands.TryGetValue(command, out ci)) {
  //       var prompts = ci.GeneratePrompts();
  //       var requiredPrompts
  //         = prompts
  //         .Where(p => p.defaultValue == null)
  //         .ToArray();
  //       if (argumentCount > requiredPrompts.Length) {
  //         minibuffer.Message("Too many arguments.");
  //         return;
  //       }
  //       else if (argumentCount < requiredPrompts.Length) {
  //         minibuffer.Message("Too few arguments.");
  //         return;
  //       }
  //       var parameters = ci.methodInfo.GetParameters();
  //       System.Object[] arguments = new System.Object[parameters.Length];
  //       // Debug.Log("arguments length " + arguments.Length);
  //       Assert.IsTrue(arguments.Length == prompts.Length);
  //       for (int i = 0, j = 1; i < prompts.Length; i++) {
  //         if (prompts[i].defaultValue != null) {
  //           arguments[i] = prompts[i].defaultValue;
  //           // Debug.Log(string.Format("argument {0} has default value '{1}'", i, arguments[i]));
  //         } else if (j < parsedCmd.Count && parameters[i].ParameterType == typeof(string)) {
  //           arguments[i] = parsedCmd[j].Value;
  //           j++;
  //         } else if (j < parsedCmd.Count && prompts[i].completerEntity.coercer != null) {
  //           arguments[i] = prompts[i].completerEntity.coercer.Coerce(parsedCmd[j].Value, parameters[i].ParameterType);
  //           j++;
  //         } else {
  //           // We don't have a parsed thing to get what we need.
  //         }
  //       }
  //       // Debug.Log("delegate target" + ci.delegate_.Target);
  //       var result = ci.methodInfo.Invoke(ci.delegate_.Target,
  //                                         arguments);
  //       if (ci.methodInfo.ReturnType == typeof(IEnumerator)) {
  //         StartCoroutine((IEnumerator) result);
  //       } else if (ci.methodInfo.ReturnType == typeof(string)) {
  //         minibuffer.Message((string) result);
  //       }
  //     } else {
  //       minibuffer.Message("No such command: {0}", command);
  //     }
  //   } catch (System.Exception e) {
  //     Debug.LogError(e);
  //     minibuffer.Message("Error: {0}", e.Message);
  //   }
  // }

  private String PrintParseResults(IEnumerable<PosAwareStr> result) {
    return "parsed: " + string.Join(", ", result.Select(r => r.Value).ToArray())
      + "\nindices: " + string.Join(", ", result.Select(r => r.Pos.Column.ToString()).ToArray())
      + "\nlength: " + string.Join(", ", result.Select(r => r.Length.ToString()).ToArray());

  }

}

static class CLIHelper {
  public static Parser<PosAwareStr> MyPositioned(this Parser<string> parser) {
    if (parser == null) throw new ArgumentNullException("parser");
    return (from s in parser
            select new PosAwareStr { Value = s})
      .Positioned();
  }

  public static Parser<PosAwareStr> MyPositioned(this Parser<PosAwareStr> parser) {
    if (parser == null) throw new ArgumentNullException("parser");
    return parser.Positioned();
  }

  public static Parser<PosAwareStr> MyToken(this Parser<string> parser) {
    if (parser == null) throw new ArgumentNullException("parser");

    return from leading in Parse.WhiteSpace.Many().Text()
      from item in parser.MyPositioned()
      from trailing in Parse.WhiteSpace.Many().Text()
      select item;
  }

  public static Parser<CLIElement> Wrapped(this Parser<PosAwareStr> parser) {
    if (parser == null) throw new ArgumentNullException("parser");
    return from item in parser
      select new CLIElement(item);
  }

  public static Parser<CLIElement> Wrapped(this Parser<ShortFlags> parser) {
    if (parser == null) throw new ArgumentNullException("parser");
    return from item in parser
      select new CLIElement(item);
  }

  public static Parser<CLIElement> Wrapped(this Parser<LongFlag> parser) {
    if (parser == null) throw new ArgumentNullException("parser");
    return from item in parser
      select new CLIElement(item);
  }

  public static string Replace(this string str, int index, int length, string substitution) {
    var sb = new StringBuilder(str);
    sb.Remove(index, length);
    sb.Insert(index, substitution);
    return sb.ToString();
  }

  public static string FirstLetterToUpperCase(this string s) {
    if (string.IsNullOrEmpty(s))
      // throw new ArgumentException("There is no first letter");
      return s;

    char[] a = s.ToCharArray();
    a[0] = char.ToUpper(a[0]);
    return new string(a);
  }
  // public static Parser<PosAwareStr> MyToken(this Parser<PosAwareStr> parser) {
  //   if (parser == null) throw new ArgumentNullException("parser");

  //   return from leading in Parse.WhiteSpace.Many().Text()
  //     from item in parser.Positioned()
  //     from trailing in Parse.WhiteSpace.Many().Text()
  //     select item;
  // }
}
}
