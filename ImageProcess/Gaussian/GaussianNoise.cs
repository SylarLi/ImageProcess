using System;
using System.Drawing;
using System.Drawing.Imaging;

public class GaussianNoise
{
    private static byte GreyLevel = 128;

    public GaussianNoise()
    {

    }

    /// <summary>
    /// 生成高斯白噪声图片
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool Process(int width, int height, out Bitmap to)
    {
        int bbp = 3;
        int byteSize = width * height * bbp;
        int stride = width * bbp;
        byte[] values = new byte[byteSize];
        for (int j = 0; j < height; j++)
        {
            int bi = j * stride;
            for (int i = 0; i < width; i++)
            {
                // BGR
                int ii = bi + i * bbp;
                byte number = (byte)(Math.Abs(GaussianRandom.Random1()) * 255);
                values[ii + 2] = number;
                values[ii + 1] = number;
                values[ii] = number;
            }
        }
        to = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        BitmapData toData = to.LockBits(new Rectangle(0, 0, to.Width, to.Height), ImageLockMode.WriteOnly, to.PixelFormat);
        IntPtr toPtr = toData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(values, 0, toPtr, values.Length);
        to.UnlockBits(toData);
        return true;
    }

    /// <summary>
    /// 为图片增加高斯噪声
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool Process(Bitmap from, out Bitmap to)
    {
        BitmapData fromData = from.LockBits(new Rectangle(0, 0, from.Width, from.Height), ImageLockMode.ReadOnly, from.PixelFormat);
        IntPtr fromPtr = fromData.Scan0;
        int byteSize = Math.Abs(fromData.Stride) * fromData.Height;
        int bbp = fromData.Stride / from.Width;
        if (bbp < 3) throw new NotSupportedException();
        byte[] values = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(fromPtr, values, 0, values.Length);
        for (int j = 0; j < fromData.Height; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = 0; i < fromData.Width; i++)
            {
                // BGRA
                int ii = bi + i * bbp;
                float level = Math.Abs(GaussianRandom.Random1());
                values[ii + 2] = Lerp(values[ii + 2], GreyLevel, level);
                values[ii + 1] = Lerp(values[ii + 1], GreyLevel, level);
                values[ii] = Lerp(values[ii], GreyLevel, level);
            }
        }
        to = new Bitmap(from.Width, from.Height, from.PixelFormat);
        BitmapData toData = to.LockBits(new Rectangle(0, 0, to.Width, to.Height), ImageLockMode.WriteOnly, to.PixelFormat);
        IntPtr toPtr = toData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(values, 0, toPtr, values.Length);
        from.UnlockBits(fromData);
        to.UnlockBits(toData);
        return true;
    }

    private byte Lerp(byte value1, byte value2, float t)
    {
        if (t < 0 || t > 1) throw new NotSupportedException();
        return (byte)((1 - t) * value1 + t * value2);
    }
}