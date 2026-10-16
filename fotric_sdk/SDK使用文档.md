# IRtek 热像/声像文件SDK使用文档

## 目录

1. [概述](#1-概述)
2. [环境要求](#2-环境要求)
3. [项目结构](#3-项目结构)
4. [快速开始](#4-快速开始)
5. [热像文件SDK (IRtekFileSDK)](#5-热像文件sdk-irtekfilesdk)
6. [声像文件SDK (IRtekACFileSDK)](#6-声像文件sdk-irtekacfilesdk)
7. [公共类型定义](#7-公共类型定义)
8. [事件处理](#8-事件处理)
9. [错误处理](#9-错误处理)
10. [完整示例](#10-完整示例)

---

## 1. 概述

IRtek SDK 是一套用于处理红外热像文件和声像文件的开发工具包，提供以下功能：

- **热像文件处理**：读取、创建、保存热像图片和视频文件
- **声像文件处理**：读取、创建、保存声像图片和视频文件
- **温度测量**：点温、区域测温、线温等多种测量方式
- **声压级测量**：点测量、椭圆区域测量
- **图像处理**：调色板管理、图像旋转镜像、可见光融合
- **参数设置**：发射率、距离、环境温度等测温参数

---

## 2. 环境要求

### 2.1 开发环境

| 项目 | 要求 |
|------|------|
| 框架版本 | .NET Framework 4.5 或更高 |
| 开发工具 | Visual Studio 2015 或更高 |
| 语言 | C# |
| 平台 | Windows x86 / x64 |

### 2.2 依赖文件

**热像SDK依赖：**
- `IRtekFileSDK.dll` - 热像文件处理核心库
- `Amur.IRtekFileSDK.dll` - .NET托管封装库
- `Amur.ThermalImageFile.dll` - 公共接口库

**声像SDK依赖：**
- `IRtekACFileSDK.dll` - 声像文件处理核心库
- `AcousticUtils.dll` - 声像图像处理工具库
- `Amur.IRtekACFileSDK.dll` - .NET托管封装库
- `Amur.ThermalImageFile.dll` - 公共接口库

### 2.3 引用配置

在项目中添加引用：

```xml
<ItemGroup>
  <Reference Include="Amur.ThermalImageFile" />
  <Reference Include="Amur.IRtekFileSDK" />      <!-- 热像SDK -->
  <Reference Include="Amur.IRtekACFileSDK" />    <!-- 声像SDK -->
  <Reference Include="System.Drawing" />
</ItemGroup>
```

---

## 3. 项目结构

```
Amur/
├── Amur.ThermalImageFile/           # 公共接口和类型定义
│   ├── Interfaces/
│   │   ├── IThermalImageFile.cs     # 热像文件主接口
│   │   ├── IMarker.cs               # 测量标识接口
│   │   └── ...
│   └── Commons/
│       └── Commons.cs               # 枚举、结构体定义
│
├── Amur.IRtekFileSDK/               # 热像文件SDK
│   ├── ThermalImageFile.cs          # 热像文件主类
│   ├── ThermalImageFile.Markers.cs  # 测量标识实现
│   ├── ThermalImageFile.Deltas.cs   # 温差标记实现
│   └── IRtekFileSDK.cs              # 原生API声明
│
└── Amur.IRtekACFileSDK/             # 声像文件SDK
    ├── AcousticImageFile.cs         # 声像文件主类
    ├── AcousticImageFile.Markers.cs # 测量标识实现
    ├── AcousticImageUtils.cs        # 声像处理工具
    └── IRtekACFileSDK.cs            # 原生API声明
```

---

## 4. 快速开始

### 4.1 热像文件基本操作

```csharp
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

// 验证并创建热像文件对象
var validator = IRtekFileSDKValidator.Instance;
FileType fileType = validator.IsValidFile("thermal.jpg");

if (fileType != FileType.Other)
{
    IThermalImageFile thermalFile = validator.Create();
    
    // 打开文件
    if (thermalFile.Open("thermal.jpg"))
    {
        // 获取某点温度
        float temp = thermalFile.GetSpotValue(new Point(100, 100));
        Console.WriteLine($"温度: {temp - 273.15:F1}°C");
        
        // 关闭文件
        thermalFile.Close();
    }
}
```

### 4.2 声像文件基本操作

```csharp
using Amur.IRtekACFileSDK;

// 创建声像文件对象
var acousticFile = new AcousticImageFile();

// 打开文件
if (acousticFile.Open("acoustic.ac"))
{
    // 获取声像图
    Bitmap acousticImage = acousticFile.GetAcousticImage();
    
    // 获取声压级参数
    AcousticParams param = acousticFile.GetAcousticParams();
    
    // 关闭文件
    acousticFile.Close();
}
```

---

## 5. 热像文件SDK (IRtekFileSDK)

### 5.1 文件操作

#### 5.1.1 打开文件

```csharp
// 方式一：使用验证器（推荐）
var validator = IRtekFileSDKValidator.Instance;
FileType fileType = validator.IsValidFile("test.jpg");

if (fileType == FileType.ThermalPicture || fileType == FileType.ThermalVideo)
{
    IThermalImageFile thermalFile = validator.Create();
    if (thermalFile.Open("test.jpg"))
    {
        // 处理文件...
    }
}

// 方式二：直接创建实例
IThermalImageFile thermalFile = new ThermalImageFile();
if (thermalFile.Open("test.jpg"))
{
    // 处理文件...
}
```

#### 5.1.2 创建新文件

```csharp
IThermalImageFile thermalFile = new ThermalImageFile();

// 创建热像图片文件
if (thermalFile.Create("new_thermal.jpg", FileType.ThermalPicture))
{
    // 设置热像尺寸
    thermalFile.SetThermalSize(new Size(640, 480));
    
    // 设置帧数据
    Frame frame = new Frame(DateTime.Now, new ushort[640 * 480]);
    thermalFile.AppendRawFrame(frame);
    
    // 保存文件
    thermalFile.Save();
    thermalFile.Close();
}
```

#### 5.1.3 保存文件

```csharp
// 保存到原路径
thermalFile.Save();

// 另存为
thermalFile.SaveAs("new_file.jpg");

// 保存当前帧为新文件
thermalFile.SaveFrameAs("frame.jpg", coverImage);
```

#### 5.1.4 关闭文件

```csharp
thermalFile.Close();
```

### 5.2 温度数据获取

#### 5.2.1 获取单点温度

```csharp
Point point = new Point(100, 100);
float tempKelvin = thermalFile.GetSpotValue(point);
float tempCelsius = tempKelvin - 273.15F;

// 使用局部测温参数
LocalThermalParams localParams = new LocalThermalParams
{
    LocalParams = true,
    Emissivity = 0.95F,
    Distance = 2.0F,
    ReflTemp = 293.15F
};
float temp = thermalFile.GetSpotValue(point, localParams);
```

#### 5.2.2 获取矩形区域温度

```csharp
Rectangle rect = new Rectangle(50, 50, 100, 100);
TemperaturePoint[] points = thermalFile.GetRectangleValues(rect);

// 计算最高、最低、平均温度
float maxTemp = points.Max(p => p.Value);
float minTemp = points.Min(p => p.Value);
float avgTemp = points.Average(p => p.Value);
```

#### 5.2.3 获取椭圆区域温度

```csharp
Rectangle ellipseRect = new Rectangle(50, 50, 100, 80);
TemperaturePoint[] points = thermalFile.GetEllipseValues(ellipseRect);
```

#### 5.2.4 获取多边形区域温度

```csharp
Point[] polygonPoints = new Point[]
{
    new Point(100, 50),
    new Point(150, 100),
    new Point(100, 150),
    new Point(50, 100)
};
TemperaturePoint[] points = thermalFile.GetPolygonValues(polygonPoints);
```

#### 5.2.5 获取折线温度

```csharp
Point[] linePoints = new Point[]
{
    new Point(0, 100),
    new Point(200, 100),
    new Point(400, 150)
};
TemperaturePoint[] points = thermalFile.GetPolylineValues(linePoints);
```

### 5.3 测温参数设置

#### 5.3.1 获取/设置全局测温参数

```csharp
// 获取当前测温参数
ThermalParams param = thermalFile.GetThermalParams();
Console.WriteLine($"发射率: {param.Emissivity}");
Console.WriteLine($"距离: {param.Distance}m");
Console.WriteLine($"环境温度: {param.AtmTemp - 273.15}°C");
Console.WriteLine($"反射温度: {param.ReflTemp - 273.15}°C");

// 设置新的测温参数
ThermalParams newParam = new ThermalParams(
    reflTemp: 293.15F,      // 反射温度 (K)
    atmTemp: 298.15F,       // 大气温度 (K)
    distance: 5.0F,         // 目标距离 (m)
    relHumidity: 0.6F,      // 相对湿度 (0~1)
    emissivity: 0.95F,      // 发射率 (0.01~1)
    extOptTemp: 293.15F,    // 外部光学温度 (K)
    extOptTrans: 1.0F       // 外部光学透过率 (0~1)
);
thermalFile.SetThermalParams(newParam);
```

#### 5.3.2 测温参数说明

| 参数 | 类型 | 范围 | 默认值 | 说明 |
|------|------|------|--------|------|
| ReflTemp | float | 0~5750K | 293.15K | 反射温度 |
| AtmTemp | float | 0~5750K | 293.15K | 大气温度 |
| Distance | float | 0~50000m | 1.0m | 目标距离 |
| RelHumidity | float | 0.01~1.0 | 0.5 | 相对湿度 |
| Emissivity | float | 0.01~1.0 | 1.0 | 发射率 |
| ExtOptTemp | float | - | 293.15K | 外部光学温度 |
| ExtOptTrans | float | 0~1.0 | 1.0 | 外部光学透过率 |

### 5.4 测量标识 (Markers)

#### 5.4.1 添加点标识

```csharp
// 创建点标识
IMarkerSpot spot = thermalFile.CreateSpot(new Point(100, 100));
spot.Name = "测温点1";

// 设置局部测温参数
spot.ThermalParams = new LocalThermalParams
{
    LocalParams = true,
    Emissivity = 0.9F
};

// 添加到文件
thermalFile.Markers.Add(spot);

// 获取温度值
float temp = spot.Value;
```

#### 5.4.2 添加矩形标识

```csharp
IMarkerRectangle rect = thermalFile.CreateRectangle(new Rectangle(50, 50, 100, 100));
rect.Name = "区域1";
thermalFile.Markers.Add(rect);

// 获取测量结果
float max = rect.Max;        // 最高温度
float min = rect.Min;        // 最低温度
float avg = rect.Avg;        // 平均温度
Point hotSpot = rect.HotSpot; // 热点位置
Point coldSpot = rect.ColdSpot; // 冷点位置
float area = rect.Area;      // 面积
```

#### 5.4.3 添加椭圆标识

```csharp
IMarkerEllipse ellipse = thermalFile.CreateEllipse(new Rectangle(50, 50, 100, 80));
ellipse.Name = "椭圆区域1";
thermalFile.Markers.Add(ellipse);
```

#### 5.4.4 添加多边形标识

```csharp
Point[] points = new Point[]
{
    new Point(100, 50),
    new Point(150, 100),
    new Point(100, 150),
    new Point(50, 100)
};
IMarkerPolygon polygon = thermalFile.CreatePolygon(points);
polygon.Name = "多边形区域1";
thermalFile.Markers.Add(polygon);
```

#### 5.4.5 添加折线标识

```csharp
Point[] points = new Point[]
{
    new Point(0, 100),
    new Point(200, 100)
};
IMarkerPolyline polyline = thermalFile.CreatePolyline(points);
polyline.Name = "线温1";
thermalFile.Markers.Add(polyline);

float length = polyline.Length; // 线长
```

#### 5.4.6 管理测量标识

```csharp
// 获取标识数量
int count = thermalFile.Markers.Count;

// 按名称获取标识
IMarker marker = thermalFile.Markers["测温点1"];

// 按索引获取标识
IMarker marker = thermalFile.Markers[0];

// 移除标识
thermalFile.Markers.Remove("测温点1");
thermalFile.Markers.RemoveAt(0);

// 清除所有用户标识
thermalFile.Markers.Clear(ClearType.UserMarkers);
```

### 5.5 温差标记 (Deltas)

```csharp
// 创建温差标记
IDelta delta = thermalFile.CreateDelta();
delta.Name = "温差1";
delta.SrcName = "测温点1";       // 源标识名称
delta.SrcValueType = ValueType.Max;  // 源值类型
delta.DestName = "区域1";        // 目标标识名称
delta.DestValueType = ValueType.Avg; // 目标值类型

thermalFile.Deltas.Add(delta);

// 获取温差值
DeltaParams param = delta.GetDeltaParams();
float deltaTemp = param.SrcTemp - param.DestTemp;
```

### 5.6 温宽设置 (Gain)

```csharp
// 获取当前温宽
Gain gain = thermalFile.GetGain();

// 设置温宽
Gain newGain = new Gain(
    mode: GainMode.Fixed,    // 模式：Fixed/Auto/Touch
    max: 323.15F,            // 最高温度 (K)
    min: 273.15F,            // 最低温度 (K)
    fixedSpan: false,        // 是否固定温宽
    span: 20.0F              // 温宽值 (K)
);
thermalFile.SetGain(newGain);
```

### 5.7 调色板设置

```csharp
// 获取当前调色板
Palette palette = thermalFile.GetPalette();

// 设置预设调色板
Palette newPalette = new Palette(
    refNo: 1,           // 调色板编号
    colors: null,       // 自定义颜色（null使用预设）
    inverted: false     // 是否反转
);
thermalFile.SetPalette(newPalette);

// 设置自定义调色板
uint[] customColors = new uint[256];
for (int i = 0; i < 256; i++)
{
    customColors[i] = GetCustomColor(i);
}
Palette customPalette = new Palette(0xFF, customColors, false);
thermalFile.SetPalette(customPalette);

// 获取调色板图像
Image paletteImage = thermalFile.GetPaletteImage();
```

### 5.8 报警设置

```csharp
// 获取当前报警设置
Alarm alarm = thermalFile.GetAlarm();

// 设置报警
Alarm newAlarm = new Alarm(
    type: AlarmType.Above,    // 报警类型
    custom: true,             // 是否自定义颜色
    color: Color.Red,         // 报警颜色
    max: 353.15F,             // 上限温度 (K)
    min: 273.15F,             // 下限温度 (K)
    rhLimit: 1.0F,            // 湿度限制
    indoorTemp: 293.15F,      // 室内温度
    outdoorTemp: 283.15F,     // 室外温度
    heatIndex: 293.15F,       // 热指数
    threshold: 293.15F        // 阈值
);
thermalFile.SetAlarm(newAlarm);
```

### 5.9 视频文件处理

```csharp
// 打开热像视频
if (thermalFile.Open("thermal_video.avi"))
{
    // 获取帧数
    int frameCount = thermalFile.GetFrameCount();
    
    // 获取帧率
    float frameRate = thermalFile.GetFrameRate();
    
    // 切换帧
    for (int i = 0; i < frameCount; i++)
    {
        thermalFile.SetFrameIndex(i);
        
        // 获取当前帧
        Frame frame = thermalFile.GetFrame();
        
        // 处理帧数据...
    }
    
    thermalFile.Close();
}
```

### 5.10 图像处理

```csharp
// 获取热像图
Image thermalImage = thermalFile.GetThermalImage();

// 获取可见光图
Image visualImage = thermalFile.GetVisualImage();

// 设置可见光图
thermalFile.SetVisualImage(visualImage);

// 获取封面图
Image coverImage = thermalFile.GetCoverImage();

// 图像旋转
thermalFile.Rotate(RotateType.Rotate90);   // 顺时针90度
thermalFile.Rotate(RotateType.Rotate180);  // 180度
thermalFile.Rotate(RotateType.Rotate270);  // 顺时针270度

// 图像镜像
thermalFile.Mirror(MirrorType.MirrorHorizontal);  // 水平镜像
thermalFile.Mirror(MirrorType.MirrorVertical);    // 垂直镜像
```

### 5.11 元数据操作

```csharp
// 获取相机信息
CameraInfo cameraInfo = thermalFile.GetCameraInfo();
Console.WriteLine($"相机型号: {cameraInfo.Model}");
Console.WriteLine($"序列号: {cameraInfo.SerialNo}");

// 获取镜头信息
LensInfo lensInfo = thermalFile.GetLensInfo();
Console.WriteLine($"镜头: {lensInfo.Name}");
Console.WriteLine($"焦距: {lensInfo.FocalLength}mm");

// 获取地理位置
GeoLocation geoLocation = thermalFile.GetGeoLocation();
if (geoLocation.IsGPSValid)
{
    Console.WriteLine($"经度: {geoLocation.Longitude}");
    Console.WriteLine($"纬度: {geoLocation.Latitude}");
}

// 获取/设置注释
string note = thermalFile.GetNoteAnnotation();
thermalFile.SetNoteAnnotation("检测备注");

// 获取/设置标签
string tag = thermalFile.GetTagAnnotation();
thermalFile.SetTagAnnotation("重要");

// 获取/设置收藏状态
bool favorite = thermalFile.GetFavorite();
thermalFile.SetFavorite(true);

// 用户自定义数据
thermalFile.SetUserData("CustomKey", "CustomValue");
string value = thermalFile.GetUserData("CustomKey");
```

---

## 6. 声像文件SDK (IRtekACFileSDK)

### 6.1 文件操作

#### 6.1.1 打开文件

```csharp
var acousticFile = new AcousticImageFile();

if (acousticFile.Open("acoustic.ac"))
{
    // 处理文件...
    acousticFile.Close();
}
```

#### 6.1.2 创建新文件

```csharp
var acousticFile = new AcousticImageFile();

// 创建声像图片
if (acousticFile.Create("new.ac", FileType.AcousticPicture))
{
    // 设置声像尺寸
    acousticFile.SetAcousticSize(new Size(320, 240));
    
    // 添加帧数据
    Frame frame = new Frame
    {
        TimeStamp = (ulong)DateTime.Now.Ticks,
        AcousticImageSpl = new float[320, 240]
    };
    acousticFile.AppendFrame(frame);
    
    // 保存
    acousticFile.Save();
    acousticFile.Close();
}
```

### 6.2 声像参数设置

#### 6.2.1 获取/设置声像参数

```csharp
// 获取声像参数
AcousticParams param = acousticFile.GetAcousticParams();
Console.WriteLine($"增益: {param.Gain}");
Console.WriteLine($"平滑系数: {param.Smooth}");

foreach (var band in param.BandList)
{
    Console.WriteLine($"频带: {band.Min}Hz - {band.Max}Hz");
}

// 设置声像参数
AcousticParams newParam = new AcousticParams
{
    Gain = 3,
    Smooth = 0.5F,
    BandList = new List<Band>
    {
        new Band { Min = 1000F, Max = 8000F }
    }
};
acousticFile.SetAcousticParams(newParam);
```

#### 6.2.2 声像参数说明

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| Gain | int | -1~5 | 增益值 |
| Smooth | float | 0~1.0 | 帧间平均系数 |
| BandList | List\<Band\> | - | 频带列表 |
| Band.Min | float | 0~1000000Hz | 频率下限 |
| Band.Max | float | 0~1000000Hz | 频率上限 |

### 6.3 声宽设置 (DbScale)

```csharp
// 获取声宽
DbScale dbScale = acousticFile.GetDbScale();
Console.WriteLine($"模式: {dbScale.Mode}");
Console.WriteLine($"最大声压级: {dbScale.Max}dB");
Console.WriteLine($"最小声压级: {dbScale.Min}dB");

// 设置声宽
DbScale newDbScale = new DbScale(
    mode: DbScaleMode.Fixed,   // 模式：Fixed/Auto/Touch/AutoHolo
    max: 80.0F,                // 最大声压级 (dB)
    min: 20.0F                 // 最小声压级 (dB)
);
acousticFile.SetDbScale(newDbScale);
```

### 6.4 测量标识

#### 6.4.1 添加点标识

```csharp
IMarkerSpot spot = acousticFile.CreateSpot(new Point(100, 100));
spot.Name = "测点1";
acousticFile.Markers.Add(spot);

// 获取声压级值
float dbValue = spot.Value;
```

#### 6.4.2 添加椭圆标识

```csharp
IMarkerEllipse ellipse = acousticFile.CreateEllipse(new Rectangle(50, 50, 100, 80));
ellipse.Name = "椭圆区域1";
acousticFile.Markers.Add(ellipse);

// 获取测量结果
float max = ellipse.Max;     // 最大声压级
float min = ellipse.Min;     // 最小声压级
float avg = ellipse.Avg;     // 平均声压级
```

### 6.5 图像获取

```csharp
// 获取声像图
Bitmap acousticImage = acousticFile.GetAcousticImage();

// 获取可见光图
Bitmap visualImage = acousticFile.GetVisualImage();

// 获取调色板图
Bitmap paletteImage = acousticFile.GetPaletteImage();
```

### 6.6 视频文件处理

```csharp
if (acousticFile.Open("acoustic_video.avi"))
{
    // 获取帧数
    int frameCount = acousticFile.GetFrameCount();
    
    // 切换帧
    for (uint i = 0; i < frameCount; i++)
    {
        Frame frame = acousticFile.GetFrame(i);
        
        // 处理帧数据...
        // frame.AcousticImageSpl - 声压级网格数据
        // frame.VisualImage - 可见光图像
        // frame.Sound - 声音数据
        // frame.Spectrum - 频谱数据
    }
    
    acousticFile.Close();
}
```

### 6.7 帧数据结构

```csharp
public struct Frame
{
    public ulong TimeStamp;              // 时间戳
    public float[,] AcousticImageSpl;    // 声压级网格数据
    public byte[] VisualImage;           // 可见光图像数据
    public Sound Sound;                  // 声音数据
    public Spectrum Spectrum;            // 频谱数据
    public PointF[] PRPD;                // PRPD数据
}
```

---

## 7. 公共类型定义

### 7.1 枚举类型

#### FileType - 文件类型

```csharp
public enum FileType
{
    Other = 0,              // 其他类型
    ThermalPicture = 1,     // 热像图片
    ThermalVideo = 2,       // 热像视频
    VisualPicture = 3,      // 可见光图片
    GridThermalPicture = 4, // 电网热像图片
    AcousticPicture = 5,    // 声像图片
    AcousticVideo = 6,      // 声像视频
}
```

#### MarkerType - 测量标识类型

```csharp
public enum MarkerType
{
    Spot = 1,       // 点
    Rectangle = 3,  // 矩形
    Ellipse = 4,    // 椭圆
    Polygon = 5,    // 多边形
    Polyline = 6,   // 折线
}
```

#### ValueType - 值类型

```csharp
public enum ValueType
{
    Max = 1,  // 最大值
    Min = 2,  // 最小值
    Avg = 3,  // 平均值
}
```

#### GainMode - 温宽/声宽模式

```csharp
public enum GainMode
{
    Fixed = 1,  // 固定模式
    Auto = 2,   // 自动模式
    Touch = 3,  // 触控模式
}
```

#### AlarmType - 报警类型

```csharp
public enum AlarmType
{
    Normal = 0,       // 正常
    Below = 1,        // 低于下限
    Above = 2,        // 高于上限
    Between = 3,      // 在范围内
    MagicThermal = 4, // 魔术热像
    Outside = 5,      // 在范围外
    Humidity = 6,     // 湿度
    Insulation = 7,   // 绝缘
}
```

#### RotateType - 旋转类型

```csharp
public enum RotateType
{
    Rotate90 = 1,   // 顺时针90度
    Rotate180 = 2,  // 180度
    Rotate270 = 3,  // 顺时针270度
}
```

#### MirrorType - 镜像类型

```csharp
public enum MirrorType
{
    MirrorHorizontal = 1,  // 水平镜像
    MirrorVertical = 2,    // 垂直镜像
}
```

#### ViewType - 显示类型

```csharp
public enum ViewType
{
    Thermal = 1,    // 纯热像
    PicInPic = 2,   // 画中画
    T_DEF = 3,      // T_DEF模式
    Fusion = 4,     // 融合模式
}
```

### 7.2 结构体类型

#### ThermalParams - 测温参数

```csharp
public struct ThermalParams
{
    public float ReflTemp;      // 反射温度 (K)
    public float AtmTemp;       // 大气温度 (K)
    public float Distance;      // 目标距离 (m)
    public float RelHumidity;   // 相对湿度 (0~1)
    public float Emissivity;    // 发射率 (0.01~1)
    public float ExtOptTemp;    // 外部光学温度 (K)
    public float ExtOptTrans;   // 外部光学透过率 (0~1)
    
    public static readonly ThermalParams DEFAULT = new ThermalParams(
        293.15F, 293.15F, 1.0F, 0.5F, 1.0F, 293.15F, 1.0F
    );
}
```

#### LocalThermalParams - 局部测温参数

```csharp
public struct LocalThermalParams
{
    public bool LocalParams;    // 是否启用局部参数
    public float Emissivity;    // 发射率
    public float Distance;      // 距离
    public float ReflTemp;      // 反射温度
}
```

#### Gain - 温宽设置

```csharp
public struct Gain
{
    public GainMode Mode;       // 模式
    public float Max;           // 最高温度 (K)
    public float Min;           // 最低温度 (K)
    public bool FixedSpan;      // 是否固定温宽
    public float Span;          // 温宽值 (K)
}
```

#### Palette - 调色板

```csharp
public struct Palette
{
    public byte RefNo;          // 调色板编号 (0xFF表示自定义)
    public uint[] Colors;       // 256色调色板数据
    public bool Inverted;       // 是否反转
}
```

#### Alarm - 报警设置

```csharp
public struct Alarm
{
    public AlarmType Type;      // 报警类型
    public bool Custom;         // 是否自定义颜色
    public Color Color;         // 报警颜色
    public float Max;           // 上限温度 (K)
    public float Min;           // 下限温度 (K)
    public float RHLimit;       // 湿度限制
    public float IndoorTemp;    // 室内温度 (K)
    public float OutdoorTemp;   // 室外温度 (K)
    public float HeatIndex;     // 热指数
    public float Threshold;     // 阈值
}
```

#### CameraInfo - 相机信息

```csharp
public struct CameraInfo
{
    public string Model;        // 型号
    public string SerialNo;     // 序列号
    public string Brand;        // 品牌
    public string Company;      // 公司
    public Image Logo;          // Logo图像
}
```

#### LensInfo - 镜头信息

```csharp
public struct LensInfo
{
    public string Name;         // 名称
    public float FocalLength;   // 焦距 (mm)
    public float HFov;          // 水平视场角
    public string Band;         // 波段
}
```

#### GeoLocation - 地理位置

```csharp
public struct GeoLocation
{
    public bool IsGPSValid;     // GPS是否有效
    public double Longitude;    // 经度
    public double Latitude;     // 纬度
    public double Altitude;     // 海拔
    public bool IsCompassValid; // 罗盘是否有效
    public short Direction;     // 方向
}
```

#### Frame - 帧数据

```csharp
public struct Frame
{
    public DateTime TimeStamp;  // 时间戳
    public ushort[] Data;       // AD值数组
    public ushort[,] Values;    // 二维数据
}
```

#### TemperaturePoint - 温度点

```csharp
public struct TemperaturePoint
{
    public float X;       // X坐标
    public float Y;       // Y坐标
    public float Value;   // 温度值 (K)
}
```

#### Reference - 参考温度

```csharp
public struct Reference
{
    public bool Enabled;        // 是否启用
    public string Name;         // 标识名称
    public ValueType ValueType; // 值类型
    public float Temp;          // 参考温度 (K)
}
```

---

## 8. 事件处理

### 8.1 热像文件事件

```csharp
IThermalImageFile thermalFile = validator.Create();

// 测量标识事件
thermalFile.MarkerAdded += (sender, e) => 
{
    Console.WriteLine($"添加标识: {e.Marker.Name}");
};

thermalFile.MarkerChanged += (sender, e) => 
{
    Console.WriteLine($"标识已更改: {e.Marker.Name}");
};

thermalFile.MarkerRemoved += (sender, e) => 
{
    Console.WriteLine($"移除标识: {e.Marker.Name}");
};

// 温差标记事件
thermalFile.DeltaAdded += (sender, e) => 
{
    Console.WriteLine($"添加温差: {e.Delta.Name}");
};

thermalFile.DeltaChanged += (sender, e) => 
{
    Console.WriteLine($"温差已更改: {e.Delta.Name}");
};

// 参数变更事件
thermalFile.ThermalParamsChanged += (sender, e) => 
{
    Console.WriteLine("测温参数已更改");
};

thermalFile.GainChanged += (sender, e) => 
{
    Console.WriteLine("温宽已更改");
};

thermalFile.PaletteChanged += (sender, e) => 
{
    Console.WriteLine("调色板已更改");
};

// 图像变更事件
thermalFile.ImageChanged += (sender, e) => 
{
    Console.WriteLine("图像已更改");
};

// 报警事件
thermalFile.AlarmChanged += (sender, e) => 
{
    Console.WriteLine("报警设置已更改");
};

// 帧切换事件
thermalFile.FrameIndexChanged += (sender, e) => 
{
    Console.WriteLine("当前帧已切换");
};

// 修改状态事件
thermalFile.ModifiedStatusChanged += (sender, e) => 
{
    Console.WriteLine($"文件修改状态: {thermalFile.IsModified}");
};
```

### 8.2 声像文件事件

```csharp
var acousticFile = new AcousticImageFile();

// 测量标识事件
acousticFile.MarkerAdded += (sender, e) => 
{
    Console.WriteLine($"添加标识: {e.Marker.Name}");
};

acousticFile.MarkerChanged += (sender, e) => 
{
    Console.WriteLine($"标识已更改: {e.Marker.Name}");
};

// 声宽变更事件
acousticFile.DbScaleChanged += (sender, e) => 
{
    Console.WriteLine("声宽已更改");
};

// 帧切换事件
acousticFile.FrameIndexChanged += (sender, e) => 
{
    Console.WriteLine($"当前帧: {e.FrameIndex}");
};
```

---

## 9. 错误处理

### 9.1 基本错误处理

```csharp
try
{
    if (!thermalFile.Open("test.jpg"))
    {
        Console.WriteLine("打开文件失败");
        return;
    }
    
    // 处理文件...
}
catch (Exception ex)
{
    Console.WriteLine($"发生错误: {ex.Message}");
}
finally
{
    thermalFile?.Close();
}
```

### 9.2 文件验证

```csharp
var validator = IRtekFileSDKValidator.Instance;
FileType fileType = validator.IsValidFile("test.jpg");

if (fileType == FileType.Other)
{
    Console.WriteLine("不支持的文件类型");
    return;
}

Console.WriteLine($"文件类型: {fileType}");
```

### 9.3 状态检查

```csharp
// 检查文件是否有效
if (!thermalFile.IsValid)
{
    Console.WriteLine("文件句柄无效");
    return;
}

// 检查文件是否已修改
if (thermalFile.IsModified)
{
    Console.WriteLine("文件已修改，需要保存");
}
```

---

## 10. 完整示例

### 10.1 热像文件分析示例

```csharp
using System;
using System.Drawing;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

class ThermalAnalysisExample
{
    static void Main(string[] args)
    {
        string filePath = "thermal_image.jpg";
        
        // 验证文件
        var validator = IRtekFileSDKValidator.Instance;
        FileType fileType = validator.IsValidFile(filePath);
        
        if (fileType == FileType.Other)
        {
            Console.WriteLine("不支持的文件类型");
            return;
        }
        
        // 创建并打开文件
        IThermalImageFile thermalFile = validator.Create();
        
        try
        {
            if (!thermalFile.Open(filePath))
            {
                Console.WriteLine("打开文件失败");
                return;
            }
            
            Console.WriteLine($"文件类型: {thermalFile.FileType}");
            Console.WriteLine($"文件版本: {thermalFile.FileVersion}");
            Console.WriteLine($"热像尺寸: {thermalFile.GetThermalSize()}");
            
            // 获取相机信息
            CameraInfo cameraInfo = thermalFile.GetCameraInfo();
            Console.WriteLine($"相机型号: {cameraInfo.Model}");
            Console.WriteLine($"序列号: {cameraInfo.SerialNo}");
            
            // 获取测温参数
            ThermalParams param = thermalFile.GetThermalParams();
            Console.WriteLine($"发射率: {param.Emissivity}");
            Console.WriteLine($"距离: {param.Distance}m");
            
            // 添加点测量
            IMarkerSpot spot = thermalFile.CreateSpot(new Point(100, 100));
            spot.Name = "中心点";
            thermalFile.Markers.Add(spot);
            Console.WriteLine($"中心点温度: {spot.Value - 273.15:F1}°C");
            
            // 添加矩形区域测量
            IMarkerRectangle rect = thermalFile.CreateRectangle(
                new Rectangle(50, 50, 100, 100));
            rect.Name = "ROI区域";
            thermalFile.Markers.Add(rect);
            Console.WriteLine($"区域最高温度: {rect.Max - 273.15:F1}°C");
            Console.WriteLine($"区域最低温度: {rect.Min - 273.15:F1}°C");
            Console.WriteLine($"区域平均温度: {rect.Avg - 273.15:F1}°C");
            
            // 添加温差标记
            IDelta delta = thermalFile.CreateDelta();
            delta.Name = "温差分析";
            delta.SrcName = "中心点";
            delta.SrcValueType = ValueType.Max;
            delta.DestName = "ROI区域";
            delta.DestValueType = ValueType.Avg;
            thermalFile.Deltas.Add(delta);
            
            DeltaParams deltaParam = delta.GetDeltaParams();
            Console.WriteLine($"温差: {deltaParam.SrcTemp - deltaParam.DestTemp:F1}K");
            
            // 保存修改
            if (thermalFile.IsModified)
            {
                thermalFile.Save();
                Console.WriteLine("文件已保存");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
        }
        finally
        {
            thermalFile.Close();
        }
    }
}
```

### 10.2 声像文件分析示例

```csharp
using System;
using System.Drawing;
using Amur.IRtekACFileSDK;

class AcousticAnalysisExample
{
    static void Main(string[] args)
    {
        string filePath = "acoustic_image.ac";
        
        var acousticFile = new AcousticImageFile();
        
        try
        {
            if (!acousticFile.Open(filePath))
            {
                Console.WriteLine("打开文件失败");
                return;
            }
            
            Console.WriteLine($"文件类型: {acousticFile.FileType}");
            Console.WriteLine($"声像尺寸: {acousticFile.GetAcousticSize()}");
            
            // 获取声像参数
            AcousticParams param = acousticFile.GetAcousticParams();
            Console.WriteLine($"增益: {param.Gain}");
            Console.WriteLine($"平滑系数: {param.Smooth}");
            
            foreach (var band in param.BandList)
            {
                Console.WriteLine($"频带: {band.Min}Hz - {band.Max}Hz");
            }
            
            // 获取声宽
            DbScale dbScale = acousticFile.GetDbScale();
            Console.WriteLine($"声压级范围: {dbScale.Min}dB - {dbScale.Max}dB");
            
            // 添加点测量
            IMarkerSpot spot = acousticFile.CreateSpot(new Point(160, 120));
            spot.Name = "中心点";
            acousticFile.Markers.Add(spot);
            Console.WriteLine($"中心点声压级: {spot.Value:F1}dB");
            
            // 添加椭圆区域测量
            IMarkerEllipse ellipse = acousticFile.CreateEllipse(
                new Rectangle(100, 80, 120, 80));
            ellipse.Name = "ROI区域";
            acousticFile.Markers.Add(ellipse);
            Console.WriteLine($"区域最大声压级: {ellipse.Max:F1}dB");
            Console.WriteLine($"区域最小声压级: {ellipse.Min:F1}dB");
            Console.WriteLine($"区域平均声压级: {ellipse.Avg:F1}dB");
            
            // 获取声像图
            Bitmap acousticImage = acousticFile.GetAcousticImage();
            acousticImage.Save("acoustic_output.png");
            Console.WriteLine("声像图已保存");
            
            // 保存修改
            if (acousticFile.IsModified)
            {
                acousticFile.Save();
                Console.WriteLine("文件已保存");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
        }
        finally
        {
            acousticFile.Close();
        }
    }
}
```

### 10.3 热像视频处理示例

```csharp
using System;
using System.Drawing;
using Amur.IRtekFileSDK;
using Amur.ThermalImageFile;

class ThermalVideoExample
{
    static void Main(string[] args)
    {
        string filePath = "thermal_video.avi";
        
        var validator = IRtekFileSDKValidator.Instance;
        FileType fileType = validator.IsValidFile(filePath);
        
        if (fileType != FileType.ThermalVideo)
        {
            Console.WriteLine("不是热像视频文件");
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
            
            int frameCount = thermalFile.GetFrameCount();
            float frameRate = thermalFile.GetFrameRate();
            
            Console.WriteLine($"总帧数: {frameCount}");
            Console.WriteLine($"帧率: {frameRate}fps");
            
            // 遍历所有帧
            for (int i = 0; i < frameCount; i++)
            {
                thermalFile.SetFrameIndex(i);
                
                Frame frame = thermalFile.GetFrame();
                Console.WriteLine($"帧 {i}: {frame.TimeStamp}");
                
                // 获取帧内某点温度
                float temp = thermalFile.GetSpotValue(new Point(100, 100));
                Console.WriteLine($"  温度: {temp - 273.15:F1}°C");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
        }
        finally
        {
            thermalFile.Close();
        }
    }
}
```

---

## 附录

### A. 温度单位转换

SDK中温度单位为开尔文(K)，常用转换：

```csharp
// 开尔文转摄氏度
float celsius = kelvin - 273.15F;

// 摄氏度转开尔文
float kelvin = celsius + 273.15F;

// 开尔文转华氏度
float fahrenheit = kelvin * 9 / 5 - 459.67F;
```

### B. 常用发射率参考值

| 材料 | 发射率 |
|------|--------|
| 氧化铝 | 0.40 |
| 沥青 | 0.95 |
| 砖 | 0.90 |
| 混凝土 | 0.95 |
| 玻璃 | 0.85 |
| 人体皮肤 | 0.98 |
| 铸铁(氧化) | 0.79 |
| 铜(氧化) | 0.65 |
| 不锈钢(氧化) | 0.85 |
| 水 | 0.96 |

### C. 技术支持

如有问题，请联系 IRtek 技术支持团队。

---

**版权所有 © IRtek.Co.Ltd. 保留所有权利。**
