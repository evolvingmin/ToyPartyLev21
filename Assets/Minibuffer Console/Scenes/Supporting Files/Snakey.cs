/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {
/*
  Makes a hinge joint move sinusoidally depending on the amplitude and
  frequency, only used in the examples.
 */
public class Snakey : MonoBehaviour {
  public float amplitude = 10f;
  public float frequency = 1f;

  // Use this for initialization
  void Start () {
  }

  void OnEnable() {
    BrainUpdate();
  }

  void BrainUpdate() {
    if (GetComponent<HingeJoint>() == null || ! enabled)
      return;
    JointSpring spring = GetComponent<HingeJoint>().spring;
    spring.targetPosition = amplitude * Mathf.Sin(Time.fixedTime);
    GetComponent<HingeJoint>().spring = spring;
    Invoke("BrainUpdate", frequency);
  }
}
}
