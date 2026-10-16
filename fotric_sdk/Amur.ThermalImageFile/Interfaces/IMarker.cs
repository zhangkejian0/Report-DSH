using System.Collections;
using System.Drawing;

namespace Amur.ThermalImageFile
{
    public interface IMarker
    {
        IThermalImage Owner { get; }
        
        string Name { get; set; }
        LocalThermalParams ThermalParams { get; set; }
        MarkerType Type { get; }
    }

    public interface IMarkerSpot : IMarker
    {
        Point Point { get; set; }

        float Value { get; }
    }

    public interface IMarkerRectangle : IMarker
    {
        Rectangle Rectangle { get; set; }

        float Max { get; }
        float Min { get; }
        float Avg { get; }
        Point ColdSpot { get; }
        Point HotSpot { get; }
        float Area { get; }
    }

    public interface IMarkerEllipse : IMarker
    {
        Rectangle Rectangle { get; set; }

        float Max { get; }
        float Min { get; }
        float Avg { get; }
        float Area { get; }
        Point ColdSpot { get; }
        Point HotSpot { get; }
    }

    public interface IMarkerPolygon : IMarker
    {
        Point[] Points { get; set; }

        float Max { get; }
        float Min { get; }
        float Avg { get; }
        Point ColdSpot { get; }
        Point HotSpot { get; }
        float Area { get; }
    }

    public interface IMarkerPolyline : IMarker
    {
        Point[] Points { get; set; }

        float Max { get; }
        float Min { get; }
        float Avg { get; }
        Point ColdSpot { get; }
        Point HotSpot { get; }
        float Length { get; }
    }

    public interface IMarkerCollection: IEnumerable
    {
        IThermalImage Owner { get; }
        
        IMarker this[int index] { get; }
        IMarker this[string name] { get; }

        int Count { get; }
        string NextAreaName { get; }
        string NextPolylineName { get; }
        string NextSpotName { get; }
        
        void Add(IMarker marker);
        void Clear(ClearType clearType);
        int IndexOf(string name);
        bool Remove(string name);
        void RemoveAt(int index);
    }
}
