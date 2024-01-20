using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

using LinearAlgebra;

namespace Engine
{
    [Flags]
    public enum InputMode
    {
        GameOnly = 1,
        UIOnly = 1 << 1,
        GameAndUI = GameOnly | UIOnly
    }
    public static class Input
    {
        public static InputMode InputMode { get; set; } = InputMode.GameAndUI;

        //// Windows message listener
        private static SharpDX.DirectInput.DirectInput inputListener;
        //
        //// Devices
        //private static Keyboard keyboard;
        private static SharpDX.DirectInput.Mouse mouse;
        //
        //// States
        //private static KeyboardState prevKeyboardState;
        //private static KeyboardState keyboardState;
        //private static KeyboardState prevFixedKeyboardState;
        //private static KeyboardState fixedKeyboardState;
        private static SharpDX.DirectInput.MouseState mouseState;
        private static SharpDX.DirectInput.MouseState fixedMouseState;
        private static Vector2 mouseDelta;
        private static Vector2 nextMouseDelta;
        private static Vector2 fixedMouseDelta;
        private static Vector2 nextFixedMouseDelta;

        private static Dictionary<Key, bool> prevFixedKeysStates = new Dictionary<Key, bool>();
        private static Dictionary<Key, bool> fixedKeysStates = new Dictionary<Key, bool>();

        private static Dictionary<Key, bool> prevKeysStates = new Dictionary<Key, bool>();
        private static Dictionary<Key, bool> keysStates = new Dictionary<Key, bool>();
        private static Dictionary<Key, bool> nextKeysStates = new Dictionary<Key, bool>();

        private static Dictionary<MouseButton, bool> prevFixedMouseButtonStates = new Dictionary<MouseButton, bool>();
        private static Dictionary<MouseButton, bool> fixedMouseButtonStates = new Dictionary<MouseButton, bool>();

        private static Dictionary<MouseButton, bool> prevMouseButtonStates = new Dictionary<MouseButton, bool>();
        private static Dictionary<MouseButton, bool> mouseButtonStates = new Dictionary<MouseButton, bool>();
        private static Dictionary<MouseButton, bool> nextMouseButtonStates = new Dictionary<MouseButton, bool>();

        internal static bool isMouseDirectlyOverViewport { get; set; } = false;

        internal static void Init()
        {
            inputListener = new SharpDX.DirectInput.DirectInput();
            //
            //keyboard = new Keyboard(inputListener);
            //keyboard.Acquire();
            //keyboardState = keyboard.GetCurrentState();
            //prevKeyboardState = keyboardState;
            //fixedKeyboardState = keyboardState;
            //prevFixedKeyboardState = keyboardState;
            //
            mouse = new SharpDX.DirectInput.Mouse(inputListener);
            mouse.Acquire();
            mouse.GetCurrentState();
        }

        internal static void SetNextMouseButtonState(MouseButton button, bool state)
        {
            nextMouseButtonStates[button] = state;
        }

        internal static void SetNextKeyState(Key key, bool state)
        {
            nextKeysStates[key] = state;
        }

        internal static void Update()
        {
            prevMouseButtonStates = new Dictionary<MouseButton, bool>(mouseButtonStates);
            mouseButtonStates = new Dictionary<MouseButton, bool>(nextMouseButtonStates);

            prevKeysStates = new Dictionary<Key, bool>(keysStates);
            keysStates = new Dictionary<Key, bool>(nextKeysStates);
            //prevKeyboardState = keyboardState;
            //keyboardState = keyboard.GetCurrentState();
            //
            mouseState = mouse.GetCurrentState();
            
            Vector2 mouseDeltaFromLastState = new Vector2(mouseState.X, mouseState.Y);
            nextMouseDelta += mouseDeltaFromLastState;
            nextFixedMouseDelta += mouseDeltaFromLastState;
            
            mouseDelta = nextMouseDelta;
            nextMouseDelta = Vector2.Zero;
        }

        internal static void FixedUpdate()
        {
            prevFixedMouseButtonStates = new Dictionary<MouseButton, bool>(fixedMouseButtonStates);
            fixedMouseButtonStates = new Dictionary<MouseButton, bool>(nextMouseButtonStates);

            prevFixedKeysStates = new Dictionary<Key, bool>(fixedKeysStates);
            fixedKeysStates = new Dictionary<Key, bool>(nextKeysStates);
            //prevFixedKeyboardState = fixedKeyboardState;
            //fixedKeyboardState = keyboard.GetCurrentState();
            //
            fixedMouseState = mouse.GetCurrentState();
            
            Vector2 mouseDeltaFromLastState = new Vector2(fixedMouseState.X, fixedMouseState.Y);
            nextMouseDelta += mouseDeltaFromLastState;
            nextFixedMouseDelta += mouseDeltaFromLastState;
            
            fixedMouseDelta = nextFixedMouseDelta;
            nextFixedMouseDelta = Vector2.Zero;
        }

