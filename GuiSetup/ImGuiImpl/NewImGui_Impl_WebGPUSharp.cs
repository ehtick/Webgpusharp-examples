using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using WebGpuSharp;
using WebGpuSharp.FFI;
using static WebGpuSharp.Marshalling.WebGPUMarshal;
using static GuiSetup.ImGuiImpl.ImGuiDisposer;
using Buffer = WebGpuSharp.Buffer;
using System.Numerics;

namespace GuiSetup.ImGuiImpl;

public static unsafe class ImGui_Impl_WebGPUSharp
{
    private static readonly GCHandle s_backendNameHandle =
        GCHandle.Alloc("imgui_impl_webgpu\0"u8.ToArray(), GCHandleType.Pinned);
    private static readonly GCHandle s_stage_desc_EntryPointHandle =
        GCHandle.Alloc("main\0"u8.ToArray(), GCHandleType.Pinned);

    static ImGui_ImplWGPU_Data* ImGui_ImplWGPU_GetBackendData()
    {
        return ImGui.GetCurrentContext() != 0 ? (ImGui_ImplWGPU_Data*)ImGui.GetIO().BackendRendererUserData : null;
    }

    static ComputeStateFFI ImGui_ImplWGPU_CreateShaderModule(ReadOnlySpan<byte> wgsl_source)
    {
        fixed (byte* wgsl_source_ptr = wgsl_source)
        {
            ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();

            ShaderSourceWGSLFFI wgsl_desc = new()
            {
                Chain = new()
                {
                    SType = SType.ShaderSourceWGSL,
                    Next = null,
                },
                Code = StringViewFFI.CreateExplicitlySized(wgsl_source_ptr, (nuint)wgsl_source.Length),
            };

            ShaderModuleDescriptorFFI desc = new()
            {
                NextInChain = &wgsl_desc.Chain
            };

            ComputeStateFFI stage_desc = new()
            {
                Module = bd->wgpuDevice.CreateShaderModule(&desc),
                EntryPoint = StringViewFFI.CreateNullTerminated((byte*)s_stage_desc_EntryPointHandle.AddrOfPinnedObject()),
            };

            return stage_desc;
        }
    }

    private static BindGroupHandle ImGui_ImplWGPU_CreateImageBindGroup(BindGroupLayoutHandle layout, TextureViewHandle texture)
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        BindGroupEntryFFI image_bg_entries = new()
        {
            NextInChain = null,
            Binding = 0,
            Buffer = BufferHandle.Null,
            Offset = 0,
            Size = 0,
            TextureView = texture,
        };

