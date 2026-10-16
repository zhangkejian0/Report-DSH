using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Amur.IRtekACFileSDK
{
    public class VisualImageFile : ImageBase
    {
        public VisualImageFile()
        {
        }

        public Bitmap CoverImage
        {
            get
            {
                if (HImage == IntPtr.Zero)
                {
                    return null;
                }

                IRtekACFileSDK.ac_file_buffer irBuffer = new IRtekACFileSDK.ac_file_buffer();
                IRtekACFileSDK.ac_file_get_cover_image(HImage, ref irBuffer);
                if (irBuffer.length != 0)
                {
                    try
                    {
                        irBuffer.buffer = Marshal.AllocHGlobal(irBuffer.length);
                        IRtekACFileSDK.ac_file_get_cover_image(HImage, ref irBuffer);
                        byte[] buffer = new byte[irBuffer.length];
                        Marshal.Copy(irBuffer.buffer, buffer, 0, irBuffer.length);
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

                IRtekACFileSDK.ac_file_geo_location irGeoLocation = new IRtekACFileSDK.ac_file_geo_location();
                int result = IRtekACFileSDK.ac_file_get_geo_location(HImage, ref irGeoLocation);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
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
                    throw new Exception(string.Format("invoke ac_file_get_geo_location error code {0}.", result));
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

                IRtekACFileSDK.ac_file_buffer irNote = new IRtekACFileSDK.ac_file_buffer();
                int result = IRtekACFileSDK.ac_file_get_note(HImage, ref irNote);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    try
                    {
                        irNote.buffer = Marshal.AllocHGlobal(irNote.length);
                        result = IRtekACFileSDK.ac_file_get_note(HImage, ref irNote);
                        if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                        {
                            byte[] buffer = new byte[irNote.length];
                            Marshal.Copy(irNote.buffer, buffer, 0, irNote.length);
                            return BytesToString(buffer, Encoding.UTF8);
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ac_file_get_note error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irNote.buffer);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_note error code {0}.", result));
                }
            }
            set
            {
                if (HImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekACFileSDK.ac_file_buffer irNote = new IRtekACFileSDK.ac_file_buffer();
                byte[] buffer = StringToBytes(value, Encoding.UTF8);
                try
                {
                    irNote.length = buffer.Length;
                    irNote.buffer = Marshal.AllocHGlobal(irNote.length);
                    Marshal.Copy(buffer, 0, irNote.buffer, irNote.length);
                    int result = IRtekACFileSDK.ac_file_set_note(HImage, ref irNote);
                    if (result != IRtekACFileSDK.AC_FILE_EC_OK)
                    {
                        throw new Exception(string.Format("invoke ac_file_set_note error code {0}.", result));
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
                int result = IRtekACFileSDK.ac_file_get_shot_time(HImage, ref lSpan);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    DateTime dtStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    TimeSpan tsSpan = new TimeSpan((long)lSpan * TimeSpan.TicksPerMillisecond);
                    return (dtStart + tsSpan).ToLocalTime();
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_shot_time error code {0}.", result));
                }
            }
        }

        public void Open(string fileName)
        {
            if (HImage != IntPtr.Zero)
            {
                IRtekACFileSDK.ac_file_close(HImage);
                HImage = IntPtr.Zero;
            }

            IntPtr h = IntPtr.Zero;
            int result = IRtekACFileSDK.ac_file_open(fileName, ref h);
            if (result == IRtekACFileSDK.AC_FILE_EC_OK)
            {
                HImage = h;
            }
            else
            {
                throw new Exception(string.Format("invoke ac_file_open error code {0}.", result));
            }
        }

        public void Save()
        {
            if (HImage == IntPtr.Zero)
            {
                return;
            }

            IRtekACFileSDK.ac_file_save(HImage);
        }

        public void Close()
        {
            if (HImage == IntPtr.Zero)
            {
                return;
            }

            IRtekACFileSDK.ac_file_close(HImage);
            HImage = IntPtr.Zero;
        }
    }
}