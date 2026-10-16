using System.Drawing;

namespace Amur.ThermalImageFile
{
    public interface IMarkerFactory
    {
        IMarkerSpot CreateMarkerSpot(string name, LocalThermalParams thermalParams, Point point);
        IMarkerRectangle CreateMarkerRectangle(string name, LocalThermalParams thermalParams, Rectangle rectangle);
        IMarkerEllipse CreateMarkerEllipse(string name, LocalThermalParams thermalParams, Rectangle rectangle);
        IMarkerPolygon CreateMarkerPolygon(string name, LocalThermalParams thermalParams, Point[] points);
        IMarkerPolyline CreateMarkerPolyline(string name, LocalThermalParams thermalParams, Point[] points);
    }
}
