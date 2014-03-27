using System.Collections.Generic;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;

namespace RTS {
    internal class MapObject  {
        static readonly List<MESH> ObjectMeshes = new List<MESH>();

        public Point MapPos { get { return _mapPos; } }

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

        public MapObject() {
            _type = 0;
        }

        public MapObject(int t, Point mp, Vector3 pos, Vector3 rot, Vector3 sca) {
            _type = t;
            _mapPos = mp;
            _meshInstance = new MeshInstance(ObjectMeshes[_type],pos, rot, sca);
            _meshInstance.Position = pos;
            _meshInstance.Rotation = rot;
            _meshInstance.Scale = sca;
            _meshInstance.Mesh = ObjectMeshes[_type];
        }

        public void Render() { _meshInstance.Render(); }

    }
}