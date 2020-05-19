using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CPUShaders
{
    public class SoftwareRasterizer
    {
        public static List<FragmentData> Rasterize(VertexData V0, VertexData V1, VertexData V2, float screenHeight, float screenWidth)
        { 
            List<FragmentData> ret = new List<FragmentData>();

            //calculate NDC coordinates
            Vector3 V0P = new Vector3(V0.Position.X / V0.Position.W, V0.Position.Y / V0.Position.W, V0.Position.Z / V0.Position.W);
            Vector3 V1P = new Vector3(V1.Position.X / V1.Position.W, V1.Position.Y / V1.Position.W, V1.Position.Z / V1.Position.W);
            Vector3 V2P = new Vector3(V2.Position.X / V2.Position.W, V2.Position.Y / V2.Position.W, V2.Position.Z / V2.Position.W);

            //discard triangles who are facing away from the camera (facing in same direction as camera is facing)
            if (Vector3.Dot(Vector3.Cross(V0P - V2P, V0P - V1P), Vector3.UnitZ) > 0)
                return ret;

            //calculate 2d coords
            Vector2 V02 = new Vector2(V0P.X, V0P.Y);
            Vector2 V12 = new Vector2(V1P.X, V1P.Y);
            Vector2 V22 = new Vector2(V2P.X, V2P.Y);

            //Rasterize
            Vector2 min = new Vector2(V0P.X, V0P.Y);
            Vector2 max = new Vector2(V0P.X, V0P.Y);

            //find Triangle bounds
            if (V1P.X < min.X)
                min.X = V1P.X;
            else if (V1P.X > max.X)
                max.X = V1P.X;
            if (V1P.Y < min.Y)
                min.Y = V1P.Y;
            else if (V1P.Y > max.Y)
                max.Y = V1P.Y;

            if (V2P.X < min.X)
                min.X = V2P.X;
            else if (V2P.X > max.X)
                max.X = V2P.X;
            if (V2P.Y < min.Y)
                min.Y = V2P.Y;
            else if (V2P.Y > max.Y)
                max.Y = V2P.Y;

            //bound the min and max to screen space (clip)
            if (min.X < -1)
                min.X = -1;
            if (min.X > 1)
                return ret;
            if (max.X > 1)
                max.X = 1;
            if (max.X < -1)
                return ret;

            if (min.Y < -1)
                min.Y = -1;
            if (min.Y > 1)
                return ret;
            if (max.Y > 1)
                max.Y = 1;
            if (max.Y < -1)
                return ret;
            //scale coords to screen space and shift
            min.X *= screenWidth / 2;
            min.Y *= screenHeight / 2;

            max.X *= screenWidth / 2;
            max.Y *= screenHeight / 2;

            V0P.X *= screenWidth / 2;
            V0P.Y *= screenHeight / 2;

            V1P.X *= screenWidth / 2;
            V1P.Y *= screenHeight / 2;

            V2P.X *= screenWidth / 2;
            V2P.Y *= screenHeight / 2;

            V02.X *= screenWidth / 2;
            V02.Y *= screenHeight / 2;

            V12.X *= screenWidth / 2;
            V12.Y *= screenHeight / 2;

            V22.X *= screenWidth / 2;
            V22.Y *= screenHeight / 2;

            min.X += screenWidth / 2;
            min.Y += screenHeight / 2;

            max.X += screenWidth / 2;
            max.Y += screenHeight / 2;

            V0P.X += screenWidth / 2;
            V0P.Y += screenHeight / 2;

            V1P.X += screenWidth / 2;
            V1P.Y += screenHeight / 2;

            V2P.X += screenWidth / 2;
            V2P.Y += screenHeight / 2;

            V02.X += screenWidth / 2;
            V02.Y += screenHeight / 2;

            V12.X += screenWidth / 2;
            V12.Y += screenHeight / 2;

            V22.X += screenWidth / 2;
            V22.Y += screenHeight / 2;

            int width = (int)(max.X - min.X);
            int height = (int)(max.Y - min.Y);
            int offsetX = (int)min.X;
            int offsetY = (int)min.Y;
            FragmentData?[] data = new FragmentData?[width * height];

            //run fragment creation per pixel possible
            Parallel.For(0, height, (y) =>
             {
                 Parallel.For(0, width, (x) =>
                 {
                     FragmentData? datA = AsyncTask(x + offsetX, y + offsetY, ref V0P, ref V1P, ref V2P, ref V02, ref V12, ref V22,
                            ref V0, ref V1, ref V2);
                     data[(y * width) + x] = datA;
                 });
             });

            foreach (FragmentData? frag in data)
                if (frag.HasValue)
                    ret.Add(frag.Value);

            return ret;
        }



        private static FragmentData? AsyncTask(int x, int y, ref Vector3 V0P, ref Vector3 V1P, ref Vector3 V2P,
            ref Vector2 V02, ref Vector2 V12, ref Vector2 V22, ref VertexData V0, ref VertexData V1, ref VertexData V2)
        {
            Vector2 pos;
            Vector3 BaryCoord;
            //check if pixel is to be ignored or not

            //conservative rasterization only rasterizes if center of fragment is in the triangle
            pos = new Vector2(x + .5f, y + .5f);
            BaryCoord = BaryCentricCoord(pos, V0P, V1P, V2P);

            //discard fragments which do not have their centers intersect the triangle
            if (!InBounds(BaryCoord.X, 0, 1) || !InBounds(BaryCoord.Y, 0, 1) || !InBounds(BaryCoord.Z, 0, 1))
            {
                return null;
            }
            //interpolate vertex data into fragment data
            FragmentData frag = InterpolateData(V0, V1, V2, BaryCoord);
            //interpolate z position and store x and y
            frag.Position = new Vector3(x, y, V0P.Z * BaryCoord.X + V1P.Z * BaryCoord.Y + V2P.Z * BaryCoord.Z);
            //save fragment (order is not important)
            return frag;
        }

        /// <summary>
        /// project a point onto a line
        /// </summary>
        /// <param name="line1">point 1 in the line</param>
        /// <param name="line2">point 2 in the line</param>
        /// <param name="toProject">point being projected</param>
        /// <returns>projected point</returns>
        private static Vector2 Project(Vector2 line1, Vector2 line2, Vector2 toProject)
        {
            float m = (line2.Y - line1.Y) / (line2.X - line1.X);
            float b = line1.Y - (m * line1.X);

            float x = (m * toProject.Y + toProject.X - m * b) / (m * m + 1);
            float y = (m * m * toProject.Y + m * toProject.X + b) / (m * m + 1);

            return new Vector2((int)x, (int)y);
        }

        /// <summary>
        /// determine if a value is between two bounds
        /// </summary>
        /// <param name="val">value to check</param>
        /// <param name="min">low bound</param>
        /// <param name="max">high bound</param>
        /// <returns>if the value is in bounds</returns>
        public static bool InBounds(float val, float min, float max)
        {
            return val <= max && val >= min;
        }

        /// <summary>
        /// Find barycentric coordinates of a point with relation to a trinagle
        /// </summary>
        /// <param name="P">the point</param>
        /// <param name="V0">vertex 1 in the triangle</param>
        /// <param name="V1">vertex 2 in the triangle</param>
        /// <param name="V2">vertex 3 in the triangle</param>
        /// <returns>the barycentric coordinates</returns>
        public static Vector3 BaryCentricCoord(Vector2 P, Vector3 V0, Vector3 V1, Vector3 V2)
        {
            //calculate values used multiple times
            float y12 = (V1.Y - V2.Y);
            float x21 = (V2.X - V1.X);
            float y20 = (V2.Y - V0.Y);
            float x02 = (V0.X - V2.X);

            Vector3 ret =  new Vector3((y12 * (P.X - V2.X) + x21 * (P.Y - V2.Y)) / (y12 * (V0.X - V2.X) + x21 * (V0.Y - V2.Y)),
                (y20 * (P.X - V2.X) + x02 * (P.Y - V2.Y)) / (y20 * (V1.X - V2.X) + x02 * (V1.Y - V2.Y)), 0);
            //return coordinates (x is found since all 3 coordinates must alway sum to 1)
            ret.Z = 1 - ret.X - ret.Y;
            return ret;
        }

        /// <summary>
        /// a 2d cross product (while not mathematically accurate, it functions for our needs)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>a cross product like value</returns>
        public static float Cross (Vector2 a, Vector2 b)
        {
            // just calculate the z-component
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>
        /// Interpolate a Fragment's data using its location with regards to its triangle and the triangle data
        /// </summary>
        /// <param name="V0">vertex data for vertex 1 of the triangle</param>
        /// <param name="V1">vertex data for vertex 2 of the triangle</param>
        /// <param name="V2">vertex data for vertex 3 of the triangle</param>
        /// <param name="Coordinates">barycentric coordinates of the fragment</param>
        /// <returns>interpolated fragment data</returns>
        public static FragmentData InterpolateData(VertexData V0, VertexData V1, VertexData V2, Vector3 Coordinates)
        {
            FragmentData ret = FragmentData.Default();
            //linearly interpolate all data for fragment
            for( int i = 0; i < V0.Booleans.Count; i++)
            {
                float zero = (V0.Booleans[i] ? Coordinates.X : 0),
                    one = (V1.Booleans[i] ? Coordinates.Y : 0),
                    two = (V2.Booleans[i] ? Coordinates.Z : 0);
                ret.Booleans.Add((zero + one + two) >= .5);
            }
            for (int i = 0; i < V0.DoubleFloatingPoints.Count; i++)
                ret.DoubleFloatingPoints.Add((V0.DoubleFloatingPoints[i] * Coordinates.X) +
                    (V1.DoubleFloatingPoints[i] * Coordinates.Y) +
                    (V2.DoubleFloatingPoints[i] * Coordinates.Z));
            for (int i = 0; i < V0.FloatingPoints.Count; i++)
                ret.FloatingPoints.Add((V0.FloatingPoints[i] * Coordinates.X) +
                    (V1.FloatingPoints[i] * Coordinates.Y) +
                    (V2.FloatingPoints[i] * Coordinates.Z));
            for (int i = 0; i < V0.Integer16s.Count; i++)
                ret.Integer16s.Add((short)((V0.Integer16s[i] * Coordinates.X) +
                    (V1.Integer16s[i] * Coordinates.Y) +
                    (V2.Integer16s[i] * Coordinates.Z)));
            for (int i = 0; i < V0.Integer64s.Count; i++)
                ret.Integer64s.Add((long)((V0.Integer64s[i] * Coordinates.X) +
                    (V1.Integer64s[i] * Coordinates.Y) +
                    (V2.Integer64s[i] * Coordinates.Z)));
            for (int i = 0; i < V0.Integers.Count; i++)
                ret.Integers.Add((int)((V0.Integers[i] * Coordinates.X) +
                    (V1.Integers[i] * Coordinates.Y) +
                    (V2.Integers[i] * Coordinates.Z)));
            for (int i = 0; i < V0.Integer8s.Count; i++)
                ret.Integer8s.Add((byte)((V0.Integer8s[i] * Coordinates.X) +
                    (V1.Integer8s[i] * Coordinates.Y) +
                    (V2.Integer8s[i] * Coordinates.Z)));
            for (int i = 0; i < V0.Matricies.Count; i++)
                ret.Matricies.Add((V0.Matricies[i] * Coordinates.X) +
                    (V1.Matricies[i] * Coordinates.Y) +
                    (V2.Matricies[i] * Coordinates.Z));
            for (int i = 0; i < V0.Vector2s.Count; i++)
                ret.Vector2s.Add((V0.Vector2s[i] * Coordinates.X) +
                    (V1.Vector2s[i] * Coordinates.Y) +
                    (V2.Vector2s[i] * Coordinates.Z));
            for (int i = 0; i < V0.Vector3s.Count; i++)
                ret.Vector3s.Add((V0.Vector3s[i] * Coordinates.X) +
                    (V1.Vector3s[i] * Coordinates.Y) +
                    (V2.Vector3s[i] * Coordinates.Z));
            for (int i = 0; i < V0.Vector4s.Count; i++)
                ret.Vector4s.Add((V0.Vector4s[i] * Coordinates.X) +
                    (V1.Vector4s[i] * Coordinates.Y) +
                    (V2.Vector4s[i] * Coordinates.Z));

            return ret;
        }

        /// <summary>
        /// Clear a bitmap to all the same color using multithreading
        /// </summary>
        /// <param name="img">image to clear</param>
        /// <param name="col">color to set</param>
        public static void ClearBitmap(Bitmap img, Color col)
        {
            //save size variable
            int size = img.Width * img.Height;

            //Get Bitmap data and set color data into a byte array
            BitmapData dat = img.LockBits(new Rectangle(0, 0, 800, 600), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] colDat = new byte[size * 4];
            Marshal.Copy(dat.Scan0, colDat, 0, colDat.Length);

            //save image width 
            int width = img.Width;
            Parallel.For(0, img.Height, (y) =>
            {
                //write whole rows in a single thread
                for (int x = 0; x < width; x++)
                {
                    int val = (y * width) + x;
                    val *= 4;
                    colDat[val] = col.B;
                    colDat[val + 1] = col.G;
                    colDat[val + 2] = col.R;
                    colDat[val + 3] = col.A;
                }
            });

            //copy byte array back to bitmap
            Marshal.Copy(colDat, 0, dat.Scan0, colDat.Length);
            img.UnlockBits(dat);
        }

        public static void ClearDepth(float[,] depthBuffer)
        {
            int width = depthBuffer.GetLength(0);
            int height = depthBuffer.GetLength(1);

            Parallel.For(0, height, (y) =>
            {
                //write whole rows in a single thread
                for (int x = 0; x < width; x++)
                {
                    depthBuffer[x, y] = float.PositiveInfinity;
                }
            });
        }
    }
}
