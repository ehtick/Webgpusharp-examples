using System.Runtime.InteropServices;
using WebGpuSharp.FFI;

namespace GuiSetup.ImGuiImpl;

internal static unsafe class ImGuiDisposer
{
    public static unsafe void SafeRelease(ref ushort* res)
    {
        if (res != null)
        {
            NativeMemory.Free(res);
        }
        res = null;
    }
    public static unsafe void SafeRelease(ref ImDrawVert* res)
    {
        if (res != null)
        {
            NativeMemory.Free(res);
        }
        res = null;
    }

    public static void SafeRelease(ref BindGroupLayoutHandle res)
    {
        if (!BindGroupLayoutHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = BindGroupLayoutHandle.Null;
    }

    public static void SafeRelease(ref BindGroupHandle res)
    {
        if (!BindGroupHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = BindGroupHandle.Null;
    }

    public static void SafeRelease(ref BufferHandle res)
    {
        if (!BufferHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = BufferHandle.Null;
    }

    public static void SafeRelease(ref RenderPipelineHandle res)
    {
        if (!RenderPipelineHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = RenderPipelineHandle.Null;
    }

    public static void SafeRelease(ref SamplerHandle res)
    {
        if (!SamplerHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = SamplerHandle.Null;
    }

    public static void SafeRelease(ref ShaderModuleHandle res)
    {
        if (!ShaderModuleHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = ShaderModuleHandle.Null;
    }

    public static void SafeRelease(ref TextureViewHandle res)
    {
        if (!TextureViewHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = TextureViewHandle.Null;
    }

    public static void SafeRelease(ref TextureHandle res)
    {
        if (!TextureHandle.IsNull(res))
        {
            res.Dispose();
        }
        res = TextureHandle.Null;
    }

    public static void SafeRelease(ref RenderResources res)
    {
        SafeRelease(ref res.FontTexture);
        SafeRelease(ref res.FontTextureView);
        SafeRelease(ref res.Sampler);
        SafeRelease(ref res.Uniforms);
        SafeRelease(ref res.CommonBindGroup);
        SafeRelease(ref res.ImageBindGroup);
        SafeRelease(ref res.ImageBindGroupLayout);
    }

    public static void SafeRelease(ref FrameResources res)
    {
        SafeRelease(ref res.IndexBuffer);
        SafeRelease(ref res.VertexBuffer);
        SafeRelease(ref res.IndexBufferHost);
        SafeRelease(ref res.VertexBufferHost);
    }
}
