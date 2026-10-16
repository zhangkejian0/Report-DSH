using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

/// <summary>
/// 按 JSON 编辑指令修改热像文件并另存副本（辐射数据保留，原文件不动）：
/// 测温参数（发射率/距离/反射温度/环境温度/湿度）、温宽、调色板、旋转、镜像、文字备注。
/// 用法: EditImage.exe <src.jpg> <dst.jpg> <edit.json>
/// edit.json（各节均可选，至少一项）:
/// {"params":{"emissivity":0.9,"distance":3.5,"reflTemp":25.0,"atmTemp":22.0,"relHumidity":0.35},
///  "gain":{"mode":"Fixed","min":18.0,"max":28.0},
///  "palette":{"refNo":1,"inverted":false},
///  "rotate":90, "mirror":"horizontal", "note":"检测备注"}
/// 温度一律摄氏度输入。成功: stdout 输出回读摘要 JSON，退出码 0。
/// 失败: stdout 打印 "ERROR <what> <message>"，退出码 2=非热像 3=打开失败 4=指令无效或异常 5=保存失败。
/// </summary>
static class EditImage
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length < 3) { Console.WriteLine("ERROR usage <src> <dst> <edit.json>"); return 4; }
        string src = args[0], dst = args[1], editPath = args[2];
        try
        {
            var validator = IRtekFileSDKValidator.Instance;
            FileType fileType = validator.IsValidFile(src);
            if (fileType != FileType.ThermalPicture && fileType != FileType.GridThermalPicture)
            { Console.WriteLine("ERROR not-thermal " + fileType); return 2; }

            var spec = ParseEdit(editPath);

            var file = validator.Create();
            if (!file.Open(src)) { Console.WriteLine("ERROR open-failed"); return 3; }
            Apply(file, spec);
            bool saved = file.SaveAs(dst);
            if (!saved) { Console.WriteLine("ERROR save-failed"); return 5; }
            file.Close();
            EmitReport(validator, dst, src);
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

    /// <summary>一份编辑指令：各节独立可选。</summary>
    sealed class EditSpec
    {
        public double? Emissivity, Distance, ReflTemp, AtmTemp, RelHumidity;
        public string GainMode; public double? GainMin, GainMax;
        public int? PaletteRefNo; public bool? PaletteInverted;
        public int? Rotate; public string Mirror; public string Note;
    }

    static EditSpec ParseEdit(string editPath)
    {
        if (!File.Exists(editPath)) throw new SpecException("edit file missing: " + editPath);
        var root = new JsonParser(File.ReadAllText(editPath, Encoding.UTF8)).Parse() as Dictionary<string, object>;
        if (root == null) throw new SpecException("edit root must be an object");
        var spec = new EditSpec();
        int applied = 0;

        object rawParams;
        if (root.TryGetValue("params", out rawParams) && rawParams is Dictionary<string, object>)
        {
            var p = (Dictionary<string, object>)rawParams;
            spec.Emissivity = OptNumber(p, "emissivity", "params.emissivity");
            spec.Distance = OptNumber(p, "distance", "params.distance");
            spec.ReflTemp = OptNumber(p, "reflTemp", "params.reflTemp");
            spec.AtmTemp = OptNumber(p, "atmTemp", "params.atmTemp");
            spec.RelHumidity = OptNumber(p, "relHumidity", "params.relHumidity");
            applied++;
        }
        object rawGain;
        if (root.TryGetValue("gain", out rawGain) && rawGain is Dictionary<string, object>)
        {
            var g = (Dictionary<string, object>)rawGain;
            object mode;
            g.TryGetValue("mode", out mode);
            spec.GainMode = mode as string;
            spec.GainMin = OptNumber(g, "min", "gain.min");
            spec.GainMax = OptNumber(g, "max", "gain.max");
            applied++;
        }
        object rawPalette;
        if (root.TryGetValue("palette", out rawPalette) && rawPalette is Dictionary<string, object>)
        {
            var pal = (Dictionary<string, object>)rawPalette;
            spec.PaletteRefNo = (int?)OptNumber(pal, "refNo", "palette.refNo");
            object inverted;
            pal.TryGetValue("inverted", out inverted);
            if (inverted is bool) spec.PaletteInverted = (bool)inverted;
            applied++;
        }
        object rawRotate;
        root.TryGetValue("rotate", out rawRotate);
        if (rawRotate is double)
        {
            int r = (int)(double)rawRotate;
            if (r != 90 && r != 180 && r != 270) throw new SpecException("rotate must be 90, 180, or 270");
            spec.Rotate = r;
            applied++;
        }
        object rawMirror;
        root.TryGetValue("mirror", out rawMirror);
        if (rawMirror is string)
        {
            string m = (string)rawMirror;
            if (m != "horizontal" && m != "vertical") throw new SpecException("mirror must be horizontal or vertical");
            spec.Mirror = m;
            applied++;
        }
        object rawNote;
        root.TryGetValue("note", out rawNote);
        if (rawNote is string) { spec.Note = (string)rawNote; applied++; }

        if (applied == 0) throw new SpecException("edit must carry at least one of params/gain/palette/rotate/mirror/note");
        return spec;
    }

    static double? OptNumber(Dictionary<string, object> section, string key, string what)
    {
        object value;
        if (!section.TryGetValue(key, out value)) return null;
        if (!(value is double)) throw new SpecException(what + " must be a number");
        return (double)value;
    }

    static void Apply(IThermalImageFile file, EditSpec spec)
    {
        if (spec.Emissivity.HasValue || spec.Distance.HasValue || spec.ReflTemp.HasValue
            || spec.AtmTemp.HasValue || spec.RelHumidity.HasValue)
        {
            var p = file.GetThermalParams();
            if (spec.Emissivity.HasValue) p.Emissivity = ClampF((float)spec.Emissivity, 0.01f, 1.0f);
            if (spec.Distance.HasValue) p.Distance = ClampF((float)spec.Distance, 0.01f, 50000f);
            if (spec.ReflTemp.HasValue) p.ReflTemp = (float)spec.ReflTemp + 273.15f;
            if (spec.AtmTemp.HasValue) p.AtmTemp = (float)spec.AtmTemp + 273.15f;
            if (spec.RelHumidity.HasValue) p.RelHumidity = ClampF((float)spec.RelHumidity, 0.01f, 1.0f);
            Console.Error.WriteLine("SetThermalParams: " + file.SetThermalParams(p));
        }
        if (spec.GainMin.HasValue || spec.GainMax.HasValue || spec.GainMode != null)
        {
            var g = file.GetGain();
            if (spec.GainMode == "Fixed") g.Mode = GainMode.Fixed;
            if (spec.GainMode == "Auto") g.Mode = GainMode.Auto;
            if (spec.GainMin.HasValue) g.Min = (float)spec.GainMin + 273.15f;
            if (spec.GainMax.HasValue) g.Max = (float)spec.GainMax + 273.15f;
            Console.Error.WriteLine("SetGain: " + file.SetGain(g));
        }
        if (spec.PaletteRefNo.HasValue || spec.PaletteInverted.HasValue)
        {
            var pal = file.GetPalette();
            if (spec.PaletteRefNo.HasValue) pal.RefNo = (byte)spec.PaletteRefNo;
            if (spec.PaletteInverted.HasValue) pal.Inverted = (bool)spec.PaletteInverted;
            Console.Error.WriteLine("SetPalette: " + file.SetPalette(pal));
        }
        if (spec.Rotate == 90) file.Rotate(RotateType.Rotate90);
        if (spec.Rotate == 180) file.Rotate(RotateType.Rotate180);
        if (spec.Rotate == 270) file.Rotate(RotateType.Rotate270);
        if (spec.Mirror == "horizontal") file.Mirror(MirrorType.MirrorHorizontal);
        if (spec.Mirror == "vertical") file.Mirror(MirrorType.MirrorVertical);
        if (spec.Note != null) Console.Error.WriteLine("SetNoteAnnotation: " + file.SetNoteAnnotation(spec.Note));
    }

    static float ClampF(float value, float low, float high)
    {
        return value < low ? low : (value > high ? high : value);
    }

    static void EmitReport(IRtekFileSDKValidator validator, string dst, string src)
    {
        var file = validator.Create();
        try
        {
            if (!file.Open(dst)) { Console.WriteLine("ERROR reopen-failed"); Environment.Exit(3); }
            Size size = file.GetThermalSize();
            var json = new StringBuilder();
            json.Append("{\"ok\":true,");
            Str(json, "dst", dst); json.Append(',');
            Str(json, "src", src); json.Append(',');

            ThermalParams p = file.GetThermalParams();
            json.Append("\"params\":{");
            Num(json, "emissivity", p.Emissivity); json.Append(',');
            Num(json, "distance", p.Distance); json.Append(',');
            Num(json, "reflTemp", p.ReflTemp - 273.15f); json.Append(',');
            Num(json, "atmTemp", p.AtmTemp - 273.15f); json.Append(',');
            Num(json, "relHumidity", p.RelHumidity);
            json.Append("},");

            Gain g = file.GetGain();
            json.Append("\"gain\":{");
            Str(json, "mode", g.Mode.ToString()); json.Append(',');
            Num(json, "min", g.Min - 273.15f); json.Append(',');
            Num(json, "max", g.Max - 273.15f);
            json.Append("},");

            Palette pal = file.GetPalette();
            json.Append("\"palette\":{");
            Num(json, "refNo", pal.RefNo); json.Append(',');
            json.Append("\"inverted\":" + (pal.Inverted ? "true" : "false"));
            json.Append("},");

            Str(json, "note", TryS(() => file.GetNoteAnnotation())); json.Append(',');
            json.Append("\"file\":{");
            Num(json, "width", size.Width); json.Append(',');
            Num(json, "height", size.Height);
            json.Append("}}");
            Console.WriteLine(json.ToString());
        }
        finally { file.Close(); }
    }

    static string TryS(Func<string> get) { try { return get() ?? ""; } catch (Exception) { return ""; } }

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
