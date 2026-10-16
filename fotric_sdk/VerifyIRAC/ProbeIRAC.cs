using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Amur.IRtekACFileSDK;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;
using TFileType = Amur.ThermalImageFile.FileType;
using ACFileType = Amur.IRtekACFileSDK.FileType;
using TCam = Amur.ThermalImageFile.CameraInfo;
using ACam = Amur.IRtekACFileSDK.CameraInfo;
using TAudio = Amur.ThermalImageFile.AudioAnnotation;
using AAudio = Amur.IRtekACFileSDK.AudioAnnotation;
using TZoom = Amur.ThermalImageFile.ZoomInfo;
using AZoom = Amur.IRtekACFileSDK.ZoomInfo;
using TMarkerType = Amur.ThermalImageFile.MarkerType;
using AMarkerType = Amur.IRtekACFileSDK.MarkerType;
using TPreset = Amur.ThermalImageFile.PresetPalette;
using APreset = Amur.IRtekACFileSDK.PresetPalette;
using TPalette = Amur.ThermalImageFile.Palette;
using APalette = Amur.IRtekACFileSDK.Palette;
using TFrame = Amur.ThermalImageFile.Frame;
using AFrame = Amur.IRtekACFileSDK.Frame;

/// <summary>
/// IRtek SDK × IRAC 深度数据探测。
/// 在 VerifyIRAC 基础上,把两个 SDK 公开的全部读取接口逐一调用:
/// 元数据 / 标记详情 / 用户数据键值对 / 音频 / 传感器 / 频谱 / PRPD / 声压网格等。
/// 只读不写,不调用 Save,不影响原文件。
/// </summary>
public static class ProbeIRAC
{
    private static StreamWriter _report;
    private static int _thermalCount;
    private static int _acousticCount;
    private static int _unknownCount;

    public static int Main(string[] args)
    {
        string iracDir = args.Length > 0 ? args[0] : @"D:\develop\report-editor\canvas-editor\IRAC";
        string outDir = args.Length > 1 ? args[1] : Path.Combine(iracDir, "_parsed");
        string reportFile = args.Length > 2 ? args[2] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "探测结果.txt");
        Directory.CreateDirectory(outDir);
        _report = new StreamWriter(reportFile, false, new UTF8Encoding(true));

        List<string> files = new List<string>(Directory.EnumerateFiles(iracDir, "*.jpg", SearchOption.AllDirectories));
        files.Sort(StringComparer.OrdinalIgnoreCase);

        Out("=== IRtek SDK 深度数据探测 ===");
        Out("样本目录: " + iracDir);
        Out("图片数量: " + files.Count);
        Out("图像导出: " + outDir);

        var thermalValidator = IRtekFileSDKValidator.Instance;
        var acousticValidator = IRtekACFileSDKValidator.Instance;

        // SDK 自带的预置调色板
        ProbePresetPalettes();

        foreach (string f in files)
        {
            Out("");
            Out("----------------------------------------");
            Out("文件: " + f.Replace(iracDir, "").TrimStart('\\'));

            TFileType thermalType = thermalValidator.IsValidFile(f);
            if (thermalType == TFileType.ThermalPicture || thermalType == TFileType.ThermalVideo
                || thermalType == TFileType.GridThermalPicture)
            {
                ProbeThermal(f, thermalType, outDir);
                continue;
            }

            ACFileType acousticType;
            if (acousticValidator.IsValidFile(f, out acousticType))
            {
                ProbeAcoustic(f, acousticType, outDir);
                continue;
            }

            _unknownCount++;
            Out("  [失败] 两个 SDK 均无法识别");
        }

