using System;
using System.Drawing;
using System.Drawing.Imaging;

public class GreyScale
{
    private float featureR = 0.299f;
    private float featureG = 0.587f;
    private float featureB = 0.114f;

    public GreyScale()
    {
        
    }

    /// <summary>
    /// 灰化
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool ProcessMemory(Bitmap from, out Bitmap to)
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
                int ii = bi + i * 3;
                byte grey = (byte)(values[ii + 2] * featureR + values[ii + 1] * featureG + values[ii] * featureB);
                values[ii + 2] = grey;
                values[ii + 1] = grey;
                values[ii] = grey;
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

    public bool ProcessSimple(Bitmap from, out Bitmap to)
    {
        to = new Bitmap(from.Width, from.Height, from.PixelFormat);
        for (int i = 0; i < from.Width; i++)
        {
            for (int j = 0; j < from.Height; j++)
            {
                Color fc = from.GetPixel(i, j);
                byte grey = (byte)(fc.R * featureR + fc.G * featureG + fc.B * featureB);
                Color tc = Color.FromArgb(grey, grey, grey);
                to.SetPixel(i, j, tc);
            }
        }
        return true;
    }
}