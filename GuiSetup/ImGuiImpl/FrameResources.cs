using System.Runtime.InteropServices;
using WebGpuSharp.FFI;

namespace GuiSetup.ImGuiImpl;


internal unsafe struct FrameResources : IDisposable
{
    public BufferHandle IndexBuffer;
    public BufferHandle VertexBuffer;
    public ushort* IndexBufferHost;
    public ImDrawVert* VertexBufferHost;
    public int IndexBufferSize;
    public int VertexBufferSize;

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
        NativeMemory.Free(IndexBufferHost);
        NativeMemory.Free(VertexBufferHost);
        IndexBufferHost = null;
        VertexBufferHost = null;
        IndexBufferSize = 0;
        VertexBufferSize = 0;
    }
};
