using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

/// <summary>
/// 对比 SDK 渲染 PNG 与我们引擎渲染的 raw RGBA：
/// 用法 CompareRender.exe sdk.png ours.raw 宽 高 [输出对比图]
/// </summary>
static class CompareRender
{
    static void Main(string[] args)
    {
        var bmp = new Bitmap(args[0]);
        int w = int.Parse(args[2]), h = int.Parse(args[3]);
        byte[] raw = File.ReadAllBytes(args[1]);
        Console.WriteLine("sdk " + bmp.Width + "x" + bmp.Height + "  ours " + w + "x" + h + " raw " + raw.Length);

        double sum = 0; int maxd = 0, nDiff = 0, n = 0;
        int[,] diffMap = new int[w, h];
        for (int y = 0; y < Math.Min(h, bmp.Height); y++)
        {
            for (int x = 0; x < Math.Min(w, bmp.Width); x++)
            {
                var s = bmp.GetPixel(x, y);
                int i = (y * w + x) * 4;
                int dr = Math.Abs(s.R - raw[i]), dg = Math.Abs(s.G - raw[i + 1]), db = Math.Abs(s.B - raw[i + 2]);
                int d = Math.Max(dr, Math.Max(dg, db));
                diffMap[x, y] = d;
                sum += d; if (d > maxd) maxd = d; if (d > 8) nDiff++; n++;
            }
        }
        Console.WriteLine(string.Format("像素平均色差 {0:F2}  最大 {1}  色差>8 的像素 {2:F1}%", sum / n, maxd, nDiff * 100.0 / n));

        // 分块统计（8x6 块），找差异分布
        var sb = new StringBuilder();
        int bw = w / 16, bh = h / 12;
        for (int by = 0; by < 12; by++)
        {
            for (int bx = 0; bx < 16; bx++)
            {
                double bs = 0; int bn = 0;
                for (int y = by * bh; y < (by + 1) * bh; y++)
                    for (int x = bx * bw; x < (bx + 1) * bw; x++) { bs += diffMap[x, y]; bn++; }
                sb.Append(Math.Round(bs / bn).ToString().PadLeft(4));
            }
            sb.AppendLine();
        }
        Console.WriteLine("分块平均色差图:");
        Console.Write(sb.ToString());

        // 采样几个点的具体颜色
        for (int k = 0; k < 5; k++)
        {
            int x = 80 + k * 120, y = 240;
            var s = bmp.GetPixel(x, y);
            int i = (y * w + x) * 4;
            Console.WriteLine(string.Format("({0},{1}) sdk=({2},{3},{4}) ours=({5},{6},{7})", x, y, s.R, s.G, s.B, raw[i], raw[i + 1], raw[i + 2]));
        }
    }
}
