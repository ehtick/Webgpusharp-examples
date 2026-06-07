using System.Runtime.InteropServices;
using WebGpuSharp;
using WebGpuSharp.FFI;

namespace GuiSetup.ImGuiImpl;

internal unsafe struct ImGui_ImplWGPU_Data
{
    public DeviceHandle wgpuDevice = DeviceHandle.Null;
    public QueueHandle defaultQueue = QueueHandle.Null;
    public TextureFormat renderTargetFormat = TextureFormat.Undefined;
    public TextureFormat depthStencilFormat = TextureFormat.Undefined;
    public RenderPipelineHandle pipelineState = RenderPipelineHandle.Null;

    public RenderResources renderResources = new();
    public FrameResources* pFrameResources = null;
    public uint numFramesInFlight = 0;
    public uint frameIndex = uint.MaxValue;

    public ImGui_ImplWGPU_Data()
    {
    }
}
