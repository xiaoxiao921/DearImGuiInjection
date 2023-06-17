using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DearImGuiInjection.Windows;
using ImGuiNET;

namespace DearImGuiInjection.Backends;

public static unsafe class ImGuiWin32Impl
{
    private delegate uint XInputGetCapabilitiesDelegate(uint a, uint b, IntPtr c);
    private delegate uint XInputGetStateDelegate(uint a, IntPtr b);

    private static IntPtr _windowHandle;
    private static IntPtr _mouseHandle;
    private static int _mouseTrackedArea;   // 0: not tracked, 1: client are, 2: non-client area
    private static int _mouseButtonsDown;
    private static long _time;
    private static long _ticksPerSecond;
    private static ImGuiMouseCursor _lastMouseCursor;

    private static bool _hasGamepad;
    private static bool _wantUpdateHasGamepad;
    private static IntPtr _xInputDLL;
    private static XInputGetCapabilitiesDelegate _xInputGetCapabilities;
    private static XInputGetStateDelegate _xInputGetState;

    private static bool ImGui_ImplWin32_InitEx(void* windowHandle, bool platform_has_own_dc)
    {
        var io = ImGui.GetIO();
        if (_windowHandle != IntPtr.Zero)
        {
            Log.Error("Already initialized a platform backend!");
            return false;
        }

        if (!Kernel32.QueryPerformanceFrequency(out var perf_frequency))
            return false;
        if (!Kernel32.QueryPerformanceCounter(out var perf_counter))
            return false;

        // Setup backend capabilities flags
        io.NativePtr->BackendPlatformName = (byte*)Marshal.StringToHGlobalAnsi("imgui_impl_win32");
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;         // We can honor GetMouseCursor() values (optional)
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;          // We can honor io.WantSetMousePos requests (optional, rarely used)

        _windowHandle = (IntPtr)windowHandle;
        _ticksPerSecond = perf_frequency;
        _time = perf_counter;
        _lastMouseCursor = ImGuiMouseCursor.COUNT;

        // Set platform dependent data in viewport
        ImGui.GetMainViewport().NativePtr->PlatformHandleRaw = windowHandle;
        //IM_UNUSED(platform_has_own_dc); // Used in 'docking' branch

        // Dynamically load XInput library
        _wantUpdateHasGamepad = true;
        var xinput_dll_names = new List<string>()
        {
            "xinput1_4.dll",   // Windows 8+
            "xinput1_3.dll",   // DirectX SDK
            "xinput9_1_0.dll", // Windows Vista, Windows 7
            "xinput1_2.dll",   // DirectX SDK
            "xinput1_1.dll"    // DirectX SDK
        };

        for (int n = 0; n < xinput_dll_names.Count; n++)
        {
            var dll = Kernel32.LoadLibrary(xinput_dll_names[n]);
            if (dll != IntPtr.Zero)
            {
                _xInputDLL = dll;
                _xInputGetCapabilities = Marshal.GetDelegateForFunctionPointer<XInputGetCapabilitiesDelegate>(Kernel32.GetProcAddress(dll, "XInputGetCapabilities"));
                _xInputGetState = Marshal.GetDelegateForFunctionPointer<XInputGetStateDelegate>(Kernel32.GetProcAddress(dll, "XInputGetState"));
                break;
            }
        }

        return true;
    }

    public static bool ImGui_ImplWin32_Init(void* hwnd)
    {
        return ImGui_ImplWin32_InitEx(hwnd, false);
    }

    public static bool ImGui_ImplWin32_InitForOpenGL(void* hwnd)
    {
        // OpenGL needs CS_OWNDC
        return ImGui_ImplWin32_InitEx(hwnd, true);
    }

