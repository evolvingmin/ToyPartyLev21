/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {
/*
  This component will show the contents of the current buffer on a UI.Text
  object, and scroll it automatically. It should probably be disabled initially.
 */
public class ScrollUp : MonoBehaviour {
  public float speed;
  public MinibufferListing minibufferExtensions;

  void Start() {
    Minibuffer.Register(this);
  }

  // Update is called once per frame
  void Update () {
    transform.position += Vector3.up * speed;
  }

  [Command]
  public void ShowLikeCredits([Current] IBuffer b) {
    var p = transform.position;
    p.y = 0f;
    transform.position = p;
    enabled = true;
    GetComponent<Text>().text = b.content;
  }
}

}
