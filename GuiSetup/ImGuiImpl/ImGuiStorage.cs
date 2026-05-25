using System.Runtime.InteropServices;

namespace GuiSetup.ImGuiImpl;


internal struct ImGuiStorage
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

    private readonly SortedList<uint, Value> _storage = [];

    public ImGuiStorage()
    {
    }
}