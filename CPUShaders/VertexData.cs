using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CPUShaders
{
    public struct VertexData
    {
        public Vector4 Position;
        public List<Matrix4x4> Matricies;
        public List<Vector2> Vector2s;
        public List<Vector3> Vector3s;
        public List<Vector4> Vector4s;
        public List<int> Integers;
        public List<bool> Booleans;
        public List<float> FloatingPoints;
        public List<byte> Integer8s;
        public List<short> Integer16s;
        public List<long> Integer64s;
        public List<double> DoubleFloatingPoints;

        public static VertexData Default()
        {
            return new VertexData()
            {
                Booleans = new List<bool>(),
                DoubleFloatingPoints = new List<double>(),
                FloatingPoints = new List<float>(),
                Integer16s = new List<short>(),
                Integer64s = new List<long>(),
                Integer8s = new List<byte>(),
                Integers = new List<int>(),
                Matricies = new List<Matrix4x4>(),
                Vector2s = new List<Vector2>(),
                Vector3s = new List<Vector3>(),
                Vector4s = new List<Vector4>()
            };
        }
    }
}
