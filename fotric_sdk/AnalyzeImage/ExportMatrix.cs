using System;
using System.Drawing;
using System.IO;
using System.Text;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 导出热像文件的完整温度矩阵与 AD 矩阵，并打印内置测温标记的几何与统计值。
/// 用法: ExportMatrix.exe 图片路径 输出目录
/// 输出: <名称>.temp.csv（℃, 480 行 x 640 列）、<名称>.ad.csv（ushort AD 值）
/// </summary>
static class ExportMatrix
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string filePath = args[0];
        string outDir = args.Length > 1 ? args[1] : Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(outDir);
        string name = Path.GetFileNameWithoutExtension(filePath);

        var validator = IRtekFileSDKValidator.Instance;
        if (validator.IsValidFile(filePath) == FileType.Other) { Console.WriteLine("not thermal"); Environment.Exit(2); }
        var file = validator.Create();
        if (!file.Open(filePath)) { Console.WriteLine("open failed"); Environment.Exit(3); }

        try
        {
            Size size = file.GetThermalSize();
            Console.WriteLine(string.Format("size {0}x{1}", size.Width, size.Height));

            // 内置标记几何与统计
            for (int i = 0; i < file.Markers.Count; i++)
            {
                IMarker m = file.Markers[i];
                if (m is IMarkerRectangle)
                {
                    IMarkerRectangle r = (IMarkerRectangle)m;
                    Console.WriteLine(string.Format(
                        "MARKER {0} rect=({1},{2},{3},{4}) max={5:F2}C@({6},{7}) min={8:F2}C@({9},{10}) avg={11:F2}C",
                        m.Name, r.Rectangle.X, r.Rectangle.Y, r.Rectangle.Width, r.Rectangle.Height,
                        r.Max - 273.15f, r.HotSpot.X, r.HotSpot.Y,
                        r.Min - 273.15f, r.ColdSpot.X, r.ColdSpot.Y, r.Avg - 273.15f));
                }
                else if (m is IMarkerSpot)
                {
                    IMarkerSpot s = (IMarkerSpot)m;
                    Console.WriteLine(string.Format("MARKER {0} point=({1},{2}) value={3:F2}C",
                        m.Name, s.Point.X, s.Point.Y, s.Value - 273.15f));
                }
                else
                {
                    Console.WriteLine("MARKER " + m.Name + " type=" + m.Type);
                }
            }

            // 温度矩阵（K→℃）
            TemperaturePoint[] pts = file.GetRectangleValues(new Rectangle(0, 0, size.Width, size.Height));
            float[,] temp = new float[size.Height, size.Width];
            foreach (TemperaturePoint tp in pts) temp[(int)tp.Y, (int)tp.X] = tp.Value - 273.15f;
            WriteCsv(Path.Combine(outDir, name + ".temp.csv"), temp, size, v => v.ToString("F3"));

            // AD 原始矩阵
            Frame f = file.GetFrame();
            if (f.Values != null)
            {
                int h = f.Values.GetLength(0), w = f.Values.GetLength(1);
                ushort[,] ad = new ushort[h, w];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) ad[y, x] = f.Values[y, x];
                WriteCsv(Path.Combine(outDir, name + ".ad.csv"), ad, new Size(w, h), v => v.ToString());
            }

            Console.WriteLine("DONE");
        }
        finally
        {
            file.Close();
        }
    }

    static void WriteCsv<T>(string path, T[,] m, Size size, Func<T, string> fmt)
    {
        using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
        {
            for (int y = 0; y < size.Height; y++)
            {
                var row = new string[size.Width];
                for (int x = 0; x < size.Width; x++) row[x] = fmt(m[y, x]);
                writer.WriteLine(string.Join(",", row));
            }
        }
        Console.WriteLine("wrote " + Path.GetFileName(path));
    }
}
