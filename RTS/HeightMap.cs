using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RTS {
    internal class HeightMap : DisposableClass {
        public static float Noise(int x) {
            x = (x << 13) ^ x;
            return (1.0f - ((x * (x * x * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);
        }

        public static float CosInterpolate(float v1, float v2, float a) {
            var angle = a * Math.PI;
            var prc = (float)((1.0f - Math.Cos(angle)) * 0.5f);
            return v1 * (1.0f - prc) + v2 * prc;
        }

        public Point Size { get; set; }
        public float MaxHeight { get; set; }
        private List<float> _heightMap = new List<float>();

        public HeightMap(Point size, float maxHeight) {
            Size = size;
            MaxHeight = maxHeight;
            _heightMap = Enumerable.Repeat(0f, size.X * size.Y).ToList();
        }


        public static HeightMap operator *(HeightMap lhs, HeightMap rhs) {
            var hm = new HeightMap(lhs.Size, lhs.MaxHeight);
            for (int y = 0; y < lhs.Size.Y; y++) {
                for (int x = 0; x < lhs.Size.X; x++) {
                    var a = lhs.GetHeight(x, y) / lhs.MaxHeight;
                    var b = 1.0f;
                    if (x <= rhs.Size.X && y <= rhs.Size.Y) {
                        b = rhs.GetHeight(x, y) / rhs.MaxHeight;
                    }
                    hm._heightMap[x + y * hm.Size.X] = a * b * hm.MaxHeight;
                }
            }
            return hm;
        }
        public void Release() { _heightMap.Clear(); }

        public void CreateRandomHeightMap(int seed, float noiseSize, float persistence, int octaves) {
            for (int y = 0; y < Size.Y; y++) {
                for (int x = 0; x < Size.X; x++) {
                    var xf = ((float)x / Size.X) * noiseSize;
                    var yf = ((float)y / Size.Y) * noiseSize;

                    var total = 0f;

                    for (int i = 0; i < octaves; i++) {
                        var freq = Math.Pow(2.0f, i);
                        var amp = (float) Math.Pow(persistence, i);

                        var tx = xf*freq;
                        var ty = yf*freq;
                        var txInt = (int) tx;
                        var tyInt = (int) ty;

                        var fracX = (float) (tx - txInt);
                        var fracY = (float) (ty - tyInt);

                        var v1 = Noise(txInt + tyInt*57 + seed);
                        var v2 = Noise(txInt + 1 + tyInt*57 + seed);
                        var v3 = Noise(txInt + (tyInt+1) * 57 + seed);
                        var v4 = Noise(txInt + 1 + (tyInt+1) * 57 + seed);

                        var i1 = CosInterpolate(v1, v2, fracX);
                        var i2 = CosInterpolate(v3, v4, fracX);

                        total += CosInterpolate(i1, i2, fracY)*amp;
                    }
                    var b = (int) (128 + total*128.0f);
                    if (b < 0) b = 0;
                    if (b > 255) b = 255;

                    _heightMap[x + y*Size.X] = (b/255.0f)*MaxHeight;
                }
            }
        }

        public void Cap(float capHeight) {
            MaxHeight = 0.0f;

            for (int y = 0; y < Size.Y; y++) {
                for (int x = 0; x < Size.X; x++) {
                    _heightMap[x + y * Size.X] -= capHeight;
                    if (_heightMap[x + y * Size.X] < 0.0f) {
                        _heightMap[x + y * Size.X] = 0.0f;
                    }
                    if (_heightMap[x + y * Size.X] > MaxHeight) {
                        MaxHeight = _heightMap[x + y * Size.X];
                    }
                }
            }
        }


        public float GetHeight(int x, int y) {
            if (x < 0 || y < 0 || x > Size.X || y > Size.Y) {
                return 0;
            }
            return _heightMap[x + y * Size.X];
        }
        public float GetHeight(Point mapPos) { return GetHeight(mapPos.X, mapPos.Y); }


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
}