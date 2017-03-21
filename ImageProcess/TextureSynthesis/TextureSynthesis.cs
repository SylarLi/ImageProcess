using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

public class TextureSynthesis
{
    public const bool R = false;

    private Bitmap from;

    private Bitmap noise;

    private int N;

    private int L;

    public TextureSynthesis(Bitmap from, Bitmap noise, int N, int L)
    {
        BitmapData fromData = from.LockBits(new Rectangle(0, 0, from.Width, from.Height), ImageLockMode.ReadOnly, from.PixelFormat);
        IntPtr fromPtr = fromData.Scan0;
        int fromBbp = fromData.Stride / from.Width;
        if (fromBbp < 3) throw new NotSupportedException();
        from.UnlockBits(fromData);

        BitmapData noiseData = noise.LockBits(new Rectangle(0, 0, noise.Width, noise.Height), ImageLockMode.ReadOnly, noise.PixelFormat);
        IntPtr noisePtr = noiseData.Scan0;
        int noiseBbp = noiseData.Stride / noise.Width;
        if (noiseBbp < 3) throw new NotSupportedException();
        noise.UnlockBits(noiseData);

        if (fromBbp != noiseBbp ||
            N % 2 != 1 || 
            L < 1) throw new NotSupportedException();

        this.from = from;
        this.noise = noise;
        this.N = N;
        this.L = L;
    }

    public bool Process(out Bitmap to)
    {
        LevelData[] Ga = BuildPyramid(from);
        LevelData[] Gs = BuildPyramid(noise);
        CollectNeighbor(Ga);
        for (int i = Gs.Length - 1; i >= 0; i--)
        {
            for (int y = 0; y < noise.Height; y++)
            {
                int bi = y * Gs[i].stride;
                for (int x = 0; x < noise.Width; x++)
                {
                    int ii = bi + x * Gs[i].bbp;
                    byte[] color = FindBestMatch(Ga, Gs, i, x, y);
                    for (int p = 0; p < Gs[i].bbp; p++)
                    {
                        Gs[i].bytes[ii + p] = color[p];
                    }
                }
                //break;
            }
            //break;
        }
        to = new Bitmap(Gs[0].size, Gs[0].size, noise.PixelFormat);
        BitmapData toData = to.LockBits(new Rectangle(0, 0, to.Width, to.Height), ImageLockMode.WriteOnly, to.PixelFormat);
        IntPtr toPtr = toData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(Gs[0].bytes, 0, toPtr, Gs[0].bytes.Length);
        to.UnlockBits(toData);
        return true;
    }

    private void TrainData(LevelData[] G)
    {
        
    }

    private byte[] FindBestMatch(LevelData[] Ga, LevelData[] Gs, int i, int x, int y)
    {
        byte[] color = new byte[Ga[i].bbp];
        int minDiff = int.MaxValue;
        byte[] Ns = BuildNeighbor(Gs, i, x, y);
        for (int ya = 0; ya < Ga[i].size; ya++)
        {
            int bi = ya * Ga[i].stride;
            for (int xa = 0; xa < Ga[i].size; xa++)
            {
                byte[] Na = Ga[i].NB[xa][ya];
                int diff = ColorDiff(Ns, Na);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    int ii = bi + xa * Ga[i].bbp;
                    for (int p = 0; p < color.Length; p++)
                    {
                        color[p] = Ga[i].bytes[ii + p];
                    }
                }
            }
        }
        return color;
    }

    private int ColorDiff(byte[] b1, byte[] b2)
    {
        if (b1.Length != b2.Length) throw new InvalidOperationException();
        int diff = 0;
        for (int i = 0; i < b1.Length; i++)
        {
            int minus = b1[i] - b2[i];
            diff += minus * minus;
        }
        return diff;
    }

    /// <summary>
    /// neighbor取当前level和level+1层级的像素
    /// </summary>
    /// <param name="G"></param>
    /// <param name="i"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private byte[] BuildNeighbor(LevelData[] G, int i, int x, int y)
    {
        byte[] nbs = new byte[G[i].NL * G[i].bbp];
        int x2 = x / (i + 1);
        int y2 = y / (i + 1);
        int index = 0;
        for (int level = i; level <= i + 1 && level < G.Length; level++)
        {
            var data = G[level];
            int n2 = data.N2;
            for (int iy = y2 - n2; iy <= y2 + n2; iy++)
            {
                if (level == i && iy > y2) continue;
                int riy = iy;
                if (riy < 0) riy = riy + data.size;
                else if (riy >= data.size) riy = riy - data.size;
                int bi = riy * data.stride;
                for (int ix = x2 - n2; ix <= x2 + n2; ix++)
                {
                    if (level == i && iy == y2 && ix > x2) continue;
                    int rix = ix;
                    if (rix < 0) rix = rix + data.size;
                    else if (rix >= data.size) rix = rix - data.size;
                    int ii = bi + rix * data.bbp;
                    for (int p = 0; p < data.bbp; p++)
                    {
                        nbs[index++] = data.bytes[ii + p];
                    }
                }
            }
            x2 = (int)(x2 * 0.5f);
            y2 = (int)(y2 * 0.5f);
        }
        return nbs;
    }

    private LevelData[] BuildPyramid(Bitmap bitmap)
    {
        Bitmap[] pyramis = new Bitmap[L];
        pyramis[0] = bitmap;
        for (int i = 1; i < L; i++)
        {
            Bitmap last = pyramis[i - 1];
            pyramis[i] = ResizeImage(last, last.Width / 2, last.Height / 2);
        }
        LevelData[] G = new LevelData[pyramis.Length];
        for (int i = G.Length - 1; i >= 0; i--)
        {
            LevelData ld = new LevelData();
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = data.Scan0;
            int byteSize = Math.Abs(data.Stride) * data.Height;
            byte[] values = new byte[byteSize];
            System.Runtime.InteropServices.Marshal.Copy(ptr, values, 0, values.Length);
            bitmap.UnlockBits(data);
            ld.bytes = values;
            ld.size = data.Width;
            ld.bbp = data.Stride / data.Width;
            ld.stride = ld.size * ld.bbp;
            ld.N = (int)Math.Ceiling(N / (i + 1f));
            ld.N2 = (int)(ld.N * 0.5f);
            ld.NL = ld.N * ld.N2 + ld.N2 + 1;
            if (i + 1 < G.Length) ld.NL += G[i + 1].N * G[i + 1].N;
            G[i] = ld;
        }
        return G;
    }

    private void CollectNeighbor(LevelData[] G)
    {
        for (int level = G.Length - 1; level >= 0; level--)
        {
            G[level].NB = new byte[G[level].size][][];
            for (int xa = 0; xa < G[level].size; xa++)
            {
                G[level].NB[xa] = new byte[G[level].size][];
                for (int ya = 0; ya < G[level].size; ya++)
                {
                    G[level].NB[xa][ya] = BuildNeighbor(G, level, xa, ya);
                }
            }
        }
    }

    private Bitmap ResizeImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height, image.PixelFormat);
        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }
        return destImage;
    }

    private class LevelData
    {
        public byte[] bytes;                    // 像素

        public int size;                        // 宽高

        public int bbp;                         // bytes per pixel

        public int stride;                      // size * bbp

        public int N;                           // neighbor范围
                
        public int N2;                          // (int)(N * 0.5f)

        public int NL;                          // neighbor总数量

        public byte[][][] NB;                   // neighbor列表
    }
}