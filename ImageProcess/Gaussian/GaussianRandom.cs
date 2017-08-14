using System;

/// <summary>
/// 高斯随机数
/// </summary>
public class GaussianRandom
{
    private static Random RND = new Random(DateTime.Now.Millisecond);

    public static float Random()
    {
        double v1, v2, s;
        do
        {
            v1 = RND.NextDouble() * 2 - 1;
            v2 = RND.NextDouble() * 2 - 1;
            s = v1 * v1 + v2 * v2;
        }
        while (s >= 1 || s == 0);
        s = Math.Sqrt((-2 * Math.Log(s)) / s);
        return (float)(v1 * s);
    }

    /// <summary>
    /// 生成的值有%95的几率分布在[mean - 2 * sd, mean + 2 * sd]之间
    /// </summary>
    /// <param name="mean">平均值</param>
    /// <param name="sd">标准偏差</param>
    /// <returns></returns>
    public static float Random(float mean, float sd)
    {
        return mean + Random() * sd;
    }

    public static float Random(float mean, float sd, float min, float max)
    {
        double s;
        do
        {
            s = Random(mean, sd);
        }
        while (s < min || s > max);
        return (float)s;
    }

    /// <summary>
    /// 标准正交高斯分布随机数生成
    /// 分布范围固定取[-radius, radius]
    /// 生成数值区间为[-1, 1]
    /// </summary>
    /// <returns></returns>
    public static float Random1()
    {
        float mean = 0;
        float sd = 1;
        float radius = 4;
        return Random(mean, sd, -radius, radius) / radius;
    }
}