
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using SDL2;
using static SDL2.SDL;

internal unsafe static class NewImGui_Impl_SDL2
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SDL_free(IntPtr memblock);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_GetClipboardText")]
    private static extern IntPtr INTERNAL_SDL_GetClipboardText();
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_SetClipboardText")]
    private unsafe static extern int INTERNAL_SDL_SetClipboardText(byte* text);

    struct ImGui_ImplSDL2_Data
    {
        public UIntPtr Window;
        public UIntPtr Renderer;
        public ulong Time;
        public uint MouseWindowID;
        public int MouseButtonsDown;
        public InlineArray9<UIntPtr> MouseCursors;
        public UIntPtr LastMouseCursor;
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
    static byte* ImGui_ImplSDL2_GetClipboardText()
    {
        ImGui_ImplSDL2_Data* bd = ImGui_ImplSDL2_GetBackendData();
        if (bd->ClipboardTextData != null)
        {
            SDL_free((nint)bd->ClipboardTextData);
        }
        bd->ClipboardTextData = (byte*)INTERNAL_SDL_GetClipboardText();
        return bd->ClipboardTextData;
    }


    static void ImGui_ImplSDL2_SetClipboardText(void* _0, byte* text)
    {
        _ = INTERNAL_SDL_SetClipboardText(text);
    }

    // Note: native IME will only display if user calls SDL_SetHint(SDL_HINT_IME_SHOW_UI, "1") _before_ SDL_CreateWindow().
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
    static bool ImGui_ImplSDL2_ProcessEvent(in SDL_Event @event)
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
                    if (!OperatingSystem.IsBrowser())
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
}