        BindGroupDescriptorFFI image_bg_descriptor = new()
        {
            Layout = layout,
            EntryCount = 1,
            Entries = &image_bg_entries,
        };
        return bd->wgpuDevice.CreateBindGroup(&image_bg_descriptor);
    }

    private static void ImGui_ImplWGPU_SetupRenderState(ImDrawDataPtr drawData, RenderPassEncoderHandle ctx, ref FrameResources fr)
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData()!;
        // Setup orthographic projection matrix into our constant buffer
        // Our visible imgui space lies from draw_data->DisplayPos (top left) to draw_data->DisplayPos+data_data->DisplaySize (bottom right).
        {
            float l = drawData.DisplayPos.X;
            float r = drawData.DisplayPos.X + drawData.DisplaySize.X;
            float t = drawData.DisplayPos.Y;
            float b = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            Matrix4x4 mvp = new(
                2.0f / (r - l), 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f / (t - b), 0.0f, 0.0f,
                0.0f, 0.0f, 0.5f, 0.0f,
                (r + l) / (l - r), (t + b) / (b - t), 0.5f, 1.0f
            );
            bd->defaultQueue.WriteBuffer(bd->renderResources.Uniforms!, (ulong)Marshal.OffsetOf<Uniforms>(nameof(Uniforms.mvp)), &mvp, (nuint)sizeof(Matrix4x4));

            float gamma;
            switch (bd->renderTargetFormat)
            {
                case TextureFormat.ASTC10x10UnormSrgb:
                case TextureFormat.ASTC10x5UnormSrgb:
                case TextureFormat.ASTC10x6UnormSrgb:
                case TextureFormat.ASTC10x8UnormSrgb:
                case TextureFormat.ASTC12x10UnormSrgb:
                case TextureFormat.ASTC12x12UnormSrgb:
                case TextureFormat.ASTC4x4UnormSrgb:
                case TextureFormat.ASTC5x5UnormSrgb:
                case TextureFormat.ASTC6x5UnormSrgb:
                case TextureFormat.ASTC6x6UnormSrgb:
                case TextureFormat.ASTC8x5UnormSrgb:
                case TextureFormat.ASTC8x6UnormSrgb:
                case TextureFormat.ASTC8x8UnormSrgb:
                case TextureFormat.BC1RGBAUnormSrgb:
                case TextureFormat.BC2RGBAUnormSrgb:
                case TextureFormat.BC3RGBAUnormSrgb:
                case TextureFormat.BC7RGBAUnormSrgb:
                case TextureFormat.BGRA8UnormSrgb:
                case TextureFormat.ETC2RGB8A1UnormSrgb:
                case TextureFormat.ETC2RGB8UnormSrgb:
                case TextureFormat.ETC2RGBA8UnormSrgb:
                case TextureFormat.RGBA8UnormSrgb:
                    gamma = 2.2f;
                    break;
                default:
                    gamma = 1.0f;
                    break;
            }

            bd->defaultQueue.WriteBuffer(bd->renderResources.Uniforms!, (ulong)Marshal.OffsetOf<Uniforms>(nameof(Uniforms.gamma)), &gamma, sizeof(float));
        }

        // Setup viewport
        ctx.SetViewport(0, 0, drawData.FramebufferScale.X * drawData.DisplaySize.X, drawData.FramebufferScale.Y * drawData.DisplaySize.Y, 0, 1);

        // Bind shader and vertex buffers
        ctx.SetVertexBuffer(0, fr.VertexBuffer!, 0, (ulong)fr.VertexBufferSize * (ulong)sizeof(ImDrawVert));
        ctx.SetIndexBuffer(fr.IndexBuffer!, IndexFormat.Uint16, 0, (ulong)fr.IndexBufferSize * sizeof(ushort));
        ctx.SetPipeline(bd->pipelineState!);
        ctx.SetBindGroup(0, bd->renderResources.CommonBindGroup!, 0, null);

        // Setup blend factor
        ctx.SetBlendConstant(new Color(0, 0, 0, 0));
    }

    public static void ImGui_ImplWGPU_RenderDrawData(ImDrawDataPtr draw_data, RenderPassEncoderHandle passEncoder)
    {
        // Avoid rendering when minimized
        if (draw_data.DisplaySize.X <= 0.0f || draw_data.DisplaySize.Y <= 0.0f)
        {
            return;
        }

        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        bd->frameIndex = bd->frameIndex + 1;
        FrameResources* fr = &bd->pFrameResources[bd->frameIndex % bd->numFramesInFlight];

        // Create and grow vertex/index buffers if needed
        if (fr->VertexBuffer == null || fr->VertexBufferSize < draw_data.TotalVtxCount)
        {
            if (fr->VertexBuffer != null)
            {
                fr->VertexBuffer.Destroy();
                fr->VertexBuffer.Release();
            }
            SafeRelease(ref fr->VertexBufferHost);
            fr->VertexBufferSize = draw_data.TotalVtxCount + 5000;

            BufferDescriptor vb_desc = new()
            {
                Label = "Dear ImGui Vertex buffer",
                Usage = BufferUsage.CopyDst | BufferUsage.Vertex,
                Size = Align((ulong)fr->VertexBufferSize * (ulong)sizeof(ImDrawVert), 4),
                MappedAtCreation = false,
            };
            fr->VertexBuffer = bd->wgpuDevice.CreateBuffer(vb_desc);
            if (fr->VertexBuffer == null)
            {
                return;
            }

            fr->VertexBufferHost = (ImDrawVert*)NativeMemory.Alloc((nuint)fr->VertexBufferSize, (nuint)sizeof(ImDrawVert));
        }

        if (fr->IndexBuffer == null || fr->IndexBufferSize < draw_data.TotalIdxCount)
        {
            if (fr->IndexBuffer != null)
            {
                fr->IndexBuffer.Destroy();
                fr->IndexBuffer.Release();
            }
            SafeRelease(ref fr->IndexBufferHost);
            fr->IndexBufferSize = draw_data.TotalIdxCount + 10000;

            BufferDescriptor ib_desc = new()
            {
                Label = "Dear ImGui Index buffer",
                Usage = BufferUsage.CopyDst | BufferUsage.Index,
                Size = Align((ulong)fr->IndexBufferSize * sizeof(ushort), 4),
                MappedAtCreation = false
            };
            fr->IndexBuffer = bd->wgpuDevice.CreateBuffer(ib_desc);
            if (fr->IndexBuffer == null)
                return;

            fr->IndexBufferHost = (ushort*)NativeMemory.Alloc((nuint)fr->IndexBufferSize, sizeof(ushort));
        }

        // Upload vertex/index data into a single contiguous GPU buffer
        ImDrawVert* vtx_dst = fr->VertexBufferHost;
        ushort* idx_dst = fr->IndexBufferHost;

        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[n];
            Unsafe.CopyBlock(vtx_dst, (void*)cmd_list.VtxBuffer.Data, (uint)cmd_list.VtxBuffer.Size * (uint)sizeof(ImDrawVert));
            Unsafe.CopyBlock(idx_dst, (void*)cmd_list.IdxBuffer.Data, (uint)cmd_list.IdxBuffer.Size * sizeof(ushort));

            vtx_dst += cmd_list.VtxBuffer.Size;
            idx_dst += cmd_list.IdxBuffer.Size;
        }
        long vb_write_size = Align((byte*)vtx_dst - (byte*)fr->VertexBufferHost, 4);
        long ib_write_size = Align((byte*)idx_dst - (byte*)fr->IndexBufferHost, 4);
        bd->defaultQueue.WriteBuffer(fr->VertexBuffer, 0, (void*)fr->VertexBufferHost, (nuint)vb_write_size);
        bd->defaultQueue.WriteBuffer(fr->IndexBuffer, 0, (void*)fr->IndexBufferHost, (nuint)ib_write_size);

        // Setup desired render state
        ImGui_ImplWGPU_SetupRenderState(draw_data, passEncoder, ref *fr);

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_vtx_offset = 0;
        int global_idx_offset = 0;
        Vector2 clip_scale = draw_data.FramebufferScale;
        Vector2 clip_off = draw_data.DisplayPos;
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = draw_data.CmdLists[n];
            for (int cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    // User callback, registered via ImDrawList::AddCallback()
                    // (ImDrawCallback_ResetRenderState is a special callback value used by the user to request the renderer to reset render state.)
                    const nint ImDrawCallback_ResetRenderState = -1;
                    if (pcmd.UserCallback == ImDrawCallback_ResetRenderState)
                    {
                        ImGui_ImplWGPU_SetupRenderState(draw_data, passEncoder, ref *fr);
                    }
                    else
                    {
                        var callback = (delegate* unmanaged[Cdecl]<ImDrawList*, ImDrawCmd*, void>)pcmd.UserCallback;
                        callback(cmdList.NativePtr, pcmd.NativePtr);
                    }
                }
                else
                {
                    // Bind custom texture
                    nint tex_id = pcmd.GetTexID();
                    uint tex_id_hash = ImGuiHash.ImHashData(&tex_id, (nuint)sizeof(nint));
                    var bind_group = bd->renderResources.ImageBindGroups.GetVoidPtr(tex_id_hash);
                    if (bind_group != null)
                    {
                        passEncoder.SetBindGroup(1, new BindGroupHandle((nuint)bind_group), 0, null);
                    }
                    else
                    {
                        BindGroupHandle image_bind_group = ImGui_ImplWGPU_CreateImageBindGroup(bd->renderResources.ImageBindGroupLayout, new TextureViewHandle((nuint)tex_id));
                        bd->renderResources.ImageBindGroups.SetVoidPtr(tex_id_hash, (void*)image_bind_group.GetAddress());
                        passEncoder.SetBindGroup(1, image_bind_group, 0, null);
                    }

                    // Project scissor/clipping rectangles into framebuffer space
                    Vector2 clip_min = new((pcmd.ClipRect.X - clip_off.X) * clip_scale.X, (pcmd.ClipRect.Y - clip_off.Y) * clip_scale.Y);
                    Vector2 clip_max = new((pcmd.ClipRect.Z - clip_off.X) * clip_scale.X, (pcmd.ClipRect.W - clip_off.Y) * clip_scale.Y);
                    if (clip_max.X <= clip_min.X || clip_max.Y <= clip_min.Y)
                    {
                        continue;
                    }

                    // Apply scissor/clipping rectangle, Draw
                    passEncoder.SetScissorRect((uint)clip_min.X, (uint)clip_min.Y, (uint)(clip_max.X - clip_min.X), (uint)(clip_max.Y - clip_min.Y));
                    passEncoder.DrawIndexed(pcmd.ElemCount, 1, (uint)(pcmd.IdxOffset + global_idx_offset), (int)(pcmd.VtxOffset + global_vtx_offset), 0);
                }
            }

            global_idx_offset += cmdList.IdxBuffer.Size;
            global_vtx_offset += cmdList.VtxBuffer.Size;
        }
    }

    private static void ImGui_ImplWGPU_CreateFontsTexture()
    {
        // Build texture atlas
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        ref ImGuiIO io = ref *ImGui.GetIO().NativePtr;
        new ImFontAtlasPtr(io.Fonts).GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int size_pp);

        // Upload texture to graphics system
        {
            TextureDescriptor tex_desc = new()
            {
                Label = "Dear ImGui Font Texture",
                Dimension = TextureDimension.D2,
                Size = new((uint)width, (uint)height, 1),
                SampleCount = 1,
                Format = TextureFormat.RGBA8Unorm,
                MipLevelCount = 1,
                Usage = TextureUsage.CopyDst | TextureUsage.TextureBinding,
            };
            bd->renderResources.FontTexture = bd->wgpuDevice.CreateTexture(tex_desc);

            TextureViewDescriptor textureViewDescriptor = new()
            {
                Format = TextureFormat.RGBA8Unorm,
                Dimension = TextureViewDimension.D2,
                BaseMipLevel = 0,
                MipLevelCount = 1,
                BaseArrayLayer = 0,
                ArrayLayerCount = 1,
                Aspect = TextureAspect.All,
            };
            bd->renderResources.FontTextureView = bd->renderResources.FontTexture.CreateView(textureViewDescriptor);
        }

        // Upload texture data
        {
            TexelCopyTextureInfoFFI dst_view = new()
            {
                Texture = bd->renderResources.FontTexture,
                MipLevel = 0,
                Origin = new(0, 0, 0),
                Aspect = TextureAspect.All,
            };
            TexelCopyBufferLayout layout = new()
            {
                Offset = 0,
                BytesPerRow = (uint)(width * size_pp),
                RowsPerImage = (uint)height,
            };
            Extent3D size = new((uint)width, (uint)height, 1);
            bd->defaultQueue.WriteTexture(dst_view, new ReadOnlySpan<byte>(pixels, width * size_pp * height), layout, size);
        }

        // Create the associated sampler
        // (Bilinear sampling is required by default. Set 'io.Fonts->Flags |= ImFontAtlasFlags_NoBakedLines' or 'style.AntiAliasedLinesUseTex = false' to allow point/nearest sampling)
        {
            SamplerDescriptorFFI sampler_desc = new()
            {
                MinFilter = FilterMode.Linear,
                MagFilter = FilterMode.Linear,
                MipmapFilter = MipmapFilterMode.Linear,
                AddressModeU = AddressMode.Repeat,
                AddressModeV = AddressMode.Repeat,
                AddressModeW = AddressMode.Repeat,
                MaxAnisotropy = 1,
            };
            bd->renderResources.Sampler = bd->wgpuDevice.CreateSampler(&sampler_desc);
        }

        // Store our identifier
        new ImFontAtlasPtr(io.Fonts).SetTexID((nint)bd->renderResources.FontTextureView.GetAddress());
    }

    private static void ImGui_ImplWGPU_CreateUniformBuffer()
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData()!;
        bd->renderResources.Uniforms = bd->wgpuDevice.CreateBuffer(new BufferDescriptor()
        {
            Label = "Dear ImGui Uniform buffer"u8,
            Usage = BufferUsage.CopyDst | BufferUsage.Uniform,
            Size = Align((ulong)sizeof(Uniforms), 16),
            MappedAtCreation = false,
        });
    }

    public static bool ImGui_ImplWGPU_CreateDeviceObjects()
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        if (bd->wgpuDevice == null)
        {
            return false;
        }

        if (bd->pipelineState != null)
        {
            ImGui_ImplWGPU_InvalidateDeviceObjects();
        }

        // Create render pipeline
        RenderPipelineDescriptorFFI graphics_pipeline_desc = new()
        {
            Vertex = default,//set later
            Primitive = new()
            {
                Topology = PrimitiveTopology.TriangleList,
                StripIndexFormat = IndexFormat.Undefined,
                FrontFace = FrontFace.CW,
                CullMode = CullMode.None,
            },
            Multisample = new()
            {
                Count = 1,
                Mask = uint.MaxValue,
                AlphaToCoverageEnabled = false,
            },
        };

        // Bind group layouts
        InlineArray2<BindGroupLayoutEntry> common_bg_layout_entries = default;
        common_bg_layout_entries[0] = new()
        {
            Binding = 0,
            Visibility = ShaderStage.Vertex | ShaderStage.Fragment,
            Buffer = new() { Type = BufferBindingType.Uniform },
        };
        common_bg_layout_entries[1] = new()
        {
            Binding = 1,
            Visibility = ShaderStage.Fragment,
            Sampler = new() { Type = SamplerBindingType.Filtering },
        };

        BindGroupLayoutEntry image_bg_layout_entries = new()
        {
            Binding = 0,
            Visibility = ShaderStage.Fragment,
            Texture = new()
            {
                SampleType = TextureSampleType.Float,
                ViewDimension = TextureViewDimension.D2,
            }
        };

        BindGroupLayoutDescriptorFFI common_bg_layout_desc = new()
        {
            EntryCount = 2,
            Entries = &common_bg_layout_entries[0],
        };

        BindGroupLayoutDescriptorFFI image_bg_layout_desc = new()
        {
            EntryCount = 1,
            Entries = &image_bg_layout_entries,
        };

        InlineArray2<BindGroupLayoutHandle> bg_layouts = default;
        bg_layouts[0] = bd->wgpuDevice.CreateBindGroupLayout(&common_bg_layout_desc);
        bg_layouts[1] = bd->wgpuDevice.CreateBindGroupLayout(&image_bg_layout_desc);


        PipelineLayoutDescriptorFFI layout_desc = new()
        {
            BindGroupLayoutCount = 2,
            BindGroupLayouts = &bg_layouts[0],
        };
        graphics_pipeline_desc.Layout = bd->wgpuDevice.CreatePipelineLayout(&layout_desc);

        // Create the vertex shader
        ComputeStateFFI vertex_shader_desc = ImGui_ImplWGPU_CreateShaderModule(ImGuiShaders.ShaderVertWgsl);
        graphics_pipeline_desc.Vertex.Module = vertex_shader_desc.Module;
        graphics_pipeline_desc.Vertex.EntryPoint = vertex_shader_desc.EntryPoint;


        InlineArray3<VertexAttribute> attribute_desc = default;
        attribute_desc[0] = new()
        {
            Format = VertexFormat.Float32x2,
            Offset = (ulong)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos)),
            ShaderLocation = 0,
        };
        attribute_desc[1] = new()
        {
            Format = VertexFormat.Float32x2,
            Offset = (ulong)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv)),
            ShaderLocation = 1,
        };
        attribute_desc[2] = new()
        {
            Format = VertexFormat.Unorm8x4,
            Offset = (ulong)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col)),
            ShaderLocation = 2,
        };

        VertexBufferLayoutFFI buffer_layouts = new VertexBufferLayoutFFI()
        {
            ArrayStride = (ulong)sizeof(ImDrawVert),
            StepMode = VertexStepMode.Vertex,
            AttributeCount = 3,
            Attributes = &attribute_desc[0],
        };

        graphics_pipeline_desc.Vertex.BufferCount = 1;
        graphics_pipeline_desc.Vertex.Buffers = &buffer_layouts;

        // Create the pixel shader
        ComputeStateFFI pixel_shader_desc = ImGui_ImplWGPU_CreateShaderModule(ImGuiShaders.ShaderFragWgsl);

        BlendState blend_state = new()
        {
            Alpha = new BlendComponent
            {
                Operation = BlendOperation.Add,
                SrcFactor = BlendFactor.One,
                DstFactor = BlendFactor.OneMinusSrcAlpha,
            },
            Color = new BlendComponent
            {
                Operation = BlendOperation.Add,
                SrcFactor = BlendFactor.SrcAlpha,
                DstFactor = BlendFactor.OneMinusSrcAlpha,
            }
        };

        ColorTargetStateFFI color_state = new()
        {
            Format = bd->renderTargetFormat,
            Blend = &blend_state,
            WriteMask = ColorWriteMask.All,
        };

        FragmentStateFFI fragment_state = new()
        {
            Module = pixel_shader_desc.Module,
            EntryPoint = pixel_shader_desc.EntryPoint,
            TargetCount = 1,
            Targets = &color_state
        };

        graphics_pipeline_desc.Fragment = &fragment_state;

        // Create depth-stencil State
        DepthStencilState depth_stencil_state = new()
        {
            Format = bd->depthStencilFormat,
            DepthWriteEnabled = OptionalBool.False,
            DepthCompare = CompareFunction.Always,
            StencilFront = new StencilFaceState
            {
                Compare = CompareFunction.Always,
            },
            StencilBack = new StencilFaceState
            {
                Compare = CompareFunction.Always,
            }
        };

        // Configure disabled depth-stencil state
        graphics_pipeline_desc.DepthStencil = bd->depthStencilFormat == TextureFormat.Undefined ? null : &depth_stencil_state;

        bd->pipelineState = bd->wgpuDevice.CreateRenderPipeline(&graphics_pipeline_desc);

        ImGui_ImplWGPU_CreateFontsTexture();
        ImGui_ImplWGPU_CreateUniformBuffer();

        InlineArray2<BindGroupEntryFFI> common_bg_entries = default;
        common_bg_entries[0] = new()
        {
            Binding = 0,
            Buffer = bd->renderResources.Uniforms,
            Offset = 0,
            Size = Align((ulong)sizeof(Uniforms), 16),
        };
        common_bg_entries[1] = new()
        {
            Binding = 1,
            Offset = 0,
            Size = 0,
            Sampler = bd->renderResources.Sampler,
        };

        BindGroupDescriptorFFI common_bg_descriptor = new()
        {
            Layout = bg_layouts[0],
            EntryCount = 2,
            Entries = &common_bg_entries[0],
        };
        bd->renderResources.CommonBindGroup = bd->wgpuDevice.CreateBindGroup(&common_bg_descriptor);

        BindGroupHandle image_bind_group = ImGui_ImplWGPU_CreateImageBindGroup(bg_layouts[1], bd->renderResources.FontTextureView);
        bd->renderResources.ImageBindGroup = image_bind_group;
        bd->renderResources.ImageBindGroupLayout = bg_layouts[1];
        bd->renderResources.ImageBindGroups.SetVoidPtr(ImGuiHash.ImHashData((void*)bd->renderResources.FontTextureView.GetAddress(), sizeof(nuint)), (void*)image_bind_group.GetAddress());


        SafeRelease(ref vertex_shader_desc.Module);
        SafeRelease(ref pixel_shader_desc.Module);
        SafeRelease(ref bg_layouts[0]);

        return true;
    }

    public static void ImGui_ImplWGPU_InvalidateDeviceObjects()
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        if (bd->wgpuDevice == null)
        {
            return;
        }

        SafeRelease(ref bd->pipelineState);
        SafeRelease(ref bd->renderResources);

        var io = ImGui.GetIO();
        io.Fonts.SetTexID(IntPtr.Zero);

        for (int i = 0; i < bd->numFramesInFlight; i++)
        {
            SafeRelease(ref bd->pFrameResources[i]);
        }
    }

    public static unsafe bool ImGui_ImplWGPU_Init(DeviceHandle device, uint numFramesInFlight, TextureFormat rtFormat, TextureFormat depthFormat)
    {
        var io = ImGui.GetIO();
        Debug.Assert(io.BackendRendererUserData == IntPtr.Zero, "Already initialized a renderer backend!");

        ImGui_ImplWGPU_Data* bd = (ImGui_ImplWGPU_Data*)NativeMemory.Alloc((nuint)sizeof(ImGui_ImplWGPU_Data));
        *bd = new ImGui_ImplWGPU_Data();
        io.NativePtr->BackendRendererUserData = bd;
        io.NativePtr->BackendRendererName = (byte*)s_backendNameHandle.AddrOfPinnedObject();
        io.NativePtr->BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        bd->wgpuDevice = device;
        bd->defaultQueue = device.GetQueue();
        bd->renderTargetFormat = rtFormat;
        bd->depthStencilFormat = depthFormat;
        bd->numFramesInFlight = numFramesInFlight;
        bd->frameIndex = uint.MaxValue;

        bd->renderResources.FontTexture = TextureHandle.Null;
        bd->renderResources.FontTextureView = TextureViewHandle.Null;
        bd->renderResources.Sampler = SamplerHandle.Null;
        bd->renderResources.Uniforms = BufferHandle.Null;
        bd->renderResources.CommonBindGroup = BindGroupHandle.Null;
        bd->renderResources.ImageBindGroups.Reserve(100);
        bd->renderResources.ImageBindGroup = BindGroupHandle.Null;
        bd->renderResources.ImageBindGroupLayout = BindGroupLayoutHandle.Null;

        bd->pFrameResources = (FrameResources*)NativeMemory.Alloc(numFramesInFlight * (nuint)sizeof(FrameResources));
        for (int i = 0; i < numFramesInFlight; i++)
        {
            FrameResources* fr = &bd->pFrameResources[i];
            fr->IndexBuffer = BufferHandle.Null;
            fr->VertexBuffer = BufferHandle.Null;
            fr->IndexBufferHost = null;
            fr->VertexBufferHost = null;
            fr->IndexBufferSize = 10000;
            fr->VertexBufferSize = 5000;


        }

        return true;
    }

    public static void ImGui_ImplWGPU_Shutdown()
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        Debug.Assert(bd != null, "No renderer backend to shutdown, or already shutdown?");
        ref ImGuiIO io = ref *ImGui.GetIO().NativePtr;

        ImGui_ImplWGPU_InvalidateDeviceObjects();
        NativeMemory.Free(bd->pFrameResources);
        bd->pFrameResources = null;
        bd->defaultQueue.Dispose();
        bd->defaultQueue = QueueHandle.Null;
        bd->wgpuDevice = DeviceHandle.Null;
        bd->numFramesInFlight = 0;
        bd->frameIndex = uint.MaxValue;

        io.BackendRendererName = null;
        io.BackendRendererUserData = null;
        io.BackendFlags &= ~ImGuiBackendFlags.RendererHasVtxOffset;
        bd->renderResources.ImageBindGroups.Dispose();
        NativeMemory.Free(bd);
    }

    public static void ImGui_ImplWGPU_NewFrame()
    {
        ImGui_ImplWGPU_Data* bd = ImGui_ImplWGPU_GetBackendData();
        if (bd->pipelineState == null)
        {
            ImGui_ImplWGPU_CreateDeviceObjects();
        }
    }

    private static ulong Align(ulong size, ulong align)
    {
        return (size + (align - 1)) & ~(align - 1);
    }

    private static long Align(long size, long align)
    {
        return (size + (align - 1)) & ~(align - 1);
    }
}
