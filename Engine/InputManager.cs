using LinearAlgebra;

using SharpDX.DirectInput;

namespace Engine
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
        private static Vector2 mouseDelta;
        private static Vector2 nextMouseDelta;
        private static Vector2 fixedMouseDelta;
        private static Vector2 nextFixedMouseDelta;

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
            mouse.GetCurrentState();
        }

        internal static void Update()
        {
            prevKeyboardState = keyboardState;
            keyboardState = keyboard.GetCurrentState();

            prevMouseState = mouseState;
            mouseState = mouse.GetCurrentState();

            Vector2 mouseDeltaFromLastState = new Vector2(mouseState.X, mouseState.Y);
            nextMouseDelta += mouseDeltaFromLastState;
            nextFixedMouseDelta += mouseDeltaFromLastState;

            mouseDelta = nextMouseDelta;
            nextMouseDelta = Vector2.Zero;
        }

        internal static void FixedUpdate()
        {
            prevFixedKeyboardState = fixedKeyboardState;
            fixedKeyboardState = keyboard.GetCurrentState();

            prevFixedMouseState = fixedMouseState;
            fixedMouseState = mouse.GetCurrentState();

            Vector2 mouseDeltaFromLastState = new Vector2(fixedMouseState.X, fixedMouseState.Y);
            nextMouseDelta += mouseDeltaFromLastState;
            nextFixedMouseDelta += mouseDeltaFromLastState;

            fixedMouseDelta = nextFixedMouseDelta;
            nextFixedMouseDelta = Vector2.Zero;
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
                return fixedMouseDelta;
            return mouseDelta;
        }
    }
}