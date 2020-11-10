/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {
/*
  Cycle each child on then off then go to the next child.
 */
public class ChildrenCycler : MonoBehaviour {
  public int index = 0;
  private bool firstTime = true;
  void Start () { }

  public void Next() {
    Move(true);
  }

  private void Move(bool forward) {
    if (! enabled)
      return;

    var child = transform.GetChild(index);
    var on = child.gameObject.activeSelf;
    if (on) {
      // If it's on, turn it off.
      child.gameObject.SetActive(false);
    } else {
      // If it's not on, go to the next child and turn it on.
      if (! firstTime) {
        if (forward) {
          index = (index + 1) % transform.childCount;
        } else {
          index = (index - 1);
          if (index < 0)
            index = transform.childCount - 1;
        }
      }
      child = transform.GetChild(index);
      child.gameObject.SetActive(true);
    }
    firstTime = false;
  }

  public void Previous() {
    Move(false);
  }

}

}
