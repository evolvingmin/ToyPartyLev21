/*
  Copyright (c) 2017 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;

namespace SeawispHunter.MinibufferConsole {
/**
 */

/* Consider turning this into a property and drawer a la:
   http://answers.unity3d.com/questions/444312/having-text-or-notes-in-the-inspector.html
*/
public class Note : MonoBehaviour {
  [System.Serializable]
  public struct NoteData {
    public Texture image;
    [TextArea]
    public string text;
    [TextArea(1,1)]
    public string link;
  }

  public NoteData editNotes;
}

}
