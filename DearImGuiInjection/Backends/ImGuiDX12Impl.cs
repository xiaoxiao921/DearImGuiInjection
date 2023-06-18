using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using DearImGuiInjection.Windows;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D12.Device;
using FenceFlags = SharpDX.Direct3D12.FenceFlags;
using Resource = SharpDX.Direct3D12.Resource;
using ResourceDimension = SharpDX.Direct3D12.ResourceDimension;
using ShaderResourceViewDescription = SharpDX.Direct3D12.ShaderResourceViewDescription;
using TextureLayout = SharpDX.Direct3D12.TextureLayout;

namespace DearImGuiInjection.Backends;

public static class ImGuiDX12Impl
{
    public class RenderBuffer
    {
        public Resource IndexBuffer;
        public Resource VertexBuffer;
        public int IndexBufferSize;
        public int VertexBufferSize;
    }

    private static Device _device;
    private static RootSignature pRootSignature;
    private static PipelineState pPipelineState;
    private static Format RTVFormat;
    private static Resource pFontTextureResource;
    private static CpuDescriptorHandle hFontSrvCpuDescHandle;
    private static GpuDescriptorHandle hFontSrvGpuDescHandle;
    private static DescriptorHeap pd3dSrvDescHeap;
    private static uint numFramesInFlight;
    private static RenderBuffer[] pFrameResources;
    private static uint frameIndex = uint.MaxValue;

    private static RawVector4 _blendFactor = new(0, 0, 0, 0);

    private static ShaderBytecode _vertexShader;
    private static InputLayoutDescription _inputLayout;
    private static ShaderBytecode _pixelShader;
    private static IntPtr _renderNamePtr;

    public unsafe struct VERTEX_CONSTANT_BUFFER_DX12
    {
        public fixed float mvp[4 * 4];
    }

    // Functions
    static unsafe void ImGui_ImplDX12_SetupRenderState(ImDrawData* draw_data, GraphicsCommandList ctx, RenderBuffer fr)
    {
        VERTEX_CONSTANT_BUFFER_DX12 constant_buffer;
        float L = draw_data->DisplayPos.X;
        float R = draw_data->DisplayPos.X + draw_data->DisplaySize.X;
        float T = draw_data->DisplayPos.Y;
        float B = draw_data->DisplayPos.Y + draw_data->DisplaySize.Y;

        constant_buffer.mvp[0] = 2.0f / (R - L);
        constant_buffer.mvp[1] = 0.0f;
        constant_buffer.mvp[2] = 0.0f;
        constant_buffer.mvp[3] = 0.0f;

        constant_buffer.mvp[4] = 0.0f;
        constant_buffer.mvp[5] = 2.0f / (T - B);
        constant_buffer.mvp[6] = 0.0f;
        constant_buffer.mvp[7] = 0.0f;

        constant_buffer.mvp[8] = 0.0f;
        constant_buffer.mvp[9] = 0.0f;
        constant_buffer.mvp[10] = 0.5f;
        constant_buffer.mvp[11] = 0.0f;

        constant_buffer.mvp[12] = (R + L) / (L - R);
        constant_buffer.mvp[13] = (T + B) / (B - T);
        constant_buffer.mvp[14] = 0.5f;
        constant_buffer.mvp[15] = 1.0f;

        ctx.SetViewport(new RawViewportF()
        {
            Width = draw_data->DisplaySize.X,
            Height = draw_data->DisplaySize.Y,
            MinDepth = 0.0f,
            MaxDepth = 1.0f,
            X = 0.0f,
            Y = 0.0f
        });

        // Bind shader and vertex buffers
        uint stride = (uint)sizeof(ImDrawVert);
        uint offset = 0;
        VertexBufferView vbv;
        vbv.BufferLocation = fr.VertexBuffer.GPUVirtualAddress + offset;
        vbv.SizeInBytes = (int)(fr.VertexBufferSize * stride);
        vbv.StrideInBytes = (int)stride;
        ctx.SetVertexBuffer(0, vbv);
        IndexBufferView ibv;
        ibv.BufferLocation = fr.IndexBuffer.GPUVirtualAddress;
        ibv.SizeInBytes = fr.IndexBufferSize * sizeof(ushort); // sizeof(ImDrawIdx)
        ibv.Format = sizeof(ushort) == 2 ? Format.R16_UInt : Format.R32_UInt; // sizeof(ImDrawIdx)
        ctx.SetIndexBuffer(ibv);
        ctx.PrimitiveTopology = PrimitiveTopology.TriangleList;
        ctx.PipelineState = pPipelineState;
        ctx.SetGraphicsRootSignature(pRootSignature);
        ctx.SetGraphicsRoot32BitConstants(0, 16, (IntPtr)(&constant_buffer), 0);

        // Setup blend factor
        ctx.BlendFactor = _blendFactor;
    }

