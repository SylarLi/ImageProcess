using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace Clutters
{
    class Program
    {
        static void Main(string[] args)
        {
            TestWhiteNoise();
            //TestVQ();
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

        private static void TestVQ()
        {
            List<Vector<float>> samples = new List<Vector<float>>(10000);
            Random r = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 10000; i++)
            {
                samples.Add(new Vector<float>(new float[] 
                {
                    GaussianRandom.Random1()
                }));
            }
            DateTime t = DateTime.Now;
            VQ vq = new VQ(samples, 2, 0.001f);
            List<Vector<float>> c = vq.Process();
            Dictionary<Vector<float>, int> counter = new Dictionary<Vector<float>, int>();
            foreach (var ci in c)
            {
                counter.Add(ci, 0);
            }
            foreach (var pair in vq.codeBook)
            {
                counter[pair.Value] += 1;
            }
            foreach (var pair in counter)
            {
                Console.WriteLine(pair.Key[0] + " : " + pair.Value);
            }
            Console.WriteLine("time cost: " + (DateTime.Now - t).TotalSeconds);
            Console.ReadLine();
        }
    }
}
