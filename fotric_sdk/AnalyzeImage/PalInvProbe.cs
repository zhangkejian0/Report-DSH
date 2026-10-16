using System;
using System.IO;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>SetPalette(RefNo=4, Inverted=true) 另存，找反转标志字节</summary>
static class PalInvProbe
{
    static int Main(string[] args)
    {
        var validator = IRtekFileSDKValidator.Instance;
        var file = validator.Create();
        if (!file.Open(args[0])) { Console.WriteLine("open failed"); return 3; }
        var pal = file.GetPalette();
        pal.RefNo = 4; pal.Inverted = true;
        Console.WriteLine("SetPalette(inv): " + file.SetPalette(pal));
        bool ok = file.SaveAs(args[1]);
        Console.WriteLine("SaveAs: " + ok);
        file.Close();
        return 0;
    }
}
