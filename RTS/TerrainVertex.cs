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
}