using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GPUShaders.ShaderProfiles
{
    using Rectangle = System.Drawing.Rectangle;
    using SharpDX;
    using SharpDX.Direct3D12;

    public class Untextured : IShaderProfile
    {
        public string Name => "Untextured";

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public PipelineState PipelineState => _pipelineState;
        public GraphicsResource[] Resources => _resources;

        public RootSignature RootSignature => _rootSignature;

        // App resources.
        protected Resource _vertexBuffer;
        protected VertexBufferView _vertexBufferView;

        protected Resource _perPassBuffer;
        protected DescriptorHeap _perPassViewHeap;
        protected IntPtr _perPassPointer;

        protected int[] _indicies;

        protected Resource _indexBuffer;
        protected IndexBufferView _indexBufferView;
        protected PipelineState _pipelineState;
        protected RootSignature _rootSignature;
        protected GraphicsResource[] _resources;

        protected PerPass buffer;

        Matrix World, View, Projection;

        double rotation;
        public void Update(double frameInterval)
        {
            rotation += frameInterval * .1;
            View = Matrix.LookAtLH(new Vector3(10 * (float)Math.Sin(rotation), 5, 10 * (float)Math.Cos(rotation)), Vector3.Zero, Vector3.UnitY);
            Matrix world = Matrix.Translation(-2.5f, -2.5f, -2.5f);
            buffer.WVP = world * View * Projection;

            Utilities.Write(_perPassPointer, ref buffer);
        }

        public void BuildPSO(Device3 device, GraphicsCommandList commandList)
        {
            Projection = Matrix.PerspectiveFovLH((float)Math.PI / 3f, 4f / 3f, 1, 1000);
            View = Matrix.LookAtLH(new Vector3(10 * (float)Math.Sin(rotation), 5, 10 * (float)Math.Cos(rotation)), Vector3.Zero, Vector3.UnitY);
            World = Matrix.Translation(-2.5f, -2.5f, -2.5f);

            DescriptorHeapDescription cbvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            _perPassViewHeap = device.CreateDescriptorHeap(cbvHeapDesc);

            RootParameter[] rootParameters = new RootParameter[] { new RootParameter(ShaderVisibility.All, new RootDescriptor(0,0), RootParameterType.ConstantBufferView)};


            // Create an empty root signature.
            RootSignatureDescription rootSignatureDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters);
            _rootSignature = device.CreateRootSignature(rootSignatureDesc.Serialize());

            // Create the pipeline state, which includes compiling and loading shaders.

#if DEBUG
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/Untextured.hlsl", "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/Untextured.hlsl", "VSMain", "vs_5_0"));
#endif

#if DEBUG
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/Untextured.hlsl", "PSMain", "ps_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/Untextured.hlsl", "PSMain", "ps_5_0"));
#endif

            // Define the vertex input layout.
            InputElement[] inputElementDescs = new InputElement[]
            {
                    new InputElement("POSITION",0,Format.R32G32B32_Float,0,0),
                    new InputElement("COLOR",0 , Format.R32G32B32A32_Float,12,0)
            };

            // Describe and create the graphics pipeline state object (PSO).
            GraphicsPipelineStateDescription psoDesc = new GraphicsPipelineStateDescription()
            {
                InputLayout = new InputLayoutDescription(inputElementDescs),
                RootSignature = _rootSignature,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                RasterizerState = RasterizerStateDescription.Default(),
                BlendState = BlendStateDescription.Default(),
                DepthStencilFormat = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                DepthStencilState = DepthStencilStateDescription.Default(),
                SampleMask = int.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RenderTargetCount = 1,
                Flags = PipelineStateFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                StreamOutput = new StreamOutputDescription()
            };
            psoDesc.RenderTargetFormats[0] = SharpDX.DXGI.Format.R8G8B8A8_UNorm;

            _pipelineState = device.CreateGraphicsPipelineState(psoDesc);

            // Define the geometry for a triangle.
            Vertex[] triangleVertices = new Vertex[]
            {
                    //Front
                new Vertex() { Position = new Vector3(0, 0, 0), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(0, 5, 0), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(5, 0, 0), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(5, 5, 0), Color = new Vector4(0, 0, 1, 1) },

                //Back
                new Vertex() { Position = new Vector3(0, 0, 5), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(0, 5, 5), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(5, 0, 5), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(5, 5, 5), Color = new Vector4(0, 0, 1, 1) },

                //Left
                new Vertex() { Position = new Vector3(0, 0, 0), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(0, 5, 0), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(0, 0, 5), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(0, 5, 5), Color = new Vector4(0, 1, 0, 1) },

                //Right
                new Vertex() { Position = new Vector3(5, 0, 0), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(5, 5, 0), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(5, 0, 5), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(5, 5, 5), Color = new Vector4(0, 1, 0, 1) },

                //Top
                new Vertex() { Position = new Vector3(0, 0, 0), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(0, 0, 5), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(5, 0, 0), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(5, 0, 5), Color = new Vector4(1, 0, 0, 1) },

                //Bottom
                new Vertex() { Position = new Vector3(0, 5, 0), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(0, 5, 5), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(5, 5, 0), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(5, 5, 5), Color = new Vector4(1, 0, 0, 1) }
            };

            int vertexBufferSize = Utilities.SizeOf(triangleVertices);

            // Note: using upload heaps to transfer static data like vert buffers is not 
            // recommended. Every time the GPU needs it, the upload heap will be marshalled 
            // over. Please read up on Default Heap usage. An upload heap is used here for 
            // code simplicity and because there are very few verts to actually transfer.
            _vertexBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(vertexBufferSize), ResourceStates.GenericRead);

            // Copy the triangle data to the vertex buffer.
            IntPtr pVertexDataBegin = _vertexBuffer.Map(0);
            Utilities.Write(pVertexDataBegin, triangleVertices, 0, triangleVertices.Length);
            _vertexBuffer.Unmap(0);

            _indicies = new int[] { 0,1,2,
                                      3,2,1,
                                      6,5,4,
                                      5,6,7,

                                      10,9,8,
                                      9,10,11,
                                      12,13,14,
                                      15,14,13,

                                      18,17,16,
                                      17,18,19,
                                      20,21,22,
                                      23,22,21};

            int indBufferSize = Utilities.SizeOf(_indicies);

            _indexBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(indBufferSize), ResourceStates.GenericRead);

            IntPtr pIndBegin = _indexBuffer.Map(0);
            Utilities.Write(pIndBegin, _indicies, 0, _indicies.Length);
            _indexBuffer.Unmap(0);

            _indexBufferView = new IndexBufferView()
            {
                BufferLocation = _indexBuffer.GPUVirtualAddress,
                Format = Format.R32_UInt,
                SizeInBytes = indBufferSize
            };

            // Initialize the vertex buffer view.
            _vertexBufferView = new VertexBufferView
            {
                BufferLocation = _vertexBuffer.GPUVirtualAddress,
                StrideInBytes = Utilities.SizeOf<Vertex>(),
                SizeInBytes = vertexBufferSize
            };

            _perPassBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(Utilities.SizeOf<PerPass>()), ResourceStates.GenericRead);

            //// Describe and create a constant buffer view.
            ConstantBufferViewDescription cbvDesc = new ConstantBufferViewDescription()
            {
                BufferLocation = _perPassBuffer.GPUVirtualAddress,
                SizeInBytes = (Utilities.SizeOf<PerPass>() + 255) & ~255
            };
            device.CreateConstantBufferView(cbvDesc, _perPassViewHeap.CPUDescriptorHandleForHeapStart);

            // Initialize and map the constant buffers. We don't unmap this until the
            // app closes. Keeping things mapped for the lifetime of the resource is okay.
            _perPassPointer = _perPassBuffer.Map(0);
            Utilities.Write(_perPassPointer, ref buffer);

            _resources = new[] { new GraphicsResource() { Resource = _perPassBuffer, Register = 0, type = ResourceType.ConstantBufferView} };
        }

        long GetRequiredIntermediateSize(Device device, Resource destinationResource, int firstSubresource, int NumSubresources)
        {
            ResourceDescription desc = destinationResource.Description;
            long RequiredSize = 0;
            device.GetCopyableFootprints(ref desc, firstSubresource, NumSubresources, 0, null, null, null, out RequiredSize);
            return RequiredSize;
        }

        public void BundleDraw(GraphicsCommandList bundleList)
        {
            bundleList.SetGraphicsRootConstantBufferView(0, _perPassBuffer.GPUVirtualAddress);

            bundleList.SetVertexBuffer(0, _vertexBufferView);
            bundleList.SetIndexBuffer(_indexBufferView);
            bundleList.DrawIndexedInstanced(36, 1, 0, 0, 0);
        }

        struct Vertex
        {
            public Vector3 Position;
            public Vector4 Color;
        }

        protected struct PerPass
        {
            public Matrix WVP;
        }

        static int ComponentMapping(int src0, int src1, int src2, int src3)

        {
            return ((((src0) & ComponentMappingMask) |
            (((src1) & ComponentMappingMask) << ComponentMappingShift) |
                                                                (((src2) & ComponentMappingMask) << (ComponentMappingShift * 2)) |
                                                                (((src3) & ComponentMappingMask) << (ComponentMappingShift * 3)) |
                                                                ComponentMappingAlwaysSetBitAvoidingZeromemMistakes));

        }
        const int ComponentMappingMask = 0x7;
        const int ComponentMappingShift = 3;
        const int ComponentMappingAlwaysSetBitAvoidingZeromemMistakes = (1 << (ComponentMappingShift * 4));
    }
}