        Out("");
        Out("=== 汇总 ===");
        Out(string.Format("热像: {0} | 声像: {1} | 无法识别: {2} | 总计: {3}",
            _thermalCount, _acousticCount, _unknownCount, files.Count));
        _report.Dispose();
        Console.WriteLine("报告: " + Path.GetFullPath(reportFile));
        return 0;
    }

    // ---------------- 预置调色板 ----------------

    private static void ProbePresetPalettes()
    {
        Out("");
        Out("== SDK 预置调色板 ==");
        try
        {
            List<TPreset> palettes = IRtekFileSDKValidator.Instance.Create().GetPresetPalettes();
            List<string> names = new List<string>();
            foreach (TPreset p in palettes)
            {
                names.Add(string.Format("{0}:{1}", p.RefNo, p.Name));
            }
            Out("  热像: " + string.Join(", ", names.ToArray()));
        }
        catch (Exception ex) { Out("  热像预置调色板: 不可用 (" + Trim(ex.Message) + ")"); }

        try
        {
            List<APreset> palettes = AcousticImageFile.GetPresetPalettes();
            List<string> names = new List<string>();
            foreach (APreset p in palettes)
            {
                names.Add(string.Format("{0}:{1}", p.RefNo, p.Name));
            }
            Out("  声像: " + string.Join(", ", names.ToArray()));
        }
        catch (Exception ex) { Out("  声像预置调色板: 不可用 (" + Trim(ex.Message) + ")"); }
    }

    // ---------------- 热像 ----------------

    private static void ProbeThermal(string path, TFileType fileType, string outDir)
    {
        IThermalImageFile file = null;
        string stem = Path.GetFileNameWithoutExtension(path);
        try
        {
            file = IRtekFileSDKValidator.Instance.Create();
            if (!file.Open(path))
            {
                Out("  [失败] 识别为热像但 Open 失败");
                _unknownCount++;
                return;
            }
            _thermalCount++;

            Out("  [类型] " + fileType + "  文件版本 v" + file.FileVersion);

            Size size = file.GetThermalSize();
            Out(string.Format("  [分辨率] 热像 {0} x {1}", size.Width, size.Height));

            Try("设备信息", delegate()
            {
                TCam cam = file.GetCameraInfo();
                Out(string.Format("  [设备] 型号={0}  序列号={1}  品牌={2}  厂商={3}  Logo={4}",
                    SafeStr(cam.Model), SafeStr(cam.SerialNo), SafeStr(cam.Brand), SafeStr(cam.Company),
                    cam.Logo != null ? string.Format("{0}x{1}", cam.Logo.Width, cam.Logo.Height) : "无"));
            });

            Try("镜头", delegate()
            {
                LensInfo lens = file.GetLensInfo();
                Out(string.Format("  [镜头] 名称={0}  焦距={1:F1}mm  水平视场角={2:F1}°  波段={3}",
                    SafeStr(lens.Name), lens.FocalLength, lens.HFov, SafeStr(lens.Band)));
            });

            Try("测温参数", delegate()
            {
                ThermalParams p = file.GetThermalParams();
                Out(string.Format("  [测温] 发射率={0:F3}  距离={1:F2}m  反射温度={2:F1}℃  大气温度={3:F1}℃  湿度={4:F2}  外部光学温度={5:F1}℃  外部光学透射率={6:F2}",
                    p.Emissivity, p.Distance, p.ReflTemp - 273.15f, p.AtmTemp - 273.15f,
                    p.RelHumidity, p.ExtOptTemp - 273.15f, p.ExtOptTrans));
            });

            Try("温宽", delegate()
            {
                Gain gain = file.GetGain();
                Out(string.Format("  [温宽] 模式={0}  [{1:F1}℃, {2:F1}℃]  固定跨度={3}  跨度={4:F1}",
                    gain.Mode, gain.Min - 273.15f, gain.Max - 273.15f, gain.FixedSpan, gain.Span));
            });

            Try("测温范围", delegate()
            {
                TempRange range = file.GetTempRange();
                Out(string.Format("  [测温范围] [{0:F1}℃, {1:F1}℃]", range.Min - 273.15f, range.Max - 273.15f));
            });

            Try("调色板", delegate()
            {
                TPalette pal = file.GetPalette();
                Out(string.Format("  [调色板] 编号=0x{0:X2}  反相={1}  自定义色数={2}",
                    pal.RefNo, pal.Inverted, pal.Colors != null ? pal.Colors.Length : 0));
            });

            Try("拍摄时间", delegate()
            {
                Out("  [拍摄时间] " + file.GetShotTime().ToString("yyyy-MM-dd HH:mm:ss"));
            });

            Try("修改时间", delegate()
            {
                Out("  [修改时间] " + file.GetModifiedTime().ToString("yyyy-MM-dd HH:mm:ss"));
            });

            Try("GPS", delegate()
            {
                Amur.ThermalImageFile.GeoLocation geo = file.GetGeoLocation();
                if (geo.IsGPSValid)
                {
                    Out(string.Format("  [GPS] 经度={0:F6}  纬度={1:F6}  海拔={2:F1}m  罗盘有效={3}  方向={4}°",
                        geo.Longitude, geo.Latitude, geo.Altitude, geo.IsCompassValid, geo.Direction));
                }
                else
                {
                    Out("  [GPS] 无有效数据");
                }
            });

            Try("备注", delegate()
            {
                string note = file.GetNoteAnnotation();
                Out("  [备注] " + (string.IsNullOrEmpty(note) ? "(空)" : note));
            });

            Try("用户数据", delegate()
            {
                string[] keys = file.GetUserDataKeys();
                if (keys == null || keys.Length == 0)
                {
                    Out("  [用户数据] (无)");
                }
                else
                {
                    foreach (string key in keys)
                    {
                        string value = file.GetUserData(key);
                        Out(string.Format("  [用户数据] {0} = {1}", key, Truncate(value)));
                    }
                }
            });

            Try("测量标记", delegate()
            {
                Out("  [测量标记] 共 " + file.Markers.Count + " 个");
                for (int i = 0; i < file.Markers.Count; i++)
                {
                    IMarker m = file.Markers[i];
                    DescribeThermalMarker(file, m, "    ");
                }
            });

            Try("温差Delta", delegate()
            {
                Out("  [温差Delta] 共 " + file.Deltas.Count + " 个");
                for (int i = 0; i < file.Deltas.Count; i++)
                {
                    IDelta d = file.Deltas[i];
                    DeltaParams dp = d.DeltaParams;
                    Out(string.Format("    - {0}: {1}({2:F1}℃) - {3}({4:F1}℃) = {5:F1}K",
                        d.Name, dp.SrcName, dp.SrcTemp - 273.15f, dp.DestName, dp.DestTemp - 273.15f, d.Value));
                }
            });

            Try("帧信息", delegate()
            {
                int frameCount = file.GetFrameCount();
                float frameRate = file.GetFrameRate();
                TFrame frame = file.GetFrame();
                if (frame.Values != null)
                {
                    int h = frame.Values.GetLength(0);
                    int w = frame.Values.GetLength(1);
                    Out(string.Format("  [帧] 数量={0}  帧率={1:F1}fps  AD矩阵={2}x{3}  时间戳={4:yyyy-MM-dd HH:mm:ss}",
                        frameCount, frameRate, w, h, frame.TimeStamp));
                }
                else if (frame.Data != null)
                {
                    Out(string.Format("  [帧] 数量={0}  帧率={1:F1}fps  AD数据={2}点  时间戳={3:yyyy-MM-dd HH:mm:ss}",
                        frameCount, frameRate, frame.Data.Length, frame.TimeStamp));
                }
            });

            Try("温度场", delegate()
            {
                TemperaturePoint[] pts = file.GetRectangleValues(new Rectangle(0, 0, size.Width, size.Height));
                if (pts != null && pts.Length > 0)
                {
                    float max = float.MinValue, min = float.MaxValue;
                    int maxIdx = 0, minIdx = 0;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        if (pts[i].Value > max) { max = pts[i].Value; maxIdx = i; }
                        if (pts[i].Value < min) { min = pts[i].Value; minIdx = i; }
                    }
                    float center = file.GetSpotValue(new Point(size.Width / 2, size.Height / 2));
                    Out(string.Format("  [温度场] 最高 {0:F1}℃ @({1:F0},{2:F0}) | 最低 {3:F1}℃ @({4:F0},{5:F0}) | 中心点 {6:F1}℃ | 采样 {7}",
                        max - 273.15f, pts[maxIdx].X, pts[maxIdx].Y,
                        min - 273.15f, pts[minIdx].X, pts[minIdx].Y,
                        center - 273.15f, pts.Length));
                }
            });

            Try("数码变焦", delegate()
            {
                TZoom zoom = file.GetZoomInfo();
                Out(string.Format("  [数码变焦] 启用={0}  倍数={1:F2}", zoom.Enabled, zoom.Ratio));
            });

            Try("参考温度", delegate()
            {
                float refTemp = file.GetReferenceTemperature();
                Out(string.Format("  [参考温度] {0:F1}℃", refTemp - 273.15f));
            });

            Try("音频", delegate()
            {
                TAudio audio = file.GetAudioAnnotation();
                if (audio.IsValid && audio.Data != null && audio.Data.Length > 0)
                {
                    string ext = audio.Type.ToString().ToLowerInvariant();
                    string audioPath = Path.Combine(outDir, stem + ".audio." + ext);
                    File.WriteAllBytes(audioPath, audio.Data);
                    Out(string.Format("  [音频] {0} {1}字节 → {2}", audio.Type, audio.Data.Length, Path.GetFileName(audioPath)));
                }
                else
                {
                    Out("  [音频] 无");
                }
            });

            SaveImage(file.GetThermalImage(), Path.Combine(outDir, stem + ".thermal.png"), "热像图");
            SaveImage(file.GetVisualImage(), Path.Combine(outDir, stem + ".visual.png"), "可见光图");
            SaveImage(file.GetCoverImage(), Path.Combine(outDir, stem + ".cover.png"), "封面图");
            SaveImage(file.GetPaletteImage(), Path.Combine(outDir, stem + ".palette.png"), "调色板色带");
        }
        catch (Exception ex)
        {
            Out("  [异常] " + ex.GetType().Name + ": " + Trim(ex.Message));
            _unknownCount++;
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    private static void DescribeThermalMarker(IThermalImageFile file, IMarker m, string indent)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(indent).Append("- ").Append(m.Type).Append(" \"").Append(m.Name).Append("\"");
        try
        {
            switch (m.Type)
            {
                case TMarkerType.Spot:
                    IMarkerSpot spot = (IMarkerSpot)m;
                    sb.Append(string.Format("  点({0},{1})  温度 {2:F1}℃", spot.Point.X, spot.Point.Y, spot.Value - 273.15f));
                    break;
                case TMarkerType.Rectangle:
                {
                    IMarkerRectangle r = (IMarkerRectangle)m;
                    sb.Append(string.Format("  矩形({0},{1},{2}x{3})  最高 {4:F1}℃  最低 {5:F1}℃  平均 {6:F1}℃  热点({7},{8})  冷点({9},{10})  面积 {11:F2}m²",
                        r.Rectangle.X, r.Rectangle.Y, r.Rectangle.Width, r.Rectangle.Height,
                        r.Max - 273.15f, r.Min - 273.15f, r.Avg - 273.15f,
                        r.HotSpot.X, r.HotSpot.Y, r.ColdSpot.X, r.ColdSpot.Y, r.Area));
                    break;
                }
                case TMarkerType.Ellipse:
                {
                    IMarkerEllipse e = (IMarkerEllipse)m;
                    sb.Append(string.Format("  椭圆({0},{1},{2}x{3})  最高 {4:F1}℃  最低 {5:F1}℃  平均 {6:F1}℃  热点({7},{8})  面积 {9:F2}m²",
                        e.Rectangle.X, e.Rectangle.Y, e.Rectangle.Width, e.Rectangle.Height,
                        e.Max - 273.15f, e.Min - 273.15f, e.Avg - 273.15f,
                        e.HotSpot.X, e.HotSpot.Y, e.Area));
                    break;
                }
                case TMarkerType.Polygon:
                {
                    IMarkerPolygon p = (IMarkerPolygon)m;
                    sb.Append(string.Format("  多边形{0}个点  最高 {1:F1}℃  最低 {2:F1}℃  平均 {3:F1}℃  面积 {4:F2}m²",
                        p.Points != null ? p.Points.Length : 0,
                        p.Max - 273.15f, p.Min - 273.15f, p.Avg - 273.15f, p.Area));
                    break;
                }
                case TMarkerType.Polyline:
                {
                    IMarkerPolyline p = (IMarkerPolyline)m;
                    sb.Append(string.Format("  折线{0}个点  最高 {1:F1}℃  最低 {2:F1}℃  平均 {3:F1}℃  长度 {4:F2}m",
                        p.Points != null ? p.Points.Length : 0,
                        p.Max - 273.15f, p.Min - 273.15f, p.Avg - 273.15f, p.Length));
                    break;
                }
            }
            LocalThermalParams tp = m.ThermalParams;
            try
            {
                sb.Append(string.Format("  [标记独立测温参数 局部={0} ε={1:F2} 距离={2:F1}m]", tp.LocalParams, tp.Emissivity, tp.Distance));
            }
            catch (Exception)
            {
                // 标记测温参数缺失则跳过
            }
        }
        catch (Exception ex)
        {
            sb.Append("  (详情读取失败: " + Trim(ex.Message) + ")");
        }
        Out(sb.ToString());
    }

    // ---------------- 声像 ----------------

    private static void ProbeAcoustic(string path, ACFileType fileType, string outDir)
    {
        AcousticImageFile file = null;
        string stem = Path.GetFileNameWithoutExtension(path);
        try
        {
            file = IRtekACFileSDKValidator.Instance.Create();
            if (!file.Open(path))
            {
                Out("  [失败] 识别为声像但 Open 失败");
                _unknownCount++;
                return;
            }
            _acousticCount++;

            Out("  [类型] " + fileType);

            Size size = file.GetAcousticSize();
            Out(string.Format("  [分辨率] 声像 {0} x {1}", size.Width, size.Height));

            Try("设备信息", delegate()
            {
                ACam cam = file.GetCameraInfo();
                Out(string.Format("  [设备] 型号={0}  序列号={1}  品牌={2}  厂商={3}  Logo={4}",
                    SafeStr(cam.Model), SafeStr(cam.SerialNo), SafeStr(cam.Brand), SafeStr(cam.Company),
                    cam.Logo != null ? string.Format("{0}x{1}", cam.Logo.Width, cam.Logo.Height) : "无"));
            });

            Try("传感器", delegate()
            {
                SensorInfo sensor = file.GetSensorInfo();
                Out(string.Format("  [传感器] 序列号={0}  通道数={1}  监视麦={2}  采样率={3}Hz  固件={4}  最大幅度={5:F2}",
                    SafeStr(sensor.SerialNo), sensor.ChannelCount, sensor.RefChannel,
                    sensor.SampleRate, SafeStr(sensor.Version), sensor.Amplitude));
            });

            Try("拍摄时间", delegate()
            {
                Out("  [拍摄时间] " + file.GetShotTime().ToString("yyyy-MM-dd HH:mm:ss"));
            });

            Try("修改时间", delegate()
            {
                Out("  [修改时间] " + file.GetModifiedTime().ToString("yyyy-MM-dd HH:mm:ss"));
            });

            Try("声像参数", delegate()
            {
                AcousticParams p = file.GetAcousticParams();
                Out(string.Format("  [声像参数] 增益={0}  平滑={1:F2}  频带数={2}", p.Gain, p.Smooth,
                    p.BandList != null ? p.BandList.Count : 0));
                if (p.BandList != null)
                {
                    foreach (Band b in p.BandList)
                    {
                        Out(string.Format("    频带: {0:F0} Hz ~ {1:F0} Hz", b.Min, b.Max));
                    }
                }
            });

            Try("频率范围", delegate()
            {
                Band range = file.GetFrequencyRange();
                Out(string.Format("  [频率范围] {0:F0} Hz ~ {1:F0} Hz", range.Min, range.Max));
            });

            Try("声宽", delegate()
            {
                DbScale db = file.GetDbScale();
                Out(string.Format("  [声宽] 模式={0}  [{1:F1} dB, {2:F1} dB]  固定跨度={3}  跨度={4:F1}",
                    db.Mode, db.Min, db.Max, db.FixedSpan, db.Span));
            });

            Try("检测模式", delegate()
            {
                Out(string.Format("  [检测模式] {0}  聚焦={1}  范围={2:F2}  灰度可见光={3}",
                    file.GetCaptureMode(), file.GetFocusMode().Enabled, file.GetFocusMode().Scope, file.GetGreyScale()));
            });

            Try("距离", delegate()
            {
                Out(string.Format("  [距离] 目标距离={0:F1}m  标准声压级修正={1}  交流电频率={2:F0}Hz",
                    file.GetDistance(), file.GetNormalizeDistance(), file.GetAcFrequency()));
            });

            Try("GPS", delegate()
            {
                Amur.IRtekACFileSDK.GeoLocation geo = file.GetGeoLocation();
                if (geo.IsGPSValid)
                {
                    Out(string.Format("  [GPS] 经度={0:F6}  纬度={1:F6}  海拔={2:F1}m  罗盘有效={3}  方向={4}°",
                        geo.Longitude, geo.Latitude, geo.Altitude, geo.IsCompassValid, geo.Direction));
                }
                else
                {
                    Out("  [GPS] 无有效数据");
                }
            });

            Try("备注", delegate()
            {
                string note = file.GetNoteAnnotation();
                Out("  [备注] " + (string.IsNullOrEmpty(note) ? "(空)" : note));
            });

            Try("用户数据", delegate()
            {
                string[] keys = file.GetUserDataKeys();
                if (keys == null || keys.Length == 0)
                {
                    Out("  [用户数据] (无)");
                }
                else
                {
                    foreach (string key in keys)
                    {
                        string value = file.GetUserData(key);
                        Out(string.Format("  [用户数据] {0} = {1}", key, Truncate(value)));
                    }
                }
            });

            Try("收藏/标签", delegate()
            {
                string tag = file.GetTagAnnotation();
                Out(string.Format("  [收藏/标签] 收藏={0}  标签={1}  分组文件=[{2}]",
                    file.GetFavorite(), string.IsNullOrEmpty(tag) ? "(无)" : tag,
                    string.Join(",", file.GetGroupFiles())));
            });

            Try("调色板", delegate()
            {
                APalette pal = file.GetPalette();
                Out(string.Format("  [调色板] 编号=0x{0:X2}  透明度={1}", pal.RefNo, pal.Alpha));
            });

            Try("数码变焦", delegate()
            {
                AZoom zoom = file.GetZoomInfo();
                Out(string.Format("  [数码变焦] 启用={0}  倍数={1:F2}", zoom.Enabled, zoom.Ratio));
            });

            Try("测量标记", delegate()
            {
                Out("  [测量标记] 共 " + file.Markers.Count + " 个");
                for (int i = 0; i < file.Markers.Count; i++)
                {
                    AcousticImageFile.Marker m = file.Markers[i];
                    try { m.Sync(); }
                    catch (Exception ex) { Out("    - " + m.Type + " \"" + m.Name + "\" (Sync失败: " + Trim(ex.Message) + ")"); continue; }

                    if (m.Type == AMarkerType.Spot)
                    {
                        AcousticImageFile.MarkerSpot spot = (AcousticImageFile.MarkerSpot)m;
                        Out(string.Format("    - Spot \"{0}\"  点({1},{2})  声压级 {3:F1} dB",
                            spot.Name, spot.Point.X, spot.Point.Y, spot.Value));
                    }
                    else if (m.Type == AMarkerType.Ellipse)
                    {
                        AcousticImageFile.MarkerEllipse e = (AcousticImageFile.MarkerEllipse)m;
                        Out(string.Format("    - Ellipse \"{0}\"  区域({1},{2},{3}x{4})  最大 {5:F1} dB  热点({6},{7})",
                            e.Name, e.Rectangle.X, e.Rectangle.Y, e.Rectangle.Width, e.Rectangle.Height,
                            e.Max, e.HotSpot.X, e.HotSpot.Y));
                    }
                    else
                    {
                        Out("    - " + m.Type + " \"" + m.Name + "\"");
                    }
                }
            });

            Try("声压级探测", delegate()
            {
                float center = file.GetSpotValue(new Point(size.Width / 2, size.Height / 2));
                Rectangle roi = new Rectangle(size.Width / 4, size.Height / 4, size.Width / 2, size.Height / 2);
                float[] values = file.GetEllipseValues(roi);
                if (values != null && values.Length > 0)
                {
                    float max = float.MinValue, min = float.MaxValue, sum = 0;
                    foreach (float v in values)
                    {
                        if (v > max) max = v;
                        if (v < min) min = v;
                        sum += v;
                    }
                    Out(string.Format("  [声压级探测] 中心点 {0:F1} dB | 中央区域({1},{2},{3}x{4}) 最大 {5:F1} dB  最小 {6:F1} dB  平均 {7:F1} dB  采样 {8}",
                        center, roi.X, roi.Y, roi.Width, roi.Height, max, min, sum / values.Length, values.Length));
                }
                else
                {
                    Out(string.Format("  [声压级探测] 中心点 {0:F1} dB | 区域采样失败", center));
                }
            });

            Try("帧数据", delegate()
            {
                int frameCount = file.GetFrameCount();
                float frameRate = file.GetFrameRate();
                uint frameIndex = file.GetFrameIndex();
                AFrame frame = file.GetFrame(frameIndex);
                int splW = frame.AcousticImageSpl != null ? frame.AcousticImageSpl.GetLength(1) : 0;
                int splH = frame.AcousticImageSpl != null ? frame.AcousticImageSpl.GetLength(0) : 0;
                float splMax = float.MinValue, splMin = float.MaxValue, splSum = 0;
                if (frame.AcousticImageSpl != null)
                {
                    foreach (float v in frame.AcousticImageSpl)
                    {
                        if (v > splMax) splMax = v;
                        if (v < splMin) splMin = v;
                        splSum += v;
                    }
                }
                Out(string.Format("  [帧] 数量={0}  帧率={1:F1}fps  当前帧={2}  相对时间戳={3}ms", frameCount, frameRate, frameIndex, frame.TimeStamp));
                if (splW > 0)
                {
                    Out(string.Format("    声压级网格: {0}x{1}  最大 {2:F1} dB  最小 {3:F1} dB  平均 {4:F1} dB",
                        splW, splH, splMax, splMin, splSum / (splW * splH)));
                }
                if (frame.Sound.Source != null)
                {
                    Out(string.Format("    声音数据: 原始{0}点  滤波{1}点  降采样{2}点",
                        frame.Sound.Source.Length, frame.Sound.Filter != null ? frame.Sound.Filter.Length : 0,
                        frame.Sound.DownSample != null ? frame.Sound.DownSample.Length : 0));
                }
                if (frame.Spectrum.FFT != null)
                {
                    Out(string.Format("    频谱FFT: {0}点  范围 {1:F0}~{2:F0} Hz  分辨率 {3:F1} Hz/点",
                        frame.Spectrum.FFT.Length, frame.Spectrum.Range.Min, frame.Spectrum.Range.Max, frame.Spectrum.Delta));
                }
                Out(string.Format("    PRPD散点: {0}点  可见光JPEG: {1}字节",
                    frame.PRPD != null ? frame.PRPD.Length : 0,
                    frame.VisualImage != null ? frame.VisualImage.Length : 0));
            });

            Try("声像图", delegate()
            {
                Bitmap acoustic = file.GetAcousticImage();
                if (acoustic != null)
                {
                    Amur.IRtekACFileSDK.AcousticSummary summary = file.AcousticSummary;
                    Out(string.Format("  [声像极值] 最大 {0:F1} dB @({1},{2})  最小 {3:F1} dB @({4},{5})",
                        summary.Max.Value, summary.Max.X, summary.Max.Y,
                        summary.Min.Value, summary.Min.X, summary.Min.Y));
                }
            });

            Try("音频", delegate()
            {
                AAudio audio = file.GetAudioAnnotation();
                if (audio.IsValid && audio.Data != null && audio.Data.Length > 0)
                {
                    string ext = audio.Type.ToString().ToLowerInvariant();
                    string audioPath = Path.Combine(outDir, stem + ".audio." + ext);
                    File.WriteAllBytes(audioPath, audio.Data);
                    Out(string.Format("  [音频] {0} {1}字节 → {2}", audio.Type, audio.Data.Length, Path.GetFileName(audioPath)));
                }
                else
                {
                    Out("  [音频] 无");
                }
            });

            SaveImage(file.GetAcousticImage(), Path.Combine(outDir, stem + ".acoustic.png"), "声像融合图");
            SaveImage(file.GetVisualImage(), Path.Combine(outDir, stem + ".visual.png"), "可见光图");
            SaveImage(file.GetCoverImage(), Path.Combine(outDir, stem + ".cover.png"), "封面图");
            SaveImage(file.GetPaletteImage(), Path.Combine(outDir, stem + ".palette.png"), "调色板色带");
        }
        catch (Exception ex)
        {
            Out("  [异常] " + ex.GetType().Name + ": " + Trim(ex.Message));
            _unknownCount++;
        }
        finally
        {
            if (file != null) file.Close();
        }
    }

    // ---------------- 工具 ----------------

    private static void Try(string label, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Out("  [" + label + "] 不可用 (" + Trim(ex.Message) + ")");
        }
    }

    private static string SafeStr(string s)
    {
        return string.IsNullOrEmpty(s) ? "-" : s;
    }

    private static string Trim(string s)
    {
        if (string.IsNullOrEmpty(s)) return "-";
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
    }

    private static string Truncate(string s)
    {
        if (string.IsNullOrEmpty(s)) return "(空)";
        s = s.Replace("\n", "\\n");
        return s.Length > 200 ? s.Substring(0, 200) + "…(共" + s.Length + "字符)" : s;
    }

    private static void SaveImage(Image img, string path, string label)
    {
        if (img == null)
        {
            Out("  [图像] " + label + ": 无");
            return;
        }
        img.Save(path, ImageFormat.Png);
        Out(string.Format("  [图像] {0}: {1}x{2} → {3}", label, img.Width, img.Height, Path.GetFileName(path)));
        img.Dispose();
    }

    private static void Out(string line)
    {
        Console.WriteLine(line);
        _report.WriteLine(line);
    }
}
