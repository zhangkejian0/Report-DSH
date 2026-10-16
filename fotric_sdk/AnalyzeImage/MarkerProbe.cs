using System;
using System.Drawing;
using System.IO;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 每种 Marker 类型各加一个后另存，用于逆向各类型记录格式
/// 用法: MarkerProbe.exe 原文件 输出文件
/// </summary>
static class MarkerProbe
{
    static int Main(string[] args)
    {
        var validator = IRtekFileSDKValidator.Instance;
        if (validator.IsValidFile(args[0]) == FileType.Other) { Console.WriteLine("not thermal"); return 2; }
        var file = validator.Create();
        if (!file.Open(args[0])) { Console.WriteLine("open failed"); return 3; }
        file.Markers.Add(file.CreateMarkerSpot("Msp", new LocalThermalParams(), new Point(111, 222)));
        file.Markers.Add(file.CreateMarkerRectangle("Mbx", new LocalThermalParams(), new Rectangle(10, 20, 30, 40)));
        file.Markers.Add(file.CreateMarkerEllipse("Mel", new LocalThermalParams(), new Rectangle(50, 60, 70, 80)));
        file.Markers.Add(file.CreateMarkerPolygon("Mpg", new LocalThermalParams(), new Point[] { new Point(1,2), new Point(300,4), new Point(500,400) }));
        file.Markers.Add(file.CreateMarkerPolyline("Mpl", new LocalThermalParams(), new Point[] { new Point(9,8), new Point(600,450) }));
        Console.WriteLine("markers=" + file.Markers.Count);
        bool ok = file.SaveAs(args[1]);
        Console.WriteLine("SaveAs: " + ok + " size=" + (ok ? new FileInfo(args[1]).Length : -1));
        file.Close();
        return ok ? 0 : 9;
    }
}
