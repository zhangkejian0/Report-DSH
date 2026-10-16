using System;
using System.Drawing;

namespace Amur.ThermalImageFile
{
    #region Constants

    public static class Commons
    {
        public const string GLOBAL_MARKER = "global";
    }

    #endregion Constants

    #region Enums

    public enum FileType
    {
        Other = 0,
        ThermalPicture = 1,
        ThermalVideo = 2,
        VisualPicture = 3,
        GridThermalPicture = 4,
        AcousticPicture = 5,
        AcousticVideo = 6,
    }

    public enum AudioType
    {
        Wav = 1,
        MP3 = 2
    }

    public enum MarkerType
    {
        Spot = 1,
        Rectangle = 3,
        Ellipse = 4,
        Polygon = 5,
        Polyline = 6,
    }

    public enum ValueType
    {
        Max = 1,
        Min = 2,
        Avg = 3,
    }

    public enum RotateType
    {
        Rotate90 = 1,
        Rotate180 = 2,
        Rotate270 = 3,
    }

    public enum MirrorType
    {
        MirrorHorizontal = 1,
        MirrorVertical = 2,
    }

    public enum GainMode
    {
        Fixed = 1,
        Auto = 2,
        Touch = 3,
    }

    public enum AlarmType
    {
        Normal = 0,
        Below = 1,
        Above = 2,
        Between = 3,
        MagicThermal = 4,
        Outside = 5,
        Humidity = 6,
        Insulation = 7,
    }

    public enum ColorDistributionType
    {
        T_TWB = 1,
        TempLinear = 2,
        ADLinear = 3,
    }

    public enum ViewType
    {
        Thermal = 1,
        PicInPic = 2,
        T_DEF = 3,
        Fusion = 4,
    }

    public enum ClearType
    {
        UserMarkers = 1,
    }

    #endregion Enums

    #region Structs

    public struct CameraInfo
    {
        public static readonly CameraInfo DEFAULT = new CameraInfo(string.Empty, string.Empty, string.Empty, string.Empty, null);

        public CameraInfo(string model, string serialNo, string brand, string company, Image logo)
        {
            Model = model;
            SerialNo = serialNo;
            Brand = brand;
            Company = company;
            Logo = logo;
        }

        public string Model { get; set; }
        public string SerialNo { get; set; }
        public string Brand { get; set; }
        public string Company { get; set; }
        public Image Logo { get; set; }
    }

    public struct LensInfo
    {
        public static readonly LensInfo DEFAULT = new LensInfo(string.Empty, 0.0F, 0.0F, string.Empty);

        public LensInfo(string name, float focalLength, float hFov, string band)
        {
            Name = name;
            FocalLength = focalLength;
            HFov = hFov;
            Band = band;
        }

        public string Name { get; set; }
        public float FocalLength { get; set; }
        public float HFov { get; set; }
        public string Band { get; set; }
    }

    public struct TempRange
    {
        public static readonly TempRange DEFAULT = new TempRange(0.0F, 0.0F);

        public TempRange(float max, float min)
        {
            Max = max;
            Min = min;
        }

        public float Max { get; set; }
        public float Min { get; set; }
    }

    public struct GeoLocation
    {
        public static readonly GeoLocation DEFAULT = new GeoLocation(false, 0.0F, 0.0F, 0.0F, false, 0);

        public GeoLocation(bool isGPSValid, float longitude, float latitude, float altitude, bool isCompassValid, short direction)
        {
            IsGPSValid = isGPSValid;
            Longitude = longitude;
            Latitude = latitude;
            Altitude = altitude;
            IsCompassValid = isCompassValid;
            Direction = direction;
        }

        public bool IsGPSValid { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Altitude { get; set; }
        public bool IsCompassValid { get; set; }
        public short Direction { get; set; }
    }

    public struct ThermalParams
    {
        public static readonly ThermalParams DEFAULT = new ThermalParams(293.15F, 293.15F, 1.0F, 0.5F, 1.0F, 293.15F, 1.0F);

