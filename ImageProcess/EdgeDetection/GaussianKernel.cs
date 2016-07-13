using System;

public sealed class GaussianKernel
{
    public static float[,] Calculate(int kernel, float weight)
    {
        if (kernel % 2 == 0 || weight <= 0) throw new NotSupportedException();
        float[,] values = new float[kernel, kernel];
        int kindex = kernel / 2;
        double left = (float)(1 / (2 * Math.PI * weight * weight));
        double down = -1 / (2 * weight * weight);
        float sum = 0;
        for (int i = 0; i < kernel; i++)
        {
            for (int j = 0; j < kernel; j++)
            {
                int x = i - kindex;
                int y = j - kindex;
                values[i, j] = (float)(left * Math.Exp(down * (x * x + y * y)));
                sum += values[i, j];
            }
        }
        sum = 1 / sum;
        for (int i = 0; i < kernel; i++)
        {
            for (int j = 0; j < kernel; j++)
            {
                values[i, j] *= sum;
            }
        }
        return values;
    }
}