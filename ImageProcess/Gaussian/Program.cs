using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Gaussian
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestBlur();
            TestGaussianNoiseMap();
            //TestGaussianNoise();
            //TestRandom();
        }

        private static void TestKernel()
        {
            float[,] G = GaussianKernel.Calculate(3, 1.4f);
            float a = 0;
            for (int i = 0; i < G.GetLength(0); i++)
            {
                for (int j = 0; j < G.GetLength(1); j++)
                {
                    Console.Write(G[i, j] + ", ");
                    a += G[i, j];
                }
                Console.WriteLine();
            }
            Console.WriteLine(a);
            Console.ReadLine();
        }

        private static void TestBlur()
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

        private static void TestRandom()
        {
            int[] counts = new int[10];
            for (int i = 0; i < 10000; i++)
            {
                float f = (GaussianRandom.Random1() + 1) * 0.5f;
                int index = (int)(f * 10);
                counts[index] += 1;
            }
            for (int i = 0; i < counts.Length; i++)
            {
                Console.WriteLine(i + " : " + counts[i]);
            }
            Console.ReadLine();
        }

        private static void TestGaussianNoise()
        {
            GaussianNoise noise = new GaussianNoise();
            string sourcePath = "../../../Resources/source.png";
            string outputPath = "../../../Resources/gaussian-noise.png";
            Bitmap source = new Bitmap(sourcePath);
            Bitmap output = null;
            DateTime t1 = DateTime.Now;
            if (noise.Process(source, out output))
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

        private static void TestGaussianNoiseMap()
        {
            GaussianNoise noise = new GaussianNoise();
            string outputPath = "../../../Resources/gaussian-noise-map.png";
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
