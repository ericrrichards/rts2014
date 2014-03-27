using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.Direct3D9;

namespace RTS {
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV1;
        public Vector2 UV2;


        public TerrainVertex(Vector3 pos, Vector3 norm, Vector2 uv1, Vector2 uv2) {
            Position = pos;
            Normal = norm;
            UV1 = uv1;
            UV2 = uv2;
        }
        public TerrainVertex(Vector3 pos, Vector2 uv1, Vector2 uv2) {
            Position = pos;
            Normal = Vector3.UnitY;
            UV1 = uv1;
            UV2 = uv2;
        }

        public static VertexFormat FVF = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Texture2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public float U;
        public float V;
        public static VertexFormat FVF = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Texture1;

        public ObjectVertex(Vector3 pos, Vector3 norm, float u, float v) {
            Position = pos;
            Normal = norm;
            U = u;
            V = v;
        }
    }
}