using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using WebGpuSharp;
using WebGpuSharp.FFI;
using static WebGpuSharp.Marshalling.WebGPUMarshal;
using Buffer = WebGpuSharp.Buffer;

namespace GuiSetup;

public static unsafe class ImGui_Impl_WebGPUSharp
{
	private static readonly nint s_backendName = Marshal.StringToHGlobalAnsi("imgui_impl_webgpu");
	private static readonly IntPtr s_resetRenderStateCallback = new(-1);
	private static readonly uint[] s_crc32LookupTable =
	[
		0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91,
		0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9, 0xFA0F3D63, 0x8D080DF5,
		0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59,
		0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599, 0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924, 0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D,
		0x76DC4190, 0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
		0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950, 0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65,
		0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9,
		0x5005713C, 0x270241AA, 0xBE0B1010, 0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F, 0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD,
		0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
		0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
		0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF, 0x4669BE79,
		0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236, 0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE, 0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
		0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713, 0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21,
		0x86D3D2D4, 0xF1D4E242, 0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45,
		0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
		0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605, 0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D,
	];

	private const string ShaderVertWgsl = @"
struct VertexInput {
	@location(0) position: vec2<f32>,
	@location(1) uv: vec2<f32>,
	@location(2) color: vec4<f32>,
};

struct VertexOutput {
	@builtin(position) position: vec4<f32>,
	@location(0) color: vec4<f32>,
	@location(1) uv: vec2<f32>,
};

struct Uniforms {
	mvp: mat4x4<f32>,
	gamma: f32,
};

@group(0) @binding(0) var<uniform> uniforms: Uniforms;

@vertex
fn main(in: VertexInput) -> VertexOutput {
	var out: VertexOutput;
	out.position = uniforms.mvp * vec4<f32>(in.position, 0.0, 1.0);
	out.color = in.color;
	out.uv = in.uv;
	return out;
}
";

	private const string ShaderFragWgsl = @"
struct VertexOutput {
	@builtin(position) position: vec4<f32>,
	@location(0) color: vec4<f32>,
	@location(1) uv: vec2<f32>,
};

struct Uniforms {
	mvp: mat4x4<f32>,
	gamma: f32,
};

@group(0) @binding(0) var<uniform> uniforms: Uniforms;
@group(0) @binding(1) var s: sampler;
@group(1) @binding(0) var t: texture_2d<f32>;

@fragment
fn main(in: VertexOutput) -> @location(0) vec4<f32> {
	let color = in.color * textureSample(t, s, in.uv);
	let corrected_color = pow(color.rgb, vec3<f32>(uniforms.gamma));
	return vec4<f32>(corrected_color, color.a);
}
";

	public struct ImGui_ImplWGPU_InitInfo
	{
		public required Device device;
		public required int num_frames_in_flight;
		public required TextureFormat rt_format;
		public TextureFormat depth_format;
	}

	private struct ImageBindGroupEntry
	{
		public TextureView? TextureView;
		public BindGroup? BindGroup;
	}

	private struct RenderResources
	{
		public Texture? FontTexture;
		public TextureView? FontTextureView;
		public Sampler? Sampler;
		public Buffer? Uniforms;
		public BindGroup? CommonBindGroup;
		public Dictionary<uint, ImageBindGroupEntry>? ImageBindGroups;
		public BindGroup? ImageBindGroup;
		public BindGroupLayout? ImageBindGroupLayout;
	}

	private struct FrameResources
	{
		public Buffer? IndexBuffer;
		public Buffer? VertexBuffer;
		public ushort* IndexBufferHost;
		public ImDrawVert* VertexBufferHost;
		public int IndexBufferSize;
		public int VertexBufferSize;
	}

	[StructLayout(LayoutKind.Sequential)]
	private unsafe struct Uniforms
	{
		public fixed float MVP[16];
		public float Gamma;
		private fixed float Padding[3];
	}

