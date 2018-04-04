using System;
using System.Collections.Generic;
using System.Linq;

public class RectBinPacker
{
    public enum Algorithm
    {
        Skyline,
    }

    private Algorithm mAlgorithm;

    public RectBinPacker(Algorithm algorithm)
    {
        mAlgorithm = algorithm;
    }

    public bool Pack(Bin bin, Area[] areas, out Rect[] rects)
    {
        IPackAlgorithm packAlgorithm = null;
        switch (mAlgorithm)
        {
            case Algorithm.Skyline:
                {
                    packAlgorithm = new SkylineAlgorithm(bin, areas);
                    break;
                }
            default:
                {
                    throw new NotSupportedException();
                }
        }
        return packAlgorithm.Pack(out rects);
    }

    public class Area
    {
        public int width;

        public int height;

        public Area(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }

    public class Bin: Area
    {
        public Bin(int width, int height): base(width, height)
        {

        }
    }

    public class Rect
    {
        public int top;

        public int bottom;

        public int left;

        public int right;

        public Rect()
        {

        }

        public Rect(int top, int left, int bottom, int right)
        {
            this.top = top;
            this.left = left;
            this.bottom = bottom;
            this.right = right;
        }
    }

    private interface IPackAlgorithm
    {
        bool Pack(out Rect[] rects);
    }

    private abstract class PackAlgorithm : IPackAlgorithm
    {
        protected Area[] mAreas;

        protected Bin mBin;

        public PackAlgorithm(Bin bin, Area[] areas)
        {
            mBin = bin;
            mAreas = areas;
        }

        public virtual bool Pack(out Rect[] rects)
        {
            throw new NotImplementedException();
        }
    }

    // Horizontal Skyline
    private class SkylineAlgorithm: PackAlgorithm
    {
        public SkylineAlgorithm(Bin bin, Area[] areas): base(bin, areas)
        {

        }

        public override bool Pack(out Rect[] rects)
        {
            rects = new Rect[mAreas.Length];
            var edges = new List<Skyline>();
            edges.Add(new Skyline(0, mBin.width - 1, 0));
            for (int i = 0; i < mAreas.Length; i++)
            {
                rects[i] = AddAreaByBL(edges, mAreas[i]);
                if (rects[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        // Priority: bottom left
        private Rect AddAreaByBL(List<Skyline> edges, Area area)
        {
            int index = -1;
            var tindex = -1;
            for (int i = 0; i < edges.Count; i++)
            {
                var areaHeight = edges[i].height + area.height;
                var areaTo = edges[i].from + area.width - 1;
                if (areaHeight <= mBin.height && areaTo < mBin.width)
                {
                    var j = i;
                    while (j < edges.Count)
                    {
                        if (edges[j].height > edges[i].height)
                        {
                            break;
                        }
                        if (edges[j].to >= areaTo)
                        {
                            if (index == -1 ||
                                edges[i].height < edges[index].height ||
                                edges[i].from < edges[index].from)
                            {
                                index = i;
                                tindex = j;
                            }
                            break;
                        }
                        j += 1;
                    }
                }
            }
            if (index != -1 && tindex != -1)
            {
                var rect = new Rect();
                var edge1 = edges[index];
                var edge2 = edges[tindex];
                var areaFrom = edge1.from;
                var areaTo = edge1.from + area.width - 1;
                var areaHeight = edge1.height + area.height;
                edges.RemoveRange(index, tindex - index + 1);
                edges.Insert(index, new Skyline(areaFrom, areaTo, areaHeight));
                if (areaTo < edge2.to)
                {
                    edges.Insert(index + 1, new Skyline(areaTo + 1, edge2.to, edge2.height));
                }
                return new Rect(areaHeight - 1, areaFrom, edge1.height, areaTo);
            }
            return null;
        }

        private class Skyline
        {
            public int from;

            public int to;

            public int height;

            public Skyline(int from, int to, int height)
            {
                this.from = from;
                this.to = to;
                this.height = height;
            }
        }
    }
}