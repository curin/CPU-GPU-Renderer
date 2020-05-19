using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Drawing;

using CPUShaders;
using System.Diagnostics;

namespace CPUShaders.ShaderProfiles
{
    public class SimpleColors : IShaderProfile
    {
        ShaderPipeline<Vertex, CBuffer> _pipeline;
        Vertex[] vertexBuffer;
        int[] indexBuffer;
        CBuffer buffer;

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public string Name => "Simple - Colors";

        C3DApp _app;
        public C3DApp Application { get => _app; set => _app = value; }

        public SimpleColors(C3DApp app)
        {
            _app = app;
        }

        public void Draw()
        {
            SoftwareRasterizer.ClearBitmap(_app.CurrentSwapchainBuffer, Color.CornflowerBlue);
            SoftwareRasterizer.ClearDepth(_app.CurrentDepthBuffer);

            _pipeline.Run(vertexBuffer, indexBuffer, buffer, _app.CurrentSwapchainBuffer, _app.CurrentDepthBuffer);
        }

        public void Initialize()
        {
            ShaderProgram prog = new ShaderProgram();
            _pipeline = new ShaderPipeline<Vertex, CBuffer>(prog, prog);
            vertexBuffer = new Vertex[4]
            {
                new Vertex() { Position = new Vector3(-.5f, -.5f, .5f), Color = new Vector4(1, 0, 0, 1) },
                new Vertex() { Position = new Vector3(-.5f, .5f, .5f), Color = new Vector4(0, 0, 1, 1) },
                new Vertex() { Position = new Vector3(.5f, -.5f, .5f), Color = new Vector4(0, 1, 0, 1) },
                new Vertex() { Position = new Vector3(.5f, .5f, .5f), Color = new Vector4(1, 0, 0, 1) }
            };
            indexBuffer = new int[] { 0,1,2,
                                      3,2,1};
        }

        public struct Vertex
        {
            public Vector3 Position;
            public Vector4 Color;
        }

        public void Update(double frameInterval)
        {

        }

        public struct CBuffer
        {
        }

        public class ShaderProgram : ShaderPipeline<Vertex, CBuffer>.IFragmentShader, ShaderPipeline<Vertex, CBuffer>.IVertexShader
        {
            public Vector4 FragmentMain(FragmentData fragData, in ShaderPipeline<Vertex, CBuffer>.TextureSampler Sampler,
                in CBuffer constantBuffer)
            {
                return fragData.Vector4s[0];
            }

            public VertexData VertexMain(Vertex vertexDat, in CBuffer constantBuffer)
            {
                VertexData ret = VertexData.Default();
                ret.Vector4s.Add(vertexDat.Color);
                ret.Position = new Vector4(vertexDat.Position.X, vertexDat.Position.Y, vertexDat.Position.Z, 1);
                return ret;
            }
        }
    }
}
