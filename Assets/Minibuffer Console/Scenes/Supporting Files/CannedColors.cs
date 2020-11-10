/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Collections;
using SeawispHunter.MinibufferConsole;

public class CannedColors : MonoBehaviour {
  public Color[] colors;
  private int index = 0;
  // Use this for initialization
  void Start () {
    Minibuffer.Register(this);
  }

  [Command(keyBinding = "C-x c")]
  public void NextColor() {
    if (index < colors.Length) {
      StartCoroutine(FadeCamera(Camera.main, colors[index], 3f));
      // Camera.main.backgroundColor = colors[index];
      index++;
    } else {
      index = 0;
    }
  }

  IEnumerator FadeCamera(Camera c, Color endColor, float duration) {
    Color initColor = c.backgroundColor;
    float start = Time.time;
    float t;
    while ((t = Time.time - start) < duration) {
      c.backgroundColor = Color.Lerp(initColor, endColor, t/duration);
      yield return new WaitForEndOfFrame();
    }
  }
}
