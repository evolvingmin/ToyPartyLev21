/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Assertions;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

[System.Serializable]
[Group(tag = "built-in")]
public class Window {
  public GameObject window;
  public ScrollRect scrollRect;
  public Text content;
  public Text content2;
  public Text name;
  public bool resizable = false;

  private Vector2 _fontCharSize;
  private bool gotMask = false;
  private Mask _mask;
  private bool firstTime = true;
  private const int MAX_UI_TEXT_LENGTH = 15000;

  static Window() {
    Minibuffer.Register(typeof(Window));
  }

  private Mask mask {
    get {
      if (! gotMask) {
        _mask = scrollRect.GetComponentInChildren<Mask>();
        gotMask = true;
      }
      return _mask;
    }
  }

  public bool visible {
    get {
      Assert.IsNotNull(window, "window");
      return window.activeInHierarchy;
    }
    set {
      Assert.IsNotNull(window, "window");
      if (value != window.activeInHierarchy) {
        // We're transitioning.
        Assert.IsNotNull(scrollRect, "scrollRect");
        ScrollToTop();
        if (firstTime && resizable) {
          var h = Minibuffer.instance.advancedOptions.initialWindowHeight;
          var w = Minibuffer.instance.advancedOptions.initialWindowWidth;
          ResizeWindowHeight(Mathf.Abs(h), h > 0f);
          ResizeWindowWidth(Mathf.Abs(w), w > 0f);
          // Minibuffer.instance.DoNextTick(() => {
          //     HalfsizeWindow();
          //     firstTime = false;
          //   });
        }
      }
      window.SetActive(value);
    }
  }

