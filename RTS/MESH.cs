using System.Collections.Generic;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;

namespace RTS {
    internal class MESH : DisposableClass {
        public Device Device { get; private set; }

        public Mesh Mesh { get { return _mesh; } }

        private Mesh _mesh;
        private readonly List<Texture> _textures = new List<Texture>();
        private readonly List<Material> _materials = new List<Material>();
        private Material _white;

        public MESH() {
            Device = null;
            _mesh = null;
        }

        public MESH(string filename, Device device) {
            Device = device;
            _mesh = null;
            Load(filename, device);
        }

        public void Load(string filename, Device device) {
            _white = new Material() {
                Ambient = Color.White,
                Specular = Color.White,
                Diffuse = Color.White,
                Emissive = Color.Black,
                Power = 1.0f
            };
            Release();
            _mesh = Mesh.FromFile(Device, filename, MeshFlags.IndexBufferManaged);

            var mtrls = Mesh.GetMaterials();
            foreach (var mtrl in mtrls) {
                _materials.Add(mtrl.MaterialD3D);
                if (!string.IsNullOrWhiteSpace(mtrl.TextureFileName)) {
                    var texFile = "meshes/" + mtrl.TextureFileName;
                    var tex = Texture.FromFile(Device, texFile);
                    _textures.Add(tex);
                } else {
                    _textures.Add(null);
                }
            }
            Mesh.OptimizeInPlace(MeshOptimizeFlags.AttributeSort | MeshOptimizeFlags.Compact | MeshOptimizeFlags.VertexCache);
        }

        public void Render() {
            for (int i = 0; i < _materials.Count; i++) {
                if (_textures[i] != null) {
                    Device.Material = _white;
                } else {
                    Device.Material = _materials[i];
                }
                Device.SetTexture(0, _textures[i]);
                Mesh.DrawSubset(i);
            }
        }

        public void Release() {
            Util.ReleaseCom(ref _mesh);
            for (int i = 0; i < _textures.Count; i++) {
                var tex = _textures[i];
                Util.ReleaseCom(ref tex);
            }
            _textures.Clear();
            _materials.Clear();
        }

        private bool _disposed;
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Release();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        
    }
    internal class MeshInstance {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public MESH Mesh { get; set; }

        public MeshInstance() {
            Mesh = null;
            Position = Rotation = Vector3.Zero;
            Scale = new Vector3(1.0f);
        }

        public MeshInstance(MESH mesh) : this() { Mesh = mesh; }
        public MeshInstance(MESH mesh, Vector3 pos, Vector3 rot, Vector3 sca) {
            Mesh = mesh;
            Position = pos;
            Rotation = rot;
            Scale = sca;
        }

        public void Render() {
            if (Mesh != null) {
                
                Mesh.Device.SetTransform(TransformState.World, GetWorldMatrix());
                Mesh.Render();
            }
        }

        public Matrix GetWorldMatrix() {
            var p = Matrix.Translation(Position);
            var r = Matrix.RotationYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
            var s = Matrix.Scaling(Scale);

            var world = s * r * p;
            return world;
        }

        public BoundingBox GetBoundingBox() {
            if (Mesh == null || Mesh.Mesh == null) {
                return new BoundingBox();
            }
            if (Mesh.Mesh.VertexFormat != ObjectVertex.FVF) {
                return new BoundingBox();
            }


        }
        public BoundingSphere GetBoundingSphere() { }
    }
}