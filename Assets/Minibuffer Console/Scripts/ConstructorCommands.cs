/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SeawispHunter.MinibufferConsole {
/*

*/
[Group("constructors", tags = new [] { "hide-group", "built-in" })]
public static class ConstructorCommands {

  static ConstructorCommands() {
    // We register in Minibuffer.Start() because it's totally static.
    // Minibuffer.RegisterOnce(typeof(ConstructorCommands));
  }

  [Command(description = "Creates a Vector2 (internal use)")]
  public static Vector2 MakeVector2(float x, float y) {
    return new Vector2(x, y);
  }

  // Hmm... There's a sense here that I could just use commands and
  // the command's parameters could be used to build composite types.
  // These are completers in a sense.
  [Command(description = "Creates a Vector3 (internal use)")]
  public static Vector3 MakeVector3(float x, float y, float z) {
    return new Vector3(x, y, z);
  }

  [Command(description = "Creates a Vector4 (internal use)")]
  public static Vector4 MakeVector4(float x, float y, float z, float w) {
    return new Vector4(x, y, z, w);
  }

  [Command(description = "Creates a Quaternion (internal use)")]
  public static Quaternion MakeQuaternion(float angleInDegrees, Vector3 axis) {
    return Quaternion.AngleAxis(angleInDegrees, axis);
  }

}
}
