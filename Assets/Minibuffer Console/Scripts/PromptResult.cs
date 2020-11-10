/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/

namespace SeawispHunter.MinibufferConsole {

/**
   Return the user input string _and_ coerced object (if any).

   PromptResult provides the string the user input `str` and the object (if any)
   that it was coerced to `obj`.

   Use this as a return type to get the input string that was used.

   ```
    public struct PromptResult {
      // The user provided this string as input.
      public string str;
      // The ICoercer ultimately provided this object as output.
      public object obj;
    }
   ```

   There is also a generic version of PromptResult: PromptResult<T>. See
   Prompt.completions show an instance of how and why you might want to use
   this.

   ```
   public struct PromptResult<T> {
    // The user provided this string as input.
    public string str;
    // The ICoercer ultimately provided this object as output.
    public T obj;
   }
   ```
*/
public struct PromptResult {
  /** The user provided this string as input. */
  public string str;
  /** The ICoercer ultimately provided this object as output. */
  public object obj;
}

/* Use PromptResult<T> as a return type to get the input string that was used. */
public struct PromptResult<T> {
  /* The user provided this string as input. */
  public string str;
  /** The ICoercer ultimately provided this object as output or null if no
      coercion was possible. */
  public T obj;
}
}
