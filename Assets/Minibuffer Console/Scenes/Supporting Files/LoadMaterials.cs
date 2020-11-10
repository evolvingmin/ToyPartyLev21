/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {
/*
  Just keeps these materials loaded so that the previews on <kbd>M-x
  quadrapus-material</kbd> will work.
 */
public class LoadMaterials : MonoBehaviour {
  [Header("Keep these materials loaded so previews will work.")]
  public Material[] materials;
}

}
