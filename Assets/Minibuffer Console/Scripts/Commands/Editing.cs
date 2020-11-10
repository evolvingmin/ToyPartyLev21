/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace SeawispHunter.MinibufferConsole {

[Group("editing", tags = new [] { "hide-group", "built-in" })]
public class Editing : MonoBehaviour {

  private bool markSet = false;
  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
  }

  [Command("forward-char",
           tag = "bind-only",
           description = "Move cursor forward one character",
           keyBinding = "C-f")]
  public void ForwardChar([Current] InputField inputField) {
   // print("forwardchar");
    // inputField.caretPosition += 1;
    MoveRight(inputField, markSet, false);
  }

  [Command("backward-char",
           tag = "bind-only",
           description = "Move cursor backward one character",
           keyBinding = "C-b")]
  public void BackwardChar([Current] InputField inputField) {
    // inputField.caretPosition -= 1;
    MoveLeft(inputField, markSet, false);
  }

  [Command("kill-ring-save",
           tag = "bind-only",
           description = "Copy what is in the selection",
           keyBinding = "M-w")]
  public void Copy([Current] InputField inputField) {
    if (inputField.inputType != InputField.InputType.Password)
      clipboard = GetSelectedString(inputField);
    else
      clipboard = "";
    markSet = false;
  }

  [Command("yank",
           tag = "bind-only",
           description = "Paste what is in the clipboard",
           keyBinding = "C-y")]
  public void Paste([Current] InputField inputField) {
    Append(inputField, clipboard);
    SendOnValueChangedAndUpdateLabel(inputField);
    markSet = false;
  }

  [Command("backward-word",
           tag = "bind-only",
           description = "Move cursor backword one word",
           keyBinding = "M-b")]
  public void BackwardWord([Current] InputField inputField) {
    // MoveLeft(markSet, true);
    MoveLeft(inputField, false, true);
    UpdateLabel(inputField);
  }

  [Command("backward-kill-word",
           tag = "bind-only",
           description = "Delete backward one word",
           keyBinding = "M-backspace")]
  public void BackwardKillWord([Current] InputField inputField) {
    MoveLeft(inputField, true, true);
    Cut(inputField);
    markSet = false;
  }

  [Command("kill-word",
           tag = "bind-only",
           description = "Kill one word",
           keyBinding = "M-d")]
  public void KillWord([Current] InputField inputField) {
    MoveRight(inputField, true, true);
    Cut(inputField);
    markSet = false;
  }

  [Command("kill-selection",
           tag = "bind-only",
           description = "Cut the selection to the clipboard",
           keyBinding = "C-w")]
  public void Cut([Current] InputField inputField) {
    if (inputField.inputType != InputField.InputType.Password)
      clipboard = GetSelectedString(inputField);
    else
      clipboard = "";
    Delete(inputField);
    SendOnValueChangedAndUpdateLabel(inputField);
    markSet = false;
  }

  [Command("forward-word",
           tag = "bind-only",
           description = "Move forward one word",
           keyBinding = "M-f")]
  public void ForwardWord([Current] InputField inputField) {
    // print("forward-word");
    MoveRight(inputField, markSet, true);
    UpdateLabel(inputField);
  }

  [Command("next-line",
           tag = "bind-only",
           description = "Go to the next line",
           keyBinding = "C-n")]
  public void MoveDown([Current] InputField input)
  {
    MoveDown(input, false, false);
  }

  [Command("mark-whole-buffer",

           description = "Select all",
           keyBinding = "C-x h")]
  public void SelectAll([Current] InputField inputField)
  {
    inputField.selectionAnchorPosition = 0;
    inputField.selectionFocusPosition = inputField.text.Length;
    // markSet = true;
  }

  // XXX This doesn't really work.
  [Command("set-mark-command",
           tag = "bind-only",
           description = "Start selection here",
           keyBinding = "C-space")]
  public void StartSelect([Current] InputField inputField)
  {
      // This is still super wonky.
    markSet = ! markSet;
    if (markSet)
      inputField.selectionAnchorPosition = inputField.caretPosition;
  }

  [Command("end-of-buffer",
           description = "Move to end of buffer",
           keyBinding = null)]
  public void EndOfBuffer([Current] InputField inputField) {
    inputField.MoveTextEnd(markSet);
  }

  // Not named backspace because that's the name of a key
  // and that can cause issues.
  [Command("delete-backward-char",
           tag = "bind-only",
           description = "Delete the previous character",
           keyBinding = "backspace")]
  public void MyBackspace([Current] InputField inputField) {
    Backspace(inputField);
  }

  [Command("move-beginning-of-line",
           tag = "bind-only",
           description = "Move to beginning of line",
           keyBinding = "C-a")]
  public void MoveBeginningOfLine([Current] InputField inputField) {
    // inputField.MoveLineStart(markSet);
    inputField.MoveTextStart(markSet);
  }

  [Command("kill-line",
           tag = "bind-only",
           description = "Cut the line from cursor to end of line",
           keyBinding = "C-k")]
  public void KillLine([Current] InputField inputField) {
    PushSelection(inputField, true);
    markSet = true;
    MoveEndOfLine(inputField);

    // On the first kill we get rid of content. On the next kill we
    // get rid of the newline.
    // if (caretSelectPositionInternal == caretPositionInternal)
    //   caretPositionInternal += 1;
    Cut(inputField);
    markSet = false;
    PopSelection(inputField);
  }
  [Command("insert-space",
           description = "Inserts a space. Special case.",
           keyBinding = "space",
           tags = new [] { "pass-to-inputfield", "bind-only" })]
  public void InsertSpace([Current] InputField inputField) {
    Append(inputField, ' ');
  }

  [Command("quoted-insert",
           description = "Insert the next char verbatim.",
           keyBinding = "C-q")]
  public void QuotedInsert() {
    Minibuffer.instance.ReadChar()
      .Then(c => {
          Editing.Append(Minibuffer.instance.gui.input, c);
        });
  }


  [Command("previous-line",
           tag = "bind-only",
           description = "Go to the previous line",
           keyBinding = "C-p")]
  public void MoveUp([Current] InputField inputField)
  {
    MoveUp(inputField, markSet, false);
  }

  [Command("move-end-of-line",
           tag = "bind-only",
           description = "Move to end of line",
           keyBinding = "C-e")]
  public void MoveEndOfLine([Current] InputField inputField) {
    // MoveLineEnd(markSet);
    inputField.MoveTextEnd(markSet);
  }

  [Command("delete-char",
           tag = "bind-only",
           description = "Delete the forward character",
           keyBinding = "C-d")]
  public void DeleteChar([Current] InputField inputField) //[UniversalArgument] int count)
  {
    ForwardSpace(inputField);
  }

  private static string GetSelectedString([Current] InputField inputField)
  {
    int startPos = inputField.selectionAnchorPosition;
    int endPos = inputField.selectionFocusPosition;

    // Ensure pos is always less then selPos to make the code simpler
    if (startPos > endPos)
    {
      int temp = startPos;
      startPos = endPos;
      endPos = temp;
    }

    return inputField.text.Substring(startPos, endPos - startPos);
  }

  [Command(keyBinding = "s-A", /*, keymap = "core"*/
           description = "Deslect all text")]
  public static void
    DeselectAll([Current] InputField inputField) {
    inputField.caretPosition = inputField.selectionFocusPosition;
    // inputField.selectionFocusPosition = inputField.selectionAnchorPosition = inputField.caretPosition;
    // if (inputField.caretPosition > 0) {
    //   MoveLeft(inputField, false, false);
    //   MoveRight(inputField, false, false);
    // } else {
    //   MoveRight(inputField, false, false);
    //   MoveLeft(inputField, false, false);
    // }
  }

  public struct Selection {
    public bool markWasSet;
    public int start;
    public int end;
  }
  private Stack<Selection> stack = new Stack<Selection>();

  private void PushSelection(InputField inputField, bool clearSelection = false) {
    stack.Push(new Selection() {
        markWasSet = markSet,
        start = inputField.selectionAnchorPosition,
        end = inputField.selectionFocusPosition });
    if (clearSelection)
      inputField.selectionFocusPosition = inputField.selectionAnchorPosition = inputField.caretPosition;
  }

  private void PopSelection(InputField inputField) {
    var s = stack.Pop();
    if (s.start != s.end) {
      this.markSet = s.markWasSet;
      inputField.selectionAnchorPosition = s.start;
      inputField.selectionFocusPosition = s.end;
    }
  }

  #region InputField copy
  public static string clipboard
  {
    get
    {
      return GUIUtility.systemCopyBuffer;
    }
    set
    {
      GUIUtility.systemCopyBuffer = value;
    }
  }
  #endregion

  #region InputField bypass privates

  private static MethodInfo moveLeft = null;
  private static object[] moveLeftArgs = new object[2];
  private static void MoveLeft(InputField inputField, bool shift, bool ctrl)
  {
    if (moveLeft == null)
      moveLeft = typeof(InputField).GetMethod("MoveLeft", BindingFlags.NonPublic | BindingFlags.Instance);
    moveLeftArgs[0] = shift;
    moveLeftArgs[1] = ctrl;
    moveLeft.Invoke(inputField, moveLeftArgs);
  }

  private static MethodInfo moveRight = null;
  private static object[] moveRightArgs = new object[2];
  public static void MoveRight(InputField inputField, bool shift, bool ctrl)
  {
    if (moveRight == null)
      moveRight = typeof(InputField).GetMethod("MoveRight", BindingFlags.NonPublic | BindingFlags.Instance);
    moveRightArgs[0] = shift;
    moveRightArgs[1] = ctrl;
    moveRight.Invoke(inputField, moveRightArgs);
  }

  private static MethodInfo moveDown = null;
  private static object[] moveDownArgs = new object[2];
  private static void MoveDown(InputField inputField, bool shift, bool ctrl)
  {
    if (moveDown == null)
      moveDown = typeof(InputField).GetMethod("MoveDown", BindingFlags.NonPublic | BindingFlags.Instance);
    moveDownArgs[0] = shift;
    moveDownArgs[1] = ctrl;
    moveDown.Invoke(inputField, moveDownArgs);
  }
  private static MethodInfo moveUp = null;
  private static object[] moveUpArgs = new object[2];
  private static void MoveUp(InputField inputField, bool shift, bool ctrl)
  {
    if (moveUp == null)
      moveUp = typeof(InputField).GetMethod("MoveUp", BindingFlags.NonPublic | BindingFlags.Instance);
    moveUpArgs[0] = shift;
    moveUpArgs[1] = ctrl;
    moveUp.Invoke(inputField, moveUpArgs);
  }

  private static MethodInfo delete = null;
  private static void Delete(InputField inputField)
  {
    if (delete == null)
      delete = typeof(InputField).GetMethod("Delete", BindingFlags.NonPublic | BindingFlags.Instance);
    delete.Invoke(inputField, null);
  }

  private static MethodInfo backspace = null;
  private static void Backspace(InputField inputField)
  {
    if (backspace == null)
      backspace = typeof(InputField).GetMethod("Backspace", BindingFlags.NonPublic | BindingFlags.Instance);
    backspace.Invoke(inputField, null);
  }
  private static MethodInfo updateLabel = null;
  private static void UpdateLabel(InputField inputField)
  {
    if (updateLabel == null)
      updateLabel = typeof(InputField).GetMethod("UpdateLabel", BindingFlags.NonPublic | BindingFlags.Instance);
    updateLabel.Invoke(inputField, null);
  }
  private static MethodInfo forwardSpace = null;
  private static void ForwardSpace(InputField inputField)
  {
    if (forwardSpace == null)
      forwardSpace = typeof(InputField).GetMethod("ForwardSpace", BindingFlags.NonPublic | BindingFlags.Instance);
    forwardSpace.Invoke(inputField, null);
  }
  private static MethodInfo sendOnValueChangedAndUpdateLabel = null;
  private static void SendOnValueChangedAndUpdateLabel(InputField inputField)
  {
    if (sendOnValueChangedAndUpdateLabel == null)
      sendOnValueChangedAndUpdateLabel = typeof(InputField).GetMethod("SendOnValueChangedAndUpdateLabel", BindingFlags.NonPublic | BindingFlags.Instance);
    sendOnValueChangedAndUpdateLabel.Invoke(inputField, null);
  }
  private static MethodInfo appendChar;
  private static object[] appendCharArgs = new object[1];
  public static void Append(InputField inputField, char c) {
    if (appendChar == null)
      appendChar = typeof(InputField).GetMethod("Append", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(char) }, null);
    appendCharArgs[0] = c;
    appendChar.Invoke(inputField, appendCharArgs);
  }

  private static MethodInfo appendString;
  private static object[] appendStringArgs = new object[1];
  public static void Append(InputField inputField, string s) {
    if (appendString == null)
      appendString = typeof(InputField).GetMethod("Append", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);
    appendStringArgs[0] = s;
    appendString.Invoke(inputField, appendStringArgs);
  }
  #endregion
}

}
