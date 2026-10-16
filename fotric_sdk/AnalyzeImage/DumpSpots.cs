using System;
using System.Drawing;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 按像素坐标批量读单点温度（K→℃）：用法 DumpSpots.exe 图片路径 x1,y1 x2,y2 ...
/// </summary>
static class DumpSpots
{
    static void Main(string[] args)
    {
        var validator = IRtekFileSDKValidator.Instance;
        if (validator.IsValidFile(args[0]) == FileType.Other) { Console.WriteLine("not thermal"); Environment.Exit(2); }
        var file = validator.Create();
        if (!file.Open(args[0])) { Console.WriteLine("open failed"); Environment.Exit(3); }
        for (int i = 1; i < args.Length; i++)
        {
            var xy = args[i].Split(',');
            var p = new Point(int.Parse(xy[0]), int.Parse(xy[1]));
            Console.WriteLine(string.Format("{0},{1} {2:F3}", p.X, p.Y, file.GetSpotValue(p) - 273.15f));
        }
    }
}
