/*
 * Copyright (c) 2010 Daniel Rasmussen <cyclotis04.dev@gmail.com>.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *   
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Drawing;
using System.Linq;

namespace Keystone.SpatialNodes
{
    // NOTE: I have to research  2d spatial hashing to really determine if this is something that is useful for 
    // my project.  Maybe i could convert the below to work with my RegionNode's as an alternative ISpatialNode type
    // that i can just plug in and test it to see if there's performance gains when doing cull traversal in particular.
    // But this has to be low priority for now.  Just need to focus on reaching tech demo.

    // http://gamedev.stackexchange.com/questions/13349/a-way-to-store-potentially-infinite-2d-map-data/13388#13388
    // I ran into a similar problem and decided to create my own structure to handle the data. It's based loosely on a quadtree, 
    // but has infinite (at least as big as an Int) expandability in all directions. It was designed to handle grid-based 
    // data which expanded from a central point, much like Minecraft does now. It is space efficient in memory, and very fast.
    
    // You can specify a minimum magnitude for each node (a magnitude of 7 would be 128x128) and once any node has a specified 
    // percentage of its subnodes populated, it automatically flattens itself into a two-dimensional array. This means that a 
    // very densely populated portion (e.g., a completely explored continent) will have the performance of an array (very fast) 
    // but a sparsely populated portion (e.g., a shoreline someone wandered up and down but didn't explore inland) will have good 
    // performance and a low memory usage.

    // My code can be found here. The code is complete, tested (unit- and load-tests), and quite optimized. The inner workings aren't 
    // too well documented yet, however, but all the public methods are so it should be usable. If anyone decides to try it out, 
    // feel free to contact me with questions or comments.

    // The biggest improvement over an expandable tree is that it 'flattens' certain nodes of the tree which are heavily 
    // populated (default 70%) into 2D arrays, rather than keeping them structured like trees. This gives you the speed of an array 
    // lookup, without the (infinite) size of an infinite array. – Dan Rasmussen

    //
    // http://www.gamedev.net/page/resources/_/technical/game-programming/spatial-hashing-r2697
    // http://entitycrisis.blogspot.com/2007/11/spatial-hashing.html
    public class MatrixSpacePartition<T>
    {
        #region Delegates

        public delegate T Initializer(int x, int y);

        #endregion

        private const int DefaultBlockMagnitude = 4;
        private const float DefaultSaturationThreshold = 0.75f;
        private readonly int _blockMagnitude;
        private readonly int _blockSize;
        private readonly MatrixSpacePartitionNode _root;
        private readonly float _threshold;

        /// <summary>
        ///   Initializes a new instance of the MatrixSpacePartition class with the default block magnitude and saturation threshold.
        /// </summary>
        public MatrixSpacePartition() : this(DefaultBlockMagnitude, DefaultSaturationThreshold) { }

        /// <summary>
        ///   Initializes a new instance of the MatrixSpacePartition class with a specified block magnitude and the default saturation threshold.
        /// </summary>
        public MatrixSpacePartition(int blockMagnitude) : this(blockMagnitude, DefaultSaturationThreshold) { }

        /// <summary>
        ///   Initializes a new instance of the MatrixSpacePartition class with a specified block magnitude and saturation threshold.
        /// </summary>
        public MatrixSpacePartition(int blockMagnitude, float saturationThreshold)
        {
            if (blockMagnitude < 0 || blockMagnitude > 30)
                throw new ArgumentOutOfRangeException("blockMagnitude",
                                                      "Block magnitude must be between 0 and 30, inclusive.");
            if (saturationThreshold < 0 || saturationThreshold > 1)
            {
                throw new ArgumentOutOfRangeException("saturationThreshold",
                                                      "Saturation threshold must be between 0 and 1, inclusive.");
            }

            _blockMagnitude = blockMagnitude;
            _blockSize = (1 << blockMagnitude);
            _threshold = saturationThreshold;
            _root = new MatrixSpacePartitionNode(this);
        }

        /// <summary>
        ///   Gets the magnitude of the minimum block size. Block width and height is equal to two to the power of its magnitude.
        /// </summary>
        public int BlockMagnitude
        {
            get { return _blockMagnitude; }
        }

        /// <summary>
        ///   Gets the minimal width and height of an allocated matrix. Matrices are always square and powers of two.
        /// </summary>
        public int BlockSize
        {
            get { return _blockSize; }
        }

        /// <summary>
        ///   Gets the rectangle which contains all internal allocated matrices. Bounds are expanded as needed.
        /// </summary>
        public Rectangle Bounds
        {
            get { return _root.Bounds; }
        }

        /// <summary>
        ///   Retrieves the item located at the point specified, or the default value if one does not exist.
        /// </summary>
        public T this[Point p]
        {
            get { return Get(p); }
            set { Set(p, value); }
        }

        /// <summary>
        ///   Retrieves the item located at the point specified, or the default value if one does not exist.
        /// </summary>
        public T this[int x, int y]
        {
            get { return Get(x, y); }
            set { Set(x, y, value); }
        }

        /// <summary>
        ///   Gets the level at which a subnode aggregates its internal matrixes, reducing tree depth but increasing memory usage.
        /// </summary>
        public float SaturationThreshold
        {
            get { return _threshold; }
        }

        /// <summary>
        ///   Retrieves the item located at the point specified, or the default value if one does not exist.
        /// </summary>
        public T Get(Point p)
        {
            return _root.Get(p);
        }

        /// <summary>
        ///   Retrieves the item located at the point specified, or the default value if one does not exist.
        /// </summary>
        public T Get(int x, int y)
        {
            return Get(new Point(x, y));
        }

        /// <summary>
        ///   Retrieves the item located at the point specified, or calculates and sets the point if the default value is found.
        /// </summary>
        public T Get(Point p, Initializer initializer)
        {
            T value;
            _root.Get(p, initializer, out value);
            return value;
        }

        /// <summary>
        ///   Retrieves the item located at the point specified, or calculates and sets the point if the default value is found.
        /// </summary>
        public T Get(int x, int y, Initializer initializer)
        {
            return Get(new Point(x, y), initializer);
        }

        /// <summary>
        ///   Copies the items in the selected region into a single new matrix.
        /// </summary>
        /// <param name = "bounds">The region of space to copy into a new matrix.</param>
        public T[,] GetRegion(Rectangle bounds)
        {
            var data = new T[bounds.Width, bounds.Height];
            _root.CopyInto(ref data, bounds);
            return data;
        }

        /// <summary>
        ///   Sets the value of the item located at the point specified.
        /// </summary>
        public void Set(Point p, T value)
        {
            _root.Set(p, value);
        }

        /// <summary>
        ///   Sets the value of the item located at the point specified.
        /// </summary>
        public void Set(int x, int y, T value)
        {
            Set(new Point(x, y), value);
        }

        /// <summary>
        ///   Allocates space for the selected region, setting unallocated items to their default values.
        /// </summary>
        /// <param name = "region">The region of space to allocate into matrices.</param>
        public void SetRegion(Rectangle region)
        {
            _root.SetRegion(region, null, false);
        }

        /// <summary>
        ///   Allocates space for the selected region.
        /// </summary>
        /// <param name = "region">The region of space to allocate into matrices.</param>
        /// <param name = "initializer">The custom constructor for allocated items.</param>
        /// <param name = "overwrite">Whether existing non-default items should be overwritten by the custom constructor.</param>
        public void SetRegion(Rectangle region, Initializer initializer, bool overwrite)
        {
            _root.SetRegion(region, initializer, overwrite);
        }

        #region Nested type: MatrixSpacePartitionNode

        private sealed class MatrixSpacePartitionNode
        {
            private readonly Rectangle _bounds;
            private readonly Point _center;
            private readonly bool _isRootNode;
            private readonly MatrixSpacePartition<T> _root;
            private T[,] _data;
            private MatrixSpacePartitionNode[] _nodes;

            public MatrixSpacePartitionNode(MatrixSpacePartition<T> root)
            {
                _root = root;
                _isRootNode = true;
                _nodes = new MatrixSpacePartitionNode[4];
                _center = new Point(0, 0);
            }

            private MatrixSpacePartitionNode(MatrixSpacePartition<T> root, Rectangle bounds, bool aggregate)
            {
                _root = root;
                _bounds = bounds;
                _center = new Point { X = bounds.X + bounds.Width / 2, Y = bounds.Y + bounds.Height / 2 };

                aggregate = aggregate || bounds.Width <= _root.BlockSize;
                if (!aggregate) _nodes = new MatrixSpacePartitionNode[4];
                else
                {
                    _data = new T[Bounds.Width, Bounds.Height];
                    Saturation = 1;
                }
            }

            private MatrixSpacePartitionNode(MatrixSpacePartition<T> root, Rectangle bounds,
                                             MatrixSpacePartitionNode subNode)
                : this(root, bounds, false)
            {
                _nodes[GetQuadrant(subNode.Bounds.Location)] = subNode;
            }

            public Rectangle Bounds
            {
                get
                {
                    if (!_isRootNode) return _bounds;

                    var bounds = new Rectangle(0, 0, 1, 1);

                    if (_nodes[0] == null && _nodes[1] == null && _nodes[2] == null && _nodes[3] == null) return bounds;

                    bounds = _nodes.First(node => node != null).Bounds;
                    return _nodes.Where(node => node != null).Aggregate(bounds,
                                                                        (current, node) =>
                                                                        BoundingRectangle(current, node.Bounds));
                }
            }

            private Point Center
            {
                get { return _center; }
            }

            private double Saturation { get; set; }

            public void CopyInto(ref T[,] data, Rectangle bounds)
            {
                if (_data == null) foreach (MatrixSpacePartitionNode node in _nodes.Where(node => node != null)) node.CopyInto(ref data, bounds);
                else
                {
                    int minX = Math.Max(Bounds.X, bounds.X);
                    int minY = Math.Max(Bounds.Y, bounds.Y);
                    int maxX = Math.Min(Bounds.X + Bounds.Width, bounds.X + bounds.Width);
                    int maxY = Math.Min(Bounds.Y + Bounds.Height, bounds.Y + bounds.Height);

                    for (int x = minX; x < maxX; x++) for (int y = minY; y < maxY; y++) data[x - bounds.X, y - bounds.Y] = _data[x - Bounds.X, y - Bounds.Y];
                }
            }

            public T Get(Point p)
            {
                if (_data != null) return _data[p.X - Bounds.X, p.Y - Bounds.Y];

                int q = GetQuadrant(p);
                if (_nodes[q] != null && _nodes[q].Bounds.Contains(p)) return _nodes[q].Get(p);

                return default(T);
            }

            public bool Get(Point p, Initializer initializer, out T value)
            {
                if (_data != null)
                {
                    value = _data[p.X - Bounds.X, p.Y - Bounds.Y];
                    return false;
                }

                int q = GetQuadrant(p);
                if (_nodes[q] != null && _nodes[q].Bounds.Contains(p))
                {
                    if (_nodes[q].Get(p, initializer, out value))
                    {
                        RecalculateSaturation();
                        return true;
                    }
                }

                value = initializer(p.X, p.Y);
                if (AddToSubnode(p, value))
                {
                    RecalculateSaturation();
                    return true;
                }
                return false;
            }

            public bool Set(Point p, T value)
            {
                if (_data != null) _data[p.X - Bounds.X, p.Y - Bounds.Y] = value;
                else
                {
                    if (AddToSubnode(p, value))
                    {
                        RecalculateSaturation();
                        return true;
                    }
                }
                return false;
            }

            public bool SetRegion(Rectangle region, Initializer initializer, bool overwrite)
            {
                if (_data != null)
                {
                    if (initializer != null)
                    {
                        int minX = Math.Max(Bounds.X, region.X);
                        int minY = Math.Max(Bounds.Y, region.Y);
                        int maxX = Math.Min(Bounds.X + Bounds.Width, region.X + region.Width);
                        int maxY = Math.Min(Bounds.Y + Bounds.Height, region.Y + region.Height);

                        if (overwrite) for (int x = minX; x < maxX; x++) for (int y = minY; y < maxY; y++) _data[x - Bounds.X, y - Bounds.Y] = initializer.Invoke(x, y);
                        else for (int x = minX; x < maxX; x++) for (int y = minY; y < maxY; y++) if (_data[x - Bounds.X, y - Bounds.Y].Equals(default(T))) _data[x - Bounds.X, y - Bounds.Y] = initializer.Invoke(x, y);
                    }
                    return false;
                }

                bool dirtied = false;

                foreach (MatrixSpacePartitionNode node in
                    _nodes.Where(node => node != null).Where(
                        node => node.Bounds == region || node.Bounds.Contains(region)))
                {
                    dirtied = node.SetRegion(region, initializer, overwrite);
                    if (dirtied) RecalculateSaturation();
                    return dirtied;
                }

                for (int q = 0; q < 4; q++)
                {
                    Rectangle qb = GetQuadrantBounds(q);
                    if (!qb.IntersectsWith(region)) continue;

                    qb.Intersect(region);

                    if (_nodes[q] == null)
                    {
                        qb = BoundingBlock(qb, _root.BlockMagnitude);
                        _nodes[q] = new MatrixSpacePartitionNode(_root, qb, false);
                        _nodes[q].SetRegion(region, initializer, overwrite);
                        dirtied = true;
                    }
                    else if (_nodes[q].Bounds == region || _nodes[q].Bounds.Contains(qb)) dirtied = _nodes[q].SetRegion(region, initializer, overwrite) || dirtied;
                    else
                    {
                        qb = BoundingRectangle(qb, _nodes[q].Bounds);
                        qb = BoundingBlock(qb, _root.BlockMagnitude);
                        _nodes[q] = new MatrixSpacePartitionNode(_root, qb, _nodes[q]);
                        _nodes[q].SetRegion(region, initializer, overwrite);
                        dirtied = true;
                    }
                }

                if (dirtied) RecalculateSaturation();
                return dirtied;
            }

            private bool AddToSubnode(Point p, T value)
            {
                int q = GetQuadrant(p);

                if (_nodes[q] == null)
                {
                    CreateSubnode(p, q);
                    _nodes[q].Set(p, value);
                    return true;
                }
                if (!_nodes[q].Bounds.Contains(p))
                {
                    ExpandSubnode(p, q);
                    _nodes[q].Set(p, value);
                    return true;
                }
                return _nodes[q].Set(p, value);
            }

            private void Aggregate()
            {
                _data = new T[Bounds.Width, Bounds.Height];
                foreach (MatrixSpacePartitionNode node in _nodes.Where(node => node != null)) node.CopyInto(ref _data, Bounds);
                _nodes = null;

                Saturation = 1;
            }

            private static int BitScanReverse(int mask)
            {
                int index = 0;
                while (mask > 0)
                {
                    mask >>= 1;
                    index++;
                }
                return index;
            }

            private static Rectangle BoundingBlock(Point p, int magnitude)
            {
                return new Rectangle
                {
                    X = (p.X & ~((1 << magnitude) - 1)),
                    Y = (p.Y & ~((1 << magnitude) - 1)),
                    Width = (1 << magnitude),
                    Height = (1 << magnitude)
                };
            }

            private static Rectangle BoundingBlock(Rectangle r, int blockMagnitude)
            {
                int msb = BitScanReverse((r.X ^ (r.X + r.Width - 1)) | (r.Y ^ (r.Y + r.Height - 1)));
                return BoundingBlock(r.Location, Math.Max(msb, blockMagnitude));
            }

            private static Rectangle BoundingRectangle(Point p, Rectangle r)
            {
                var l = new Point { X = Math.Min(r.X, p.X), Y = Math.Min(r.Y, p.Y) };

                var s = new Size
                {
                    Width = Math.Max(r.X + r.Width, p.X + 1) - l.X,
                    Height = Math.Max(r.Y + r.Height, p.Y + 1) - l.Y
                };

                return new Rectangle(l, s);
            }

            private static Rectangle BoundingRectangle(Rectangle r1, Rectangle r2)
            {
                if (r1 == r2) return r1;

                var l = new Point { X = Math.Min(r1.X, r2.X), Y = Math.Min(r1.Y, r2.Y) };

                var s = new Size
                {
                    Width = Math.Max(r1.X + r1.Width, r2.X + r2.Width) - l.X,
                    Height = Math.Max(r1.Y + r1.Height, r2.Y + r2.Height) - l.Y
                };

                return new Rectangle(l, s);
            }

            private void CreateSubnode(Point p, int q)
            {
                Rectangle b = BoundingBlock(p, _root.BlockMagnitude);
                _nodes[q] = new MatrixSpacePartitionNode(_root, b, true);
            }

            private void ExpandSubnode(Point p, int q)
            {
                MatrixSpacePartitionNode subNode = _nodes[q];
                Rectangle b = BoundingRectangle(p, subNode.Bounds);
                b = BoundingBlock(b, _root.BlockMagnitude);
                _nodes[q] = new MatrixSpacePartitionNode(_root, b, subNode);
            }

            private int GetQuadrant(Point p)
            {
                if (p.X >= Center.X) return p.Y >= Center.Y ? 0 : 3;
                return p.Y >= Center.Y ? 1 : 2;
            }

            private Rectangle GetQuadrantBounds(int index)
            {
                Rectangle q;

                if (!_isRootNode)
                {
                    q = new Rectangle(0, 0, Bounds.Width / 2, Bounds.Height / 2);
                    switch (index)
                    {
                        case 1:
                            q.X = Bounds.X;
                            q.Y = Center.Y;
                            return q;
                        case 2:
                            q.Location = Bounds.Location;
                            return q;
                        case 3:
                            q.X = Center.X;
                            q.Y = Bounds.Y;
                            return q;
                        default:
                            q.Location = Center;
                            return q;
                    }
                }

                q = new Rectangle(0, 0, int.MaxValue, int.MaxValue);
                switch (index)
                {
                    case 1:
                        q.X = -int.MaxValue;
                        return q;
                    case 2:
                        q.X = -int.MaxValue;
                        q.Y = -int.MaxValue;
                        return q;
                    case 3:
                        q.Y = -int.MaxValue;
                        return q;
                    default:
                        return q;
                }
            }

            private void RecalculateSaturation()
            {
                if (_isRootNode) Saturation = 0;
                else
                {
                    double s = (from node in _nodes.Where(node => node != null)
                                let p = (node.Bounds.Height * node.Bounds.Width) / (double)(Bounds.Height * Bounds.Width)
                                select (node.Saturation * p)).Sum();

                    if (s >= _root.SaturationThreshold) Aggregate();
                    else Saturation = s;
                }
            }
        }

        #endregion
    }
}