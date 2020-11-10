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
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Reflection;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {

/*
  Highlights the line the caret is currently on.
 */
[RequireComponent(typeof(InputField))]
public class HighlightLine : UIBehaviour {
  InputField m_InputField;
  Text m_TextComponent;
  RectTransform highlightRectTrans;
  [SerializeField]
  private RectTransform m_Mask = null;
  private int lastSelectedLineDrawn = -1;
  // private int zeroCount = 0;

  protected override void Start() {
    base.Start();
    m_InputField = GetComponent<InputField>();
    Assert.IsNotNull(m_InputField);
    m_TextComponent = m_InputField.textComponent;
    Assert.IsNotNull(m_TextComponent);
    UpdateGeometry();
  }

  void Update() {
    var line = currentLine;
    // We skip a spurious 0 line update.
    if (lastSelectedLineDrawn < 0 || lastSelectedLineDrawn != line) {
      // if (line == 0 && zeroCount++ < 1)
      //   return;
      // zeroCount = 0;
      UpdateGeometry();
      // Debug.Log("drawing highlight " + line + "; last selected line " + lastSelectedLineDrawn);
      lastSelectedLineDrawn = line;
    }
  }

  public void ForceRedraw() {
    lastSelectedLineDrawn = -1;
  }

  TextGenerator cachedInputTextGenerator {
    get { return m_TextComponent.cachedTextGenerator; }
  }

  /*
    Returns the line index of the selected line if any otherwise returns -1.
  */
  public int currentLine {
    get {
      return m_InputField.lineIndexForPosition(m_InputField.caretPosition);
    }
    set {
      // Debug.Log("Set current line " + value);
      if (value < 0 || value >= lineCount)
        return;
      m_InputField.caretPosition = m_InputField.positionForLineIndex(value);
    }
  }

  public int lineCount {
    get {
      return cachedInputTextGenerator.lineCount;
    }
  }

  public bool isLineVisible {
    get {
      if (m_Mask == null || m_LineVerts == null)
        return true;
      else {
        var rectTransform = m_Mask;
        var topWorldPos
          = m_TextComponent.rectTransform.TransformPoint(m_LineVerts[3].position);
        var bottomWorldPos
          = m_TextComponent.rectTransform.TransformPoint(m_LineVerts[0].position);
        var bottomLeft = rectTransform.TransformPoint(rectTransform.rect.min);
        var topRight = rectTransform.TransformPoint(rectTransform.rect.max);
        return (topWorldPos.y >= bottomLeft.y
                && topWorldPos.y <= topRight.y
                && bottomWorldPos.y >= bottomLeft.y
                && bottomWorldPos.y <= topRight.y);
      }
    }
  }

  public void Rebuild(CanvasUpdate update)
  {
    print("Rebuild");
    switch (update)
    {
      case CanvasUpdate.LatePreRender:
        UpdateGeometry();
        break;
    }
  }

  private Mesh m_Mesh;
  protected Mesh mesh
  {
    get
    {
      if (m_Mesh == null)
        m_Mesh = new Mesh();
      return m_Mesh;
    }
  }
  private CanvasRenderer m_CachedInputRenderer;
  private void UpdateGeometry() {
    // print("UpdateGeometry");
    #if UNITY_EDITOR
    if (!Application.isPlaying)
      return;
    #endif
    if (m_CachedInputRenderer == null && m_TextComponent != null)
    {
      GameObject go = new GameObject(transform.name + " Highlight Line", typeof(RectTransform), typeof(CanvasRenderer));
      go.hideFlags = HideFlags.DontSave;
      go.transform.SetParent(m_TextComponent.transform.parent);
      go.transform.SetAsFirstSibling();
      go.layer = gameObject.layer;

      highlightRectTrans = go.GetComponent<RectTransform>();
      m_CachedInputRenderer = go.GetComponent<CanvasRenderer>();
      m_CachedInputRenderer.SetMaterial(m_TextComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);

      // Needed as if any layout is present we want the caret to always be the same as the text area.
      go.AddComponent<LayoutElement>().ignoreLayout = true;

      AssignPositioningIfNeeded();
    }

    if (m_CachedInputRenderer == null)
      return;

    OnFillVBO(mesh);
    m_CachedInputRenderer.SetMesh(mesh);
  }

  private void OnFillVBO(Mesh vbo)
  {
    using (var helper = new VertexHelper())
    {

      Rect inputRect = m_TextComponent.rectTransform.rect;
      Vector2 extents = inputRect.size;

      // get the text alignment anchor point for the text in local space
      Vector2 textAnchorPivot = Text.GetTextAnchorPivot(m_TextComponent.alignment);
      Vector2 refPoint = Vector2.zero;

      refPoint.x = Mathf.Lerp(inputRect.xMin, inputRect.xMax, textAnchorPivot.x);
      refPoint.y = Mathf.Lerp(inputRect.yMin, inputRect.yMax, textAnchorPivot.y);

      // Adjust the anchor point in screen space
      Vector2 roundedRefPoint = m_TextComponent.PixelAdjustPoint(refPoint);

      // Determine fraction of pixel to offset text mesh.
      // This is the rounding in screen space, plus the fraction of a pixel the text anchor pivot is from the corner of the text mesh.
      Vector2 roundingOffset = roundedRefPoint - refPoint + Vector2.Scale(extents, textAnchorPivot);
      roundingOffset.x = roundingOffset.x - Mathf.Floor(0.5f + roundingOffset.x);
      roundingOffset.y = roundingOffset.y - Mathf.Floor(0.5f + roundingOffset.y);

      if (currentLine >= 0)
        GenerateLineHighlight(helper, roundingOffset, currentLine);

      helper.FillMesh(vbo);
    }
  }

  private FieldInfo drawStartField;
  private int m_DrawStart {
    get {
      if (drawStartField == null)
        drawStartField = typeof(InputField).GetField("m_DrawStart", BindingFlags.NonPublic | BindingFlags.Instance);
      return (int) drawStartField.GetValue(m_InputField);
    }
  }

  protected UIVertex[] m_LineVerts = null;
  [SerializeField]
  private Color m_LineColor = new Color(168f / 255f, 206f / 255f, 255f / 255f, 192f / 255f);
  private void GenerateLineHighlight(VertexHelper vbo, Vector2 roundingOffset, int lineIndex)
  {
    // print("GenerateLineHighlight");
    if (m_LineVerts == null)
    {
      m_LineVerts = CreateCursorVerts();
    }

    //float width = m_CaretWidth;
    TextGenerator gen = m_TextComponent.cachedTextGenerator;

    if (gen == null)
      return;

    if (gen.lineCount == 0)
      return;
    if (lineIndex >= gen.lineCount || lineIndex < 0)
      return;

    int charPos = gen.lines[lineIndex].startCharIdx;
    int adjustedPos = Mathf.Max(0, charPos - m_DrawStart);

    Vector2 startPosition = Vector2.zero;

    // Calculate startPosition
    // if (adjustedPos < gen.characters.Count)
    // {
    //   UICharInfo cursorChar = gen.characters[adjustedPos];
    //   startPosition.x = cursorChar.cursorPos.x;
    // }
    startPosition.x = gen.characters[charPos].cursorPos.x;
    startPosition.x /= m_TextComponent.pixelsPerUnit;

    // TODO: Only clamp when Text uses horizontal word wrap.
    // if (startPosition.x > m_TextComponent.rectTransform.rect.xMax)
    //   startPosition.x = m_TextComponent.rectTransform.rect.xMax;
    startPosition.x = m_TextComponent.rectTransform.rect.xMin;

    int characterLine = DetermineCharacterLine(adjustedPos, gen);
    startPosition.y = gen.lines[characterLine].topY / m_TextComponent.pixelsPerUnit;
    float height = gen.lines[characterLine].height / m_TextComponent.pixelsPerUnit;


    for (int i = 0; i < m_LineVerts.Length; i++)
      m_LineVerts[i].color = m_LineColor;

    m_LineVerts[0].position = new Vector3(startPosition.x, startPosition.y - height, 0.0f);
    m_LineVerts[1].position = new Vector3(//startPosition.x + width,
                                          m_TextComponent.rectTransform.rect.xMax,
                                          startPosition.y - height, 0.0f);
    m_LineVerts[2].position = new Vector3(//startPosition.x + width,
                                          m_TextComponent.rectTransform.rect.xMax,
                                          startPosition.y, 0.0f);
    m_LineVerts[3].position = new Vector3(startPosition.x, startPosition.y, 0.0f);

    // if (! isLineVisible) {
    //   print("outside of mask; don't draw.");
    //   return;
    // }
    if (roundingOffset != Vector2.zero)
    {
      for (int i = 0; i < m_LineVerts.Length; i++)
      {
        UIVertex uiv = m_LineVerts[i];
        uiv.position.x += roundingOffset.x;
        uiv.position.y += roundingOffset.y;
      }
    }

    vbo.AddUIVertexQuad(m_LineVerts);

    int screenHeight = Screen.height;
    // Removed multiple display support until it supports none native resolutions(case 741751)
    //int displayIndex = m_TextComponent.canvas.targetDisplay;
    //if (Screen.fullScreen && displayIndex < Display.displays.Length)
    //    screenHeight = Display.displays[displayIndex].renderingHeight;

    startPosition.y = screenHeight - startPosition.y;
    //Input.compositionCursorPos = startPosition;
  }
  #region InputField copies

  private static int GetLineStartPosition(TextGenerator gen, int line)
  {
    line = Mathf.Clamp(line, 0, gen.lines.Count - 1);
    return gen.lines[line].startCharIdx;
  }

  private static int GetLineEndPosition(TextGenerator gen, int line)
  {
    line = Mathf.Max(line, 0);
    if (line + 1 < gen.lines.Count)
      return gen.lines[line + 1].startCharIdx - 1;
    return gen.characterCountVisible;
  }

  // private bool hasSelection { get { return caretPositionInternal != caretSelectPositionInternal; } }

  private int DetermineCharacterLine(int charPos, TextGenerator generator)
  {
    for (int i = 0; i < generator.lineCount - 1; ++i)
    {
      if (generator.lines[i + 1].startCharIdx > charPos)
        return i;
    }

    return generator.lineCount - 1;
  }
  // private void MarkGeometryAsDirty()
  // {
  //   #if UNITY_EDITOR
  //   if (!Application.isPlaying || UnityEditor.PrefabUtility.GetPrefabObject(gameObject) != null)
  //     return;
  //   #endif

  //   CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
  // }
  private void AssignPositioningIfNeeded()
  {
    if (m_TextComponent != null && highlightRectTrans != null &&
        (highlightRectTrans.localPosition != m_TextComponent.rectTransform.localPosition ||
         highlightRectTrans.localRotation != m_TextComponent.rectTransform.localRotation ||
         highlightRectTrans.localScale != m_TextComponent.rectTransform.localScale ||
         highlightRectTrans.anchorMin != m_TextComponent.rectTransform.anchorMin ||
         highlightRectTrans.anchorMax != m_TextComponent.rectTransform.anchorMax ||
         highlightRectTrans.anchoredPosition != m_TextComponent.rectTransform.anchoredPosition ||
         highlightRectTrans.sizeDelta != m_TextComponent.rectTransform.sizeDelta ||
         highlightRectTrans.pivot != m_TextComponent.rectTransform.pivot))
    {
      highlightRectTrans.localPosition = m_TextComponent.rectTransform.localPosition;
      highlightRectTrans.localRotation = m_TextComponent.rectTransform.localRotation;
      highlightRectTrans.localScale = m_TextComponent.rectTransform.localScale;
      highlightRectTrans.anchorMin = m_TextComponent.rectTransform.anchorMin;
      highlightRectTrans.anchorMax = m_TextComponent.rectTransform.anchorMax;
      highlightRectTrans.anchoredPosition = m_TextComponent.rectTransform.anchoredPosition;
      highlightRectTrans.sizeDelta = m_TextComponent.rectTransform.sizeDelta;
      highlightRectTrans.pivot = m_TextComponent.rectTransform.pivot;
    }
  }
  #endregion

  #region InputField modified

  private UIVertex[] CreateCursorVerts()
  {
    var vs = new UIVertex[4];

    for (int i = 0; i < vs.Length; i++)
    {
      vs[i] = UIVertex.simpleVert;
      vs[i].uv0 = Vector2.zero;
    }
    return vs;
  }

  protected override void OnEnable()
  {
    base.OnEnable();
    if (m_TextComponent != null)
    {
      m_TextComponent.RegisterDirtyMaterialCallback(UpdateLineMaterial);
    }
    UpdateLineMaterial();
    UpdateGeometry();
  }

  protected override void OnDisable()
  {
    if (m_TextComponent != null)
    {
      m_TextComponent.UnregisterDirtyMaterialCallback(UpdateLineMaterial);
    }
    // CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

    // Clear needs to be called otherwise sync never happens as the object is disabled.
    if (m_CachedInputRenderer != null)
      m_CachedInputRenderer.Clear();

    if (m_Mesh != null)
      DestroyImmediate(m_Mesh);
    m_Mesh = null;

    base.OnDisable();
  }

  private void UpdateLineMaterial()
  {
    if (m_TextComponent != null && m_CachedInputRenderer != null)
      m_CachedInputRenderer.SetMaterial(m_TextComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);
  }
  #endregion
}

}
