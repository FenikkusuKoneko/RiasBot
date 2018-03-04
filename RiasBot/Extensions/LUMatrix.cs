using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Extensions
{
    public class LUMatrix
    {
        public static double[,] l;
        public static double[,] u;
        public static void LU(double[,] a, int n)
        {
            l = new double[n, n];
            u = new double[n, n];

            int i = 0, j = 0, k = 0;
            for (i = 0; i < n; i++)
            {
                for (j = 0; j < n; j++)
                {
                    if (j < i)
                        l[j, i] = 0;
                    else
                    {
                        l[j, i] = a[j, i];
                        for (k = 0; k < i; k++)
                        {
                            l[j, i] = l[j, i] - l[j, k] * u[k, i];
                        }
                    }
                }
                for (j = 0; j < n; j++)
                {
                    if (j < i)
                        u[i, j] = 0;
                    else if (j == i)
                        u[i, j] = 1;
                    else
                    {
                        u[i, j] = a[i, j] / l[i, i];
                        for (k = 0; k < i; k++)
                        {
                            u[i, j] = u[i, j] - ((l[i, k] * u[k, j]) / l[i, i]);
                        }
                    }
                }
            }
        }
    }
}
