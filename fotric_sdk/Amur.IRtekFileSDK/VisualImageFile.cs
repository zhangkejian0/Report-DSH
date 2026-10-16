using Amur.ThermalImageFile;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Amur.IRtekFileSDK
{
    public class VisualImageFile : IVisualImageFile
    {
        private const byte TRUE = 0x01;
        private const byte FALSE = 0x00;

        public VisualImageFile()
        {
            HImage = IntPtr.Zero;
            IsModified = false;
        }

        protected IntPtr HImage { get; set; }

        public bool IsModified { get; protected set; }

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
                        case IRtekFileSDK.IR_FILE_VISUAL_PICTURE:
                            return FileType.VisualPicture;
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

        public Bitmap CoverImage
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return null;
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
                        return new Bitmap(ms);
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
        }

        public GeoLocation GeoLocation
        {
            get
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
                    throw new Exception(string.Format("invoke ir_file_get_geo_location error code {0}.", result));
                }
            }
        }

        public string NoteAnnotation
        {
            get
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
                            throw new Exception(string.Format("invoke ir_file_get_note error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irNote.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_note error code {0}.", result));
                }
            }
            set
            {
                if (HImage == IntPtr.Zero)
                {
                    return;
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
                        throw new Exception(string.Format("invoke ir_file_set_note error code {0}.", result));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(irNote.buffer);
                }
            }
        }

        public DateTime ShotTime
        {
            get
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
                    TimeSpan tsSpan = new TimeSpan((long)lSpan * TimeSpan.TicksPerMillisecond);
                    return dtStart + tsSpan;
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_shot_time error code {0}.", result));
                }
            }
        }

        public bool Open(string fileName)
        {
            if (HImage != IntPtr.Zero)
            {
                IRtekFileSDK.ir_file_close(HImage);
                HImage = IntPtr.Zero;
            }

            IntPtr h = IntPtr.Zero;
            int result = IRtekFileSDK.ir_file_open(fileName, ref h);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                HImage = h;

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
                case FileType.VisualPicture:
                    irFileType = IRtekFileSDK.IR_FILE_VISUAL_PICTURE;
                    break;
                default:
                    return false;
            }

            IntPtr h = IntPtr.Zero;
            int result = IRtekFileSDK.ir_file_create(fileName, irFileType, ref h);
            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                HImage = h;
                return true;
            }

            return false;
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

        public void Close()
        {
            if (HImage == IntPtr.Zero)
            {
                return;
            }

            IRtekFileSDK.ir_file_close(HImage);
            HImage = IntPtr.Zero;
            IsModified = false;
        }

        public bool SaveAsFJPG(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
