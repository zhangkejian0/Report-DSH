using System;
using System.Collections.Generic;
using System.Drawing;
using Amur.ThermalImageFile;

namespace Amur.IRtekFileSDK
{
    internal partial class ThermalImage
    {
        #region InnerClasses
        internal abstract class Marker : IMarker
        {
            #region Fields
            private string _name;
            private LocalThermalParams _thermalParams;
            #endregion

            #region Constructors
            public Marker(string name, LocalThermalParams thermalParams)
            {
                _name = name;
                _thermalParams = thermalParams;

                MaxInternal = 313.15F;
                MinInternal = 293.15F;
                AvgInternal = 303.15F;
                HotSpotInternal = Point.Empty;
                ColdSpotInternal = Point.Empty;
                MeasuredValueInternal = 0.0F;
            }
            #endregion

            #region Properties
            public IThermalImage Owner { get; internal set; }

            public string Name 
            { 
                get { return _name; }
                set { throw new InvalidProgramException(); }
            }

            public LocalThermalParams ThermalParams 
            { 
                get { return _thermalParams; }
                set { throw new InvalidProgramException(); }
            }
            
            public abstract MarkerType Type { get; }

            internal float MaxInternal { get; set; }
            internal float MinInternal { get; set; }
            internal float AvgInternal { get; set; }
            internal Point HotSpotInternal { get; set; }
            internal Point ColdSpotInternal { get; set; }
            internal float MeasuredValueInternal { get; set; }
            #endregion
        }

        internal class MarkerSpot : Marker, IMarkerSpot
        {
            #region Fields
            private Point _point;
            #endregion

            #region Constructors
            public MarkerSpot(string name, LocalThermalParams thermalParams, Point point)
                : base(name, thermalParams)
            {
                _point = point;
            }
            #endregion

            #region Properties
            public override MarkerType Type
            {
                get { return MarkerType.Spot; }
            }

            public Point Point
            {
                get { return _point; }
                set { throw new InvalidProgramException(); }
            }

            public float Value
            {
                get { return MaxInternal; }
            }
            #endregion
        }

        internal class MarkerRectangle : Marker, IMarkerRectangle
        {
            #region Fields
            private Rectangle _rectangle;
            #endregion

            #region Constructors
            public MarkerRectangle(string name, LocalThermalParams thermalParams, Rectangle rectangle)
                : base(name, thermalParams)
            {
                _rectangle = rectangle;
            }
            #endregion

            #region Properties
            public override MarkerType Type
            {
                get { return MarkerType.Rectangle; }
            }

            public Rectangle Rectangle
            {
                get { return _rectangle; }
                set { throw new InvalidProgramException(); }
            }

            public float Max
            {
                get { return MaxInternal; }
            }

            public float Min
            {
                get { return MinInternal; }
            }

            public float Avg
            {
                get { return AvgInternal; }
            }

            public Point HotSpot
            {
                get { return HotSpotInternal; }
            }

            public Point ColdSpot
            {
                get { return ColdSpotInternal; }
            }

            public float Area
            {
                get { return MeasuredValueInternal; }
            }
            #endregion
        }

        internal class MarkerEllipse : Marker, IMarkerEllipse
        {
            #region Fields
            private Rectangle _rectangle;
            #endregion

            #region Constructors
            public MarkerEllipse(string name, LocalThermalParams thermalParams, Rectangle rectangle)
                : base(name, thermalParams)
            {
                _rectangle = rectangle;
            }
            #endregion

            #region Properties
            public override MarkerType Type
            {
                get { return MarkerType.Ellipse; }
            }

            public Rectangle Rectangle
            {
                get { return _rectangle; }
                set { throw new InvalidProgramException(); }
            }

            public float Max
            {
                get { return MaxInternal; }
            }

            public float Min
            {
                get { return MinInternal; }
            }

            public float Avg
            {
                get { return AvgInternal; }
            }

            public Point HotSpot
            {
                get { return HotSpotInternal; }
            }

