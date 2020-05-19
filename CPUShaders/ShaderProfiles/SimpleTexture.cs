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
    public class SimpleTexture : IShaderProfile
    {
        ShaderPipeline<Vertex, CBuffer> _pipeline;
        Vertex[] vertexBuffer;
        int[] indexBuffer;
        CBuffer buffer;

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public string Name => "Simple - Texture";

        C3DApp _app;
        public C3DApp Application { get => _app; set => _app = value; }

        public SimpleTexture(C3DApp app)
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
            _pipeline.LoadTexture(new Bitmap("koala.png"));
            vertexBuffer = new Vertex[4]
            {
                new Vertex() { Position = new Vector3(-.5f, -.5f, .5f), TexCoord = new Vector2(1, 1) },
                new Vertex() { Position = new Vector3(-.5f, .5f, .5f), TexCoord = new Vector2(1, 0) },
                new Vertex() { Position = new Vector3(.5f, -.5f, .5f), TexCoord = new Vector2(0, 1) },
                new Vertex() { Position = new Vector3(.5f, .5f, .5f), TexCoord = new Vector2(0, 0) }
            };
            indexBuffer = new int[] { 0,1,2,
                                      3,2,1};
        }

        public void Update(double frameInterval)
        {

        }

        public struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
        }

        public struct CBuffer
        {
        }

        public class ShaderProgram : ShaderPipeline<Vertex, CBuffer>.IFragmentShader, ShaderPipeline<Vertex, CBuffer>.IVertexShader
        {
            public Vector4 FragmentMain(FragmentData fragData, in ShaderPipeline<Vertex, CBuffer>.TextureSampler Sampler,
                in CBuffer constantBuffer)
            {
                return Sampler.Sample(0, fragData.Vector2s[0]);
            }

            public VertexData VertexMain(Vertex vertexDat, in CBuffer constantBuffer)
            {
                VertexData ret = VertexData.Default();
                ret.Vector2s.Add(vertexDat.TexCoord);
                ret.Position = new Vector4(vertexDat.Position.X, vertexDat.Position.Y, vertexDat.Position.Z, 1);
                return ret;
            }
        }
    }
}
