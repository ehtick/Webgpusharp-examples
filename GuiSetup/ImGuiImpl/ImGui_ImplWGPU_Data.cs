using System.Runtime.InteropServices;
using WebGpuSharp;
using WebGpuSharp.FFI;

namespace GuiSetup.ImGuiImpl;

internal unsafe struct ImGui_ImplWGPU_Data : IDisposable
{
    public DeviceHandle wgpuDevice = DeviceHandle.Null;
    public QueueHandle defaultQueue = QueueHandle.Null;
    public TextureFormat renderTargetFormat = TextureFormat.Undefined;
    public TextureFormat depthStencilFormat = TextureFormat.Undefined;
    public RenderPipelineHandle pipelineState = RenderPipelineHandle.Null;

    public RenderResources renderResources;
    public FrameResources* pFrameResources = null;
    public uint numFramesInFlight = 0;
    public uint frameIndex = uint.MaxValue;

    public ImGui_ImplWGPU_Data()
    {
    }

    public void Dispose()
    {
        wgpuDevice.Dispose();
        wgpuDevice = DeviceHandle.Null;
        defaultQueue.Dispose();
        defaultQueue = QueueHandle.Null;
        pipelineState.Dispose();
        pipelineState = RenderPipelineHandle.Null;
        renderResources.Dispose();
        renderResources = new();
        if (pFrameResources != null)
        {
            for (uint i = 0; i < numFramesInFlight; i++)
            {
                pFrameResources[i].Dispose();
            }
            NativeMemory.Free(pFrameResources);
            pFrameResources = null;
            numFramesInFlight = 0;
        }
    }
}