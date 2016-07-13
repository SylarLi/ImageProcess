using System;
using System.Drawing;
using System.Drawing.Imaging;

public class Canny
{
    /// <summary>
    /// Sobel算子
    /// </summary>
    private float[,] Gx;
    private float[,] Gy;
    private float Min;
    private float Max;

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
        Max = 100;
        Min = Max / 4;
    }

    /// <summary>
    /// Canny边缘检测
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool Process(Bitmap from, out Bitmap to)
    {
        BitmapData fromData = from.LockBits(new Rectangle(0, 0, from.Width, from.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        IntPtr fromPtr = fromData.Scan0;
        int byteSize = Math.Abs(fromData.Stride) * fromData.Height;
        byte[] source = new byte[byteSize];
        double[,,] gradient = new double[fromData.Width, fromData.Height, 2];
        byte[,] edge = new byte[fromData.Width, fromData.Height];
        byte[] values = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(fromPtr, source, 0, source.Length);
        // Sobel算子滤波(Sobel filter)
        int kernel = Gx.GetLength(0);
        int kernel2 = kernel / 2;
        for (int j = 0; j < fromData.Height; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = 0; i < fromData.Width; i++)
            {
                double gx = 0;
                double gy = 0;
                int gimin = Math.Max(0, i - kernel2);
                int gimax = Math.Min(fromData.Width - 1, i + kernel2);
                int gjmin = Math.Max(0, j - kernel2);
                int gjmax = Math.Min(fromData.Height - 1, j + kernel2);
                for (int gj = gjmin; gj <= gjmax; gj++)
                {
                    int gbi = gj * fromData.Stride;
                    for (int gi = gimin; gi <= gimax; gi++)
                    {
                        int gii = gbi + gi * 3;
                        int fx = gi + kernel2 - i;
                        int fy = gj + kernel2 - j;
                        gx += source[gii] * Gx[fx, fy];
                        gy += source[gii] * Gy[fx, fy];
                    }
                }
                int ii = bi + i * 3;
                double magnitude = Math.Sqrt(gx * gx + gy * gy);
                double angle = Math.Atan2(gy, gx) / Math.PI * 180;
                if (angle < 0) angle = angle + 180;
                gradient[i, j, 0] = magnitude;
                gradient[i, j, 1] = angle;
            }
        }
        // 非极大值抑制(None-maximum suppression)
        for (int j = 0; j < fromData.Height; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = 0; i < fromData.Width; i++)
            {
                int ii = bi + i * 3;
                double magnitude = gradient[i, j, 0];
                double angle = gradient[i, j, 1];
                double left = 0;
                double right = 0;
                double top = 0;
                double bottom = 0;
                double topLeft = 0;
                double topRight = 0;
                double bottomLeft = 0;
                double bottomRight = 0;
                if (i - 1 >= 0)
                {
                    left = gradient[i - 1, j, 0];
                    if (j - 1 >= 0) topLeft = gradient[i - 1, j - 1, 0];
                    if (j + 1 < fromData.Height) bottomLeft = gradient[i - 1, j + 1, 0];
                }
                if (i + 1 < fromData.Width)
                {
                    right = gradient[i + 1, j, 0];
                    if (j - 1 >= 0) topRight = gradient[i + 1, j - 1, 0];
                    if (j + 1 < fromData.Height) bottomRight = gradient[i + 1, j + 1, 0];
                }
                if (j - 1 >= 0) top = gradient[i, j - 1, 0];
                if (j + 1 < fromData.Height) bottom = gradient[i, j + 1, 0];
                double v1 = 0;
                double v2 = 0;
                if (angle >= 22.5f && angle < 67.5f)
                {
                    v1 = topRight;
                    v2 = bottomLeft;
                }
                else if (angle >= 67.5f && angle < 112.5f)
                {
                    v1 = top;
                    v2 = bottom;
                }
                else if (angle >= 112.5f && angle < 157.5f)
                {
                    v1 = topLeft;
                    v2 = bottomRight;
                }
                else
                {
                    v1 = right;
                    v2 = left;
                }
                // 0 none, 1 weak, 2 strong
                edge[i, j] = 0;
                if (magnitude >= v1 && magnitude >= v2)
                {
                    if (magnitude >= Max)
                    {
                        edge[i, j] = 2;
                    }
                    else if (magnitude >= Min)
                    {
                        edge[i, j] = 1;
                    }
                }
            }
        }
        // Edge tracking(blob analysis)
        bool[,] visited = new bool[fromData.Width, fromData.Height];
        for (int j = kernel2; j < fromData.Height - kernel2; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = kernel2; i < fromData.Width - kernel2; i++)
            {
                int ii = bi + i * 3;
                if (edge[i, j] == 2)
                {
                    Hysteresis(edge, visited, i, j);
                }
            }
        }
        for (int j = 0; j < fromData.Height; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = 0; i < fromData.Width; i++)
            {
                int ii = bi + i * 3;
                if (edge[i, j] == 2)
                {
                    values[ii] = values[ii + 1] = values[ii + 2] = 255;
                }
            }
        }
        to = new Bitmap(from.Width, from.Height, from.PixelFormat);
        BitmapData toData = to.LockBits(new Rectangle(0, 0, to.Width, to.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        IntPtr toPtr = toData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(values, 0, toPtr, values.Length);
        from.UnlockBits(fromData);
        to.UnlockBits(toData);
        return true;
    }

    private void Hysteresis(byte[,] edge, bool[,] visited, int i, int j)
    {
        if (visited[i, j]) return;
        visited[i, j] = true;
        int kernel = Gx.GetLength(0);
        int kernel2 = kernel / 2;
        int gimin = i - kernel2;
        int gimax = i + kernel2;
        int gjmin = j - kernel2;
        int gjmax = j + kernel2;
        for (int gj = gjmin; gj <= gjmax; gj++)
        {
            for (int gi = gimin; gi <= gimax; gi++)
            {
                if (edge[gi, gj] == 1)
                {
                    edge[gi, gj] = 2;
                    Hysteresis(edge, visited, gi, gj);
                }
            }
        }
    }
}