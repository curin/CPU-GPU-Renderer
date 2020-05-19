using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CPUShaders
{
    public interface IShaderProfile
    {
        long Fence { get; set; }
        Stopwatch Watch { get; set; }
        string Name { get; }
        C3DApp Application { get; set; }
        void Initialize();
        void Draw();
        void Update(double frameInterval);
    }
}
