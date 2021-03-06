﻿using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        TestGrey();
    }

    static void TestGrey()
    {
        GreyScale grey = new GreyScale();
        string sourcePath = "../../../Resources/source.png";
        string outputPath = "../../../Resources/grey.png";
        Bitmap source = new Bitmap(sourcePath);
        Bitmap output = null;
        DateTime t1 = DateTime.Now;
        if (grey.ProcessMemory(source, out output))
        {
            output.Save(outputPath);
            Process process = Process.Start("explorer.exe", Path.GetFullPath(outputPath));
            process.Exited += (object sender, EventArgs e) => output.Dispose();
        }
        DateTime t2 = DateTime.Now;
        TimeSpan d = t2 - t1;
        Console.WriteLine(string.Format("image size: {0} X {1}, process time: {2}ms", source.Width, source.Height, d.TotalMilliseconds));
        source.Dispose();
    }
}

