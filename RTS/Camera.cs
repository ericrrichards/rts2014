using System;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using SlimDX;
using SlimDX.Direct3D9;
using Device = SlimDX.Direct3D9.Device;

namespace RTS {
    public class Camera {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Device _device;
        private float _alpha;
        private float _beta;
        private float _radius;
        private float _fov;
        private Vector3 _eye;
        private Vector3 _focus;
        private Vector3 _right;
        private Vector3 _look;

        private Plane[] _frustum = new Plane[6];

        // Init
        public Camera() { Init(null);}

        public Vector3 Focus { get { return _focus; } set { _focus = value; } }

        public float FOV { get { return _fov; } set { _fov = value; } }

        public float Radius { get { return _radius; } set { _radius = value; } }

        public void Init(Device device) {
            _device = device;
            _alpha = _beta = 0.5f;
            Radius = 10.0f;
            FOV = Util.PI/4.0f;

            _eye = new Vector3(50, 50, 50);
            Focus = Vector3.Zero;
        }

        // Movement
        public void Scroll(Vector3 vec) {
            var newFocus = Focus + vec;
            Focus = newFocus;
        }

        public void Pitch(float f) {
            _beta += f;
            _beta = Util.Clamp(_beta, 0.5f, Util.PI/2 - 0.05f);
        }

        public void Yaw(float f) {
            _alpha += f;
            if (_alpha > Util.PI*2) _alpha -= Util.PI*2;
            if (_alpha < -Util.PI*2) _alpha += Util.PI*2;
        }

        public void Zoom(float f) {
            FOV += f;
            FOV = Util.Clamp(FOV, 0.1f, Util.PI/2);
        }

        public void ChangeRadius(float f) {
            Radius += f;
            Radius = Util.Clamp(Radius, 2, 70);
        }

        public void Update(Mouse mouse, Terrain terrain, float dt) {
            _right.Y = _look.Y = 0.0f;
            
            _look.Normalize();
            _right.Normalize();

            if (mouse.X < mouse.ViewPort.Left + 10) { Scroll(-_right*dt * (4.0f + Radius*0.2f));}
            if (mouse.X > mouse.ViewPort.Right - 10) { Scroll(_right * dt * (4.0f + Radius * 0.2f)); }
            if (mouse.Y < mouse.ViewPort.Top + 10) { Scroll(_look * dt * (4.0f + Radius * 0.2f)); }
            if (mouse.Y > mouse.ViewPort.Bottom - 10) { Scroll(-_look * dt * (4.0f + Radius * 0.2f)); }

            if (Util.IsKeyDown(Keys.Left)) {Yaw(-dt);}
            if (Util.IsKeyDown(Keys.Right)) {Yaw(dt);}
            if (Util.IsKeyDown(Keys.Up)) { Pitch(dt);}
            if (Util.IsKeyDown(Keys.Down)) { Pitch(-dt);}

            if (Util.IsKeyDown(Keys.Add)) { Zoom(-dt);}
            if (Util.IsKeyDown(Keys.Subtract)) { Zoom(dt);}

            if (mouse.WheelUp()) { ChangeRadius(-1.0f);}
            if (mouse.WheelDown()) { ChangeRadius(1.0f);}

            var sideRadius = Radius*Util.Cos(_beta);
            var height = Radius*Util.Sin(_beta);

            _eye = new Vector3(
                Focus.X + sideRadius*Util.Cos(_alpha), 
                Focus.Y + height,
                Focus.Z + sideRadius * Util.Sin(_alpha)
            );
            foreach (var patch in terrain.Patches) {
                var mr = patch.MapRect;
                if (mr.Contains((int) Focus.X, (int) -Focus.Z)) {
                    float dist;
                    if (patch.Mesh.Intersects(new Ray(new Vector3( Focus.X, 10000, Focus.Z), new Vector3(0,-1,0) ), out dist)) {
                        _focus.Y = 10000.0f - dist;
                    }
                }
            }
            if (_device != null) {
                var view = GetViewMatrix();
                var proj = GetProjectionMatrix();

                _device.SetTransform(TransformState.View, view);
                _device.SetTransform(TransformState.Projection, proj);

                CalculateFrustum(view, proj);
            }

        }

        public Matrix GetViewMatrix() {
            var matView = Matrix.LookAtLH(_eye, Focus, Vector3.UnitY);
            _right = new Vector3(matView[0,0], matView[1,0], matView[2,0]);
            _right.Normalize();
            
            _look = new Vector3(matView[0,2], matView[1,2], matView[2,2]);
            _look.Normalize();

            return matView;
        }

        public Matrix GetProjectionMatrix() {
            var proj = Matrix.PerspectiveFovLH(FOV, 800.0f/600.0f, 1.0f, 1000.0f);
            return proj;
        }

        public void CalculateFrustum(Matrix view, Matrix proj) {
            try {
                var comb = view*proj;
                // left
                _frustum[0] = new Plane(
                    comb.M14 + comb.M11, 
                    comb.M24 + comb.M21, 
                    comb.M34 + comb.M31, 
                    comb.M44 + comb.M41);
                // right
                _frustum[1] = new Plane(
                    comb.M14 - comb.M11, 
                    comb.M24 - comb.M21, 
                    comb.M34 - comb.M31, 
                    comb.M44 - comb.M41);
                // top
                _frustum[2] = new Plane(
                    comb.M14 - comb.M12, 
                    comb.M24 - comb.M22, 
                    comb.M34 - comb.M32, 
                    comb.M44 - comb.M42);
                // bottom
                _frustum[3] = new Plane(
                    comb.M14 + comb.M12, 
                    comb.M24 + comb.M22, 
                    comb.M34 + comb.M32, 
                    comb.M44 + comb.M42);
                // near
                _frustum[4] = new Plane(
                    comb.M13, 
                    comb.M23, 
                    comb.M33, 
                    comb.M43);
                // far
                _frustum[5] = new Plane(
                    comb.M14 - comb.M13, 
                    comb.M24 - comb.M23, 
                    comb.M34 - comb.M33, 
                    comb.M44 - comb.M43);

                for (int i = 0; i < _frustum.Length; i++) {
                    _frustum[i].Normalize();
                }
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        public bool Cull(BoundingBox box) {
            try {
                foreach (var plane in _frustum) {
                    var intersection = Plane.Intersects(plane, box);
                    if (intersection == PlaneIntersectionType.Back) return true;
                }
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
            return false;
        }

        public bool Cull(BoundingSphere sphere) {
            try {
                foreach (var plane in _frustum) {
                    var intersection = Plane.Intersects(plane, sphere);
                    if (intersection == PlaneIntersectionType.Back) return true;
                }
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
            return true;
        }
    }
}
