using System.Collections.Generic;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;

namespace RTS {
    internal class MapObject  {
        static readonly List<MESH> ObjectMeshes = new List<MESH>();

        public Point MapPos { get { return _mapPos; } }

        public BoundingBox BoundingBox { get { return _bbox; } }

        public MeshInstance MeshInstance { get { return _meshInstance; } }

        public static void LoadObjectResources(Device device) {
            var tree = new MESH("meshes/tree.x", device);
            ObjectMeshes.Add(tree);
            var stone = new MESH("meshes/stone.x", device);
            ObjectMeshes.Add(stone);
        }

        public static void UnloadObjectResources() {
            foreach (var objectMesh in ObjectMeshes) {
                var mesh = objectMesh;
                Util.ReleaseCom(ref mesh);
            }
            ObjectMeshes.Clear();
        }

        private Point _mapPos;
        private MeshInstance _meshInstance;
        private int _type;
        private BoundingBox _bbox;

        public MapObject() {
            _type = 0;
        }

        public MapObject(int t, Point mp, Vector3 pos, Vector3 rot, Vector3 sca) {
            _type = t;
            _mapPos = mp;
            _meshInstance = new MeshInstance(ObjectMeshes[_type],pos, rot, sca);
            MeshInstance.Position = pos;
            MeshInstance.Rotation = rot;
            MeshInstance.Scale = sca;
            MeshInstance.Mesh = ObjectMeshes[_type];
            _bbox = MeshInstance.GetBoundingBox();
        }

        public void Render() { MeshInstance.Render(); }

    }
}