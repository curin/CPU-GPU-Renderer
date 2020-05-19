using SharpDX.DXGI;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GPUShaders
{
    using SharpDX;
    using SharpDX.Direct3D12;
    using SharpDX.Windows;
    public class D3DApp : IDisposable
    {
        protected RenderForm _window;
        protected ViewportF _viewport;
        protected Rectangle _scissorRect;
        protected Device3 _device;
        protected CommandQueue _graphicsQueue;
        protected Stopwatch _gametimer;
        protected double _frameInterval, _lastframe, _framerate;

        protected SwapChain3 _swapChain;
        protected int _frameIndex;
        protected int _adapterIndex;
        public const int WIDTH = 800, HEIGHT = 600, FRAME_COUNT = 3;

        protected DescriptorHeap _depthStencilView;
        protected Resource[] _swapchainBuffer;
        protected ResourceDescription _depthStencilDesc;
        protected ClearValue _depthStencilClear;
        protected Resource _depthStencilBuffer;

        protected Resource[] _renderTargets = new Resource[FRAME_COUNT];
        protected DescriptorHeap _renderTargetViewHeap;

        protected CommandAllocator _commandAllocator;
        protected GraphicsCommandList _commandList;

        protected CommandAllocator _bundleCommandAllocator;
        protected GraphicsCommandList _bundleCommandList;
        protected int _rtvDescriptorSize;

        // Synchronization objects.
        protected AutoResetEvent _fenceEvent;

        protected PipelineState _pipelineState;
        protected RootSignature _rootSignature;
        protected GraphicsResource[] _resources;

        protected Fence _fence;
        protected long _currentFence;

        public RenderForm Window => _window;
        protected CpuDescriptorHandle DepthStencilHandle => _depthStencilView.CPUDescriptorHandleForHeapStart;
        protected System.Windows.Forms.Label _frameLabel;

        public D3DApp(string name, int adapterIndex = 0)
        {
            _window = new RenderForm(name);
            _window.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            _adapterIndex = adapterIndex;
            _gametimer = new Stopwatch();

            _frameLabel = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(10, 10),
                BackColor = System.Drawing.Color.Green,
                ForeColor = System.Drawing.Color.White,
                Size = new System.Drawing.Size(93, 39),
                Font = new System.Drawing.Font("Arial", 25)
            };
            _window.Controls.Add(_frameLabel);
        }

        public void Initialize()
        {
            loadDevice();

            BuildPSOs();

            _commandList.Close();
            _graphicsQueue.ExecuteCommandList(_commandList);

            // Create synchronization objects.
            _fence = _device.CreateFence(0, FenceFlags.None);
            _currentFence = 1;

            // Create an event handle to use for frame synchronization.
            _fenceEvent = new AutoResetEvent(false);
        }

        void loadDevice()
        {
            _resources = new GraphicsResource[0];

            _viewport.Width = WIDTH;
            _viewport.Height = HEIGHT;
            _viewport.MaxDepth = 1.0f;

            _scissorRect.Right = WIDTH;
            _scissorRect.Bottom = HEIGHT;

#if DEBUG
            // Enable the D3D12 debug layer.
            {
                DebugInterface.Get().EnableDebugLayer();
            }
#endif
            
            using (var factory = new Factory4())
            {
                _device = new Device(factory.GetAdapter(_adapterIndex), SharpDX.Direct3D.FeatureLevel.Level_12_1).QueryInterface<Device3>();
                // Describe and create the command queue.
                CommandQueueDescription queueDesc = new CommandQueueDescription(CommandListType.Direct);
                _graphicsQueue = _device.CreateCommandQueue(queueDesc);


                // Describe and create the swap chain.
                SwapChainDescription swapChainDesc = new SwapChainDescription()
                {
                    BufferCount = FRAME_COUNT,
                    ModeDescription = new ModeDescription(WIDTH, HEIGHT, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    Usage = Usage.RenderTargetOutput,
                    SwapEffect = SwapEffect.FlipDiscard,
                    OutputHandle = _window.Handle,
                    Flags = SwapChainFlags.AllowModeSwitch,
                    SampleDescription = new SampleDescription(1, 0),
                    IsWindowed = true
                };

                SwapChain tempSwapChain = new SwapChain(factory, _graphicsQueue, swapChainDesc);
                _swapChain = tempSwapChain.QueryInterface<SwapChain3>();
                tempSwapChain.Dispose();
                _frameIndex = _swapChain.CurrentBackBufferIndex;
            }

            // Create descriptor heaps.
            // Describe and create a render target view (RTV) descriptor heap.
            DescriptorHeapDescription rtvHeapDesc = new DescriptorHeapDescription()
            {
                DescriptorCount = FRAME_COUNT,
                Flags = DescriptorHeapFlags.None,
                Type = DescriptorHeapType.RenderTargetView
            };

            _renderTargetViewHeap = _device.CreateDescriptorHeap(rtvHeapDesc);

            DescriptorHeapDescription _dsvHeapDescription = new DescriptorHeapDescription()
            {
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.None,
                NodeMask = 0,
                Type = DescriptorHeapType.DepthStencilView
            };
            _depthStencilView = _device.CreateDescriptorHeap(_dsvHeapDescription);

            _rtvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);

            // Create frame resources.
            CpuDescriptorHandle rtvHandle = _renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            for (int n = 0; n < FRAME_COUNT; n++)
            {
                _renderTargets[n] = _swapChain.GetBackBuffer<Resource>(n);
                _device.CreateRenderTargetView(_renderTargets[n], null, rtvHandle);
                rtvHandle += _rtvDescriptorSize;
            }

            //Initialize Depth/Stencil Buffer
            _depthStencilDesc = new ResourceDescription(ResourceDimension.Texture2D, 0,
                _window.Width, _window.Height, 1, 1, Format.D24_UNorm_S8_UInt, 1, 0,
                TextureLayout.Unknown, ResourceFlags.AllowDepthStencil);
            _depthStencilClear = new ClearValue()
            {
                DepthStencil = new DepthStencilValue()
                {
                    Depth = 1.0f,
                    Stencil = 0
                },
                Format = Format.D24_UNorm_S8_UInt
            };
            _depthStencilBuffer = _device.CreateCommittedResource(new HeapProperties(HeapType.Default),
                HeapFlags.None, _depthStencilDesc, ResourceStates.Common, _depthStencilClear);

            //Create Descriptor to mip level 0 of the entire resource using format of the resouce
            _device.CreateDepthStencilView(_depthStencilBuffer, null, DepthStencilHandle);

            _commandAllocator = _device.CreateCommandAllocator(CommandListType.Direct);
            _bundleCommandAllocator = _device.CreateCommandAllocator(CommandListType.Bundle);

            // Create the command list.
            _commandList = _device.CreateCommandList(CommandListType.Direct, _commandAllocator, null);
            _bundleCommandList = _device.CreateCommandList(CommandListType.Bundle, _bundleCommandAllocator, null);

            _commandList.ResourceBarrier(new ResourceBarrier(new ResourceTransitionBarrier(_depthStencilBuffer,
                ResourceStates.Common, ResourceStates.DepthWrite)));

            // Command lists are created in the recording state, but there is nothing
            // to record yet. The main loop expects it to be closed, so close it now.
            _bundleCommandList.Close();
        }

        private void PopulateCommandList()
        {
            // Command list allocators can only be reset when the associated 
            // command lists have finished execution on the GPU; apps should use 
            // fences to determine GPU execution progress.
            _commandAllocator.Reset();
            _bundleCommandAllocator.Reset();

            // However, when ExecuteCommandList() is called on a particular command 
            // list, that command list can then be reset at any time and must be before 
            // re-recording.
            _commandList.Reset(_commandAllocator, null);
            _bundleCommandList.Reset(_bundleCommandAllocator, _pipelineState);


            // Set necessary state.
            _commandList.SetGraphicsRootSignature(_rootSignature);

                List<DescriptorHeap> heaps = new List<DescriptorHeap>();
                foreach (GraphicsResource resource in _resources)
                {
                    if (resource.type == ResourceType.DescriptorTable)
                        heaps.Add(resource.Heap);
                }

                if (heaps.Count > 0)
                    _commandList.SetDescriptorHeaps(heaps.ToArray());
                foreach (GraphicsResource resource in _resources)
                {
                    if (resource.type == ResourceType.ConstantBufferView)
                        _commandList.SetGraphicsRootConstantBufferView(resource.Register, resource.Resource.GPUVirtualAddress);
                    else
                        _commandList.SetGraphicsRootDescriptorTable(resource.Register, resource.Heap.GPUDescriptorHandleForHeapStart);
                }
            _commandList.SetViewport(_viewport);
            _commandList.SetScissorRectangles(_scissorRect);

            // Indicate that the back buffer will be used as a render target.
            _commandList.ResourceBarrierTransition(_renderTargets[_frameIndex], ResourceStates.Present, ResourceStates.RenderTarget);


            CpuDescriptorHandle rtvHandle = _renderTargetViewHeap.CPUDescriptorHandleForHeapStart;
            rtvHandle += _frameIndex * _rtvDescriptorSize;
            _commandList.SetRenderTargets(1, rtvHandle, DepthStencilHandle);

            // Record commands.
            _commandList.ClearRenderTargetView(rtvHandle, Color.CornflowerBlue, 0, null);
            _commandList.ClearDepthStencilView(DepthStencilHandle, ClearFlags.FlagsDepth | ClearFlags.FlagsStencil, 1, 0);

            BundleDraw();

            _commandList.ExecuteBundle(_bundleCommandList);

            // Indicate that the back buffer will now be used to present.
            _commandList.ResourceBarrierTransition(_renderTargets[_frameIndex], ResourceStates.RenderTarget, ResourceStates.Present);

            _commandList.Close();
        }


        /// <summary> 
        /// Wait the previous command list to finish executing. 
        /// </summary> 
        private void WaitForPreviousFrame()
        {
            // WAITING FOR THE FRAME TO COMPLETE BEFORE CONTINUING IS NOT BEST PRACTICE. 
            // This is code implemented as such for simplicity. 

            long currentFence = _currentFence;
            _graphicsQueue.Signal(_fence, currentFence);
            _currentFence++;

            // Wait until the previous frame is finished.
            if (_fence.CompletedValue < currentFence)
            {
                _fence.SetEventOnCompletion(currentFence, _fenceEvent.SafeWaitHandle.DangerousGetHandle());
                _fenceEvent.WaitOne();
            }

            _frameIndex = _swapChain.CurrentBackBufferIndex;
        }

        public void Run()
        {
            Initialize();
            Window.Show();
            _gametimer.Start();

            RenderLoop.Run(Window, () =>
            {
                Update();
                Render();
            });
        }

        public virtual void Update()
        {
        }


        public void Render()
        {
            // Record all the commands we need to render the scene into the command list.
            PopulateCommandList();

            // Execute the command list.
            _graphicsQueue.ExecuteCommandList(_commandList);

            // Present the frame.
            _swapChain.Present(0, 0);

            WaitForPreviousFrame();

            //Get FrameInterval and Framerate
            _frameInterval = _gametimer.Elapsed.TotalSeconds - _lastframe;
            _framerate = 1 / (_gametimer.Elapsed.TotalSeconds - _lastframe);
            _lastframe = _gametimer.Elapsed.TotalSeconds;
            if (_gametimer.Elapsed.TotalMilliseconds % 10 <= 5)
            {
                _frameLabel.ForeColor = System.Drawing.Color.White;
                _frameLabel.Text = ((int)(_currentFence / _gametimer.Elapsed.TotalSeconds)).ToString();

                if (_frameLabel.Text == "0")
                {
                    _frameLabel.ForeColor = System.Drawing.Color.Red;
                    _frameLabel.Text = ((int)(_gametimer.Elapsed.TotalSeconds / _currentFence)).ToString();
                }
            }
        }

        protected virtual void BundleDraw()
        {

        }

        protected virtual void BuildPSOs()
        {

        }

        public void Dispose()
        {
            // Wait for the GPU to be done with all resources.
            WaitForPreviousFrame();

            foreach (var target in _renderTargets)
            {
                target.Dispose();
            }
            _commandAllocator.Dispose();
            _graphicsQueue.Dispose();
            _rootSignature.Dispose();
            _renderTargetViewHeap.Dispose();
            _pipelineState.Dispose();
            _commandList.Dispose();
            _fence.Dispose();
            _swapChain.Dispose();
            _device.Dispose();
        }
    }

    public struct GraphicsResource
    {
        public DescriptorHeap Heap;
        public Resource Resource;
        public ResourceType type;
        public int Register;
    }

    public enum ResourceType
    {
        DescriptorTable,
        ConstantBufferView
    }
}
