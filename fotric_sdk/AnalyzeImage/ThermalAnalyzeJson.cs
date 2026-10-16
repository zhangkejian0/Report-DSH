using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 用 IRtek 热像 SDK 分析一张热像图并输出单个 JSON 文件（供 DSH thermal-analyze 服务消费）。
/// 用法: ThermalAnalyzeJson.exe 图片路径 输出目录
/// 成功: 输出目录下写 <名称>.json，退出码 0。
/// 失败: stdout 打印 "ERROR <code> <message>"，退出码 2=非热像文件 3=打开失败 4=异常。
/// JSON 内温度均为摄氏度；thermal/palette 图像以 base64 PNG 内嵌。
/// </summary>
static class ThermalAnalyzeJson
{
    static StringBuilder json = new StringBuilder();

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length < 2) { Console.WriteLine("ERROR usage <image> <outDir>"); return 4; }
        string filePath = args[0];
        string outDir = args[1];
        try
        {
            Directory.CreateDirectory(outDir);
            var validator = IRtekFileSDKValidator.Instance;
            FileType fileType = validator.IsValidFile(filePath);
            if (fileType != FileType.ThermalPicture && fileType != FileType.GridThermalPicture)
            {
                Console.WriteLine("ERROR not-thermal " + fileType);
                return 2;
            }
            var file = validator.Create();
            try
            {
                if (!file.Open(filePath)) { Console.WriteLine("ERROR open-failed"); return 3; }
                WriteJson(file, filePath, outDir);
                return 0;
            }
            finally { file.Close(); }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR failed " + ex.Message.Replace('\n', ' ').Replace('\r', ' '));
            return 4;
        }
    }

    static void WriteJson(IThermalImageFile file, string filePath, string outDir)
    {
        Size size = file.GetThermalSize();
        int width = size.Width, height = size.Height;

        json.Append('{');

        json.Append("\"file\":{");
        Str("path", filePath); json.Append(',');
        Str("type", "ThermalPicture"); json.Append(',');
        Num("version", file.FileVersion); json.Append(',');
        Str("modifiedTime", TryS(() => file.GetModifiedTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))); json.Append(',');
        Num("width", width); json.Append(',');
        Num("height", height);
        json.Append("},");

        json.Append("\"camera\":{");
        Str("model", TryS(() => file.GetCameraInfo().Model)); json.Append(',');
        Str("serialNo", TryS(() => file.GetCameraInfo().SerialNo)); json.Append(',');
        Str("brand", TryS(() => file.GetCameraInfo().Brand)); json.Append(',');
        Str("company", TryS(() => file.GetCameraInfo().Company));
        json.Append("},");

        json.Append("\"lens\":{");
        Num("focalLength", TryF(() => file.GetLensInfo().FocalLength)); json.Append(',');
        Num("hFov", TryF(() => file.GetLensInfo().HFov));
        json.Append("},");

        json.Append("\"params\":{");
        Num("emissivity", TryF(() => file.GetThermalParams().Emissivity)); json.Append(',');
        Num("distance", TryF(() => file.GetThermalParams().Distance)); json.Append(',');
        Num("reflTemp", TryF(() => file.GetThermalParams().ReflTemp - 273.15f)); json.Append(',');
        Num("atmTemp", TryF(() => file.GetThermalParams().AtmTemp - 273.15f)); json.Append(',');
        Num("relHumidity", TryF(() => file.GetThermalParams().RelHumidity));
        json.Append("},");

        json.Append("\"gain\":{");
        Str("mode", TryS(() => file.GetGain().Mode.ToString())); json.Append(',');
        Num("min", TryF(() => file.GetGain().Min - 273.15f)); json.Append(',');
        Num("max", TryF(() => file.GetGain().Max - 273.15f));
        json.Append("},");

        // 全幅温度场
        float[,] temp = new float[height, width];
        TemperaturePoint[] pts = file.GetRectangleValues(new Rectangle(0, 0, width, height));
        float mn = float.MaxValue, mx = float.MinValue, sum = 0;
        int hotX = 0, hotY = 0, coldX = 0, coldY = 0;
        foreach (TemperaturePoint tp in pts)
        {
            float c = tp.Value - 273.15f;
            temp[(int)tp.Y, (int)tp.X] = c;
            if (c < mn) { mn = c; coldX = (int)tp.X; coldY = (int)tp.Y; }
            if (c > mx) { mx = c; hotX = (int)tp.X; hotY = (int)tp.Y; }
            sum += c;
        }
        json.Append("\"stats\":{");
        Num("min", mn); json.Append(',');
        Num("max", mx); json.Append(',');
        Num("avg", sum / pts.Length); json.Append(',');
        json.Append("\"minAt\":[" + coldX + "," + coldY + "],");
        json.Append("\"maxAt\":[" + hotX + "," + hotY + "],");
        Num("count", pts.Length);
        json.Append("},");

        // 内置测量标记（全部类型；矩形坐标 rect，多边形/折线附 points，点附 point，
        // Spot 的 max/min/avg 均为该点值，rect 为 1x1 外接框，供 UI 表格与框定位兼容）
        json.Append("\"markers\":[");
        int markerCount = 0;
        try
        {
            for (int i = 0; i < file.Markers.Count; i++)
            {
                IMarker m = file.Markers[i];
                if (markerCount > 0) json.Append(',');
                markerCount++;
                json.Append('{');
                Str("name", m.Name); json.Append(',');
                Str("type", m.Type.ToString()); json.Append(',');

                IMarkerSpot sp = m as IMarkerSpot;
                IMarkerRectangle rc = m as IMarkerRectangle;
                IMarkerEllipse el = m as IMarkerEllipse;
                IMarkerPolygon pg = m as IMarkerPolygon;
                IMarkerPolyline pl = m as IMarkerPolyline;

                if (sp != null)
                {
                    json.Append("\"point\":[" + sp.Point.X + "," + sp.Point.Y + "],");
                    json.Append("\"rect\":[" + sp.Point.X + "," + sp.Point.Y + ",1,1],");
                    double v = sp.Value - 273.15f;
                    Num("max", v); json.Append(',');
                    Num("min", v); json.Append(',');
                    Num("avg", v); json.Append(',');
                    json.Append("\"hotSpot\":[" + sp.Point.X + "," + sp.Point.Y + "],");
                    json.Append("\"coldSpot\":[" + sp.Point.X + "," + sp.Point.Y + "]");
                }
                else if (rc != null || el != null)
                {
                    Rectangle r = rc != null ? rc.Rectangle : el.Rectangle;
                    json.Append("\"rect\":[" + r.X + "," + r.Y + "," + r.Width + "," + r.Height + "],");
                    double maxV = (rc != null ? rc.Max : el.Max) - 273.15f;
                    double minV = (rc != null ? rc.Min : el.Min) - 273.15f;
                    double av = (rc != null ? rc.Avg : el.Avg) - 273.15f;
                    Point hot = rc != null ? rc.HotSpot : el.HotSpot;
                    Point cold = rc != null ? rc.ColdSpot : el.ColdSpot;
                    Num("max", maxV); json.Append(',');
                    Num("min", minV); json.Append(',');
                    Num("avg", av); json.Append(',');
                    json.Append("\"hotSpot\":[" + hot.X + "," + hot.Y + "],");
                    json.Append("\"coldSpot\":[" + cold.X + "," + cold.Y + "]");
                }
                else if (pg != null || pl != null)
                {
                    Point[] verts = pg != null ? pg.Points : pl.Points;
                    int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                    foreach (Point p in verts)
                    {
                        if (p.X < minX) minX = p.X;
                        if (p.X > maxX) maxX = p.X;
                        if (p.Y < minY) minY = p.Y;
                        if (p.Y > maxY) maxY = p.Y;
                    }
                    json.Append("\"points\":" + PointsJson(verts)); json.Append(',');
                    json.Append("\"rect\":[" + minX + "," + minY + ","
                        + Math.Max(1, maxX - minX + 1) + "," + Math.Max(1, maxY - minY + 1) + "],");
                    double maxV = (pg != null ? pg.Max : pl.Max) - 273.15f;
                    double minV = (pg != null ? pg.Min : pl.Min) - 273.15f;
                    double av = (pg != null ? pg.Avg : pl.Avg) - 273.15f;
                    Point hot = pg != null ? pg.HotSpot : pl.HotSpot;
                    Point cold = pg != null ? pg.ColdSpot : pl.ColdSpot;
                    Num("max", maxV); json.Append(',');
                    Num("min", minV); json.Append(',');
                    Num("avg", av); json.Append(',');
                    json.Append("\"hotSpot\":[" + hot.X + "," + hot.Y + "],");
                    json.Append("\"coldSpot\":[" + cold.X + "," + cold.Y + "]");
                }
                json.Append('}');
            }
        }
        catch (Exception) { /* 标记枚举失败时保留已收集项 */ }
        json.Append("],");

        // 降采样温度网格（4x4 块平均），供热图悬停读数
        int gw = width / 4, gh = height / 4;
        json.Append("\"grid\":{");
        Num("width", gw); json.Append(',');
        Num("height", gh); json.Append(',');
        json.Append("\"block\":4,\"data\":[");
        var gridRow = new StringBuilder();
        for (int gy = 0; gy < gh; gy++)
        {
            gridRow.Length = 0;
            for (int gx = 0; gx < gw; gx++)
            {
                if (gx > 0) gridRow.Append(',');
                double blockSum = 0;
                for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) blockSum += temp[gy * 4 + y, gx * 4 + x];
                gridRow.Append((blockSum / 16.0).ToString("F2", CultureInfo.InvariantCulture));
            }
            if (gy > 0) json.Append(',');
            json.Append(gridRow.ToString());
        }
        json.Append("]},");

        // AD 帧信息（先取一次帧，统计与时间戳共用）
        Frame frame = file.GetFrame();
        long adMin = 0, adMax = 0;
        double adAvg = 0;
        if (frame.Values != null)
        {
            ushort adMn = ushort.MaxValue, adMx = 0; double adSum = 0; long adCnt = 0;
            for (int y = 0; y < frame.Values.GetLength(0); y++)
                for (int x = 0; x < frame.Values.GetLength(1); x++)
                {
                    ushort v = frame.Values[y, x];
                    if (v < adMn) adMn = v;
                    if (v > adMx) adMx = v;
                    adSum += v; adCnt++;
                }
            adMin = adMn; adMax = adMx; adAvg = adSum / adCnt;
        }
        json.Append("\"frame\":{");
        Str("timestamp", frame.Values != null
            ? (frame.TimeStamp != null ? frame.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : "")
            : ""); json.Append(',');
        Num("adMin", adMin); json.Append(',');
        Num("adMax", adMax); json.Append(',');
        Num("adAvg", adAvg);
        json.Append("},");

        // 图像（base64 PNG）
        json.Append("\"images\":{");
        Str("thermal", TryS(() => PngBase64(file.GetThermalImage()))); json.Append(',');
        Str("palette", TryS(() => PngBase64(file.GetPaletteImage())));
        json.Append("}}");

        string outPath = Path.Combine(outDir, "result.json");
        File.WriteAllText(outPath, json.ToString(), new UTF8Encoding(false));
    }

    static string PngBase64(Image img)
    {
        if (img == null) return "";
        using (MemoryStream ms = new MemoryStream())
        {
            img.Save(ms, ImageFormat.Png);
            img.Dispose();
            return Convert.ToBase64String(ms.ToArray());
        }
    }

    static string PointsJson(Point[] points)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < points.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append("[" + points[i].X + "," + points[i].Y + "]");
        }
        return sb.Append(']').ToString();
    }

    static string TryS(Func<string> get) { try { return get() ?? ""; } catch (Exception) { return ""; } }
    static float TryF(Func<float> get) { try { return get(); } catch (Exception) { return 0; } }

    static void Str(string key, string value)
    {
        json.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
    }

    static void Num(string key, double value)
    {
        json.Append('"').Append(key).Append("\":");
        if (value == Math.Floor(value)) json.Append(((long)value).ToString(CultureInfo.InvariantCulture));
        else json.Append(value.ToString("F3", CultureInfo.InvariantCulture));
    }

    static string Escape(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
    }
}
