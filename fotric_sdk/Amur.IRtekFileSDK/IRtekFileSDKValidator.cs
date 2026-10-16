using Amur.ThermalImageFile;

namespace Amur.IRtekFileSDK
{
    public sealed class IRtekFileSDKValidator : IThermalImageFileValidator
    {
        #region Fields
        public static IRtekFileSDKValidator Instance;

        private const string VALIDATOR_NAME = "IRtekFileSDK";
        #endregion

        #region Constrctors
        private IRtekFileSDKValidator()
        {
        }

        static IRtekFileSDKValidator()
        {
            Instance = new IRtekFileSDKValidator();
        }
        #endregion

        #region Properties
        public string Name
        {
            get { return VALIDATOR_NAME; }
        }
        #endregion

        #region Methods
        public IThermalImageFile Create()
        {
            return new ThermalImageFile();
        }

        public FileType IsValidFile(string fileName)
        {
            int irFileType = 0;
            int result = IRtekFileSDK.ir_file_peek_file_type(fileName, ref irFileType);

            if (result == IRtekFileSDK.IR_FILE_EC_OK)
            {
                switch (irFileType)
                {
                    case IRtekFileSDK.IR_FILE_THERMAL_PICTURE:
                        return FileType.ThermalPicture;
                    case IRtekFileSDK.IR_FILE_THERMAL_VIDEO:
                        return FileType.ThermalVideo;
                    case IRtekFileSDK.IR_FILE_VISUAL_PICTURE:
                        return FileType.VisualPicture;
                    case IRtekFileSDK.IR_FILE_GRID_THERMAL_PICTURE:
                        return FileType.GridThermalPicture;
                    default:
                        return FileType.Other;
                }
            }
            else
            {
                return FileType.Other;
            }
        }
        #endregion
    }
}