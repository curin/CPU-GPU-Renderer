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
    public class TwoFold : IShaderProfile
    {
        ShaderPipeline<Vertex, CBuffer> _pipeline;
        Vertex[] vertexBuffer;
        int[] indexBuffer;
        CBuffer buffer;
        Matrix4x4 world1, world2, view, projection;
        Matrix4x4 WVP1, WVP2;

        public long Fence { get; set; }
        public Stopwatch Watch { get; set; }

        public string Name => "TwoFold";

        C3DApp _app;
        public C3DApp Application { get => _app; set => _app = value; }

        public TwoFold(C3DApp app)
        {
            _app = app;
        }

        public void Draw()
        {
            SoftwareRasterizer.ClearBitmap(_app.CurrentSwapchainBuffer, Color.CornflowerBlue);
            SoftwareRasterizer.ClearDepth(_app.CurrentDepthBuffer);

            buffer.WVP = WVP1;
            _pipeline.Run(vertexBuffer, indexBuffer, buffer, _app.CurrentSwapchainBuffer, _app.CurrentDepthBuffer);

            buffer.WVP = WVP2;
            _pipeline.Run(vertexBuffer, indexBuffer, buffer, _app.CurrentSwapchainBuffer, _app.CurrentDepthBuffer);
        }

        public void Initialize()
        {
            ShaderProgram prog = new ShaderProgram();
            _pipeline = new ShaderPipeline<Vertex, CBuffer>(prog, prog);
            _pipeline.LoadTexture(new Bitmap("koala.png"));
            vertexBuffer = new Vertex[24]
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

            world1 = Matrix4x4.CreateTranslation(new Vector3(-2.5f, -2.5f, -2.5f));
            projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 3, _app.CurrentSwapchainBuffer.Width / _app.CurrentSwapchainBuffer.Height,
                1, 1000);

            buffer.GlobalAmbient = new Vector3(1, 1, .8f);
            buffer.Ka = new Vector3(.1f, .1f, .1f);
            buffer.Kd = new Vector3(.5f, .5f, .5f);
            buffer.Ke = new Vector3(.25f, .25f, .25f);
            buffer.Ks = new Vector3(.5f, .5f, .5f);
            buffer.LightColor = new Vector3(1, 1, 1);
            buffer.LightPosition = new Vector3(10, 10, 10);
            buffer.Shininess = 5;
        }


        double rotation = 0;
        public void Update(double frameInterval)
        {
            rotation += frameInterval * .1;
            buffer.EyePosition = new Vector3(15 * (float)Math.Sin(rotation), 7.5f, 15 * (float)Math.Cos(rotation));
            world2 = world1 * Matrix4x4.CreateTranslation(new Vector3(5 * (float)Math.Sin(-rotation), 5f, 5 * (float)Math.Cos(-rotation)));
            view = Matrix4x4.CreateLookAt(buffer.EyePosition, Vector3.Zero, Vector3.UnitY);
            WVP1 = world1 * view * projection;
            WVP2 = world2 * view * projection;
        }

        public struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public Vector3 Normal;
        }

        public struct CBuffer
        {
            public Matrix4x4 WVP;
            public Vector3 GlobalAmbient;
            public Vector3 LightColor;
            public Vector3 LightPosition;
            public Vector3 EyePosition;
            public Vector3 Ke;
            public Vector3 Ka;
            public Vector3 Kd;
            public Vector3 Ks;
            public float Shininess;
        }

        public class ShaderProgram : ShaderPipeline<Vertex, CBuffer>.IFragmentShader, ShaderPipeline<Vertex, CBuffer>.IVertexShader
        {
            public Vector4 FragmentMain(FragmentData fragData, in ShaderPipeline<Vertex, CBuffer>.TextureSampler Sampler,
                in CBuffer constantBuffer)
            {
                Vector3 Position = fragData.Vector3s[0];
                Vector3 Normal = fragData.Vector3s[1];

                //calculate ambient and emissive light
                Vector3 Ambient = constantBuffer.Ka * constantBuffer.GlobalAmbient;
                Vector3 Emissive = constantBuffer.Ke;

                //calculate diffuse light
                Vector3 L = Vector3.Normalize(constantBuffer.LightPosition - Position);
                float DiffuseLight = Math.Max(Vector3.Dot(Normal, L), 0);
                Vector3 Diffuse = constantBuffer.Kd * constantBuffer.LightColor * DiffuseLight;

                //calculate specular light
                Vector3 V = Vector3.Normalize(constantBuffer.EyePosition - Position);
                Vector3 H = Vector3.Normalize(L + V);
                float SpecularLight = (float)Math.Pow(Math.Max(Vector3.Dot(Normal, H), 0), constantBuffer.Shininess);
                if (DiffuseLight <= 0) SpecularLight = 0;
                Vector3 Specular = constantBuffer.Ks * constantBuffer.LightColor * SpecularLight;

                return new Vector4(Emissive + Ambient + Diffuse + Specular, 1)
                    * Sampler.Sample(0, fragData.Vector2s[0])
                    ;
            }

            public VertexData VertexMain(Vertex vertexDat, in CBuffer constantBuffer)
            {
                VertexData ret = VertexData.Default();
                ret.Vector3s.Add(vertexDat.Position);
                ret.Vector3s.Add(vertexDat.Normal);
                ret.Vector2s.Add(vertexDat.TexCoord);
                ret.Position = Vector4.Transform(vertexDat.Position, constantBuffer.WVP);
                return ret;
            }
        }
    }
}
