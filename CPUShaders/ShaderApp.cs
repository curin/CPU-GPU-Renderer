using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using CPUShaders.ShaderProfiles;

namespace CPUShaders
{
    public class ShaderApp : C3DApp
    {
        
        List<IShaderProfile> _profiles = new List<IShaderProfile>();
        Stopwatch _timer;
        long _fence = 1;
        int _activeProfile = -1;
        bool _testing = false, _done = false;
        List<TimingInfo> _timingData = new List<TimingInfo>();

        protected override void Initialize()
        {
            _window.KeyPress += Window_KeyPress;
            _profiles.Add(new Simple(this));
            _profiles.Add(new SimpleColors(this));
            _profiles.Add(new SimpleTexture(this));
            _profiles.Add(new AtlasWalk(this));

            _profiles.Add(new Untextured(this));
            _profiles.Add(new Unlit(this));
            _profiles.Add(new LitVertex(this));
            _profiles.Add(new LitPixel(this));

            _profiles.Add(new TwoFold(this));

            foreach (IShaderProfile profile in _profiles)
            {
                profile.Watch = new Stopwatch();
                profile.Watch.Restart();
                profile.Initialize();
            }

            _timer = _gametimer;
            _window.Text = "0. Empty";
            base.Initialize();
        }

        protected override void Update()
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

            if (_activeProfile > -1)
                _profiles[_activeProfile].Update(_framerate);
            base.Update();
        }

        private void Window_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            ProfileMove(char.ToLower(e.KeyChar) == 'x', char.ToLower(e.KeyChar) == 'z');
            if (char.ToLower(e.KeyChar) == 'd')
            {
                _window.FrameLabel.Visible = !_window.FrameLabel.Visible;
                _window.ControlLabel.Visible = !_window.ControlLabel.Visible;
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
            }
            else
            {
                _gametimer = _timer;
                _currentFence = _fence;
                _window.Text = "0. Empty";
            }
            _gametimer.Start();
        }

        protected override void Draw()
        {
            if (_activeProfile > -1)
                _profiles[_activeProfile].Draw();
            else
                SoftwareRasterizer.ClearBitmap(CurrentSwapchainBuffer, System.Drawing.Color.CornflowerBlue);
            base.Draw();
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
