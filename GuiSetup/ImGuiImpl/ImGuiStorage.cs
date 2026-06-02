using System.Runtime.InteropServices;
using WebGpuSharp.FFI;

namespace GuiSetup.ImGuiImpl;


internal unsafe struct ImGuiStorage : IDisposable
{
    [StructLayout(LayoutKind.Explicit)]
    private unsafe struct Value
    {
        [FieldOffset(0)]
        public int val_i;
        [FieldOffset(0)]
        public float val_f;
        [FieldOffset(0)]
        public void* val_p;
    }

    private readonly GCHandle<SortedList<uint, Value>> _storageHandle;

    public ImGuiStorage()
    {
        _storageHandle = new GCHandle<SortedList<uint, Value>>([]);
    }

    public readonly void Reserve(int count)
    {
        _storageHandle.Target.Capacity = count;
    }

    public readonly void Dispose()
    {
        _storageHandle.Target.Clear();
        _storageHandle.Dispose();
    }

    internal void* GetVoidPtr(uint key)
    {
        _storageHandle.Target.TryGetValue(key, out var value);
        return value.val_p;
    }

    internal void SetVoidPtr(uint key, void* val)
    {
        _storageHandle.Target[key] = new Value { val_p = val };
    }
}