        /// <summary>
        /// Returns true if key is currently being pressed down
        /// </summary>
        public static bool IsKeyDown(Key key)
        {
            if (Time.IsFixed)
                return fixedKeysStates.GetValueOrDefault(key, false);
            return keysStates.GetValueOrDefault(key, false);
        }

        /// <summary>
        /// Returns true if key is currently not being pressed down
        /// </summary>
        public static bool IsKeyUp(Key key)
        {
            return !IsKeyDown(key);
        }

        /// <summary>
        /// Returns true if key just has been pressed in this frame
        /// </summary>
        public static bool IsKeyPressed(Key key)
        {
            if (Time.IsFixed)
                return !prevFixedKeysStates.GetValueOrDefault(key, false) && fixedKeysStates.GetValueOrDefault(key, false);
            return !prevKeysStates.GetValueOrDefault(key, false) && keysStates.GetValueOrDefault(key, false);
        }

        /// <summary>
        /// Returns true if key just has been released in this frame
        /// </summary>
        public static bool IsKeyReleased(Key key)
        {
            if (Time.IsFixed)
                return prevFixedKeysStates.GetValueOrDefault(key, false) && !fixedKeysStates.GetValueOrDefault(key, false);
            return prevKeysStates.GetValueOrDefault(key, false) && !keysStates.GetValueOrDefault(key, false);
        }

        /// <summary>
        /// Returns true if mouse button is currently being pressed down
        /// </summary>
        public static bool IsMouseButtonDown(MouseButton button)
        {
            if (Time.IsFixed)
                return fixedMouseButtonStates.GetValueOrDefault(button, false);
            return mouseButtonStates.GetValueOrDefault(button, false);
        }

        /// <summary>
        /// Returns true if mouse button is currently not being pressed down
        /// </summary>
        public static bool IsMouseButtonUp(MouseButton button)
        {
            return !IsMouseButtonDown(button);
        }

        /// <summary>
        /// Returns true if mouse button just has been pressed in this frame
        /// </summary>
        public static bool IsMouseButtonPressed(MouseButton button)
        {
            if (Time.IsFixed)
                return !prevFixedMouseButtonStates.GetValueOrDefault(button, false) && fixedMouseButtonStates.GetValueOrDefault(button, false);
            return !prevMouseButtonStates.GetValueOrDefault(button, false) && mouseButtonStates.GetValueOrDefault(button, false);
        }

        /// <summary>
        /// Returns true if mouse button just has been released in this frame
        /// </summary>
        public static bool IsMouseButtonReleased(MouseButton button)
        {
            if (Time.IsFixed)
                return prevFixedMouseButtonStates.GetValueOrDefault(button, false) && !fixedMouseButtonStates.GetValueOrDefault(button, false);
            return prevMouseButtonStates.GetValueOrDefault(button, false) && !mouseButtonStates.GetValueOrDefault(button, false);
        }

        public static Vector2 GetMouseDelta()
        {
            if (!isMouseDirectlyOverViewport || InputMode == InputMode.UIOnly)
                return Vector2.Zero;
            if (Time.IsFixed)
                return fixedMouseDelta;
            return mouseDelta;
        }

        public static Vector2 GetMousePos()
        {
            System.Windows.Point point = Mouse.GetPosition(GraphicsCore.ViewportPanel);
            return new Vector2(point.X, point.Y);
        }

        private static CursorState cursorState = CursorState.Default;
        public static CursorState CursorState
        {
            get => cursorState;
            set
            {
                if (value == cursorState)
                    return;

                cursorState = value;

                if (cursorState.HasFlag(CursorState.Hidden))
                    GraphicsCore.ViewportPanel.Dispatcher.Invoke(System.Windows.Forms.Cursor.Hide);
                else
                    GraphicsCore.ViewportPanel.Dispatcher.Invoke(System.Windows.Forms.Cursor.Show);

                if (cursorState.HasFlag(CursorState.Locked))
                    System.Windows.Forms.Cursor.Clip = new Rectangle(System.Windows.Forms.Cursor.Position, System.Drawing.Size.Empty);
                else
                    System.Windows.Forms.Cursor.Clip = Rectangle.Empty;
            }
        }
    }

    [Flags]
    public enum CursorState
    {
        Default = 0,
        Hidden = 1 << 0,
        Locked = 1 << 1,
        HiddenAndLocked = Hidden | Locked
    }
}