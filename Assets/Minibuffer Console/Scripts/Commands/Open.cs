/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
#if ! SH_MINIBUFFER_NOLIBS
#define COMMON_MARK
#endif
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SeawispHunter.MinibufferConsole.Extensions;
using RSG;
#if COMMON_MARK
using CommonMark;
#endif
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace SeawispHunter.MinibufferConsole {

/**
   ![Open commands in the inspector](inspector/open.png)
   %Open commands.

   These commands "open" a URL or buffer using the OS facilities that Unity
   exposes. One can open a URL, a path, a buffer in a text editor, and a buffer
   in a web browser.

   Note that <kbd>M-x open-buffer-in-browser</kbd> will
   [Markdown](https://daringfireball.net/projects/markdown/syntax) the buffer
   before opening unless <kbd>C-u</kbd> is used.

   Note: Opening a path, does not "open" it in %Minibuffer. It hands it off to
   the OS to "open" it.

   All of open's commands are static methods. One might wonder why then is the
   `Open` class not static? The reason is that by making `Open` a MonoBehaviour
   it is easy to include or exclude its commands by enabling or disabling it in
   the Unity Inspector window.
 */
[Group("open", tag = "built-in")]
public class Open : MonoBehaviour {


  // [Header("Markdown the buffer when opening in browser?")]
  // public bool useMarkdown = true;
  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
  }

  static readonly char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
  // https://stackoverflow.com/questions/333175/is-there-a-way-of-making-strings-file-path-safe-in-c
  public static string SafeFileName(string filename) {
    // Build a string out of valid chars
    return new string(filename.Where(ch => ! invalidFileNameChars.Contains(ch)).ToArray());
  }

  // May want to split these open commands out of Help.  Could see someone wanting
  // help but not open available in their game.

  [Command("open-url",
           description = "Open a URL using the OS facilities.",
           keyBinding = "C-x u")]
  public static void URL([Prompt("URL: ",
                                     history = "url")]
                             string url) {
    Application.OpenURL(url);
    Minibuffer.instance.Message("Open URL {0}", url);
  }

#if ! UNITY_WEBPLAYER
  [Command("open-buffer-in-text-editor",
           description = "Write current buffer to a temp file then open it.",
           keyBinding = "C-c C-c t")]
  public static void BufferAsURL([Current] IBuffer b) {

    // XXX I could use Markdown to send this to the browser
    // for a little bit better rendering.
    //var b = m.gui.main.buffer;
    var path = PathName.instance.Expand("$temp/" + SafeFileName(b.name) + ".txt");
    System.IO.File.WriteAllText(path, b.content);
    Open.Path(path);
  }
#endif

  [Command("open-path",
           description = "Open a given path using the OS facilities. "
           + "Substitutions $data, $temp, $assets, and $streaming are available.",
           keyBinding = "C-x p")]
  public static void Path([Prompt("Path: ",
                                  completer = "file",
                                  input = "$data/")]
                          string path) {
    // XXX I could use Markdown to send this to the browser
    // for a little bit better rendering.
    //var b = m.gui.main.buffer;
    var p = PathName.instance.Expand(path);
    var url = System.Uri.EscapeUriString("file://" + p);
    Application.OpenURL(url);
    Minibuffer.instance.Message("Open path {0}", PathName.instance.Compress(path));
  }

#if UNITY_WEBGL
  [DllImport("__Internal")]
  private static extern void SetHTML(string str);
#endif

  [Command("open-buffer-in-browser",
           description = "Show current buffer as HTML. "
                       + "Buffer is treated as Markdown unless C-u is used.",
           keyBinding = "C-c C-c p")]
  public static void BufferAsHtmlURL([Current] IBuffer b,
                                     // Breaking my rule for a negative boolean but
                                     // UniversalArgument is false by default.
                                     [UniversalArgument] bool dontMarkdown) {
    string content;
    if (dontMarkdown) {
      content = "<pre>" + b.content + "</pre>";
    } else {
      #if COMMON_MARK
      var settings = CommonMarkSettings.Default.Clone();
      settings.AdditionalFeatures
        = CommonMarkAdditionalFeatures.GithubStyleTables;
      settings.RenderSoftLineBreaksAsLineBreaks = true;
      content = CommonMarkConverter.Convert(b.content, settings);
      #else
      content = "<pre>" + b.content + "</pre>";
      #endif
    }
    var title = b.name;

    #if ! UNITY_WEBGL || UNITY_EDITOR
    var templateAsset = Resources.Load("markdown-template") as TextAsset;
    var template = templateAsset != null ? templateAsset.text : "{{content}}";
    var path = PathName.instance.Expand("$temp/" + SafeFileName(b.name) + ".html");
    System.IO.File
      .WriteAllText(path,
                    template
                      .Replace("{{title}}", title)
                      .Replace("{{content}}", content)
                      .Replace("{{version}}", "v" + MinibufferVersion.fileVersion));
    Open.Path(path);
    #else
    SetHTML(content);
    #endif
  }

}

} // namespace
