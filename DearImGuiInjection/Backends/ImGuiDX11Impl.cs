using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using BlendState = SharpDX.Direct3D11.BlendState;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using SamplerStateDescription = SharpDX.Direct3D11.SamplerStateDescription;
using ShaderResourceViewDescription = SharpDX.Direct3D11.ShaderResourceViewDescription;
using ShaderResourceViewDimension = SharpDX.Direct3D.ShaderResourceViewDimension;

namespace DearImGuiInjection.Backends;

public static class ImGuiDX11Impl
{
    private static IntPtr _renderNamePtr;
    private static Device _device;
    private static DeviceContext _deviceContext;
    private static ShaderResourceView _fontResourceView;
    private static SamplerState _fontSampler;
    private static VertexShader _vertexShader;
    private static PixelShader _pixelShader;
    private static InputLayout _inputLayout;
    private static Buffer _vertexConstantBuffer;
    private static BlendState _blendState;
    private static RasterizerState _rasterizerState;
    private static DepthStencilState _depthStencilState;
    private static Buffer _vertexBuffer;
    private static Buffer _indexBuffer;
    private static int _vertexBufferSize;
    private static int _indexBufferSize;
    private static VertexBufferBinding _vertexBinding;
    // so we don't make a temporary object every frame
    private static RawColor4 _blendColor = new RawColor4(0, 0, 0, 0);

    public unsafe struct VERTEX_CONSTANT_BUFFER_DX11
    {
        public fixed float mvp[4 * 4];
    }

    // Functions
    static unsafe void ImGui_ImplDX11_SetupRenderState(ImDrawData* draw_data, IntPtr ID3D11DeviceContextPtr)
    {
        var deviceContext = new DeviceContext(ID3D11DeviceContextPtr);

        // Setup viewport
        deviceContext.Rasterizer.SetViewport(0, 0, draw_data->DisplaySize.X, draw_data->DisplaySize.Y);


        // Setup shader and vertex buffers
        deviceContext.InputAssembler.InputLayout = _inputLayout;

        deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding()
        {
            Stride = sizeof(ImDrawVert),
            Offset = 0,
            Buffer = _vertexBuffer
        });
        deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
        deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        deviceContext.VertexShader.SetShader(_vertexShader, null, 0);
        deviceContext.VertexShader.SetConstantBuffer(0, _vertexConstantBuffer);
        deviceContext.PixelShader.SetShader(_pixelShader, null, 0);
        deviceContext.PixelShader.SetSampler(0, _fontSampler);
        deviceContext.GeometryShader.SetShader(null, null, 0);
        deviceContext.HullShader.SetShader(null, null, 0);
        deviceContext.DomainShader.SetShader(null, null, 0);
        deviceContext.ComputeShader.SetShader(null, null, 0);

