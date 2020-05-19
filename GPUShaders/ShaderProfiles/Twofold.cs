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

    public class Twofold : IShaderProfile
    {
        public string Name => "Twofold";

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public PipelineState PipelineState => _pipelineState;
        public GraphicsResource[] Resources => _resources;

        public RootSignature RootSignature => _rootSignature;

        // App resources.
        protected Resource _vertexBuffer;
        protected VertexBufferView _vertexBufferView;

        protected Resource _objectBuffer, _lightingBuffer;
        protected DescriptorHeap _objectViewHeap, _lightingViewHeap;
        protected IntPtr _objectPointer, _lightingPointer;

        protected int[] _indicies;

        protected Resource _texture;
        protected DescriptorHeap _srvDescriptorHeap;

        protected Resource _indexBuffer;
        protected IndexBufferView _indexBufferView;
        protected PipelineState _pipelineState;
        protected RootSignature _rootSignature;
        protected GraphicsResource[] _resources;

        protected ObjectData[] buffer = new ObjectData[2];
        protected Lighting light;

        const int textureWidth = 512, textureHeight = 512;
        Matrix World, View, Projection;

        double rotation = 0;
        public Vector3 eyePosition;
        public void Update(double frameInterval)
        {
            rotation += frameInterval * .1;
            eyePosition = new Vector3(15 * (float)Math.Sin(rotation), 7.5f, 15 * (float)Math.Cos(rotation));
            light.EyePositionX = eyePosition.X;
            light.EyePositionY = eyePosition.Y;
            light.EyePositionZ = eyePosition.Z;
            View = Matrix.LookAtLH(eyePosition, Vector3.Zero, Vector3.UnitY);
            buffer[1].World = buffer[0].World * Matrix.Translation(new Vector3(5 * (float)Math.Sin(-rotation), 5f, 5 * (float)Math.Cos(-rotation)));
            buffer[0].WVP = buffer[0].World * View * Projection;
            buffer[1].WVP = buffer[1].World * View * Projection;

            Utilities.Write(_lightingPointer, ref light);
            CopyData(0, ref buffer[0]);
            CopyData(1, ref buffer[1]);
        }

        public void BuildPSO(Device3 device, GraphicsCommandList commandList)
        {
            buffer[0].World = Matrix.Translation(-2.5f,-2.5f,-2.5f);
            buffer[1].World = Matrix.Translation(2.5f, 2.5f, 2.5f);
            light = new Lighting
            {
                GlobalAmbientX = 1,
                GlobalAmbientY = 1,
                GlobalAmbientZ = 1,
                KaX = .1f,
                KaY = .1f,
                KaZ = .1f,
                KdX = .5f,
                KdY = .5f,
                KdZ = .5f,
                KeX = .25f,
                KeY = .25f,
                KeZ = .25f,
                KsX = .1f,
                KsY = .1f,
                KsZ = .1f,
                LightColorX = 1,
                LightColorY = 1,
                LightColorZ = 1,
                LightPositionX = 10,
                LightPositionY = 10,
                LightPositionZ = 10,
                shininess = 5
            };

            DescriptorHeapDescription srvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            _srvDescriptorHeap = device.CreateDescriptorHeap(srvHeapDesc);

            //setup descriptor ranges
            DescriptorRange[] ranges = new DescriptorRange[] { new DescriptorRange() { RangeType = DescriptorRangeType.ShaderResourceView, DescriptorCount = 1, OffsetInDescriptorsFromTableStart = int.MinValue, BaseShaderRegister = 0 } };

            //Get sampler state setup
            StaticSamplerDescription sampler = new StaticSamplerDescription()
            {
                Filter = Filter.MinimumMinMagMipPoint,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                MipLODBias = 0,
                MaxAnisotropy = 0,
                ComparisonFunc = Comparison.Never,
                BorderColor = StaticBorderColor.TransparentBlack,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue,
                ShaderRegister = 0,
                RegisterSpace = 0,
                ShaderVisibility = ShaderVisibility.Pixel,
            };

            Projection = Matrix.PerspectiveFovLH((float)Math.PI / 3f, 4f / 3f, 1, 1000);
            View = Matrix.LookAtLH(new Vector3(10 * (float)Math.Sin(rotation), 5, 10 * (float)Math.Cos(rotation)), Vector3.Zero, Vector3.UnitY);
            World = Matrix.Translation(-2.5f, -2.5f, -2.5f);

            DescriptorHeapDescription cbvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
            };

            _objectViewHeap = device.CreateDescriptorHeap(cbvHeapDesc);
            _lightingViewHeap = device.CreateDescriptorHeap(cbvHeapDesc);

            RootParameter[] rootParameters = new RootParameter[] { new RootParameter(ShaderVisibility.Pixel, ranges),
                new RootParameter(ShaderVisibility.All, new RootDescriptor(1, 0), RootParameterType.ConstantBufferView),
                new RootParameter(ShaderVisibility.All, new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)};


            // Create an empty root signature.
            RootSignatureDescription rootSignatureDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters, new StaticSamplerDescription[] { sampler });
            _rootSignature = device.CreateRootSignature(rootSignatureDesc.Serialize());

            // Create the pipeline state, which includes compiling and loading shaders.

