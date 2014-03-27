using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using log4net;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.DirectInput;
using Device = SlimDX.Direct3D9.Device;

namespace RTS {
    public class Mouse :DisposableClass {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public int X { get; set; }
        public int Y { get; set; }
        public float Speed { get; set; }
        public int Type { get; set; }
        public Point MapPosition { get; set; }
        public Vector3 BallPosition { get; set; }
        public Vector2 UV { get; set; }
        public Rectangle ViewPort { get { return _viewport; } }

        private Device _device;
        private SlimDX.DirectInput.Mouse _mouseDevice;
        private MouseState _mouseState;
        private Texture _mouseTexture;
        private Mesh _sphereMesh;
        private Sprite _sprite;
        private Material _ballMtrl;
        private Rectangle _viewport;

        public Mouse() {
            Type = 0;
            _mouseTexture = null;
            _mouseDevice = null;
            Speed = 1.5f;
        }

        public void InitMouse(Device dev) {
            try {
                _device = dev;

                _mouseTexture = Texture.FromFile(_device, "cursor/cursor.dds");
                _sprite = new Sprite(_device);

                using (var directInput = new DirectInput()) {


                    _mouseDevice = new SlimDX.DirectInput.Mouse(directInput);
                    _mouseDevice.Acquire();

                    var v = _device.Viewport;
                    _viewport = new Rectangle(v.X, v.Y, v.Width, v.Height);

                    X = _viewport.X + _viewport.Width/2;
                    Y = _viewport.Y + _viewport.Height/2;

                }
                _sphereMesh = Mesh.CreateSphere(_device, 0.2f, 5, 5);
                _ballMtrl = new Material() {
                    Diffuse = Color.Yellow,
                    Specular = Color.Black,
                    Ambient = Color.Black,
                    Emissive = Color.Black
                };


            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        public bool ClickLeft() { return _mouseState.IsPressed(0); }
        public bool ClickRight() { return _mouseState.IsPressed(1); }
        public bool WheelUp() { return _mouseState.Z > 0; }
        public bool WheelDown() { return _mouseState.Z < 0; }

        public bool Over(Rectangle dest) {
            if (X < dest.Left || X > dest.Right) return false;
            if (Y < dest.Top|| Y > dest.Bottom) return false;
            return true;
        }

        public bool PressInRect(Rectangle dest) { return ClickLeft() && Over(dest); }

        public void Update(Terrain terrain) {
            _mouseState = _mouseDevice.GetCurrentState();
            X += (int)(_mouseState.X*Speed);
            Y += (int)(_mouseState.Y*Speed);

            X = Util.Clamp(X, _viewport.Left, _viewport.Right);
            Y = Util.Clamp(Y, _viewport.Top, _viewport.Bottom);

            CalculateMapPosition(terrain);
        }

        public void Paint() {
            var world = Matrix.Translation(BallPosition);
            _device.SetTransform(TransformState.World, world);
            _device.Material = _ballMtrl;
            _device.SetTexture(0, null);
            _sphereMesh.DrawSubset(0);

            if (_mouseTexture != null) {
                var src = new[] {
                    new Rectangle(0, 0, 20, 20),
                    new Rectangle(0, 20, 20, 20),
                    new Rectangle(20, 20, 20, 20),
                    new Rectangle(0, 40, 20, 20),
                    new Rectangle(20, 40, 20, 20),
                };
                _sprite.Begin(SpriteFlags.AlphaBlend);
                _sprite.Draw(_mouseTexture, src[Type], null, new Vector3(X, Y, 0), Color.White);
                _sprite.End();
            }
        }

        public Ray GetRay() {
            try {
                var proj = _device.GetTransform(TransformState.Projection);
                var view = _device.GetTransform(TransformState.View);
                var world = _device.GetTransform(TransformState.World);

                var width = _viewport.Width;
                var height = _viewport.Height;

                var angleX = (((2.0f*X)/width) - 1.0f)/proj[0, 0];
                var angleY = (((-2.0f*Y)/height) + 1.0f)/proj[1, 1];

                var ray = new Ray(Vector3.Zero, new Vector3(angleX, angleY, 1.0f));
                var m = world*view;
                var inverseWorldView = Matrix.Invert(m);
                ray.Position = Vector3.TransformCoordinate(ray.Position, inverseWorldView);
                ray.Direction = Vector3.TransformNormal(ray.Direction, inverseWorldView);

                ray.Direction.Normalize();
                return ray;

            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
            return new Ray();
        }


        private void CalculateMapPosition(Terrain terrain) {
            var world = Matrix.Identity;
            _device.SetTransform(TransformState.World, world);
            var ray = GetRay();

            var minDistance = 10000.0f;
            foreach (var patch in terrain.Patches) {
                float dist;
                if (Ray.Intersects(ray, patch.BoundingBox, out dist) && dist< minDistance) {
                    int face;
                    IntersectInformation[] intersections;
                    if (patch.Mesh.Intersects(ray, out dist, out face, out intersections) && dist < minDistance) {

                        var hitU = intersections.First().U;
                        var hitV = intersections.First().V;

                        minDistance = dist;
                        var tiles = face/2;
                        var tilesPerRow = patch.MapRect.Width;
                        var y = tiles/tilesPerRow;
                        var x = tiles - y*tilesPerRow;

                        if (face%2 == 0) {
                            if (hitU > 0.5f) x++;
                            else if (hitV > 0.5f) y++;
                        } else {
                            if (hitU + hitV < 0.5f) y++;
                            else if (hitU > 0.5f) x++;
                            else {
                                x++;
                                y++;
                            }
                        }
                        MapPosition = new Point(patch.MapRect.Left + x, patch.MapRect.Top + y);
                        BallPosition = terrain.GetWorldPosition(MapPosition);
                        UV = new Vector2(hitU, hitV);
                    }
                }
            }
        }


        private bool _disposed;
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _mouseDevice);
                    Util.ReleaseCom(ref _mouseTexture);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}