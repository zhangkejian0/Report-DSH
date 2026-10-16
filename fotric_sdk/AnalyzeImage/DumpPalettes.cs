using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 导出官方调色板与渲染基准：
///  1) GetPresetPalettes 全部预设（编号+名称）
///  2) 每个预设的 Colors 表（GetPaletteImage 像素读取）或文件内 Palette.Colors
///  3) GetThermalImage 渲染基准 PNG
/// 用法: DumpPalettes.exe 图片路径 输出目录
/// </summary>
static class DumpPalettes
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string file = args[0], outDir = args[1];
        var validator = IRtekFileSDKValidator.Instance;
        var f = validator.Create();
        if (!f.Open(file)) { Console.WriteLine("open failed"); Environment.Exit(3); }

        // 1) 文件内调色板
        var p = f.GetPalette();
        Console.WriteLine("file palette: refNo=0x" + p.RefNo.ToString("X2") + " inverted=" + p.Inverted + " colors=" + (p.Colors == null ? "null" : p.Colors.Length + " 项"));
        if (p.Colors != null && p.Colors.Length > 0)
            Console.WriteLine("fileColors: " + string.Join(",", p.Colors.Take(8).Select(c => c.ToString("X8"))) + (p.Colors.Length > 8 ? ",..." : ""));

        var v2 = f.GetPaletteV2();
        Console.WriteLine("paletteV2: refNo=0x" + v2.RefNo.ToString("X2") + " inverted=" + v2.Inverted
            + " src=" + (v2.SrcColors == null ? "null" : v2.SrcColors.Length.ToString())
            + " dest=" + (v2.DestColors == null ? "null" : v2.DestColors.Length.ToString()));

        // 2) 预设列表
        var presets = f.GetPresetPalettes();
        if (presets != null)
        {
            Console.WriteLine("presets: " + presets.Count);
            foreach (var pp in presets) Console.WriteLine("  0x" + pp.RefNo.ToString("X2") + " " + pp.Name);
        }

        // 3) 每个预设调色板条带像素 → JSON 行
        if (presets != null)
        {
            foreach (var pp in presets)
            {
                try
                {
                    using (var img = f.GetPresetPaletteImage(pp.RefNo))
                    {
                        if (img == null) { Console.WriteLine("strip 0x" + pp.RefNo.ToString("X2") + " null"); continue; }
                        int w = img.Width, h = img.Height;
                        var bmp = new Bitmap(img);
                        // 取中间行，全程采样
                        var sb = new StringBuilder();
                        for (int x = 0; x < w; x++)
                        {
                            var c = bmp.GetPixel(x, h / 2);
                            if (x > 0) sb.Append(',');
                            sb.Append(c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"));
                        }
                        Console.WriteLine("strip 0x" + pp.RefNo.ToString("X2") + " " + pp.Name + " " + w + "x" + h + " " + sb);
                        bmp.Save(System.IO.Path.Combine(outDir, "pal_" + pp.RefNo.ToString("X2") + ".png"), ImageFormat.Png);
                    }
                }
                catch (Exception ex) { Console.WriteLine("strip 0x" + pp.RefNo.ToString("X2") + " err " + ex.Message); }
            }
        }

        // 4) 渲染基准
        using (var t = f.GetThermalImage())
        {
            if (t != null) { t.Save(System.IO.Path.Combine(outDir, "sdk_thermal.png"), ImageFormat.Png); Console.WriteLine("sdk_thermal.png " + t.Width + "x" + t.Height); }
        }
        using (var pv = f.GetPaletteImage())
        {
            if (pv != null) { pv.Save(System.IO.Path.Combine(outDir, "sdk_palette.png"), ImageFormat.Png); Console.WriteLine("sdk_palette.png " + pv.Width + "x" + pv.Height); }
        }
    }
}
