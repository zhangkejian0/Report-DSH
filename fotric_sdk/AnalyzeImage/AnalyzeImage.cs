using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 用 IRtek 热像 SDK 分析单张热像图的基本信息，并导出内部图像。
/// 用法: AnalyzeImage.exe [图片路径] [导出目录]
/// </summary>
static class AnalyzeImage
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string filePath = args.Length > 0 ? args[0] : @"D:\develop\Report Studio\IRAC\FOTRIC_20200903_0007_20250909175505A001.jpg";
        string outDir = args.Length > 1 ? args[1] : Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(outDir);
        string name = Path.GetFileNameWithoutExtension(filePath);

        if (!File.Exists(filePath))
        {
            Console.WriteLine("文件不存在: " + filePath);
            Environment.Exit(1);
        }

        Console.WriteLine("=== IRtek SDK 热像图分析 ===");
        Console.WriteLine("文件: " + filePath);
        Console.WriteLine(string.Format("文件大小: {0:N0} 字节", new FileInfo(filePath).Length));

        var validator = IRtekFileSDKValidator.Instance;
        FileType fileType = validator.IsValidFile(filePath);
        Console.WriteLine("识别文件类型: " + fileType);
        if (fileType == FileType.Other)
        {
            Console.WriteLine("不是有效的热像文件");
            Environment.Exit(2);
        }

        IThermalImageFile file = null;
        try
        {
            file = validator.Create();
            if (!file.Open(filePath))
            {
                Console.WriteLine("[失败] Open 失败");
                Environment.Exit(3);
            }

            Safe("文件版本", () => "v" + file.FileVersion);
            Safe("修改时间", () => file.GetModifiedTime().ToString("yyyy-MM-dd HH:mm:ss"));

            Size size = new Size(0, 0);
            Safe("热像尺寸", () =>
            {
                size = file.GetThermalSize();
                return string.Format("{0} x {1}", size.Width, size.Height);
            });
            if (size.Width == 0) { Safe("热像尺寸(重试)", () => { size = file.GetThermalSize(); return size.Width + " x " + size.Height; }); }

            Safe("视图模式", () =>
            {
                ViewInfo v = file.GetViewInfo();
                return string.Format("{0}  显示范围 [{1:F1}℃, {2:F1}℃]", v.Type, v.Min - 273.15f, v.Max - 273.15f);
            });

            Safe("相机信息", () =>
            {
                CameraInfo c = file.GetCameraInfo();
                return string.Format("型号={0}  序列号={1}  品牌={2}  厂商={3}",
                    S(c.Model), S(c.SerialNo), S(c.Brand), S(c.Company));
            });

            Safe("镜头信息", () =>
            {
                LensInfo l = file.GetLensInfo();
                return string.Format("名称={0}  焦距={1:F1}mm  水平视场角={2:F1}°  波段={3}",
                    S(l.Name), l.FocalLength, l.HFov, S(l.Band));
            });

            Safe("测温参数", () =>
            {
                ThermalParams p = file.GetThermalParams();
                return string.Format("发射率={0:F2}  距离={1:F2}m  反射温度={2:F1}℃  大气温度={3:F1}℃  湿度={4:F2}  外部光学温度={5:F1}℃  透过率={6:F2}",
                    p.Emissivity, p.Distance, p.ReflTemp - 273.15f, p.AtmTemp - 273.15f, p.RelHumidity, p.ExtOptTemp - 273.15f, p.ExtOptTrans);
            });

            Safe("温宽", () =>
            {
                Gain g = file.GetGain();
                return string.Format("模式={0}  [{1:F1}℃, {2:F1}℃]  跨度={3:F1}℃", g.Mode, g.Min - 273.15f, g.Max - 273.15f, g.Max - g.Min);
            });

            Safe("测温量程", () =>
            {
                TempRange r = file.GetTempRange();
                return string.Format("[{0:F1}℃, {1:F1}℃]", r.Min - 273.15f, r.Max - 273.15f);
            });

            Safe("调色板", () =>
            {
                Palette p = file.GetPalette();
                return string.Format("编号=0x{0:X2}  反转={1}", p.RefNo, p.Inverted);
            });

            Safe("色彩分布", () => file.GetColorDistribution().ToString());

            Safe("报警设置", () =>
            {
                Alarm a = file.GetAlarm();
                return string.Format("类型={0}  阈值[{1:F1}℃, {2:F1}℃]", a.Type, a.Min - 273.15f, a.Max - 273.15f);
            });

            Safe("GPS 定位", () =>
            {
                GeoLocation g = file.GetGeoLocation();
                if (!g.IsGPSValid) return "无有效 GPS 数据";
                return string.Format("经度={0:F6}  纬度={1:F6}  海拔={2:F1}m  方向={3}°", g.Longitude, g.Latitude, g.Altitude, g.Direction);
            });

            Safe("注释/标签", () => string.Format("备注=\"{0}\"  标签=\"{1}\"  收藏={2}", S(file.GetNoteAnnotation()), S(file.GetTagAnnotation()), file.GetFavorite()));

            Safe("用户自定义数据", () =>
            {
                string[] keys = file.GetUserDataKeys();
                if (keys == null || keys.Length == 0) return "无";
                var parts = Array.ConvertAll(keys, k => k + "=\"" + file.GetUserData(k) + "\"");
                return string.Join("; ", parts);
            });

            Safe("内置测量标记", () =>
            {
                int n = file.Markers.Count;
                if (n == 0) return "0 个";
                var list = new string[n];
                for (int i = 0; i < n; i++) list[i] = string.Format("{0}({1})", file.Markers[i].Name, file.Markers[i].Type);
                return n + " 个: " + string.Join(", ", list);
            });

            Safe("帧信息", () =>
            {
                int frames = file.GetFrameCount();
                float fps = file.GetFrameRate();
                return string.Format("帧数={0}  帧率={1:F1}fps  当前帧={2}", frames, fps, file.GetFrameIndex());
            });

            Safe("AD 原始矩阵", () =>
            {
                Frame f = file.GetFrame();
                if (f.Values == null) return "无帧数据";
                int h = f.Values.GetLength(0), w = f.Values.GetLength(1);
                ushort mn = ushort.MaxValue, mx = 0; double sum = 0; long cnt = 0;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        ushort v = f.Values[y, x];
                        if (v < mn) mn = v;
                        if (v > mx) mx = v;
                        sum += v; cnt++;
                    }
                return string.Format("{0} x {1}  时间戳={2}  AD范围=[{3}, {4}]  AD均值={5:F1}", w, h, f.TimeStamp, mn, mx, sum / cnt);
            });

            if (size.Width > 0)
            {
                Safe("全幅温度场", () =>
                {
                    TemperaturePoint[] pts = file.GetRectangleValues(new Rectangle(0, 0, size.Width, size.Height));
                    if (pts == null || pts.Length == 0) return "不可用";
                    float mx = float.MinValue, mn = float.MaxValue, sum = 0;
                    Point hot = new Point(-1, -1), cold = new Point(-1, -1);
                    foreach (TemperaturePoint tp in pts)
                    {
                        if (tp.Value > mx) { mx = tp.Value; hot = new Point((int)tp.X, (int)tp.Y); }
                        if (tp.Value < mn) { mn = tp.Value; cold = new Point((int)tp.X, (int)tp.Y); }
                        sum += tp.Value;
                    }
                    return string.Format("最高 {0:F1}℃ @({1},{2})  最低 {3:F1}℃ @({4},{5})  平均 {6:F1}℃  采样 {7} 点",
                        mx - 273.15f, hot.X, hot.Y, mn - 273.15f, cold.X, cold.Y, sum / pts.Length - 273.15f, pts.Length);
                });

                Safe("中心点温度", () =>
                {
                    float t = file.GetSpotValue(new Point(size.Width / 2, size.Height / 2));
                    return string.Format("{0:F1}℃", t - 273.15f);
                });

                Safe("中心区域温度(1/2幅面)", () =>
                {
                    Rectangle roi = new Rectangle(size.Width / 4, size.Height / 4, size.Width / 2, size.Height / 2);
                    TemperaturePoint[] pts = file.GetRectangleValues(roi);
                    if (pts == null || pts.Length == 0) return "不可用";
                    float mx = float.MinValue, mn = float.MaxValue, sum = 0;
                    foreach (TemperaturePoint tp in pts)
                    {
                        if (tp.Value > mx) mx = tp.Value;
                        if (tp.Value < mn) mn = tp.Value;
                        sum += tp.Value;
                    }
                    return string.Format("最高 {0:F1}℃  最低 {1:F1}℃  平均 {2:F1}℃", mx - 273.15f, mn - 273.15f, sum / pts.Length - 273.15f);
                });
            }

            string thermalPng = Path.Combine(outDir, name + ".thermal.png");
            Save(file.GetThermalImage(), thermalPng);
            string visualPng = Path.Combine(outDir, name + ".visual.png");
            Save(file.GetVisualImage(), visualPng);
            string coverPng = Path.Combine(outDir, name + ".cover.png");
            Save(file.GetCoverImage(), coverPng);
            string palettePng = Path.Combine(outDir, name + ".palette.png");
            Save(file.GetPaletteImage(), palettePng);
            Console.WriteLine();
            Console.WriteLine("已导出图像: " + Path.GetFileName(thermalPng) + ", " + Path.GetFileName(visualPng)
                + (File.Exists(coverPng) ? ", " + Path.GetFileName(coverPng) : "")
                + (File.Exists(palettePng) ? ", " + Path.GetFileName(palettePng) : ""));
        }
        catch (Exception ex)
        {
            Console.WriteLine("[异常] " + ex.GetType().Name + ": " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(4);
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    static string S(string s) { return string.IsNullOrEmpty(s) ? "-" : s; }

    static void Safe(string label, Func<string> get)
    {
        try { Console.WriteLine("  " + label + ": " + get()); }
        catch (Exception ex) { Console.WriteLine("  " + label + ": 不可用 (" + ex.Message.Replace('\n', ' ').Replace('\r', ' ') + ")"); }
    }

    static void Save(Image img, string path)
    {
        if (img == null) return;
        img.Save(path, ImageFormat.Png);
        img.Dispose();
    }
}
