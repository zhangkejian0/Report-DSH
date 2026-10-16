using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Amur.IRtekACFileSDK;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;
using FileType = Amur.ThermalImageFile.FileType;
using CameraInfo = Amur.ThermalImageFile.CameraInfo;
using Frame = Amur.ThermalImageFile.Frame;
using AcFileType = Amur.IRtekACFileSDK.FileType;

/// <summary>
/// IRtek SDK × IRAC 图片数据验证工具。
/// 遍历样本目录，分别用热像 SDK 与声像 SDK 识别并解析，
/// 输出元数据 / 温度场 / 声压级统计，并把内部图像导出为 PNG。
/// </summary>
public static class VerifyIRAC
{
    private static int _thermalCount;
    private static int _acousticCount;
    private static int _unknownCount;

    public static int Main(string[] args)
    {
        string iracDir = args.Length > 0 ? args[0] : @"D:\develop\Report Studio\IRAC";
        string outDir = args.Length > 1 ? args[1] : @"D:\develop\Report Studio\IRAC\_parsed";
        Directory.CreateDirectory(outDir);

        List<string> files = new List<string>(Directory.EnumerateFiles(iracDir, "*.jpg", SearchOption.AllDirectories));
        files.Sort(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine("=== IRtek SDK 图片数据解析验证 ===");
        Console.WriteLine("样本目录: " + iracDir);
        Console.WriteLine("图片数量: " + files.Count);
        Console.WriteLine("图像导出: " + outDir);

        var thermalValidator = IRtekFileSDKValidator.Instance;
        var acousticValidator = IRtekACFileSDKValidator.Instance;

        foreach (string f in files)
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("文件: " + f.Replace(iracDir, "").TrimStart('\\'));

            FileType thermalType = thermalValidator.IsValidFile(f);
            if (thermalType == FileType.ThermalPicture || thermalType == FileType.ThermalVideo
                || thermalType == FileType.GridThermalPicture)
            {
                VerifyThermal(f, thermalType, outDir);
                continue;
            }

            AcFileType acousticType;
            if (acousticValidator.IsValidFile(f, out acousticType))            {
                VerifyAcoustic(f, acousticType, outDir);
                continue;
            }

            _unknownCount++;
            Console.WriteLine("  [失败] 两个 SDK 均无法识别 (FileType.Other)");
        }

        Console.WriteLine();
        Console.WriteLine("=== 汇总 ===");
        Console.WriteLine(string.Format(
            "热像识别: {0} 个 | 声像识别: {1} 个 | 无法识别: {2} 个 | 总计: {3} 个",
            _thermalCount, _acousticCount, _unknownCount, files.Count));
        Console.WriteLine(string.Format("解析结果图像已导出到: {0}", outDir));
        return 0;
    }

    // ---------------- 热像 ----------------

