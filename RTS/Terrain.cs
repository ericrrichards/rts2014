using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Windows.Forms.VisualStyles;
using log4net;
using SlimDX;
using SlimDX.Direct3D9;


namespace RTS {
    public class Terrain : DisposableClass {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Point _size;
        private Device _device;
        private HeightMap _heightMap;
        private List<Patch> _patches;
        private List<Texture> _diffuseMaps;
        private List<MapObject> _objects;
        private Texture _alphaMap;
        private Shader _terrainPS;
        private Material _mtrl;
        public List<MapTile> MapTiles { get; set; }
        public int Width { get { return _size.X; } }
        public int Height { get { return _size.Y; } }
        public List<Patch> Patches { get { return _patches; } }

        public Terrain() {
            _diffuseMaps = new List<Texture>();
            _patches = new List<Patch>();
            _objects = new List<MapObject>();
            MapTiles = new List<MapTile>();
        }

        public void Init(Device dev, Point size) {
            _device = dev;
            _size = size;
            _heightMap = null;

            // create map tiles
            MapTiles.Clear();
            for (int i = 0; i < size.X * size.Y; i++) {
                MapTiles.Add(new MapTile());
            }

            // clear old textures
            for (int i = 0; i < _diffuseMaps.Count; i++) {
                var diffuseMap = _diffuseMaps[i];
                Util.ReleaseCom(ref diffuseMap);
            }
            _diffuseMaps.Clear();

            // load textures
            Texture grass = null, mount = null, snow = null;
            try {
                grass = Texture.FromFile(_device, "textures/grass.jpg");
            } catch (Exception ex) {
                Log.Error("Could not load grass.jpg");
            }
            try {
                mount = Texture.FromFile(_device, "textures/mountain.jpg");
            } catch (Exception ex) {
                Log.Error("Could not load mountain.jpg");
            }
            try {
                snow = Texture.FromFile(_device, "textures/snow.jpg");
            } catch (Exception ex) {
                Log.Error("Could not load snow.jpg");
            }
            _diffuseMaps.AddRange(new[] { grass, mount, snow });
            _alphaMap = null;

            // load pixelshader
            _terrainPS = new Shader(_device, "Shaders/terrain.ps", ShaderType.PixelShader);

            _mtrl = new Material() {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Specular = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = new Color4(0.5f, 0.5f, 0.5f),
                Emissive = Color.Black,
            };
            GenerateRandomTerrain(3);
        }

        public void Release() {
            for (int i = 0; i < _patches.Count; i++) {
                var patch = _patches[i];
                Util.ReleaseCom(ref patch);
            }
            _patches.Clear();

            Util.ReleaseCom(ref _heightMap);
            
            _objects.Clear();
        }