        // Setup blend state
        RawColor4 blendFactor = new(0.0f, 0.0f, 0.0f, 0.0f);
        deviceContext.OutputMerger.SetBlendState(_blendState, blendFactor, 0xffffffff);
        deviceContext.OutputMerger.SetDepthStencilState(_depthStencilState, 0);
        deviceContext.Rasterizer.State = _rasterizerState;
    }

    private unsafe delegate void ImDrawUserCallBack(ImDrawList* a, ImDrawCmd* b);

    // Render function
    public static unsafe void RenderDrawData(ImDrawData* draw_data)
    {
        // Avoid rendering when minimized
        if (draw_data->DisplaySize.X <= 0.0f || draw_data->DisplaySize.Y <= 0.0f)
            return;

        DeviceContext ctx = _deviceContext;

        // Create and grow vertex/index buffers if needed
        if (_vertexBuffer == null || _vertexBufferSize < draw_data->TotalVtxCount)
        {
            _vertexBuffer?.Dispose();

            _vertexBufferSize = draw_data->TotalVtxCount + 5000;

            _vertexBuffer = new Buffer(_device, new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = _vertexBufferSize * Unsafe.SizeOf<ImDrawVert>(),
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            });

            // (Re)make this here rather than every frame
            _vertexBinding = new VertexBufferBinding
            {
                Buffer = _vertexBuffer,
                Stride = Unsafe.SizeOf<ImDrawVert>(),
                Offset = 0
            };
        }

        if (_indexBuffer == null || _indexBufferSize < draw_data->TotalIdxCount)
        {
            _indexBuffer?.Dispose();

            _indexBufferSize = draw_data->TotalIdxCount + 10000;

            _indexBuffer = new Buffer(_device, new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = _indexBufferSize * sizeof(ushort),
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
            });
        }

        // Upload vertex/index data into a single contiguous GPU buffer
        ctx.MapSubresource(_vertexBuffer, MapMode.WriteDiscard, MapFlags.None, out var vtx_resource);
        ctx.MapSubresource(_indexBuffer, MapMode.WriteDiscard, MapFlags.None, out var idx_resource);

        ImDrawVert* vtx_dst = (ImDrawVert*)vtx_resource.DataPointer;
        ushort* idx_dst = (ushort*)idx_resource.DataPointer;
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

        ctx.UnmapSubresource(_vertexBuffer, 0);
        ctx.UnmapSubresource(_indexBuffer, 0);

        // Setup orthographic projection matrix into our constant buffer
        // Our visible imgui space lies from draw_data->DisplayPos (top left) to draw_data->DisplayPos+data_data->DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
        ctx.MapSubresource(_vertexConstantBuffer, MapMode.WriteDiscard, MapFlags.None, out var mapped_resource);

        VERTEX_CONSTANT_BUFFER_DX11* constant_buffer = (VERTEX_CONSTANT_BUFFER_DX11*)mapped_resource.DataPointer;
        float L = draw_data->DisplayPos.X;
        float R = draw_data->DisplayPos.X + draw_data->DisplaySize.X;
        float T = draw_data->DisplayPos.Y;
        float B = draw_data->DisplayPos.Y + draw_data->DisplaySize.Y;

        constant_buffer->mvp[0] = 2.0f / (R - L);
        constant_buffer->mvp[1] = 0.0f;
        constant_buffer->mvp[2] = 0.0f;
        constant_buffer->mvp[3] = 0.0f;

        constant_buffer->mvp[4] = 0.0f;
        constant_buffer->mvp[5] = 2.0f / (T - B);
        constant_buffer->mvp[6] = 0.0f;
        constant_buffer->mvp[7] = 0.0f;

        constant_buffer->mvp[8] = 0.0f;
        constant_buffer->mvp[9] = 0.0f;
        constant_buffer->mvp[10] = 0.5f;
        constant_buffer->mvp[11] = 0.0f;

        constant_buffer->mvp[12] = (R + L) / (L - R);
        constant_buffer->mvp[13] = (T + B) / (B - T);
        constant_buffer->mvp[14] = 0.5f;
        constant_buffer->mvp[15] = 1.0f;

        ctx.UnmapSubresource(_vertexConstantBuffer, 0);

        var old = BackupRenderState(ctx);

        ImGui_ImplDX11_SetupRenderState(draw_data, ctx.NativePointer);

        // Render command lists
        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_idx_offset = 0;
        int global_vtx_offset = 0;
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
                        ImGui_ImplDX11_SetupRenderState(draw_data, ctx.NativePointer);
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

                    // Apply scissor/clipping rectangle
                    RawRectangle r = new((int)clip_min.X, (int)clip_min.Y, (int)clip_max.X, (int)clip_max.Y);
                    ctx.Rasterizer.SetScissorRectangles(r);

                    ctx.PixelShader.SetShaderResource(0, new(pcmd.TextureId));
                    ctx.DrawIndexed((int)pcmd.ElemCount, (int)(pcmd.IdxOffset + global_idx_offset), (int)(pcmd.VtxOffset + global_vtx_offset));
                }
            }
            global_idx_offset += cmd_list->IdxBuffer.Size;
            global_vtx_offset += cmd_list->VtxBuffer.Size;
        }

        RestoreRenderState(ctx, old);
    }

    private static StateBackup BackupRenderState(DeviceContext ctx)
    {
        const int D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE = 16;

        var backup = new StateBackup
        {
            ScissorRects = new Rectangle[D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE],
            Viewports = new RawViewportF[D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE],
            VertexBuffers = new Buffer[InputAssemblerStage.VertexInputResourceSlotCount],
            VertexBufferStrides = new int[InputAssemblerStage.VertexInputResourceSlotCount],
            VertexBufferOffsets = new int[InputAssemblerStage.VertexInputResourceSlotCount],

            // IA
            InputLayout = ctx.InputAssembler.InputLayout
        };
        ctx.InputAssembler.GetIndexBuffer(out backup.IndexBuffer, out backup.IndexBufferFormat, out backup.IndexBufferOffset);
        backup.PrimitiveTopology = ctx.InputAssembler.PrimitiveTopology;
        ctx.InputAssembler.GetVertexBuffers(0, InputAssemblerStage.VertexInputResourceSlotCount, backup.VertexBuffers, backup.VertexBufferStrides, backup.VertexBufferOffsets);

        // RS
        backup.RS = ctx.Rasterizer.State;
        ctx.Rasterizer.GetScissorRectangles<Rectangle>(backup.ScissorRects);
        ctx.Rasterizer.GetViewports<RawViewportF>(backup.Viewports);

        // OM
        backup.BlendState = ctx.OutputMerger.GetBlendState(out backup.BlendFactor, out backup.SampleMask);
        backup.DepthStencilState = ctx.OutputMerger.GetDepthStencilState(out backup.DepthStencilRef);
        backup.RenderTargetViews = ctx.OutputMerger.GetRenderTargets(OutputMergerStage.SimultaneousRenderTargetCount, out backup.DepthStencilView);

        // VS
        backup.VS = ctx.VertexShader.Get();
        backup.VSSamplers = ctx.VertexShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
        backup.VSConstantBuffers = ctx.VertexShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
        backup.VSResourceViews = ctx.VertexShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

        // HS
        backup.HS = ctx.HullShader.Get();
        backup.HSSamplers = ctx.HullShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
        backup.HSConstantBuffers = ctx.HullShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
        backup.HSResourceViews = ctx.HullShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

        // DS
        backup.DS = ctx.DomainShader.Get();
        backup.DSSamplers = ctx.DomainShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
        backup.DSConstantBuffers = ctx.DomainShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
        backup.DSResourceViews = ctx.DomainShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

        // GS
        backup.GS = ctx.GeometryShader.Get();
        backup.GSSamplers = ctx.GeometryShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
        backup.GSConstantBuffers = ctx.GeometryShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
        backup.GSResourceViews = ctx.GeometryShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

        // PS
        backup.PS = ctx.PixelShader.Get();
        backup.PSSamplers = ctx.PixelShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
        backup.PSConstantBuffers = ctx.PixelShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
        backup.PSResourceViews = ctx.PixelShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);

        // CS
        backup.CS = ctx.ComputeShader.Get();
        backup.CSSamplers = ctx.ComputeShader.GetSamplers(0, CommonShaderStage.SamplerSlotCount);
        backup.CSConstantBuffers = ctx.ComputeShader.GetConstantBuffers(0, CommonShaderStage.ConstantBufferApiSlotCount);
        backup.CSResourceViews = ctx.ComputeShader.GetShaderResources(0, CommonShaderStage.InputResourceSlotCount);
        backup.CSUAVs = ctx.ComputeShader.GetUnorderedAccessViews(0, ComputeShaderStage.UnorderedAccessViewSlotCount);   // should be register count and not slot, but the value is correct

        return backup;
    }

    private static void RestoreRenderState(DeviceContext ctx, StateBackup old)
    {
        // IA
        ctx.InputAssembler.InputLayout = old.InputLayout;
        ctx.InputAssembler.SetIndexBuffer(old.IndexBuffer, old.IndexBufferFormat, old.IndexBufferOffset);
        ctx.InputAssembler.PrimitiveTopology = old.PrimitiveTopology;
        ctx.InputAssembler.SetVertexBuffers(0, old.VertexBuffers, old.VertexBufferStrides, old.VertexBufferOffsets);

        // RS
        ctx.Rasterizer.State = old.RS;
        ctx.Rasterizer.SetScissorRectangles(old.ScissorRects);
        ctx.Rasterizer.SetViewports(old.Viewports, old.Viewports.Length);

        // OM
        ctx.OutputMerger.SetBlendState(old.BlendState, old.BlendFactor, old.SampleMask);
        ctx.OutputMerger.SetDepthStencilState(old.DepthStencilState, old.DepthStencilRef);
        ctx.OutputMerger.SetRenderTargets(old.DepthStencilView, old.RenderTargetViews);

        // VS
        ctx.VertexShader.Set(old.VS);
        ctx.VertexShader.SetSamplers(0, old.VSSamplers);
        ctx.VertexShader.SetConstantBuffers(0, old.VSConstantBuffers);
        ctx.VertexShader.SetShaderResources(0, old.VSResourceViews);

        // HS
        ctx.HullShader.Set(old.HS);
        ctx.HullShader.SetSamplers(0, old.HSSamplers);
        ctx.HullShader.SetConstantBuffers(0, old.HSConstantBuffers);
        ctx.HullShader.SetShaderResources(0, old.HSResourceViews);

        // DS
        ctx.DomainShader.Set(old.DS);
        ctx.DomainShader.SetSamplers(0, old.DSSamplers);
        ctx.DomainShader.SetConstantBuffers(0, old.DSConstantBuffers);
        ctx.DomainShader.SetShaderResources(0, old.DSResourceViews);

        // GS
        ctx.GeometryShader.Set(old.GS);
        ctx.GeometryShader.SetSamplers(0, old.GSSamplers);
        ctx.GeometryShader.SetConstantBuffers(0, old.GSConstantBuffers);
        ctx.GeometryShader.SetShaderResources(0, old.GSResourceViews);

        // PS
        ctx.PixelShader.Set(old.PS);
        ctx.PixelShader.SetSamplers(0, old.PSSamplers);
        ctx.PixelShader.SetConstantBuffers(0, old.PSConstantBuffers);
        ctx.PixelShader.SetShaderResources(0, old.PSResourceViews);

        // CS
        ctx.ComputeShader.Set(old.CS);
        ctx.ComputeShader.SetSamplers(0, old.CSSamplers);
        ctx.ComputeShader.SetConstantBuffers(0, old.CSConstantBuffers);
        ctx.ComputeShader.SetShaderResources(0, old.CSResourceViews);
        ctx.ComputeShader.SetUnorderedAccessViews(0, old.CSUAVs);
    }

    private class StateBackup
    {
        // IA
        public InputLayout InputLayout;
        public PrimitiveTopology PrimitiveTopology;
        public Buffer IndexBuffer;
        public SharpDX.DXGI.Format IndexBufferFormat;
        public int IndexBufferOffset;
        public Buffer[] VertexBuffers;
        public int[] VertexBufferStrides;
        public int[] VertexBufferOffsets;

        // RS
        public RasterizerState RS;
        public Rectangle[] ScissorRects;
        public RawViewportF[] Viewports;

        // OM
        public BlendState BlendState;
        public RawColor4 BlendFactor;
        public int SampleMask;
        public DepthStencilState DepthStencilState;
        public int DepthStencilRef;
        public DepthStencilView DepthStencilView;
        public RenderTargetView[] RenderTargetViews;

        // VS
        public VertexShader VS;
        public Buffer[] VSConstantBuffers;
        public SamplerState[] VSSamplers;
        public ShaderResourceView[] VSResourceViews;

        // HS
        public HullShader HS;
        public Buffer[] HSConstantBuffers;
        public SamplerState[] HSSamplers;
        public ShaderResourceView[] HSResourceViews;

        // DS
        public DomainShader DS;
        public Buffer[] DSConstantBuffers;
        public SamplerState[] DSSamplers;
        public ShaderResourceView[] DSResourceViews;

        // GS
        public GeometryShader GS;
        public Buffer[] GSConstantBuffers;
        public SamplerState[] GSSamplers;
        public ShaderResourceView[] GSResourceViews;

        // PS
        public PixelShader PS;
        public Buffer[] PSConstantBuffers;
        public SamplerState[] PSSamplers;
        public ShaderResourceView[] PSResourceViews;

        public ComputeShader CS;
        public Buffer[] CSConstantBuffers;
        public SamplerState[] CSSamplers;
        public ShaderResourceView[] CSResourceViews;
        public UnorderedAccessView[] CSUAVs;
    }

    public static unsafe void CreateFontsTexture()
    {
        var io = ImGui.GetIO();

        // Build texture atlas
        io.Fonts.GetTexDataAsRGBA32(out IntPtr fontPixels, out int fontWidth, out int fontHeight, out int fontBytesPerPixel);

        // Upload texture to graphics system
        var texDesc = new Texture2DDescription
        {
            Width = fontWidth,
            Height = fontHeight,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

        using (var fontTexture = new Texture2D(_device, texDesc, new DataRectangle(fontPixels, fontWidth * fontBytesPerPixel)))
        {
            // Create texture view
            _fontResourceView = new ShaderResourceView(_device, fontTexture, new ShaderResourceViewDescription
            {
                Format = texDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = { MipLevels = texDesc.MipLevels }
            });
        }

        // Store our identifier
        io.Fonts.SetTexID(_fontResourceView.NativePointer);

        // Create texture sampler
        _fontSampler = new SamplerState(_device, new SamplerStateDescription
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLodBias = 0,
            ComparisonFunction = Comparison.Always,
            MinimumLod = 0,
            MaximumLod = 0
        });
    }

    public static unsafe bool CreateDeviceObjects()
    {
        if (_device == null)
        {
            return false;
        }

        if (_fontSampler != null)
        {
            InvalidateDeviceObjects();
        }

        var shadersFolder = Path.Combine(DearImGuiInjection.AssetsFolderPath, "Shaders");

        var vertexShaderPath = Path.Combine(shadersFolder, "imgui-vertex.hlsl.bytes");
        byte[] vertexShaderBytes = File.ReadAllBytes(vertexShaderPath);
        _vertexShader = new VertexShader(_device, vertexShaderBytes);

        // Create the input layout
        _inputLayout = new InputLayout(_device, vertexShaderBytes, new[]
        {
            new InputElement("POSITION", 0, Format.R32G32_Float, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0),
            new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 0)
        });

        // Create the constant buffer
        _vertexConstantBuffer = new Buffer(_device, new BufferDescription
        {
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ConstantBuffer,
            CpuAccessFlags = CpuAccessFlags.Write,
            OptionFlags = ResourceOptionFlags.None,
            SizeInBytes = 16 * sizeof(float)
        });

        var pixelShaderPath = Path.Combine(shadersFolder, "imgui-frag.hlsl.bytes");
        byte[] pixelShaderBytes = File.ReadAllBytes(pixelShaderPath);
        _pixelShader = new PixelShader(_device, pixelShaderBytes);

        // Create the blending setup
        // ...of course this was setup in a way that can't be done inline
        var blendStateDesc = new BlendStateDescription
        {
            AlphaToCoverageEnable = false
        };
        blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
        blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
        blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.InverseSourceAlpha;
        blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
        blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
        blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
        _blendState = new BlendState(_device, blendStateDesc);

        // Create the rasterizer state
        _rasterizerState = new RasterizerState(_device, new RasterizerStateDescription
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            IsScissorEnabled = true,
            IsDepthClipEnabled = true
        });

        // Create the depth-stencil State
        _depthStencilState = new DepthStencilState(_device, new DepthStencilStateDescription
        {
            IsDepthEnabled = false,
            DepthWriteMask = DepthWriteMask.All,
            DepthComparison = Comparison.Always,
            IsStencilEnabled = false,
            FrontFace =
                {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Always
                },
            BackFace =
                {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Always
                }
        });

        CreateFontsTexture();

        return true;
    }

    public static void InvalidateDeviceObjects()
    {
        if (_device == null)
        {
            return;
        }

        _fontSampler?.Dispose();
        _fontSampler = null;

        _fontResourceView?.Dispose();
        _fontResourceView = null;
        ImGui.GetIO().Fonts.SetTexID(IntPtr.Zero);

        _indexBuffer?.Dispose();
        _indexBuffer = null;

        _vertexBuffer?.Dispose();
        _vertexBuffer = null;

        _blendState?.Dispose();
        _blendState = null;

        _depthStencilState?.Dispose();
        _depthStencilState = null;

        _rasterizerState?.Dispose();
        _rasterizerState = null;

        _pixelShader?.Dispose();
        _pixelShader = null;

        _vertexConstantBuffer?.Dispose();
        _vertexConstantBuffer = null;

        _inputLayout?.Dispose();
        _inputLayout = null;

        _vertexShader?.Dispose();
        _vertexShader = null;
    }

    public static void Shutdown()
    {
        InvalidateDeviceObjects();

        // we don't own these, so no Dispose()
        _device = null;
        _deviceContext = null;

        if (_renderNamePtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_renderNamePtr);
            _renderNamePtr = IntPtr.Zero;
        }
    }

    public static void NewFrame()
    {
        if (_fontSampler == null)
        {
            CreateDeviceObjects();
        }
    }

    internal static unsafe void Init(void* device, void* deviceContext)
    {
        ImGui.GetIO().BackendFlags = ImGui.GetIO().BackendFlags | ImGuiBackendFlags.RendererHasVtxOffset;

        _renderNamePtr = Marshal.StringToHGlobalAnsi("imgui_impl_dx11_c#");
        ImGui.GetIO().NativePtr->BackendRendererName = (byte*)_renderNamePtr.ToPointer();

        _device = new((IntPtr)device);
        _deviceContext = new((IntPtr)deviceContext);
    }
}
