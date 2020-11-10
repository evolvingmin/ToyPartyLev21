/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {

public class RandomizeColor : MonoBehaviour {

  void Start () {
    GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
  }
}
}
