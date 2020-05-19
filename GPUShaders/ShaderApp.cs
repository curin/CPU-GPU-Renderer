using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using GPUShaders.ShaderProfiles;

namespace GPUShaders
{
    public class ShaderApp : D3DApp
    {
        List<IShaderProfile> _profiles = new List<IShaderProfile>();
        int _activeProfile = -1;
        long _fence;
        Stopwatch _timer;
        System.Windows.Forms.Label ControlLabel;
        bool _testing = false, _done = false;
        List<TimingInfo> _timingData = new List<TimingInfo>();

        public ShaderApp(string name, int adapterIndex = 0) : base("0. Empty", adapterIndex)
        {
            _window.KeyPress += _window_KeyPress;

            ControlLabel = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(25, 550),
                BackColor = System.Drawing.Color.Green,
                ForeColor = System.Drawing.Color.White,
                Size = new System.Drawing.Size(715, 30),
                Font = new System.Drawing.Font("Arial", 20),
                Text = "Press x For Next, z for Previous, d to Hide/Show Interface"
            };
            _window.Controls.Add(ControlLabel);
        }

        private void _window_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            ProfileMove(char.ToLower(e.KeyChar) == 'x', char.ToLower(e.KeyChar) == 'z');
            if (char.ToLower(e.KeyChar) == 'd')
            {
                _frameLabel.Visible = !_frameLabel.Visible;
                ControlLabel.Visible = !ControlLabel.Visible;
            }
        }

        void ProfileMove(bool increase, bool decrease)
        {
            StopProfile();
            if (increase)
            {
                NextProfile();
            }
            if (decrease)
            {
                PreviousProfile();
            }
            SetProfileData();
        }

        void NextProfile()
        {
            _activeProfile++;
            if (_activeProfile == _profiles.Count)
            {
                _activeProfile = -1;
            }
        }

        void PreviousProfile()
        {
            _activeProfile--;
            if (_activeProfile < -1)
            {
                _activeProfile = _profiles.Count - 1;
            }
        }
        
        void StopProfile()
        {
            _gametimer.Stop();
            if (_activeProfile == -1)
                _fence = _currentFence;
            else
                _profiles[_activeProfile].Fence = _currentFence;
        }

        void SetProfileData()
        {
            if (_activeProfile != -1)
            {
                _gametimer = _profiles[_activeProfile].Watch;
                _currentFence = _profiles[_activeProfile].Fence;
                _window.Text = (_activeProfile + 1).ToString() + ". " + _profiles[_activeProfile].Name;
                _rootSignature = _profiles[_activeProfile].RootSignature;
                _pipelineState = _profiles[_activeProfile].PipelineState;
                _resources = _profiles[_activeProfile].Resources;
            }
            else
            {
                _gametimer = _timer;
                _currentFence = _fence;
                _window.Text = "0. Empty";
                _rootSignature = null;
                _pipelineState = null;
                _resources = new GraphicsResource[0];
            }
            _gametimer.Start();
        }

        protected override void BuildPSOs()
        {
            _profiles.Add(new Simple());
            _profiles.Add(new SimpleColors());
            _profiles.Add(new SimpleTexture());
            _profiles.Add(new AtlasWalk());

            _profiles.Add(new Untextured());
            _profiles.Add(new Unlit());
            _profiles.Add(new LitVertex());
            _profiles.Add(new LitPixel());

            _profiles.Add(new Twofold());

            base.BuildPSOs();
            foreach (IShaderProfile profile in _profiles)
            {
                profile.BuildPSO(_device, _commandList);
                profile.Watch = new Stopwatch();
                profile.Fence = 1;
            }

            _timer = _gametimer;
        }

        protected override void BundleDraw()
        {
            base.BundleDraw();
            _bundleCommandList.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            if (_activeProfile != -1)
            {
                _bundleCommandList.SetGraphicsRootSignature(_profiles[_activeProfile].RootSignature);
                _profiles[_activeProfile].BundleDraw(_bundleCommandList);
            }

            _bundleCommandList.Close();
        }

        public override void Update()
        {
            if (!_done)
            {
                if (_testing)
                {
                    _timingData.Add(new TimingInfo(_currentFence, _gametimer.Elapsed));

                    if (_gametimer.Elapsed.TotalSeconds >= 60)
                    {
                        _gametimer.Stop();
                        StreamWriter write = new StreamWriter(_window.Text + ".csv", false);
                        foreach (TimingInfo info in _timingData)
                        {
                            write.WriteLine(info.Frame + "," + info.ElapsedTime.TotalSeconds + ",");
                        }
                        write.Close();
                        ProfileMove(true, false);
                        _testing = false;
                        if (_activeProfile == -1)
                            _done = true;
                        _gametimer.Stop();
                        _gametimer.Reset();
                        _gametimer.Start();
                    }
                }

                if (!_testing && _gametimer.Elapsed.TotalSeconds >= 120)
                {
                    _currentFence = 0;
                    _gametimer.Stop();
                    _gametimer.Reset();
                    _gametimer.Start();
                    _testing = true;
                    _timingData.Clear();
                }
            }

            if (_activeProfile != -1)
            {
                _profiles[_activeProfile].Update(_frameInterval);
            }
            base.Update();
        }

        struct TimingInfo
        {
            public TimingInfo(long frame, TimeSpan elapsed)
            {
                Frame = frame;
                ElapsedTime = elapsed;
            }

            public long Frame;
            public TimeSpan ElapsedTime;
        }
    }
}
