using Amur.ThermalImageFile;
using System;
using System.Drawing;

namespace Amur.IRtekFileSDK
{
    internal partial class ThermalImage : IThermalImage
    {
        #region Constructors
        internal ThermalImage(ThermalImageFile owner)
        {
            Owner = owner;
        }
        #endregion

        #region Properties
        public IThermalImageFile Owner { get; private set; }
        public IMarkerCollection Markers { get; internal set; }
        public IDeltaCollection Deltas { get; internal set; }
        internal DateTime ShotTime { get; set; }
        #endregion

        #region Methods
        public DateTime GetShotTime()
        {
            return ShotTime;
        }
        #endregion
    }
}
