/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis

  * * *

  Licensed under the MIT License

  Permission is hereby granted, free of charge, to any person
  obtaining a copy of this software and associated documentation files
  (the "Software"), to deal in the Software without restriction,
  including without limitation the rights to use, copy, modify, merge,
  publish, distribute, sublicense, and/or sell copies of the Software,
  and to permit persons to whom the Software is furnished to do so,
  subject to the following conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
  BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
  ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
  CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

namespace SeawispHunter.MinibufferConsole {

/**
   ![Show commands, etc in inspector](minibuffer-listing.png)
   Display listing of commands, variables, and key bindings in Inspector window.

   Whatever commands, variables, and key bindings that are provided by the
   script and knowable at compile-time are shown in the inspector by adding the
   following line to the script.

   ```
     public MinibufferListing minibufferExtensions;
   ```

   \note This is a dummy class. This class is not instantiable. It is used
   purely to invoke the MinibufferListingDrawer class for the inspector.
 */
/** \ingroup MIT */
[System.Serializable]
public class MinibufferListing {
  /**
    It's not instantiable.
   */
  private MinibufferListing() {}
}

}
