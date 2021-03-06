﻿using System;
using System.Drawing;
using System.Drawing.Imaging;

public class WhiteNoise
{
    public WhiteNoise()
    {

    }

    /// <summary>
    /// 生成随机白噪声图片
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool Process(int width, int height, out Bitmap to)
    {
        Random rnd = new Random();
        int bbp = 4;
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
                values[ii + 3] = (byte)(rnd.Next() * 255);
                values[ii + 2] = (byte)(rnd.Next() * 255);
                values[ii + 1] = (byte)(rnd.Next() * 255);
                values[ii] = (byte)(rnd.Next() * 255);
            }
        }
        to = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData toData = to.LockBits(new Rectangle(0, 0, to.Width, to.Height), ImageLockMode.WriteOnly, to.PixelFormat);
        IntPtr toPtr = toData.Scan0;
        System.Runtime.InteropServices.Marshal.Copy(values, 0, toPtr, values.Length);
        to.UnlockBits(toData);
        return true;
    }
}