using System;
using System.Drawing;
using System.Reflection;
using log4net;
using SlimDX;
using SlimDX.Direct3D9;

namespace RTS {
    public class Patch : DisposableClass {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Device _device;
        private Mesh _mesh;
        public Mesh Mesh { get { return _mesh; }  }
        public BoundingBox BoundingBox {get; private set;}
        public Rectangle MapRect { get; set; }


        public Result CreateMesh(Terrain terrain, Rectangle source, Device device) {
            if (_mesh != null) {
                Util.ReleaseCom(ref _mesh);
            }
            try {
                _device = device;
                MapRect = source;

                var width = source.Width;
                var height = source.Height;
                var nrVerts = (width + 1)*(height + 1);
                var nrTris = width*height*2;

                var max = new Vector3(-10000);
                var min = new Vector3(10000);

                try {
                    _mesh = new Mesh(_device, nrTris, nrVerts, MeshFlags.Managed, TerrainVertex.FVF);
                } catch (Exception ex) {
                    Log.Error("Could not create mesh for Patch");
                    return ResultCode.Failure;
                }
                var data = _mesh.LockVertexBuffer(LockFlags.Discard);
                for (int z = source.Top; z <= source.Bottom; z++) {
                    for (int x = source.Left; x <= source.Right; x++) {
                        var tile = terrain.GetTile(x, z);

                        var pos = new Vector3(x, tile.Height, -z);
                        min = Vector3.Minimize(min, pos);
                        max = Vector3.Maximize(max, pos);

                        var norm = terrain.GetNormal(x, z);
                        var alphaUV = new Vector2(((float) x)/terrain.Width, ((float) z)/terrain.Height);
                        var colorUV = alphaUV*8;
                        data.Write(new TerrainVertex(pos, norm, alphaUV, colorUV));
                    }
                }
                _mesh.UnlockVertexBuffer();
                BoundingBox =new BoundingBox(min, max);

                data = _mesh.LockIndexBuffer(LockFlags.Discard);
                for (int z = source.Top, z0=0; z < source.Bottom; z++, z0++) {
                    for (int x = source.Left, x0=0; x < source.Right; x++, x0++) {
                        data.Write<short>((short) (z0 * (width+1) + x0));
                        data.Write<short>((short) (z0 * (width + 1) + x0 + 1));
                        data.Write<short>((short) ((z0 + 1) * (width + 1) + x0));

                        data.Write<short>((short) ((z0 + 1) * (width + 1) + x0));
                        data.Write<short>((short) (z0 * (width + 1) + x0 + 1));
                        data.Write<short>((short) ((z0 + 1) * (width + 1) + x0 + 1));
                    }
                }
                _mesh.UnlockIndexBuffer();

                data = _mesh.LockAttributeBuffer(LockFlags.Discard);
                for (int i = 0; i < nrTris; i++) {
                    data.Write(0);
                }
                _mesh.UnlockAttributeBuffer();

                //_mesh.ComputeNormals();


            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }

            return ResultCode.Success;
        }

        public void Render() {
            if (_mesh != null) {
                _mesh.DrawSubset(0);
            }
        }

        private bool _disposed;
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _mesh);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}