using System;
using System.Drawing;
using System.Drawing.Imaging;

public class AlphaSplit
{
    public AlphaSplit()
    {
        
    }

    /// <summary>
    /// alpha通道分离
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool ProcessMemory(Bitmap from, out Bitmap alpha)
    {
        BitmapData fromData = from.LockBits(new Rectangle(0, 0, from.Width, from.Height), ImageLockMode.ReadOnly, from.PixelFormat);
        IntPtr fromPtr = fromData.Scan0;
        int byteSize = Math.Abs(fromData.Stride) * fromData.Height;
        int bbp = fromData.Stride / from.Width;
        if (bbp < 4) throw new NotSupportedException();
        byte[] values = new byte[byteSize];
        System.Runtime.InteropServices.Marshal.Copy(fromPtr, values, 0, values.Length);
        
        for (int j = 0; j < fromData.Height; j++)
        {
            int bi = j * fromData.Stride;
            for (int i = 0; i < fromData.Width; i++)
            {
                int ii = bi + i * 4;
                values[ii] = values[ii + 3];
                values[ii + 1] = values[ii + 3];
                values[ii + 2] = values[ii + 3];
                values[ii + 3] = 255;
            }
        }
        alpha = new Bitmap(from.Width, from.Height, from.PixelFormat);
        BitmapData alphaData = alpha.LockBits(new Rectangle(0, 0, alpha.Width, alpha.Height), ImageLockMode.WriteOnly, alpha.PixelFormat);
        IntPtr alphaPtr = alphaData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(values, 0, alphaPtr, values.Length);

        from.UnlockBits(fromData);
        alpha.UnlockBits(alphaData);
        return true;
    }
}