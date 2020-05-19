using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUShaders.ShaderProfiles
{
    using SharpDX;
    using SharpDX.Direct3D12;

    public class SimpleColors : IShaderProfile
    {
        public string Name => "Simple - Colors";

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public GraphicsResource[] Resources => _resources;
        public PipelineState PipelineState => _pipelineState;

        public RootSignature RootSignature => _rootSignature;

        // App resources.
        protected Resource _vertexBuffer;
        protected VertexBufferView _vertexBufferView;

        protected int[] _indicies;

        protected Resource _indexBuffer;
        protected IndexBufferView _indexBufferView;
        protected PipelineState _pipelineState;
        protected RootSignature _rootSignature;
        protected GraphicsResource[] _resources;

        public void Update(double frameInterval)
        {

        }

        public void BuildPSO(Device3 device, GraphicsCommandList commandList)
        {
            _resources = new GraphicsResource[0];
            // Create an empty root signature.
            RootSignatureDescription rootSignatureDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout);
            _rootSignature = device.CreateRootSignature(rootSignatureDesc.Serialize());

            // Create the pipeline state, which includes compiling and loading shaders.

#if DEBUG
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/SimpleShader.hlsl", "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/SimpleShader.hlsl", "VSMain", "vs_5_0"));
#endif

#if DEBUG
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/SimpleShader.hlsl", "PSMain", "ps_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/SimpleShader.hlsl", "PSMain", "ps_5_0"));
#endif

            // Define the vertex input layout.
            InputElement[] inputElementDescs = new InputElement[]
            {
                    new InputElement("POSITION",0,Format.R32G32B32_Float,0,0),
                    new InputElement("COLOR",0,Format.R32G32B32A32_Float,12,0)
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
                DepthStencilFormat = SharpDX.DXGI.Format.D32_Float,
                DepthStencilState = new DepthStencilStateDescription() { IsDepthEnabled = false, IsStencilEnabled = false },
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
                    new Vertex() {position=new Vector3(-0.5f, -0.5f, 0.5f),color=new Vector4(1.0f, 0.0f, 0.0f, 1.0f ) },
                    new Vertex() {position=new Vector3(-0.5f, 0.5f, 0.5f ),color=new Vector4(0.0f, 1.0f, 0.0f, 1.0f ) },
                    new Vertex() {position=new Vector3(0.5f, -0.5f, 0.5f),color=new Vector4(0.0f, 0.0f, 1.0f, 1.0f) },
                    new Vertex() {position=new Vector3(0.5f, 0.5f, 0.5f),color=new Vector4(1.0f, 0.0f, 0.0f, 1.0f) }
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
                                      3,2,1};

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
        }

        public void BundleDraw(GraphicsCommandList bundleList)
        {
            bundleList.SetVertexBuffer(0, _vertexBufferView);
            bundleList.SetIndexBuffer(_indexBufferView);
            bundleList.DrawIndexedInstanced(6, 1, 0, 0, 0);
        }

        struct Vertex
        {
            public Vector3 position;
            public Vector4 color;
        };
    }
}
