using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amur.IRtekACFileSDK
{
    public sealed class IRtekACFileSDKValidator
    {
        private static IRtekACFileSDKValidator instance = new IRtekACFileSDKValidator();

        private IRtekACFileSDKValidator()
        { }

        public static IRtekACFileSDKValidator Instance => instance;

        public bool IsValidFile(string fileName, out FileType fileType)
        {
            int acFileType = 0;
            fileType = FileType.Other;
            int result = IRtekACFileSDK.ac_file_peek_file_type(fileName, ref acFileType);

            if (result == IRtekACFileSDK.AC_FILE_EC_OK)
            {
                switch (acFileType)
                {
                    case IRtekACFileSDK.AC_FILE_PICTURE:
                        fileType = FileType.AcousticPicture;
                        return true;

                    case IRtekACFileSDK.AC_FILE_VIDEO:
                        fileType = FileType.AcousticVideo;
                        return true;

                    default:
                        return false;
                }
            }

            return false;
        }

        public AcousticImageFile Create()
        {
            return new AcousticImageFile();
        }
    }
}