    private unsafe delegate void ImDrawUserCallBack(ImDrawList* a, ImDrawCmd* b);

    // Render function
    public static unsafe void RenderDrawData(ImDrawData* draw_data, GraphicsCommandList ctx)
    {
        // Avoid rendering when minimized
        if (draw_data->DisplaySize.X <= 0.0f || draw_data->DisplaySize.Y <= 0.0f)
            return;

        // FIXME: I'm assuming that this only gets called once per frame!
        // If not, we can't just re-allocate the IB or VB, we'll have to do a proper allocator.
        frameIndex = frameIndex + 1;
        var fr = pFrameResources[frameIndex % numFramesInFlight];

        // Create and grow vertex/index buffers if needed
        if (fr.VertexBuffer == null || fr.VertexBufferSize < draw_data->TotalVtxCount)
        {
            fr.VertexBuffer?.Dispose();
            fr.VertexBuffer = null;
            fr.VertexBufferSize = draw_data->TotalVtxCount + 5000;

            HeapProperties props = new();
            props.Type = HeapType.Upload;
            props.CPUPageProperty = CpuPageProperty.Unknown;
            props.MemoryPoolPreference = MemoryPool.Unknown;
            ResourceDescription desc = new();
            desc.Dimension = ResourceDimension.Buffer;
            desc.Width = fr.VertexBufferSize * sizeof(ImDrawVert);
            desc.Height = 1;
            desc.DepthOrArraySize = 1;
            desc.MipLevels = 1;
            desc.Format = Format.Unknown;
            desc.SampleDescription.Count = 1;
            desc.Layout = TextureLayout.RowMajor;
            desc.Flags = ResourceFlags.None;

            fr.VertexBuffer = _device.CreateCommittedResource(props, HeapFlags.None, desc, ResourceStates.GenericRead);
        }

        if (fr.IndexBuffer == null || fr.IndexBufferSize < draw_data->TotalIdxCount)
        {
            fr.IndexBuffer?.Dispose(); fr.IndexBuffer = null;
            fr.IndexBufferSize = draw_data->TotalIdxCount + 10000;
            HeapProperties props = new();
            props.Type = HeapType.Upload;
            props.CPUPageProperty = CpuPageProperty.Unknown;
            props.MemoryPoolPreference = MemoryPool.Unknown;
            ResourceDescription desc = new();
            desc.Dimension = ResourceDimension.Buffer;
            desc.Width = fr.IndexBufferSize * sizeof(ushort); //sizeof(ImDrawIdx)
            desc.Height = 1;
            desc.DepthOrArraySize = 1;
            desc.MipLevels = 1;
            desc.Format = Format.Unknown;
            desc.SampleDescription.Count = 1;
            desc.Layout = TextureLayout.RowMajor;
            desc.Flags = ResourceFlags.None;

            fr.IndexBuffer = _device.CreateCommittedResource(props, HeapFlags.None, desc, ResourceStates.GenericRead);
        }

        // Upload vertex/index data into a single contiguous GPU buffer
        SharpDX.Direct3D12.Range range = new();

        var vtx_resource = fr.VertexBuffer.Map(0, range);
        var idx_resource = fr.IndexBuffer.Map(0, range);

        ImDrawVert* vtx_dst = (ImDrawVert*)vtx_resource;
        ushort* idx_dst = (ushort*)idx_resource;
        for (int n = 0; n < draw_data->CmdListsCount; n++)
        {
            ImDrawList* cmd_list = draw_data->CmdLists[n];

            var len = cmd_list->VtxBuffer.Size * sizeof(ImDrawVert);
            System.Buffer.MemoryCopy((void*)cmd_list->VtxBuffer.Data, vtx_dst, len, len);

            len = cmd_list->IdxBuffer.Size * sizeof(ushort);
            System.Buffer.MemoryCopy((void*)cmd_list->IdxBuffer.Data, idx_dst, len, len);

            vtx_dst += cmd_list->VtxBuffer.Size;
            idx_dst += cmd_list->IdxBuffer.Size;
        }
        fr.VertexBuffer.Unmap(0, range);
        fr.IndexBuffer.Unmap(0, range);

        // Setup desired DX state
        ImGui_ImplDX12_SetupRenderState(draw_data, ctx, fr);

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_vtx_offset = 0;
        int global_idx_offset = 0;
        Vector2 clip_off = draw_data->DisplayPos;
        for (int n = 0; n < draw_data->CmdListsCount; n++)
        {
            ImDrawList* cmd_list = draw_data->CmdLists[n];
            for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
            {
                var pcmd = cmd_list->CmdBuffer.Ref<ImDrawCmd>(cmd_i);
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    var userCallback = Marshal.GetDelegateForFunctionPointer<ImDrawUserCallBack>(pcmd.UserCallback);

                    // User callback, registered via ImDrawList::AddCallback()
                    // (ImDrawCallback_ResetRenderState is a special callback value used by the user to request the renderer to reset render state.)
                    if (pcmd.UserCallback == new IntPtr(-1))
                        ImGui_ImplDX12_SetupRenderState(draw_data, ctx, fr);
                    else
                        userCallback(cmd_list, &pcmd);
                }
                else
                {
                    // Project scissor/clipping rectangles into framebuffer space
                    Vector2 clip_min = new(pcmd.ClipRect.X - clip_off.X, pcmd.ClipRect.Y - clip_off.Y);
                    Vector2 clip_max = new(pcmd.ClipRect.Z - clip_off.X, pcmd.ClipRect.W - clip_off.Y);
                    if (clip_max.X <= clip_min.X || clip_max.Y <= clip_min.Y)
                        continue;

                    // Apply Scissor/clipping rectangle, Bind texture, Draw
                    RawRectangle r = new((int)clip_min.X, (int)clip_min.Y, (int)clip_max.X, (int)clip_max.Y);
                    GpuDescriptorHandle texture_handle = new();
                    texture_handle.Ptr = (long)pcmd.TextureId;

                    ctx.SetGraphicsRootDescriptorTable(1, texture_handle);

                    ctx.SetScissorRectangles(r);

                    ctx.DrawIndexedInstanced((int)pcmd.ElemCount, 1, (int)(pcmd.IdxOffset + global_idx_offset), (int)(pcmd.VtxOffset + global_vtx_offset), 0);
                }
            }
            global_idx_offset += cmd_list->IdxBuffer.Size;
            global_vtx_offset += cmd_list->VtxBuffer.Size;
        }
    }

    public static unsafe void CreateFontsTexture()
    {
        // Build texture atlas
        var io = ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height);

        // Upload texture to graphics system
        {
            HeapProperties props = new();
            props.Type = HeapType.Default;
            props.CPUPageProperty = CpuPageProperty.Unknown;
            props.MemoryPoolPreference = MemoryPool.Unknown;

            ResourceDescription desc = new();
            desc.Dimension = ResourceDimension.Texture2D;
            desc.Alignment = 0;
            desc.Width = width;
            desc.Height = height;
            desc.DepthOrArraySize = 1;
            desc.MipLevels = 1;
            desc.Format = Format.R8G8B8A8_UNorm;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.Layout = TextureLayout.Unknown;
            desc.Flags = ResourceFlags.None;

            var pTexture = _device.CreateCommittedResource(props, HeapFlags.None, desc, ResourceStates.CopyDestination);

            const int D3D12_TEXTURE_DATA_PITCH_ALIGNMENT = 256;

            uint uploadPitch = (uint)((width * 4 + D3D12_TEXTURE_DATA_PITCH_ALIGNMENT - 1u) & ~(D3D12_TEXTURE_DATA_PITCH_ALIGNMENT - 1u));
            uint uploadSize = (uint)(height * uploadPitch);
            desc.Dimension = ResourceDimension.Buffer;
            desc.Alignment = 0;
            desc.Width = uploadSize;
            desc.Height = 1;
            desc.DepthOrArraySize = 1;
            desc.MipLevels = 1;
            desc.Format = Format.Unknown;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.Layout = TextureLayout.RowMajor;
            desc.Flags = ResourceFlags.None;

            props.Type = HeapType.Upload;
            props.CPUPageProperty = CpuPageProperty.Unknown;
            props.MemoryPoolPreference = MemoryPool.Unknown;

            var uploadBuffer = _device.CreateCommittedResource(props, HeapFlags.None, desc, ResourceStates.GenericRead);

            SharpDX.Direct3D12.Range range = new()
            {
                Begin = 0,
                End = uploadSize
            };

            var mapped = (nuint)(nint)uploadBuffer.Map(0, range);

            nuint nuintHeight = (nuint)height;
            nuint nuintWidth = (nuint)width;
            for (nuint y = 0; y < nuintHeight; y++)
            {
                var dst = mapped + y * uploadPitch;
                var src = pixels + y * nuintWidth * 4;
                var len = width * 4;

                System.Buffer.MemoryCopy(src, (void*)dst, len, len);
            }

            uploadBuffer.Unmap(0, range);

            TextureCopyLocation srcLocation = new(uploadBuffer, new PlacedSubResourceFootprint()
            {
                Footprint = new()
                {
                    Format = Format.R8G8B8A8_UNorm,
                    Width = width,
                    Height = height,
                    Depth = 1,
                    RowPitch = (int)uploadPitch
                },
            })
            {
                Type = TextureCopyType.PlacedFootprint
            };

            TextureCopyLocation dstLocation = new(pTexture, 0)
            {
                Type = TextureCopyType.SubResourceIndex
            };

            const int D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES = unchecked((int)0xffffffff);
            ResourceBarrier barrier = new();
            barrier.Type = ResourceBarrierType.Transition;
            barrier.Flags = ResourceBarrierFlags.None;
            barrier.Transition = new ResourceTransitionBarrier(pTexture, ResourceStates.CopyDestination, ResourceStates.PixelShaderResource)
            {
                Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES
            };

            var fence = _device.CreateFence(0, FenceFlags.None);

            var @event = Kernel32.CreateEvent(IntPtr.Zero, false, false, null);
            if (@event == IntPtr.Zero)
            {
                Log.Error("Kernel32.CreateEvent failed.");
            }

            CommandQueueDescription queueDesc = new();
            queueDesc.Type = CommandListType.Direct;
            queueDesc.Flags = CommandQueueFlags.None;
            queueDesc.NodeMask = 1;

            var cmdQueue = _device.CreateCommandQueue(queueDesc);

            var cmdAlloc = _device.CreateCommandAllocator(CommandListType.Direct);

            var cmdList = _device.CreateCommandList(CommandListType.Direct, cmdAlloc, null);

            cmdList.CopyTextureRegion(dstLocation, 0, 0, 0, srcLocation, null);

            cmdList.ResourceBarrier(barrier);

            cmdList.Close();

            cmdQueue.ExecuteCommandList(cmdList);

            cmdQueue.Signal(fence, 1);

            fence.SetEventOnCompletion(1, @event);


            Kernel32.WaitForSingleObject(@event, Kernel32.INFINITE);

            cmdList?.Dispose();

            cmdAlloc?.Dispose();

            cmdQueue?.Dispose();

            Kernel32.CloseHandle(@event);

            fence?.Dispose();

            uploadBuffer?.Dispose();


            const int D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING = 0x1688;
            ShaderResourceViewDescription srvDesc = new();
            srvDesc.Format = Format.R8G8B8A8_UNorm;
            srvDesc.Dimension = SharpDX.Direct3D12.ShaderResourceViewDimension.Texture2D;
            srvDesc.Texture2D.MipLevels = desc.MipLevels;
            srvDesc.Texture2D.MostDetailedMip = 0;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
            _device.CreateShaderResourceView(pTexture, srvDesc, hFontSrvCpuDescHandle);
            pFontTextureResource?.Dispose();
            pFontTextureResource = pTexture;
        }

        // Store our identifier
        // READ THIS IF THE STATIC_ASSERT() TRIGGERS:
        // - Important: to compile on 32-bit systems, this backend requires code to be compiled with '#define ImTextureID ImU64'.
        // - This is because we need ImTextureID to carry a 64-bit value and by default ImTextureID is defined as void*.
        // [Solution 1] IDE/msbuild: in "Properties/C++/Preprocessor Definitions" add 'ImTextureID=ImU64' (this is what we do in the 'example_win32_direct12/example_win32_direct12.vcxproj' project file)
        // [Solution 2] IDE/msbuild: in "Properties/C++/Preprocessor Definitions" add 'IMGUI_USER_CONFIG="my_imgui_config.h"' and inside 'my_imgui_config.h' add '#define ImTextureID ImU64' and as many other options as you like.
        // [Solution 3] IDE/msbuild: edit imconfig.h and add '#define ImTextureID ImU64' (prefer solution 2 to create your own config file!)
        // [Solution 4] command-line: add '/D ImTextureID=ImU64' to your cl.exe command-line (this is what we do in the example_win32_direct12/build_win32.bat file)
        //static_assert(sizeof(ImTextureID) >= sizeof(bd->hFontSrvGpuDescHandle.ptr), "Can't pack descriptor handle into TexID, 32-bit not supported yet.");

        io.Fonts.SetTexID((nint)hFontSrvGpuDescHandle.Ptr);
    }

    public static unsafe bool CreateDeviceObjects()
    {
        if (_device == null)
            return false;

        if (pPipelineState != null)
            InvalidateDeviceObjects();

        RootParameter[] param = new RootParameter[2];

        var param0 = new RootParameter(ShaderVisibility.Vertex, new RootConstants()
        {
            ShaderRegister = 0,
            RegisterSpace = 0,
            Value32BitCount = 16
        });
        param[0] = param0;

        DescriptorRange descRange = new();
        descRange.RangeType = DescriptorRangeType.ShaderResourceView;
        descRange.DescriptorCount = 1;
        descRange.BaseShaderRegister = 0;
        descRange.RegisterSpace = 0;
        descRange.OffsetInDescriptorsFromTableStart = 0;
        var param1 = new RootParameter(ShaderVisibility.Pixel, descRange);
        param[1] = param1;

        // Bilinear sampling is required by default. Set 'io.Fonts->Flags |= ImFontAtlasFlags_NoBakedLines' or 'style.AntiAliasedLinesUseTex = false' to allow point/nearest sampling.
        StaticSamplerDescription staticSampler = new();
        staticSampler.Filter = SharpDX.Direct3D12.Filter.ComparisonMinMagMipLinear;
        staticSampler.AddressU = SharpDX.Direct3D12.TextureAddressMode.Wrap;
        staticSampler.AddressV = SharpDX.Direct3D12.TextureAddressMode.Wrap;
        staticSampler.AddressW = SharpDX.Direct3D12.TextureAddressMode.Wrap;
        staticSampler.MipLODBias = 0.0f;
        staticSampler.MaxAnisotropy = 0;
        staticSampler.ComparisonFunc = SharpDX.Direct3D12.Comparison.Always;
        staticSampler.BorderColor = StaticBorderColor.TransparentBlack;
        staticSampler.MinLOD = 0.0f;
        staticSampler.MaxLOD = 0.0f;
        staticSampler.ShaderRegister = 0;
        staticSampler.RegisterSpace = 0;
        staticSampler.ShaderVisibility = ShaderVisibility.Pixel;

        RootSignatureDescription rootSignatureDescription = new(
            RootSignatureFlags.AllowInputAssemblerInputLayout |
            RootSignatureFlags.DenyHullShaderRootAccess |
            RootSignatureFlags.DenyDomainShaderRootAccess |
            RootSignatureFlags.DenyGeometryShaderRootAccess,
            param,
            new[] { staticSampler }
        );
        var blob = rootSignatureDescription.Serialize();
        pRootSignature = _device.CreateRootSignature(new DataPointer(blob.BufferPointer, blob.BufferSize));
        if (pRootSignature == null)
        {
            Log.Error("pRootSignature == null");
        }
        blob?.Dispose();

        GraphicsPipelineStateDescription psoDesc = new();
        psoDesc.NodeMask = 1;
        psoDesc.PrimitiveTopologyType = PrimitiveTopologyType.Triangle;
        psoDesc.RootSignature = pRootSignature;
        psoDesc.SampleMask = unchecked((int)uint.MaxValue);
        psoDesc.RenderTargetCount = 1;
        psoDesc.RenderTargetFormats[0] = RTVFormat;
        psoDesc.SampleDescription.Count = 1;
        psoDesc.Flags = PipelineStateFlags.None;

        var shadersFolder = Path.Combine(DearImGuiInjection.AssetsFolderPath, "Shaders");

        var vertexShaderPath = Path.Combine(shadersFolder, "imgui-vertex.hlsl.bytes");
        byte[] vertexShaderBytes = File.ReadAllBytes(vertexShaderPath);
        _vertexShader = new ShaderBytecode(vertexShaderBytes);

        // Create the input layout
        _inputLayout = new InputLayoutDescription();
        _inputLayout.Elements = new InputElement[]
        {
            new InputElement("POSITION", 0, Format.R32G32_Float, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0),
            new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 0)
        };

        psoDesc.VertexShader = _vertexShader;
        psoDesc.InputLayout = _inputLayout;

        var pixelShaderPath = Path.Combine(shadersFolder, "imgui-frag.hlsl.bytes");
        byte[] pixelShaderBytes = File.ReadAllBytes(pixelShaderPath);
        _pixelShader = new ShaderBytecode(pixelShaderBytes);
        psoDesc.PixelShader = _pixelShader;

        // Create the blending setup
        {
            BlendStateDescription blendStateDescription = psoDesc.BlendState;

            blendStateDescription.AlphaToCoverageEnable = false;
            blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
            blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            psoDesc.BlendState = blendStateDescription;
        }

        // Create the rasterizer state
        {
            const int D3D12_DEFAULT_DEPTH_BIAS = 0;
            const float D3D12_DEFAULT_DEPTH_BIAS_CLAMP = 0.0f;
            const float D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS = 0.0f;

            RasterizerStateDescription rasterizerStateDescription = psoDesc.RasterizerState;

            rasterizerStateDescription.FillMode = FillMode.Solid;
            rasterizerStateDescription.CullMode = CullMode.None;
            rasterizerStateDescription.IsFrontCounterClockwise = false;
            rasterizerStateDescription.DepthBias = D3D12_DEFAULT_DEPTH_BIAS;
            rasterizerStateDescription.DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP;
            rasterizerStateDescription.SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
            rasterizerStateDescription.IsDepthClipEnabled = true;
            rasterizerStateDescription.IsMultisampleEnabled = false;
            rasterizerStateDescription.IsAntialiasedLineEnabled = false;
            rasterizerStateDescription.ForcedSampleCount = 0;
            rasterizerStateDescription.ConservativeRaster = ConservativeRasterizationMode.Off;

            psoDesc.RasterizerState = rasterizerStateDescription;
        }

        // Create depth-stencil State
        {
            var depthStencilState = psoDesc.DepthStencilState;

            depthStencilState.IsDepthEnabled = false;
            depthStencilState.DepthWriteMask = DepthWriteMask.All;
            depthStencilState.DepthComparison = Comparison.Always;
            depthStencilState.IsStencilEnabled = false;
            depthStencilState.FrontFace.FailOperation = depthStencilState.FrontFace.FailOperation = depthStencilState.FrontFace.PassOperation = StencilOperation.Keep;
            depthStencilState.FrontFace.Comparison = Comparison.Always;
            depthStencilState.BackFace = depthStencilState.FrontFace;

            psoDesc.DepthStencilState = depthStencilState;
        }

        // needed because sharpdx throw some stupid error otherwise
        psoDesc.StreamOutput = new();

        pPipelineState = _device.CreateGraphicsPipelineState(psoDesc);

        CreateFontsTexture();

        return true;
    }

    public static void InvalidateDeviceObjects()
    {
        if (_device == null)
        {
            return;
        }

        var io = ImGui.GetIO();

        pRootSignature?.Dispose();
        pRootSignature = null;

        pPipelineState?.Dispose();
        pPipelineState = null;

        pFontTextureResource?.Dispose();
        pFontTextureResource = null;

        io.Fonts.SetTexID(IntPtr.Zero); // We copied bd->pFontTextureView to io.Fonts->TexID so let's clear that as well.

        for (uint i = 0; i < numFramesInFlight; i++)
        {
            var fr = pFrameResources[i];

            fr.IndexBuffer?.Dispose();
            fr.IndexBuffer = null;

            fr.VertexBuffer?.Dispose();
            fr.VertexBuffer = null;
        }
        pFrameResources = Array.Empty<RenderBuffer>();
    }

    internal static unsafe void Init(void* device, int num_frames_in_flight, Format rtv_format, void* cbv_srv_heap,
        CpuDescriptorHandle font_srv_cpu_desc_handle, GpuDescriptorHandle font_srv_gpu_desc_handle)
    {
        if (_device != null)
        {
            Log.Error("Already initialized a renderer backend!");
        }

        // Setup backend capabilities flags
        _renderNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_dx12_c#");
        ImGui.GetIO().NativePtr->BackendRendererName = (byte*)_renderNamePtr.ToPointer();
        ImGui.GetIO().BackendFlags = ImGui.GetIO().BackendFlags | ImGuiBackendFlags.RendererHasVtxOffset;

        _device = new Device(new IntPtr(device));
        RTVFormat = rtv_format;
        hFontSrvCpuDescHandle = font_srv_cpu_desc_handle;
        hFontSrvGpuDescHandle = font_srv_gpu_desc_handle;
        pFrameResources = new RenderBuffer[num_frames_in_flight];
        numFramesInFlight = (uint)num_frames_in_flight;
        pd3dSrvDescHeap = new DescriptorHeap((IntPtr)cbv_srv_heap);
        frameIndex = uint.MaxValue;

        // Create buffers with a default size (they will later be grown as needed)
        for (int i = 0; i < num_frames_in_flight; i++)
        {
            var fr = new RenderBuffer();
            fr.IndexBuffer = null;
            fr.VertexBuffer = null;
            fr.IndexBufferSize = 10000;
            fr.VertexBufferSize = 5000;

            pFrameResources[i] = fr;
        }
    }

    internal static void Shutdown()
    {
        if (_device == null)
        {
            Log.Error("No renderer backend to shutdown, or already shutdown?");
        }

        var io = ImGui.GetIO();

        // Clean up windows and device objects
        InvalidateDeviceObjects();
        pFrameResources = null;

        if (_renderNamePtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_renderNamePtr);
            _renderNamePtr = IntPtr.Zero;
        }

        io.BackendFlags = io.BackendFlags & ~ImGuiBackendFlags.RendererHasVtxOffset;
    }

    internal static void NewFrame()
    {
        if (pPipelineState == null)
            CreateDeviceObjects();
    }
}
