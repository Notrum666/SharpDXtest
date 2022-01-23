﻿using System;
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
        private static KeyboardState prevFixedKeyboardState;
        private static KeyboardState fixedKeyboardState;
        private static MouseState prevMouseState;
        private static MouseState mouseState;
        private static MouseState prevFixedMouseState;
        private static MouseState fixedMouseState;

        internal static void Init()
        {
            inputListener = new DirectInput();

            keyboard = new Keyboard(inputListener);
            keyboard.Acquire();
            keyboardState = keyboard.GetCurrentState();
            prevKeyboardState = keyboardState;
            fixedKeyboardState = keyboardState;
            prevFixedKeyboardState = keyboardState;

            mouse = new Mouse(inputListener);
            mouse.Acquire();
            mouseState = mouse.GetCurrentState();
            prevMouseState = mouseState;
            fixedMouseState = mouseState;
            prevFixedMouseState = mouseState;
        }
        internal static void Update()
        {
            prevKeyboardState = keyboardState;
            keyboardState = keyboard.GetCurrentState();

            prevMouseState = mouseState;
            mouseState = mouse.GetCurrentState();
        }
        internal static void FixedUpdate()
        {
            prevFixedKeyboardState = fixedKeyboardState;
            fixedKeyboardState = keyboard.GetCurrentState();

            prevFixedMouseState = fixedMouseState;
            fixedMouseState = mouse.GetCurrentState();
        }
        public static bool IsKeyDown(Key key)
        {
            if (Time.IsFixed)
                return fixedKeyboardState.IsPressed(key);
            return keyboardState.IsPressed(key);
        }
        public static bool IsKeyUp(Key key)
        {
            if (Time.IsFixed)
                return !fixedKeyboardState.IsPressed(key);
            return !keyboardState.IsPressed(key);
        }
        public static bool IsKeyPressed(Key key)
        {
            if (Time.IsFixed)
                return !prevFixedKeyboardState.IsPressed(key) && fixedKeyboardState.IsPressed(key);
            return !prevKeyboardState.IsPressed(key) && keyboardState.IsPressed(key);
        }
        public static bool IsKeyReleased(Key key)
        {
            if (Time.IsFixed)
                return prevFixedKeyboardState.IsPressed(key) && !fixedKeyboardState.IsPressed(key);
            return prevKeyboardState.IsPressed(key) && !keyboardState.IsPressed(key);
        }

        public static bool IsMouseButtonDown(int button)
        {
            if (Time.IsFixed)
                return fixedMouseState.Buttons[button];
            return mouseState.Buttons[button];
        }
        public static bool IsMouseButtonUp(int button)
        {
            if (Time.IsFixed)
                return !fixedMouseState.Buttons[button];
            return !mouseState.Buttons[button];
        }
        public static bool IsMouseButtonPressed(int button)
        {
            if (Time.IsFixed)
                return !prevFixedMouseState.Buttons[button] && prevMouseState.Buttons[button];
            return !prevMouseState.Buttons[button] && mouseState.Buttons[button];
        }
        public static bool IsMouseButtonReleased(int button)
        {
            if (Time.IsFixed)
                return prevFixedMouseState.Buttons[button] && !fixedMouseState.Buttons[button];
            return prevMouseState.Buttons[button] && !mouseState.Buttons[button];
        }
        public static Vector2 GetMouseDelta()
        {
            if (Time.IsFixed)
                return new Vector2(fixedMouseState.X, fixedMouseState.Y);
            return new Vector2(mouseState.X, mouseState.Y);
        }
    }
}