	private sealed class ImGui_ImplWGPU_Data
	{
		public required Device Device;
		public required Queue DefaultQueue;
		public TextureFormat RenderTargetFormat = TextureFormat.Undefined;
		public TextureFormat DepthStencilFormat = TextureFormat.Undefined;
		public RenderPipeline? PipelineState;
		public RenderResources RenderResources;
		public FrameResources[] FrameResources = [];
		public int NumFramesInFlight;
		public uint FrameIndex = uint.MaxValue;
		public GCHandle Handle;
	}

	public static bool Init(ImGui_ImplWGPU_InitInfo initInfo)
	{
		if (initInfo.num_frames_in_flight <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(initInfo.num_frames_in_flight), "num_frames_in_flight must be greater than zero.");
		}

		var io = ImGui.GetIO();
		if (io.BackendRendererUserData != IntPtr.Zero)
		{
			throw new InvalidOperationException("Renderer backend already initialized.");
		}

		var bd = new ImGui_ImplWGPU_Data
		{
			Device = initInfo.device,
			DefaultQueue = initInfo.device.GetQueue(),
			RenderTargetFormat = initInfo.rt_format,
			DepthStencilFormat = initInfo.depth_format,
			NumFramesInFlight = initInfo.num_frames_in_flight,
			FrameIndex = uint.MaxValue,
			FrameResources = new FrameResources[initInfo.num_frames_in_flight],
			RenderResources = new RenderResources
			{
				ImageBindGroups = new Dictionary<uint, ImageBindGroupEntry>(100),
			},
		};

		for (int i = 0; i < bd.FrameResources.Length; i++)
		{
			ResetFrameResources(ref bd.FrameResources[i]);
		}

		bd.Handle = GCHandle.Alloc(bd);

		io.BackendRendererUserData = GCHandle.ToIntPtr(bd.Handle);
		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
		io.NativePtr->BackendRendererName = (byte*)s_backendName;