        public void GenerateRandomTerrain(int numPatches) {
            try {
                Release();

                _heightMap = new HeightMap(_size, 20.0f);
                var hm2 = new HeightMap(_size, 2.0f);

                _heightMap.CreateRandomHeightMap(Util.Rand(2000), 2.0f, 0.5f, 8);
                hm2.CreateRandomHeightMap(Util.Rand(2000), 2.5f, 0.8f, 3);

                hm2.Cap(hm2.MaxHeight * 0.4f);

                _heightMap *= hm2;

                var hm3 = new HeightMap(_size, 1.0f);
                hm3.CreateRandomHeightMap(Util.Rand(1000), 5.5f, 0.9f, 7);

                for (int y = 0; y < _size.Y; y++) {
                    for (int x = 0; x < _size.X; x++) {
                        if (_heightMap.GetHeight(x, y) == 0.0f && hm3.GetHeight(x, y) > 0.7f && Util.Rand(6) == 0) {
                            AddObject(0, new Point(x, y));
                        } else if (_heightMap.GetHeight(x, y) >= 1.0f && hm3.GetHeight(x, y) > 0.9f && Util.Rand(20) == 0) {
                            AddObject(1, new Point(x, y));
                        }
                    }
                }
                hm3.Release();

                InitPathfinding();
                CreatePatches(numPatches);
                CalculateAlphaMaps();

            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }



        private void CreatePatches(int numPatches) {
            try {
                for (int i = 0; i < _patches.Count; i++) {
                    var patch = _patches[i];
                    Util.ReleaseCom(ref patch);
                }
                _patches.Clear();

                for (int y = 0; y < numPatches; y++) {
                    for (int x = 0; x < numPatches; x++) {
                        var r = new Rectangle(
                            (int)(x * (_size.X - 1) / (float)numPatches),
                            (int)(y * (_size.Y - 1) / (float)numPatches),
                            (int)((_size.X - 1) / (float)numPatches),
                            (int)((_size.Y - 1) / (float)numPatches)
                            );
                        var p = new Patch();
                        p.CreateMesh(this, r, _device);
                        _patches.Add(p);

                    }
                }
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        private void CalculateAlphaMaps() {
            Util.ReleaseCom(ref _alphaMap);

            _alphaMap = new Texture(_device, 128, 128, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);


            var cols = new List<float[]>();
            for (int i = 0; i < 128*128; i++) {
                cols.Add(new[] {0f, 0f, 0f});
            }



            for (int i = 0; i < _diffuseMaps.Count; i++) {
                for (int y = 0; y < 128; y++) {
                    for (int x = 0; x < 128; x++) {
                        var terrainX = (int)(_size.X * (x / 128.0f));
                        var terrainY = (int)(_size.Y * (y / 128.0f));
                        var tile = GetTile(terrainX, terrainY);
                        if (tile != null && tile.Type == i) {
                            cols[y * 128 + x][i] = 1.0f;
                        }
                    }
                }
            }
            var colors = cols.Select(c => new Color4(1.0f, c[2], c[1], c[0])).ToList();


            var dr = _alphaMap.LockRectangle(0, LockFlags.None);
            foreach (var color4 in colors) {
                dr.Data.Write(color4.ToArgb());
            }
            _alphaMap.UnlockRectangle(0);

            Texture.ToFile(_alphaMap, "alphamap.bmp", ImageFileFormat.Bmp);
        }

        private void AddObject(int type, Point mapPos) {
            var pos = new Vector3(mapPos.X, _heightMap.GetHeight(mapPos), -mapPos.Y);
            var rot = new Vector3(Util.RandF() * 0.13f, Util.RandF() * 3.0f, Util.RandF() * 0.13f);
            var scaXZ = Util.RandF() * 0.5f + 0.5f;
            var scaY = Util.RandF() * 1.0f + 0.5f;
            var sca = new Vector3(scaXZ, scaY, scaXZ);

            _objects.Add(new MapObject(type, mapPos, pos, rot, sca));
        }

        public void Render() {
            _device.SetRenderState(RenderState.Lighting, false);
            _device.SetRenderState(RenderState.ZWriteEnable, true);

            _device.SetTexture(0, _alphaMap);
            _device.SetTexture(1, _diffuseMaps[0]);
            _device.SetTexture(2, _diffuseMaps[1]);
            _device.SetTexture(3, _diffuseMaps[2]);
            _device.Material = _mtrl;

            _terrainPS.Begin();
            foreach (var patch in _patches) {
                patch.Render();
            }
            _terrainPS.End();

            foreach (var mapObject in _objects) {
                mapObject.Render();
            }
        }

        private bool Within(Point p) { return p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height; }

        private void InitPathfinding() {
            try {
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        var tile = GetTile(x, y);
                        if (_heightMap != null) {
                            tile.Height = _heightMap.GetHeight(x, y);
                        }
                        tile.MapPosition = new Point(x,y);
                        if (tile.Height < 1.0f) {
                            tile.Type = 0; // grass
                        } else if (tile.Height < 15.0f) {
                            tile.Type = 1; //stone
                        } else {
                            tile.Type = 2; //snow
                        }
                    }
                }
                // calculate tile cost
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        var tile = GetTile(x, y);
                        if (tile != null) {
                            var p = new[] {
                                new Point(x - 1, y - 1), new Point(x, y - 1), new Point(x + 1, y - 1),
                                new Point(x - 1, y), new Point(x + 1, y),
                                new Point(x - 1, y + 1), new Point(x, y + 1), new Point(x + 1, y + 1)
                            };
                            var variance = 0.0f;
                            var nr = 0;
                            for (int i = 0; i < p.Length; i++) {
                                if (Within(p[i])) {
                                    var neighbor = GetTile(p[i]);
                                    if (neighbor != null) {
                                        var v = neighbor.Height - tile.Height;
                                        variance += v*v;
                                        nr++;
                                    }
                                }
                            }
                            variance /= nr;
                            tile.Cost = Math.Min(variance + 0.1f, 1.0f);
                            tile.Walkable = tile.Cost < 0.5f;
                        }
                    }
                }
                // set tiles with trees & rocks unwalkable
                foreach (var mapObject in _objects) {
                    var tile = GetTile(mapObject.MapPos);
                    if (tile != null) {
                        tile.Walkable = false;
                        tile.Cost = 1.0f;
                    }
                }
                // connect tiles with neighbors
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        var tile = GetTile(x, y);
                        if (tile != null && tile.Walkable) {
                            
                            for (int i = 0; i < tile.Neighbors.Count; i++) {
                                tile.Neighbors[i] = null;
                            }
                            var p = new[] {
                                new Point(x - 1, y - 1), new Point(x, y - 1), new Point(x + 1, y - 1),
                                new Point(x - 1, y), new Point(x + 1, y),
                                new Point(x - 1, y + 1), new Point(x, y + 1), new Point(x + 1, y + 1)
                            };
                            for (int i = 0; i < p.Length; i++) {
                                if (Within(p[i])) {
                                    var neighbor = GetTile(p[i]);
                                    if (neighbor != null && neighbor.Walkable) {
                                        tile.Neighbors[i] = neighbor;
                                    }
                                }
                            }
                        }
                    }
                }
                CreateTileSets();


            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
        }

        private void CreateTileSets() {
            var setNo = 0;
            var unvisited = new HashSet<MapTile>();
            foreach (var mapTile in MapTiles) {
                if (mapTile.Walkable) {
                    unvisited.Add(mapTile);
                } else {
                    mapTile.Set = --setNo;
                }
            }
            setNo = 0;

            var stack = new Stack<MapTile>();
            while (unvisited.Any()) {
                var newFirst = unvisited.First();
                stack.Push(newFirst);
                unvisited.Remove(newFirst);

                while (stack.Any()) {
                    var next = stack.Pop();
                    next.Set = setNo;
                    foreach (var neighbor in next.Neighbors.Where(n=>n!=null && unvisited.Contains(n))) {
                        stack.Push(neighbor);
                        unvisited.Remove(neighbor);
                    }
                }
                setNo++;
            }
        }

        public List<Point> GetPath(Point start, Point goal) {
            try {
                var startTile = GetTile(start);
                var goalTile = GetTile(goal);

                // bounds check
                if (!Within(start) || !Within(goal) || startTile == null || goalTile == null) {
                    return new List<Point>();
                }
                // walkability check
                if (!startTile.Walkable || !goalTile.Walkable || startTile.Set != goalTile.Set) {
                    return new List<Point>();
                }
                // init search
                foreach (var mapTile in MapTiles) {
                    mapTile.F = mapTile.G = float.MaxValue;
                    mapTile.Parent = null;
                }
                var open = new PriorityQueue<MapTile>(MapTiles.Count);
                var closed = new HashSet<MapTile>();

                startTile.G = 0;
                startTile.F = H(start, goal);

                open.Enqueue(startTile, startTile.F);
                MapTile current = null;
                while (open.Any() && current != goalTile) {
                    current = open.Dequeue();
                    closed.Add(current);
                    for (int i = 0; i < 8; i++) {
                        var neighbor = current.Neighbors[i];
                        if ( neighbor == null) continue;

                        var cost = current.G + neighbor.Cost;
                        if (open.Contains(neighbor) && cost < neighbor.G) {
                            open.Remove(neighbor);
                        }
                        if (closed.Contains(neighbor) && cost < neighbor.G) {
                            closed.Remove(neighbor);
                        }
                        if (!open.Contains(neighbor) && !closed.Contains(neighbor)) {
                            neighbor.G = cost;
                            var f = cost + H(neighbor.MapPosition, goal);
                            open.Enqueue(neighbor, f);
                            neighbor.Parent = current;
                        }
                    }
                }
                System.Diagnostics.Debug.Assert(current == goalTile);
                var path = new List<Point>();
                while (current != startTile) {
                    path.Add(current.MapPosition);
                    current = current.Parent;
                }
                path.Reverse();
                return path;

            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
            }
            return new List<Point>();
        }

        private static float H(Point start, Point goal) { return Vector2.Distance(new Vector2(start.X, start.Y), new Vector2(goal.X, goal.Y)); }

        private MapTile GetTile(Point point) { return GetTile(point.X, point.Y); }
        public MapTile GetTile(int x, int y) {
            if (MapTiles == null) return null;
            return MapTiles[x + y*Width];
        }


        private bool _disposed;
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Release();
                    Util.ReleaseCom(ref _terrainPS);
                    foreach (var diffuseMap in _diffuseMaps) {
                        var tex = diffuseMap;
                        Util.ReleaseCom(ref tex);
                    }
                    Util.ReleaseCom(ref _alphaMap);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }


    }
    
}