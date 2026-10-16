using System.Drawing;

namespace Amur.ThermalImageFile
{
    public interface IDeltaFactory
    {
        IDelta CreateDelta(string deltaName, DeltaParams deltaParams);
    }
}
