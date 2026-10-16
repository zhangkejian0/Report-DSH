using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

/// <summary>
/// 提取文件第一个 JPEG（相机内嵌热像渲染）并与 SDK 渲染对比
/// 用法: CompareEmbedded.exe 热像文件 sdk_thermal.png
/// </summary>
static class CompareEmbedded
{
    static void Main(string[] args)
    {
        byte[] u8 = File.ReadAllBytes(args[0]);
        // 第一个 JPEG: 从 0 开始到 EOI
        int eoi = -1;
        for (int k = 2; k + 1 < u8.Length; k++)
            if (u8[k] == 0xFF && u8[k + 1] == 0xD9) { eoi = k + 2; break; }
        var jpg = new byte[eoi];
        Array.Copy(u8, jpg, eoi);
        using (var emb = new Bitmap(new MemoryStream(jpg)))
        using (var sdk = new Bitmap(args[1]))
        {
            Console.WriteLine("embedded " + emb.Width + "x" + emb.Height + "  sdk " + sdk.Width + "x" + sdk.Height);
            double sum = 0; int max = 0, n = 0;
            for (int y = 0; y < Math.Min(emb.Height, sdk.Height); y += 2)
                for (int x = 0; x < Math.Min(emb.Width, sdk.Width); x += 2)
                {
                    var a = emb.GetPixel(x, y); var b = sdk.GetPixel(x, y);
                    int d = Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
                    sum += d; if (d > max) max = d; n++;
                }
            Console.WriteLine(string.Format("内嵌JPEG vs SDK渲染: 平均色差 {0:F2}  最大 {1}", sum / n, max));
        }
    }
}
