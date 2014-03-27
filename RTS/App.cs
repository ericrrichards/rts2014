﻿namespace RTS {
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Reflection;
    using System.Windows.Forms;

    using log4net;

    using SlimDX;
    using SlimDX.Direct3D9;

    using Font = SlimDX.Direct3D9.Font;

    public class App {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Device _device;
        private Font _font;
        private D3DForm _mainWindow;
        private bool _running;

        private Terrain _terrain;

        private float _angle;
        private float _radius;
        private bool _wireframe;


        public App() {
            _device = null;
            _mainWindow = null;
            _font = null;
            _angle = 0;
            _radius = 0;
            _wireframe = false;
        }

        public Result Init(int width, int height, bool windowed) {
            Log.Info("Application initiated");
            _mainWindow = new D3DForm {
                Text = "Example",
                Name = "D3DWndClassName",
                FormBorderStyle = FormBorderStyle.FixedSingle,
                ClientSize = new Size(width, height),
                StartPosition = FormStartPosition.CenterScreen,
                MyWndProc = WndProc,
            };
            //Cursor.Hide();

            _mainWindow.Show();
            _mainWindow.Update();
            try {
                using (var d3d9 = new Direct3D()) {
                    var caps = d3d9.GetDeviceCaps(0, DeviceType.Hardware);

                    var vp = caps.DeviceCaps.HasFlag(DeviceCaps.HWTransformAndLight) ? 
                        CreateFlags.HardwareVertexProcessing : 
                        CreateFlags.SoftwareVertexProcessing;
                    if (caps.VertexShaderVersion < new Version(2, 0) || caps.PixelShaderVersion < new Version(2, 0)) {
                        Log.Warn("Warning - your graphics card does not support vertex and pixel shaders 2.0");
                    }
                    var d3dpp = new PresentParameters {
                        BackBufferWidth =  width,
                        BackBufferHeight = height,
                        BackBufferFormat = Format.A8R8G8B8,
                        BackBufferCount = 1,
                        Multisample = MultisampleType.None,
                        MultisampleQuality = 0,
                        SwapEffect = SwapEffect.Discard,
                        DeviceWindowHandle = _mainWindow.Handle,
                        Windowed = windowed,
                        EnableAutoDepthStencil = true,
                        AutoDepthStencilFormat = Format.D24S8,
                        FullScreenRefreshRateInHertz = (int)Present.None,
                        PresentFlags = PresentFlags.None,
                        PresentationInterval = PresentInterval.Immediate
                    };
                    try {
                        _device = new Device(d3d9, 0, DeviceType.Hardware, _mainWindow.Handle, vp, d3dpp);
                    } catch (Exception ex) {
                        Log.Error("Failed to create device", ex);
                        return ResultCode.Failure;
                    }
                }
            } catch (Exception ex) {
                Log.Error("Failed to create Direct3D object", ex);
                return ResultCode.Failure;
            }

            _font = new Font(_device, 48, 0, FontWeight.Bold, 1, false, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default|PitchAndFamily.DontCare, "Arial");
            
            MapObject.LoadObjectResources(_device);
            _terrain = new Terrain();
            _terrain.Init(_device, new Point(100,100));
            _radius = 100.0f;

            _device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
            _device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
            _device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Point);
            
            
            
            _running = true;
            return ResultCode.Success;
        }
        // ReSharper disable InconsistentNaming
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SIZE = 0x0005;
        private const int WM_DESTROY = 0x0002;
        // ReSharper restore InconsistentNaming
        private bool WndProc(ref Message m) {
            switch (m.Msg) {
                case WM_DESTROY:
                    _running = false;
                    return true;
            }
            return false;
        }

        public Result Update(float deltaTime) {

            _angle += deltaTime*0.5f;

            var center = new Vector2(50.0f, 50.0f);
            var eye = new Vector3(center.X + (float)Math.Cos(_angle) * _radius, _radius, -center.Y + (float)Math.Sin(_angle) * _radius);
            var lookat = new Vector3(center.X, 0, -center.Y);

            var world = Matrix.Identity;
            var view = Matrix.LookAtLH(eye, lookat, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float) Math.PI/4, 1.3333f, 1.0f, 1000.0f);

            _device.SetTransform(TransformState.World, world);
            _device.SetTransform(TransformState.View, view);
            _device.SetTransform(TransformState.Projection, proj);






            if (Util.IsKeyDown(Keys.Escape)) {
                Quit();
            }

            return ResultCode.Success;
        }

        public Result Render() {
            _device.Clear(ClearFlags.All, Color.Black, 1.0f, 0);

            if (_device.BeginScene().IsSuccess) {

                _terrain.Render();


                var r = new Rectangle(0, 0, 640, 480);
                //_font.DrawString(null, "Hello world!", r, DrawTextFormat.Center | DrawTextFormat.VerticalCenter | DrawTextFormat.NoClip, Color.White);

                _device.EndScene();
                _device.Present();
            }


            return ResultCode.Success;
        }

        public Result Cleanup() {
            try {
                Util.ReleaseCom(ref _terrain);
                Util.ReleaseCom(ref _font);
                Util.ReleaseCom(ref _device);

                MapObject.UnloadObjectResources();

                Log.Info("Application terminated");
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
            return ResultCode.Success;
        }

        public Result Quit() {
            _running = false;
            
            return ResultCode.Success;
        }

        public void Run() {
            var startTime = Stopwatch.GetTimestamp();
            while (_running) {

                Application.DoEvents();
                var t = Stopwatch.GetTimestamp();
                var dt = ((float)(t - startTime)) / Stopwatch.Frequency;

                Update(dt);
                Render();

                startTime = t;

            }
            Cleanup();
        }
    }
}