    private static void VerifyThermal(string path, FileType fileType, string outDir)
    {
        IThermalImageFile file = null;
        try
        {
            file = IRtekFileSDKValidator.Instance.Create();
            if (!file.Open(path))
            {
                Console.WriteLine("  [失败] 识别为热像但 Open 失败");
                _unknownCount++;
                return;
            }

            _thermalCount++;
            Console.WriteLine("  [成功] 热像文件 (" + fileType + ")");
            Console.WriteLine("  文件版本: v" + file.FileVersion);

            Size size = file.GetThermalSize();
            Console.WriteLine(string.Format("  热像分辨率: {0} x {1}", size.Width, size.Height));

            try
            {
                CameraInfo cam = file.GetCameraInfo();
                Console.WriteLine("  相机: " + SafeStr(cam.Model) + "  序列号: " + SafeStr(cam.SerialNo) + "  品牌: " + SafeStr(cam.Brand));
            }
            catch (Exception ex) { Console.WriteLine("  相机信息: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                LensInfo lens = file.GetLensInfo();
                Console.WriteLine(string.Format("  镜头: {0}  焦距: {1}mm  波段: {2}", SafeStr(lens.Name), lens.FocalLength, SafeStr(lens.Band)));
            }
            catch (Exception ex) { Console.WriteLine("  镜头信息: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                ThermalParams p = file.GetThermalParams();
                Console.WriteLine(string.Format(
                    "  测温参数: 发射率={0:F2}  距离={1:F1}m  反射温度={2:F1}℃  湿度={3:F2}",
                    p.Emissivity, p.Distance, p.ReflTemp - 273.15f, p.RelHumidity));
            }
            catch (Exception ex) { Console.WriteLine("  测温参数: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                Gain gain = file.GetGain();
                Console.WriteLine(string.Format("  温宽: {0}  [{1:F1}℃, {2:F1}℃]", gain.Mode, gain.Min - 273.15f, gain.Max - 273.15f));
            }
            catch (Exception ex) { Console.WriteLine("  温宽: 不可用 (" + Trim(ex.Message) + ")"); }

            // 全幅最高/最低温度
            try
            {
                TemperaturePoint[] pts = file.GetRectangleValues(new Rectangle(0, 0, size.Width, size.Height));
                if (pts != null && pts.Length > 0)
                {
                    float max = float.MinValue, min = float.MaxValue;
                    foreach (TemperaturePoint tp in pts)
                    {
                        if (tp.Value > max) max = tp.Value;
                        if (tp.Value < min) min = tp.Value;
                    }
                    Console.WriteLine(string.Format(
                        "  全幅温度场: 最高 {0:F1}℃ | 最低 {1:F1}℃ | 采样点 {2}",
                        max - 273.15f, min - 273.15f, pts.Length));
                }
            }
            catch (Exception ex) { Console.WriteLine("  全幅温度场: 不可用 (" + Trim(ex.Message) + ")"); }

            // 中心点温度
            try
            {
                float center = file.GetSpotValue(new Point(size.Width / 2, size.Height / 2));
                Console.WriteLine(string.Format("  中心点温度: {0:F1}℃", center - 273.15f));
            }
            catch (Exception ex) { Console.WriteLine("  中心点温度: 不可用 (" + Trim(ex.Message) + ")"); }

            // 原始温度矩阵
            try
            {
                Frame frame = file.GetFrame();
                if (frame.Values != null)
                {
                    int h = frame.Values.GetLength(0);
                    int w = frame.Values.GetLength(1);
                    Console.WriteLine(string.Format("  温度矩阵: {0} x {1} (AD值)  时间戳: {2}", w, h, frame.TimeStamp));
                }
            }
            catch (Exception ex) { Console.WriteLine("  温度矩阵: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                Console.WriteLine("  文件内置测量标记: " + file.Markers.Count);
            }
            catch (Exception ex) { Console.WriteLine("  内置标记: 不可用 (" + Trim(ex.Message) + ")"); }

            SaveImage(file.GetThermalImage(), Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".thermal.png"));
            SaveImage(file.GetVisualImage(), Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".visual.png"));
            Console.WriteLine("  已导出: 热像图 / 可见光图 PNG");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [异常] " + ex.GetType().Name + ": " + Trim(ex.Message));
            _unknownCount++;
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    // ---------------- 声像 ----------------

    private static void VerifyAcoustic(string path, AcFileType fileType, string outDir)
    {
        AcousticImageFile file = null;
        try
        {
            file = IRtekACFileSDKValidator.Instance.Create();
            if (!file.Open(path))
            {
                Console.WriteLine("  [失败] 识别为声像但 Open 失败");
                _unknownCount++;
                return;
            }

            _acousticCount++;
            Console.WriteLine("  [成功] 声像文件 (" + fileType + ")");

            try
            {
                Amur.IRtekACFileSDK.CameraInfo cam = file.GetCameraInfo();
                Console.WriteLine("  相机: " + SafeStr(cam.Model) + "  序列号: " + SafeStr(cam.SerialNo));
            }
            catch (Exception ex) { Console.WriteLine("  相机信息: 不可用 (" + Trim(ex.Message) + ")"); }

            Size size = file.GetAcousticSize();
            Console.WriteLine(string.Format("  声像分辨率: {0} x {1}", size.Width, size.Height));

            try
            {
                DateTime shot = file.GetShotTime();
                Console.WriteLine("  拍摄时间: " + shot.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex) { Console.WriteLine("  拍摄时间: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                AcousticParams p = file.GetAcousticParams();
                Console.WriteLine(string.Format("  声像参数: 增益={0}  平滑={1:F2}  频带数={2}",
                    p.Gain, p.Smooth, p.BandList != null ? p.BandList.Count : 0));
                if (p.BandList != null)
                {
                    foreach (Band b in p.BandList)
                    {
                        Console.WriteLine(string.Format("    频带: {0:F0} Hz ~ {1:F0} Hz", b.Min, b.Max));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("  声像参数: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                DbScale db = file.GetDbScale();
                Console.WriteLine(string.Format("  声宽: {0}  [{1:F1} dB, {2:F1} dB]", db.Mode, db.Min, db.Max));
            }
            catch (Exception ex) { Console.WriteLine("  声宽: 不可用 (" + Trim(ex.Message) + ")"); }

            // 声压级测量（内存标记，不保存，不影响原文件）
            try
            {
                AcousticImageFile.MarkerSpot spot = new AcousticImageFile.MarkerSpot(file, "C", new Point(size.Width / 2, size.Height / 2));
                file.Markers.Add(spot);
                Console.WriteLine(string.Format("  中心点声压级: {0:F1} dB", spot.Value));
            }
            catch (Exception ex) { Console.WriteLine("  中心点声压级: 不可用 (" + Trim(ex.Message) + ")"); }

            try
            {
                Rectangle roi = new Rectangle(size.Width / 4, size.Height / 4, size.Width / 2, size.Height / 2);
                AcousticImageFile.MarkerEllipse ellipse = new AcousticImageFile.MarkerEllipse(file, "ROI", roi);
                file.Markers.Add(ellipse);
                Console.WriteLine(string.Format(
                    "  中心区域声压级: 最大 {0:F1} dB  热点({1}, {2})",
                    ellipse.Max, ellipse.HotSpot.X, ellipse.HotSpot.Y));
            }
            catch (Exception ex) { Console.WriteLine("  区域声压级: 不可用 (" + Trim(ex.Message) + ")"); }

            SaveBitmap(file.GetAcousticImage(), Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".acoustic.png"));
            SaveBitmap(file.GetVisualImage(), Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".visual.png"));
            Console.WriteLine("  已导出: 声像图 / 可见光图 PNG");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  [异常] " + ex.GetType().Name + ": " + Trim(ex.Message));
            _unknownCount++;
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    // ---------------- 工具 ----------------

    private static string SafeStr(string s)
    {
        return string.IsNullOrEmpty(s) ? "-" : s;
    }

    private static string Trim(string s)
    {
        if (string.IsNullOrEmpty(s)) return "-";
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
    }

    private static void SaveImage(Image img, string path)
    {
        if (img == null) return;
        img.Save(path, ImageFormat.Png);
        img.Dispose();
    }

    private static void SaveBitmap(Bitmap bmp, string path)
    {
        if (bmp == null) return;
        bmp.Save(path, ImageFormat.Png);
        bmp.Dispose();
    }
}