#if DEBUG
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/LitPixel.hlsl", "VSMain", "vs_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var vertexShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/LitPixel.hlsl", "VSMain", "vs_5_0"));
#endif

#if DEBUG
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/LitPixel.hlsl", "PSMain", "ps_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug));
#else
            var pixelShader = new ShaderBytecode(SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/LitPixel.hlsl", "PSMain", "ps_5_0"));
#endif

            // Define the vertex input layout.
            InputElement[] inputElementDescs = new InputElement[]
            {
                    new InputElement("POSITION",0,Format.R32G32B32_Float,0,0),
                    new InputElement("NORMAL",0,Format.R32G32B32_Float,12,0),
                    new InputElement("TEXCOORD",0, Format.R32G32_Float,24,0)
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
                new Vertex() { Position = new Vector3(0, 0, 0), TexCoord = new Vector2(1, 1), Normal = -Vector3.UnitZ },
                new Vertex() { Position = new Vector3(0, 5, 0), TexCoord = new Vector2(1, 0), Normal = -Vector3.UnitZ },
                new Vertex() { Position = new Vector3(5, 0, 0), TexCoord = new Vector2(0, 1), Normal = -Vector3.UnitZ },
                new Vertex() { Position = new Vector3(5, 5, 0), TexCoord = new Vector2(0, 0), Normal = -Vector3.UnitZ },

                //Back
                new Vertex() { Position = new Vector3(0, 0, 5), TexCoord = new Vector2(1, 1), Normal = Vector3.UnitZ },
                new Vertex() { Position = new Vector3(0, 5, 5), TexCoord = new Vector2(1, 0), Normal = Vector3.UnitZ },
                new Vertex() { Position = new Vector3(5, 0, 5), TexCoord = new Vector2(0, 1), Normal = Vector3.UnitZ },
                new Vertex() { Position = new Vector3(5, 5, 5), TexCoord = new Vector2(0, 0), Normal = Vector3.UnitZ },

                //Left
                new Vertex() { Position = new Vector3(0, 0, 0), TexCoord = new Vector2(1, 1), Normal = -Vector3.UnitX },
                new Vertex() { Position = new Vector3(0, 5, 0), TexCoord = new Vector2(1, 0), Normal = -Vector3.UnitX },
                new Vertex() { Position = new Vector3(0, 0, 5), TexCoord = new Vector2(0, 1), Normal = -Vector3.UnitX },
                new Vertex() { Position = new Vector3(0, 5, 5), TexCoord = new Vector2(0, 0), Normal = -Vector3.UnitX },

                //Right
                new Vertex() { Position = new Vector3(5, 0, 0), TexCoord = new Vector2(1, 1), Normal = Vector3.UnitX },
                new Vertex() { Position = new Vector3(5, 5, 0), TexCoord = new Vector2(1, 0), Normal = Vector3.UnitX },
                new Vertex() { Position = new Vector3(5, 0, 5), TexCoord = new Vector2(0, 1), Normal = Vector3.UnitX },
                new Vertex() { Position = new Vector3(5, 5, 5), TexCoord = new Vector2(0, 0), Normal = Vector3.UnitX },

                //Top
                new Vertex() { Position = new Vector3(0, 0, 0), TexCoord = new Vector2(1, 1), Normal = -Vector3.UnitY },
                new Vertex() { Position = new Vector3(0, 0, 5), TexCoord = new Vector2(1, 0), Normal = -Vector3.UnitY },
                new Vertex() { Position = new Vector3(5, 0, 0), TexCoord = new Vector2(0, 1), Normal = -Vector3.UnitY },
                new Vertex() { Position = new Vector3(5, 0, 5), TexCoord = new Vector2(0, 0), Normal = -Vector3.UnitY },

                //Bottom
                new Vertex() { Position = new Vector3(0, 5, 0), TexCoord = new Vector2(1, 1), Normal = Vector3.UnitY },
                new Vertex() { Position = new Vector3(0, 5, 5), TexCoord = new Vector2(1, 0), Normal = Vector3.UnitY },
                new Vertex() { Position = new Vector3(5, 5, 0), TexCoord = new Vector2(0, 1), Normal = Vector3.UnitY },
                new Vertex() { Position = new Vector3(5, 5, 5), TexCoord = new Vector2(0, 0), Normal = Vector3.UnitY }
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

            _objectBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(((Utilities.SizeOf<ObjectData>() + 255) & ~255) * 2), ResourceStates.GenericRead);

            //// Describe and create a constant buffer view.
            ConstantBufferViewDescription cbvDesc = new ConstantBufferViewDescription()
            {
                BufferLocation = _objectBuffer.GPUVirtualAddress,
                SizeInBytes = ((Utilities.SizeOf<ObjectData>() + 255) & ~255) * 2
            };
            device.CreateConstantBufferView(cbvDesc, _objectViewHeap.CPUDescriptorHandleForHeapStart);

            // Initialize and map the constant buffers. We don't unmap this until the
            // app closes. Keeping things mapped for the lifetime of the resource is okay.
            _objectPointer = _objectBuffer.Map(0);

            _lightingBuffer = device.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(Utilities.SizeOf<Lighting>()), ResourceStates.GenericRead);

            //// Describe and create a constant buffer view.
            ConstantBufferViewDescription cbvDesc2 = new ConstantBufferViewDescription()
            {
                BufferLocation = _objectBuffer.GPUVirtualAddress,
                SizeInBytes = (Utilities.SizeOf<Lighting>() + 255) & ~255
            };
            device.CreateConstantBufferView(cbvDesc2, _lightingViewHeap.CPUDescriptorHandleForHeapStart);

            // Initialize and map the constant buffers. We don't unmap this until the
            // app closes. Keeping things mapped for the lifetime of the resource is okay.
            _lightingPointer = _lightingBuffer.Map(0);
            Utilities.Write(_lightingPointer, ref light);

            Resource textureUploadHeap;

            // Create the texture.
            // Describe and create a Texture2D.
            ResourceDescription textureDesc = ResourceDescription.Texture2D(Format.R8G8B8A8_UNorm, textureWidth, textureHeight);
            _texture = device.CreateCommittedResource(new HeapProperties(HeapType.Default), HeapFlags.None, textureDesc, ResourceStates.CopyDestination);

            long uploadBufferSize = GetRequiredIntermediateSize(device, _texture, 0, 1);

            // Create the GPU upload buffer.
            textureUploadHeap = device.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, ResourceDescription.Texture2D(Format.R8G8B8A8_UNorm, textureWidth, textureHeight), ResourceStates.GenericRead);

            // Copy data to the intermediate upload heap and then schedule a copy 
            // from the upload heap to the Texture2D.
            byte[] textureData = GenerateTextureData();

            GCHandle handle = GCHandle.Alloc(textureData, GCHandleType.Pinned);
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(textureData, 0);
            textureUploadHeap.WriteToSubresource(0, null, ptr, 4 * textureWidth, textureData.Length);
            handle.Free();

            commandList.CopyTextureRegion(new TextureCopyLocation(_texture, 0), 0, 0, 0, new TextureCopyLocation(textureUploadHeap, 0), null);

            commandList.ResourceBarrierTransition(_texture, ResourceStates.CopyDestination, ResourceStates.PixelShaderResource);

            // Describe and create a SRV for the texture.
            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription()
            {
                Shader4ComponentMapping = ComponentMapping(0, 1, 2, 3),
                Format = textureDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
            };
            srvDesc.Texture2D.MipLevels = 1;

            device.CreateShaderResourceView(_texture, srvDesc, _srvDescriptorHeap.CPUDescriptorHandleForHeapStart);

            _resources = new[] { new GraphicsResource() { Heap = _srvDescriptorHeap, Register = 0, type = ResourceType.DescriptorTable},
                new GraphicsResource() { Resource = _objectBuffer, Register = 2, type = ResourceType.ConstantBufferView },
                new GraphicsResource() { Resource = _lightingBuffer, Register = 1, type = ResourceType.ConstantBufferView } };
        }

        long GetRequiredIntermediateSize(Device device, Resource destinationResource, int firstSubresource, int NumSubresources)
        {
            ResourceDescription desc = destinationResource.Description;
            long RequiredSize = 0;
            device.GetCopyableFootprints(ref desc, firstSubresource, NumSubresources, 0, null, null, null, out RequiredSize);
            return RequiredSize;
        }

        void CopyData(int elementIndex, ref ObjectData data)
        {
            Marshal.StructureToPtr(data, _objectPointer + elementIndex * ((Utilities.SizeOf<ObjectData>() + 255) & ~255), true);
        }

        byte[] GenerateTextureData()
        {
            Bitmap img = new Bitmap("koala.png");
            int size = img.Width * img.Height;
            BitmapData dat = img.LockBits(new Rectangle(0, 0, textureWidth, textureHeight), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] colDat = new byte[size * 4];
            Marshal.Copy(dat.Scan0, colDat, 0, colDat.Length);

            for (int i = 0; i < colDat.Length; i += 4)
            {
                byte r = colDat[i + 2],
                    g = colDat[i + 1],
                    b = colDat[i],
                    a = colDat[i + 3];
                colDat[i] = r;
                colDat[i + 1] = g;
                colDat[i + 2] = b;
                colDat[i + 3] = a;
            }
            return colDat;
        }

        public void BundleDraw(GraphicsCommandList bundleList)
        {
            bundleList.SetVertexBuffer(0, _vertexBufferView);
            bundleList.SetIndexBuffer(_indexBufferView);
            bundleList.DrawIndexedInstanced(36, 1, 0, 0, 0);
            bundleList.SetGraphicsRootConstantBufferView(2, _objectBuffer.GPUVirtualAddress + (Utilities.SizeOf<ObjectData>() + 255) & ~255);
            bundleList.DrawIndexedInstanced(36, 1, 0, 0, 0);
        }

        struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TexCoord;
        }

        protected struct ObjectData
        {
            public Matrix WVP;
            public Matrix World;
        }

        protected struct Lighting
        {
            public float GlobalAmbientX;
            public float GlobalAmbientY;
            public float GlobalAmbientZ;
            public float LightColorX;
            public float LightColorY;
            public float LightColorZ;
            public float LightPositionX;
            public float LightPositionY;
            public float LightPositionZ;
            public float EyePositionX;
            public float EyePositionY;
            public float EyePositionZ;
            public float KeX;
            public float KeY;
            public float KeZ;
            public float KaX;
            public float KaY;
            public float KaZ;
            public float KdX;
            public float KdY;
            public float KdZ;
            public float KsX;
            public float KsY;
            public float KsZ;
            public float shininess;
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