        public ThermalParams(float reflTemp, float atmTemp, float distance, float relHumidity, float emissivity, float extOptTemp, float extOptTrans)
        {
            ReflTemp = reflTemp;
            AtmTemp = atmTemp;
            Distance = distance;
            RelHumidity = relHumidity;
            Emissivity = emissivity;
            ExtOptTemp = extOptTemp;
            ExtOptTrans = extOptTrans;
        }

        public float ReflTemp { get; set; }
        public float AtmTemp { get; set; }
        public float Distance { get; set; }
        public float RelHumidity { get; set; }
        public float Emissivity { get; set; }
        public float ExtOptTemp { get; set; }
        public float ExtOptTrans { get; set; }
    }

    public class TimerFileInfo
    {
        public TimerFileInfo(string guid, int no)
        {
            GUID = guid;
            No = no;
        }

        public string GUID { get; private set; }
        public int No { get; private set; }
    }

    public struct LocalThermalParams
    {
        public bool LocalParams { get; set; }
        public float Emissivity { get; set; }
        public float Distance { get; set; }
        public float ReflTemp { get; set; }
    }

    public struct AudioAnnotation
    {
        public AudioAnnotation(bool isValid, AudioType type, byte[] data)
        {
            IsValid = isValid;
            Type = type;
            Data = data;
        }

        public bool IsValid { get; private set; }
        public AudioType Type { get; private set; }
        public byte[] Data { get; private set; }
    }

    public struct DeltaParams
    {
        public DeltaParams(string srcName, ValueType srcValueType, float srcTemp, string destName, ValueType destValueType, float destTemp)
        {
            SrcName = srcName;
            SrcValueType = srcValueType;
            SrcTemp = srcTemp;
            DestName = destName;
            DestValueType = destValueType;
            DestTemp = destTemp;
        }

        public string SrcName { get; set; }
        public ValueType SrcValueType { get; set; }
        public float SrcTemp { get; set; }
        public string DestName { get; set; }
        public ValueType DestValueType { get; set; }
        public float DestTemp { get; set; }
    }

    public struct Reference
    {
        public static readonly Reference DEFAULT = new Reference(false, string.Empty, ValueType.Max, 293.15F);

        public Reference(bool enabled, string name, ValueType valueType, float temp)
        {
            Enabled = enabled;
            Name = name;
            ValueType = valueType;
            Temp = temp;
        }

        public bool Enabled { get; set; }
        public string Name { get; set; }
        public ValueType ValueType { get; set; }
        public float Temp { get; set; }
    }

    public struct Gain
    {
        public static readonly Gain DEFAULT = new Gain(GainMode.Auto, 293.15F, 273.15F, false, 2.0F);

        public Gain(GainMode mode, float max, float min, bool fixedSpan, float span)
        {
            Mode = mode;
            Max = max;
            Min = min;
            FixedSpan = fixedSpan;
            Span = span;
        }

        public GainMode Mode { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public bool FixedSpan { get; set; }
        public float Span { get; set; }
    }

    public struct Alarm
    {
        public static readonly Alarm DEFAULT = new Alarm(AlarmType.Normal, false, Color.Black, 313.15F, 293.15F, 1.0F, 293.15F, 283.15F, 0.8F, 293.15F);

        public Alarm(AlarmType type, bool custom, Color color, float max, float min, float rhLimit, float indoorTemp, float outdoorTemp, float heatIndex, float threshold)
        {
            Type = type;
            Custom = custom;
            Color = color;
            Max = max;
            Min = min;
            RHLimit = rhLimit;
            IndoorTemp = indoorTemp;
            OutdoorTemp = outdoorTemp;
            HeatIndex = heatIndex;
            Threshold = threshold;
        }

