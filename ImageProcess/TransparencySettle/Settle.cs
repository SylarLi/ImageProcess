using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Settle
{
    private enum CheckTransparencyDensity
    {
        Low,
        Middium,
        High,
    }

    private const int TexturePadding = 1;

    private const float SplitRatioThreshold = 2.5f;

    private const int StartBinSize = 512;
    private const int MaxBinSize = 2048;

    private Bitmap mSource;

    private int mBlockWidth;

    private int mBlockHeight;

    private int mRows;

    private int mColumns;

    private BitmapData mSourceData;

    private byte[] mSourceBytes;

    public Settle(Bitmap source, int blockWidth, int blockHeight)
    {
        mSource = source;
        mBlockWidth = blockWidth;
        mBlockHeight = blockHeight;
        mSourceData = mSource.LockBits(new Rectangle(0, 0, mSource.Width, mSource.Height), ImageLockMode.ReadOnly, mSource.PixelFormat);
        int byteSize = Math.Abs(mSourceData.Stride) * mSourceData.Height;
        int bbp = mSourceData.Stride / mSource.Width;
        if (bbp != 4) throw new NotSupportedException();
        mSourceBytes = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(mSourceData.Scan0, mSourceBytes, 0, mSourceBytes.Length);
        mRows = (int)Math.Ceiling(mSource.Height / (float)mBlockHeight);
        mColumns = (int)Math.Ceiling(mSource.Width / (float)mBlockWidth);
    }

    public bool Parse(out Bitmap texture, out Mesh mesh)
    {
        var blocks = GenerateNonTransparentBlocks();
        var rects = SliceSearchSettleRect(blocks);
        Array.Sort(rects, (r1, r2) =>
        {
            return r2.area - r1.area;
        });
        var pixelRects = Array.ConvertAll(rects, rect =>
        {
            return new Rect(
                rect.top * mBlockHeight,
                rect.left * mBlockWidth,
                Math.Min((rect.bottom + 1) * mBlockHeight, mSourceData.Height) - 1,
                Math.Min((rect.right + 1) * mBlockWidth, mSourceData.Width) - 1);
        });
        var meshRects = Array.ConvertAll(pixelRects, rect =>
        {
            return new Rect(
                Math.Max(rect.top, 0),
                Math.Max(rect.left, 0),
                Math.Min(rect.bottom + 1, mSourceData.Height - 1),
                Math.Min(rect.right + 1, mSourceData.Width - 1));
        });
        var textureRects = Array.ConvertAll(pixelRects, rect =>
        {
            return new Rect(
                rect.top - TexturePadding,
                rect.left - TexturePadding,
                rect.bottom + TexturePadding,
                rect.right + TexturePadding);
        });
        var textureAreas = Array.ConvertAll(textureRects, rect =>
        {
            return new RectBinPacker.Area(
                rect.right - rect.left + 1,
                rect.bottom - rect.top + 1);
        });
        int binSize = StartBinSize;
        while (binSize <= MaxBinSize)
        {
            RectBinPacker.Rect[] packedRects;
            var bin = new RectBinPacker.Bin(binSize, binSize);
            var packer = new RectBinPacker(RectBinPacker.Algorithm.Skyline);
            if (packer.Pack(
                bin,
                textureAreas,
                out packedRects))
            {
                var binStride = bin.width * 4;
                int byteSize = binStride * bin.height;
                byte[] values = new byte[byteSize];
                int maxHeight = 0;
                var finalRects = new Rect[packedRects.Length];
                for (int p = 0; p < textureRects.Length; p++)
                {
                    var packedRect = packedRects[p];
                    var finalRect = new Rect(
                        bin.height - 1 - packedRect.top,
                        packedRect.left,
                        bin.height - 1 - packedRect.bottom,
                        packedRect.right);
                    var fromRect = textureRects[p];
                    for (int j = 0; j < fromRect.height; j++)
                    {
                        int br = j + fromRect.top;
                        int bi = br * mSourceData.Stride;
                        int bi2 = (j + finalRect.top) * binStride;
                        for (int i = 0; i < fromRect.width; i++)
                        {
                            int bc = i + fromRect.left;
                            int ii = bi + bc * 4;
                            int ii2 = bi2 + (i + finalRect.left) * 4;
                            if (br >= 0 && br < mSourceData.Height &&
                                bc >= 0 && bc < mSourceData.Width)
                            {
                                values[ii2] = mSourceBytes[ii];
                                values[ii2 + 1] = mSourceBytes[ii + 1];
                                values[ii2 + 2] = mSourceBytes[ii + 2];
                                values[ii2 + 3] = mSourceBytes[ii + 3];
                            }
                        }
                    }
                    maxHeight = Math.Max(bin.height - finalRect.top, maxHeight);
                    finalRects[p] = finalRect;
                }
                var slicedHeight = bin.height - maxHeight;
                for (int i = 0; i < finalRects.Length; i++)
                {
                    finalRects[i].top -= slicedHeight;
                    finalRects[i].bottom -= slicedHeight;
                }
                var shade = new Bitmap(bin.width, maxHeight, mSource.PixelFormat);
                BitmapData shadeData = shade.LockBits(new Rectangle(0, 0, shade.Width, shade.Height), ImageLockMode.WriteOnly, shade.PixelFormat);
                IntPtr shadePtr = shadeData.Scan0;
                var shadeByteSize = shadeData.Stride * shadeData.Height;
                System.Runtime.InteropServices.Marshal.Copy(values, values.Length - shadeByteSize, shadePtr, shadeByteSize);
                shade.UnlockBits(shadeData);
                texture = shade;
                mesh = GenerateMeshData(rects, meshRects, finalRects, bin.width, maxHeight);
                return true;
            }
            binSize *= 2;
        }
        texture = null;
        mesh = null;
        return false;
    }

    private Mesh GenerateMeshData(Rect[] rawRects, Rect[] meshRects, Rect[] textureRects, int binWidth, int binHeight)
    {
        var vertices = new Vector3[meshRects.Length * 4];
        var uvs = new Vector2[meshRects.Length * 4];
        var triangles = new int[meshRects.Length * 6];
        for (int i = 0; i < meshRects.Length; i++)
        {
            var meshRect = meshRects[i];
            var xMin = meshRect.left;
            var xMax = meshRect.right;
            var yMin = mSourceData.Height - 1 - meshRect.bottom;
            var yMax = mSourceData.Height - 1 - meshRect.top;
            vertices[i * 4] = new Vector3(xMin, yMin, 0);
            vertices[i * 4 + 1] = new Vector3(xMin, yMax, 0);
            vertices[i * 4 + 2] = new Vector3(xMax, yMax, 0);
            vertices[i * 4 + 3] = new Vector3(xMax, yMin, 0);
            var rawRect = rawRects[i];
            var textureRect = textureRects[i];
            var uOffsetLeft = TexturePadding;
            var uOffsetRight = TexturePadding - 1;
            var uOffsetTop = TexturePadding;
            var uOffsetBottom = TexturePadding - 1;
            var uMin = (textureRect.left + uOffsetLeft) / (float)binWidth;
            var uMax = (textureRect.right - uOffsetRight) / (float)binWidth;
            var vMin = 1 - (textureRect.bottom - uOffsetBottom) / (float)binHeight;
            var vMax = 1 - (textureRect.top + uOffsetTop) / (float)binHeight;
            uvs[i * 4] = new Vector2(uMin, vMin);
            uvs[i * 4 + 1] = new Vector2(uMin, vMax);
            uvs[i * 4 + 2] = new Vector2(uMax, vMax);
            uvs[i * 4 + 3] = new Vector2(uMax, vMin);
            triangles[i * 6] = i * 4;
            triangles[i * 6 + 1] = i * 4 + 1;
            triangles[i * 6 + 2] = i * 4 + 2;
            triangles[i * 6 + 3] = i * 4 + 2;
            triangles[i * 6 + 4] = i * 4 + 3;
            triangles[i * 6 + 5] = i * 4;
        }
        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uvs = uvs;
        mesh.triangles = triangles;
        return mesh;
    }

    private Rect[] SliceSearchSettleRect(Block[,] blocks)
    {
        List<Rect> settleRects = new List<Rect>();
        int bottom = 0, right = 0;
        while (true)
        {
            int maxSize = 0;
            int fTop = 0, fLeft = 0, fBottom = 0, fRight = 0;
            for (int i = 0; i < mRows; i++)
            {
                for (int j = 0; j < mColumns; j++)
                {
                    var block = blocks[i, j];
                    if (block != null)
                    {
                        int size = SearchMaxContinousBlocks(blocks, i, j, out bottom, out right);
                        if (maxSize < size)
                        {
                            maxSize = size;
                            fTop = i;
                            fLeft = j;
                            fBottom = bottom;
                            fRight = right;
                        }
                    }
                }
            }
            var fRect = new Rect(fTop, fLeft, fBottom, fRight);
            if (fRect.height > fRect.width &&
                ((float)fRect.height / fRect.width >= SplitRatioThreshold || 
                fRect.height * mBlockHeight > 512))
            {
                // 高宽比大于SplitRatioThreshold或高度大于512，进行分割
                int center = (fRect.bottom + fRect.top) / 2;
                settleRects.Add(new Rect(fTop, fLeft, center, fRight));
                settleRects.Add(new Rect(center + 1, fLeft, fBottom, fRight));
            }
            else if (fRect.width > fRect.height &&
                ((float)fRect.width / fRect.height >= SplitRatioThreshold ||
                fRect.width * mBlockWidth > 512))
            {
                // 宽高比大于SplitRatioThreshold或宽度大于512，进行分割
                int center = (fRect.left + fRect.right) / 2;
                settleRects.Add(new Rect(fTop, fLeft, fBottom, center));
                settleRects.Add(new Rect(fTop, center + 1, fBottom, fRight));
            }
            else
            {
                settleRects.Add(fRect);
            }
            for (int i = fTop; i <= fBottom; i++)
            {
                for (int j = fLeft; j <= fRight; j++)
                {
                    blocks[i, j] = null;
                }
            }
            if (fBottom - fTop == 0 && fRight - fLeft == 0)
            {
                for (int i = 0; i < mRows; i++)
                {
                    for (int j = 0; j < mColumns; j++)
                    {
                        if (blocks[i, j] != null)
                        {
                            settleRects.Add(new Rect(i, j, i, j));
                        }
                    }
                }
                break;
            }
        }
        return settleRects.ToArray();
    }

    private bool CheckRectTransparency(Rectangle rect, CheckTransparencyDensity density)
    {
        int step = 1;
        switch (density)
        {
            case CheckTransparencyDensity.Low:
                {
                    step = 5;
                    break;
                }
            case CheckTransparencyDensity.Middium:
                {
                    step = 3;
                    break;
                }
            case CheckTransparencyDensity.High:
                {
                    step = 1;
                    break;
                }
        }
        for (int j = rect.Top; j < rect.Bottom; j += step)
        {
            int bi = j * mSourceData.Stride;
            for (int i = rect.Left; i < rect.Right; i += step)
            {
                int ii = bi + i * 4;
                if (mSourceBytes[ii + 3] > 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private Block[,] GenerateNonTransparentBlocks()
    {
        var blocks = new Block[mRows, mColumns];
        for (int j = 0; j < mColumns; j++)
        {
            int x = j * mBlockWidth;
            for (int i = 0; i < mRows; i++)
            {
                int y = i * mBlockHeight;
                var rect = new Rectangle(
                    x, y, 
                    Math.Min(mSource.Width, x + mBlockWidth) - x,
                    Math.Min(mSource.Height, y + mBlockHeight) - y);
                if (!CheckRectTransparency(rect, CheckTransparencyDensity.High))
                {
                    blocks[i, j] = new Block()
                    {
                        row = i,
                        column = j
                    };
                }
            }
        }
        return blocks;
    }

    private int SearchMaxContinousBlocks(Block[,] blocks, int top, int left, out int bottom, out int right)
    {
        // 优先横向搜索
        int hRight = mColumns - 1;
        int hI = top;
        while(hI < mRows)
        {
            if (blocks[hI, left] != null)
            {
                int hJ = left;
                while (++hJ <= hRight)
                {
                    if (blocks[hI, hJ] == null)
                    {
                        hJ -= 1;
                        break;
                    }
                }
                hRight = Math.Min(hRight, hJ);
                hI += 1;
            }
            else
            {
                hI -= 1;
                break;
            }
        }
        int hBottom = Math.Min(hI, mRows - 1);

        // 优先纵向搜索
        int vBottom = mRows - 1;
        int vJ = left;
        while (vJ < mColumns)
        {
            if (blocks[top, vJ] != null)
            {
                int vI = top;
                while (++vI <= vBottom)
                {
                    if (blocks[vI, vJ] == null)
                    {
                        vI -= 1;
                        break;
                    }
                }
                vBottom = Math.Min(vBottom, vI);
                vJ += 1;
            }
            else
            {
                vJ -= 1;
                break;
            }
        }
        int vRight = Math.Min(vJ, mColumns - 1);

        int hSize = (hRight - left + 1) * (hBottom - top + 1);
        int vSize = (vRight - left + 1) * (vBottom - top + 1);
        if (hSize >= vSize)
        {
            right = hRight;
            bottom = hBottom;
            return hSize;
        }
        else
        {
            right = vRight;
            bottom = vBottom;
            return vSize;
        }
    }

    public void Dispose()
    {
        mSource.UnlockBits(mSourceData);
        mSource = null;
        mSourceData = null;
        mSourceBytes = null;
    }

    private class Block
    {
        public int row;
        public int column;
    }

    private class Rect
    {
        public int top;
        public int left;
        public int bottom;
        public int right;
        public int width;
        public int height;
        public int area;

        public Rect(int top, int left, int bottom, int right)
        {
            Set(top, left, bottom, right);
        }

        public void Set(int top, int left, int bottom, int right)
        {
            this.top = top;
            this.left = left;
            this.bottom = bottom;
            this.right = right;
            width = right - left + 1;
            height = bottom - top + 1;
            area = width * height;
        }
    }

    public class Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public class Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class Mesh
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] triangles;
        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                writer.Write(vertices[i].x);
                writer.Write(vertices[i].y);
                writer.Write(vertices[i].z);
            }
            writer.Write(uvs.Length);
            for (int i = 0; i < uvs.Length; i++)
            {
                writer.Write(uvs[i].x);
                writer.Write(uvs[i].y);
            }
            writer.Write(triangles.Length);
            for (int i = 0; i < triangles.Length; i++)
            {
                writer.Write(triangles[i]);
            }
        }
    }
}