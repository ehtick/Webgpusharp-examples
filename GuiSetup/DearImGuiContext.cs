using GuiSetup.ImGuiImpl;
using ImGuiNET;
using SDL2;
using Setup;
using WebGpuSharp;
using WebGpuSharp.Marshalling;

namespace GuiSetup;

public class DearImGuiContext : IGuiContext<DearImGuiContext>
{
    private IntPtr? _window;
    private Device? _device;

    private DearImGuiContext()
    {
    }

    public static DearImGuiContext Create(nint window)
    {
        var context = new DearImGuiContext();
        context._window = window;
        return context;
    }

    public void SetupIMGUI(Device device, TextureFormat ttFormat)
    {
        if (_window is null)
            throw new InvalidOperationException("Window must be set before setting up ImGui");

        _device = device;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        unsafe
        {
            io.NativePtr->IniFilename = null;
        }

        ImGui_Impl_WebGPUSharp.ImGui_ImplWGPU_Init(
            device: WebGPUMarshal.GetHandle(device).AddRef(),
            numFramesInFlight: 3,
            rtFormat: ttFormat,
            depthFormat: TextureFormat.Undefined
        );
        ImGui_Impl_SDL2.ImGui_ImplSDL2_Init(_window.Value, IntPtr.Zero);

        io.Fonts.AddFontDefault();
        io.Fonts.Build();
    }

    public void NewFrame()
    {
        ImGui_Impl_SDL2.ImGui_ImplSDL2_NewFrame();
        ImGui_Impl_WebGPUSharp.ImGui_ImplWGPU_NewFrame();
        ImGui.NewFrame();
    }

    public void EndFrame()
    {
        ImGui.EndFrame();
    }

    public CommandBuffer? Render(Surface surface)
    {
        // Perform rendering
        SurfaceTexture surfaceTexture = surface.GetCurrentTexture();
        // Failed to get the surface texture. TODO handle
        if (surfaceTexture.Status is not (SurfaceGetCurrentTextureStatus.SuccessOptimal or SurfaceGetCurrentTextureStatus.SuccessSuboptimal))
            return null;

        TextureViewDescriptor viewdescriptor = new()
        {
            Format = surfaceTexture.Texture!.GetFormat(),
            Dimension = TextureViewDimension.D2,
            MipLevelCount = 1,
            BaseMipLevel = 0,
            BaseArrayLayer = 0,
            ArrayLayerCount = 1,
            Aspect = TextureAspect.All,
        };
        TextureView textureView = surfaceTexture.Texture.CreateView(viewdescriptor) ?? throw new Exception("Failed to create texture view");

        // Command Encoder
        var commandEncoder = _device!.CreateCommandEncoder(new() { Label = "Main Command Encoder" });

        Span<RenderPassColorAttachment> colorAttachments = [
            new(){
                        View = textureView,
                        ResolveTarget = default,
                        LoadOp = LoadOp.Load,
                        StoreOp = StoreOp.Store,
                        ClearValue = new Color(0,0,0,0)
                    }
        ];

        // Render Imgui
        {
            RenderPassDescriptor renderPassDesc = new()
            {
                Label = "Pass IMGUI",
                ColorAttachments = colorAttachments,
                DepthStencilAttachment = null
            };
            var RenderPassEncoder = commandEncoder.BeginRenderPass(renderPassDesc);

            ImGui.Render();
            ImGui_Impl_WebGPUSharp.ImGui_ImplWGPU_RenderDrawData(ImGui.GetDrawData(), WebGPUMarshal.GetHandle(RenderPassEncoder));

            RenderPassEncoder.End();
        }

        // Finish Rendering
        return commandEncoder.Finish(new() { });
    }

    bool IGuiContext.ProcessEvent(in SDL.SDL_Event @event)
    {
        return ImGui_Impl_SDL2.ImGui_ImplSDL2_ProcessEvent(@event);
    }
}