    public static void Shutdown()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            Log.Error("No platform backend to shutdown, or already shutdown?");
            return;
        }

        var io = ImGui.GetIO();

        // Unload XInput library
        if (_xInputDLL != IntPtr.Zero)
            Kernel32.FreeLibrary(_xInputDLL);

        io.NativePtr->BackendPlatformName = null;
        io.NativePtr->BackendPlatformUserData = null;
        io.BackendFlags &= ~(ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.HasGamepad);
    }

    static bool ImGui_ImplWin32_UpdateMouseCursor()
    {
        var io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
            return false;

        ImGuiMouseCursor imgui_cursor = ImGui.GetMouseCursor();
        if (imgui_cursor == ImGuiMouseCursor.None || io.MouseDrawCursor)
        {
            // Hide OS mouse cursor if imgui is drawing it or if it wants no cursor
            User32.SetCursor(IntPtr.Zero);
        }
        else
        {
            const int
                IDC_ARROW = 32512,
                IDC_IBEAM = 32513,
                IDC_SIZENWSE = 32642,
                IDC_SIZENESW = 32643,
                IDC_SIZEWE = 32644,
                IDC_SIZENS = 32645,
                IDC_SIZEALL = 32646,
                IDC_NO = 32648,
                IDC_HAND = 32649;

            var win32_cursor = IDC_ARROW;
            switch (imgui_cursor)
            {
                case ImGuiMouseCursor.Arrow: win32_cursor = IDC_ARROW; break;
                case ImGuiMouseCursor.TextInput: win32_cursor = IDC_IBEAM; break;
                case ImGuiMouseCursor.ResizeAll: win32_cursor = IDC_SIZEALL; break;
                case ImGuiMouseCursor.ResizeEW: win32_cursor = IDC_SIZEWE; break;
                case ImGuiMouseCursor.ResizeNS: win32_cursor = IDC_SIZENS; break;
                case ImGuiMouseCursor.ResizeNESW: win32_cursor = IDC_SIZENESW; break;
                case ImGuiMouseCursor.ResizeNWSE: win32_cursor = IDC_SIZENWSE; break;
                case ImGuiMouseCursor.Hand: win32_cursor = IDC_HAND; break;
                case ImGuiMouseCursor.NotAllowed: win32_cursor = IDC_NO; break;
            }
            User32.SetCursor(User32.LoadCursor(IntPtr.Zero, win32_cursor));
        }
        return true;
    }

    static bool IsVkDown(User32.VirtualKey vk)
    {
        return (User32.GetKeyState(vk) & 0x8000) != 0;
    }

    static void ImGui_ImplWin32_AddKeyEvent(ImGuiKey key, bool down, User32.VirtualKey native_keycode, int native_scancode = -1)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(key, down);
        io.SetKeyEventNativeData(key, (int)native_keycode, native_scancode); // To support legacy indexing (<1.87 user code)
    }

    static void ImGui_ImplWin32_ProcessKeyEventsWorkarounds()
    {
        // Left & right Shift keys: when both are pressed together, Windows tend to not generate the WM_KEYUP event for the first released one.
        if (ImGui.IsKeyDown(ImGuiKey.LeftShift) && !IsVkDown(User32.VirtualKey.VK_LSHIFT))
            ImGui_ImplWin32_AddKeyEvent(ImGuiKey.LeftShift, false, User32.VirtualKey.VK_LSHIFT);
        if (ImGui.IsKeyDown(ImGuiKey.RightShift) && !IsVkDown(User32.VirtualKey.VK_RSHIFT))
            ImGui_ImplWin32_AddKeyEvent(ImGuiKey.RightShift, false, User32.VirtualKey.VK_RSHIFT);

        // Sometimes WM_KEYUP for Win key is not passed down to the app (e.g. for Win+V on some setups, according to GLFW).
        if (ImGui.IsKeyDown(ImGuiKey.LeftSuper) && !IsVkDown(User32.VirtualKey.VK_LWIN))
            ImGui_ImplWin32_AddKeyEvent(ImGuiKey.LeftSuper, false, User32.VirtualKey.VK_LWIN);
        if (ImGui.IsKeyDown(ImGuiKey.RightSuper) && !IsVkDown(User32.VirtualKey.VK_RWIN))
            ImGui_ImplWin32_AddKeyEvent(ImGuiKey.RightSuper, false, User32.VirtualKey.VK_RWIN);
    }

    public static void ImGui_ImplWin32_UpdateKeyModifiers()
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModCtrl, IsVkDown(User32.VirtualKey.VK_CONTROL));
        io.AddKeyEvent(ImGuiKey.ModShift, IsVkDown(User32.VirtualKey.VK_SHIFT));
        io.AddKeyEvent(ImGuiKey.ModAlt, IsVkDown(User32.VirtualKey.VK_MENU));
        io.AddKeyEvent(ImGuiKey.ModSuper, IsVkDown(User32.VirtualKey.VK_APPS));
    }

    public static void ImGui_ImplWin32_UpdateMouseData()
    {
        var io = ImGui.GetIO();

        IntPtr focused_window = User32.GetForegroundWindow();
        bool is_app_focused = focused_window == _windowHandle;
        if (is_app_focused)
        {
            // (Optional) Set OS mouse position from Dear ImGui if requested (rarely used, only when ImGuiConfigFlags_NavEnableSetMousePos is enabled by user)
            if (io.WantSetMousePos)
            {
                User32.POINT pos = new User32.POINT((int)io.MousePos.X, (int)io.MousePos.Y);
                if (User32.ClientToScreen(_windowHandle, ref pos))
                    User32.SetCursorPos(pos.X, pos.Y);
            }

            // (Optional) Fallback to provide mouse position when focused (WM_MOUSEMOVE already provides this when hovered or captured)
            // This also fills a short gap when clicking non-client area: WM_NCMOUSELEAVE -> modal OS move -> gap -> WM_NCMOUSEMOVE
            if (!io.WantSetMousePos && _mouseTrackedArea == 0)
            {
                User32.POINT pos;
                if (User32.GetCursorPos(out pos) && User32.ScreenToClient(_windowHandle, ref pos))
                    io.AddMousePosEvent(pos.X, pos.Y);
            }
        }
    }

    // Gamepad navigation mapping
    static void ImGui_ImplWin32_UpdateGamepads()
    {
        /*var io = ImGui.GetIO();
        ImGui_ImplWin32_Data* bd = ImGui_ImplWin32_GetBackendData();
        //if ((io.ConfigFlags & ImGuiConfigFlags_NavEnableGamepad) == 0) // FIXME: Technically feeding gamepad shouldn't depend on this now that they are regular inputs.
        //    return;

        // Calling XInputGetState() every frame on disconnected gamepads is unfortunately too slow.
        // Instead we refresh gamepad availability by calling XInputGetCapabilities() _only_ after receiving WM_DEVICECHANGE.
        if (bd->WantUpdateHasGamepad)
        {
            XINPUT_CAPABILITIES caps = { };
            bd->HasGamepad = bd->XInputGetCapabilities ? (bd->XInputGetCapabilities(0, XINPUT_FLAG_GAMEPAD, &caps) == ERROR_SUCCESS) : false;
            bd->WantUpdateHasGamepad = false;
        }

        io.BackendFlags &= ~ImGuiBackendFlags_HasGamepad;
        XINPUT_STATE xinput_state;
        XINPUT_GAMEPAD & gamepad = xinput_state.Gamepad;
        if (!bd->HasGamepad || bd->XInputGetState == nullptr || bd->XInputGetState(0, &xinput_state) != ERROR_SUCCESS)
            return;
        io.BackendFlags |= ImGuiBackendFlags_HasGamepad;

        MAP_BUTTON(ImGuiKey_GamepadStart, XINPUT_GAMEPAD_START);
        MAP_BUTTON(ImGuiKey_GamepadBack, XINPUT_GAMEPAD_BACK);
        MAP_BUTTON(ImGuiKey_GamepadFaceLeft, XINPUT_GAMEPAD_X);
        MAP_BUTTON(ImGuiKey_GamepadFaceRight, XINPUT_GAMEPAD_B);
        MAP_BUTTON(ImGuiKey_GamepadFaceUp, XINPUT_GAMEPAD_Y);
        MAP_BUTTON(ImGuiKey_GamepadFaceDown, XINPUT_GAMEPAD_A);
        MAP_BUTTON(ImGuiKey_GamepadDpadLeft, XINPUT_GAMEPAD_DPAD_LEFT);
        MAP_BUTTON(ImGuiKey_GamepadDpadRight, XINPUT_GAMEPAD_DPAD_RIGHT);
        MAP_BUTTON(ImGuiKey_GamepadDpadUp, XINPUT_GAMEPAD_DPAD_UP);
        MAP_BUTTON(ImGuiKey_GamepadDpadDown, XINPUT_GAMEPAD_DPAD_DOWN);
        MAP_BUTTON(ImGuiKey_GamepadL1, XINPUT_GAMEPAD_LEFT_SHOULDER);
        MAP_BUTTON(ImGuiKey_GamepadR1, XINPUT_GAMEPAD_RIGHT_SHOULDER);
        MAP_ANALOG(ImGuiKey_GamepadL2, gamepad.bLeftTrigger, XINPUT_GAMEPAD_TRIGGER_THRESHOLD, 255);
        MAP_ANALOG(ImGuiKey_GamepadR2, gamepad.bRightTrigger, XINPUT_GAMEPAD_TRIGGER_THRESHOLD, 255);
        MAP_BUTTON(ImGuiKey_GamepadL3, XINPUT_GAMEPAD_LEFT_THUMB);
        MAP_BUTTON(ImGuiKey_GamepadR3, XINPUT_GAMEPAD_RIGHT_THUMB);
        MAP_ANALOG(ImGuiKey_GamepadLStickLeft, gamepad.sThumbLX, -XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, -32768);
        MAP_ANALOG(ImGuiKey_GamepadLStickRight, gamepad.sThumbLX, +XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, +32767);
        MAP_ANALOG(ImGuiKey_GamepadLStickUp, gamepad.sThumbLY, +XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, +32767);
        MAP_ANALOG(ImGuiKey_GamepadLStickDown, gamepad.sThumbLY, -XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, -32768);
        MAP_ANALOG(ImGuiKey_GamepadRStickLeft, gamepad.sThumbRX, -XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, -32768);
        MAP_ANALOG(ImGuiKey_GamepadRStickRight, gamepad.sThumbRX, +XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, +32767);
        MAP_ANALOG(ImGuiKey_GamepadRStickUp, gamepad.sThumbRY, +XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, +32767);
        MAP_ANALOG(ImGuiKey_GamepadRStickDown, gamepad.sThumbRY, -XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE, -32768);*/
    }

    public static void NewFrame()
    {
        var io = ImGui.GetIO();

        // Setup display size (every frame to accommodate for window resizing)
        User32.GetClientRect(_windowHandle, out var rect);
        io.DisplaySize = new System.Numerics.Vector2(rect.Right - rect.Left, rect.Bottom - rect.Top);

        // Setup time step
        Kernel32.QueryPerformanceCounter(out var current_time);
        io.DeltaTime = (float)(current_time - _time) / _ticksPerSecond;
        _time = current_time;

        // Update OS mouse position
        ImGui_ImplWin32_UpdateMouseData();

        // Process workarounds for known Windows key handling issues
        ImGui_ImplWin32_ProcessKeyEventsWorkarounds();

        // Update OS mouse cursor with the cursor requested by imgui
        ImGuiMouseCursor mouse_cursor = io.MouseDrawCursor ? ImGuiMouseCursor.None : ImGui.GetMouseCursor();
        if (_lastMouseCursor != mouse_cursor)
        {
            _lastMouseCursor = mouse_cursor;
            ImGui_ImplWin32_UpdateMouseCursor();
        }

        // Update game controllers (if enabled and available)
        ImGui_ImplWin32_UpdateGamepads();
    }

    public const User32.VirtualKey IM_VK_KEYPAD_ENTER = (User32.VirtualKey)((int)User32.VirtualKey.VK_RETURN + 256);

    // Map VK_xxx to ImGuiKey_xxx.
    public static ImGuiKey ImGui_ImplWin32_VirtualKeyToImGuiKey(User32.VirtualKey wParam)
    {
        switch (wParam)
        {
            case User32.VirtualKey.VK_TAB: return ImGuiKey.Tab;
            case User32.VirtualKey.VK_LEFT: return ImGuiKey.LeftArrow;
            case User32.VirtualKey.VK_RIGHT: return ImGuiKey.RightArrow;
            case User32.VirtualKey.VK_UP: return ImGuiKey.UpArrow;
            case User32.VirtualKey.VK_DOWN: return ImGuiKey.DownArrow;
            case User32.VirtualKey.VK_PRIOR: return ImGuiKey.PageUp;
            case User32.VirtualKey.VK_NEXT: return ImGuiKey.PageDown;
            case User32.VirtualKey.VK_HOME: return ImGuiKey.Home;
            case User32.VirtualKey.VK_END: return ImGuiKey.End;
            case User32.VirtualKey.VK_INSERT: return ImGuiKey.Insert;
            case User32.VirtualKey.VK_DELETE: return ImGuiKey.Delete;
            case User32.VirtualKey.VK_BACK: return ImGuiKey.Backspace;
            case User32.VirtualKey.VK_SPACE: return ImGuiKey.Space;
            case User32.VirtualKey.VK_RETURN: return ImGuiKey.Enter;
            case User32.VirtualKey.VK_ESCAPE: return ImGuiKey.Escape;
            case User32.VirtualKey.VK_OEM_7: return ImGuiKey.Apostrophe;
            case User32.VirtualKey.VK_OEM_COMMA: return ImGuiKey.Comma;
            case User32.VirtualKey.VK_OEM_MINUS: return ImGuiKey.Minus;
            case User32.VirtualKey.VK_OEM_PERIOD: return ImGuiKey.Period;
            case User32.VirtualKey.VK_OEM_2: return ImGuiKey.Slash;
            case User32.VirtualKey.VK_OEM_1: return ImGuiKey.Semicolon;
            case User32.VirtualKey.VK_OEM_PLUS: return ImGuiKey.Equal;
            case User32.VirtualKey.VK_OEM_4: return ImGuiKey.LeftBracket;
            case User32.VirtualKey.VK_OEM_5: return ImGuiKey.Backslash;
            case User32.VirtualKey.VK_OEM_6: return ImGuiKey.RightBracket;
            case User32.VirtualKey.VK_OEM_3: return ImGuiKey.GraveAccent;
            case User32.VirtualKey.VK_CAPITAL: return ImGuiKey.CapsLock;
            case User32.VirtualKey.VK_SCROLL: return ImGuiKey.ScrollLock;
            case User32.VirtualKey.VK_NUMLOCK: return ImGuiKey.NumLock;
            case User32.VirtualKey.VK_SNAPSHOT: return ImGuiKey.PrintScreen;
            case User32.VirtualKey.VK_PAUSE: return ImGuiKey.Pause;
            case User32.VirtualKey.VK_NUMPAD0: return ImGuiKey.Keypad0;
            case User32.VirtualKey.VK_NUMPAD1: return ImGuiKey.Keypad1;
            case User32.VirtualKey.VK_NUMPAD2: return ImGuiKey.Keypad2;
            case User32.VirtualKey.VK_NUMPAD3: return ImGuiKey.Keypad3;
            case User32.VirtualKey.VK_NUMPAD4: return ImGuiKey.Keypad4;
            case User32.VirtualKey.VK_NUMPAD5: return ImGuiKey.Keypad5;
            case User32.VirtualKey.VK_NUMPAD6: return ImGuiKey.Keypad6;
            case User32.VirtualKey.VK_NUMPAD7: return ImGuiKey.Keypad7;
            case User32.VirtualKey.VK_NUMPAD8: return ImGuiKey.Keypad8;
            case User32.VirtualKey.VK_NUMPAD9: return ImGuiKey.Keypad9;
            case User32.VirtualKey.VK_DECIMAL: return ImGuiKey.KeypadDecimal;
            case User32.VirtualKey.VK_DIVIDE: return ImGuiKey.KeypadDivide;
            case User32.VirtualKey.VK_MULTIPLY: return ImGuiKey.KeypadMultiply;
            case User32.VirtualKey.VK_SUBTRACT: return ImGuiKey.KeypadSubtract;
            case User32.VirtualKey.VK_ADD: return ImGuiKey.KeypadAdd;
            case IM_VK_KEYPAD_ENTER: return ImGuiKey.KeypadEnter;
            case User32.VirtualKey.VK_LSHIFT: return ImGuiKey.LeftShift;
            case User32.VirtualKey.VK_LCONTROL: return ImGuiKey.LeftCtrl;
            case User32.VirtualKey.VK_LMENU: return ImGuiKey.LeftAlt;
            case User32.VirtualKey.VK_LWIN: return ImGuiKey.LeftSuper;
            case User32.VirtualKey.VK_RSHIFT: return ImGuiKey.RightShift;
            case User32.VirtualKey.VK_RCONTROL: return ImGuiKey.RightCtrl;
            case User32.VirtualKey.VK_RMENU: return ImGuiKey.RightAlt;
            case User32.VirtualKey.VK_RWIN: return ImGuiKey.RightSuper;
            case User32.VirtualKey.VK_APPS: return ImGuiKey.Menu;
            case (User32.VirtualKey)'0': return ImGuiKey._0;
            case (User32.VirtualKey)'1': return ImGuiKey._1;
            case (User32.VirtualKey)'2': return ImGuiKey._2;
            case (User32.VirtualKey)'3': return ImGuiKey._3;
            case (User32.VirtualKey)'4': return ImGuiKey._4;
            case (User32.VirtualKey)'5': return ImGuiKey._5;
            case (User32.VirtualKey)'6': return ImGuiKey._6;
            case (User32.VirtualKey)'7': return ImGuiKey._7;
            case (User32.VirtualKey)'8': return ImGuiKey._8;
            case (User32.VirtualKey)'9': return ImGuiKey._9;
            case (User32.VirtualKey)'A': return ImGuiKey.A;
            case (User32.VirtualKey)'B': return ImGuiKey.B;
            case (User32.VirtualKey)'C': return ImGuiKey.C;
            case (User32.VirtualKey)'D': return ImGuiKey.D;
            case (User32.VirtualKey)'E': return ImGuiKey.E;
            case (User32.VirtualKey)'F': return ImGuiKey.F;
            case (User32.VirtualKey)'G': return ImGuiKey.G;
            case (User32.VirtualKey)'H': return ImGuiKey.H;
            case (User32.VirtualKey)'I': return ImGuiKey.I;
            case (User32.VirtualKey)'J': return ImGuiKey.J;
            case (User32.VirtualKey)'K': return ImGuiKey.K;
            case (User32.VirtualKey)'L': return ImGuiKey.L;
            case (User32.VirtualKey)'M': return ImGuiKey.M;
            case (User32.VirtualKey)'N': return ImGuiKey.N;
            case (User32.VirtualKey)'O': return ImGuiKey.O;
            case (User32.VirtualKey)'P': return ImGuiKey.P;
            case (User32.VirtualKey)'Q': return ImGuiKey.Q;
            case (User32.VirtualKey)'R': return ImGuiKey.R;
            case (User32.VirtualKey)'S': return ImGuiKey.S;
            case (User32.VirtualKey)'T': return ImGuiKey.T;
            case (User32.VirtualKey)'U': return ImGuiKey.U;
            case (User32.VirtualKey)'V': return ImGuiKey.V;
            case (User32.VirtualKey)'W': return ImGuiKey.W;
            case (User32.VirtualKey)'X': return ImGuiKey.X;
            case (User32.VirtualKey)'Y': return ImGuiKey.Y;
            case (User32.VirtualKey)'Z': return ImGuiKey.Z;
            case User32.VirtualKey.VK_F1: return ImGuiKey.F1;
            case User32.VirtualKey.VK_F2: return ImGuiKey.F2;
            case User32.VirtualKey.VK_F3: return ImGuiKey.F3;
            case User32.VirtualKey.VK_F4: return ImGuiKey.F4;
            case User32.VirtualKey.VK_F5: return ImGuiKey.F5;
            case User32.VirtualKey.VK_F6: return ImGuiKey.F6;
            case User32.VirtualKey.VK_F7: return ImGuiKey.F7;
            case User32.VirtualKey.VK_F8: return ImGuiKey.F8;
            case User32.VirtualKey.VK_F9: return ImGuiKey.F9;
            case User32.VirtualKey.VK_F10: return ImGuiKey.F10;
            case User32.VirtualKey.VK_F11: return ImGuiKey.F11;
            case User32.VirtualKey.VK_F12: return ImGuiKey.F12;
            default: return ImGuiKey.None;
        }
    }

    // See https://learn.microsoft.com/en-us/windows/win32/tablet/system-events-and-mouse-messages
    // Prefer to call this at the top of the message handler to avoid the possibility of other Win32 calls interfering with this.
    static ImGuiMouseSource GetMouseSourceFromMessageExtraInfo()
    {
        var extra_info = (uint)User32.GetMessageExtraInfo();
        if ((extra_info & 0xFFFFFF80) == 0xFF515700)
            return ImGuiMouseSource.Pen;
        if ((extra_info & 0xFFFFFF80) == 0xFF515780)
            return ImGuiMouseSource.TouchScreen;
        return ImGuiMouseSource.Mouse;
    }

    public static int GET_X_LPARAM(IntPtr lp) => unchecked((short)(long)lp);
    public static int GET_Y_LPARAM(IntPtr lp) => unchecked((short)((long)lp >> 16));

    public static ushort HIWORD(IntPtr dwValue) => unchecked((ushort)((long)dwValue >> 16));
    public static ushort HIWORD(UIntPtr dwValue) => unchecked((ushort)((ulong)dwValue >> 16));

    public static ushort LOWORD(IntPtr dwValue) => unchecked((ushort)(long)dwValue);
    public static ushort LOWORD(UIntPtr dwValue) => unchecked((ushort)(ulong)dwValue);

    public static ushort GET_XBUTTON_WPARAM(UIntPtr val)
    {
        // #define GET_XBUTTON_WPARAM(wParam)  (HIWORD(wParam))
        return HIWORD(val);
    }
    const int XBUTTON1 = 1;
    const int WHEEL_DELTA = 120;
    public static ushort GET_XBUTTON_WPARAM(IntPtr val)
    {
        // #define GET_XBUTTON_WPARAM(wParam)  (HIWORD(wParam))
        return HIWORD(val);
    }

    internal static int GET_WHEEL_DELTA_WPARAM(IntPtr wParam)
    {
        return (short)HIWORD(wParam);
    }

    internal static int GET_WHEEL_DELTA_WPARAM(UIntPtr wParam)
    {
        return (short)HIWORD(wParam);
    }

    public static byte LOBYTE(ushort wValue) => (byte)(wValue & 0xff);

    public static IntPtr WndProcHandler(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
    {
        if (ImGui.GetCurrentContext() == IntPtr.Zero)
            return IntPtr.Zero;

        var io = ImGui.GetIO();

        switch (msg)
        {
            case WindowMessage.WM_MOUSEMOVE:
            case WindowMessage.WM_NCMOUSEMOVE:
                {
                    // We need to call TrackMouseEvent in order to receive WM_MOUSELEAVE events
                    ImGuiMouseSource mouse_source = GetMouseSourceFromMessageExtraInfo();
                    int area = msg == WindowMessage.WM_MOUSEMOVE ? 1 : 2;
                    _mouseHandle = hwnd;
                    if (_mouseTrackedArea != area)
                    {
                        User32.TRACKMOUSEEVENT tme_cancel = new(User32.TMEFlags.TME_CANCEL, hwnd, 0);
                        User32.TRACKMOUSEEVENT tme_track = new(area == 2 ? User32.TMEFlags.TME_LEAVE | User32.TMEFlags.TME_NONCLIENT : User32.TMEFlags.TME_LEAVE, hwnd, 0);
                        if (_mouseTrackedArea != 0)
                            User32.TrackMouseEvent(ref tme_cancel);
                        User32.TrackMouseEvent(ref tme_track);
                        _mouseTrackedArea = area;
                    }
                    User32.POINT mouse_pos = new(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
                    if (msg == WindowMessage.WM_NCMOUSEMOVE && User32.ScreenToClient(hwnd, ref mouse_pos) == false) // WM_NCMOUSEMOVE are provided in absolute coordinates.
                        break;
                    io.AddMouseSourceEvent(mouse_source);
                    io.AddMousePosEvent(mouse_pos.X, mouse_pos.Y);
                    break;
                }
            case WindowMessage.WM_MOUSELEAVE:
            case WindowMessage.WM_NCMOUSELEAVE:
                {
                    int area = msg == WindowMessage.WM_MOUSELEAVE ? 1 : 2;
                    if (_mouseTrackedArea == area)
                    {
                        if (_mouseHandle == hwnd)
                            _mouseHandle = IntPtr.Zero;
                        _mouseTrackedArea = 0;
                        io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
                    }
                    break;
                }
            case WindowMessage.WM_LBUTTONDOWN:
            case WindowMessage.WM_LBUTTONDBLCLK:
            case WindowMessage.WM_RBUTTONDOWN:
            case WindowMessage.WM_RBUTTONDBLCLK:
            case WindowMessage.WM_MBUTTONDOWN:
            case WindowMessage.WM_MBUTTONDBLCLK:
            case WindowMessage.WM_XBUTTONDOWN:
            case WindowMessage.WM_XBUTTONDBLCLK:
                {
                    ImGuiMouseSource mouse_source = GetMouseSourceFromMessageExtraInfo();
                    int button = 0;
                    if (msg == WindowMessage.WM_LBUTTONDOWN || msg == WindowMessage.WM_LBUTTONDBLCLK) { button = 0; }
                    if (msg == WindowMessage.WM_RBUTTONDOWN || msg == WindowMessage.WM_RBUTTONDBLCLK) { button = 1; }
                    if (msg == WindowMessage.WM_MBUTTONDOWN || msg == WindowMessage.WM_MBUTTONDBLCLK) { button = 2; }
                    if (msg == WindowMessage.WM_XBUTTONDOWN || msg == WindowMessage.WM_XBUTTONDBLCLK) { button = GET_XBUTTON_WPARAM(wParam) == XBUTTON1 ? 3 : 4; }
                    if (_mouseButtonsDown == 0 && User32.GetCapture() == IntPtr.Zero)
                        User32.SetCapture(hwnd);
                    _mouseButtonsDown |= 1 << button;
                    io.AddMouseSourceEvent(mouse_source);
                    io.AddMouseButtonEvent(button, true);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_LBUTTONUP:
            case WindowMessage.WM_RBUTTONUP:
            case WindowMessage.WM_MBUTTONUP:
            case WindowMessage.WM_XBUTTONUP:
                {
                    ImGuiMouseSource mouse_source = GetMouseSourceFromMessageExtraInfo();
                    int button = 0;
                    if (msg == WindowMessage.WM_LBUTTONUP) { button = 0; }
                    if (msg == WindowMessage.WM_RBUTTONUP) { button = 1; }
                    if (msg == WindowMessage.WM_MBUTTONUP) { button = 2; }
                    if (msg == WindowMessage.WM_XBUTTONUP) { button = GET_XBUTTON_WPARAM(wParam) == XBUTTON1 ? 3 : 4; }
                    _mouseButtonsDown &= ~(1 << button);
                    if (_mouseButtonsDown == 0 && User32.GetCapture() == hwnd)
                        User32.ReleaseCapture();
                    io.AddMouseSourceEvent(mouse_source);
                    io.AddMouseButtonEvent(button, false);
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_MOUSEWHEEL:
                io.AddMouseWheelEvent(0.0f, GET_WHEEL_DELTA_WPARAM(wParam) / (float)WHEEL_DELTA);
                return IntPtr.Zero;
            case WindowMessage.WM_MOUSEHWHEEL:
                io.AddMouseWheelEvent(-(float)GET_WHEEL_DELTA_WPARAM(wParam) / WHEEL_DELTA, 0.0f);
                return IntPtr.Zero;
            case WindowMessage.WM_KEYDOWN:
            case WindowMessage.WM_KEYUP:
            case WindowMessage.WM_SYSKEYDOWN:
            case WindowMessage.WM_SYSKEYUP:
                {
                    bool is_key_down = msg == WindowMessage.WM_KEYDOWN || msg == WindowMessage.WM_SYSKEYDOWN;
                    if ((int)wParam < 256)
                    {
                        // Submit modifiers
                        ImGui_ImplWin32_UpdateKeyModifiers();

                        // Obtain virtual key code
                        // (keypad enter doesn't have its own... VK_RETURN with KF_EXTENDED flag means keypad enter, see IM_VK_KEYPAD_ENTER definition for details, it is mapped to ImGuiKey_KeyPadEnter.)
                        User32.VirtualKey vk = (User32.VirtualKey)wParam;


                        bool isEnter = (User32.VirtualKey)wParam == User32.VirtualKey.VK_RETURN;
                        bool hasExtendedKeyFlag = (HIWORD(lParam) & (ushort)User32.KeyFlag.KF_EXTENDED) != 0;
                        if (isEnter && hasExtendedKeyFlag)
                            vk = IM_VK_KEYPAD_ENTER;

                        // Submit key event
                        ImGuiKey key = ImGui_ImplWin32_VirtualKeyToImGuiKey(vk);
                        int scancode = LOBYTE(HIWORD(lParam));
                        if (key != ImGuiKey.None)
                            ImGui_ImplWin32_AddKeyEvent(key, is_key_down, vk, scancode);

                        // Submit individual left/right modifier events
                        if (vk == User32.VirtualKey.VK_SHIFT)
                        {
                            // Important: Shift keys tend to get stuck when pressed together, missing key-up events are corrected in ImGui_ImplWin32_ProcessKeyEventsWorkarounds()
                            if (IsVkDown(User32.VirtualKey.VK_LSHIFT) == is_key_down) { ImGui_ImplWin32_AddKeyEvent(ImGuiKey.LeftShift, is_key_down, User32.VirtualKey.VK_LSHIFT, scancode); }
                            if (IsVkDown(User32.VirtualKey.VK_RSHIFT) == is_key_down) { ImGui_ImplWin32_AddKeyEvent(ImGuiKey.RightShift, is_key_down, User32.VirtualKey.VK_RSHIFT, scancode); }
                        }
                        else if (vk == User32.VirtualKey.VK_CONTROL)
                        {
                            if (IsVkDown(User32.VirtualKey.VK_LCONTROL) == is_key_down) { ImGui_ImplWin32_AddKeyEvent(ImGuiKey.LeftCtrl, is_key_down, User32.VirtualKey.VK_LCONTROL, scancode); }
                            if (IsVkDown(User32.VirtualKey.VK_RCONTROL) == is_key_down) { ImGui_ImplWin32_AddKeyEvent(ImGuiKey.RightCtrl, is_key_down, User32.VirtualKey.VK_RCONTROL, scancode); }
                        }
                        else if (vk == User32.VirtualKey.VK_MENU)
                        {
                            if (IsVkDown(User32.VirtualKey.VK_LMENU) == is_key_down) { ImGui_ImplWin32_AddKeyEvent(ImGuiKey.LeftAlt, is_key_down, User32.VirtualKey.VK_LMENU, scancode); }
                            if (IsVkDown(User32.VirtualKey.VK_RMENU) == is_key_down) { ImGui_ImplWin32_AddKeyEvent(ImGuiKey.RightAlt, is_key_down, User32.VirtualKey.VK_RMENU, scancode); }
                        }
                    }
                    return IntPtr.Zero;
                }
            case WindowMessage.WM_SETFOCUS:
            case WindowMessage.WM_KILLFOCUS:
                io.AddFocusEvent(msg == WindowMessage.WM_SETFOCUS);
                return IntPtr.Zero;
            case WindowMessage.WM_CHAR:
                if (User32.IsWindowUnicode(hwnd))
                {
                    // You can also use ToAscii()+GetKeyboardState() to retrieve characters.
                    if ((int)wParam > 0 && (int)wParam < 0x10000)
                        io.AddInputCharacterUTF16((ushort)wParam);
                }
                else
                {
                    byte[] lolxd = new byte[1] { 0 };
                    lolxd[0] = *(byte*)&wParam;

                    const int wideBufferSize = 1;
                    var wideBuffer = Marshal.AllocHGlobal(wideBufferSize);
                    Kernel32.MultiByteToWideChar(Kernel32.CP_ACP, Kernel32.MB_PRECOMPOSED, lolxd, 1, wideBuffer, wideBufferSize);
                    var wideBufferChar = *(char*)wideBuffer;
                    io.AddInputCharacter(wideBufferChar);
                    Marshal.FreeHGlobal(wideBuffer);
                }
                return IntPtr.Zero;
            case WindowMessage.WM_SETCURSOR:
                const int HTCLIENT = 1;
                // This is required to restore cursor when transitioning from e.g resize borders to client area.
                if (LOWORD(lParam) == HTCLIENT && ImGui_ImplWin32_UpdateMouseCursor())
                    return new(1);
                return IntPtr.Zero;
            case WindowMessage.WM_DEVICECHANGE:
                const int DBT_DEVNODES_CHANGED = 0x0007;
                if ((uint)wParam == DBT_DEVNODES_CHANGED)
                    _wantUpdateHasGamepad = true;
                return IntPtr.Zero;
        }
        return IntPtr.Zero;
    }

    static bool _IsWindowsVersionOrGreater(short major, short minor, short unused)
    {
        const uint VER_MAJORVERSION = 0x0000002;
        const uint VER_MINORVERSION = 0x0000001;
        const byte VER_GREATER_EQUAL = 3;

        var versionInfo = OSVERSIONINFOEX.Create();
        ulong conditionMask = 0;
        versionInfo.dwMajorVersion = major;
        versionInfo.dwMinorVersion = minor;
        Kernel32.VER_SET_CONDITION(ref conditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
        Kernel32.VER_SET_CONDITION(ref conditionMask, VER_MINORVERSION, VER_GREATER_EQUAL);
        return Ntdll.RtlVerifyVersionInfo(&versionInfo, VER_MASK.VER_MAJORVERSION | VER_MASK.VER_MINORVERSION, (long)conditionMask) == 0 ? true : false;
    }

    static bool _IsWindowsVistaOrGreater() => _IsWindowsVersionOrGreater((short)Kernel32.HiByte(0x0600), LOBYTE(0x0600), 0); // _WIN32_WINNT_VISTA
    static bool _IsWindows8OrGreater() => _IsWindowsVersionOrGreater((short)Kernel32.HiByte(0x0602), LOBYTE(0x0602), 0); // _WIN32_WINNT_WIN8
    static bool _IsWindows8Point1OrGreater() => _IsWindowsVersionOrGreater((short)Kernel32.HiByte(0x0603), LOBYTE(0x0603), 0); // _WIN32_WINNT_WINBLUE
    static bool _IsWindows10OrGreater() => _IsWindowsVersionOrGreater((short)Kernel32.HiByte(0x0A00), LOBYTE(0x0A00), 0); // _WIN32_WINNT_WINTHRESHOLD / _WIN32_WINNT_WIN10

    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    // Helper function to enable DPI awareness without setting up a manifest
    static void ImGui_ImplWin32_EnableDpiAwareness()
    {
        if (_IsWindows10OrGreater())
        {
            User32.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            return;
        }
        if (_IsWindows8Point1OrGreater())
        {
            Shellscalingapi.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
            return;
        }

        User32.SetProcessDPIAware();
    }

    public static float ImGui_ImplWin32_GetDpiScaleForMonitor(void* monitor)
    {
        uint xdpi;

        if (_IsWindows8Point1OrGreater())
        {
            Shellscalingapi.GetDpiForMonitor((IntPtr)monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out xdpi, out _);

            return xdpi / 96.0f;
        }

        const int LOGPIXELSX = 88;

        var dc = User32.GetDC(IntPtr.Zero);
        xdpi = (uint)Gdi32.GetDeviceCaps(dc, LOGPIXELSX);

        User32.ReleaseDC(IntPtr.Zero, dc);
        return xdpi / 96.0f;
    }

    public static unsafe float ImGui_ImplWin32_GetDpiScaleForHwnd(void* hwnd)
    {
        const int MONITOR_DEFAULTTONEAREST = 2;

        var monitor = User32.MonitorFromWindow((IntPtr)hwnd, MONITOR_DEFAULTTONEAREST);
        return ImGui_ImplWin32_GetDpiScaleForMonitor((void*)monitor);
    }

    public static unsafe void ImGui_ImplWin32_EnableAlphaCompositing(void* hwnd)
    {
        if (!_IsWindowsVistaOrGreater())
            return;

        var hres = Dwmapi.DwmIsCompositionEnabled(out bool composition);

        if (hres != 0 || !composition)
            return;

        hres = Dwmapi.DwmGetColorizationColor(out var color, out var opaque);

        if (_IsWindows8OrGreater() || hres == 0 && !opaque)
        {
            var region = Gdi32.CreateRectRgn(0, 0, -1, -1);
            Dwmapi.DWM_BLURBEHIND bb = new(true);
            bb.dwFlags |= Dwmapi.DWM_BB.BlurRegion;
            bb.hRgnBlur = region;
            Dwmapi.DwmEnableBlurBehindWindow((IntPtr)hwnd, ref bb);
            Gdi32.DeleteObject(region);
        }
        else
        {
            Dwmapi.DWM_BLURBEHIND bb = new(true);
            Dwmapi.DwmEnableBlurBehindWindow((IntPtr)hwnd, ref bb);
        }
    }

    internal static void Init(IntPtr windowHandle) => ImGui_ImplWin32_Init((void*)windowHandle);
}
