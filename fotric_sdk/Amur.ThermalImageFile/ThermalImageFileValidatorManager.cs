using System.Collections.Generic;

namespace Amur.ThermalImageFile
{
    public sealed class ThermalImageFileValidatorManager
    {
        #region Fields
        private Dictionary<string, IThermalImageFileValidator> _validators;

        public static ThermalImageFileValidatorManager Instance;
        #endregion

        #region Constructors
        private ThermalImageFileValidatorManager()
        {
            _validators = new Dictionary<string, IThermalImageFileValidator>();
        }

        static ThermalImageFileValidatorManager()
        {
            if (Instance == null)
            {
                Instance = new ThermalImageFileValidatorManager();
            }
        }
        #endregion

        #region Properties

        #endregion

        #region Methods
        public void AddValidator(IThermalImageFileValidator validator)
        {
            if (_validators.ContainsKey(validator.Name))
            {
                return;
            }

            _validators.Add(validator.Name, validator);
        }

        public bool Validate(string fileName, out IThermalImageFileValidator validator, out FileType fileType)
        {
            validator = null;
            fileType = FileType.Other;

            foreach (IThermalImageFileValidator v in _validators.Values)
            {
                fileType = v.IsValidFile(fileName);
                if (fileType != FileType.Other)
                {
                    validator = v;
                    return true;
                }
            }

            return false;
        }

        public bool Create(string fileName, out IThermalImageFile validator)
        {
            validator = null;
            foreach (IThermalImageFileValidator v in _validators.Values)
            {
                if (v.IsValidFile(fileName) != FileType.Other)
                {
                    validator = v.Create();
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
