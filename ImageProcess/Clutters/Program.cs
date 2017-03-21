using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;

namespace Clutters
{
    class Program
    {
        static void Main(string[] args)
        {
            TestWhiteNoise();
        }

        private static void TestWhiteNoise()
        {
            WhiteNoise noise = new WhiteNoise();
            string outputPath = "../../../Resources/white-noise-map.png";
            Bitmap output = null;
            DateTime t1 = DateTime.Now;
            if (noise.Process(512, 512, out output))
            {
                output.Save(outputPath);
                Process process = Process.Start("explorer.exe", Path.GetFullPath(outputPath));
                process.Exited += (object sender, EventArgs e) => output.Dispose();
            }
            DateTime t2 = DateTime.Now;
            TimeSpan d = t2 - t1;
            Console.WriteLine(string.Format("image size: {0} X {1}, process time: {2}ms", output.Width, output.Height, d.TotalMilliseconds));
        }
    }
}
