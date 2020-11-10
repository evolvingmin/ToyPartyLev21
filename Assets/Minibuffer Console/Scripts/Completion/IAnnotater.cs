
/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

using System;

namespace SeawispHunter.MinibufferConsole {
/**
   Annotate completions.

   ![Annotations off](files/annotations-off.png)

   You can add annotations to completions. For instance, when show-annotations
   is on when running execute-extended-command <kbd>M-x</kbd>, a brief
   description for each command is shown.

   <br>
   <br>
   <br>
   <br>
   <br>
   <br>
   <br>
   <br>
   ![Annotations on](files/annotations-on.png)
*/
public interface IAnnotater {
  /**
     Annotate a completion. Return null if no annotation is available.
  */
  string Annotate(string completion);
}

public class NoAnnotater : IAnnotater {
  public string Annotate(string completion) {
    return null;
  }
}
}
