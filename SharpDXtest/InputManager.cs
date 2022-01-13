using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DirectInput;
using System.Diagnostics;

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
        internal static void Update()
        {
            prevKeyboardState = keyboardState;
            keyboardState = keyboard.GetCurrentState();

            prevMouseState = mouseState;
            mouseState = mouse.GetCurrentState();
        }
        public static bool IsKeyDown(Key key)
        {
            return keyboardState.IsPressed(key);
        }
        public static bool IsKeyUp(Key key)
        {
            return !keyboardState.IsPressed(key);
        }
        public static bool IsKeyPressed(Key key)
        {
            return !prevKeyboardState.IsPressed(key) && keyboardState.IsPressed(key);
        }
        public static bool IsKeyReleased(Key key)
        {
            return prevKeyboardState.IsPressed(key) && !keyboardState.IsPressed(key);
        }

        public static bool IsMouseButtonDown(int button)
        {
            return mouseState.Buttons[button];
        }
        public static bool IsMouseButtonUp(int button)
        {
            return !mouseState.Buttons[button];
        }
        public static bool IsMouseButtonPressed(int button)
        {
            return !prevMouseState.Buttons[button] && mouseState.Buttons[button];
        }
        public static bool IsMouseButtonReleased(int button)
        {
            return prevMouseState.Buttons[button] && !mouseState.Buttons[button];
        }
        public static Vector2 GetMouseDelta()
        {
            return new Vector2(mouseState.X, mouseState.Y);
        }
    }
}