using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RTS {
    public class MapTile :PriorityQueueNode {
        public int Type { get; set; }
        public int Set { get; set; }
        public float Height { get; set; }
        public float Cost { get; set; }
        public bool Walkable { get; set; }
        public List<MapTile> Neighbors { get; set; }

        // pathfinding cruft that will probably get refactored
        public Point MapPosition { get; set; }
        public float F { get; set; }
        public float G { get; set; }
        public MapTile Parent { get; set; }

        public MapTile() {
            Neighbors = new List<MapTile>(Enumerable.Repeat<MapTile>(null, 8));
        }
    }
}