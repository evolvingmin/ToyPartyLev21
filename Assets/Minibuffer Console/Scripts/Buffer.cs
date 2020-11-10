/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/*
  I was very hesitant to add buffers, but they're not for editing.
  Just a place to put junk.
*/
public interface IBuffer {
  /** Buffer name */
  string name { get; set; }
  /** Buffer content */
  string content { get; set; }
  /** If this buffer is updated, call this action. */
  Action<IBuffer> onUpdate { get; set; }
  //Dictionary<string, object> metaData { get; set; }
  string savePath { get; set; }
  /** Window caches the scroll position in this variable when the buffer is not in a window.

      Window sets scrollPosition to null out when the buffer is placed in a window.

      If the buffer sets scrollPosition, Window will set the scroll position of
      the window when onUpdate is called.

      Scroll position (0,0) is the top left. Scroll position (1,1) is the bottom
      right. */
  Vector2? scrollPosition { get; set; }
}

[Group(tag = "built-in")]
public class Buffer : IBuffer {
  public string name { get; set; }
  [SerializeField]
  private string _content;
  public Action<IBuffer> onUpdate { get; set; }

  public Vector2? scrollPosition { get; set; }
  //public Dictionary<string, object> metaData { get; set; }
  public string savePath { get; set; }
  public virtual string content {
    get { return _content; }
    set { if (value != _content) {
        _content = value;
        if (onUpdate != null)
          onUpdate(this);
      }
    }
  }

  static Buffer() {
    Minibuffer.Register(typeof(Buffer));
  }

  public Buffer(string name) {
    this.name = name;
    this.content = "";
  }

  public override string ToString() {
    return name;
  }

  [Command("switch-to-buffer",
           description = "Switch to one of the existing buffers.",
           keyBinding = "C-x b")]
  public static void SwitchToBuffer([Prompt("Buffer: ",
                                     completer = "buffer")]
                                    IBuffer buffer) {
    var m = Minibuffer.instance;
    m.gui.main.buffer = buffer;
    m.gui.main.visible = true;
  }


  [Command("list-buffers",
           description = "List existing buffers.",
           keyBinding = "C-x C-b")]
  public static void ListBuffers() {
    var strings = Minibuffer.instance.buffers
      .Select(b =>
              string.Format("{0,-30} {1}", b.name, b.content.Length))
                  .Prepend(string.Format("{0,-30} {1}", "------", "----"))
                  .Prepend(string.Format("{0,-30} {1}", "Buffer", "Size"))
                  .Append("\n");
    var bufferList = Minibuffer.instance.GetBuffer("*Buffer List*", true);
    bufferList.content = string.Join("\n", strings.ToArray());
    Minibuffer.instance.Display(bufferList);
  }

  [Command("copy-buffer-to-clipboard",
           description = "Copy the contents of the current buffer to the clipboard.")]
  public static void CopyToClipboard([Current] IBuffer b) {
    GUIUtility.systemCopyBuffer = b.content;
  }

  [Command("save-buffer",
           description = "Save the current buffer to a file.",
           keyBinding = "C-x C-s")]
  public static void Save([Current] IBuffer b) {
    // Current buffer is whatever is in main.
    //var b = this;
    var m = Minibuffer.instance;
    if (b.savePath.IsZull()) {
      // Need a save path.
      m.Read("Save file to path: ",
             completer: "file",
             history: "save-buffer",
             #if UNITY_EDITOR
             input: "$assets"
             #else
             input: "$data"
             #endif
             )
        .Done((fileInfo) => {
            b.savePath = fileInfo.ToString();
            Save(b);
          });
    } else {
      #if ! UNITY_WEBGL && ! UNITY_WEBPLAYER
      System.IO.File.WriteAllText(b.savePath, b.content);
      m.Message("Wrote {0} to {1}", b.name, b.savePath);
      #else
      m.Message("Unable to write file on WebGL build.");
      #endif
    }
  }

  // I'm skeptical about doing anything like this.
  [Command("kill-buffer",
           description = "Kill a buffer.",
           keymap = "core",
           keyBinding = "C-x k")]
  public static void Kill([Prompt("Kill buffer: ",
                                  completer = "buffer")]
                          IBuffer b) {
    var m = Minibuffer.instance;

    m.buffers.Remove(b);
    if (m.gui.main.buffer == b) {
      m.gui.main.buffer = m.buffers.Count != 0 ? m.buffers[0] : null;
    }
  }
}

public class AppendableBuffer : IBuffer {
  public string name { get; set; }
  protected List<string> lines = new List<string>();
  private string _content; // When _content = null, it's akin to being
                           // marked dirty so regenerate from lines.
  public bool autoScrollDown = false;
  Vector2? _scrollPosition;
  public Vector2? scrollPosition {
    get { return autoScrollDown ? new Vector2(_scrollPosition.HasValue ? _scrollPosition.Value.x : 0f, 1f) : _scrollPosition; }
    set { _scrollPosition = value; }
  }
  public Action<IBuffer> onUpdate { get; set; }
  public string savePath { get; set; }
  public virtual string content {
    get {
      if (_content == null)
        _content = string.Join("\n", lines.ToArray());
      return _content;
    }
    set {
      lines.Clear();
      lines.Add(value);
      if (onUpdate != null)
        onUpdate(this);
    }
  }

  public AppendableBuffer(string name) {
    this.name = name;
  }

  public override string ToString() {
    return name;
  }

  public virtual void AppendLine(string s) {
    lines.Add(s);
    _content = null;
    if (onUpdate != null)
        onUpdate(this);
  }
}

/*
  Do some Run Length Encoding (RLE) for repeated lines.
 */
public class RLEBuffer : AppendableBuffer {
  private int repeatCount = 0;
  private string lastLine;
  public RLEBuffer(string name) : base(name) { }

  public override void AppendLine(string s) {
    if (lastLine == s && lines.Any()) {
      repeatCount++;
      lines.RemoveAt(lines.Count - 1);
      base.AppendLine(s + " [" + (repeatCount + 1) + " times]");
    } else {
      repeatCount = 0;
      base.AppendLine(s);
    }
    lastLine = s;
  }
}


}
