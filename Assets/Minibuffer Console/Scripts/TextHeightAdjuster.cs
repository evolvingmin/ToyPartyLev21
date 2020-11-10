/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
// [RequireComponent(typeof(Scrollbar))]
public class TextHeightAdjuster : MonoBehaviour {
  private Scrollbar scrollbar;
  void Start() {
  }

  public void SetScrollbarSteps(InputField inputField) {
    SetScrollbarSteps(inputField.text);
  }

  private void SetScrollbarSteps(string text) {
    // Debug.Log("set scrollbar steps " + text);
    if (! enabled)
      return;
    if (scrollbar == null)
      scrollbar = GetComponent<Scrollbar>();
    scrollbar.numberOfSteps
      = System.Math.Max(1, text.LineCount() - Minibuffer.instance.advancedOptions.maxAutocompleteLines);
  }

  private Vector3[] corners = new Vector3[4];
  public void ResizeHeightToFitText(InputField inputField) {
    var rt = inputField.GetComponent<RectTransform>();
    var parentRt = inputField.transform.parent.GetComponent<RectTransform>();
    var sd = rt.sizeDelta;
    var charSize = inputField.textComponent.CharSize();
    parentRt.GetLocalCorners(corners);
    var bottom = corners[0].y;
    var top = corners[1].y;
    var rectsHeight = top - bottom;
    // var canvas = inputField.GetComponentInParent<Canvas>();
    // sd.y = (inputField.text.LineCount() * charSize.y - rectsHeight / canvas.scaleFactor).Max(0);
    // Don't have to scale it yourself, if you do it using local corners.
    sd.y = (inputField.text.LineCount() * charSize.y - rectsHeight).Max(0);
    // print("top " + top + " bottom " + bottom + " y " + sd.y);
    rt.sizeDelta = sd;
  }

}
}
