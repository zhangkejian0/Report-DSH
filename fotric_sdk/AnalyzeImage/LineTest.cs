using System;
using System.Drawing;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>验证线温数据可用性：取几条典型线，看采样密度与温度分辨率。</summary>
static class LineTest
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string filePath = args.Length > 0 ? args[0] : @"D:\develop\Report Studio\IRAC\FOTRIC_20200903_0007_20250909175505A001.jpg";

        IThermalImageFile file = IRtekFileSDKValidator.Instance.Create();
        try
        {
            if (!file.Open(filePath)) { Console.WriteLine("Open 失败"); return 1; }
            Size size = file.GetThermalSize();

            // 线1: 横穿全幅最高温点 (25,153) 的水平线
            Test("横线 y=153 (穿最高温点)", file,
                new Point[] { new Point(0, 153), new Point(size.Width - 1, 153) });
            // 线2: 纵向中线
            Test("纵线 x=320 (画面中线)", file,
                new Point[] { new Point(320, 0), new Point(320, size.Height - 1) });
            // 线3: 斜线（验证非水平/垂直线）
            Test("斜线 (0,0)-(639,479)", file,
                new Point[] { new Point(0, 0), new Point(size.Width - 1, size.Height - 1) });
            return 0;
        }
        finally { file.Close(); }
    }

    static void Test(string label, IThermalImageFile file, Point[] line)
    {
        try
        {
            TemperaturePoint[] pts = file.GetPolylineValues(line);
            if (pts == null || pts.Length == 0) { Console.WriteLine("  " + label + ": 不可用"); return; }
            float mx = float.MinValue, mn = float.MaxValue;
            int mxIdx = 0, mnIdx = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].Value > mx) { mx = pts[i].Value; mxIdx = i; }
                if (pts[i].Value < mn) { mn = pts[i].Value; mnIdx = i; }
            }
            // 温度分辨率：相邻采样点的最小差异
            float minStep = float.MaxValue;
            for (int i = 1; i < pts.Length; i++)
            {
                float d = Math.Abs(pts[i].Value - pts[i - 1].Value);
                if (d > 0 && d < minStep) minStep = d;
            }
            Console.WriteLine("  " + label + ": " + pts.Length + " 个采样点" +
                "  最高 " + (mx - 273.15f).ToString("F2") + "℃ @位置" + mxIdx +
                "  最低 " + (mn - 273.15f).ToString("F2") + "℃ @位置" + mnIdx +
                (minStep < float.MaxValue ? "  相邻点最小温差 " + (minStep * 1000).ToString("F1") + "mK" : ""));
            // 打印线上均匀 10 个点，看曲线形状数据
            var sb = new System.Text.StringBuilder("    沿线采样: ");
            for (int k = 0; k < 10; k++)
            {
                int i = k * (pts.Length - 1) / 9;
                sb.Append((pts[i].Value - 273.15f).ToString("F1") + "℃ ");
            }
            Console.WriteLine(sb.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine("  " + label + ": 异常 " + ex.Message);
        }
    }
}
