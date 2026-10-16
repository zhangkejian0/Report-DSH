using System;
using System.Drawing;
using System.IO;
using System.Text;
using Amur.IRtekACFileSDK;
using AcFileType = Amur.IRtekACFileSDK.FileType;

/// <summary>
/// 从声像文件导出图表种子数据（频谱FFT / PRPD散点 / 声压统计）为 JSON，
/// 供 report-editor 前端 ECharts 图表卡片使用。
/// </summary>
public static class DumpChartData
{
    public static int Main(string[] args)
    {
        string file = args.Length > 0
            ? args[0]
            : @"D:\develop\report-editor\canvas-editor\IRAC\20260121\FOTRIC_2026-01-21_0009.jpg";
        string outFile = args.Length > 1
            ? args[1]
            : @"D:\develop\report-editor\canvas-editor\src\report\chart-seed-acoustic.json";

        var validator = IRtekACFileSDKValidator.Instance;
        AcFileType type;
        if (!validator.IsValidFile(file, out type))
        {
            Console.WriteLine("[失败] 非声像文件: " + file);
            return 1;
        }
        var img = validator.Create();
        if (!img.Open(file))
        {
            Console.WriteLine("[失败] Open 失败");
            return 1;
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");

        // 设备与参数
        Size size = img.GetAcousticSize();
        sb.AppendLine(string.Format("  \"source\": \"{0}\",", Path.GetFileName(file)));
        sb.AppendLine(string.Format("  \"camera\": \"{0}\",", img.GetCameraInfo().Model.Replace("\"", "")));
        sb.AppendLine(string.Format("  \"shotTime\": \"{0:yyyy-MM-dd HH:mm:ss}\",", img.GetShotTime()));
        sb.AppendLine(string.Format("  \"acousticSize\": \"{0} x {1}\",", size.Width, size.Height));
        Band band = img.GetFrequencyRange();
        sb.AppendLine(string.Format("  \"band\": [{0:F0}, {1:F0}],", band.Min, band.Max));

        // 帧：频谱FFT + PRPD + 声压网格统计
        Amur.IRtekACFileSDK.Frame frame = img.GetFrame(0);
        var fft = frame.Spectrum.FFT;
        double delta = frame.Spectrum.Delta;
        sb.AppendLine("  \"fftDelta\": " + delta.ToString("F2") + ",");
        sb.Append("  \"fft\": [");
        for (int i = 0; i < fft.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(fft[i].ToString("F4"));
        }
        sb.AppendLine("],");

        var prpd = frame.PRPD;
        sb.Append("  \"prpd\": [");
        for (int i = 0; i < prpd.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(string.Format("[{0:F4},{1:F4}]", prpd[i].X, prpd[i].Y));
        }
        sb.AppendLine("]");

        sb.AppendLine("}");
        File.WriteAllText(outFile, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine("FFT点数: " + fft.Length + "  PRPD点数: " + prpd.Length);
        Console.WriteLine("已导出: " + outFile);
        img.Close();
        return 0;
    }
}
