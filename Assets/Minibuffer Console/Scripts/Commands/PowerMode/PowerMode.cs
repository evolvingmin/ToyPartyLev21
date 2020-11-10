/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/**
  ![Power mode in the inspector](inspector/power-mode.png)
  Make typing fun again! Type <kbd>M-x power-mode</kbd> to begin.

  Inspired by [activate-power-mode](https://atom.io/packages/activate-power-mode) for Atom.
*/
[Group(tag = "built-in")]
public class PowerMode : MonoBehaviour {

  public bool powerMode;
  [System.Serializable]
  public struct Effects {
    public bool fireworksOnInput;
    public bool shakeCamera;
    public bool shakeUI;
    public bool rumbleLetters;
  }

  [Header("Enable which effects?")]
  public Effects effects;

  [System.Serializable]
  public struct GUI {
    public CameraShake powerModeShake;
    public UIShake uiShake;
    public LetterRumble letterRumble;
    public ParticleSystem cursorParticle;
  }


  public float chanceOfWave = 0.40f; // 40% chance of initiating a wave on each key press.


  private bool setupCameraOnLevelChange = false;
  public bool preserveOnSceneChange = true;

  public GUI GUIElements;
  public MinibufferListing minibufferExtensions;

  void Start() {
    // Debug.Log("Registering PowerMode");
    Minibuffer.Register(this);
    Minibuffer.With(minibuffer => {
        if (powerMode) {
          powerMode = false;
          this.DoNextTick(() => TogglePowerMode());
        }
      });
    #if UNITY_5_4_OR_NEWER
    SceneManager.sceneLoaded += SceneLoaded;
    #endif
  }

  void OnDestroy() {
    Minibuffer.Unregister(this);
  }

  private bool CaretPosition(InputField inputField, out Vector3 position) {
    // var caretRectTrans = inputField.caretRectTrans;
    var caretRectTrans = (RectTransform)
      typeof(InputField)
      .GetField("caretRectTrans", BindingFlags.NonPublic | BindingFlags.Instance)
      .GetValue(inputField);
    var m_CursorVerts
      = (UIVertex[]) typeof(InputField).GetField("m_CursorVerts", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inputField);
   if (m_CursorVerts != null
       && m_CursorVerts.Length == 4
       && caretRectTrans != null) {
     int index = Random.Range(0, 4);
     var pos = m_CursorVerts[index].position;
     // Vector3 worldPoint;
     // RectTransformUtility
     //   .ScreenPointToWorldPointInRectangle(caretRectTrans,
     //                                       //new Vector2(pos.x, pos.y),
     //                                       Vector2.zero,
     //                                       Camera.main,
     //                                       out worldPoint);
     position = caretRectTrans.TransformPoint(pos);
     return true;
     //return worldPoint;
     //return caretRectTrans.rect.center;
     //return pos;
   } else {
     position = Vector3.zero;
     return false;
   }
  }

  /*
    This method is called by the On Value Changed event for the
    TappedInputField prompt.
   */
  public void ParticleSprayOverCursor() {
    if (powerMode) {
      // var minibufferPrompt = Minibuffer.instance.gui.input;
      // var cpos = minibufferPrompt.caretPosition;
      Vector3 cpos;
      if (! CaretPosition(Minibuffer.instance.gui.input, out cpos))
        return;
      // print("cursor " + cpos);
      // var pos = Camera.main.ScreenToWorldPoint(cpos);
      // print("world " + pos);
      if (effects.fireworksOnInput) {
        var particle = (ParticleSystem) Instantiate(GUIElements.cursorParticle, cpos, Quaternion.Euler(-90f, 0f, 0f));
        particle.transform.parent = Minibuffer.instance.transform.parent;
      }
      if (effects.shakeCamera && GUIElements.powerModeShake != null && GUIElements.powerModeShake.enabled) {
        if (GUIElements.powerModeShake.camTransform == null) {
          Debug.LogWarning("PowerMode: Setting the Main Camera as the powerModeShake camera.");
          GUIElements.powerModeShake.camTransform = Camera.main.transform;
        }
        // http://answers.unity3d.com/questions/840538/how-can-i-draw-a-particlesystem-over-the-new-unity.html
        // alt: http://answers.unity3d.com/questions/1005298/how-to-do-screenshake-without-influencing-the-tran.html
        GUIElements.powerModeShake.shakeDuration = 0.1f;
      }

      if (effects.shakeUI && GUIElements.uiShake) {
        GUIElements.uiShake.Shake();
      }

      if (effects.rumbleLetters && GUIElements.letterRumble != null) {
        if (Random.value < chanceOfWave)
          GUIElements.letterRumble.StartWaveRandom(cpos);
      }
    }
  }

  [Command("power-mode",
           description = "Make typing fun again.")]
  public void TogglePowerMode() {
    powerMode = !powerMode;
    if (powerMode) {

      if (! effects.rumbleLetters
          && ! effects.shakeUI
          && ! effects.shakeCamera
          && ! effects.fireworksOnInput) {
        Debug.LogWarning("No effects enabled for power-mode. Won't be very exciting.");
      }

      // Check Minibuffer Canvas set to Screen Space - Camera
      // Render Camera is set and order in layer to 0.
      var c = Minibuffer.instance.transform.parent.gameObject.GetComponent<Canvas>();
      if (c.renderMode != RenderMode.ScreenSpaceCamera
          || c.worldCamera == null
          || c.sortingOrder != 0) {
        Minibuffer.instance
          .ReadYesOrNo("Warning: power-mode particles will not work unless Minibuffer "
                       + "Canvas set to 'Screen Space - Camera', and Render Camera set; change them?")
          .Then(yes =>
              {
                if (yes) {
                  setupCameraOnLevelChange = true;
                  SetupCamera();
                  Minibuffer.instance.Message("You have THE POWER!");
                } else {
                  Minibuffer.instance.Message("You have a POWER!");
                }
              });
      } else {
        Minibuffer.instance.Message("You have the POWER!");
      }
    } else {
      setupCameraOnLevelChange = false;
      Minibuffer.instance.Message("You had the power.");
    }
  }

  public void SetupCamera() {
    Debug.Log("Setting up canvas for power-mode.");
    var c = Minibuffer.instance.transform.parent.gameObject.GetComponent<Canvas>();
    c.renderMode = RenderMode.ScreenSpaceCamera;
    c.worldCamera = Camera.main;
    c.sortingOrder = 0;
  }

  #if UNITY_5_4_OR_NEWER
  void SceneLoaded(Scene scene, LoadSceneMode mode) {
  #else
  void OnLevelWasLoaded(int level) {
  #endif
    if (! enabled)
      return;
    if (preserveOnSceneChange && setupCameraOnLevelChange && powerMode)
      SetupCamera();
    if (! preserveOnSceneChange && powerMode)
      TogglePowerMode();
  }
}
}
