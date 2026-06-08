using WebGpuSharp.FFI;

namespace GuiSetup.ImGuiImpl;


internal struct RenderResources()
{
    /// <summary>
    /// Font texture
    /// </summary>
    public TextureHandle FontTexture = TextureHandle.Null;
    /// <summary>
    /// Texture view for the font texture
    /// </summary>
    public TextureViewHandle FontTextureView = TextureViewHandle.Null;      // Texture view for font texture
    /// <summary>
    /// Sampler for the font texture
    /// </summary>
    public SamplerHandle Sampler = SamplerHandle.Null;              // Sampler for the font texture
    /// <summary>
    /// Shader uniforms
    /// </summary>
    public BufferHandle Uniforms = BufferHandle.Null;
    /// <summary>
    /// Resources bind-group to bind the common resources to pipeline
    /// </summary>
    public BindGroupHandle CommonBindGroup = BindGroupHandle.Null;
    /// <summary>
    /// Resources bind-group to bind the font/image resources to pipeline (this is a key->value map)
    /// </summary>
    public ImGuiStorage ImageBindGroups = new();
    /// <summary>
    /// Default font-resource of Dear ImGui
    /// </summary>
    public BindGroupHandle ImageBindGroup = BindGroupHandle.Null;
    /// <summary>
    ///  Cache layout used for the image bind group. Avoids allocating unnecessary JS objects when working with WebASM
    /// </summary>
    public BindGroupLayoutHandle ImageBindGroupLayout = BindGroupLayoutHandle.Null;
}
