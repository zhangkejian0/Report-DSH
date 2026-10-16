using System;
using System.Drawing;
using System.IO;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

class ThermalMatrixDemo
{
    static void Main(string[] args)
    {
        string filePath = @"C:\Users\Zhangkejian\Downloads\Amur\public\IR1.jpg";
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine(string.Format("文件不存在: {0}", filePath));
            return;
        }
        
        var validator = IRtekFileSDKValidator.Instance;
        FileType fileType = validator.IsValidFile(filePath);
        
        Console.WriteLine(string.Format("文件类型: {0}", fileType));
        
        if (fileType == FileType.Other)
        {
            Console.WriteLine("不是有效的热像文件");
            return;
        }
        
        IThermalImageFile thermalFile = validator.Create();
        
        try
        {
            if (!thermalFile.Open(filePath))
            {
                Console.WriteLine("打开文件失败");
                return;
            }
            
            Console.WriteLine(string.Format("文件版本: {0}", thermalFile.FileVersion));
            
            Size thermalSize = thermalFile.GetThermalSize();
            Console.WriteLine(string.Format("热像尺寸: {0} x {1}", thermalSize.Width, thermalSize.Height));
            
            ThermalParams param = thermalFile.GetThermalParams();
            Console.WriteLine(string.Format("发射率: {0}", param.Emissivity));
            Console.WriteLine(string.Format("距离: {0}m", param.Distance));
            
            Frame frame = thermalFile.GetFrame();
            
            if (frame.Values != null)
            {
                int height = frame.Values.GetLength(0);
                int width = frame.Values.GetLength(1);
                
                Console.WriteLine("\n热像矩阵数据 (AD值):");
                Console.WriteLine(string.Format("矩阵尺寸: {0} x {1}", width, height));
                Console.WriteLine(string.Format("时间戳: {0}", frame.TimeStamp));
                
                ushort minAD = ushort.MaxValue;
                ushort maxAD = ushort.MinValue;
                double sumAD = 0;
                int count = 0;
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort adValue = frame.Values[y, x];
                        if (adValue < minAD) minAD = adValue;
                        if (adValue > maxAD) maxAD = adValue;
                        sumAD += adValue;
                        count++;
                    }
                }
                
                Console.WriteLine("\nAD值统计:");
                Console.WriteLine(string.Format("最小AD值: {0}", minAD));
                Console.WriteLine(string.Format("最大AD值: {0}", maxAD));
                Console.WriteLine(string.Format("平均AD值: {0:F2}", sumAD / count));
                
                Console.WriteLine("\n前10行前10列的AD值矩阵:");
                Console.Write("      ");
                for (int x = 0; x < Math.Min(10, width); x++)
                {
                    Console.Write(string.Format("{0,6}", x));
                }
                Console.WriteLine();
                
                for (int y = 0; y < Math.Min(10, height); y++)
                {
                    Console.Write(string.Format("{0,4}: ", y));
                    for (int x = 0; x < Math.Min(10, width); x++)
                    {
                        Console.Write(string.Format("{0,6}", frame.Values[y, x]));
                    }
                    Console.WriteLine();
                }
                
                Console.WriteLine("\n采样点温度:");
                Point[] samplePoints = new Point[]
                {
                    new Point(width / 2, height / 2),
                    new Point(0, 0),
                    new Point(width - 1, height - 1),
                    new Point(width / 4, height / 4),
                    new Point(width * 3 / 4, height * 3 / 4)
                };
                
                foreach (var pt in samplePoints)
                {
                    float tempK = thermalFile.GetSpotValue(pt);
                    float tempC = tempK - 273.15F;
                    Console.WriteLine(string.Format("  点({0}, {1}): {2:F2}°C (AD值: {3})", pt.X, pt.Y, tempC, frame.Values[pt.Y, pt.X]));
                }
                
                string outputPath = @"C:\Users\Zhangkejian\Downloads\Amur\public\thermal_matrix.csv";
                using (var writer = new StreamWriter(outputPath))
                {
                    writer.WriteLine("热像矩阵数据导出");
                    writer.WriteLine(string.Format("文件: {0}", filePath));
                    writer.WriteLine(string.Format("尺寸: {0} x {1}", width, height));
                    writer.WriteLine(string.Format("时间戳: {0}", frame.TimeStamp));
                    writer.WriteLine();
                    
                    for (int y = 0; y < height; y++)
                    {
                        var row = new string[width];
                        for (int x = 0; x < width; x++)
                        {
                            row[x] = frame.Values[y, x].ToString();
                        }
                        writer.WriteLine(string.Join(",", row));
                    }
                }
                Console.WriteLine(string.Format("\n完整矩阵数据已导出到: {0}", outputPath));
            }
            else
            {
                Console.WriteLine("无法获取帧数据");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("发生错误: {0}", ex.Message));
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            thermalFile.Close();
        }
    }
}
