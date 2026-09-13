using UnityEngine;
using UnityEngine.InputSystem;

namespace EiraGame
{
    /// <summary>Teclado vía el nuevo Input System (activeInputHandler = New).</summary>
    public static class Inputs
    {
        public static bool Press(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].isPressed;
        }

        public static bool KeyDown(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].wasPressedThisFrame;
        }

        public static float MoveAxis()
        {
            var kb = Keyboard.current;
            if (kb == null) return 0f;
            float v = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v += 1f;
            return v;
        }

        public static bool Jump()      => KeyDown(Key.Space) || KeyDown(Key.W) || KeyDown(Key.UpArrow);
        public static bool JumpHeld()  => Press(Key.Space) || Press(Key.W) || Press(Key.UpArrow);
        public static bool Interact()  => KeyDown(Key.E) || KeyDown(Key.Z);
        public static bool Pulse()     => KeyDown(Key.X) || KeyDown(Key.J);
        public static bool Start()     => KeyDown(Key.E) || KeyDown(Key.Space);
        public static bool Restart()   => KeyDown(Key.R);
        public static bool PauseKey()  => KeyDown(Key.Escape);

        public static bool Number(int n)
        {
            var kb = Keyboard.current;
            if (kb == null || n < 1 || n > 4) return false;
            var dig = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
            var pad = new[] { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4 };
            return kb[dig[n - 1]].wasPressedThisFrame || kb[pad[n - 1]].wasPressedThisFrame;
        }
    }
}