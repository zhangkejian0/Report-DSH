// Copyright(c) IRtek.Co.Ltd. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Amur.ThermalImageFile;

namespace Amur.IRtekFileSDK
{
    internal partial class ThermalImageFile : IThermalImageFile
    {
        #region Fields
        private object _thermalImageCacheLock = new object();
        private object _paletteImageCacheLock = new object();

        private bool _isMarkersSynced;
        private IMarkerCollection _markers;
        private bool _isDeltasSynced;
        private IDeltaCollection _deltas;
        private Reference _referenceCache;
        private Image _thermalImageCache;
        private Image _paletteImageCache;
        private Image _visualImageCache;
        private Image _coverImageCache;
        private bool _isModified;

        private const byte TRUE = 0x01;
        private const byte FALSE = 0x00;

        private const string USER_DATA_KEY_FAVORITE = "FAVORITE";
        private const string USER_DATA_KEY_TAG = "TAG";
        private const string USER_DATA_KEY_GROUPFILES = "GROUPFILES";
        private const string USER_DATA_KEY_TIMERFILE = "TIMERFILE";

        private const string USER_DATA_SEPARATOR = "\n";
        private const string USER_DATA_FAVORITE_YES = "Y";
        private const string USER_DATA_FAVORITE_NO = "N";

        private const string SPOT_PREFIX = "Sp";
        private const string AREA_PREFIX = "Ar";
        private const string POLYLINE_PREFIX = "Li";
        private const string DELTA_PREFIX = "De";

        #endregion

        #region Constructors
        public ThermalImageFile()
        {
            HImage = IntPtr.Zero;
            IsModified = false;

            _isMarkersSynced = false;
            _markers = new MarkerCollection(this);
            _isDeltasSynced = false;
            _deltas = new DeltaCollection(this);
            _referenceCache = Reference.DEFAULT;
            _thermalImageCache = null;
            _paletteImageCache = null;
            _visualImageCache = null;
            _coverImageCache = null;
        }
        #endregion

        #region Events
        public event MarkerEventHandler MarkerAdded = null;
        public event MarkerEventHandler MarkerChanged = null;
        public event MarkerEventHandler MarkerRemoved = null;
        public event DeltaEventHandler DeltaAdded = null;
        public event DeltaEventHandler DeltaChanged = null;
        public event DeltaEventHandler DeltaRemoved = null;
        public event EventHandler ThermalParamsChanged = null;
        public event EventHandler ImageChanged = null;
        public event EventHandler PaletteChanged = null;
        public event EventHandler GainChanged = null;
        public event EventHandler RotationTypeChanged = null;
        public event EventHandler MirrorTypeChanged = null;
        public event EventHandler ViewInfoChanged = null;
        public event EventHandler MappingInfoChanged = null;
        public event EventHandler AlarmChanged = null;
        public event EventHandler ReferenceChanged = null;
        public event EventHandler FrameIndexChanged = null;
        public event EventHandler ModifiedStatusChanged = null;
        #endregion

        #region Properties
        protected IntPtr HImage { get; set; }

        public bool IsModified
        {
            get => _isModified;
            protected set 
            {
                _isModified = value;
                HandleModifiedStatusChangedEvent();
            }
        }

        public bool IsValid
        {
            get { return HImage != IntPtr.Zero; }
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
                int result = IRtekFileSDK.ir_file_get_file_type(HImage, ref irFileType);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    switch (irFileType)
                    {
                        case IRtekFileSDK.IR_FILE_THERMAL_PICTURE:
                            return FileType.ThermalPicture;
                        case IRtekFileSDK.IR_FILE_THERMAL_VIDEO:
                            return FileType.ThermalVideo;
                        case IRtekFileSDK.IR_FILE_GRID_THERMAL_PICTURE:
                            return FileType.GridThermalPicture;
                        default:
                            return FileType.Other;
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_file_type error code {0}.", result));
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
                int result = IRtekFileSDK.ir_file_get_file_version(HImage, ref irFileVersion);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return irFileVersion;
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_file_version error code {0}.", result));
                }
            }
        }

