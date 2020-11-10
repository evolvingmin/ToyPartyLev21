/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {
/*
  Makes a hinge joint twitch randomly depending on the amplitude and
  frequency, only used in the examples.
 */
public class Twitchy : MonoBehaviour {
  public float amplitude = 10f;
  public float frequency = 1f;
  void Start () {
  }

  void OnEnable() {
    BrainUpdate();
  }

  void BrainUpdate() {
    if (! enabled)
      return;
    JointSpring spring = GetComponent<HingeJoint>().spring;
    spring.targetPosition = Random.Range(-amplitude, amplitude);
    GetComponent<HingeJoint>().spring = spring;
    Invoke("BrainUpdate", frequency);
  }
}
}
