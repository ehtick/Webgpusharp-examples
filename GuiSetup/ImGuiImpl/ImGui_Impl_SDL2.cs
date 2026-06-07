
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using SDL2;
using static SDL2.SDL;

internal unsafe static class ImGui_Impl_SDL2
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SDL_free(IntPtr memblock);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_GetClipboardText")]
    private static extern IntPtr INTERNAL_SDL_GetClipboardText();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_SetClipboardText")]
    private unsafe static extern int INTERNAL_SDL_SetClipboardText(byte* text);

    private static readonly GCHandle s_BackendPlatformNameHandle =
        GCHandle.Alloc("imgui_impl_sdl2\0"u8.ToArray(), GCHandleType.Pinned);
    private static ulong? s_PerformanceFrequency;

    private static bool SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE => !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() && !OperatingSystem.IsBrowser();

    struct ImGui_ImplSDL2_Data
    {
        public IntPtr Window;
        public IntPtr Renderer;
        public ulong Time;
        public uint MouseWindowID;
        public int MouseButtonsDown;
        public InlineArray9<IntPtr> MouseCursors;
        public IntPtr LastMouseCursor;
        public int PendingMouseLeaveFrame;
        public byte* ClipboardTextData;
        public bool MouseCanUseGlobalState;
    }

    // Backend data stored in io.BackendPlatformUserData to allow support for multiple Dear ImGui contexts
    // It is STRONGLY preferred that you use docking branch with multi-viewports (== single Dear ImGui context + multiple windows) instead of multiple Dear ImGui contexts.
    // FIXME: multi-context support is not well tested and probably dysfunctional in this backend.
    // FIXME: some shared resources (mouse cursor shape, gamepad) are mishandled when using multi-context.
    private static ImGui_ImplSDL2_Data* ImGui_ImplSDL2_GetBackendData()
    {
        return ImGui.GetCurrentContext() != 0 ? (ImGui_ImplSDL2_Data*)ImGui.GetIO().BackendPlatformUserData : null;
    }

    // Functions
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static byte* ImGui_ImplSDL2_GetClipboardText(void* _0)
    {
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();
        if (bd->ClipboardTextData != null)
        {
            SDL_free((nint)bd->ClipboardTextData);
        }
        bd->ClipboardTextData = (byte*)INTERNAL_SDL_GetClipboardText();
        return bd->ClipboardTextData;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void ImGui_ImplSDL2_SetClipboardText(void* _0, byte* text)
    {
        _ = INTERNAL_SDL_SetClipboardText(text);
    }

    // Note: native IME will only display if user calls SDL_SetHint(SDL_HINT_IME_SHOW_UI, "1") _before_ SDL_CreateWindow().
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void ImGui_ImplSDL2_SetPlatformImeData(ImGuiViewport* _0, ImGuiPlatformImeData* data)
    {
        if (data->WantVisible != 0)
        {
            SDL_Rect r;
            r.x = (int)data->InputPos.X;
            r.y = (int)data->InputPos.Y;
            r.w = 1;
            r.h = (int)data->InputLineHeight;
            SDL_SetTextInputRect(ref r);
        }
    }

    static ImGuiKey ImGui_ImplSDL2_KeycodeToImGuiKey(int keycode) => (SDL_Keycode)keycode switch
    {
        SDL_Keycode.SDLK_TAB => ImGuiKey.Tab,
        SDL_Keycode.SDLK_LEFT => ImGuiKey.LeftArrow,
        SDL_Keycode.SDLK_RIGHT => ImGuiKey.RightArrow,
        SDL_Keycode.SDLK_UP => ImGuiKey.UpArrow,
        SDL_Keycode.SDLK_DOWN => ImGuiKey.DownArrow,
        SDL_Keycode.SDLK_PAGEUP => ImGuiKey.PageUp,
        SDL_Keycode.SDLK_PAGEDOWN => ImGuiKey.PageDown,
        SDL_Keycode.SDLK_HOME => ImGuiKey.Home,
        SDL_Keycode.SDLK_END => ImGuiKey.End,
        SDL_Keycode.SDLK_INSERT => ImGuiKey.Insert,
        SDL_Keycode.SDLK_DELETE => ImGuiKey.Delete,
        SDL_Keycode.SDLK_BACKSPACE => ImGuiKey.Backspace,
        SDL_Keycode.SDLK_SPACE => ImGuiKey.Space,
        SDL_Keycode.SDLK_RETURN => ImGuiKey.Enter,
        SDL_Keycode.SDLK_ESCAPE => ImGuiKey.Escape,
        SDL_Keycode.SDLK_QUOTE => ImGuiKey.Apostrophe,
        SDL_Keycode.SDLK_COMMA => ImGuiKey.Comma,
        SDL_Keycode.SDLK_MINUS => ImGuiKey.Minus,
        SDL_Keycode.SDLK_PERIOD => ImGuiKey.Period,
        SDL_Keycode.SDLK_SLASH => ImGuiKey.Slash,
        SDL_Keycode.SDLK_SEMICOLON => ImGuiKey.Semicolon,
        SDL_Keycode.SDLK_EQUALS => ImGuiKey.Equal,
        SDL_Keycode.SDLK_LEFTBRACKET => ImGuiKey.LeftBracket,
        SDL_Keycode.SDLK_BACKSLASH => ImGuiKey.Backslash,
        SDL_Keycode.SDLK_RIGHTBRACKET => ImGuiKey.RightBracket,
        SDL_Keycode.SDLK_BACKQUOTE => ImGuiKey.GraveAccent,
        SDL_Keycode.SDLK_CAPSLOCK => ImGuiKey.CapsLock,
        SDL_Keycode.SDLK_SCROLLLOCK => ImGuiKey.ScrollLock,
        SDL_Keycode.SDLK_NUMLOCKCLEAR => ImGuiKey.NumLock,
        SDL_Keycode.SDLK_PRINTSCREEN => ImGuiKey.PrintScreen,
        SDL_Keycode.SDLK_PAUSE => ImGuiKey.Pause,
        SDL_Keycode.SDLK_KP_0 => ImGuiKey.Keypad0,
        SDL_Keycode.SDLK_KP_1 => ImGuiKey.Keypad1,
        SDL_Keycode.SDLK_KP_2 => ImGuiKey.Keypad2,
        SDL_Keycode.SDLK_KP_3 => ImGuiKey.Keypad3,
        SDL_Keycode.SDLK_KP_4 => ImGuiKey.Keypad4,
        SDL_Keycode.SDLK_KP_5 => ImGuiKey.Keypad5,
        SDL_Keycode.SDLK_KP_6 => ImGuiKey.Keypad6,
        SDL_Keycode.SDLK_KP_7 => ImGuiKey.Keypad7,
        SDL_Keycode.SDLK_KP_8 => ImGuiKey.Keypad8,
        SDL_Keycode.SDLK_KP_9 => ImGuiKey.Keypad9,
        SDL_Keycode.SDLK_KP_PERIOD => ImGuiKey.KeypadDecimal,
        SDL_Keycode.SDLK_KP_DIVIDE => ImGuiKey.KeypadDivide,
        SDL_Keycode.SDLK_KP_MULTIPLY => ImGuiKey.KeypadMultiply,
        SDL_Keycode.SDLK_KP_MINUS => ImGuiKey.KeypadSubtract,
        SDL_Keycode.SDLK_KP_PLUS => ImGuiKey.KeypadAdd,
        SDL_Keycode.SDLK_KP_ENTER => ImGuiKey.KeypadEnter,
        SDL_Keycode.SDLK_KP_EQUALS => ImGuiKey.KeypadEqual,
        SDL_Keycode.SDLK_LCTRL => ImGuiKey.LeftCtrl,
        SDL_Keycode.SDLK_LSHIFT => ImGuiKey.LeftShift,
        SDL_Keycode.SDLK_LALT => ImGuiKey.LeftAlt,
        SDL_Keycode.SDLK_LGUI => ImGuiKey.LeftSuper,
        SDL_Keycode.SDLK_RCTRL => ImGuiKey.RightCtrl,
        SDL_Keycode.SDLK_RSHIFT => ImGuiKey.RightShift,
        SDL_Keycode.SDLK_RALT => ImGuiKey.RightAlt,
        SDL_Keycode.SDLK_RGUI => ImGuiKey.RightSuper,
        SDL_Keycode.SDLK_APPLICATION => ImGuiKey.Menu,
        SDL_Keycode.SDLK_0 => ImGuiKey._0,
        SDL_Keycode.SDLK_1 => ImGuiKey._1,
        SDL_Keycode.SDLK_2 => ImGuiKey._2,
        SDL_Keycode.SDLK_3 => ImGuiKey._3,
        SDL_Keycode.SDLK_4 => ImGuiKey._4,
        SDL_Keycode.SDLK_5 => ImGuiKey._5,
        SDL_Keycode.SDLK_6 => ImGuiKey._6,
        SDL_Keycode.SDLK_7 => ImGuiKey._7,
        SDL_Keycode.SDLK_8 => ImGuiKey._8,
        SDL_Keycode.SDLK_9 => ImGuiKey._9,
        SDL_Keycode.SDLK_a => ImGuiKey.A,
        SDL_Keycode.SDLK_b => ImGuiKey.B,
        SDL_Keycode.SDLK_c => ImGuiKey.C,
        SDL_Keycode.SDLK_d => ImGuiKey.D,
        SDL_Keycode.SDLK_e => ImGuiKey.E,
        SDL_Keycode.SDLK_f => ImGuiKey.F,
        SDL_Keycode.SDLK_g => ImGuiKey.G,
        SDL_Keycode.SDLK_h => ImGuiKey.H,
        SDL_Keycode.SDLK_i => ImGuiKey.I,
        SDL_Keycode.SDLK_j => ImGuiKey.J,
        SDL_Keycode.SDLK_k => ImGuiKey.K,
        SDL_Keycode.SDLK_l => ImGuiKey.L,
        SDL_Keycode.SDLK_m => ImGuiKey.M,
        SDL_Keycode.SDLK_n => ImGuiKey.N,
        SDL_Keycode.SDLK_o => ImGuiKey.O,
        SDL_Keycode.SDLK_p => ImGuiKey.P,
        SDL_Keycode.SDLK_q => ImGuiKey.Q,
        SDL_Keycode.SDLK_r => ImGuiKey.R,
        SDL_Keycode.SDLK_s => ImGuiKey.S,
        SDL_Keycode.SDLK_t => ImGuiKey.T,
        SDL_Keycode.SDLK_u => ImGuiKey.U,
        SDL_Keycode.SDLK_v => ImGuiKey.V,
        SDL_Keycode.SDLK_w => ImGuiKey.W,
        SDL_Keycode.SDLK_x => ImGuiKey.X,
        SDL_Keycode.SDLK_y => ImGuiKey.Y,
        SDL_Keycode.SDLK_z => ImGuiKey.Z,
        SDL_Keycode.SDLK_F1 => ImGuiKey.F1,
        SDL_Keycode.SDLK_F2 => ImGuiKey.F2,
        SDL_Keycode.SDLK_F3 => ImGuiKey.F3,
        SDL_Keycode.SDLK_F4 => ImGuiKey.F4,
        SDL_Keycode.SDLK_F5 => ImGuiKey.F5,
        SDL_Keycode.SDLK_F6 => ImGuiKey.F6,
        SDL_Keycode.SDLK_F7 => ImGuiKey.F7,
        SDL_Keycode.SDLK_F8 => ImGuiKey.F8,
        SDL_Keycode.SDLK_F9 => ImGuiKey.F9,
        SDL_Keycode.SDLK_F10 => ImGuiKey.F10,
        SDL_Keycode.SDLK_F11 => ImGuiKey.F11,
        SDL_Keycode.SDLK_F12 => ImGuiKey.F12,
        _ => (ImGuiKey)ImGuiKey.None,
    };

    static void ImGui_ImplSDL2_UpdateKeyModifiers(SDL_Keymod sdl_key_mods)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModCtrl, (sdl_key_mods & SDL_Keymod.KMOD_CTRL) != 0);
        io.AddKeyEvent(ImGuiKey.ModShift, (sdl_key_mods & SDL_Keymod.KMOD_SHIFT) != 0);
        io.AddKeyEvent(ImGuiKey.ModAlt, (sdl_key_mods & SDL_Keymod.KMOD_ALT) != 0);
        io.AddKeyEvent(ImGuiKey.ModSuper, (sdl_key_mods & SDL_Keymod.KMOD_GUI) != 0);
    }


    // You can read the io.WantCaptureMouse, io.WantCaptureKeyboard flags to tell if dear imgui wants to use your inputs.
    // - When io.WantCaptureMouse is true, do not dispatch mouse input data to your main application, or clear/overwrite your copy of the mouse data.
    // - When io.WantCaptureKeyboard is true, do not dispatch keyboard input data to your main application, or clear/overwrite your copy of the keyboard data.
    // Generally you may always pass all inputs to dear imgui, and hide them from your application based on those two flags.
    // If you have multiple SDL events and some of them are not meant to be used by dear imgui, you may need to filter events based on their windowID field.
    public static bool ImGui_ImplSDL2_ProcessEvent(in SDL_Event @event)
    {
        var io = ImGui.GetIO();
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();

        switch (@event.type)
        {
            case SDL_EventType.SDL_MOUSEMOTION:
                {
                    Vector2 mouse_pos = new((float)@event.motion.x, (float)@event.motion.y);
                    io.AddMouseSourceEvent(@event.motion.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
                    io.AddMousePosEvent(mouse_pos.X, mouse_pos.Y);
                    return true;
                }
            case SDL_EventType.SDL_MOUSEWHEEL:
                {
                    float wheel_x = -@event.wheel.x;
                    float wheel_y = @event.wheel.y;
                    if (OperatingSystem.IsBrowser())
                    {
                        wheel_x /= 100.0f;
                    }
                    io.AddMouseSourceEvent(@event.wheel.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
                    io.AddMouseWheelEvent(wheel_x, wheel_y);
                    return true;
                }
            case SDL_EventType.SDL_MOUSEBUTTONDOWN:
            case SDL_EventType.SDL_MOUSEBUTTONUP:
                {
                    int mouse_button = -1;
                    if (@event.button.button == SDL_BUTTON_LEFT) { mouse_button = 0; }
                    if (@event.button.button == SDL_BUTTON_RIGHT) { mouse_button = 1; }
                    if (@event.button.button == SDL_BUTTON_MIDDLE) { mouse_button = 2; }
                    if (@event.button.button == SDL_BUTTON_X1) { mouse_button = 3; }
                    if (@event.button.button == SDL_BUTTON_X2) { mouse_button = 4; }
                    if (mouse_button == -1)
                        break;

                    io.AddMouseSourceEvent(@event.button.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
                    io.AddMouseButtonEvent(mouse_button, @event.type == SDL_EventType.SDL_MOUSEBUTTONDOWN);
                    bd->MouseButtonsDown = @event.type == SDL_EventType.SDL_MOUSEBUTTONDOWN
                        ? (bd->MouseButtonsDown | (1 << mouse_button))
                        : (bd->MouseButtonsDown & ~(1 << mouse_button));

                    return true;
                }
            case SDL_EventType.SDL_TEXTINPUT:
                {
                    fixed (byte* text = @event.text.text)
                    {
                        ImGuiNative.ImGuiIO_AddInputCharactersUTF8(io.NativePtr, text);
                    }
                    return true;
                }
            case SDL_EventType.SDL_KEYDOWN:
            case SDL_EventType.SDL_KEYUP:
                {
                    ImGui_ImplSDL2_UpdateKeyModifiers(@event.key.keysym.mod);
                    ImGuiKey key = ImGui_ImplSDL2_KeycodeToImGuiKey((int)@event.key.keysym.sym);
                    io.AddKeyEvent(key, @event.type == SDL_EventType.SDL_KEYDOWN);
                    io.SetKeyEventNativeData(key, (int)@event.key.keysym.sym, (int)@event.key.keysym.scancode, (int)@event.key.keysym.scancode); // To support legacy indexing (<1.87 user code). Legacy backend uses SDLK_*** as indices to IsKeyXXX() functions.
                    return true;
                }
            case SDL_EventType.SDL_WINDOWEVENT:
                {
                    // - When capturing mouse, SDL will send a bunch of conflicting LEAVE/ENTER event on every mouse move, but the final ENTER tends to be right.
                    // - However we won't get a correct LEAVE event for a captured window.
                    // - In some cases, when detaching a window from main viewport SDL may send SDL_WINDOWEVENT_ENTER one frame too late,
                    //   causing SDL_WINDOWEVENT_LEAVE on previous frame to interrupt drag operation by clear mouse position. This is why
                    //   we delay process the SDL_WINDOWEVENT_LEAVE events by one frame. See issue #5012 for details.
                    var window_event = @event.window.windowEvent;
                    if (window_event == SDL_WindowEventID.SDL_WINDOWEVENT_ENTER)
                    {
                        bd->MouseWindowID = @event.window.windowID;
                        bd->PendingMouseLeaveFrame = 0;
                    }
                    if (window_event == SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE)
                        bd->PendingMouseLeaveFrame = ImGui.GetFrameCount() + 1;
                    if (window_event == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
                        io.AddFocusEvent(true);
                    else if (@event.window.windowEvent == SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
                        io.AddFocusEvent(false);

                    return true;
                }
        }

        return false;
    }

    public static bool ImGui_ImplSDL2_Init(IntPtr window, IntPtr renderer)
    {
        var io = ImGui.GetIO();
        Debug.Assert(io.BackendPlatformUserData == 0, "Already initialized a platform backend!");

        // Check and store if we are on a SDL backend that supports global mouse position
        // ("wayland" and "rpi" don't support it, but we chose to use a white-list instead of a black-list)
        bool mouse_can_use_global_state = false;
        if (SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE)
        {
            string sdl_backend = SDL_GetCurrentVideoDriver();
            string[] global_mouse_whitelist = ["windows", "cocoa", "x11", "DIVE", "VMAN"];
            for (int n = 0; n < global_mouse_whitelist.Length; n++)
                if (sdl_backend.StartsWith(global_mouse_whitelist[n], StringComparison.Ordinal))
                    mouse_can_use_global_state = true;
        }

        // Setup backend capabilities flags
        ImGui_ImplSDL2_Data* bd = (ImGui_ImplSDL2_Data*)NativeMemory.AllocZeroed((nuint)sizeof(ImGui_ImplSDL2_Data));
        *bd = new ImGui_ImplSDL2_Data();
        io.NativePtr->BackendPlatformUserData = bd;
        io.NativePtr->BackendPlatformName = (byte*)s_BackendPlatformNameHandle.AddrOfPinnedObject();
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;       // We can honor GetMouseCursor() values (optional)
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;        // We can honor io.WantSetMousePos requests (optional, rarely used)

        bd->Window = window;
        bd->Renderer = renderer;
        bd->MouseCanUseGlobalState = mouse_can_use_global_state;

        var platform_io = ImGui.GetPlatformIO();
        platform_io.NativePtr->Platform_SetClipboardTextFn = (IntPtr)(delegate* unmanaged[Cdecl]<void*, byte*, void>)&ImGui_ImplSDL2_SetClipboardText;
        platform_io.NativePtr->Platform_GetClipboardTextFn = (IntPtr)(delegate* unmanaged[Cdecl]<void*, byte*>)&ImGui_ImplSDL2_GetClipboardText;
        platform_io.NativePtr->Platform_ClipboardUserData = null;
        platform_io.NativePtr->Platform_SetImeDataFn = (IntPtr)(delegate* unmanaged[Cdecl]<ImGuiViewport*, ImGuiPlatformImeData*, void>)&ImGui_ImplSDL2_SetPlatformImeData;

        // Load mouse cursors
        bd->MouseCursors[(int)ImGuiMouseCursor.Arrow] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
        bd->MouseCursors[(int)ImGuiMouseCursor.TextInput] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
        bd->MouseCursors[(int)ImGuiMouseCursor.ResizeAll] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL);
        bd->MouseCursors[(int)ImGuiMouseCursor.ResizeNS] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS);
        bd->MouseCursors[(int)ImGuiMouseCursor.ResizeEW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE);
        bd->MouseCursors[(int)ImGuiMouseCursor.ResizeNESW] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW);
        bd->MouseCursors[(int)ImGuiMouseCursor.ResizeNWSE] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE);
        bd->MouseCursors[(int)ImGuiMouseCursor.Hand] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND);
        bd->MouseCursors[(int)ImGuiMouseCursor.NotAllowed] = SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO);

        // Set platform dependent data in viewport
        // Our mouse update function expect PlatformHandle to be filled for the main viewport
        ImGuiViewportPtr main_viewport = ImGui.GetMainViewport();
        main_viewport.NativePtr->PlatformHandleRaw = null;
        SDL_SysWMinfo info = new();
        SDL_VERSION(out info.version);
        if (SDL_GetWindowWMInfo((nint)window, ref info) == SDL_bool.SDL_TRUE)
        {
            if (OperatingSystem.IsWindows())
                main_viewport.NativePtr->PlatformHandleRaw = (void*)info.info.win.window;
            else if (OperatingSystem.IsMacOS())
                main_viewport.NativePtr->PlatformHandleRaw = (void*)info.info.cocoa.window;
        }

        // From 2.0.5: Set SDL hint to receive mouse click events on window focus, otherwise SDL doesn't emit the event.
        // Without this, when clicking to gain focus, our widgets wouldn't activate even though they showed as hovered.
        // (This is unfortunately a global SDL setting, so enabling it might have a side-effect on your application.
        // It is unlikely to make a difference, but if your app absolutely needs to ignore the initial on-focus click:
        // you can ignore SDL_MOUSEBUTTONDOWN events coming right after a SDL_WINDOWEVENT_FOCUS_GAINED)
        SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

        // From 2.0.18: Enable native IME.
        // IMPORTANT: This is used at the time of SDL_CreateWindow() so this will only affects secondary windows, if any.
        // For the main window to be affected, your application needs to call this manually before calling SDL_CreateWindow().
        SDL_SetHint("SDL_IME_SHOW_UI", "1");

        // From 2.0.22: Disable auto-capture, this is preventing drag and drop across multiple windows (see #5710)
        SDL_SetHint("SDL_MOUSE_AUTO_CAPTURE", "0");

        return true;
    }

    public static void ImGui_ImplSDL2_Shutdown()
    {
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();
        Debug.Assert(bd != null, "No platform backend to shutdown, or already shutdown?");
        var io = ImGui.GetIO();

        if (bd->ClipboardTextData != null)
            SDL_free((nint)bd->ClipboardTextData);
        for (ImGuiMouseCursor cursor_n = 0; cursor_n < ImGuiMouseCursor.COUNT; cursor_n++)
            SDL_FreeCursor(bd->MouseCursors[(int)cursor_n]);
        bd->LastMouseCursor = IntPtr.Zero;

        io.NativePtr->BackendPlatformName = null;
        io.NativePtr->BackendPlatformUserData = null;
        io.BackendFlags &= ~(ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.HasGamepad);
        NativeMemory.Free(bd);
    }

    static void ImGui_ImplSDL2_UpdateMouseData()
    {
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();
        var io = ImGui.GetIO();

        bool is_app_focused;
        // We forward mouse input when hovered or captured (via SDL_MOUSEMOTION) or when focused (below)
        if (SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE)
        {
            // SDL_CaptureMouse() let the OS know e.g. that our imgui drag outside the SDL window boundaries shouldn't e.g. trigger other operations outside
            _ = SDL_CaptureMouse((bd->MouseButtonsDown != 0) ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE);
            nint focused_window = SDL_GetKeyboardFocus();
            is_app_focused = bd->Window == focused_window;
        }
        else
        {
            is_app_focused = (SDL_GetWindowFlags(bd->Window) & (uint)SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) != 0; // SDL 2.0.3 and non-windowed systems: single-viewport only
        }

        if (is_app_focused)
        {
            // (Optional) Set OS mouse position from Dear ImGui if requested (rarely used, only when ImGuiConfigFlags_NavEnableSetMousePos is enabled by user)
            if (io.WantSetMousePos)
                SDL_WarpMouseInWindow(bd->Window, (int)io.MousePos.X, (int)io.MousePos.Y);

            // (Optional) Fallback to provide mouse position when focused (SDL_MOUSEMOTION already provides this when hovered or captured)
            if (bd->MouseCanUseGlobalState && bd->MouseButtonsDown == 0)
            {
                int window_x, window_y, mouse_x_global, mouse_y_global;
                _ = SDL_GetGlobalMouseState(out mouse_x_global, out mouse_y_global);
                SDL_GetWindowPosition(bd->Window, out window_x, out window_y);
                io.AddMousePosEvent(mouse_x_global - window_x, mouse_y_global - window_y);
            }
        }
    }

    static void ImGui_ImplSDL2_UpdateMouseCursor()
    {
        var io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
            return;
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();

        ImGuiMouseCursor imgui_cursor = ImGui.GetMouseCursor();
        if (io.MouseDrawCursor || imgui_cursor == ImGuiMouseCursor.None)
        {
            // Hide OS mouse cursor if imgui is drawing it or if it wants no cursor
            SDL_ShowCursor((int)SDL_bool.SDL_FALSE);
        }
        else
        {
            // Show OS mouse cursor
            IntPtr expected_cursor = bd->MouseCursors[(int)imgui_cursor] != IntPtr.Zero ? bd->MouseCursors[(int)imgui_cursor] : bd->MouseCursors[(int)ImGuiMouseCursor.Arrow];
            if (bd->LastMouseCursor != expected_cursor)
            {
                SDL_SetCursor((nint)expected_cursor); // SDL function doesn't have an early out (see #6113)
                bd->LastMouseCursor = expected_cursor;
            }
            SDL_ShowCursor((int)SDL_bool.SDL_TRUE);
        }
    }



    static void ImGui_ImplSDL2_UpdateGamepads()
    {
        var io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) == 0) // FIXME: Technically feeding gamepad shouldn't depend on this now that they are regular inputs.
            return;

        // Get gamepad
        io.BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
        nint game_controller = SDL_GameControllerOpen(0);
        if (game_controller == 0)
            return;
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad;

        static float IM_SATURATE(float v) => v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        void MAP_BUTTON(ImGuiKey key_no, SDL_GameControllerButton button_no)
        {
            io.AddKeyEvent(key_no, SDL_GameControllerGetButton(game_controller, button_no) != 0);
        }

        void MAP_ANALOG(ImGuiKey key_no, SDL_GameControllerAxis axis_no, float v0, float v1)
        {
            float vn = (float)(SDL_GameControllerGetAxis(game_controller, axis_no) - v0) / (float)(v1 - v0);
            vn = IM_SATURATE(vn);
            io.AddKeyAnalogEvent(key_no, vn > 0.1f, vn);
        }

        // Update gamepad inputs
        const int thumb_dead_zone = 8000;           // SDL_gamecontroller.h suggests using this value.
        MAP_BUTTON(ImGuiKey.GamepadStart, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START);
        MAP_BUTTON(ImGuiKey.GamepadBack, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK);
        MAP_BUTTON(ImGuiKey.GamepadFaceLeft, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X);              // Xbox X, PS Square
        MAP_BUTTON(ImGuiKey.GamepadFaceRight, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B);              // Xbox B, PS Circle
        MAP_BUTTON(ImGuiKey.GamepadFaceUp, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y);              // Xbox Y, PS Triangle
        MAP_BUTTON(ImGuiKey.GamepadFaceDown, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A);              // Xbox A, PS Cross
        MAP_BUTTON(ImGuiKey.GamepadDpadLeft, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT);
        MAP_BUTTON(ImGuiKey.GamepadDpadRight, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT);
        MAP_BUTTON(ImGuiKey.GamepadDpadUp, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP);
        MAP_BUTTON(ImGuiKey.GamepadDpadDown, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN);
        MAP_BUTTON(ImGuiKey.GamepadL1, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER);
        MAP_BUTTON(ImGuiKey.GamepadR1, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER);
        MAP_ANALOG(ImGuiKey.GamepadL2, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT, 0.0f, 32767);
        MAP_ANALOG(ImGuiKey.GamepadR2, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT, 0.0f, 32767);
        MAP_BUTTON(ImGuiKey.GamepadL3, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK);
        MAP_BUTTON(ImGuiKey.GamepadR3, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK);
        MAP_ANALOG(ImGuiKey.GamepadLStickLeft, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, -thumb_dead_zone, -32768);
        MAP_ANALOG(ImGuiKey.GamepadLStickRight, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, +thumb_dead_zone, +32767);
        MAP_ANALOG(ImGuiKey.GamepadLStickUp, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, -thumb_dead_zone, -32768);
        MAP_ANALOG(ImGuiKey.GamepadLStickDown, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, +thumb_dead_zone, +32767);
        MAP_ANALOG(ImGuiKey.GamepadRStickLeft, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, -thumb_dead_zone, -32768);
        MAP_ANALOG(ImGuiKey.GamepadRStickRight, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, +thumb_dead_zone, +32767);
        MAP_ANALOG(ImGuiKey.GamepadRStickUp, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, -thumb_dead_zone, -32768);
        MAP_ANALOG(ImGuiKey.GamepadRStickDown, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, +thumb_dead_zone, +32767);
    }

    public static void ImGui_ImplSDL2_NewFrame()
    {
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();
        Debug.Assert(bd != null, "Did you call ImGui_ImplSDL2_Init()?");
        var io = ImGui.GetIO();

        // Setup display size (every frame to accommodate for window resizing)
        int w, h;
        int display_w, display_h;
        SDL_GetWindowSize(bd->Window, out w, out h);
        if ((SDL_GetWindowFlags(bd->Window) & (uint)SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0)
            w = h = 0;
        if (bd->Renderer != IntPtr.Zero)
            _ = SDL_GetRendererOutputSize(bd->Renderer, out display_w, out display_h);
        else
            SDL_GL_GetDrawableSize(bd->Window, out display_w, out display_h);
        io.DisplaySize = new Vector2(w, h);
        if (w > 0 && h > 0)
            io.DisplayFramebufferScale = new Vector2((float)display_w / w, (float)display_h / h);

        // Setup time step (we don't use SDL_GetTicks() because it is using millisecond resolution)
        // (Accept SDL_GetPerformanceCounter() not returning a monotonically increasing value. Happens in VMs and Emscripten, see #6189, #6114, #3644)
        if (!s_PerformanceFrequency.HasValue)
        {
            s_PerformanceFrequency = SDL_GetPerformanceFrequency();
        }
        ulong current_time = SDL_GetPerformanceCounter();
        if (current_time <= bd->Time)
            current_time = bd->Time + 1;
        io.DeltaTime = bd->Time > 0 ? (float)((double)(current_time - bd->Time) / s_PerformanceFrequency.Value) : (float)(1.0f / 60.0f);
        bd->Time = current_time;

        if (bd->PendingMouseLeaveFrame != 0 && bd->PendingMouseLeaveFrame >= ImGui.GetFrameCount() && bd->MouseButtonsDown == 0)
        {
            bd->MouseWindowID = 0;
            bd->PendingMouseLeaveFrame = 0;
            io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
        }

        ImGui_ImplSDL2_UpdateMouseData();
        ImGui_ImplSDL2_UpdateMouseCursor();

        // Update game controllers (if enabled and available)
        ImGui_ImplSDL2_UpdateGamepads();
    }
}
