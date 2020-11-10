/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/*
  Delegate the handling of some keys to %Minibuffer, but otherwise it's a
  regular InputField.
 */
[Group("editing", tag = "built-in")]
public class TappedInputField
  : InputField {

  public void ScrollEvent() {
    MarkGeometryAsDirty();
  }

  private bool HandleInMinibuffer(Event e) {
    KeyChord kc = Minibuffer.instance.EventToKeyChord(e);
    // print("minibuffer key chord '" + (kc == null ? "<null>" : kc.ToString()) + "' for event " + e);
    // KeyChord kc2 = KeyChord.FromEvent(e);
    // print("key chord from event '" + (kc2 == null ? "<null>" : kc2.ToString()) + "'");
    if (kc != null && Minibuffer.instance.WouldConsumeKeyChord(kc)) {
      // print("key chord sent " + kc);
      Minibuffer.instance.Input(kc);
      return true;
    } else {
      // We still run it through get keys which we need for macros and the like.
      if (kc != null)
        Minibuffer.instance.RunGetKeys(kc);
      return false;
    }
  }

  public void ResizeWidthToFitText() {
    var charSize = textComponent.CharSize();
    var rt = GetComponent<RectTransform>();
    var sd = rt.sizeDelta;
    sd.x = text.Length * charSize.x;
    rt.sizeDelta = sd;
  }

  public void ResizeHeightToFitText() {
    var charSize = textComponent.CharSize();
    var rt = GetComponent<RectTransform>();
    var sd = rt.sizeDelta;
    sd.y = cachedInputTextGenerator.lineCount * charSize.y;
    rt.sizeDelta = sd;
  }

  // public override void OnUpdateSelected(BaseEventData eventData) {
  //   // KeyChord kc = KeyChord.FromEvent(Event.current);
  //   print(" got event " + eventData);
  //   KeyChord kc = Minibuffer.instance.EventToKeyChord(Event.current);
  //   if (Minibuffer.instance.WouldConsumeKeyChord(kc))
  //     // Minibuffer.instance.KeyEvent(Event.current);
  //     Minibuffer.instance.Input(kc);
  //   else
  //     base.OnUpdateSelected(eventData);
  // }

  private Event m_ProcessingEvent = new Event();
  public override void OnUpdateSelected(BaseEventData eventData)
  {
    if (!isFocused)
      return;

    bool consumedEvent = false;
    while (Event.PopEvent(m_ProcessingEvent))
    {
      // print(" got event " + m_ProcessingEvent);
      if (m_ProcessingEvent.rawType == EventType.KeyDown)
      {
        consumedEvent = true;
        // *** Added this
        if (HandleInMinibuffer(m_ProcessingEvent)
            // This is hacky. M-n sometimes produces spurious small tildes (˜).
            // See Minibuffer.HistoryNext() for more details. This code prevents
            // that from happening.
            || m_ProcessingEvent.character == 732) {
          // print("handled in minibuffer: " + m_ProcessingEvent);
          break;
        }
        // print("handled by KeyPressed: " + m_ProcessingEvent);
        // ***
        var shouldContinue = KeyPressed(m_ProcessingEvent);
        if (shouldContinue == EditState.Finish)
        {
          DeactivateInputField();
          break;
        }
      }

      switch (m_ProcessingEvent.type)
      {
        case EventType.ValidateCommand:
        case EventType.ExecuteCommand:
          switch (m_ProcessingEvent.commandName)
          {
            case "SelectAll":
              SelectAll();
              consumedEvent = true;
              break;
          }
          break;
      }
    }

    if (consumedEvent)
      UpdateLabel();

    eventData.Use();
  }

  // protected EditState MyKeyPressed(Event evt) {
  //   print("miniInputField " + evt);
  //   var kc = Minibuffer.instance.EventToKeyChord(evt);
  //   Minibuffer.instance.Input(kc);
  //   return EditState.Continue;
  //   UpdateLabel();
  // }

  #region InputField copies

  private void MarkGeometryAsDirty()
  {
    #if UNITY_EDITOR
    if (!Application.isPlaying || UnityEditor.PrefabUtility.GetPrefabObject(gameObject) != null)
      return;
    #endif

    CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
  }
  #endregion
}

}
