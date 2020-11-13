using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ToyParty.Patterns;

namespace ToyParty
{
    public enum InputState
    {
        Idle,
        Lock
    }

    public class InputManager : SingletonMono<InputManager>
    {
        public InputState PlayState = InputState.Idle;
    }

}

