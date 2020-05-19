using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CPUShaders
{
    public class ShaderPipeline<VertexIn, CBuffer>
    {
        public ShaderPipeline(IVertexShader vertexShader, IFragmentShader fragmentShader)
        {
            VertexShader = vertexShader;
            FragmentShader = fragmentShader;
        }

        internal List<TextureData> _loadedTextures = new List<TextureData>();

        public IVertexShader VertexShader;
        public IFragmentShader FragmentShader;

        VertexData[] verts;
        object[,] depthLock;

        public void Run(VertexIn[] vertexBuffer, int[] indexBuffer, CBuffer constantBuffer, Bitmap outputBuffer, float[,] depthBuffer)
        {
            TextureSampler sampler = new TextureSampler()
            {
                _pipeline = this
            };
            //initialize variables
            if (verts == null || verts.Length == vertexBuffer.Length)
                verts = new VertexData[vertexBuffer.Length];

            if (depthBuffer != null)
            {
                if (depthLock == null || depthLock.GetLength(0) != outputBuffer.Width || depthLock.GetLength(1) != outputBuffer.Height)
                    depthLock = new object[depthBuffer.GetLength(0), depthBuffer.GetLength(0)];

                for (int x = 0; x < outputBuffer.Width; x++)
                    for (int y = 0; y < outputBuffer.Height; y++)
                    {
                        depthLock[x, y] = new object();
                    }
            }

            //Create tasks and process vertices;
            Parallel.For(0, vertexBuffer.Length, (i) =>
            {
                verts[i] = VertexShader.VertexMain(vertexBuffer[i], in constantBuffer);
                verts[i].Position.Y = -verts[i].Position.Y;
            });

            int size = outputBuffer.Width * outputBuffer.Height;
            BitmapData dat = outputBuffer.LockBits(new Rectangle(0, 0, 800, 600), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] colDat = new byte[size * 4];
            Marshal.Copy(dat.Scan0, colDat, 0, colDat.Length);
            int width = outputBuffer.Width;
            int height = outputBuffer.Height;

            //Create tasks and process data to output pixels
            Parallel.For(0, indexBuffer.Length/3, (i) =>
            {
                int i1 = i * 3;
                //Assemble geometry and rasterize triangles
                List<FragmentData> frags = SoftwareRasterizer.Rasterize(verts[indexBuffer[i1]], verts[indexBuffer[i1 + 1]],
                        verts[indexBuffer[i1 + 2]], height, width);


                Parallel.For(0, frags.Count, (j) =>
                {
                    //get fragment
                    FragmentData frag = frags[j];
                    //get pixel color
                    Vector4 pixel = FragmentShader.FragmentMain(frag, in sampler, in constantBuffer);

                    //store coord into ints for speed
                    int x = (int)frag.Position.X, y = (int)frag.Position.Y;

                    //don't do depth test if no buffer is given
                    if (depthBuffer == null)
                    {
                        lock (depthLock[x, y])
                        {
                            int index = (y * width) + x;
                            index *= 4;
                            colDat[index] = (byte)(pixel.Z * 255);
                            colDat[index + 1] = (byte)(pixel.Y * 255);
                            colDat[index + 2] = (byte)(pixel.X * 255);
                            colDat[index + 3] = (byte)(pixel.W * 255);
                        }
                    }
                    else
                    {
                        //check depth buffer and set output if necessary
                        //lock for thread safety
                        lock (depthLock[x, y])
                        {
                            //check if our pixel is nearer than nearest pixel previously drawn
                            float depth = depthBuffer[x, y];
                            if (frag.Position.Z <= depth)
                            {
                                //if our pixel is closest, set depthbuffer to our depth 
                                depthBuffer[x, y] = frag.Position.Z;
                                //set outputBuffer color
                                int index = (y * width) + x;
                                index *= 4;
                                colDat[index] = (byte)(pixel.Z * 255);
                                colDat[index + 1] = (byte)(pixel.Y * 255);
                                colDat[index + 2] = (byte)(pixel.X * 255);
                                colDat[index + 3] = (byte)(pixel.W * 255);
                            }
                        }
                    }
                });
            });

            Marshal.Copy(colDat, 0, dat.Scan0, colDat.Length);
            outputBuffer.UnlockBits(dat);
        }

        public void LoadTexture(Bitmap texture)
        {
            TextureData data = new TextureData();
            data.Width = texture.Width;
            data.Height = texture.Height;

            BitmapData dat = texture.LockBits(new Rectangle(0, 0, data.Width, data.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            data.Data = new byte[data.Height * data.Width * 4];
            Marshal.Copy(dat.Scan0, data.Data, 0, data.Data.Length);

            _loadedTextures.Add(data);
        }

        /// <summary>
        /// the interface for a Vertex Shader Program
        /// ONLY USE METHOD VARIABLES IN THE METHODS
        /// If you read an exterior variable, don't write to it
        /// also if you write to an exterior variable, don't read it
        /// </summary>
        public interface IVertexShader
        {
            VertexData VertexMain(VertexIn vertexDat, in CBuffer constantBuffer);
        }

        /// <summary>
        /// the interface for a Fragment Shader Program
        /// ONLY USE METHOD VARIABLES IN THE METHODS
        /// If you read an exterior variable, don't write to it
        /// also if you write to an exterior variable, don't read it
        /// </summary>
        public interface IFragmentShader
        {
            Vector4 FragmentMain(FragmentData fragData, in TextureSampler Sampler, in CBuffer constantBuffer);
        }

        public class TextureSampler
        {
            internal ShaderPipeline<VertexIn, CBuffer> _pipeline;

            public Vector4 Sample(int textureIndex, Vector2 textureCoords)
            {
                TextureData dat = _pipeline._loadedTextures[textureIndex];
                int location = (((int)(textureCoords.Y * dat.Height) * dat.Width) + (int)(textureCoords.X * dat.Width)) * 4;
                if (location > dat.Data.Length - 5)
                    location = dat.Data.Length - 5;
                Vector4 vector = new Vector4()
                {
                    Z = dat.Data[location] / 255f,
                    Y = dat.Data[location + 1] / 255f,
                    X = dat.Data[location + 2] / 255f,
                    W = dat.Data[location + 3] / 255f
                };
                return vector;
            }
        }

        internal struct TextureData
        {
            public int Width;
            public int Height;
            public byte[] Data;
        }
    }
}
