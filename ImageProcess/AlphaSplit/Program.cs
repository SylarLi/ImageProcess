using System;
using System.Drawing;

class Program
{
    static void Main(string[] args)
    {
        Split(args[0], args[1]);
    }

    static void Split(string sourcePath, string alphaPath)
    {
        Bitmap source = new Bitmap(sourcePath);
        Bitmap alpha = null;
        DateTime t1 = DateTime.Now;
        if (new AlphaSplit().ProcessMemory(source, out alpha))
        {
            alpha.Save(alphaPath);
            alpha.Dispose();
        }
        DateTime t2 = DateTime.Now;
        TimeSpan d = t2 - t1;
        Console.WriteLine(string.Format("image size: {0} X {1}, process time: {2}ms", source.Width, source.Height, d.TotalMilliseconds));
        source.Dispose();
    }
}