        public string FileName
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return string.Empty;
                }

                IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
                int result = IRtekFileSDK.ir_file_get_file_name(HImage, ref irBuffer);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                        result = IRtekFileSDK.ir_file_get_file_name(HImage, ref irBuffer);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            byte[] buffer = new byte[irBuffer.length];
                            Marshal.Copy(irBuffer.buffer, buffer, 0, (int)irBuffer.length);
                            return StringConvertHelper.BytesToString(buffer, Encoding.Default);
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_file_name error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irBuffer.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_file_name error code {0}.", result));
                }
            }
        }

        public IMarkerCollection Markers
        {
            get { return _markers; }
            private set { _markers = value; }
        }

        public IDeltaCollection Deltas
        {
            get { return _deltas; }
            private set { _deltas = value; }
        }

        private bool IsMarkersSynced
        {
            get { return _isMarkersSynced; }
        }

        private bool IsDeltasSynced
        {
            get { return _isDeltasSynced; }
        }
        #endregion

        #region Methods
        public ThermalParams GetThermalParams()
        {
            if (HImage == IntPtr.Zero)
            {
                return ThermalParams.DEFAULT;
            }

            IRtekFileSDK.ir_file_thermal_params irParams = new IRtekFileSDK.ir_file_thermal_params();
            int result = IRtekFileSDK.ir_file_get_thermal_params(HImage, ref irParams);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                ThermalParams value = new ThermalParams();
                value.ReflTemp = irParams.reflTemp;
                value.AtmTemp = irParams.atmTemp;
                value.Distance = irParams.distance;
                value.RelHumidity = irParams.relHumidity;
                value.Emissivity = irParams.emissivity;
                value.ExtOptTemp = irParams.extOptTemp;
                value.ExtOptTrans = irParams.extOptTrans;
                return value;
            }
            else
            {
                return ThermalParams.DEFAULT;
            }
        }

        public bool SetThermalParams(ThermalParams value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_thermal_params irParams = new IRtekFileSDK.ir_file_thermal_params();
            irParams.reflTemp = value.ReflTemp;
            irParams.atmTemp = value.AtmTemp;
            irParams.distance = value.Distance;
            irParams.relHumidity = value.RelHumidity;
            irParams.emissivity = value.Emissivity;
            irParams.extOptTemp = value.ExtOptTemp;
            irParams.extOptTrans = value.ExtOptTrans;
            int result = IRtekFileSDK.ir_file_set_thermal_params(HImage, ref irParams);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleThermalParamsChangedEvent();

            Gain gain = GetGain();
            if (gain.Mode == GainMode.Auto)
            {
                HandleGainChangedEvent();
            }

            return true;
        }

        public CameraInfo GetCameraInfo()
        {
            if (HImage == IntPtr.Zero)
            {
                return CameraInfo.DEFAULT;
            }

            IRtekFileSDK.ir_file_camera_info irCameraInfo = new IRtekFileSDK.ir_file_camera_info();
            int result = IRtekFileSDK.ir_file_get_camera_info(HImage, ref irCameraInfo);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                CameraInfo value = new CameraInfo();
                value.Model = StringConvertHelper.BytesToString(irCameraInfo.model, Encoding.UTF8);
                value.SerialNo = StringConvertHelper.BytesToString(irCameraInfo.serialNo, Encoding.UTF8);
                value.Brand = StringConvertHelper.BytesToString(irCameraInfo.brand, Encoding.UTF8);
                value.Company = StringConvertHelper.BytesToString(irCameraInfo.company, Encoding.UTF8);
                if (irCameraInfo.logoLength != 0)
                {
                    try
                    {
                        irCameraInfo.logo = Marshal.AllocHGlobal((int)irCameraInfo.logoLength);
                        IRtekFileSDK.ir_file_get_camera_info(HImage, ref irCameraInfo);
                        byte[] buffer = new byte[irCameraInfo.logoLength];
                        Marshal.Copy(irCameraInfo.logo, buffer, 0, (int)irCameraInfo.logoLength);
                        MemoryStream ms = new MemoryStream(buffer);
                        value.Logo = Image.FromStream(ms);
                    }
                    catch (Exception)
                    {
                        value.Logo = null;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irCameraInfo.logo);
                    }
                }
                else
                {
                    value.Logo = null;
                }

                return value;
            }
            else
            {
                return CameraInfo.DEFAULT;
            }
        }

        public bool SetCameraInfo(CameraInfo value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_camera_info irCameraInfo = new IRtekFileSDK.ir_file_camera_info();
            irCameraInfo.model = StringConvertHelper.StringToBytes(value.Model, IRtekFileSDK.MODEL_LEN, Encoding.UTF8);
            irCameraInfo.serialNo = StringConvertHelper.StringToBytes(value.SerialNo, IRtekFileSDK.SERIAL_NO_LEN, Encoding.UTF8);
            irCameraInfo.brand = StringConvertHelper.StringToBytes(value.Brand, IRtekFileSDK.BRAND_LEN, Encoding.UTF8);
            irCameraInfo.company = StringConvertHelper.StringToBytes(value.Company, IRtekFileSDK.COMPANY_LEN, Encoding.UTF8);
            if (value.Logo != null)
            {
                MemoryStream ms = new MemoryStream();
                value.Logo.Save(ms, ImageFormat.Jpeg);
                byte[] buffer = ms.GetBuffer();

                try
                {
                    irCameraInfo.logoLength = (uint)buffer.Length;
                    irCameraInfo.logo = Marshal.AllocHGlobal((int)irCameraInfo.logoLength);
                    Marshal.Copy(buffer, 0, irCameraInfo.logo, (int)irCameraInfo.logoLength);
                }
                finally
                {
                    Marshal.FreeHGlobal(irCameraInfo.logo);
                }
            }

            int result = IRtekFileSDK.ir_file_set_camera_info(HImage, ref irCameraInfo);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public Image GetCoverImage()
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            if (_coverImageCache != null)
            {
                return _coverImageCache.Clone() as Image;
            }

            IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
            IRtekFileSDK.ir_file_get_cover_image(HImage, ref irBuffer);
            if (irBuffer.length != 0)
            {
                try
                {
                    irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                    IRtekFileSDK.ir_file_get_cover_image(HImage, ref irBuffer);
                    byte[] buffer = new byte[irBuffer.length];
                    Marshal.Copy(irBuffer.buffer, buffer, 0, (int)irBuffer.length);
                    MemoryStream ms = new MemoryStream(buffer);
                    _coverImageCache = Image.FromStream(ms);
                    return _coverImageCache.Clone() as Image;
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(irBuffer.buffer);
                }
            }
            else
            {
                return null;
            }
        }

        public bool SetCoverImage(Image value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
            try
            {
                if (value != null)
                {
                    MemoryStream ms = new MemoryStream();
                    value.Save(ms, ImageFormat.Jpeg);
                    byte[] buffer = ms.GetBuffer();

                    irBuffer.length = (uint)buffer.Length;
                    irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                    Marshal.Copy(buffer, 0, irBuffer.buffer, (int)irBuffer.length);
                }

                int result = IRtekFileSDK.ir_file_set_cover_image(HImage, ref irBuffer);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                _coverImageCache = null;
                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(irBuffer.buffer);
            }

            return true;
        }

        public Image GetVisualImage()
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            if (_visualImageCache != null)
            {
                return _visualImageCache.Clone() as Image;
            }

            IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
            IRtekFileSDK.ir_file_get_visual_image(HImage, ref irBuffer);
            if (irBuffer.length != 0)
            {
                try
                {
                    irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                    IRtekFileSDK.ir_file_get_visual_image(HImage, ref irBuffer);
                    byte[] buffer = new byte[irBuffer.length];
                    Marshal.Copy(irBuffer.buffer, buffer, 0, (int)irBuffer.length);
                    MemoryStream ms = new MemoryStream(buffer);
                    _visualImageCache = Image.FromStream(ms);
                    return _visualImageCache.Clone() as Image;
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(irBuffer.buffer);
                }
            }
            else
            {
                return null;
            }
        }

        public bool SetVisualImage(Image value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
            try
            {
                if (value != null)
                {
                    MemoryStream ms = new MemoryStream();
                    value.Save(ms, ImageFormat.Jpeg);
                    byte[] buffer = ms.GetBuffer();

                    irBuffer.length = (uint)buffer.Length;
                    irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                    Marshal.Copy(buffer, 0, irBuffer.buffer, (int)irBuffer.length);
                }

                int result = IRtekFileSDK.ir_file_set_visual_image(HImage, ref irBuffer);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                _visualImageCache = null;

                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(irBuffer.buffer);
            }

            return true;
        }

        public Image GetPaletteImage()
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            lock (_paletteImageCacheLock)
            {
                if (_paletteImageCache != null)
                {
                    return _paletteImageCache.Clone() as Image;
                }

                try
                {
                    int width = 1;
                    int height = IRtekFileSDK.COLORS_LEN;
                    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    IRtekFileSDK.ir_file_image irImage = new IRtekFileSDK.ir_file_image();
                    irImage.offset = (uint)(bmpData.Stride - 3 * width);
                    irImage.data = bmpData.Scan0;
                    IRtekFileSDK.ir_file_get_palette_image(HImage, ref irImage);
                    bitmap.UnlockBits(bmpData);
                    _paletteImageCache = bitmap;
                    return _paletteImageCache.Clone() as Image;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public Image GetThermalImage()
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            lock (_thermalImageCacheLock)
            {
                if (_thermalImageCache != null)
                {
                    return _thermalImageCache.Clone() as Image;
                }

                try
                {
                    Size thermalSize = GetThermalSize();
                    int width = thermalSize.Width;
                    int height = thermalSize.Height;
                    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    IRtekFileSDK.ir_file_image irImage = new IRtekFileSDK.ir_file_image();
                    irImage.offset = (uint)(bmpData.Stride - 3 * width);
                    irImage.data = bmpData.Scan0;
                    IRtekFileSDK.ir_file_get_thermal_image(HImage, ref irImage);
                    bitmap.UnlockBits(bmpData);
                    _thermalImageCache = bitmap;
                    return _thermalImageCache.Clone() as Image;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public LensInfo GetLensInfo()
        {
            if (HImage == IntPtr.Zero)
            {
                return LensInfo.DEFAULT;
            }

            IRtekFileSDK.ir_file_lens_info_v2 irLensInfo = new IRtekFileSDK.ir_file_lens_info_v2();
            int result = IRtekFileSDK.ir_file_get_lens_info_v2(HImage, ref irLensInfo);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                LensInfo value = new LensInfo();
                value.Name = StringConvertHelper.BytesToString(irLensInfo.name, Encoding.UTF8);
                value.FocalLength = irLensInfo.focalLength;
                value.HFov = irLensInfo.hfov;
                value.Band = StringConvertHelper.BytesToString(irLensInfo.band, Encoding.UTF8);
                return value;
            }
            else
            {
                return LensInfo.DEFAULT;
            }
        }

        public bool SetLensInfo(LensInfo value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_lens_info_v2 irLensInfo = new IRtekFileSDK.ir_file_lens_info_v2();
            irLensInfo.name = StringConvertHelper.StringToBytes(value.Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            irLensInfo.focalLength = value.FocalLength;
            irLensInfo.hfov = value.HFov;
            irLensInfo.band = StringConvertHelper.StringToBytes(value.Band, IRtekFileSDK.NAME_LEN, Encoding.UTF8);

            int result = IRtekFileSDK.ir_file_set_lens_info_v2(HImage, ref irLensInfo);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public TempRange GetTempRange()
        {
            if (HImage == IntPtr.Zero)
            {
                return TempRange.DEFAULT;
            }

            IRtekFileSDK.ir_file_temp_range irTempRange = new IRtekFileSDK.ir_file_temp_range();
            int result = IRtekFileSDK.ir_file_get_temp_range(HImage, ref irTempRange);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                TempRange value = new TempRange();
                value.Min = irTempRange.min;
                value.Max = irTempRange.max;
                return value;
            }
            else
            {
                return TempRange.DEFAULT;
            }
        }

        public bool SetTempRange(TempRange value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_temp_range irTempRange = new IRtekFileSDK.ir_file_temp_range();
            irTempRange.max = value.Max;
            irTempRange.min = value.Min;

            int result = IRtekFileSDK.ir_file_set_temp_range(HImage, ref irTempRange);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public Size GetThermalSize()
        {
            if (HImage == IntPtr.Zero)
            {
                return new Size(0, 0);
            }

            IRtekFileSDK.ir_file_size irSize = new IRtekFileSDK.ir_file_size();
            int result = IRtekFileSDK.ir_file_get_thermal_size(HImage, ref irSize);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                Size value = new Size(irSize.width, irSize.height);
                return value;
            }
            else
            {
                return new Size(0, 0);
            }
        }

        public bool SetThermalSize(Size value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_size irSize = new IRtekFileSDK.ir_file_size();
            irSize.height = (short)value.Height;
            irSize.width = (short)value.Width;

            int result = IRtekFileSDK.ir_file_set_thermal_size(HImage, ref irSize);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public GeoLocation GetGeoLocation()
        {
            if (HImage == IntPtr.Zero)
            {
                return GeoLocation.DEFAULT;
            }

            IRtekFileSDK.ir_file_geo_location irGeoLocation = new IRtekFileSDK.ir_file_geo_location();
            int result = IRtekFileSDK.ir_file_get_geo_location(HImage, ref irGeoLocation);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                GeoLocation value = new GeoLocation();
                value.IsGPSValid = irGeoLocation.isGPSValid == TRUE;
                value.Longitude = irGeoLocation.longitude;
                value.Latitude = irGeoLocation.latitude;
                value.Altitude = irGeoLocation.altitude;
                value.IsCompassValid = irGeoLocation.isCompassValid == TRUE;
                value.Direction = irGeoLocation.direction;
                return value;
            }
            else
            {
                return GeoLocation.DEFAULT;
            }
        }

        public bool SetGeoLocation(GeoLocation value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_geo_location irGeoLocation = new IRtekFileSDK.ir_file_geo_location();
            irGeoLocation.isGPSValid = value.IsGPSValid ? TRUE : FALSE;
            irGeoLocation.longitude = value.Longitude;
            irGeoLocation.latitude = value.Latitude;
            irGeoLocation.altitude = value.Altitude;
            irGeoLocation.isCompassValid = value.IsCompassValid ? TRUE : FALSE;
            irGeoLocation.direction = value.Direction;

            int result = IRtekFileSDK.ir_file_set_geo_location(HImage, ref irGeoLocation);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public DateTime GetShotTime()
        {
            if (HImage == IntPtr.Zero)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            ulong lSpan = 0;
            int result = IRtekFileSDK.ir_file_get_shot_time(HImage, ref lSpan);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                try
                {
                    DateTime shotTime = dtStart + new TimeSpan((long)lSpan * TimeSpan.TicksPerMillisecond);
                    return shotTime;
                }
                catch { }
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        public bool SetShotTime(DateTime value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan tsSpan = value.ToUniversalTime() - dtStart;
            ulong lSpan = (ulong)tsSpan.Ticks / TimeSpan.TicksPerMillisecond;
            int result = IRtekFileSDK.ir_file_set_shot_time(HImage, lSpan);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public DateTime GetModifiedTime()
        {
            if (HImage == IntPtr.Zero)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            ulong lSpan = 0;
            int result = IRtekFileSDK.ir_file_get_modified_time(HImage, ref lSpan);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                try
                {
                    DateTime shotTime = dtStart + new TimeSpan((long)lSpan * TimeSpan.TicksPerMillisecond);
                    return shotTime;
                }
                catch { }
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        public AudioAnnotation GetAudioAnnotation()
        {
            if (HImage == IntPtr.Zero)
            {
                return new AudioAnnotation(false, AudioType.MP3, null);
            }

            IRtekFileSDK.ir_file_audio irAudio = new IRtekFileSDK.ir_file_audio();
            int result = IRtekFileSDK.ir_file_get_audio(HImage, ref irAudio);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    irAudio.buffer = Marshal.AllocHGlobal((int)irAudio.length);
                    result = IRtekFileSDK.ir_file_get_audio(HImage, ref irAudio);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        bool isValid = irAudio.length != 0;
                        AudioType type;
                        switch (irAudio.type)
                        {
                            case IRtekFileSDK.IR_FILE_WAV:
                                type = AudioType.Wav;
                                break;
                            case IRtekFileSDK.IR_FILE_MP3:
                                type = AudioType.MP3;
                                break;
                            default:
                                return new AudioAnnotation(false, AudioType.MP3, null);
                        }
                        byte[] buffer = new byte[irAudio.length];
                        Marshal.Copy(irAudio.buffer, buffer, 0, (int)irAudio.length);
                        return new AudioAnnotation(isValid, type, buffer);
                    }
                    else
                    {
                        return new AudioAnnotation(false, AudioType.MP3, null);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irAudio.buffer);
                }
            }
            else
            {
                return new AudioAnnotation(false, AudioType.MP3, null);
            }
        }

        public bool SetAudioAnnotation(AudioAnnotation value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_audio irAudio = new IRtekFileSDK.ir_file_audio();
            try
            {
                switch (value.Type)
                {
                    case AudioType.Wav:
                        irAudio.type = IRtekFileSDK.IR_FILE_WAV;
                        break;
                    case AudioType.MP3:
                        irAudio.type = IRtekFileSDK.IR_FILE_MP3;
                        break;
                    default:
                        return false;
                }

                if (value.Data == null)
                {
                    return false;
                }

                irAudio.length = (uint)value.Data.Length;
                irAudio.buffer = Marshal.AllocHGlobal((int)irAudio.length);
                Marshal.Copy(value.Data, 0, irAudio.buffer, (int)irAudio.length);
                int result = IRtekFileSDK.ir_file_set_audio(HImage, ref irAudio);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(irAudio.buffer);
            }

            return true;
        }

        public string GetNoteAnnotation()
        {
            if (HImage == IntPtr.Zero)
            {
                return string.Empty;
            }

            IRtekFileSDK.ir_file_buffer irNote = new IRtekFileSDK.ir_file_buffer();
            int result = IRtekFileSDK.ir_file_get_note(HImage, ref irNote);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    irNote.buffer = Marshal.AllocHGlobal((int)irNote.length);
                    result = IRtekFileSDK.ir_file_get_note(HImage, ref irNote);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        byte[] buffer = new byte[irNote.length];
                        Marshal.Copy(irNote.buffer, buffer, 0, (int)irNote.length);
                        return StringConvertHelper.BytesToString(buffer, Encoding.UTF8);
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irNote.buffer);
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public bool SetNoteAnnotation(string value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_buffer irNote = new IRtekFileSDK.ir_file_buffer();
            byte[] buffer = StringConvertHelper.StringToBytes(value, Encoding.UTF8);
            try
            {
                irNote.length = (uint)buffer.Length;
                irNote.buffer = Marshal.AllocHGlobal((int)irNote.length);
                Marshal.Copy(buffer, 0, irNote.buffer, (int)irNote.length);
                int result = IRtekFileSDK.ir_file_set_note(HImage, ref irNote);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(irNote.buffer);
            }

            return true;
        }

        public string GetTagAnnotation()
        {
            if (HImage == IntPtr.Zero)
            {
                return string.Empty;
            }

            return GetUserData(USER_DATA_KEY_TAG);
        }

        public bool SetTagAnnotation(string tagJson)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            return SetUserData(USER_DATA_KEY_TAG, tagJson);
        }

        public bool GetFavorite()
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            return USER_DATA_FAVORITE_YES.Equals(GetUserData(USER_DATA_KEY_FAVORITE));
        }

        public bool SetFavorite(bool value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            return SetUserData(USER_DATA_KEY_FAVORITE, value ? USER_DATA_FAVORITE_YES : USER_DATA_FAVORITE_NO);
        }

        public string[] GetGroupFiles()
        {
            if (HImage == IntPtr.Zero)
            {
                return new string[0];
            }

            return GetUserData(USER_DATA_KEY_GROUPFILES).Split(new string[] { USER_DATA_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool SetGroupFiles(string[] value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            return SetUserData(USER_DATA_KEY_TAG, string.Join(USER_DATA_SEPARATOR, value));
        }

        public TimerFileInfo GetTimerFileInfo()
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            string sTimerFileInfo = GetUserData(USER_DATA_KEY_TIMERFILE);
            if (!string.IsNullOrEmpty(sTimerFileInfo))
            {
                string[] s = sTimerFileInfo.Split(new string[] { USER_DATA_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
                return new TimerFileInfo(s[0], Convert.ToInt32(s[1]));
            }
            return null;
        }

        public bool SetTimerFileInfo(TimerFileInfo value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            string sTimerFileInfo = string.Empty;
            if (value != null)
            {
                sTimerFileInfo = string.Format("{0}\n{1:D}", value.GUID, value.No);
            }

            return SetUserData(USER_DATA_KEY_TIMERFILE, sTimerFileInfo);
        }

        public Reference GetReference()
        {
            if (HImage == IntPtr.Zero)
            {
                return Reference.DEFAULT;
            }

            IRtekFileSDK.ir_file_reference irReference = new IRtekFileSDK.ir_file_reference();
            int result = IRtekFileSDK.ir_file_get_reference(HImage, ref irReference);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                Reference reference = new Reference();
                reference.Enabled = irReference.enabled == TRUE;
                reference.Name = StringConvertHelper.BytesToString(irReference.name, Encoding.UTF8);
                switch (irReference.valueType)
                {
                    case IRtekFileSDK.IR_FILE_MAX:
                    default:
                        reference.ValueType = Amur.ThermalImageFile.ValueType.Max;
                        break;
                    case IRtekFileSDK.IR_FILE_MIN:
                        reference.ValueType = Amur.ThermalImageFile.ValueType.Min;
                        break;
                    case IRtekFileSDK.IR_FILE_AVG:
                        reference.ValueType = Amur.ThermalImageFile.ValueType.Avg;
                        break;
                }
                reference.Temp = irReference.temp;
                return reference;
            }
            else
            {
                return Reference.DEFAULT;
            }
        }

        public float GetReferenceTemperature()
        {
            if (HImage == IntPtr.Zero)
            {
                return 0.0F;
            }

            float irReferenceTemp = 0;
            int result = IRtekFileSDK.ir_file_get_reference_temp(HImage, ref irReferenceTemp);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                return irReferenceTemp;
            }
            else
            {
                return 0.0F;
            }
        }

        public bool SetReference(Reference value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_reference irReference = new IRtekFileSDK.ir_file_reference();
            irReference.enabled = value.Enabled ? TRUE : FALSE;
            irReference.name = StringConvertHelper.StringToBytes(value.Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            switch (value.ValueType)
            {
                case Amur.ThermalImageFile.ValueType.Max:
                default:
                    irReference.valueType = IRtekFileSDK.IR_FILE_MAX;
                    break;
                case Amur.ThermalImageFile.ValueType.Min:
                    irReference.valueType = IRtekFileSDK.IR_FILE_MIN;
                    break;
                case Amur.ThermalImageFile.ValueType.Avg:
                    irReference.valueType = IRtekFileSDK.IR_FILE_AVG;
                    break;
            }
            irReference.temp = value.Temp;
            int result = IRtekFileSDK.ir_file_set_reference(HImage, ref irReference);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleReferenceChangedEvent();

            return true;
        }

        public Gain GetGain()
        {
            if (HImage == IntPtr.Zero)
            {
                return Gain.DEFAULT;
            }

            IRtekFileSDK.ir_file_gain irGain = new IRtekFileSDK.ir_file_gain();
            int result = IRtekFileSDK.ir_file_get_gain(HImage, ref irGain);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                Gain gain = new Gain();
                switch (irGain.mode)
                {
                    case IRtekFileSDK.IR_FILE_FIXED:
                        gain.Mode = GainMode.Fixed;
                        break;
                    case IRtekFileSDK.IR_FILE_AUTO:
                    default:
                        gain.Mode = GainMode.Auto;
                        break;
                    case IRtekFileSDK.IR_FILE_TOUCH:
                        gain.Mode = GainMode.Touch;
                        break;
                }
                gain.Max = irGain.max;
                gain.Min = irGain.min;
                gain.FixedSpan = irGain.fixedSpan == TRUE ? true : false;
                gain.Span = irGain.span;
                return gain;
            }
            else
            {
                return Gain.DEFAULT;
            }
        }

        public bool SetGain(Gain value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_gain irGain = new IRtekFileSDK.ir_file_gain();
            switch (value.Mode)
            {
                case GainMode.Fixed:
                    irGain.mode = IRtekFileSDK.IR_FILE_FIXED;
                    break;
                case GainMode.Auto:
                default:
                    irGain.mode = IRtekFileSDK.IR_FILE_AUTO;
                    break;
                case GainMode.Touch:
                    irGain.mode = IRtekFileSDK.IR_FILE_TOUCH;
                    break;
            }
            irGain.max = value.Max;
            irGain.min = value.Min;
            irGain.fixedSpan = value.FixedSpan ? TRUE : FALSE;
            irGain.span = value.Span;
            int result = IRtekFileSDK.ir_file_set_gain(HImage, ref irGain);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleGainChangedEvent();

            return true;
        }

        public Alarm GetAlarm()
        {
            if (HImage == IntPtr.Zero)
            {
                return Alarm.DEFAULT;
            }

            IRtekFileSDK.ir_file_alarm_v2 irAlarmV2 = new IRtekFileSDK.ir_file_alarm_v2();
            int result = IRtekFileSDK.ir_file_get_alarm_v2(HImage, ref irAlarmV2);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                Alarm alarm = new Alarm();
                switch (irAlarmV2.type)
                {
                    case IRtekFileSDK.IR_FILE_NORMAL:
                    default:
                        alarm.Type = AlarmType.Normal;
                        break;
                    case IRtekFileSDK.IR_FILE_BELOW:
                        alarm.Type = AlarmType.Below;
                        break;
                    case IRtekFileSDK.IR_FILE_ABOVE:
                        alarm.Type = AlarmType.Above;
                        break;
                    case IRtekFileSDK.IR_FILE_BETWEEN:
                        alarm.Type = AlarmType.Between;
                        break;
                    case IRtekFileSDK.IR_FILE_MAGIC_THERMAL:
                        alarm.Type = AlarmType.MagicThermal;
                        break;
                    case IRtekFileSDK.IR_FILE_OUTSIDE:
                        alarm.Type = AlarmType.Outside;
                        break;
                    case IRtekFileSDK.IR_FILE_HUMIDITY:
                        alarm.Type = AlarmType.Humidity;
                        break;
                    case IRtekFileSDK.IR_FILE_INSULATION:
                        alarm.Type = AlarmType.Insulation;
                        break;
                }
                alarm.Custom = irAlarmV2.custom == TRUE ? true : false;
                alarm.Color = Color.FromArgb((int)irAlarmV2.color);
                alarm.Max = irAlarmV2.max;
                alarm.Min = irAlarmV2.min;
                alarm.RHLimit = irAlarmV2.rhLimit;
                alarm.IndoorTemp = irAlarmV2.indoorTemp;
                alarm.OutdoorTemp = irAlarmV2.outdoorTemp;
                alarm.HeatIndex = irAlarmV2.heatIndex;
                alarm.Threshold = irAlarmV2.threshold;
                return alarm;
            }
            else
            {
                return Alarm.DEFAULT;
            }
        }

        public bool SetAlarm(Alarm value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_alarm irAlarm = new IRtekFileSDK.ir_file_alarm();
            switch (value.Type)
            {
                case AlarmType.Normal:
                default:
                    irAlarm.type = IRtekFileSDK.IR_FILE_NORMAL;
                    break;
                case AlarmType.Below:
                    irAlarm.type = IRtekFileSDK.IR_FILE_BELOW;
                    break;
                case AlarmType.Above:
                    irAlarm.type = IRtekFileSDK.IR_FILE_ABOVE;
                    break;
                case AlarmType.Between:
                    irAlarm.type = IRtekFileSDK.IR_FILE_BETWEEN;
                    break;
                case AlarmType.MagicThermal:
                    irAlarm.type = IRtekFileSDK.IR_FILE_MAGIC_THERMAL;
                    break;
                case AlarmType.Outside:
                    irAlarm.type = IRtekFileSDK.IR_FILE_OUTSIDE;
                    break;
                case AlarmType.Humidity:
                    irAlarm.type = IRtekFileSDK.IR_FILE_HUMIDITY;
                    break;
                case AlarmType.Insulation:
                    irAlarm.type = IRtekFileSDK.IR_FILE_INSULATION;
                    break;
            }
            irAlarm.custom = value.Custom ? TRUE : FALSE;
            irAlarm.color = (uint)value.Color.ToArgb();
            irAlarm.max = value.Max;
            irAlarm.min = value.Min;
            irAlarm.rhLimit = value.RHLimit;
            irAlarm.indoorTemp = value.IndoorTemp;
            irAlarm.outdoorTemp = value.OutdoorTemp;
            irAlarm.heatIndex = value.HeatIndex;

            int result = IRtekFileSDK.ir_file_set_alarm(HImage, ref irAlarm);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleAlarmChangedEvent();

            return true;
        }

        public Palette GetPalette()
        {
            if (HImage == IntPtr.Zero)
            {
                return Palette.DEFAULT;
            }

            IRtekFileSDK.ir_file_palette irPalette = new IRtekFileSDK.ir_file_palette();
            int result = IRtekFileSDK.ir_file_get_palette(HImage, ref irPalette);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                Palette palette = new Palette();
                palette.RefNo = irPalette.refNo;
                palette.Colors = irPalette.colors;
                palette.Inverted = irPalette.inverted == TRUE ? true : false;
                return palette;
            }
            else
            {
                return Palette.DEFAULT;
            }
        }

        public PaletteV2 GetPaletteV2()
        {
            if (HImage == IntPtr.Zero)
            {
                return PaletteV2.DEFAULT;
            }

            IRtekFileSDK.ir_file_palette_v2 irPalette = new IRtekFileSDK.ir_file_palette_v2();
            int result = IRtekFileSDK.ir_file_get_palette_v2(HImage, ref irPalette);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                PaletteV2 palette = new PaletteV2();
                palette.RefNo = irPalette.refNo;
                palette.SrcColors = irPalette.srcColors;
                palette.DestColors = irPalette.destColors;
                palette.Inverted = irPalette.inverted == TRUE ? true : false;
                return palette;
            }
            else
            {
                return PaletteV2.DEFAULT;
            }
        }

        public bool SetPalette(Palette value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_palette irPalette = new IRtekFileSDK.ir_file_palette();
            irPalette.refNo = value.RefNo;
            irPalette.colors = value.Colors;
            irPalette.inverted = value.Inverted ? TRUE : FALSE;

            int result = IRtekFileSDK.ir_file_set_palette(HImage, ref irPalette);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandlePaletteChangedEvent();

            return true;
        }

        public bool SetPaletteV2(PaletteV2 value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_palette_v2 irPaletteV2 = new IRtekFileSDK.ir_file_palette_v2();
            irPaletteV2.refNo = value.RefNo;
            irPaletteV2.srcColors = value.SrcColors;
            irPaletteV2.destColors = value.DestColors;
            irPaletteV2.inverted = value.Inverted ? TRUE : FALSE;

            int result = IRtekFileSDK.ir_file_set_palette_v2(HImage, ref irPaletteV2);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandlePaletteChangedEvent();

            return true;
        }

        public ViewInfo GetViewInfo()
        {
            if (HImage == IntPtr.Zero)
            {
                return ViewInfo.DEFAULT;
            }

            IRtekFileSDK.ir_file_view_info irViewInfo = new IRtekFileSDK.ir_file_view_info();
            int result = IRtekFileSDK.ir_file_get_view_info(HImage, ref irViewInfo);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                ViewInfo viewInfo = new ViewInfo();
                switch (irViewInfo.type)
                {
                    case IRtekFileSDK.IR_FILE_THERMAL:
                    default:
                        viewInfo.Type = ViewType.Thermal;
                        break;
                    case IRtekFileSDK.IR_FILE_PIC_IN_PIC:
                        viewInfo.Type = ViewType.PicInPic;
                        break;
                    case IRtekFileSDK.IR_FILE_T_DEF:
                        viewInfo.Type = ViewType.T_DEF;
                        break;
                    case IRtekFileSDK.IR_FILE_FUSION:
                        viewInfo.Type = ViewType.Fusion;
                        break;
                }
                viewInfo.Max = irViewInfo.max;
                viewInfo.Min = irViewInfo.min;
                viewInfo.Alpha = irViewInfo.alpha;
                IRtekFileSDK.ir_file_rectangle_f area = irViewInfo.area;
                viewInfo.Area = new RectangleF(area.x, area.y, area.width, area.height);
                return viewInfo;
            }
            else
            {
                return ViewInfo.DEFAULT;
            }
        }

        public bool SetViewInfo(ViewInfo value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_view_info irViewInfo = new IRtekFileSDK.ir_file_view_info();
            switch (value.Type)
            {
                case ViewType.Thermal:
                default:
                    irViewInfo.type = IRtekFileSDK.IR_FILE_THERMAL;
                    break;
                case ViewType.PicInPic:
                    irViewInfo.type = IRtekFileSDK.IR_FILE_PIC_IN_PIC;
                    break;
                case ViewType.T_DEF:
                    irViewInfo.type = IRtekFileSDK.IR_FILE_T_DEF;
                    break;
                case ViewType.Fusion:
                    irViewInfo.type = IRtekFileSDK.IR_FILE_FUSION;
                    break;
            }
            irViewInfo.max = value.Max;
            irViewInfo.min = value.Min;
            irViewInfo.alpha = value.Alpha;
            RectangleF area = value.Area;
            IRtekFileSDK.ir_file_rectangle_f irArea = new IRtekFileSDK.ir_file_rectangle_f();
            irArea.x = area.X;
            irArea.y = area.Y;
            irArea.width = area.Width;
            irArea.height = area.Height;
            irViewInfo.area = irArea;

            int result = IRtekFileSDK.ir_file_set_view_info(HImage, ref irViewInfo);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleViewInfoChangedEvent();

            return true;
        }

        public MappingInfo GetMappingInfo()
        {
            if (HImage == IntPtr.Zero)
            {
                return MappingInfo.DEFAULT;
            }

            IRtekFileSDK.ir_file_mapping_info irMappingInfo = new IRtekFileSDK.ir_file_mapping_info();
            int result = IRtekFileSDK.ir_file_get_mapping_info(HImage, ref irMappingInfo);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                MappingInfo mappingInfo = new MappingInfo();
                mappingInfo.VisualSize = new Size(irMappingInfo.visualSize.width, irMappingInfo.visualSize.height);
                mappingInfo.MappingArea = new RectangleF(irMappingInfo.mappingArea.x, irMappingInfo.mappingArea.y, irMappingInfo.mappingArea.width, irMappingInfo.mappingArea.height);
                return mappingInfo;
            }
            else
            {
                return MappingInfo.DEFAULT;
            }
        }

        public bool SetMappingInfo(MappingInfo value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_mapping_info irMappingInfo = new IRtekFileSDK.ir_file_mapping_info();
            Size visualSize = value.VisualSize;
            IRtekFileSDK.ir_file_size irVisualSize = new IRtekFileSDK.ir_file_size();
            irVisualSize.width = (short)visualSize.Width;
            irVisualSize.height = (short)visualSize.Height;
            irMappingInfo.visualSize = irVisualSize;
            RectangleF mappingArea = value.MappingArea;
            IRtekFileSDK.ir_file_rectangle_f irMappingArea = new IRtekFileSDK.ir_file_rectangle_f();
            irMappingArea.x = mappingArea.X;
            irMappingArea.y = mappingArea.Y;
            irMappingArea.width = mappingArea.Width;
            irMappingArea.height = mappingArea.Height;
            irMappingInfo.mappingArea = irMappingArea;

            int result = IRtekFileSDK.ir_file_set_mapping_info(HImage, ref irMappingInfo);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleMappingInfoChangedEvent();

            return true;
        }

        public ZoomInfo GetZoomInfo()
        {
            if (HImage == IntPtr.Zero)
            {
                return ZoomInfo.DEFAULT;
            }

            IRtekFileSDK.ir_file_zoom_info irZoomInfo = new IRtekFileSDK.ir_file_zoom_info();
            int result = IRtekFileSDK.ir_file_get_zoom_info(HImage, ref irZoomInfo);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                ZoomInfo zoomInfo = new ZoomInfo();
                zoomInfo.Enabled = irZoomInfo.enabled == TRUE ? true : false;
                zoomInfo.Location = new PointF(irZoomInfo.location.x, irZoomInfo.location.y);
                zoomInfo.Ratio = irZoomInfo.ratio;
                return zoomInfo;
            }
            else
            {
                return ZoomInfo.DEFAULT;
            }
        }

        public bool SetZoomInfo(ZoomInfo value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_zoom_info irZoomInfo = new IRtekFileSDK.ir_file_zoom_info();
            irZoomInfo.enabled = value.Enabled ? TRUE : FALSE;
            PointF location = value.Location;
            IRtekFileSDK.ir_file_point_f irLocation = new IRtekFileSDK.ir_file_point_f();
            irLocation.x = location.X;
            irLocation.y = location.Y;
            irZoomInfo.location = irLocation;
            irZoomInfo.ratio = value.Ratio;

            int result = IRtekFileSDK.ir_file_set_zoom_info(HImage, ref irZoomInfo);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public ColorDistributionType GetColorDistribution()
        {
            if (HImage == IntPtr.Zero)
            {
                return ColorDistributionType.ADLinear;
            }

            int irColorDist = IRtekFileSDK.IR_FILE_AD_LINEAR;
            int result = IRtekFileSDK.ir_file_get_color_dist(HImage, ref irColorDist);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                ColorDistributionType colorDistribution;
                switch (irColorDist)
                {
                    case IRtekFileSDK.IR_FILE_T_TWB:
                        colorDistribution = ColorDistributionType.T_TWB;
                        break;
                    case IRtekFileSDK.IR_FILE_TEMP_LINEAR:
                        colorDistribution = ColorDistributionType.TempLinear;
                        break;
                    case IRtekFileSDK.IR_FILE_AD_LINEAR:
                    default:
                        colorDistribution = ColorDistributionType.ADLinear;
                        break;
                }
                return colorDistribution;
            }
            else
            {
                return ColorDistributionType.ADLinear;
            }
        }

        public bool SetColorDistribution(ColorDistributionType value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int irColorDist;
            switch (value)
            {
                case ColorDistributionType.T_TWB:
                    irColorDist = IRtekFileSDK.IR_FILE_T_TWB;
                    break;
                case ColorDistributionType.TempLinear:
                    irColorDist = IRtekFileSDK.IR_FILE_TEMP_LINEAR;
                    break;
                case ColorDistributionType.ADLinear:
                default:
                    irColorDist = IRtekFileSDK.IR_FILE_AD_LINEAR;
                    break;
            }
            int result = IRtekFileSDK.ir_file_set_color_dist(HImage, irColorDist);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            HandleColorDistributionChangedEvent();

            return true;
        }

        public int GetFrameCount()
        {
            if (HImage == IntPtr.Zero)
            {
                return 0;
            }

            uint irFrameCount = 0;
            int result = IRtekFileSDK.ir_file_get_frame_count(HImage, ref irFrameCount);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                return (int)irFrameCount;
            }
            else
            {
                return 0;
            }
        }

        public float GetFrameRate()
        {
            if (HImage == IntPtr.Zero)
            {
                return 1.0F;
            }

            float irFrameRate = 1.0F;
            int result = IRtekFileSDK.ir_file_get_frame_rate(HImage, ref irFrameRate);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                return irFrameRate;
            }
            else
            {
                return 1.0F;
            }
        }

        public bool SetFrameRate(float value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int result = IRtekFileSDK.ir_file_set_frame_rate(HImage, value);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = true;

            return true;
        }

        public int GetFrameIndex()
        {
            if (HImage == IntPtr.Zero)
            {
                return 0;
            }

            uint irFrameIndex = 0;
            int result = IRtekFileSDK.ir_file_get_frame_index(HImage, ref irFrameIndex);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                return (int)irFrameIndex;
            }
            else
            {
                return 0;
            }
        }

        public bool SetFrameIndex(int index)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int result = IRtekFileSDK.ir_file_set_frame_index(HImage, (uint)index);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            HandleFrameIndexChangedEvent();

            return true;
        }

        public bool SaveFrameAs(string fileName, Image coverImage)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_buffer irBuffer = new IRtekFileSDK.ir_file_buffer();
            try
            {
                if (coverImage != null)
                {
                    MemoryStream ms = new MemoryStream();
                    coverImage.Save(ms, ImageFormat.Jpeg);
                    byte[] buffer = ms.GetBuffer();

                    irBuffer.length = (uint)buffer.Length;
                    irBuffer.buffer = Marshal.AllocHGlobal((int)irBuffer.length);
                    Marshal.Copy(buffer, 0, irBuffer.buffer, (int)irBuffer.length);
                }

                int result = IRtekFileSDK.ir_file_save_frame_as(HImage, fileName, ref irBuffer);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(irBuffer.buffer);
            }

            return true;
        }

        //=========新增begin=========
        public Frame GetFrame()
        {
            if (HImage == IntPtr.Zero)
            {
                return Frame.DEFAULT;
            }

            int frameIndex = GetFrameIndex();
            if (frameIndex == -1)
            {
                return Frame.DEFAULT;
            }

            IRtekFileSDK.ir_file_frame irFrame = new IRtekFileSDK.ir_file_frame();
            int result = IRtekFileSDK.ir_file_get_frame(HImage, (uint)frameIndex, ref irFrame);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                Size thermalSize = GetThermalSize();
                int width = thermalSize.Width;
                int height = thermalSize.Height;

                if (irFrame.length != width * height)
                {
                    return Frame.DEFAULT;
                }

                try
                {
                    irFrame.buffer = Marshal.AllocHGlobal((int)irFrame.length * sizeof(ushort));
                    result = IRtekFileSDK.ir_file_get_frame(HImage, (uint)frameIndex, ref irFrame);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        Frame value = new Frame();
                        DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        TimeSpan tsSpan = new TimeSpan((long)irFrame.timeStamp * TimeSpan.TicksPerMillisecond);
                        value.TimeStamp = (dtStart + tsSpan).ToLocalTime();
                        value.Values = new ushort[height, width];
                        short[] tmpBuffer = new short[irFrame.length];
                        Marshal.Copy(irFrame.buffer, tmpBuffer, 0, (int)irFrame.length);
                        Buffer.BlockCopy(tmpBuffer, 0, value.Values, 0, tmpBuffer.Length * sizeof(ushort));
                        return value;
                    }
                    else
                    {
                        return Frame.DEFAULT;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irFrame.buffer);
                }
            }
            else
            {
                return Frame.DEFAULT;
            }
        }
        //=========新增end=========

        public IThermalImage GetFrame(int index)
        {
            ThermalImage thermalImage = new ThermalImage(this);

            ThermalImage.MarkerCollection markers = new ThermalImage.MarkerCollection(thermalImage);
            string[] names = GetMarkerNames();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                ThermalImage.Marker marker = GetMarker(index, name);
                if (marker == null)
                {
                    continue;
                }

                marker.Owner = thermalImage;
                markers.AddInternal(marker);
            }
            thermalImage.Markers = markers;

            ThermalImage.DeltaCollection deltas = new ThermalImage.DeltaCollection(thermalImage);
            names = GetDeltaNames();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                ThermalImage.Delta delta = GetDelta(index, name);
                if (delta == null)
                {
                    continue;
                }

                delta.Owner = thermalImage;
                deltas.AddInternal(delta);
            }
            thermalImage.Deltas = deltas;

            thermalImage.ShotTime = GetShotTime(index);
            return thermalImage;
        }

        public string[] GetUserDataKeys()
        {
            if (HImage == IntPtr.Zero)
            {
                return new string[0];
            }

            IRtekFileSDK.ir_file_keys irKeyList = new IRtekFileSDK.ir_file_keys();
            int result = IRtekFileSDK.ir_file_get_keys(HImage, ref irKeyList);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int bufferSize = (int)(irKeyList.count * IRtekFileSDK.KEY_LEN);
                    irKeyList.keys = Marshal.AllocHGlobal(bufferSize);
                    result = IRtekFileSDK.ir_file_get_keys(HImage, ref irKeyList);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        byte[] buffer = new byte[bufferSize];
                        Marshal.Copy(irKeyList.keys, buffer, 0, bufferSize);
                        string[] keys = new string[irKeyList.count];
                        for (int i = 0; i < irKeyList.count; i++)
                        {
                            keys[i] = StringConvertHelper.BytesToString(buffer, i * IRtekFileSDK.KEY_LEN, IRtekFileSDK.KEY_LEN, Encoding.UTF8);
                        }
                        return keys;
                    }
                    else
                    {
                        return new string[0];
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irKeyList.keys);
                }
            }
            else
            {
                return new string[0];
            }
        }

        public string GetUserData(string key)
        {
            if (HImage == IntPtr.Zero)
            {
                return string.Empty;
            }

            IRtekFileSDK.ir_file_kvPair irKVPair = new IRtekFileSDK.ir_file_kvPair();
            irKVPair.key = StringConvertHelper.StringToBytes(key, IRtekFileSDK.KEY_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_get_user_data(HImage, ref irKVPair);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    irKVPair.vBuffer = Marshal.AllocHGlobal((int)irKVPair.vLength);
                    result = IRtekFileSDK.ir_file_get_user_data(HImage, ref irKVPair);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        byte[] buffer = new byte[irKVPair.vLength];
                        Marshal.Copy(irKVPair.vBuffer, buffer, 0, (int)irKVPair.vLength);
                        return StringConvertHelper.BytesToString(buffer, Encoding.UTF8);
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irKVPair.vBuffer);
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public bool SetUserData(string key, string value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_kvPair irKVPair = new IRtekFileSDK.ir_file_kvPair();
            irKVPair.key = StringConvertHelper.StringToBytes(key, IRtekFileSDK.KEY_LEN, Encoding.UTF8);
            byte[] buffer = StringConvertHelper.StringToBytes(value, Encoding.UTF8);
            irKVPair.vLength = (uint)buffer.Length;
            try
            {
                irKVPair.vBuffer = Marshal.AllocHGlobal((int)irKVPair.vLength);
                Marshal.Copy(buffer, 0, irKVPair.vBuffer, (int)irKVPair.vLength);
                int result = IRtekFileSDK.ir_file_set_user_data(HImage, ref irKVPair);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(irKVPair.vBuffer);
            }

            return true;
        }

        public bool Open(string fileName)
        {
            if (HImage != IntPtr.Zero)
            {
                Close();
            }

            IntPtr h = IntPtr.Zero;
            int result = IRtekFileSDK.ir_file_open(fileName, ref h);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                HImage = h;
                _isMarkersSynced = false;
                string[] names = GetMarkerNames();
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    IMarker marker = GetMarker(name);
                    if (marker == null)
                    {
                        continue;
                    }

                    _markers.Add(marker);
                }
                _isMarkersSynced = true;

                _isDeltasSynced = false;
                names = GetDeltaNames();
                for (int i = 0; i < names.Length; i++)
                {
                    string name = names[i];
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    IDelta delta = GetDelta(name);
                    if (delta == null)
                    {
                        continue;
                    }

                    _deltas.Add(delta);
                }
                _isDeltasSynced = true;

                _referenceCache = GetReference();

                IsModified = false;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Create(string fileName, FileType fileType)
        {
            if (HImage != IntPtr.Zero)
            {
                return false;
            }

            int irFileType;
            switch (fileType)
            {
                case FileType.ThermalPicture:
                    irFileType = IRtekFileSDK.IR_FILE_THERMAL_PICTURE;
                    break;
                case FileType.GridThermalPicture:
                    irFileType = IRtekFileSDK.IR_FILE_GRID_THERMAL_PICTURE;
                    break;
                case FileType.ThermalVideo:
                    irFileType = IRtekFileSDK.IR_FILE_THERMAL_VIDEO;
                    break;
                default:
                    return false;
            }

            IntPtr h = IntPtr.Zero;
            int result = IRtekFileSDK.ir_file_create(fileName, irFileType, ref h);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                HImage = h;
                _isMarkersSynced = true;
                _isDeltasSynced = true;
                return true;
            }

            return false;
        }

        public void Close()
        {
            if (HImage == IntPtr.Zero)
            {
                return;
            }

            _isMarkersSynced = false;
            (_markers as MarkerCollection).Clear();
            _isDeltasSynced = false;
            (_deltas as DeltaCollection).Clear();
            _referenceCache = Reference.DEFAULT;
            _thermalImageCache = null;
            _paletteImageCache = null;
            _visualImageCache = null;
            _coverImageCache = null;
            IRtekFileSDK.ir_file_close(HImage);
            HImage = IntPtr.Zero;
            IsModified = false;
        }

        public bool Save()
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int result = IRtekFileSDK.ir_file_save(HImage);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = false;

            return true;
        }

        public bool SaveFJPG()
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int result = IRtekFileSDK.ir_file_save_fjpg(HImage);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = false;

            return true;
        }

        public bool SaveAs(string fileName)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int result = IRtekFileSDK.ir_file_save_as(HImage, fileName);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = false;

            return true;
        }

        public bool SaveAsFJPG(string fileName)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            int result = IRtekFileSDK.ir_file_save_as_fjpg(HImage, fileName);
            if (result != IRtekFileSDK.IR_FILE_EC_OK)
            {
                return false;
            }

            IsModified = false;

            return true;
        }

        public Frame GetRawFrame(int index)
        {
            if (HImage == IntPtr.Zero)
            {
                return Frame.DEFAULT;
            }

            IRtekFileSDK.ir_file_frame irFrame = new IRtekFileSDK.ir_file_frame();
            int result = IRtekFileSDK.ir_file_get_frame(HImage, (uint)index, ref irFrame);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    irFrame.buffer = Marshal.AllocHGlobal((int)(irFrame.length * sizeof(ushort)));
                    result = IRtekFileSDK.ir_file_get_frame(HImage, (uint)index, ref irFrame);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        TimeSpan tsSpan = new TimeSpan((long)irFrame.timeStamp * TimeSpan.TicksPerMillisecond);
                        ushort[] data = new ushort[irFrame.length];
                        short[] tmpData = new short[irFrame.length];
                        Marshal.Copy(irFrame.buffer, tmpData, 0, (int)irFrame.length);
                        Buffer.BlockCopy(tmpData, 0, data, 0, (int)(Marshal.SizeOf(typeof(ushort)) * irFrame.length));
                        return new Frame(dtStart + tsSpan, data);
                    }
                    else
                    {
                        return Frame.DEFAULT;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irFrame.buffer);
                }
            }
            else
            {
                return Frame.DEFAULT;
            }
        }

        public bool AppendRawFrame(Frame value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            if (value.Data == null)
            {
                return false;
            }

            IRtekFileSDK.ir_file_frame frame = new IRtekFileSDK.ir_file_frame();
            try
            {
                int size = Marshal.SizeOf(typeof(ushort));
                DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan tsSpan = value.TimeStamp.ToUniversalTime() - dtStart;
                ulong lSpan = (ulong)tsSpan.Ticks / TimeSpan.TicksPerMillisecond;

                frame.timeStamp = lSpan;
                frame.length = (uint)value.Data.Length;
                frame.buffer = Marshal.AllocHGlobal((int)(size * frame.length));
                short[] tmpData = new short[value.Data.Length];
                Buffer.BlockCopy(value.Data, 0, tmpData, 0, size * value.Data.Length);
                Marshal.Copy(tmpData, 0, frame.buffer, value.Data.Length);

                int result = IRtekFileSDK.ir_file_append_frame(HImage, ref frame);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(frame.buffer);
            }

            return true;
        }

        public LUT GetLUT()
        {
            if (HImage == IntPtr.Zero)
            {
                return LUT.DEFAULT;
            }

            IRtekFileSDK.ir_file_lut irLUT = new IRtekFileSDK.ir_file_lut();
            int result = IRtekFileSDK.ir_file_get_lut(HImage, ref irLUT);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    irLUT.adArray = Marshal.AllocHGlobal((int)irLUT.count * sizeof(ushort));
                    irLUT.tempArray = Marshal.AllocHGlobal((int)irLUT.count * sizeof(float));
                    result = IRtekFileSDK.ir_file_get_lut(HImage, ref irLUT);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        ushort[] adArray = new ushort[irLUT.count];
                        float[] tempArray = new float[irLUT.count];
                        short[] tmpData = new short[irLUT.count];
                        Marshal.Copy(irLUT.adArray, tmpData, 0, (int)irLUT.count);
                        Buffer.BlockCopy(tmpData, 0, adArray, 0, (int)(Marshal.SizeOf(typeof(ushort)) * irLUT.count));
                        Marshal.Copy(irLUT.tempArray, tempArray, 0, (int)irLUT.count);
                        return new LUT(irLUT.radMethod, adArray, tempArray);
                    }
                    else
                    {
                        return LUT.DEFAULT;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irLUT.adArray);
                    Marshal.FreeHGlobal(irLUT.tempArray);
                }
            }
            else
            {
                return LUT.DEFAULT;
            }
        }

        public bool SetLUT(LUT value)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            if (value.ADArray == null || value.TempArray == null || value.ADArray.Length != value.TempArray.Length)
            {
                return false;
            }

            IRtekFileSDK.ir_file_lut irFileLUT = new IRtekFileSDK.ir_file_lut();
            irFileLUT.radMethod = value.RadMethod;
            irFileLUT.count = (uint)value.ADArray.Length;
            try
            {
                irFileLUT.adArray = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(ushort)) * irFileLUT.count));
                irFileLUT.tempArray = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(float)) * irFileLUT.count));
                short[] tmpData = new short[irFileLUT.count];
                Buffer.BlockCopy(value.ADArray, 0, tmpData, 0, (int)(Marshal.SizeOf(typeof(ushort)) * irFileLUT.count));
                Marshal.Copy(tmpData, 0, irFileLUT.adArray, (int)irFileLUT.count);
                Marshal.Copy(value.TempArray, 0, irFileLUT.tempArray, (int)irFileLUT.count);

                int result = IRtekFileSDK.ir_file_set_lut(HImage, ref irFileLUT);
                if (result != IRtekFileSDK.IR_FILE_EC_OK)
                {
                    return false;
                }

                IsModified = true;
            }
            finally
            {
                Marshal.FreeHGlobal(irFileLUT.adArray);
                Marshal.FreeHGlobal(irFileLUT.tempArray);
            }

            return true;
        }

        public LUT GetCalculatedLUT()
        {
            if (HImage == IntPtr.Zero)
            {
                return LUT.DEFAULT;
            }

            IRtekFileSDK.ir_file_lut irLUT = new IRtekFileSDK.ir_file_lut();
            int result = IRtekFileSDK.ir_file_get_calculated_lut(HImage, ref irLUT);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    irLUT.adArray = Marshal.AllocHGlobal((int)irLUT.count * sizeof(ushort));
                    irLUT.tempArray = Marshal.AllocHGlobal((int)irLUT.count * sizeof(float));
                    result = IRtekFileSDK.ir_file_get_calculated_lut(HImage, ref irLUT);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        ushort[] adArray = new ushort[irLUT.count];
                        float[] tempArray = new float[irLUT.count];
                        short[] tmpData = new short[irLUT.count];
                        Marshal.Copy(irLUT.adArray, tmpData, 0, (int)irLUT.count);
                        Buffer.BlockCopy(tmpData, 0, adArray, 0, (int)(Marshal.SizeOf(typeof(ushort)) * irLUT.count));
                        Marshal.Copy(irLUT.tempArray, tempArray, 0, (int)irLUT.count);
                        return new LUT(irLUT.radMethod, adArray, tempArray);
                    }
                    else
                    {
                        return LUT.DEFAULT;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irLUT.adArray);
                    Marshal.FreeHGlobal(irLUT.tempArray);
                }
            }
            else
            {
                return LUT.DEFAULT;
            }
        }

        public float GetSpotValue(Point point)
        {
            LocalThermalParams thermalParams = new LocalThermalParams();
            thermalParams.LocalParams = false;
            thermalParams.Emissivity = 1.0F;
            thermalParams.Distance = 1.0F;
            thermalParams.ReflTemp = 293.15F;

            return GetSpotValue(point, thermalParams);
        }

        public float GetSpotValue(Point point, LocalThermalParams thermalParams)
        {
            if (HImage == IntPtr.Zero)
            {
                return 0.0F;
            }

            IRtekFileSDK.ir_file_temp_point irTempPoint = new IRtekFileSDK.ir_file_temp_point();
            IRtekFileSDK.ir_file_point irPoint = new IRtekFileSDK.ir_file_point();
            irPoint.x = (short)point.X;
            irPoint.y = (short)point.Y;
            irTempPoint.point = irPoint;
            irTempPoint.localParams = thermalParams.LocalParams ? TRUE : FALSE;
            irTempPoint.reflTemp = thermalParams.ReflTemp;
            irTempPoint.distance = thermalParams.Distance;
            irTempPoint.emissivity = thermalParams.Emissivity;
            int result = IRtekFileSDK.ir_file_get_temp_spot(HImage, ref irTempPoint);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                return irTempPoint.temp;
            }
            else
            {
                return 0.0F;
            }
        }

        public TemperaturePoint[] GetRectangleValues(Rectangle rectangle)
        {
            LocalThermalParams thermalParams = new LocalThermalParams();
            thermalParams.LocalParams = false;
            thermalParams.Emissivity = 1.0F;
            thermalParams.Distance = 1.0F;
            thermalParams.ReflTemp = 293.15F;

            return GetRectangleValues(rectangle, thermalParams);
        }

        public TemperaturePoint[] GetRectangleValues(Rectangle rectangle, LocalThermalParams thermalParams)
        {
            if (HImage == IntPtr.Zero)
            {
                return new TemperaturePoint[] { };
            }

            IRtekFileSDK.ir_file_temp_rect irTempRect = new IRtekFileSDK.ir_file_temp_rect();
            IRtekFileSDK.ir_file_rectangle irRectangle = new IRtekFileSDK.ir_file_rectangle();
            irRectangle.x = (short)rectangle.X;
            irRectangle.y = (short)rectangle.Y;
            irRectangle.width = (short)rectangle.Width;
            irRectangle.height = (short)rectangle.Height;
            irTempRect.rect = irRectangle;
            irTempRect.localParams = thermalParams.LocalParams ? TRUE : FALSE;
            irTempRect.reflTemp = thermalParams.ReflTemp;
            irTempRect.distance = thermalParams.Distance;
            irTempRect.emissivity = thermalParams.Emissivity;
            int result = IRtekFileSDK.ir_file_get_temp_rect(HImage, ref irTempRect);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int count = irTempRect.tempCount;
                    irTempRect.temps = Marshal.AllocHGlobal(count * Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point_value)));
                    result = IRtekFileSDK.ir_file_get_temp_rect(HImage, ref irTempRect);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        TemperaturePoint[] temperaturePoints = new TemperaturePoint[count];
                        Type type = typeof(IRtekFileSDK.ir_file_point_value);
                        int typeSize = Marshal.SizeOf(type);
                        for (int i = 0; i < count; i++)
                        {
                            IRtekFileSDK.ir_file_point_value pointValue = (IRtekFileSDK.ir_file_point_value)Marshal.PtrToStructure(IntPtr.Add(irTempRect.temps, i * typeSize), type);
                            temperaturePoints[i] = new TemperaturePoint(pointValue.point.x, pointValue.point.y, pointValue.temp);
                        }
                        return temperaturePoints;
                    }
                    else
                    {
                        return new TemperaturePoint[] { };
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irTempRect.temps);
                }
            }
            else
            {
                return new TemperaturePoint[] { };
            }
        }

        public TemperaturePoint[] GetEllipseValues(Rectangle rectangle)
        {
            LocalThermalParams thermalParams = new LocalThermalParams();
            thermalParams.LocalParams = false;
            thermalParams.Emissivity = 1.0F;
            thermalParams.Distance = 1.0F;
            thermalParams.ReflTemp = 293.15F;

            return GetEllipseValues(rectangle, thermalParams);
        }

        public TemperaturePoint[] GetEllipseValues(Rectangle rectangle, LocalThermalParams thermalParams)
        {
            if (HImage == IntPtr.Zero)
            {
                return new TemperaturePoint[] { };
            }

            IRtekFileSDK.ir_file_temp_rect irTempRect = new IRtekFileSDK.ir_file_temp_rect();
            IRtekFileSDK.ir_file_rectangle irRectangle = new IRtekFileSDK.ir_file_rectangle();
            irRectangle.x = (short)rectangle.X;
            irRectangle.y = (short)rectangle.Y;
            irRectangle.width = (short)rectangle.Width;
            irRectangle.height = (short)rectangle.Height;
            irTempRect.rect = irRectangle;
            irTempRect.localParams = thermalParams.LocalParams ? TRUE : FALSE;
            irTempRect.reflTemp = thermalParams.ReflTemp;
            irTempRect.distance = thermalParams.Distance;
            irTempRect.emissivity = thermalParams.Emissivity;
            int result = IRtekFileSDK.ir_file_get_temp_ellipse(HImage, ref irTempRect);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int count = irTempRect.tempCount;
                    irTempRect.temps = Marshal.AllocHGlobal(count * Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point_value)));
                    result = IRtekFileSDK.ir_file_get_temp_ellipse(HImage, ref irTempRect);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        TemperaturePoint[] temperaturePoints = new TemperaturePoint[count];
                        Type type = typeof(IRtekFileSDK.ir_file_point_value);
                        int typeSize = Marshal.SizeOf(type);
                        for (int i = 0; i < count; i++)
                        {
                            IRtekFileSDK.ir_file_point_value pointValue = (IRtekFileSDK.ir_file_point_value)Marshal.PtrToStructure(IntPtr.Add(irTempRect.temps, i * typeSize), type);
                            temperaturePoints[i] = new TemperaturePoint(pointValue.point.x, pointValue.point.y, pointValue.temp);
                        }
                        return temperaturePoints;
                    }
                    else
                    {
                        return new TemperaturePoint[] { };
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irTempRect.temps);
                }
            }
            else
            {
                return new TemperaturePoint[] { };
            }
        }

        public TemperaturePoint[] GetPolylineValues(Point[] points)
        {
            LocalThermalParams thermalParams = new LocalThermalParams();
            thermalParams.LocalParams = false;
            thermalParams.Emissivity = 1.0F;
            thermalParams.Distance = 1.0F;
            thermalParams.ReflTemp = 293.15F;

            return GetPolylineValues(points, thermalParams);
        }

        public TemperaturePoint[] GetPolylineValues(Point[] points, LocalThermalParams thermalParams)
        {
            if (HImage == IntPtr.Zero)
            {
                return new TemperaturePoint[] { };
            }

            IRtekFileSDK.ir_file_temp_points irTempPoints = new IRtekFileSDK.ir_file_temp_points();
            try
            {
                irTempPoints.pointCount = points.Length;
                int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                irTempPoints.points = Marshal.AllocHGlobal(irTempPoints.pointCount * size);
                for (int i = 0; i < points.Length; i++)
                {
                    IRtekFileSDK.ir_file_point p = new IRtekFileSDK.ir_file_point();
                    p.x = (short)points[i].X;
                    p.y = (short)points[i].Y;
                    Marshal.StructureToPtr(p, irTempPoints.points + i * size, false);
                }
                irTempPoints.localParams = thermalParams.LocalParams ? TRUE : FALSE;
                irTempPoints.reflTemp = thermalParams.ReflTemp;
                irTempPoints.distance = thermalParams.Distance;
                irTempPoints.emissivity = thermalParams.Emissivity;
                int result = IRtekFileSDK.ir_file_get_temp_polyline(HImage, ref irTempPoints);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int count = irTempPoints.tempCount;
                        irTempPoints.temps = Marshal.AllocHGlobal(count * Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point_value)));
                        result = IRtekFileSDK.ir_file_get_temp_polyline(HImage, ref irTempPoints);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            TemperaturePoint[] temperaturePoints = new TemperaturePoint[count];
                            Type type = typeof(IRtekFileSDK.ir_file_point_value);
                            int typeSize = Marshal.SizeOf(type);
                            for (int i = 0; i < count; i++)
                            {
                                IRtekFileSDK.ir_file_point_value pointValue = (IRtekFileSDK.ir_file_point_value)Marshal.PtrToStructure(IntPtr.Add(irTempPoints.temps, i * typeSize), type);
                                temperaturePoints[i] = new TemperaturePoint(pointValue.point.x, pointValue.point.y, pointValue.temp);
                            }
                            return temperaturePoints;
                        }
                        else
                        {
                            return new TemperaturePoint[] { };
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irTempPoints.temps);
                    }
                }
                else
                {
                    return new TemperaturePoint[] { };
                }
            }
            finally
            {
                Marshal.FreeHGlobal(irTempPoints.points);
            }
        }

        public TemperaturePoint[] GetPolygonValues(Point[] points)
        {
            LocalThermalParams thermalParams = new LocalThermalParams();
            thermalParams.LocalParams = false;
            thermalParams.Emissivity = 1.0F;
            thermalParams.Distance = 1.0F;
            thermalParams.ReflTemp = 293.15F;

            return GetPolygonValues(points, thermalParams);
        }

        public TemperaturePoint[] GetPolygonValues(Point[] points, LocalThermalParams thermalParams)
        {
            if (HImage == IntPtr.Zero)
            {
                return new TemperaturePoint[] { };
            }

            IRtekFileSDK.ir_file_temp_points irTempPoints = new IRtekFileSDK.ir_file_temp_points();
            try
            {
                irTempPoints.pointCount = points.Length;
                int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                irTempPoints.points = Marshal.AllocHGlobal(irTempPoints.pointCount * size);
                for (int i = 0; i < points.Length; i++)
                {
                    IRtekFileSDK.ir_file_point p = new IRtekFileSDK.ir_file_point();
                    p.x = (short)points[i].X;
                    p.y = (short)points[i].Y;
                    Marshal.StructureToPtr(p, irTempPoints.points + i * size, false);
                }
                irTempPoints.localParams = thermalParams.LocalParams ? TRUE : FALSE;
                irTempPoints.reflTemp = thermalParams.ReflTemp;
                irTempPoints.distance = thermalParams.Distance;
                irTempPoints.emissivity = thermalParams.Emissivity;
                int result = IRtekFileSDK.ir_file_get_temp_polygon(HImage, ref irTempPoints);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int count = irTempPoints.tempCount;
                        irTempPoints.temps = Marshal.AllocHGlobal(count * Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point_value)));
                        result = IRtekFileSDK.ir_file_get_temp_polygon(HImage, ref irTempPoints);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            TemperaturePoint[] temperaturePoints = new TemperaturePoint[count];
                            Type type = typeof(IRtekFileSDK.ir_file_point_value);
                            int typeSize = Marshal.SizeOf(type);
                            for (int i = 0; i < count; i++)
                            {
                                IRtekFileSDK.ir_file_point_value pointValue = (IRtekFileSDK.ir_file_point_value)Marshal.PtrToStructure(IntPtr.Add(irTempPoints.temps, i * typeSize), type);
                                temperaturePoints[i] = new TemperaturePoint(pointValue.point.x, pointValue.point.y, pointValue.temp);
                            }
                            return temperaturePoints;
                        }
                        else
                        {
                            return new TemperaturePoint[] { };
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irTempPoints.temps);
                    }
                }
                else
                {
                    return new TemperaturePoint[] { };
                }
            }
            finally
            {
                Marshal.FreeHGlobal(irTempPoints.points);
            }
        }

        public void Rotate(RotateType rotation)
        {
            if (HImage == IntPtr.Zero)
            {
                return;
            }

            int irRotation = IRtekFileSDK.IR_FILE_ROTATE_90;
            switch (rotation)
            {
                case RotateType.Rotate90:
                default:
                    irRotation = IRtekFileSDK.IR_FILE_ROTATE_90;
                    break;
                case RotateType.Rotate180:
                    irRotation = IRtekFileSDK.IR_FILE_ROTATE_180;
                    break;
                case RotateType.Rotate270:
                    irRotation = IRtekFileSDK.IR_FILE_ROTATE_270;
                    break;
            }

            int result = IRtekFileSDK.ir_file_set_rotation(HImage, irRotation);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                IsModified = true;

                HandleRotationTypeChangedEvent();
            }
        }

        public void Mirror(MirrorType mirror)
        {
            if (HImage == IntPtr.Zero)
            {
                return;
            }

            int irMirror = IRtekFileSDK.IR_FILE_MIRROR_HORIZONTAL;
            switch (mirror)
            {
                case MirrorType.MirrorHorizontal:
                default:
                    irMirror = IRtekFileSDK.IR_FILE_MIRROR_HORIZONTAL;
                    break;
                case MirrorType.MirrorVertical:
                    irMirror = IRtekFileSDK.IR_FILE_MIRROR_VERTICAL;
                    break;
            }

            int result = IRtekFileSDK.ir_file_set_mirror(HImage, irMirror);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                IsModified = true;

                HandleMirrorTypeChangedEvent();
            }
        }

        public IMarkerSpot CreateMarkerSpot(string name, LocalThermalParams thermalParams, Point point)
        {
            return new MarkerSpot(this, name, thermalParams, point);
        }

        public IMarkerRectangle CreateMarkerRectangle(string name, LocalThermalParams thermalParams, Rectangle rectangle)
        {
            return new MarkerRectangle(this, name, thermalParams, rectangle);
        }

        public IMarkerEllipse CreateMarkerEllipse(string name, LocalThermalParams thermalParams, Rectangle rectangle)
        {
            return new MarkerEllipse(this, name, thermalParams, rectangle);
        }

        public IMarkerPolygon CreateMarkerPolygon(string name, LocalThermalParams thermalParams, Point[] points)
        {
            return new MarkerPolygon(this, name, thermalParams, points);
        }

        public IMarkerPolyline CreateMarkerPolyline(string name, LocalThermalParams thermalParams, Point[] points)
        {
            return new MarkerPolyline(this, name, thermalParams, points);
        }

        public IDelta CreateDelta(string deltaName, DeltaParams deltaParams)
        {
            return new Delta(this, deltaName, deltaParams);
        }

        public List<PresetPalette> GetPresetPalettes()
        {
            List<PresetPalette> presetPalettes = new List<PresetPalette>();

            IRtekFileSDK.ir_file_preset_palettes irPresetPalettes = new IRtekFileSDK.ir_file_preset_palettes();
            int result = IRtekFileSDK.ir_file_get_preset_palettes(ref irPresetPalettes);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int size = 1 + IRtekFileSDK.NAME_LEN;
                    irPresetPalettes.presetPalettes = Marshal.AllocHGlobal(irPresetPalettes.count * size);
                    result = IRtekFileSDK.ir_file_get_preset_palettes(ref irPresetPalettes);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        for (int i = 0; i < irPresetPalettes.count; i++)
                        {
                            IRtekFileSDK.ir_file_preset_palette irPresetPalette = (IRtekFileSDK.ir_file_preset_palette)Marshal.PtrToStructure(irPresetPalettes.presetPalettes + i * size, typeof(IRtekFileSDK.ir_file_preset_palette));
                            PresetPalette presetPalette = new PresetPalette();
                            presetPalette.RefNo = irPresetPalette.refNo;
                            presetPalette.Name = StringConvertHelper.BytesToString(irPresetPalette.name, Encoding.UTF8);
                            presetPalettes.Add(presetPalette);
                        }
                    }

                    return presetPalettes;
                }
                finally
                {
                    Marshal.FreeHGlobal(irPresetPalettes.presetPalettes);
                }
            }
            else
            {
                return presetPalettes;
            }
        }

        public Image GetPresetPaletteImage(byte refNo)
        {
            try
            {
                int width = 1;
                int height = IRtekFileSDK.COLORS_LEN;
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                IRtekFileSDK.ir_file_image irImage = new IRtekFileSDK.ir_file_image();
                irImage.offset = (uint)(bmpData.Stride - 3 * width);
                irImage.data = bmpData.Scan0;
                IRtekFileSDK.ir_file_get_preset_palette_image(refNo, ref irImage);
                bitmap.UnlockBits(bmpData);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private ThermalImage.Marker GetMarker(int index, string name)
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
            irMarker.name = StringConvertHelper.StringToBytes(name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_get_index_marker(HImage, (uint)index, ref irMarker);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                    irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                    result = IRtekFileSDK.ir_file_get_index_marker(HImage, (uint)index, ref irMarker);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        LocalThermalParams thermalParams = new LocalThermalParams();
                        thermalParams.LocalParams = irMarker.localParams == TRUE;
                        thermalParams.Emissivity = irMarker.emissivity;
                        thermalParams.Distance = irMarker.distance;
                        thermalParams.ReflTemp = irMarker.reflTemp;

                        IRtekFileSDK.ir_file_point p;
                        ThermalImage.Marker marker;
                        switch (irMarker.type)
                        {
                            case IRtekFileSDK.IR_FILE_SPOT:
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                                Point spot = new Point(p.x, p.y);
                                marker = new ThermalImage.MarkerSpot(StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, spot);
                                break;
                            case IRtekFileSDK.IR_FILE_RECTANGLE:
                                Rectangle rect = new Rectangle();
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                                Point p1 = new Point(p.x, p.y);
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekFileSDK.ir_file_point));
                                Point p2 = new Point(p.x, p.y);
                                rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                                rect.Size = new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                                marker = new ThermalImage.MarkerRectangle(StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, rect);
                                break;
                            case IRtekFileSDK.IR_FILE_ELLIPSE:
                                rect = new Rectangle();
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                                p1 = new Point(p.x, p.y);
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekFileSDK.ir_file_point));
                                p2 = new Point(p.x, p.y);
                                rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                                rect.Size = new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                                marker = new ThermalImage.MarkerEllipse(StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, rect);
                                break;
                            case IRtekFileSDK.IR_FILE_POLYGON:
                                Point[] points = new Point[irMarker.pointCount];
                                for (int i = 0; i < irMarker.pointCount; i++)
                                {
                                    p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + i * size, typeof(IRtekFileSDK.ir_file_point));
                                    points[i] = new Point(p.x, p.y);
                                }
                                marker = new ThermalImage.MarkerPolygon(StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, points);
                                break;
                            case IRtekFileSDK.IR_FILE_POLYLINE:
                                points = new Point[irMarker.pointCount];
                                for (int i = 0; i < irMarker.pointCount; i++)
                                {
                                    p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + i * size, typeof(IRtekFileSDK.ir_file_point));
                                    points[i] = new Point(p.x, p.y);
                                }
                                marker = new ThermalImage.MarkerPolyline(StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, points);
                                break;
                            default:
                                return null;
                        }

                        float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                        marker.MaxInternal = temp;
                        temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                        marker.MinInternal = temp;
                        temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                        marker.AvgInternal = temp;
                        IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                        marker.HotSpotInternal = new Point(irPoint.x, irPoint.y);
                        irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                        marker.ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                        marker.MeasuredValueInternal = irMarker.measuredValue;

                        return marker;
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irMarker.points);
                }
            }
            else
            {
                return null;
            }
        }

        private ThermalImage.Delta GetDelta(int index, string name)
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            IRtekFileSDK.ir_file_delta irDelta = new IRtekFileSDK.ir_file_delta();
            irDelta.name = StringConvertHelper.StringToBytes(name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_get_index_delta(HImage, (uint)index, ref irDelta);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                DeltaParams deltaParams = new DeltaParams();
                deltaParams.SrcName = StringConvertHelper.BytesToString(irDelta.srcName, Encoding.UTF8);
                switch (irDelta.srcValueType)
                {
                    case IRtekFileSDK.IR_FILE_MAX:
                    default:
                        deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Max;
                        break;
                    case IRtekFileSDK.IR_FILE_MIN:
                        deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Min;
                        break;
                    case IRtekFileSDK.IR_FILE_AVG:
                        deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Avg;
                        break;
                }
                deltaParams.SrcTemp = irDelta.srcTemp;
                deltaParams.DestName = StringConvertHelper.BytesToString(irDelta.destName, Encoding.UTF8);
                switch (irDelta.destValueType)
                {
                    case IRtekFileSDK.IR_FILE_MAX:
                    default:
                        deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Max;
                        break;
                    case IRtekFileSDK.IR_FILE_MIN:
                        deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Min;
                        break;
                    case IRtekFileSDK.IR_FILE_AVG:
                        deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Avg;
                        break;
                }
                deltaParams.DestTemp = irDelta.destTemp;

                ThermalImage.Delta delta = new ThermalImage.Delta(StringConvertHelper.BytesToString(irDelta.name, Encoding.UTF8), deltaParams);
                delta.Value = irDelta.value;
                return delta;
            }
            else
            {
                return null;
            }
        }

        public DateTime GetShotTime(int index)
        {
            if (HImage == IntPtr.Zero)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            ulong lSpan = 0;
            int result = IRtekFileSDK.ir_file_get_index_shot_time(HImage, (uint)index, ref lSpan);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                try
                {
                    DateTime shotTime = dtStart + new TimeSpan((long)lSpan * TimeSpan.TicksPerMillisecond);
                    return shotTime;
                }
                catch { }
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }

        private IMarker GetMarker(string name)
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
            irMarker.name = StringConvertHelper.StringToBytes(name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_get_marker(HImage, ref irMarker);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                    irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                    result = IRtekFileSDK.ir_file_get_marker(HImage, ref irMarker);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        LocalThermalParams thermalParams = new LocalThermalParams();
                        thermalParams.LocalParams = irMarker.localParams == TRUE;
                        thermalParams.Emissivity = irMarker.emissivity;
                        thermalParams.Distance = irMarker.distance;
                        thermalParams.ReflTemp = irMarker.reflTemp;

                        IRtekFileSDK.ir_file_point p;
                        Marker marker;
                        switch (irMarker.type)
                        {
                            case IRtekFileSDK.IR_FILE_SPOT:
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                                Point spot = new Point(p.x, p.y);
                                marker = new MarkerSpot(this, StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, spot);
                                break;
                            case IRtekFileSDK.IR_FILE_RECTANGLE:
                                Rectangle rect = new Rectangle();
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                                Point p1 = new Point(p.x, p.y);
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekFileSDK.ir_file_point));
                                Point p2 = new Point(p.x, p.y);
                                rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                                rect.Size = new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y ));
                                marker = new MarkerRectangle(this, StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, rect);
                                break;
                            case IRtekFileSDK.IR_FILE_ELLIPSE:
                                rect = new Rectangle();
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                                p1 = new Point(p.x, p.y);
                                p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekFileSDK.ir_file_point));
                                p2 = new Point(p.x, p.y);
                                rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                                rect.Size = new Size(Math.Abs(p1.X - p2.X ), Math.Abs(p1.Y - p2.Y ));
                                marker = new MarkerEllipse(this, StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, rect);
                                break;
                            case IRtekFileSDK.IR_FILE_POLYGON:
                                Point[] points = new Point[irMarker.pointCount];
                                for (int i = 0; i < irMarker.pointCount; i++)
                                {
                                    p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + i * size, typeof(IRtekFileSDK.ir_file_point));
                                    points[i] = new Point(p.x, p.y);
                                }
                                marker = new MarkerPolygon(this, StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, points);
                                break;
                            case IRtekFileSDK.IR_FILE_POLYLINE:
                                points = new Point[irMarker.pointCount];
                                for (int i = 0; i < irMarker.pointCount; i++)
                                {
                                    p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + i * size, typeof(IRtekFileSDK.ir_file_point));
                                    points[i] = new Point(p.x, p.y);
                                }
                                marker = new MarkerPolyline(this, StringConvertHelper.BytesToString(irMarker.name, Encoding.UTF8), thermalParams, points);
                                break;
                            default:
                                return null;
                        }

                        float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                        marker.MaxInternal = temp;
                        temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                        marker.MinInternal = temp;
                        temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                        marker.AvgInternal = temp;
                        IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                        marker.HotSpotInternal = new Point(irPoint.x, irPoint.y);
                        irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                        marker.ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                        marker.MeasuredValueInternal = irMarker.measuredValue;

                        return marker;
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irMarker.points);
                }
            }
            else
            {
                return null;
            }
        }

        private bool SetMarker(IMarker marker)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
            irMarker.name = StringConvertHelper.StringToBytes(marker.Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            LocalThermalParams thermalParams = marker.ThermalParams;
            irMarker.localParams = thermalParams.LocalParams ? TRUE : FALSE;
            irMarker.emissivity = thermalParams.Emissivity;
            irMarker.reflTemp = thermalParams.ReflTemp;
            irMarker.distance = thermalParams.Distance;
            try
            {
                int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                IRtekFileSDK.ir_file_point p;
                switch (marker.Type)
                {
                    case MarkerType.Spot:
                        irMarker.type = IRtekFileSDK.IR_FILE_SPOT;
                        MarkerSpot markerSpot = marker as MarkerSpot;
                        irMarker.pointCount = 1;
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        p = new IRtekFileSDK.ir_file_point();
                        p.x = (short)markerSpot.Point.X;
                        p.y = (short)markerSpot.Point.Y;
                        Marshal.StructureToPtr(p, irMarker.points, false);
                        break;
                    case MarkerType.Rectangle:
                        irMarker.type = IRtekFileSDK.IR_FILE_RECTANGLE;
                        MarkerRectangle markerRectangle = marker as MarkerRectangle;
                        irMarker.pointCount = 2;
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        p = new IRtekFileSDK.ir_file_point();
                        p.x = (short)markerRectangle.Rectangle.X;
                        p.y = (short)markerRectangle.Rectangle.Y;
                        Marshal.StructureToPtr(p, irMarker.points, false);
                        p = new IRtekFileSDK.ir_file_point();
                        p.x = (short)markerRectangle.Rectangle.Right;
                        p.y = (short)markerRectangle.Rectangle.Bottom;
                        Marshal.StructureToPtr(p, irMarker.points + size, false);
                        break;
                    case MarkerType.Ellipse:
                        irMarker.type = IRtekFileSDK.IR_FILE_ELLIPSE;
                        MarkerEllipse markerEllipse = marker as MarkerEllipse;
                        irMarker.pointCount = 2;
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        p = new IRtekFileSDK.ir_file_point();
                        p.x = (short)markerEllipse.Rectangle.X;
                        p.y = (short)markerEllipse.Rectangle.Y;
                        Marshal.StructureToPtr(p, irMarker.points, false);
                        p = new IRtekFileSDK.ir_file_point();
                        p.x = (short)markerEllipse.Rectangle.Right;
                        p.y = (short)markerEllipse.Rectangle.Bottom;
                        Marshal.StructureToPtr(p, irMarker.points + size, false);
                        break;
                    case MarkerType.Polygon:
                        irMarker.type = IRtekFileSDK.IR_FILE_POLYGON;
                        MarkerPolygon markerPolygon = marker as MarkerPolygon;
                        irMarker.pointCount = markerPolygon.Points.Length;
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        for (int i = 0; i < irMarker.pointCount; i++)
                        {
                            p = new IRtekFileSDK.ir_file_point();
                            p.x = (short)markerPolygon.Points[i].X;
                            p.y = (short)markerPolygon.Points[i].Y;
                            Marshal.StructureToPtr(p, irMarker.points + i * size, false);
                        }
                        break;
                    case MarkerType.Polyline:
                        irMarker.type = IRtekFileSDK.IR_FILE_POLYLINE;
                        MarkerPolyline markerPolyline = marker as MarkerPolyline;
                        irMarker.pointCount = markerPolyline.Points.Length;
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        for (int i = 0; i < irMarker.pointCount; i++)
                        {
                            p = new IRtekFileSDK.ir_file_point();
                            p.x = (short)markerPolyline.Points[i].X;
                            p.y = (short)markerPolyline.Points[i].Y;
                            Marshal.StructureToPtr(p, irMarker.points + i * size, false);
                        }
                        break;
                }

                int result = IRtekFileSDK.ir_file_set_marker(HImage, ref irMarker);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    IsModified = true;

                    float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                    Marker markerImp = marker as Marker;
                    markerImp.MaxInternal = temp;
                    temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                    markerImp.MinInternal = temp;
                    temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                    markerImp.AvgInternal = temp;
                    IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                    markerImp.HotSpotInternal = new Point(irPoint.x, irPoint.y);
                    irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                    markerImp.ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                    markerImp.MeasuredValueInternal = irMarker.measuredValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(irMarker.points);
            }
        }

        private bool RemoveMarker(string name)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            byte[] buffer = StringConvertHelper.StringToBytes(name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_remove_marker(HImage, buffer);
            if (result == IRtekFileSDK.IR_FILE_EC_OK || result == IRtekFileSDK.IR_FILE_EC_INVALID_KEY)
            {
                IsModified = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool RenameMarker(string fromName, string toName)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            byte[] fromBuffer = StringConvertHelper.StringToBytes(fromName, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            byte[] toBuffer = StringConvertHelper.StringToBytes(toName, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_rename_marker(HImage, fromBuffer, toBuffer);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                IsModified = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        private IDelta GetDelta(string name)
        {
            if (HImage == IntPtr.Zero)
            {
                return null;
            }

            IRtekFileSDK.ir_file_delta irDelta = new IRtekFileSDK.ir_file_delta();
            irDelta.name = StringConvertHelper.StringToBytes(name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_get_delta(HImage, ref irDelta);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                DeltaParams deltaParams = new DeltaParams();
                deltaParams.SrcName = StringConvertHelper.BytesToString(irDelta.srcName, Encoding.UTF8);
                switch (irDelta.srcValueType)
                {
                    case IRtekFileSDK.IR_FILE_MAX:
                    default:
                        deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Max;
                        break;
                    case IRtekFileSDK.IR_FILE_MIN:
                        deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Min;
                        break;
                    case IRtekFileSDK.IR_FILE_AVG:
                        deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Avg;
                        break;
                }
                deltaParams.SrcTemp = irDelta.srcTemp;
                deltaParams.DestName = StringConvertHelper.BytesToString(irDelta.destName, Encoding.UTF8);
                switch (irDelta.destValueType)
                {
                    case IRtekFileSDK.IR_FILE_MAX:
                    default:
                        deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Max;
                        break;
                    case IRtekFileSDK.IR_FILE_MIN:
                        deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Min;
                        break;
                    case IRtekFileSDK.IR_FILE_AVG:
                        deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Avg;
                        break;
                }
                deltaParams.DestTemp = irDelta.destTemp;

                Delta delta = new Delta(this, StringConvertHelper.BytesToString(irDelta.name, Encoding.UTF8), deltaParams);
                delta.Value = irDelta.value;
                return delta;
            }
            else
            {
                return null;
            }
        }

        private bool SetDelta(IDelta delta)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            IRtekFileSDK.ir_file_delta irDelta = new IRtekFileSDK.ir_file_delta();
            irDelta.name = StringConvertHelper.StringToBytes(delta.Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            DeltaParams deltaParams = delta.DeltaParams;
            irDelta.srcName = StringConvertHelper.StringToBytes(deltaParams.SrcName, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            switch (deltaParams.SrcValueType)
            {
                case Amur.ThermalImageFile.ValueType.Max:
                default:
                    irDelta.srcValueType = IRtekFileSDK.IR_FILE_MAX;
                    break;
                case Amur.ThermalImageFile.ValueType.Min:
                    irDelta.srcValueType = IRtekFileSDK.IR_FILE_MIN;
                    break;
                case Amur.ThermalImageFile.ValueType.Avg:
                    irDelta.srcValueType = IRtekFileSDK.IR_FILE_AVG;
                    break;
            }
            irDelta.srcTemp = deltaParams.SrcTemp;
            irDelta.destName = StringConvertHelper.StringToBytes(deltaParams.DestName, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            switch (deltaParams.DestValueType)
            {
                case Amur.ThermalImageFile.ValueType.Max:
                default:
                    irDelta.destValueType = IRtekFileSDK.IR_FILE_MAX;
                    break;
                case Amur.ThermalImageFile.ValueType.Min:
                    irDelta.destValueType = IRtekFileSDK.IR_FILE_MIN;
                    break;
                case Amur.ThermalImageFile.ValueType.Avg:
                    irDelta.destValueType = IRtekFileSDK.IR_FILE_AVG;
                    break;
            }
            irDelta.destTemp = deltaParams.DestTemp;

            int result = IRtekFileSDK.ir_file_set_delta(HImage, ref irDelta);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                IsModified = true;

                Delta deltaImp = delta as Delta;
                deltaImp.Value = irDelta.value;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool RemoveDelta(string name)
        {
            if (HImage == IntPtr.Zero)
            {
                return false;
            }

            byte[] buffer = StringConvertHelper.StringToBytes(name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
            int result = IRtekFileSDK.ir_file_remove_delta(HImage, buffer);
            if (result == IRtekFileSDK.IR_FILE_EC_OK || result == IRtekFileSDK.IR_FILE_EC_INVALID_KEY)
            {
                IsModified = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        private void DoAlarmChanged()
        {
            if (AlarmChanged != null)
            {
                AlarmChanged(this, new EventArgs());
            }
        }

        private void DoPaletteChanged()
        {
            lock (_paletteImageCacheLock)
            {
                _paletteImageCache = null;
            }

            if (PaletteChanged != null)
            {
                PaletteChanged(this, new EventArgs());
            }
        }

        private void DoImageChanged()
        {
            lock (_thermalImageCacheLock)
            {
                _thermalImageCache = null;
            }

            if (ImageChanged != null)
            {
                ImageChanged(this, new EventArgs());
            }
        }

        private void DoMappingInfoChanged()
        {
            if (MappingInfoChanged != null)
            {
                MappingInfoChanged(this, new EventArgs());
            }
        }

        private void DoViewInfoChanged()
        {
            if (ViewInfoChanged != null)
            {
                ViewInfoChanged(this, new EventArgs());
            }
        }

        private void DoMirrorTypeChanged()
        {
            if (MirrorTypeChanged != null)
            {
                MirrorTypeChanged(this, new EventArgs());
            }
        }

        private void DoRotationTypeChanged()
        {
            if (RotationTypeChanged != null)
            {
                RotationTypeChanged(this, new EventArgs());
            }
        }

        private void DoGainChanged()
        {
            if (GainChanged != null)
            {
                GainChanged(this, new EventArgs());
            }
        }

        private void DoReferenceChanged()
        {
            _referenceCache = GetReference();
            if (ReferenceChanged != null)
            {
                ReferenceChanged(this, new EventArgs());
            }
        }

        private void DoThermalParamsChanged()
        {
            if (ThermalParamsChanged != null)
            {
                ThermalParamsChanged(this, new EventArgs());
            }
        }

        private void DoMarkerAdded(IMarker marker)
        {
            if (MarkerAdded != null)
            {
                MarkerAdded(this, new MarkerEventArgs(marker, MarkerChangeFlags.Default));
            }
        }

        private void DoMarkerChanged(IMarker marker, MarkerChangeFlags changeFlags)
        {
            if (MarkerChanged != null)
            {
                MarkerChanged(this, new MarkerEventArgs(marker, changeFlags));
            }
        }

        private void DoMarkerRemoved(IMarker marker)
        {
            if (MarkerRemoved != null)
            {
                MarkerRemoved(this, new MarkerEventArgs(marker, MarkerChangeFlags.Default));
            }
        }

        private void DoMarkerRenamed(IMarker marker, string oldName)
        {
            if (MarkerChanged != null)
            {
                MarkerChanged(this, new MarkerEventArgs(marker, MarkerChangeFlags.Name, oldName));
            }
        }

        private void DoDeltaAdded(IDelta delta)
        {
            if (DeltaAdded != null)
            {
                DeltaAdded(this, new DeltaEventArgs(delta));
            }
        }

        private void DoDeltaChanged(IDelta delta)
        {
            if (DeltaChanged != null)
            {
                DeltaChanged(this, new DeltaEventArgs(delta));
            }
        }

        private void DoDeltaRemoved(IDelta delta)
        {
            if (DeltaRemoved != null)
            {
                DeltaRemoved(this, new DeltaEventArgs(delta));
            }
        }

        private void DoFrameIndexChanged()
        {
            if (FrameIndexChanged != null)
            {
                FrameIndexChanged(this, new EventArgs());
            }
        }

        private void DoModifiedStatusChanged()
        {
            if (ModifiedStatusChanged != null)
            {
                ModifiedStatusChanged(this, new EventArgs());
            }
        }

        private void HandleAlarmChangedEvent()
        {
            DoImageChanged();
            DoPaletteChanged();
            DoAlarmChanged();
        }

        private void HandleMappingInfoChangedEvent()
        {
            ViewInfo viewInfo = GetViewInfo();
            if (viewInfo.Type != ViewType.Thermal)
            {
                DoImageChanged();
            }
            DoMappingInfoChanged();
        }

        private void HandleViewInfoChangedEvent()
        {
            DoImageChanged();
            DoViewInfoChanged();
        }

        private void HandleMirrorTypeChangedEvent()
        {
            DoImageChanged();

            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i] as Marker;
                marker.Sync();
                DoMarkerChanged(marker, MarkerChangeFlags.Shape);
            }

            DoMirrorTypeChanged();
        }

        private void HandleRotationTypeChangedEvent()
        {
            DoImageChanged();

            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i] as Marker;
                marker.Sync();
                DoMarkerChanged(marker, MarkerChangeFlags.Shape);
            }

            DoRotationTypeChanged();
        }

        private void HandleGainChangedEvent()
        {
            DoImageChanged();
            DoPaletteChanged();
            DoGainChanged();
        }

        private void HandlePaletteChangedEvent()
        {
            DoImageChanged();
            DoPaletteChanged();
        }

        private void HandleThermalParamsChangedEvent()
        {
            for (int i = 0; i < _deltas.Count; i++)
            {
                Delta delta = _deltas[i] as Delta;
                delta.Sync();
                DoDeltaChanged(delta);
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i] as Marker;
                marker.Sync();
                DoMarkerChanged(marker, MarkerChangeFlags.ThermalParams);
            }

            DoThermalParamsChanged();
        }

        private void HandleReferenceChangedEvent()
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                Marker marker = _markers[i] as Marker;
                marker.Sync();
                DoMarkerChanged(marker, MarkerChangeFlags.Reference);
            }

            DoReferenceChanged();
        }

        private void HandleColorDistributionChangedEvent()
        {
            DoImageChanged();
            DoPaletteChanged();
        }

        private void HandleMarkerAddedEvent(IMarker marker)
        {
            DoMarkerAdded(marker);
        }

        private void HandleMarkerChangedEvent(IMarker marker, MarkerChangeFlags changeFlags)
        {
            for (int i = 0; i < _deltas.Count; i++)
            {
                Delta delta = _deltas[i] as Delta;
                DeltaParams deltaParams = delta.DeltaParams;
                if (string.Equals(deltaParams.SrcName, marker.Name) || string.Equals(deltaParams.DestName, marker.Name))
                {
                    delta.Sync();
                    DoDeltaChanged(delta);
                }
            }

            DoMarkerChanged(marker, changeFlags);
        }

        private void HandleMarkerRemovedEvent(IMarker marker)
        {
            if (string.Equals(_referenceCache.Name, marker.Name))
            {
                HandleReferenceChangedEvent();
            }

            List<string> deltaNames = new List<string>(GetDeltaNames());
            for (int i = 0; i < _deltas.Count; i++)
            {
                IDelta delta = _deltas[i];
                if (!deltaNames.Contains(delta.Name))
                {
                    _deltas.RemoveAt(i);
                    DoDeltaRemoved(delta);
                }
            }

            DoMarkerRemoved(marker);
        }

        private void HandleMarkerRenamedEvent(IMarker marker, string oldName)
        {
            Reference reference = GetReference();
            if (string.Equals(reference.Name, oldName))
            {
                DoReferenceChanged();
            }

            for (int i = 0; i < _deltas.Count; i++)
            {
                Delta delta = _deltas[i] as Delta;
                DeltaParams deltaParams = delta.DeltaParams;
                if (string.Equals(deltaParams.SrcName, oldName) || string.Equals(deltaParams.DestName, oldName))
                {
                    delta.Sync();
                    DoDeltaChanged(delta);
                }
            }

            DoMarkerRenamed(marker, oldName);
        }

        private void HandleDeltaAddedEvent(IDelta delta)
        {
            DoDeltaAdded(delta);
        }

        private void HandleDeltaChangedEvent(IDelta delta)
        {
            DoDeltaChanged(delta);
        }

        private void HandleDeltaRemovedEvent(IDelta delta)
        {
            DoDeltaRemoved(delta);
        }

        private void HandleFrameIndexChangedEvent()
        {
            for (int i = 0; i < _deltas.Count; i++)
            {
                IDelta delta = _deltas[i];
                (delta as Delta).Sync();
                DoDeltaChanged(delta);
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                IMarker marker = _markers[i];
                (marker as Marker).Sync();
                DoMarkerChanged(marker, MarkerChangeFlags.FrameIndex);
            }

            Gain gain = GetGain();
            if (gain.Mode != GainMode.Fixed)
            {
                HandleGainChangedEvent();
            }
            else
            {
                DoImageChanged();
                DoPaletteChanged();
            }

            DoFrameIndexChanged();
        }

        private void HandleModifiedStatusChangedEvent()
        {
            DoModifiedStatusChanged();
        }

        private string[] GetDeltaNames()
        {
            if (HImage == IntPtr.Zero)
            {
                return new string[0];
            }

            IRtekFileSDK.ir_file_delta_names irDeltaNameList = new IRtekFileSDK.ir_file_delta_names();
            int result = IRtekFileSDK.ir_file_get_delta_names(HImage, ref irDeltaNameList);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int bufferSize = irDeltaNameList.count * IRtekFileSDK.NAME_LEN;
                    irDeltaNameList.names = Marshal.AllocHGlobal(bufferSize);
                    result = IRtekFileSDK.ir_file_get_delta_names(HImage, ref irDeltaNameList);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        byte[] buffer = new byte[bufferSize];
                        Marshal.Copy(irDeltaNameList.names, buffer, 0, bufferSize);
                        string[] markerNames = new string[irDeltaNameList.count];
                        for (int i = 0; i < irDeltaNameList.count; i++)
                        {
                            markerNames[i] = StringConvertHelper.BytesToString(buffer, i * IRtekFileSDK.NAME_LEN, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                        }
                        return markerNames;
                    }
                    else
                    {
                        return new string[0];
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irDeltaNameList.names);
                }
            }
            else
            {
                return new string[0];
            }
        }

        private string[] GetMarkerNames()
        {
            if (HImage == IntPtr.Zero)
            {
                return new string[0];
            }

            IRtekFileSDK.ir_file_marker_names irMarkerNameList = new IRtekFileSDK.ir_file_marker_names();
            int result = IRtekFileSDK.ir_file_get_marker_names(HImage, ref irMarkerNameList);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                try
                {
                    int bufferSize = irMarkerNameList.count * IRtekFileSDK.NAME_LEN;
                    irMarkerNameList.names = Marshal.AllocHGlobal(bufferSize);
                    result = IRtekFileSDK.ir_file_get_marker_names(HImage, ref irMarkerNameList);
                    if (result == IRtekFileSDK.IR_FILE_EC_OK)
                    {
                        byte[] buffer = new byte[bufferSize];
                        Marshal.Copy(irMarkerNameList.names, buffer, 0, bufferSize);
                        string[] markerNames = new string[irMarkerNameList.count];
                        for (int i = 0; i < irMarkerNameList.count; i++)
                        {
                            markerNames[i] = StringConvertHelper.BytesToString(buffer, i * IRtekFileSDK.NAME_LEN, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                        }
                        return markerNames;
                    }
                    else
                    {
                        return new string[0];
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irMarkerNameList.names);
                }
            }
            else
            {
                return new string[0];
            }
        }
        #endregion
    }
}
