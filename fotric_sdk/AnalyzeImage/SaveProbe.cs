using System;
using System.Drawing;
using System.IO;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// SDK 保存行为探针：
/// 用法: SaveProbe.exe 原文件 另存文件 [--params|--marker|--palette N|--gain]
/// </summary>
static class SaveProbe
{
    static int Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("usage: SaveProbe.exe src dst [--params|--marker|--palette N]"); return 1; }
        var validator = IRtekFileSDKValidator.Instance;
        if (validator.IsValidFile(args[0]) == FileType.Other) { Console.WriteLine("not thermal"); return 2; }
        var file = validator.Create();
        if (!file.Open(args[0])) { Console.WriteLine("open failed"); return 3; }

        string mode = args.Length > 2 ? args[2] : null;
        if (mode == "--params")
        {
            var p = file.GetThermalParams();
            p.Emissivity = 0.90f; p.Distance = 3.5f; p.ReflTemp = 25.0f + 273.15f;
            p.AtmTemp = 22.0f + 273.15f; p.RelHumidity = 35.0f; p.ExtOptTemp = 21.0f + 273.15f; p.ExtOptTrans = 0.98f;
            Console.WriteLine("SetThermalParams: " + file.SetThermalParams(p));
        }
        else if (mode == "--marker")
        {
            var mk = file.CreateMarkerRectangle("BoxT", new LocalThermalParams(), new Rectangle(40, 30, 200, 150));
            file.Markers.Add(mk);
            Console.WriteLine("marker added, count=" + file.Markers.Count);
        }
        else if (mode == "--palette")
        {
            byte n = byte.Parse(args[3]);
            var pal = file.GetPalette();
            pal.RefNo = n;
            Console.WriteLine("SetPalette: " + file.SetPalette(pal));
        }

        bool ok = file.SaveAs(args[1]);
        Console.WriteLine("SaveAs: " + ok + "  size=" + (ok ? new FileInfo(args[1]).Length : -1));
        file.Close();
        return ok ? 0 : 9;
    }
}