		return true;
	}

	public static void Shutdown()
	{
		var bd = GetBackendData();
		if (bd == null)
		{
			throw new InvalidOperationException("No renderer backend to shutdown, or already shutdown.");
		}

		var io = ImGui.GetIO();
		InvalidateDeviceObjects();

		if (bd.Handle.IsAllocated)
		{
			bd.Handle.Free();
		}

		io.NativePtr->BackendRendererName = null;
		io.NativePtr->BackendRendererUserData = null;
		io.BackendFlags &= ~ImGuiBackendFlags.RendererHasVtxOffset;
	}

	public static void NewFrame()
	{
		var bd = GetBackendData();
		if (bd == null)
		{
			throw new InvalidOperationException("Renderer backend is not initialized.");
		}

		if (bd.PipelineState == null)
		{
			if (!CreateDeviceObjects())
			{
				throw new InvalidOperationException("Failed to create ImGui WebGPU device objects.");
			}
		}
	}

	public static bool CreateDeviceObjects()
	{
		var bd = GetBackendData();
		if (bd == null)
		{
			return false;
		}

		if (bd.PipelineState != null)
		{
			InvalidateDeviceObjects();
		}

		var commonBindGroupLayout = bd.Device.CreateBindGroupLayout(new()
		{
			Entries =
			[
				new()
				{
					Binding = 0,
					Visibility = ShaderStage.Vertex | ShaderStage.Fragment,
					Buffer = new() { Type = BufferBindingType.Uniform },
				},
				new()
				{
					Binding = 1,
					Visibility = ShaderStage.Fragment,
					Sampler = new() { Type = SamplerBindingType.Filtering },
				},
			],
		});

		var imageBindGroupLayout = bd.Device.CreateBindGroupLayout(new()
		{
			Entries =
			[
				new()
				{
					Binding = 0,
					Visibility = ShaderStage.Fragment,
					Texture = new()
					{
						SampleType = TextureSampleType.Float,
						ViewDimension = TextureViewDimension.D2,
					},
				},
			],
		});

		var pipelineLayout = bd.Device.CreatePipelineLayout(new()
		{
			BindGroupLayouts = [commonBindGroupLayout, imageBindGroupLayout],
		});

		var vertexShader = bd.Device.CreateShaderModuleWGSL(new() { Code = ShaderVertWgsl });
		var fragmentShader = bd.Device.CreateShaderModuleWGSL(new() { Code = ShaderFragWgsl });

		var colorBlend = new BlendComponent
		{
			Operation = BlendOperation.Add,
			SrcFactor = BlendFactor.SrcAlpha,
			DstFactor = BlendFactor.OneMinusSrcAlpha,
		};
		var alphaBlend = new BlendComponent
		{
			Operation = BlendOperation.Add,
			SrcFactor = BlendFactor.One,
			DstFactor = BlendFactor.OneMinusSrcAlpha,
		};

		DepthStencilState? depthStencil = null;
		if (bd.DepthStencilFormat != TextureFormat.Undefined)
		{
			depthStencil = new DepthStencilState
			{
				Format = bd.DepthStencilFormat,
				DepthWriteEnabled = OptionalBool.False,
				DepthCompare = CompareFunction.Always,
				StencilFront = new() { Compare = CompareFunction.Always },
				StencilBack = new() { Compare = CompareFunction.Always },
			};
		}

		bd.PipelineState = bd.Device.CreateRenderPipelineSync(new()
		{
			Layout = pipelineLayout,
			Vertex = new()
			{
				Module = vertexShader,
				EntryPoint = "main",
				Buffers =
				[
					new VertexBufferLayout
					{
						ArrayStride = (ulong)Unsafe.SizeOf<ImDrawVert>(),
						Attributes =
						[
							new VertexAttribute
							{
								Format = VertexFormat.Float32x2,
								Offset = (ulong)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos)),
								ShaderLocation = 0,
							},
							new VertexAttribute
							{
								Format = VertexFormat.Float32x2,
								Offset = (ulong)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv)),
								ShaderLocation = 1,
							},
							new VertexAttribute
							{
								Format = VertexFormat.Unorm8x4,
								Offset = (ulong)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col)),
								ShaderLocation = 2,
							},
						],
					},
				],
			},
			Primitive = new()
			{
				Topology = PrimitiveTopology.TriangleList,
				StripIndexFormat = IndexFormat.Undefined,
				FrontFace = FrontFace.CW,
				CullMode = CullMode.None,
			},
			DepthStencil = depthStencil,
			Multisample = new()
			{
				Count = 1,
				Mask = uint.MaxValue,
				AlphaToCoverageEnabled = false,
			},
			Fragment = new()
			{
				Module = fragmentShader,
				EntryPoint = "main",
				Targets =
				[
					new ColorTargetState
					{
						Format = bd.RenderTargetFormat,
						Blend = new BlendState
						{
							Color = colorBlend,
							Alpha = alphaBlend,
						},
						WriteMask = ColorWriteMask.All,
					},
				],
			},
		});

		CreateFontsTexture();
		CreateUniformBuffer();

		bd.RenderResources.CommonBindGroup = bd.Device.CreateBindGroup(new()
		{
			Layout = commonBindGroupLayout,
			Entries =
			[
				new()
				{
					Binding = 0,
					Buffer = bd.RenderResources.Uniforms,
					Size = Align((ulong)Unsafe.SizeOf<Uniforms>(), 16),
				},
				new()
				{
					Binding = 1,
					Sampler = bd.RenderResources.Sampler,
				},
			],
		});

		bd.RenderResources.ImageBindGroup = CreateImageBindGroup(imageBindGroupLayout, bd.RenderResources.FontTextureView!);
		bd.RenderResources.ImageBindGroupLayout = imageBindGroupLayout;
		bd.RenderResources.ImageBindGroups!.Clear();
		bd.RenderResources.ImageBindGroups[GetTextureHash(GetImGuiTextureID(bd.RenderResources.FontTextureView!))] = new ImageBindGroupEntry
		{
			TextureView = bd.RenderResources.FontTextureView,
			BindGroup = bd.RenderResources.ImageBindGroup,
		};

		return true;
	}

	public static void InvalidateDeviceObjects()
	{
		var bd = GetBackendData();
		if (bd == null)
		{
			return;
		}

		bd.PipelineState = null;
		SafeRelease(ref bd.RenderResources);

		var io = ImGui.GetIO();
		io.Fonts.SetTexID(IntPtr.Zero);

		for (int i = 0; i < bd.FrameResources.Length; i++)
		{
			SafeRelease(ref bd.FrameResources[i]);
			ResetFrameResources(ref bd.FrameResources[i]);
		}

		bd.FrameIndex = uint.MaxValue;
	}

	public static void RenderDrawData(ImDrawDataPtr drawData, RenderPassEncoder passEncoder)
	{
		if (drawData.DisplaySize.X <= 0.0f || drawData.DisplaySize.Y <= 0.0f)
		{
			return;
		}

		var bd = GetBackendData();
		if (bd == null)
		{
			throw new InvalidOperationException("Renderer backend is not initialized.");
		}

		if (bd.PipelineState == null)
		{
			if (!CreateDeviceObjects())
			{
				throw new InvalidOperationException("Failed to create ImGui WebGPU device objects.");
			}
		}

		bd.FrameIndex = unchecked(bd.FrameIndex + 1);
		ref FrameResources fr = ref bd.FrameResources[bd.FrameIndex % (uint)bd.NumFramesInFlight];

		if (fr.VertexBuffer == null || fr.VertexBufferSize < drawData.TotalVtxCount)
		{
			fr.VertexBuffer?.Destroy();
			SafeRelease(ref fr.VertexBufferHost);
			fr.VertexBufferSize = drawData.TotalVtxCount + 5000;
			fr.VertexBuffer = bd.Device.CreateBuffer(new()
			{
				Label = "Dear ImGui Vertex buffer",
				Usage = BufferUsage.CopyDst | BufferUsage.Vertex,
				Size = Align((ulong)fr.VertexBufferSize * (ulong)Unsafe.SizeOf<ImDrawVert>(), 4),
				MappedAtCreation = false,
			});
			fr.VertexBufferHost = (ImDrawVert*)NativeMemory.Alloc((nuint)fr.VertexBufferSize, (nuint)Unsafe.SizeOf<ImDrawVert>());
		}

		if (fr.IndexBuffer == null || fr.IndexBufferSize < drawData.TotalIdxCount)
		{
			fr.IndexBuffer?.Destroy();
			SafeRelease(ref fr.IndexBufferHost);
			fr.IndexBufferSize = drawData.TotalIdxCount + 10000;
			fr.IndexBuffer = bd.Device.CreateBuffer(new()
			{
				Label = "Dear ImGui Index buffer",
				Usage = BufferUsage.CopyDst | BufferUsage.Index,
				Size = Align((ulong)fr.IndexBufferSize * sizeof(ushort), 4),
				MappedAtCreation = false,
			});
			fr.IndexBufferHost = (ushort*)NativeMemory.Alloc((nuint)fr.IndexBufferSize, sizeof(ushort));
		}

		ImDrawVert* vtxDst = fr.VertexBufferHost;
		ushort* idxDst = fr.IndexBufferHost;

		for (int n = 0; n < drawData.CmdListsCount; n++)
		{
			ImDrawListPtr cmdList = drawData.CmdLists[n];
			int vertexBytes = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
			int indexBytes = cmdList.IdxBuffer.Size * sizeof(ushort);

			global::System.Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDst, vertexBytes, vertexBytes);
			global::System.Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDst, indexBytes, indexBytes);

			vtxDst += cmdList.VtxBuffer.Size;
			idxDst += cmdList.IdxBuffer.Size;
		}

		bd.DefaultQueue.WriteBuffer(fr.VertexBuffer!, 0, new ReadOnlySpan<ImDrawVert>(fr.VertexBufferHost, drawData.TotalVtxCount));
		bd.DefaultQueue.WriteBuffer(fr.IndexBuffer!, 0, new ReadOnlySpan<ushort>(fr.IndexBufferHost, drawData.TotalIdxCount));

		SetupRenderState(drawData, GetHandle(passEncoder), ref fr);

		int globalVtxOffset = 0;
		int globalIdxOffset = 0;
		var clipScale = drawData.FramebufferScale;
		var clipOff = drawData.DisplayPos;
		RenderPassEncoderHandle encoder = GetHandle(passEncoder);

		for (int n = 0; n < drawData.CmdListsCount; n++)
		{
			ImDrawListPtr cmdList = drawData.CmdLists[n];
			for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
			{
				ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdIndex];
				if (pcmd.UserCallback != IntPtr.Zero)
				{
					if (pcmd.UserCallback == s_resetRenderStateCallback)
					{
						SetupRenderState(drawData, encoder, ref fr);
					}
					else
					{
						var callback = (delegate* unmanaged[Cdecl]<ImDrawList*, ImDrawCmd*, void>)pcmd.UserCallback;
						callback(cmdList.NativePtr, pcmd.NativePtr);
					}
				}
				else
				{
					IntPtr textureId = pcmd.GetTexID();
					BindGroup bindGroup = GetOrCreateImageBindGroup(textureId);
					encoder.SetBindGroup(1, bindGroup);

					var clipRect = pcmd.ClipRect;
					float clipMinX = (clipRect.X - clipOff.X) * clipScale.X;
					float clipMinY = (clipRect.Y - clipOff.Y) * clipScale.Y;
					float clipMaxX = (clipRect.Z - clipOff.X) * clipScale.X;
					float clipMaxY = (clipRect.W - clipOff.Y) * clipScale.Y;
					if (clipMaxX <= clipMinX || clipMaxY <= clipMinY)
					{
						continue;
					}

					encoder.SetScissorRect(
						(uint)clipMinX,
						(uint)clipMinY,
						(uint)(clipMaxX - clipMinX),
						(uint)(clipMaxY - clipMinY));
					encoder.DrawIndexed(
						pcmd.ElemCount,
						1,
						pcmd.IdxOffset + (uint)globalIdxOffset,
						unchecked((int)pcmd.VtxOffset + globalVtxOffset),
						0);
				}
			}

			globalIdxOffset += cmdList.IdxBuffer.Size;
			globalVtxOffset += cmdList.VtxBuffer.Size;
		}
	}

	public static IntPtr GetImGuiTextureID(TextureView textureView)
	{
		return unchecked((IntPtr)(nint)GetHandle(textureView).GetAddress());
	}

	private static ImGui_ImplWGPU_Data? GetBackendData()
	{
		if (ImGui.GetCurrentContext() == IntPtr.Zero)
		{
			return null;
		}

		IntPtr userData = ImGui.GetIO().BackendRendererUserData;
		if (userData == IntPtr.Zero)
		{
			return null;
		}

		GCHandle handle = GCHandle.FromIntPtr(userData);
		return handle.Target as ImGui_ImplWGPU_Data;
	}

	private static void SetupRenderState(ImDrawDataPtr drawData, RenderPassEncoderHandle encoder, ref FrameResources fr)
	{
		var bd = GetBackendData()!;

		Uniforms uniforms = default;
		float left = drawData.DisplayPos.X;
		float right = drawData.DisplayPos.X + drawData.DisplaySize.X;
		float top = drawData.DisplayPos.Y;
		float bottom = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

		float* mvp = uniforms.MVP;
		mvp[0] = 2.0f / (right - left);
		mvp[1] = 0.0f;
		mvp[2] = 0.0f;
		mvp[3] = 0.0f;

		mvp[4] = 0.0f;
		mvp[5] = 2.0f / (top - bottom);
		mvp[6] = 0.0f;
		mvp[7] = 0.0f;

		mvp[8] = 0.0f;
		mvp[9] = 0.0f;
		mvp[10] = 0.5f;
		mvp[11] = 0.0f;

		mvp[12] = (right + left) / (left - right);
		mvp[13] = (top + bottom) / (bottom - top);
		mvp[14] = 0.5f;
		mvp[15] = 1.0f;

		uniforms.Gamma = GetGamma(bd.RenderTargetFormat);
		bd.DefaultQueue.WriteBuffer(bd.RenderResources.Uniforms!, 0, in uniforms);

		encoder.SetViewport(
			0,
			0,
			drawData.FramebufferScale.X * drawData.DisplaySize.X,
			drawData.FramebufferScale.Y * drawData.DisplaySize.Y,
			0,
			1);

		encoder.SetVertexBuffer(0, fr.VertexBuffer!, 0, (ulong)fr.VertexBufferSize * (ulong)Unsafe.SizeOf<ImDrawVert>());
		encoder.SetIndexBuffer(fr.IndexBuffer!, IndexFormat.Uint16, 0, (ulong)fr.IndexBufferSize * sizeof(ushort));
		encoder.SetPipeline(bd.PipelineState!);
		encoder.SetBindGroup(0, bd.RenderResources.CommonBindGroup!);
		encoder.SetBlendConstant(new Color(0, 0, 0, 0));
	}

	private static void CreateFontsTexture()
	{
		var bd = GetBackendData()!;
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

		bd.RenderResources.FontTexture = bd.Device.CreateTexture(new()
		{
			Label = "Dear ImGui Font Texture",
			Dimension = TextureDimension.D2,
			Size = new((uint)width, (uint)height, 1),
			SampleCount = 1,
			Format = TextureFormat.RGBA8Unorm,
			MipLevelCount = 1,
			Usage = TextureUsage.CopyDst | TextureUsage.TextureBinding,
		});

		bd.RenderResources.FontTextureView = bd.RenderResources.FontTexture.CreateView(new()
		{
			Format = TextureFormat.RGBA8Unorm,
			Dimension = TextureViewDimension.D2,
			BaseMipLevel = 0,
			MipLevelCount = 1,
			BaseArrayLayer = 0,
			ArrayLayerCount = 1,
			Aspect = TextureAspect.All,
		});

		bd.DefaultQueue.WriteTexture(
			destination: new TexelCopyTextureInfo
			{
				Texture = bd.RenderResources.FontTexture,
				MipLevel = 0,
				Origin = new(0, 0, 0),
				Aspect = TextureAspect.All,
			},
			data: new ReadOnlySpan<byte>(pixels, width * height * bytesPerPixel),
			dataLayout: new TexelCopyBufferLayout
			{
				Offset = 0,
				BytesPerRow = (uint)(width * bytesPerPixel),
				RowsPerImage = (uint)height,
			},
			writeSize: new((uint)width, (uint)height, 1));

		SamplerDescriptor samplerDesc = new()
		{
			MinFilter = FilterMode.Linear,
			MagFilter = FilterMode.Linear,
			MipmapFilter = MipmapFilterMode.Linear,
			AddressModeU = AddressMode.Repeat,
			AddressModeV = AddressMode.Repeat,
			AddressModeW = AddressMode.Repeat,
			MaxAnisotropy = 1,
		};
		bd.RenderResources.Sampler = bd.Device.CreateSampler(samplerDesc);

		io.Fonts.SetTexID(GetImGuiTextureID(bd.RenderResources.FontTextureView));
	}

	private static void CreateUniformBuffer()
	{
		var bd = GetBackendData()!;
		bd.RenderResources.Uniforms = bd.Device.CreateBuffer(new()
		{
			Label = "Dear ImGui Uniform buffer",
			Usage = BufferUsage.CopyDst | BufferUsage.Uniform,
			Size = Align((ulong)Unsafe.SizeOf<Uniforms>(), 16),
			MappedAtCreation = false,
		});
	}

	private static BindGroup CreateImageBindGroup(BindGroupLayout layout, TextureView textureView)
	{
		var bd = GetBackendData()!;
		return bd.Device.CreateBindGroup(new()
		{
			Layout = layout,
			Entries =
			[
				new()
				{
					Binding = 0,
					TextureView = textureView,
				},
			],
		});
	}

	private static BindGroup GetOrCreateImageBindGroup(IntPtr textureId)
	{
		var bd = GetBackendData()!;
		if (textureId == IntPtr.Zero)
		{
			return bd.RenderResources.ImageBindGroup!;
		}

		uint key = GetTextureHash(textureId);
		if (bd.RenderResources.ImageBindGroups!.TryGetValue(key, out ImageBindGroupEntry cacheEntry))
		{
			return cacheEntry.BindGroup!;
		}

		TextureViewHandle textureViewHandle = TextureViewHandle.UnsafeFromPointer(unchecked((nuint)textureId.ToPointer()));
		TextureView textureView = textureViewHandle.ToSafeHandle() ?? throw new InvalidOperationException("Invalid texture view handle in ImTextureID.");
		BindGroup bindGroup = CreateImageBindGroup(bd.RenderResources.ImageBindGroupLayout!, textureView);
		bd.RenderResources.ImageBindGroups[key] = new ImageBindGroupEntry
		{
			TextureView = textureView,
			BindGroup = bindGroup,
		};
		return bindGroup;
	}

	private static void SafeRelease(ref RenderResources res)
	{
		if (res.FontTexture != null)
		{
			res.FontTexture.Destroy();
		}

		res.FontTexture = null;
		res.FontTextureView = null;
		res.Sampler = null;
		res.Uniforms = null;
		res.CommonBindGroup = null;
		res.ImageBindGroup = null;
		res.ImageBindGroupLayout = null;
		res.ImageBindGroups?.Clear();
	}

	private static void ResetFrameResources(ref FrameResources res)
	{
		res.IndexBuffer = null;
		res.VertexBuffer = null;
		res.IndexBufferHost = null;
		res.VertexBufferHost = null;
		res.IndexBufferSize = 10000;
		res.VertexBufferSize = 5000;
	}

	private static void SafeRelease(ref FrameResources res)
	{
		if (res.IndexBuffer != null)
		{
			res.IndexBuffer.Destroy();
		}

		if (res.VertexBuffer != null)
		{
			res.VertexBuffer.Destroy();
		}

		res.IndexBuffer = null;
		res.VertexBuffer = null;
		SafeRelease(ref res.IndexBufferHost);
		SafeRelease(ref res.VertexBufferHost);
		res.IndexBufferSize = 0;
		res.VertexBufferSize = 0;
	}

	private static void SafeRelease(ref ushort* ptr)
	{
		if (ptr != null)
		{
			NativeMemory.Free(ptr);
			ptr = null;
		}
	}

	private static void SafeRelease(ref ImDrawVert* ptr)
	{
		if (ptr != null)
		{
			NativeMemory.Free(ptr);
			ptr = null;
		}
	}

	private static ulong Align(ulong size, ulong align)
	{
		return (size + (align - 1)) & ~(align - 1);
	}

	private static float GetGamma(TextureFormat format)
	{
		return format.ToString().EndsWith("Srgb", StringComparison.Ordinal) ? 2.2f : 1.0f;
	}

	private static uint GetTextureHash(IntPtr textureId)
	{
		nint textureValue = textureId;
		return ImHashData(&textureValue, (nuint)sizeof(nint));
	}

	private static uint ImHashData(void* data, nuint dataSize, uint seed = 0)
	{
		uint crc = ~seed;
		byte* bytes = (byte*)data;

		for (nuint i = 0; i < dataSize; i++)
		{
			crc = (crc >> 8) ^ s_crc32LookupTable[(int)((crc & 0xFF) ^ bytes[i])];
		}

		return ~crc;
	}
}
