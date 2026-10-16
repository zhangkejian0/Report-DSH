using System;
using System.Drawing;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// Dump 内置测量标记的几何信息：用法 DumpMarkers.exe 图片路径
/// </summary>
static class DumpMarkers
{
    static void Main(string[] args)
    {
        string filePath = args[0];
        var validator = IRtekFileSDKValidator.Instance;
        if (validator.IsValidFile(filePath) == FileType.Other) { Console.WriteLine("not thermal"); Environment.Exit(2); }
        var file = validator.Create();
        if (!file.Open(filePath)) { Console.WriteLine("open failed"); Environment.Exit(3); }

        Console.WriteLine("Markers: " + file.Markers.Count);
        for (int i = 0; i < file.Markers.Count; i++)
        {
            var m = file.Markers[i];
            Console.WriteLine(string.Format("[{0}] name=\"{1}\" type={2}", i, m.Name, m.Type));
            var rect = m as IMarkerRectangle;
            if (rect != null)
            {
                Rectangle r = rect.Rectangle;
                Console.WriteLine(string.Format("    rect=({0},{1})-({2},{3})  size={4}x{5}", r.X, r.Y, r.Right, r.Bottom, r.Width, r.Height));
                Console.WriteLine(string.Format("    max={0:F2}C min={1:F2}C avg={2:F2}C area={3:F4}", rect.Max - 273.15f, rect.Min - 273.15f, rect.Avg - 273.15f, rect.Area));
                Console.WriteLine(string.Format("    hot=({0},{1}) cold=({2},{3})", rect.HotSpot.X, rect.HotSpot.Y, rect.ColdSpot.X, rect.ColdSpot.Y));
            }
            var spot = m as IMarkerSpot;
            if (spot != null)
                Console.WriteLine(string.Format("    point=({0},{1}) value={2:F2}C", spot.Point.X, spot.Point.Y, spot.Value - 273.15f));
            var poly = m as IMarkerPolygon;
            if (poly != null)
                Console.WriteLine("    polygon pts=" + string.Join(",", Array.ConvertAll(poly.Points, p => "(" + p.X + "," + p.Y + ")")));
            var line = m as IMarkerPolyline;
            if (line != null)
                Console.WriteLine("    polyline pts=" + string.Join(",", Array.ConvertAll(line.Points, p => "(" + p.X + "," + p.Y + ")")));
            var ell = m as IMarkerEllipse;
            if (ell != null)
            {
                Rectangle r = ell.Rectangle;
                Console.WriteLine(string.Format("    ellipse=({0},{1})-({2},{3})", r.X, r.Y, r.Right, r.Bottom));
            }
        }
    }
}
