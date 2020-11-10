/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Linq;
using System.Collections;
using SeawispHunter.MinibufferConsole;
using SeawispHunter.MinibufferConsole.Extensions;

/**
   ![Secret key in the inspector](inspector/secret-key.png)
   Toggle %Minibuffer with a Secret Key.

   Suppose a developer wants to include %Minibuffer in their game but they'd
   prefer players not accidentally bump into. They could disable Minibuffer, but
   then their testers wouldn't have access to it unless they want to provide two
   separate builds. %SecretKeyToggle lets one include %Minibuffer with some
   obscurity.

   Instead a developer could provide one build of their game and enable this
   SecretKey script; it's called 'Secret Activator' in the '%Minibuffer Console'
   prefab. Select their magic key chord, say, <kbd>alt-\`</kbd> and only tell
   their testers about the secret <kbd>alt-\`</kbd> that activates %Minibuffer.

   Or if obfuscation is not important, one might add a 'Developer Mode' in their
   settings that activates %Minibuffer.
 */
public class SecretKey : MonoBehaviour {
  public enum TargetState {
    Activate,
    Deactivate,
    LeaveAsIs
  };
  [Header("Will (de)activate Minibuffer when this key chord is pressed.")]
  public string _keyChord = "`";
  private KeyChord keyChord;
  [Header("Warning: this is not the same as toggling Minibuffer's visibility.")]
  public GameObject target;
  public TargetState setInitialTargetState = TargetState.LeaveAsIs;
  private float lastTimeToggled;
  private const float minTimeBetweenToggles = 0.2f;

  IEnumerator Start() {
    if (target == null) {
        Debug.LogWarning("No target selected for secret key; disabling.");
        this.enabled = false;
        yield break;
    }
    lastTimeToggled = Time.unscaledTime - minTimeBetweenToggles;
    try {
      keyChord = KeyChord.FromString(_keyChord);
      keyChord = keyChord.Canonical();
    } catch (MinibufferException me) {
      Debug.LogException(me);
    }

    // Let's yield so that if there was any setup in the target object, it
    // happens. If we don't do this, sometimes it sets up; sometimes it doesn't.
    // Why introduce that kind of random behavior if we don't have to.
    yield return null;

    switch (setInitialTargetState) {
      case TargetState.LeaveAsIs:
        break;
      case TargetState.Activate:
        target.SetActive(true);
        break;
      case TargetState.Deactivate:
        target.SetActive(false);
        break;
    }
  }

  void OnGUI() {
    if (! Event.current.isKey || keyChord == null)
      return;
    var kc = KeyChord.FromEvent(Event.current).Canonical();
    if (keyChord.Equals(kc))
      this.DoAfter(new WaitForEndOfFrame(), () => Toggle());
  }

  void Toggle() {
    if ((Time.unscaledTime - lastTimeToggled) > minTimeBetweenToggles) {
      target.SetActive(! target.activeSelf);
      lastTimeToggled = Time.unscaledTime;
    }
  }
}