            public Point ColdSpot
            {
                get { return ColdSpotInternal; }
            }

            public float Area
            {
                get { return MeasuredValueInternal; }
            }
            #endregion
        }

        internal class MarkerPolygon : Marker, IMarkerPolygon
        {
            #region Fields
            private Point[] _points;
            #endregion

            #region Constructors
            public MarkerPolygon(string name, LocalThermalParams thermalParams, Point[] points)
                : base(name, thermalParams)
            {
                _points = points;
            }
            #endregion

            #region Properties
            public override MarkerType Type
            {
                get { return MarkerType.Polygon; }
            }

            public Point[] Points
            {
                get { return (Point[])_points.Clone(); }
                set { throw new InvalidProgramException(); }
            }

            public float Max
            {
                get { return MaxInternal; }
            }

            public float Min
            {
                get { return MinInternal; }
            }

            public float Avg
            {
                get { return AvgInternal; }
            }

            public Point HotSpot
            {
                get { return HotSpotInternal; }
            }

            public Point ColdSpot
            {
                get { return ColdSpotInternal; }
            }

            public float Area
            {
                get { return MeasuredValueInternal; }
            }
            #endregion
        }

        internal class MarkerPolyline : Marker, IMarkerPolyline
        {
            #region Fields
            private Point[] _points;
            #endregion

            #region Constructors
            public MarkerPolyline(string name, LocalThermalParams thermalParams, Point[] points)
                : base(name, thermalParams)
            {
                _points = points;
            }
            #endregion

            #region Properties
            public override MarkerType Type
            {
                get { return MarkerType.Polyline; }
            }

            public Point[] Points
            {
                get { return (Point[])_points.Clone(); }
                set { throw new InvalidProgramException(); }
            }

            public float Max
            {
                get { return MaxInternal; }
            }

            public float Min
            {
                get { return MinInternal; }
            }

            public float Avg
            {
                get { return AvgInternal; }
            }

            public Point HotSpot
            {
                get { return HotSpotInternal; }
            }

            public Point ColdSpot
            {
                get { return ColdSpotInternal; }
            }

            public float Length
            {
                get { return MeasuredValueInternal; }
            }
            #endregion
        }

        internal class MarkerCollection : IMarkerCollection
        {
            #region Fields
            private readonly List<IMarker> _entries = new List<IMarker>();
            #endregion

            #region Constructors
            public MarkerCollection(ThermalImage owner)
            {
                Owner = owner;
            }
            #endregion

            #region Properties
            public IThermalImage Owner { get; private set; }

            public int Count
            {
                get { return _entries.Count; }
            }

            public IMarker this[int index]
            {
                get { return _entries[index]; }
            }

            public IMarker this[string name]
            {
                get
                {
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        if (string.Equals(_entries[i].Name, name))
                        {
                            return _entries[i];
                        }
                    }
                    throw new Exception("name not existed");
                }
            }

            public string NextSpotName
            {
                get { throw new InvalidProgramException(); }
            }

            public string NextAreaName
            {
                get { throw new InvalidProgramException(); }
            }

            public string NextPolylineName
            {
                get { throw new InvalidProgramException(); }
            }
            #endregion

            #region  Methods
            public int IndexOf(string name)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (string.Equals(_entries[i].Name, name))
                    {
                        return i;
                    }
                }
                return -1;
            }

            internal void AddInternal(IMarker marker)
            {
                _entries.Add(marker);
            }

            public void Add(IMarker marker)
            {
                throw new InvalidProgramException();
            }

            public bool Remove(string name)
            {
                throw new InvalidProgramException();
            }

            public void RemoveAt(int index)
            {
                throw new InvalidProgramException();
            }

            public void Clear(ClearType clearType)
            {
                throw new InvalidProgramException();
            }

            internal void Clear()
            {
                _entries.Clear();
            }

            public System.Collections.IEnumerator GetEnumerator()
            {
                return _entries.GetEnumerator();
            }
            #endregion
        }
        #endregion
    }
}