  public bool wrapText {
    get {
      var csf = content.transform.parent.GetComponent<ContentSizeFitter>();
      return csf.horizontalFit == ContentSizeFitter.FitMode.Unconstrained;
    }
    set {
      if (wrapText != value) {
        var rt = (RectTransform) content.transform.parent;
        var csf = rt.GetComponent<ContentSizeFitter>();
        if (value) {
          // Wrapping.
          csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
          var v2 = rt.sizeDelta;
          v2.x = 0f;
          rt.sizeDelta = v2;
        } else {
          // Not wrapping.
          csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
      }
    }
  }

  /**
     Scroll position (0,0) is the top left.  Scroll position (1,1) is the bottom right.
   */
  // XXX Redo the UI to not need this translation.
  public Vector2 scrollPosition {
    get { return new Vector2(scrollRect.horizontalNormalizedPosition,
                             1f - scrollRect.verticalNormalizedPosition); }
    set {
      scrollRect.horizontalNormalizedPosition = value.x;
      scrollRect.verticalNormalizedPosition = 1f - value.y;
    }
  }

  private IBuffer _buffer;
  public IBuffer buffer {
    get { return _buffer; }
    set {
      if (value != _buffer) {
        if (_buffer != null) {
          _buffer.onUpdate = null;
          _buffer.scrollPosition = scrollPosition;
        }
        _buffer = value;
        if (_buffer != null) {
          if (content != null) {
            if (_buffer.content.Length < MAX_UI_TEXT_LENGTH) {
              content.text = _buffer.content;
              if (content2)
                content2.text = "";
            } else {
              var lineIndex = _buffer.content.LineIndexForPosition(MAX_UI_TEXT_LENGTH);
              var charIndex = _buffer.content.PositionForLineIndex(lineIndex);
              content.text = _buffer.content.Substring(0, charIndex - 1); // Ignore the last newline.
              // Allows for 17,000-ish more characters to be displayed.
              if (content2)
                content2.text = _buffer.content.Substring(charIndex);
            }
            if (mask != null) {
              // HACK We toggle this because sometimes it'll disappear
              // if we don't. Ugh.
              mask.enabled = false;
              mask.enabled = true;
            }
            _buffer.onUpdate = (b) => { content.text = b.content;
                                        if (b.scrollPosition.HasValue)
                                          Minibuffer.instance.DoNextTick(() => scrollPosition = b.scrollPosition.Value);
            };
            // scrollPosition = _buffer.scrollPosition;
            Minibuffer.instance.DoNextTick(() => {
                scrollPosition = _buffer.scrollPosition.HasValue
                  ? _buffer.scrollPosition.Value
                  : Vector2.zero;
                _buffer.scrollPosition = null;
                });
          }
          if (name != null) {
            name.text = _buffer.name;
          }

        }
        //Minibuffer.instance.DoNextTick(() => ScrollToTopLeft());
       }
    }
  }

  private void ScrollToTopLeft() {
    this.scrollRect.horizontalNormalizedPosition = 0;
    this.scrollRect.verticalNormalizedPosition = 1;
  }

  [Command("window-scroll-to-top",
           description = "Scroll to top in active window")]
  public void ScrollToTop() {
    this.scrollRect.verticalNormalizedPosition = 1;
  }

  [Command("window-scroll-to-bottom",
           description = "Scroll to bottom in active window")]
  public void ScrollToBottom() {
    this.scrollRect.verticalNormalizedPosition = 0;
  }

  public Vector2 fontCharSize {
    get {
      if (_fontCharSize.sqrMagnitude == 0f) {
        var settings = content.GetGenerationSettings(new Vector2(1000f, 1000f));
        _fontCharSize
          = new Vector2(content.cachedTextGenerator.GetPreferredWidth("M",
                                                                      settings),
                        content.cachedTextGenerator.GetPreferredHeight("M",
                                                                       settings));
      }
      return _fontCharSize;
    }
  }

  public int fontSize {
    get {
      return content.fontSize;
    }
    set {
      int size = value;
      content.fontSize = size;
      if (name != null)
        name.fontSize = size;

      _fontCharSize = Vector2.zero;
      //Debug.Log("fontCharSize = " + fontCharSize);
    }
  }

  private Canvas _canvas;
  private Canvas canvas {
    get {
      if (_canvas == null)
        _canvas = Minibuffer.instance.transform.parent.GetComponent<Canvas>();
      return _canvas;
    }
  }

  public Vector2 sizeInChars {
    get {
      var rt = window.GetComponent<RectTransform>();
      return new Vector2((rt.sizeDelta.x - 2 * Minibuffer.BORDER_SIZE
                          - Minibuffer.SCROLLBAR_SIZE)
                         / fontCharSize.x * canvas.scaleFactor,
                         (rt.sizeDelta.y - 2 * Minibuffer.BORDER_SIZE)
                         / fontCharSize.y * canvas.scaleFactor);
    }
    set {
      var rt = window.GetComponent<RectTransform>();
      rt.sizeDelta = new Vector2(value.x * fontCharSize.x / canvas.scaleFactor
                                   + 2 * Minibuffer.BORDER_SIZE
                                   + Minibuffer.SCROLLBAR_SIZE,
                                 value.y * fontCharSize.y / canvas.scaleFactor
                                 + 2 * Minibuffer.BORDER_SIZE);
    }
  }

  [Command("window-scroll-up",
           description = "Scroll up in the active window.")]
  public void ScrollUp() {
    ScrollVertical(false);
  }

  [Command("window-scroll-down",
           description = "Scroll down in the active window.")]
  public void ScrollDown() {
    ScrollVertical(true);
  }

  [Command("window-scroll-right",
           description = "Scroll right in the active window.")]
  public void ScrollRight() {
    ScrollHorizontal(true);
  }

  [Command("window-scroll-left",
           description = "Scroll left in the active window.")]
  public void ScrollLeft() {
    ScrollHorizontal(false);
  }

  protected void ScrollVertical(bool down) {
    // Move down a page.
    float pageSize = this.scrollRect.viewport.rect.height;
    float contentSize = this.content.GetComponent<RectTransform>().rect.height;
    // this.content.rectTransform.anchoredPosition =
    //   this.content.rectTransform.anchoredPosition
    //   + new Vector2(0, down ? pageSize : -pageSize);
    this.scrollRect.verticalNormalizedPosition
      = Mathf.Clamp01(this.scrollRect.verticalNormalizedPosition + (down ? -1f : 1f) * pageSize/contentSize);
      //Mathf.Clamp01(this.scrollRect.verticalNormalizedPosition);
    Minibuffer.instance.gui.autocompleteField.ScrollEvent();
  }

  protected void ScrollHorizontal(bool right) {
    // Move down a page.
    float pageSize = this.scrollRect.viewport.rect.width;
    float contentSize = this.content.GetComponent<RectTransform>().rect.width;
    this.scrollRect.horizontalNormalizedPosition
      = Mathf.Clamp01(this.scrollRect.horizontalNormalizedPosition + (! right ? -1f : 1f) * pageSize/contentSize);
    Minibuffer.instance.gui.autocompleteField.ScrollEvent();
  }

  protected bool ResizeWindowHeight(float s, bool fromBottom = true) {
    if (! resizable)
      return false;
    var rt = window.GetComponent<RectTransform>();
    var canvasRect = canvas.GetComponent<RectTransform>().rect;

    /*
      It'd be nice to resize with just the anchors, but because we're
      connecting ourselves to the echo area it's not doable.
     */
    var h = Minibuffer.instance.echoAreaHeight;
    var height = (canvasRect.height - (h + 2 * Minibuffer.BORDER_SIZE)) * s;
    Vector2 min, delta;
    if (! fromBottom) {
      min = new Vector2(rt.offsetMin.x, height);
    } else {
      min = new Vector2(rt.offsetMin.x, Minibuffer.BORDER_SIZE);
    }
    delta = new Vector2(rt.sizeDelta.x, height);
    bool changed = rt.offsetMin != min || rt.sizeDelta != delta;
    rt.offsetMin = min;
    rt.sizeDelta = delta;
    //rt.anchorMax = new Vector2(rt.anchorMax.x, s);
    //Debug.Log("char size " + fontCharSize);
    //Debug.Log("size in chars " + sizeInChars);
    //rt.rect = r;
    return changed;
  }

  protected bool ResizeWindowWidth(float s, bool fromRight = true) {
    if (! resizable)
      return false;
    var rt = window.GetComponent<RectTransform>();
    Vector2 v, w;
    if (fromRight) {
      v = new Vector2(1 - s, rt.anchorMin.y);
      w = new Vector2(1, rt.anchorMax.y);
    } else {
      // from left
      v = new Vector2(0, rt.anchorMin.y);
      w = new Vector2(s, rt.anchorMax.y);
    }
    bool changed = rt.anchorMin != v || rt.anchorMax != w;
    rt.anchorMin = v;
    rt.anchorMax = w;
    return changed;
  }

  [Command("window-fullsize",
           description = "Make main window fullsize")]
  public void FullsizeWindow() {
    ResizeWindowHeight(1f);
    ResizeWindowWidth(1f);
    firstTime = false;
    visible = true;
  }

  [Command("window-halfsize",
           description = "Make main window halfsize")]
  public void HalfsizeWindow() {
    if (! ResizeWindowHeight(0.5f, true))
      ResizeWindowHeight(0.5f, false);
    visible = true;
  }

  [Command("window-split-right",
           description = "Make main window halfsize")]
  public void SplitWindowRight() {
    if (! ResizeWindowWidth(0.5f, true))
      ResizeWindowWidth(0.5f, false);
    visible = true;
  }

  [Command("window-hide",
           description = "Make main window hidden")]
  public void WindowHide() {
    visible = false;
  }

  [Command("toggle-text-wrap",
           description = "Wrap text or not in main window.")]
  public void ToggleTextWrap() {
    wrapText = ! wrapText;
  }

  [Command("auto-scroll",
           description = "Scroll down automatically")]
  public IEnumerator AutoScroll() {
    bool down = true;
    float speed = 0.5f; // page/second
    // Move down a page.
    float pageSize = this.scrollRect.viewport.rect.height;
    float startTime = Time.time;
    float contentSize = this.content.GetComponent<RectTransform>().rect.height;

    while (! down
           ? this.scrollRect.verticalNormalizedPosition < 0.99f
           : this.scrollRect.verticalNormalizedPosition > 0.01f) {
      this.scrollRect.verticalNormalizedPosition
        = Mathf.Clamp01( (down ? 1f : 0f) + (Time.time - startTime)
                        * (down ? -1f : 1f) * speed * pageSize/contentSize);
      Minibuffer.instance.gui.autocompleteField.ScrollEvent();
      yield return null;
    }
      //Mathf.Clamp01(this.scrollRect.verticalNormalizedPosition);
  }

}
}
