/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SeawispHunter.MinibufferConsole {

/**
   Provide the current object based on its type.

   There is value in having a context that contains a current object. In the
   Unix shell, a current working directory helps one focus on the files of
   immediate interest. The `[%Current]` attribute allows one to pull from some
   context based on the type of its argument.

   An ICurrentProvider is implemented for the following types: InputField,
   %Minibuffer, and Buffer.  But one can add their own providers.

   For instance, imagine a game with weapons that are degradable. A tester
   wishes to exercise that functionality. Suppose this command exists:

   ```
   [Command]
   public void DegradeWeapon(Weapon weapon) {
     // Your degrade code here.
   }
   ```

   This command works but it will ask the user to select the weapon each time.

   If operating on the player's equipped weapon makes sense and is convenient,
   one can add a class like `CurrentWeaponProvider` that implements %ICurrentProvider.
   Plug that class into %Minibuffer by doing the following:

   ```
   Minibuffer.instance.currentProviders.Add(new CurrentWeaponProvider());
   ```

   Then the command can be updated to this:

   ```
   [Command]
   public void DegradeWeapon([Current] Weapon weapon) {
     // Your degrade code here.
   }
   ```

   Now the user will not be prompted for a weapon, but the method will be
   provided with the "current" weapon.

   \see Current
 */
public interface ICurrentProvider {

  /**
     Return the principle type of this provider.
   */
  Type canonicalType { get; }

  /**
     Return true if it can provide the given type.
  */
  bool CanProvideType(Type t);

  /**
     Return the current object or null.
   */
  object CurrentObject();

}

public class CurrentBuffer : ICurrentProvider {

  public Type canonicalType {
    get { return typeof(IBuffer); }
  }

  public bool CanProvideType(Type t) {
    return t.IsAssignableFrom(CurrentObject().GetType());
  }

  public object CurrentObject() {
    return Minibuffer.instance.gui.main.buffer;
  }
}

public class CurrentInputField : ICurrentProvider {
  public Type canonicalType {
    get { return typeof(InputField); }
  }

  public bool CanProvideType(Type t) {
    var obj = CurrentObject();
    return t.IsAssignableFrom(obj != null ? obj.GetType() : canonicalType);
  }

  /*
    Return the currently selected input field or Minibuffer's input field if we're
    currently editing.
  */
  public object CurrentObject() {
    object obj = null;
    var c = EventSystem.current;
    GameObject go = c != null ? c.currentSelectedGameObject : null;
    if (go != null)
      obj = go.GetComponent<InputField>();
    if ((obj == null
         || ! obj.GetType()
                 .IsAssignableFrom(Minibuffer.instance.gui.input.GetType()))
         && Minibuffer.instance.editing)
      obj = Minibuffer.instance.gui.input;
    return obj;
  }
}

public class CurrentMinibuffer : ICurrentProvider {

  public Type canonicalType {
    get { return typeof(Minibuffer); }
  }

  public bool CanProvideType(Type t) {
    return t.IsAssignableFrom(canonicalType);
  }

  public object CurrentObject() {
    return Minibuffer.instance;
  }
}

} // end namespace
