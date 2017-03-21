using System;
using System.Drawing;
using System.Drawing.Imaging;

/// <summary>
/// Canny边缘检测
/// </summary>
public class Canny
{
    // Sobel算子
    private float[,] Gx;
    private float[,] Gy;

    // 双边阈值
    private float TMin;
    private float TMax;

    public Canny()
    {
        Gx = new float[,]
        {
            { -1, 0, 1 },
            { -2, 0, 2 },
            { -1, 0, 1 }
        };
        Gy = new float[,]
        {
            { -1, -2, -1 },
            { 0, 0, 0 },
            { 1, 2, 1 }
        };
        TMin = 10;
        TMax = 40;
    }

    public bool Process(Bitmap from, out Bitmap to)
    {
        byte[] source = null;
        double[,] gradients = null;
        double[,] angles = null;
        byte[,] nms = null;
        byte[] pixels = null;
        BitmapData fromData = from.LockBits(new Rectangle(0, 0, from.Width, from.Height), ImageLockMode.ReadOnly, from.PixelFormat);
        LoadSource(fromData, out source);
        SobelFilter(fromData, source, out gradients, out angles);
        NoneMaximumSuppression(fromData, gradients, angles, out nms);
        EdgeTracking(fromData, nms);
        NMSToPixels(fromData, nms, out pixels);
        to = new Bitmap(from.Width, from.Height, from.PixelFormat);
        BitmapData toData = to.LockBits(new Rectangle(0, 0, to.Width, to.Height), ImageLockMode.WriteOnly, to.PixelFormat);
        IntPtr toPtr = toData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, toPtr, pixels.Length);
        from.UnlockBits(fromData);
        to.UnlockBits(toData);
        return true;
    }

    private void LoadSource(BitmapData fromData, out byte[] source)
    {
        IntPtr fromPtr = fromData.Scan0;
        int byteSize = Math.Abs(fromData.Stride) * fromData.Height;
        int bbp = fromData.Stride / fromData.Width;
        if (bbp < 3) throw new NotSupportedException();
        source = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(fromPtr, source, 0, source.Length);
    }

    /// <summary>
    /// sobel filter --> gradient magnitude and gradient angle
    /// </summary>
    /// <param name="bmpData"></param>
    /// <param name="source"></param>
    /// <param name="gradients"></param>
    /// <param name="angles"></param>
    private void SobelFilter(BitmapData bmpData, byte[] source, out double[,] gradients, out double[,] angles)
    {
        int bbp = bmpData.Stride / bmpData.Width;
        gradients = new double[bmpData.Width, bmpData.Height];
        angles = new double[bmpData.Width, bmpData.Height];
        int maxH = bmpData.Height - 2;
        int maxW = bmpData.Width - 2;
        for (int j = 1; j <= maxH; j++)
        {
            int bi = j * bmpData.Stride;
            for (int i = 1; i <= maxW; i++)
            {
                double gx = 0;
                double gy = 0;
                int gimin = i - 1;
                int gimax = i + 1;
                int gjmin = j - 1;
                int gjmax = j + 1;
                for (int gj = gjmin; gj <= gjmax; gj++)
                {
                    int gbi = gj * bmpData.Stride;
                    for (int gi = gimin; gi <= gimax; gi++)
                    {
                        int gii = gbi + gi * bbp;
                        int fx = gi + 1 - i;
                        int fy = gj + 1 - j;
                        gx += source[gii] * Gx[fy, fx];
                        gy += source[gii] * Gy[fy, fx];
                    }
                }
                double magnitude = Math.Sqrt(gx * gx + gy * gy);
                double angle = Math.Atan2(gy, gx) / Math.PI * 180;
                if (angle < 0) angle = angle + 180;
                gradients[i, j] = magnitude;
                angles[i, j] = angle;
            }
        }
    }

    /// <summary>
    /// 非极大值抑制 --> 0 none, 1 weak edge, 2 strong edge
    /// </summary>
    /// <param name="bmpData"></param>
    /// <param name="gradients"></param>
    /// <param name="angles"></param>
    /// <param name="nms"></param>
    private void NoneMaximumSuppression(BitmapData bmpData, double[,] gradients, double[,] angles, out byte[,] nms)
    {
        nms = new byte[bmpData.Width, bmpData.Height];
        int maxH = bmpData.Height - 2;
        int maxW = bmpData.Width - 2;
        for (int j = 1; j <= maxH; j++)
        {
            int bi = j * bmpData.Stride;
            for (int i = 1; i <= maxW; i++)
            {
                double magnitude = gradients[i, j];
                double angle = angles[i, j];
                double v1 = 0;
                double v2 = 0;
                if (angle >= 22.5f && angle < 67.5f)
                {
                    v1 = gradients[i - 1, j - 1];
                    v2 = gradients[i + 1, j + 1];
                }
                else if (angle >= 67.5f && angle < 112.5f)
                {
                    v1 = gradients[i, j - 1];
                    v2 = gradients[i, j + 1]; ;
                }
                else if (angle >= 112.5f && angle < 157.5f)
                {
                    v1 = gradients[i + 1, j - 1];
                    v2 = gradients[i - 1, j + 1];
                }
                else
                {
                    v1 = gradients[i - 1, j];
                    v2 = gradients[i + 1, j];
                }
                if (magnitude >= v1 && magnitude >= v2)
                {
                    if (magnitude >= TMax) nms[i, j] = 2;
                    else if (magnitude >= TMin) nms[i, j] = 1;
                }
            }
        }
    }

    /// <summary>
    /// Edge tracking by hysteresis(recursive)
    /// </summary>
    /// <param name="bmpData"></param>
    /// <param name="nms"></param>
    private void EdgeTracking(BitmapData bmpData, byte[,] nms)
    {
        bool[,] visited = new bool[bmpData.Width, bmpData.Height];
        int maxH = bmpData.Height - 2;
        int maxW = bmpData.Width - 2;
        for (int j = 1; j <= maxH; j++)
        {
            for (int i = 1; i <= maxW; i++)
            {
                if (nms[i, j] == 2) Hysteresis(nms, visited, i, j);
            }
        }
    }

    private void Hysteresis(byte[,] nms, bool[,] visited, int i, int j)
    {
        if (visited[i, j]) return;
        visited[i, j] = true;
        int gimin = i - 1;
        int gimax = i + 1;
        int gjmin = j - 1;
        int gjmax = j + 1;
        for (int gj = gjmin; gj <= gjmax; gj++)
        {
            for (int gi = gimin; gi <= gimax; gi++)
            {
                if (nms[gi, gj] == 1)
                {
                    nms[gi, gj] = 2;
                    Hysteresis(nms, visited, gi, gj);
                }
            }
        }
    }

    private void NMSToPixels(BitmapData bmpData, byte[,] nms, out byte[] pixels)
    {
        IntPtr bmpPtr = bmpData.Scan0;
        int byteSize = Math.Abs(bmpData.Stride) * bmpData.Height;
        int bbp = bmpData.Stride / bmpData.Width;
        pixels = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(bmpPtr, pixels, 0, pixels.Length);
        for (int j = 0; j < bmpData.Height; j++)
        {
            int bi = j * bmpData.Stride;
            for (int i = 0; i < bmpData.Width; i++)
            {
                int ii = bi + i * bbp;
                if (nms[i, j] == 2)
                {
                    pixels[ii] = pixels[ii + 1] = pixels[ii + 2] = 255;
                }
            }
        }
    }
}