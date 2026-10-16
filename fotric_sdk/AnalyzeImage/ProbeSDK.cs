using System;
using System.Drawing;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// SDK 温度探针：可改参数后按点读温。
/// 用法: ProbeSDK.exe 图片路径 emis dist reflC atmC rh extTC extTr x1,y1 x2,y2 ...
/// </summary>
static class ProbeSDK
{
    static void Main(string[] args)
    {
        var validator = IRtekFileSDKValidator.Instance;
        if (validator.IsValidFile(args[0]) == FileType.Other) { Console.WriteLine("not thermal"); Environment.Exit(2); }
        var file = validator.Create();
        if (!file.Open(args[0])) { Console.WriteLine("open failed"); Environment.Exit(3); }

        var p = file.GetThermalParams();
        p.Emissivity = float.Parse(args[1]);
        p.Distance = float.Parse(args[2]);
        p.ReflTemp = float.Parse(args[3]) + 273.15f;
        p.AtmTemp = float.Parse(args[4]) + 273.15f;
        p.RelHumidity = float.Parse(args[5]);
        p.ExtOptTemp = float.Parse(args[6]) + 273.15f;
        p.ExtOptTrans = float.Parse(args[7]);
        if (!file.SetThermalParams(p)) Console.WriteLine("[warn] SetThermalParams false");

        for (int i = 8; i < args.Length; i++)
        {
            var xy = args[i].Split(',');
            var pt = new Point(int.Parse(xy[0]), int.Parse(xy[1]));
            Console.WriteLine(string.Format("{0},{1} {2:F3}", pt.X, pt.Y, file.GetSpotValue(pt) - 273.15f));
        }
    }
}
