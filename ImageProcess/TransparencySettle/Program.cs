using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;

class Program
{
    static void Main(string[] args)
    {
        Make(args[0]);
        //Make(@"E:\M02\client\project\editor\Assets\ArtResource\Atlas\Paintings\Paintings\afuleer.png");
        //Test();
    }

    /// <summary>
    /// 如果转换失败，一般可能存在两种原因：
    /// 1：转换后的贴图宽高大于2048
    /// 2：转换后的贴图内存占用比原来还大
    /// </summary>
    /// <param name="sourcePath"></param>
    static void Make(string sourcePath)
    {
        var mdFile = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + ".md");
        if (File.Exists(mdFile)) File.Delete(mdFile);
        if (File.Exists(mdFile + ".meta")) File.Delete(mdFile + ".meta");
        Bitmap source = new Bitmap(sourcePath);
        Settle settle = new Settle(source, 32, 32);
        Bitmap packed;
        Settle.Mesh mesh;
        if (settle.Parse(out packed, out mesh))
        {
            float profit = (source.Width * source.Height - packed.Width * packed.Height) * 4f / 1024;
            settle.Dispose();
            source.Dispose();
            if (profit > 0)
            {
                packed.Save(sourcePath, ImageFormat.Png);
                using (var stream = File.Create(mdFile))
                {
                    stream.Seek(0, SeekOrigin.End);
                    mesh.Serialize(stream);
                    stream.Flush();
                }
            }
            packed.Dispose();
        }
        else
        {
            settle.Dispose();
            source.Dispose();
        }
    }

    static void Test()
    {
        var pngFiles = Directory.GetFiles(@"E:\M02_new\client\project\editor\Assets\ArtResource\Atlas\Paintings\Paintings", "*.png");
        var targetPath = "../../../Resources/packed/";
        int packedNums = 0;
        float totalProfit = 0, minProfit = float.MaxValue, maxProfit = 0;
        int totalTriangles = 0, minTraingles = int.MaxValue, maxTriangles = 0;
        string minName_P = "", maxName_P = "", minName_T = "", maxName_T = "";
        foreach (var pngFile in pngFiles)
        {
            var pngFileName = Path.GetFileNameWithoutExtension(pngFile);
            if (!pngFileName.EndsWith("_enc") && 
                !pngFileName.EndsWith("hx") && 
                !pngFileName.Contains("shadow") &&
                !pngFileName.Contains("_bd"))
            {
                Bitmap source = new Bitmap(pngFile);
                Settle settle = new Settle(source, 32, 32);
                DateTime t1 = DateTime.Now;
                Bitmap packed;
                Settle.Mesh mesh;
                if (settle.Parse(out packed, out mesh))
                {
                    float profit = (source.Width * source.Height - packed.Width * packed.Height) * 4f / 1024;
                    settle.Dispose();
                    source.Dispose();
                    if (profit > 0)
                    {
                        packed.Save(targetPath + Path.GetFileName(pngFile), ImageFormat.Png);
                        using (var stream = new FileStream(targetPath + pngFileName + ".md", FileMode.OpenOrCreate))
                        {
                            stream.SetLength(0);
                            stream.Position = 0;
                            mesh.Serialize(stream);
                            stream.Flush();
                        }
                        DateTime t2 = DateTime.Now;
                        TimeSpan d = t2 - t1;
                        Console.WriteLine(pngFileName);
                        Console.WriteLine("triangles: " + mesh.triangles.Length / 3);
                        Console.WriteLine(string.Format("source image size: {0} X {1}", source.Width, source.Height));
                        Console.WriteLine(string.Format("packed image size: {0} X {1}", packed.Width, packed.Height));
                        Console.WriteLine(string.Format("process time: {0}ms, profit: {1}kb", d.TotalMilliseconds, profit));
                        Console.WriteLine();
                        packedNums += 1;
                        totalProfit += profit;
                        totalTriangles += mesh.triangles.Length / 3;
                        if (profit < minProfit)
                        {
                            minProfit = profit;
                            minName_P = pngFileName;
                        }
                        if (profit > maxProfit)
                        {
                            maxProfit = profit;
                            maxName_P = pngFileName;
                        }
                        if (mesh.triangles.Length / 3 < minTraingles)
                        {
                            minTraingles = mesh.triangles.Length / 3;
                            minName_T = pngFileName;
                        }
                        if (mesh.triangles.Length / 3 > maxTriangles)
                        {
                            maxTriangles = mesh.triangles.Length / 3;
                            maxName_T = pngFileName;
                        }
                    }
                    packed.Dispose();
                }
                else
                {
                    settle.Dispose();
                    source.Dispose();
                }
            }
        }
        if (packedNums > 0)
        {
            Console.WriteLine(string.Format("Total packed: {0}, Average profit: {1}kb, Average traingles: {2}", packedNums, totalProfit / packedNums, totalTriangles / packedNums));
            Console.WriteLine(string.Format("Min profit: {0}kb {1}, Max profit: {2}kb {3}", minProfit, minName_P, maxProfit, maxName_P));
            Console.WriteLine(string.Format("Min traingles: {0} {1}, Max triangles: {2} {3}", minTraingles, minName_T, maxTriangles, maxName_T));
        }
        Console.ReadLine();
    }
}

