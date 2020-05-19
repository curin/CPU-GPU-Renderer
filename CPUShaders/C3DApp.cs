using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

using SharpDX.Windows;

namespace CPUShaders
{
    public class C3DApp
    {
        protected Stopwatch _gametimer;
        protected double _lastframe, _frameInterval, _framerate;
        protected long _currentFence;
        protected GameWindow _window;
        protected Bitmap[] _swapchainBuffers;
        protected float[][,] _depthBuffers;
        protected int _currentBuffer = 0;
        public const int SWAPCHAIN_BUFFER_COUNT = 3;

        public Bitmap CurrentSwapchainBuffer => _swapchainBuffers[_currentBuffer];
        public float[,] CurrentDepthBuffer => _depthBuffers[_currentBuffer];

        public C3DApp()
        {
            _gametimer = new Stopwatch();
            _window = new GameWindow();
            _window.FrameLabel.Size = new System.Drawing.Size(93, 39);
        }

        void initializeResources()
        {
            _swapchainBuffers = new Bitmap[SWAPCHAIN_BUFFER_COUNT];
            _depthBuffers = new float[SWAPCHAIN_BUFFER_COUNT][,];

            for (int i =0; i < SWAPCHAIN_BUFFER_COUNT; i++)
            {
                _swapchainBuffers[i] = new Bitmap(800, 600);
                _depthBuffers[i] = new float[800, 600];

                SoftwareRasterizer.ClearBitmap(_swapchainBuffers[i], Color.CornflowerBlue);
                SoftwareRasterizer.ClearDepth(_depthBuffers[i]);
            }
        }

        void Init()
        {
            initializeResources();
        }

        public void Run()
        {
            Init();

            Initialize();

            LoadContent();

            _gametimer.Restart();
            _gametimer.Start();
            _lastframe = 0;
            _currentFence = 1;
            _currentBuffer = 0;

            RenderLoop.Run(_window, () =>
            {
                GameLoop();
            });

            UnloadContent();

            Dispose();
        }

        /// <summary>
        /// Increment buffer index
        /// </summary>
        public void IncrementBufferCounter() { _currentBuffer = (_currentBuffer + 1) % SWAPCHAIN_BUFFER_COUNT; }

        void GameLoop()
        {
            Update();

            Draw();

            present();

            _currentFence++;
            IncrementBufferCounter();

            //Get FrameInterval and Framerate
            _frameInterval = _gametimer.Elapsed.TotalSeconds - _lastframe;
            _framerate = 1 / (_gametimer.Elapsed.TotalSeconds - _lastframe);
            _lastframe = _gametimer.Elapsed.TotalSeconds;

            _window.FrameLabel.ForeColor = Color.White;
            _window.FrameLabel.Text = ((int)(_currentFence / _gametimer.Elapsed.TotalSeconds)).ToString();

            if (_window.FrameLabel.Text == "0")
            {
                _window.FrameLabel.ForeColor = Color.Red;
                _window.FrameLabel.Text = ((int)(_gametimer.Elapsed.TotalSeconds / _currentFence)).ToString();
            }
        }

        void present()
        {
            _window.FrameImage.Image = CurrentSwapchainBuffer;
        }

        /// <summary>
        /// Overloadable Initialize function
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Overloadable LoadContent function
        /// </summary>
        protected virtual void LoadContent() { }

        /// <summary>
        /// Overloadable Update function
        /// </summary>
        protected virtual void Update() { }

        /// <summary>
        /// Overloadable Draw function
        /// </summary>
        protected virtual void Draw() { }

        /// <summary>
        /// Overloadable UnloadContent function
        /// </summary>
        protected virtual void UnloadContent() { }

        /// <summary>
        /// Overloadable Dispose function
        /// </summary>
        protected virtual void Dispose() { }
    }
}
