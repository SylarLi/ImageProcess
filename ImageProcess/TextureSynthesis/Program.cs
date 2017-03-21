using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        TestTextureSynthesis();
    }

    private static void TestTextureSynthesis()
    {
        string sourcePath = "../../../Resources/source.png";
        string noisePath = "../../../Resources/noise.png";
        string outputPath = "../../../Resources/synthesis.png";
        Bitmap source = new Bitmap(sourcePath);
        Bitmap noise = new Bitmap(noisePath);
        Bitmap output = null;
        TextureSynthesis ts = new TextureSynthesis(source, noise, 5, 1);
        DateTime t1 = DateTime.Now;
        if (ts.Process(out output))
        {
            output.Save(outputPath);
            Process process = Process.Start("explorer.exe", Path.GetFullPath(outputPath));
            process.Exited += (object sender, EventArgs e) => output.Dispose();
        }
        DateTime t2 = DateTime.Now;
        TimeSpan d = t2 - t1;
        Console.WriteLine(string.Format("process time: {0}ms", d.TotalMilliseconds));
        Console.ReadLine();
        source.Dispose();
    }
}
