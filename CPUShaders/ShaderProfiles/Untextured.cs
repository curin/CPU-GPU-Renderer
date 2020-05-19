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
    public class Untextured : IShaderProfile
    {
        ShaderPipeline<Vertex, CBuffer> _pipeline;
        Vertex[] vertexBuffer;
        int[] indexBuffer;
        CBuffer buffer;
        Matrix4x4 world, view, projection;

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public string Name => "Untextured";

        C3DApp _app;
        public C3DApp Application { get => _app; set => _app = value; }

        public Untextured(C3DApp app)
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
            vertexBuffer = new Vertex[24]
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

            indexBuffer = new int[] { 2,1,0,
                                      1,2,3,
                                      4,5,6,
                                      7,6,5,

                                      8,9,10,
                                      11,10,9,
                                      14,13,12,
                                      13,14,15,

                                      16,17,18,
                                      19,18,17,
                                      22,21,20,
                                      21,22,23};
            world = Matrix4x4.CreateTranslation(new Vector3(-2.5f, -2.5f, -2.5f));
            projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI/3, (float)_app.CurrentSwapchainBuffer.Width / _app.CurrentSwapchainBuffer.Height,
                1, 1000);
        }


        double rotation;
        public void Update(double frameInterval)
        {
            rotation += frameInterval * .1;
            view = Matrix4x4.CreateLookAt(new Vector3(10 * (float)Math.Sin(rotation), 5, 10 * (float)Math.Cos(rotation)), Vector3.Zero, Vector3.UnitY);
            buffer.WVP = world * view * projection;
        }

        public struct Vertex
        {
            public Vector3 Position;
            public Vector4 Color;
        }

        public struct CBuffer
        {
            public Matrix4x4 WVP;
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
                ret.Position = Vector4.Transform(vertexDat.Position, constantBuffer.WVP);
                return ret;
            }
        }
    }
}
