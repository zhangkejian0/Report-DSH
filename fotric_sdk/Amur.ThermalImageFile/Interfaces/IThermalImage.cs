using System;

namespace Amur.ThermalImageFile
{
    public interface IThermalImage
    {
        IDeltaCollection Deltas { get; }
        IMarkerCollection Markers { get; }

        DateTime GetShotTime();
    }
}
