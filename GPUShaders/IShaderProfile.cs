using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GPUShaders
{
    using SharpDX.Direct3D12;
    public interface IShaderProfile
    {
        string Name { get; }
        long Fence { get; set; }
        Stopwatch Watch { get; set; }
        GraphicsResource[] Resources { get; }
        void BuildPSO(Device3 device, GraphicsCommandList commandList);
        PipelineState PipelineState { get; }
        RootSignature RootSignature { get; }
        void BundleDraw(GraphicsCommandList bundleList);
        void Update(double frameInterval);
    }
}
