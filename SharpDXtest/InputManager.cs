using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.DirectInput;

namespace SharpDXtest
{
    public static class InputManager
    {
        // Windows message listener
        private static DirectInput inputListener;

        // Devices
        private static Keyboard keyboard;
        private static Mouse mouse;

        // States
        private static KeyboardState prevKeyboardState;
        private static KeyboardState keyboardState;
        private static MouseState prevMouseState;
        private static MouseState mouseState;

        internal static void Init()
        {
            inputListener = new DirectInput();

            keyboard = new Keyboard(inputListener);
            keyboard.Acquire();
            keyboardState = keyboard.GetCurrentState();
            prevKeyboardState = keyboardState;

            mouse = new Mouse(inputListener);
            mouse.Acquire();
            mouseState = mouse.GetCurrentState();
            prevMouseState = mouseState;
        }
        internal static void OnUpdateFrame()
        {
            prevKeyboardState = keyboardState;
            keyboardState = keyboard.GetCurrentState();

            prevMouseState = mouseState;
            mouseState = mouse.GetCurrentState();
        }
        public static bool IsKeyDown(Key key)
        {
            return !prevKeyboardState.IsPressed(key) && keyboardState.IsPressed(key);
        }
        public static bool IsKeyUp(Key key)
        {
            return prevKeyboardState.IsPressed(key) && !keyboardState.IsPressed(key);
        }
        public static bool IsKeyPressed(Key key)
        {
            return keyboardState.IsPressed(key);
        }
        // Same as key up
        public static bool IsKeyReleased(Key key)
        {
            return IsKeyUp(key);
        }
        public static Vector2 GetMouseDelta()
        {
            return new Vector2(mouseState.X - prevMouseState.X, mouseState.Y - prevMouseState.Y);
        }
    }
}