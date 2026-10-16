using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Amur.IRtekACFileSDK
{
    public enum FileType
    {
        Other = 0,
        AcousticPicture = 1,
        AcousticVideo = 2,
    }

    public enum AudioType
    {
        Wav = 1,
        Mp3 = 2
    }

    public enum MarkerType
    {
        Spot = 1,
        Ellipse = 4,
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

    public enum DbScaleMode
    {
        AutoMono = 1,
        AutoMulti = 2,
        Fixed = 3,
        AutoHolo = 4,
    }

    public enum CaptureMode
    {
        Other = 0x00,
        LeakMode = 0x01,
        PDMode = 0x02,
    }

    public struct Band
    {
        public static readonly Band DEFAULT = new Band(100000, 5000);

        public Band(float max, float min)
        {
            Max = max;
            Min = min;
        }

        public float Max { get; set; }

        public float Min { get; set; }
    }
    public struct Spectrum
    {
        public float[] FFT { get; set; }
        public Band Range { get; set; }
        public float Delta { get; set; }
    }

    public struct Sound
    {
        public float[] Source { get; set; }
        public float[] Filter { get; set; }
        public float[] DownSample { get; set; }
    }

    public struct Frame
    {
        public static readonly Frame DEFAULT = new Frame((ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);

        public Frame(ulong timeStamp)
        {
            TimeStamp = timeStamp;
            AcousticImageSpl = null;
            VisualImage = null;
            Sound = new Sound();
            Spectrum = new Spectrum();
            PRPD = null;
        }

        public ulong TimeStamp { get; set; }
        public float[,] AcousticImageSpl { get; set; }
        public byte[] VisualImage { get; set; }
        public Sound Sound { get; set; }
        public Spectrum Spectrum { get; set; }
        public PointF[] PRPD { get; set; }
    }

    public struct CameraInfo
    {
        public static readonly CameraInfo DEFAULT = new CameraInfo(string.Empty, string.Empty, string.Empty, string.Empty, null);

        public CameraInfo(string model, string serialNo, string brand, string company, Bitmap logo)
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
        public Bitmap Logo { get; set; }
    }

    public struct SensorInfo
    {
        public static readonly SensorInfo DEFAULT = new SensorInfo(string.Empty, 0, string.Empty, 0, 0, 1);

        public SensorInfo(string serialNo, int channelCount, string version, int refChannel, int sampleRate, float amplitude)
        {
            SerialNo = serialNo;
            ChannelCount = channelCount;
            Version = version;
            RefChannel = refChannel;
            SampleRate = sampleRate;
            Amplitude = amplitude;
        }

        public string SerialNo { get; set; }
        public int ChannelCount { get; set; }
        public string Version { get; set; }
        public int RefChannel { get; set; }
        public int SampleRate { get; set; }
        public float Amplitude { get; set; }
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


    public struct AcousticParams
    {
        public static readonly AcousticParams DEFAULT = new AcousticParams();

        public List<Band> BandList { get; set; }

        public int Gain { get; set; }

        public float Smooth { get; set; }
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

    public struct DbScale
    {
        public static readonly DbScale DEFAULT = new DbScale(DbScaleMode.Fixed, 100F, 0F, false, 0.1f);

        public DbScale(DbScaleMode mode, float max, float min, bool fixedSpan, float span)
        {
            Mode = mode;
            Max = max;
            Min = min;
            FixedSpan = fixedSpan;
            Span = span;
        }

        public DbScaleMode Mode { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public bool FixedSpan { get; set; }
        public float Span { get; set; }
    }

    public struct FocusMode
    {
        public static readonly FocusMode DEFAULT = new FocusMode(false, 0.5F);

        public FocusMode(bool enabled, float scope)
        {
            Enabled = enabled;
            Scope = scope;
        }

        public bool Enabled { get; set; }
        public float Scope { get; set; }
    }
    public struct Palette
    {
        public static readonly Palette DEFAULT = new Palette(0x01, null, 60);

        public Palette(byte refNo, uint[] colors, byte alpha)
        {
            RefNo = refNo;
            Colors = colors;
            Alpha = alpha;
        }

        public byte RefNo { get; set; }
        public uint[] Colors { get; set; }
        public byte Alpha { get; set; }
    }

    public struct PresetPalette
    {
        public byte RefNo { get; set; }
        public string Name { get; set; }
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

    public struct AcousticSummary
    {
        public static readonly AcousticSummary DEFAULT = new AcousticSummary(new Point(0, 0), new Point(0, 0));

        internal AcousticSummary(Point max, Point min)
        {
            Max = new AcousticPoint(max);
            Min = new AcousticPoint(min);
            RawData = new AcousticImageUtils.acoustic_summary();
        }

        internal AcousticSummary(AcousticImageUtils.acoustic_summary rawData)
        {
            Max = new AcousticPoint(rawData.max);
            Min = new AcousticPoint(rawData.min);
            RawData = rawData;
        }

        public AcousticPoint Max { get; private set; }

        public AcousticPoint Min { get; private set; }

        public AcousticImageUtils.acoustic_summary RawData { get; private set; }
    }

    public struct AcousticPoint
    {
        internal AcousticPoint(Point point)
        {
            X = point.X;
            Y = point.Y;
            Value = 0.0f;
        }

        internal AcousticPoint(AcousticImageUtils.acoustic_point point)
        {
            X = point.x;
            Y = point.y;
            Value = point.value;
        }

        public int X { get; private set; }

        public int Y { get; private set; }

        public float Value { get; private set; }
    }

    public abstract class ImageBase
    {
        protected const byte TRUE = 0x01;
        protected const byte FALSE = 0x00;

        public ImageBase()
        {
            HImage = IntPtr.Zero;
            IsModified = false;
        }

        public static string Version
        {
            get
            {
                uint version = 0;
                IRtekACFileSDK.ac_file_buffer strVersion = new IRtekACFileSDK.ac_file_buffer();
                int result = IRtekACFileSDK.ac_file_get_sdk_version(ref version, ref strVersion);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    try
                    {
                        strVersion.buffer = Marshal.AllocHGlobal(strVersion.length);
                        result = IRtekACFileSDK.ac_file_get_sdk_version(ref version, ref strVersion);
                        if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                        {
                            byte[] buffer = new byte[strVersion.length];
                            Marshal.Copy(strVersion.buffer, buffer, 0, strVersion.length);
                            return BytesToString(buffer, Encoding.UTF8);
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ac_file_get_sdk_version error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(strVersion.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_sdk_version error code {0}.", result));
                }
            }
        }

        protected IntPtr HImage { get; set; }

        public bool IsModified { get; protected set; }

        public bool IsValid
        {
            get { return HImage != IntPtr.Zero; }
        }

        public string FileName
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return string.Empty;
                }

                IRtekACFileSDK.ac_file_buffer irBuffer = new IRtekACFileSDK.ac_file_buffer();
                int result = IRtekACFileSDK.ac_file_get_file_name(HImage, ref irBuffer);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    try
                    {
                        irBuffer.buffer = Marshal.AllocHGlobal(irBuffer.length);
                        result = IRtekACFileSDK.ac_file_get_file_name(HImage, ref irBuffer);
                        if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                        {
                            byte[] buffer = new byte[irBuffer.length];
                            Marshal.Copy(irBuffer.buffer, buffer, 0, irBuffer.length);
                            return BytesToString(buffer, Encoding.Default);
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ac_file_get_file_name error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irBuffer.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_file_name error code {0}.", result));
                }
            }
        }

        public FileType FileType
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return FileType.Other;
                }

                int irFileType = 0;
                int result = IRtekACFileSDK.ac_file_get_file_type(HImage, ref irFileType);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    switch (irFileType)
                    {
                        case IRtekACFileSDK.AC_FILE_OTHER:
                            return FileType.Other;
                        case IRtekACFileSDK.AC_FILE_PICTURE:
                            return FileType.AcousticPicture;
                        case IRtekACFileSDK.AC_FILE_VIDEO:
                            return FileType.AcousticVideo;
                        default:
                            return FileType.Other;
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_file_type error code {0}.", result));
                }
            }
        }

        public ushort FileVersion
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return 0;
                }

                ushort irFileVersion = 0;
                int result = IRtekACFileSDK.ac_file_get_file_version(HImage, ref irFileVersion);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    return irFileVersion;
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_file_version error code {0}.", result));
                }
            }
        }

        protected static string BytesToString(byte[] input, Encoding encode)
        {
            if (input == null)
            {
                return string.Empty;
            }

            int count = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 0)
                {
                    break;
                }
                count++;
            }
            return encode.GetString(input, 0, count);
        }

        protected static string BytesToString(byte[] input, int startIndex, int length, Encoding encode)
        {
            if (input == null)
            {
                return string.Empty;
            }

            int count = 0;
            for (int i = startIndex; i < input.Length && i < startIndex + length; i++)
            {
                if (input[i] == 0)
                {
                    break;
                }
                count++;
            }
            return encode.GetString(input, startIndex, count);
        }

        protected static byte[] StringToBytes(string input, Encoding encode)
        {
            return encode.GetBytes(input);
        }

        protected static byte[] StringToBytes(string input, int length, Encoding encode)
        {
            byte[] result = new byte[length];

            if (input != null)
            {
                byte[] tmp = encode.GetBytes(input);
                for (int i = 0; i < result.Length && i < tmp.Length; i++)
                {
                    result[i] = tmp[i];
                }
            }

            return result;
        }

        public static FileType PeekFileType(string fileName)
        {
            int irFileType = 0;
            int result = IRtekACFileSDK.ac_file_peek_file_type(fileName, ref irFileType);
            if (result == IRtekACFileSDK.AC_FILE_EC_OK)
            {
                switch (irFileType)
                {
                    case IRtekACFileSDK.AC_FILE_PICTURE:
                        return FileType.AcousticPicture;
                    case IRtekACFileSDK.AC_FILE_VIDEO:
                        return FileType.AcousticVideo;
                    case IRtekACFileSDK.AC_FILE_OTHER:
                        return FileType.Other;
                    default:
                        return FileType.Other;
                }
            }
            else
            {
                return FileType.Other;
            }
        }
    }

    //WAV头部结构-PCM格式
    [StructLayout(LayoutKind.Sequential)]
    public struct WavPcmFileHeader
    {
        private readonly RIFF riff;
        private readonly Format format;
        private readonly Data data;

        public WavPcmFileHeader(ushort nCh, uint nSampleRate, ushort bitsPerSample, uint dataSize)
        {
            riff = new RIFF((uint)(36 + dataSize));
            format = new Format(nCh, nSampleRate, nSampleRate * nCh * bitsPerSample / 8, (ushort)(nCh * bitsPerSample / 8), bitsPerSample);
            data = new Data(dataSize);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RIFF
    {
        private readonly byte _r;
        private readonly byte _i;
        private readonly byte _f;
        private readonly byte _t;
        private readonly uint _fileLength;
        private readonly byte _w;
        private readonly byte _a;
        private readonly byte _v;
        private readonly byte _e;

        public static readonly RIFF DEFAULT = new RIFF(0);

        public RIFF(uint fileLength)
        {
            _r = (byte)'R';
            _i = (byte)'I';
            _f = (byte)'F';
            _t = (byte)'F';
            _fileLength = fileLength;
            _w = (byte)'W';
            _a = (byte)'A';
            _v = (byte)'V';
            _e = (byte)'E';
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Format
    {
        private readonly byte f;
        private readonly byte m;
        private readonly byte t;
        private readonly byte s;
        private readonly uint blockSize; //16
        private ushort _formatTag; //1
        private ushort _channels;//1
        private uint _samplesPerSec;//22050
        private uint _avgBytesPerSec;//44100
        private ushort _blockAlign;//2
        private ushort _bitsPerSample;//16

        public Format(ushort channelCount, uint samplesPerSec, uint avgBytesPerSec, ushort blockAlign, ushort bitsPerSample)
        {
            f = (byte)'f';
            m = (byte)'m';
            t = (byte)'t';
            s = (byte)' ';
            blockSize = 16;
            _formatTag = 1;//PCM
            _channels = channelCount;
            _samplesPerSec = samplesPerSec;
            _avgBytesPerSec = avgBytesPerSec;
            _blockAlign = blockAlign;
            _bitsPerSample = bitsPerSample;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Data
    {
        public static readonly Data DEFAULT = new Data(0);

        private readonly byte d;
        private readonly byte a;
        private readonly byte t;
        private readonly byte a2;
        private readonly uint _dataLength;
        public Data(uint dataLength)
        {
            d = (byte)'d';
            a = (byte)'a';
            t = (byte)'t';
            a2 = (byte)'a';
            _dataLength = dataLength;
        }
    }
}
