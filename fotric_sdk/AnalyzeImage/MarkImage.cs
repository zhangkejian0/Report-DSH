using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 把 JSON 规格中的测量标记写入热像文件并另存副本（辐射数据保留，原文件不动）。
/// 用法: MarkImage.exe <src.jpg> <dst.jpg> <markers.json>
/// markers.json: {"markers":[
///   {"type":"rectangle","name":"区域1","rect":[x,y,w,h]},
///   {"type":"ellipse","name":"区域2","rect":[x,y,w,h]},
///   {"type":"polygon","name":"区域3","points":[[x,y],[x,y],...]},
///   {"type":"polyline","name":"线1","points":[[x,y],[x,y],...]},
///   {"type":"spot","name":"点1","point":[x,y]}]}
/// 坐标为热像文件像素空间；越界值收敛到图像范围内。
/// 成功: stdout 输出 {"ok":true,"dst":...,"file":{...},"markers":[回读统计]}，退出码 0。
/// 失败: stdout 打印 "ERROR <what> <message>"，退出码 2=非热像 3=打开失败 4=规格无效或异常 5=保存失败。
/// </summary>
static class MarkImage
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length < 3) { Console.WriteLine("ERROR usage <src> <dst> <markers.json>"); return 4; }
        string src = args[0], dst = args[1], specPath = args[2];
        try
        {
            var validator = IRtekFileSDKValidator.Instance;
            FileType fileType = validator.IsValidFile(src);
            if (fileType != FileType.ThermalPicture && fileType != FileType.GridThermalPicture)
            { Console.WriteLine("ERROR not-thermal " + fileType); return 2; }

            List<Dictionary<string, object>> specs = ParseSpec(specPath);

            var file = validator.Create();
            if (!file.Open(src)) { Console.WriteLine("ERROR open-failed"); return 3; }
            Size size = file.GetThermalSize();
            AddMarkers(file, specs, size.Width, size.Height);
            bool saved = file.SaveAs(dst);
            if (!saved) { Console.WriteLine("ERROR save-failed"); return 5; }
            file.Close();
            EmitReport(validator, dst, src, specs);
            return 0;
        }
        catch (SpecException ex)
        {
            Console.WriteLine("ERROR invalid-spec " + ex.Message.Replace('\n', ' ').Replace('\r', ' '));
            return 4;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR failed " + ex.Message.Replace('\n', ' ').Replace('\r', ' '));
            return 4;
        }
    }

    // ---------- 规格解析 ----------

    static List<Dictionary<string, object>> ParseSpec(string specPath)
    {
        if (!File.Exists(specPath)) throw new SpecException("spec file missing: " + specPath);
        object root = new JsonParser(File.ReadAllText(specPath, Encoding.UTF8)).Parse();
        var rootObj = root as Dictionary<string, object>;
        if (rootObj == null) throw new SpecException("spec root must be an object");
        object rawMarkers;
        if (!rootObj.TryGetValue("markers", out rawMarkers) || !(rawMarkers is List<object>))
            throw new SpecException("spec must carry a markers array");
        var list = (List<object>)rawMarkers;
        if (list.Count == 0) throw new SpecException("markers array must not be empty");
        var specs = new List<Dictionary<string, object>>();
        var seen = new HashSet<string>();
        foreach (object item in list)
        {
            var spec = item as Dictionary<string, object>;
            if (spec == null) throw new SpecException("each marker must be an object");
            string type = AsString(spec, "type");
            if (type != "rectangle" && type != "ellipse" && type != "polygon" && type != "polyline" && type != "spot")
                throw new SpecException("unknown marker type: " + type);
            string name = AsString(spec, "name");
            if (name.Length == 0) throw new SpecException("marker name must not be empty");
            if (!seen.Add(name)) throw new SpecException("duplicate marker name: " + name);
            specs.Add(spec);
        }
        return specs;
    }

    static string AsString(Dictionary<string, object> spec, string key)
    {
        object value;
        if (!spec.TryGetValue(key, out value) || !(value is string)) throw new SpecException(key + " must be a string");
        return (string)value;
    }

    static List<double> AsNumberList(object value, int expect, string what)
    {
        var list = value as List<object>;
        if (list == null || list.Count != expect) throw new SpecException(what + " must hold " + expect + " numbers");
        var numbers = new List<double>(expect);
        foreach (object item in list)
        {
            if (!(item is double)) throw new SpecException(what + " must hold numbers");
            numbers.Add((double)item);
        }
        return numbers;
    }

    static Point AsPoint(object value, string what)
    {
        var n = AsNumberList(value, 2, what);
        return new Point((int)Math.Round(n[0]), (int)Math.Round(n[1]));
    }

    static Rectangle AsRectangle(Dictionary<string, object> spec, int width, int height)
    {
        object raw;
        if (!spec.TryGetValue("rect", out raw)) throw new SpecException("rect is required for " + AsString(spec, "type"));
        var n = AsNumberList(raw, 4, "rect");
        int x = Clamp((int)Math.Round(n[0]), 0, width - 1);
        int y = Clamp((int)Math.Round(n[1]), 0, height - 1);
        int w = Clamp((int)Math.Round(n[2]), 1, width - x);
        int h = Clamp((int)Math.Round(n[3]), 1, height - y);
        return new Rectangle(x, y, w, h);
    }

    static Point[] AsPoints(Dictionary<string, object> spec, int minCount, int width, int height)
    {
        object raw;
        if (!spec.TryGetValue("points", out raw) || !(raw is List<object>))
            throw new SpecException("points is required for " + AsString(spec, "type"));
        var list = (List<object>)raw;
        if (list.Count < minCount) throw new SpecException("points needs at least " + minCount + " vertices");
        var points = new Point[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            Point p = AsPoint(list[i], "points[" + i + "]");
            points[i] = new Point(Clamp(p.X, 0, width - 1), Clamp(p.Y, 0, height - 1));
        }
        return points;
    }

    static int Clamp(int value, int low, int high)
    {
        return value < low ? low : (value > high ? high : value);
    }

    static void AddMarkers(IThermalImageFile file, List<Dictionary<string, object>> specs, int width, int height)
    {
        foreach (var spec in specs)
        {
            string type = (string)spec["type"];
            string name = (string)spec["name"];
            var local = new LocalThermalParams();
            switch (type)
            {
                case "rectangle":
                    file.Markers.Add(file.CreateMarkerRectangle(name, local, AsRectangle(spec, width, height)));
                    break;
                case "ellipse":
                    file.Markers.Add(file.CreateMarkerEllipse(name, local, AsRectangle(spec, width, height)));
                    break;
                case "polygon":
                    file.Markers.Add(file.CreateMarkerPolygon(name, local, AsPoints(spec, 3, width, height)));
                    break;
                case "polyline":
                    file.Markers.Add(file.CreateMarkerPolyline(name, local, AsPoints(spec, 2, width, height)));
                    break;
                case "spot":
                    {
                        object raw;
                        if (!spec.TryGetValue("point", out raw)) throw new SpecException("point is required for spot");
                        Point p = AsPoint(raw, "point");
                        file.Markers.Add(file.CreateMarkerSpot(name, local,
                            new Point(Clamp(p.X, 0, width - 1), Clamp(p.Y, 0, height - 1))));
                        break;
                    }
            }
        }
    }

    // ---------- 回读报告 ----------

    static void EmitReport(IRtekFileSDKValidator validator, string dst, string src, List<Dictionary<string, object>> specs)
    {
        var names = new HashSet<string>();
        foreach (var spec in specs) names.Add((string)spec["name"]);

        var file = validator.Create();
        try
        {
            if (!file.Open(dst)) { Console.WriteLine("ERROR reopen-failed"); Environment.Exit(3); }
            Size size = file.GetThermalSize();
            var json = new StringBuilder();
            json.Append("{\"ok\":true,");
            Str(json, "dst", dst); json.Append(',');
            Str(json, "src", src); json.Append(',');
            json.Append("\"file\":{");
            Num(json, "width", size.Width); json.Append(',');
            Num(json, "height", size.Height);
            json.Append("},\"markers\":[");
            int emitted = 0;
            for (int i = 0; i < file.Markers.Count; i++)
            {
                IMarker m = file.Markers[i];
                if (!names.Contains(m.Name)) continue;
                if (emitted > 0) json.Append(',');
                emitted++;
                json.Append('{');
                Str(json, "name", m.Name); json.Append(',');
                Str(json, "type", m.Type.ToString());
                var spot = m as IMarkerSpot;
                if (spot != null)
                {
                    json.Append(',');
                    json.Append("\"point\":[" + spot.Point.X + "," + spot.Point.Y + "],");
                    Num(json, "value", spot.Value - 273.15f);
                }
                var rect = m as IMarkerRectangle;
                if (rect != null)
                {
                    json.Append(',');
                    json.Append("\"rect\":[" + rect.Rectangle.X + "," + rect.Rectangle.Y + ","
                        + rect.Rectangle.Width + "," + rect.Rectangle.Height + "],");
                    Num(json, "max", rect.Max - 273.15f); json.Append(',');
                    Num(json, "min", rect.Min - 273.15f); json.Append(',');
                    Num(json, "avg", rect.Avg - 273.15f); json.Append(',');
                    json.Append("\"hotSpot\":[" + rect.HotSpot.X + "," + rect.HotSpot.Y + "],");
                    json.Append("\"coldSpot\":[" + rect.ColdSpot.X + "," + rect.ColdSpot.Y + "]");
                }
                var ellipse = m as IMarkerEllipse;
                if (ellipse != null)
                {
                    json.Append(',');
                    json.Append("\"rect\":[" + ellipse.Rectangle.X + "," + ellipse.Rectangle.Y + ","
                        + ellipse.Rectangle.Width + "," + ellipse.Rectangle.Height + "],");
                    Num(json, "max", ellipse.Max - 273.15f); json.Append(',');
                    Num(json, "min", ellipse.Min - 273.15f); json.Append(',');
                    Num(json, "avg", ellipse.Avg - 273.15f); json.Append(',');
                    json.Append("\"hotSpot\":[" + ellipse.HotSpot.X + "," + ellipse.HotSpot.Y + "],");
                    json.Append("\"coldSpot\":[" + ellipse.ColdSpot.X + "," + ellipse.ColdSpot.Y + "]");
                }
                var polygon = m as IMarkerPolygon;
                if (polygon != null)
                {
                    json.Append(',');
                    json.Append("\"points\":" + PointsJson(polygon.Points)); json.Append(',');
                    Num(json, "max", polygon.Max - 273.15f); json.Append(',');
                    Num(json, "min", polygon.Min - 273.15f); json.Append(',');
                    Num(json, "avg", polygon.Avg - 273.15f); json.Append(',');
                    json.Append("\"hotSpot\":[" + polygon.HotSpot.X + "," + polygon.HotSpot.Y + "],");
                    json.Append("\"coldSpot\":[" + polygon.ColdSpot.X + "," + polygon.ColdSpot.Y + "]");
                }
                var polyline = m as IMarkerPolyline;
                if (polyline != null)
                {
                    json.Append(',');
                    json.Append("\"points\":" + PointsJson(polyline.Points)); json.Append(',');
                    Num(json, "max", polyline.Max - 273.15f); json.Append(',');
                    Num(json, "min", polyline.Min - 273.15f); json.Append(',');
                    Num(json, "avg", polyline.Avg - 273.15f); json.Append(',');
                    json.Append("\"hotSpot\":[" + polyline.HotSpot.X + "," + polyline.HotSpot.Y + "],");
                    json.Append("\"coldSpot\":[" + polyline.ColdSpot.X + "," + polyline.ColdSpot.Y + "]");
                }
                json.Append('}');
            }
            json.Append("]}");
            Console.WriteLine(json.ToString());
        }
        finally { file.Close(); }
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

    static void Str(StringBuilder json, string key, string value)
    {
        json.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
    }

    static void Num(StringBuilder json, string key, double value)
    {
        json.Append('"').Append(key).Append("\":");
        if (value == Math.Floor(value)) json.Append(((long)value).ToString(CultureInfo.InvariantCulture));
        else json.Append(value.ToString("F2", CultureInfo.InvariantCulture));
    }

    static string Escape(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
    }
}
