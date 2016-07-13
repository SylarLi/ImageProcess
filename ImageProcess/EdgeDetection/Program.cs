using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace EdgeDetection
{
    class Program
    {
        static void Main(string[] args)
        {
            TestGrey();
            TestBlur();
            TestCanny();
            Console.In.ReadLine();
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

        static void TestBlur()
        {
            GaussianBlur blur = new GaussianBlur(5, 1.4f);
            string sourcePath = "../../../Resources/grey.png";
            string outputPath = "../../../Resources/blur.png";
            Bitmap source = new Bitmap(sourcePath);
            Bitmap output = null;
            DateTime t1 = DateTime.Now;
            if (blur.Process(source, out output))
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

        static void TestCanny()
        {
            Canny canny = new Canny();
            string sourcePath = "../../../Resources/blur.png";
            string outputPath = "../../../Resources/canny.png";
            Bitmap source = new Bitmap(sourcePath);
            Bitmap output = null;
            DateTime t1 = DateTime.Now;
            if (canny.Process(source, out output))
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
}
