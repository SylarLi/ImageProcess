using System;
using System.Drawing;
using System.Drawing.Imaging;

public class GaussianBlur
{
    private int kernel;
    private float weight;
    private float[,] G;

    public GaussianBlur(int kernel, float weight)
    {
        this.kernel = kernel;
        this.weight = weight;
        G = GaussianKernel.Calculate(kernel, weight);
    }

    /// <summary>
    /// 高斯滤波
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
        byte[] values = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(fromPtr, source, 0, source.Length);
        int kernel2 = kernel / 2;
        for (int j = 0; j < fromData.Height; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = 0; i < fromData.Width; i++)
            {
                float sr = 0;
                float sg = 0;
                float sb = 0;
                int gimin = Math.Max(0, i - kernel2);
                int gimax = Math.Min(fromData.Width, i + kernel2);
                int gjmin = Math.Max(0, j - kernel2);
                int gjmax = Math.Min(fromData.Height, j + kernel2);
                for (int gj = gjmin; gj < gjmax; gj++)
                {
                    int gbi = gj * fromData.Stride;
                    for (int gi = gimin; gi < gimax; gi++)
                    {
                        int gii = gbi + gi * 3;
                        float k = G[gi + kernel2 - i, gj + kernel2 - j];
                        sr += source[gii + 2] * k;
                        sg += source[gii + 1] * k;
                        sb += source[gii] * k;
                    }
                }
                int ii = bi + i * 3;
                values[ii + 2] = (byte)sr;
                values[ii + 1] = (byte)sg;
                values[ii] = (byte)sb;
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
}