        public AlarmType Type { get; set; }
        public bool Custom { get; set; }
        public Color Color { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public float RHLimit { get; set; }
        public float IndoorTemp { get; set; }
        public float OutdoorTemp { get; set; }
        public float HeatIndex { get; set; }
        public float Threshold { get; set; }
    }

    public struct Palette
    {
        public static readonly Palette DEFAULT = new Palette(0, null, false);

        public Palette(byte refNo, uint[] colors, bool inverted)
        {
            RefNo = refNo;
            Colors = colors;
            Inverted = inverted;
        }

        public byte RefNo { get; set; }
        public uint[] Colors { get; set; }
        public bool Inverted { get; set; }
    }

    public struct PaletteV2
    {
        public static readonly PaletteV2 DEFAULT = new PaletteV2(0x01, null, null, false);

        public PaletteV2(byte refNo, uint[] srcColors, uint[] destColors, bool inverted)
        {
            RefNo = refNo;
            SrcColors = srcColors;
            DestColors = destColors;
            Inverted = inverted;
        }

        public byte RefNo { get; set; }
        public uint[] SrcColors { get; set; }
        public uint[] DestColors { get; set; }
        public bool Inverted { get; set; }
    }

    public struct PresetPalette
    {
        public byte RefNo { get; set; }
        public string Name { get; set; }
    }

    public struct ViewInfo
    {
        public static readonly ViewInfo DEFAULT = new ViewInfo(ViewType.Thermal, 313.15F, 293.15F, 0, new RectangleF(0.25F, 0.25F, 0.5F, 0.5F));

        public ViewInfo(ViewType type, float max, float min, byte alpha, RectangleF area)
        {
            Type = type;
            Max = max;
            Min = min;
            Alpha = alpha;
            Area = area;
        }

        public ViewType Type { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public byte Alpha { get; set; }
        public RectangleF Area { get; set; }
    }

    public struct MappingInfo
    {
        public static readonly MappingInfo DEFAULT = new MappingInfo(new Size(800, 600), new RectangleF(0.25F, 0.25F, 0.5F, 0.5F));

        public MappingInfo(Size visualSize, RectangleF mappingArea)
        {
            VisualSize = visualSize;
            MappingArea = mappingArea;
        }

        public Size VisualSize { get; set; }
        public RectangleF MappingArea { get; set; }
    }

    public struct ZoomInfo
    {
        public static readonly ZoomInfo DEFAULT = new ZoomInfo(false, new PointF(0.0F, 0.0F), 1.0F);

        public ZoomInfo(bool enabled, PointF location, float ratio)
        {
            Enabled = enabled;
            Location = location;
            Ratio = ratio;
        }

        public bool Enabled { get; set; }
        public PointF Location { get; set; }
        public float Ratio { get; set; }
    }

    public struct Frame
    {
        public static readonly Frame DEFAULT = new Frame(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), new ushort[0]);

        public Frame(DateTime timeStamp, ushort[] data, ushort[,] datas=null)
        {
            TimeStamp = timeStamp;
            Data = data;
            Values = datas;
        }

        public DateTime TimeStamp { get; set; }
        public ushort[] Data { get; set; }
        public ushort[,] Values { get; set; }
    }

    public struct LUT
    {
        public static readonly LUT DEFAULT = new LUT(0, new ushort[] { 0, 65535 }, new float[] { 253.15F, 423.15F });

        public LUT(byte radMethod, ushort[] adArray, float[] tempArray)
        {
            RadMethod = radMethod;
            ADArray = adArray;
            TempArray = tempArray;
        }

        public byte RadMethod { get; set; }
        public ushort[] ADArray { get; set; }
        public float[] TempArray { get; set; }
    }

    public struct TemperaturePoint
    {
        public readonly static TemperaturePoint Empty = new TemperaturePoint(0, 0, 0);

        public TemperaturePoint(float x, float y, float value)
        {
            X = x;
            Y = y;
            Value = value;
        }

        public float X;
        public float Y;
        public float Value;
    }

    #endregion Structs
}