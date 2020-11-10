/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SeawispHunter.MinibufferConsole.Extensions;
using RSG;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace SeawispHunter.MinibufferConsole {

/**
   ![Help commands in the inspector](inspector/help-commands.png)
   Interrogate the running system about its commands and variables.

   %HelpCommands is an integrated help system for %Minibuffer.

   One can see

    1. what commands are available,
    2. what variables are defined,
    3. what key bindings are set,
    4. find out what a key is bound to,
    5. show history,
    6. show the license,
    7. show a tutorial,
    8. or show the readme.
 */
[Group("help", tag = "built-in")]
public class HelpCommands : MonoBehaviour {
  private Minibuffer m;

  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
    Minibuffer.With((m) => {
        // This line isn't necessary for a MonoBehaviour.
        //m.instances["HelpCommands"] = this;
        this.m = m;
        var keymap = m.GetKeymap("help", true);
        keymap["f1"]          = "help-for-help";
        keymap["?"]           = "help-for-help";
        keymap[
       @"/^.* ((C|ctrl)-h|\?|f1)$/"] = "describe-bindings-by-prefix";
        keymap.priority = m.GetKeymap("core").priority + 5;
      });
  }

  [Command("describe-variable",
           description = "Show type, class, and description for a variable.",
           keyBinding = "C-h v")]
  public void DescribeVariable([Prompt("Describe variable: ",
                                       completions  = new string[] { "*all*", "*user*" },
                                       completer    = "variable")]
                               string name) {
    if (name == "*all*") {
      DescribeVariables(true);
    } else if (name == "*user*") {
      DescribeVariables(false);
    } else if (name != null && m.variables.ContainsKey(name)) {
      var vi = m.variables[name];
      IPromise<object> instanceP;
      if (vi.IsStatic) {
        instanceP = Promise<object>.Resolved(null);
      } else {
        instanceP = m.interpreter.FillInstance(vi.DeclaringType);
      }
      instanceP.Done(instance => {
          var desc = vi.description;
          m.Message("{0} {1} = {2}.\nIt is defined in {3}.{4}",
                    vi.VariableType.PrettyName(),
                    vi.Name,
                    TextUtils.QuoteIfString(vi.GetValue(instance)),
                    vi.definedIn,
                    desc == null ? "" : "\nDescription: \"" + desc + "\"");
        });
    } else {
      m.Message("No such variable '{0}'.", name);
    }
  }

  public void DescribeVariables(bool includeSystem) {
    IEnumerable<KeyValuePair<string, VariableInfo>> variables = m.variables.ToList();
    if (! includeSystem) {
      variables = variables
        .Where(x => ! m.GetGroup(x.Value.group, true).tags.Contains("built-in"));
    }
    var strings = variables
      .GroupBy(x => { var t = x.Value.DeclaringType;
                      return t != null
                      ? m.GetGroup(t, true).name
                      : "*dynamic*"; })
      .Select(g => {
          string[][] table
          = g.OrderBy(x=> x.Key)
          .Select(kv => new string[] {kv.Key,
                                      kv.Value.IsStatic
                                      ? kv.Value.GetValue(null).ToString()
                                      : "N/A",
                                      kv.Value.briefDescription ?? "" })
          .Prepend(new string[] {"variable", "value", "description"})
          .ToArray<string[]>();
          return string.Format("## {0}\n",g.Key.ToString())
          + FormatTable(table, true);
        });
    var help = m.GetBuffer("*variables*", true);
    help.content = string.Join("\n", strings.ToArray());
    m.Display(help, false);
  }

  [Command]
  /*public*/ void DescribeGroups() {
    string[][] table
      = m.groups
         .OrderBy(x=> x.Key)
         .Select(kv => new string[] {kv.Key,
                                     kv.Value.description ?? "",
                                     kv.Value.tags.OxfordAnd() })
         .Prepend(new string[] {"group", "description", "tags"})
         .ToArray<string[]>();
    var help = m.GetBuffer("*groups*", true);
    help.content =  FormatTable(table, true);
    m.Display(help, false);
  }

  public static string[][] RemoveDups(string[][] table, string substitute) {
    if (table.Length < 2)
      return table;
    for (int i = table.Length - 1; i > 0; i--)
      for (int j = 0; j < table[i].Length; j++)
        if (table[i][j] == table[i - 1][j] && ! table[i][j].IsZull())
          table[i][j] = substitute;
    return table;
  }

  public static string FormatTable(string[][] table, bool hasHeader = false) {
    int rowCount = table.Length;
    if (rowCount == 0)
      return "";
    int columnCount = table[0].Length;
    int[] maxColumnSize = new int[columnCount];
    var writer = new System.IO.StringWriter();
    string format = "";
    for (int j = 0; j < columnCount; j++) {
      maxColumnSize[j] = table.Max((string[] row) => {
                                   if (row[j] == null)
                                     throw new NullReferenceException(
                                                 "Null in column {0}".Formatted(j));
                                   return row[j].Length;
                                   });
      format += (j == 0 ? "" : " | ")
        + "{" + j + ",-" + maxColumnSize[j] + "}";
    }
    for (int i = 0; i < rowCount; i++) {
      writer.WriteLine(format, (object[]) table[i].Select(s => s.Replace("|", @"\|")).ToArray());
      if (hasHeader && i == 0) {
        // Add header
        writer.WriteLine(format,
                         (object[])
                         // This uses the maxColumnSize.
                         // maxColumnSize
                         // .Select(k => new String('-', k))

                         // This uses the header size.
                         table[0]
                         .Select(header => new String('-', header.Length))
                         .ToArray());
      }
    }
    return writer.ToString();
  }


  [Command("describe-user-bindings",
           description = "Show key bindings (C-u to hide disabled keymaps)",
           keyBinding = "C-h B")]
  public void DescribeUserBindings([UniversalArgument]
                                   bool hideDisabled,
                                   string prefix = null) {
    DescribeBindings(hideDisabled, prefix, ci => ! (m.GetGroup(ci.group, true).tags.Contains("built-in")
                                                    || ci.tags.Contains("built-in")));
  }

  [Command("describe-bindings",
           description = "Show key bindings (C-u to hide disabled keymaps)",
           keyBinding = "C-h b")]
  public void DescribeBindings([UniversalArgument]
                               bool hideDisabled,
                               string prefix = null,
                               Func<CommandInfo, bool> whereClause = null) {
    var strings = m.keymaps
      .OrderByDescending(k => k.enabled).ThenByDescending(k => k.priority)
      .Where(k => ! hideDisabled || k.enabled)
      .Select(k =>
          {
            IEnumerable<KeyValuePair<string,string>> keys
            // by command name
            = k.ToDict().OrderBy(w => w.Value);
            // by key binding
            // = k.ToDict().OrderBy(w => w.Key);
            if (prefix != null)
              keys = keys.Where(kv => kv.Key.StartsWith(prefix));
            if (! keys.Any())
              return null;
            var entries = keys
            .Select(kv => {
                var command = kv.Value;
                CommandInfo ci;
                var desc = "* Command not found *";
                if (command != null && m.commands.TryGetValue(command, out ci)) {
                  desc = ci.briefDescription;
                  if (whereClause != null && ! whereClause(ci)) {
                    return null;
                  }
                }
                return new [] { kv.Key, kv.Value, desc ?? ""};
              })
            .Where(x => x != null);
            if (! entries.Any())
              return null;
            string[][] table = entries
            .Prepend(new [] {"key", "command", "description"})
            .ToArray();
            return string.Format("## {0} keymap ({1})\n",
                                 k.name,
                                 k.Count)
            + FormatTable(RemoveDups(table, "\""), true)
            + string.Format("> {0}priority {1}",
                            k.enabled ? "" : "DISABLED ",
                            k.priority);
          })
      .Where(s => s != null);
    var help = m.GetBuffer("*key-bindings*", true);
    help.content = string.Join("\n\n", strings.ToArray());
    m.Display(help, false);
  }

  [Command("describe-bindings-by-prefix",
           description = "Show key bindings by prefix",
           tag = "bind-only")]
  public void DescribeBindingsByPrefix([UniversalArgument]
                                       bool hideDisabled) {

    DescribeBindings(hideDisabled,
                     Regex.Replace(m.lastKeySequence, @" ((C|ctrl)-h|\?|f1)$", ""));
  }


  [Command("describe-command",
           description = "Show method, class, and keybindings for command.",
           keyBinding = "C-h c")]
  public static void DescribeCommand([Prompt("Describe command: ",
                                       completions = new string[] { "*all*", "*user*" },
                                       completer = "command")]
                                     string command,
                                     string prefix = "") {
    var m = Minibuffer.instance;
    CommandInfo ci;
    if (command == "*all*") {
      DescribeCommands(false);
    } else if (command == "*user*") {
      DescribeCommands(true);
    } else if (command != null && m.commands.TryGetValue(command, out ci)) {
      var desc = ci.description;
      var bindings = m.KeyBindingsForCommand(command);
      m.Message("{6}{0} is {1} and has {2}.{3}\nIt is defined in {5}.\n  {4}",
                command,
                bindings.Any()
                ? "bound to " + bindings.OxfordAnd()
                : "not bound to any keys",

                ci.tags.Any()
                ? "tags: " + ci.tags.OxfordAnd()
                : "no tags",
                desc == null ? "" : "\nDescription: \"" + desc + "\"",
                ci.signature,
                ci.definedIn,
                prefix);
    } else {
      m.Message("No such command {0}.", command);
    }
  }

  [Command("describe-history",
           description = "Show all the histories",
           keyBinding = "C-h H")]
  public void DescribeHistory() {
    var strings = m.histories
      .OrderBy(x => x.Key)
      .SelectMany(kv =>
          {
            var header = string.Format("## {0}", kv.Key);
            return kv.Value.Select((item, index) => "{0} {1}".Formatted(index + 1, item))
                  .Prepend(new String('-', header.Length))
                  .Prepend(header)
                  .Append("\n");
          }
                  );
    var help = m.GetBuffer("*history*", true);
    help.content = "Prompts have named histories. When prompted, the next and previous "
      + "history may be accessed with M-n and M-p respectively. Histories {0} configured "
      .Formatted(m.persistHistory ? "are" : "are not")
      + "to be saved between sessions.\n\n"
      + string.Join("\n", strings.ToArray());
    m.Display(help);
  }

  [Command("describe-commands",
           description = "Show all the commands organized by class",
           keyBinding = "C-h C")]
  public static void DescribeCommands([UniversalArgument] bool userOnly) {
    var m = Minibuffer.instance;
   var includeSystem = ! userOnly;
   IEnumerable<KeyValuePair<string, CommandInfo>> commands = m.commands;
   if (! includeSystem) {
     commands = commands
       .Where(x => ! (m.GetGroup(x.Value.group, true).tags.Contains("built-in")
                      || x.Value.tags.Contains("built-in")));
   }
   var strings = commands
      .GroupBy(x => x.Value.group)
      .OrderBy(g => {
          var keymap = m.GetKeymap(g.Key);
          if (keymap != null) {
            return keymap.priority;
          } else
            return 1000;
        })
      .ThenByDescending(g => g.Key)
      .Select(g => {
          string[][] table;
          table = g.OrderBy(x => x.Key)
          .Select(x => new [] {x.Key, x.Value.briefDescription ?? "",
                               m.KeyBindingsForCommand(x.Key).OxfordOr()})
          .Prepend(new [] {"command", "description", "key bindings"})
          .ToArray();

          // table = g.OrderBy(x => x.Key)
          // .Select(x => new [] {x.Key, x.Value.command.briefDescription ?? "",
          //                      m.KeyBindingsForCommand(x.Key).OxfordOr(),
          //                      x.Value.methodInfo.IsStatic.ToString(),
          //                      x.Value.methodInfo.PrettySignature()})
          // .Prepend(new [] {"command", "description", "key bindings", "static?", "signature"})
          // .ToArray();

          return string.Format("## {0} ({1})\n", g.Key.ToString(), g.Count())
          + FormatTable(table, true);
        });
    var help = m.GetBuffer("*commands*", true);
    help.content = string.Join("\n", strings.ToArray());
    m.Display(help, false);
  }

//  [Command]
  public void DescribeCommandsDebug() {
    var strings = m.commands
      .GroupBy(x => x.Value.group)
      .OrderBy(g => {
          var keymap = m.GetKeymap(g.Key);
          if (keymap != null) {
            return keymap.priority;
          } else
            return 1000;
        })
      .ThenByDescending(g => g.Key)
      .Select(g => {
          string[][] table;
          table = g.OrderBy(x => x.Key)
          .Select(x => new [] {x.Key,
                               x.Value.briefDescription ?? "",
                               m.KeyBindingsForCommand(x.Key).OxfordOr(),
                               x.Value.instanceExists.ToString()
            })
          .Prepend(new [] {"command", "description", "key bindings", "delegate type"})
          .ToArray();

          return string.Format("## {0} ({1})\n", g.Key.ToString(), g.Count())
          + FormatTable(table, true);
        });
    var help = m.GetBuffer("*commands*", true);
    help.content = string.Join("\n", strings.ToArray());
    m.Display(help, false);
  }

  [Command("describe-completers",
           description = "Show all the completers")]
  public void DescribeCompleters() {
    var strings = m.completers
      .Select(d => d.Key)
      .GroupBy(x => char.IsUpper(x[0]))
      .SelectMany((g) => {
          return g.OrderBy(x => x)
          .Append("\n");
        });
    var help = m.GetBuffer("*completers*", true);
    help.content
      = "Completers\n"
      + "==========\n\n"
      + string.Join("\n", strings.ToArray())
      + "Note: this list is not exhaustive.  Completers are dynamically "
      + "generated for enumerations and subclasses of UnityEngine.Object.";

    m.Display(help, true);
  }

  private void LoadHelpResource(string name, string bufferName = "*help*") {
    var ta = Resources.Load(name) as TextAsset;
    if (ta != null) {
      //var b = GetBuffer(name, true);
      var b = m.GetBuffer(bufferName, true);
      b.content = ta.text;
      m.Display(b, true);
    } else {
      m.Message("Unable to load resource '{0}'.", name);
    }
  }

  [Command("help-for-help",
           description = "Show the master help page",
           keyBinding = "C-h h")]
  public void HelpForHelp() {
    LoadHelpResource("help-for-help");
  }

  [Command("describe-license",
           description = "Show the license for minibuffer",
           keyBinding = "C-h l")]
  public void DescribeLicense() {
    LoadHelpResource("unity-asset-eula", "License");
  }

  // [Command("help-with-tutorial",
  //          description = "Show the tutorial",
  //          keyBinding = "C-h t")]
  // public void HelpWithTutorial() {
  //   LoadHelpResource("tutorial");
  // }

  [Command("describe-key",
           description = "Reports what command is run for a key sequence",
           keyBinding = "C-h k")]
  public void DescribeKey([Prompt("Describe key: ",
                                  filler = "keybinding",
                                  requireMatch = false)] // XXX I didn't know I
                                                          // actually used
                                                          // filler.
                          string key) {
    var command = m.Lookup(key, true);
    if (command != null) {
      // Found it.
      m.Message("{0} runs the command {1}.",
                key,
                command);
      // Describe command?
    } else {
      command = m.Lookup(key, false);

      if (command != null) {
        // Found it.
        m.Message("{0} runs the command {1} but the keymap is currently disabled.",
                  key,
                  command);
      } else
        // There's nothing.
        m.Message(key + " is not bound to any command.");
    }
  }

  // [Command(description = "Your game's description",
  //          keyBinding = "C-h g")]
  // public void DescribeGame() {
  //   LoadHelpResource("game-description", "*game*");
  // }


  [Command("display-universal-argument",
           description = "Displays the universal-argument as integer and bool.")]
  /** \see UniversalArgument */
  public void DisplayUniversalArgument([UniversalArgument]
                                       int prefix,
                                       [UniversalArgument]
                                       bool boolPrefix) {
    m.Message("{0} {1}", prefix, boolPrefix);
  }

}

} // namespace
