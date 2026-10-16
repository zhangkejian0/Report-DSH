// Copyright(c) IRtek.Co.Ltd. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Amur.ThermalImageFile;

namespace Amur.IRtekFileSDK
{
    internal partial class ThermalImageFile
    {
        #region InnerClasses
        internal abstract class Marker : IMarker
        {
            private string _name;
            protected LocalThermalParams _thermalParams;

            public Marker(IThermalImageFile owner, string name, LocalThermalParams thermalParams)
            {
                Owner = owner;
                _name = name;
                _thermalParams = thermalParams;

                MaxInternal = 313.15F;
                MinInternal = 293.15F;
                AvgInternal = 303.15F;
                HotSpotInternal = Point.Empty;
                ColdSpotInternal = Point.Empty;
                MeasuredValueInternal = 0.0F;
            }

            public IThermalImage Owner { get; private set; }

            public string Name
            {
                get
                {
                    return _name;
                }
                set
                {
                    if (string.Equals(_name, value))
                    {
                        return;
                    }

                    string oldValue = _name;
                    _name = value;
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.RenameMarker(oldValue, _name))
                    {
                        ownerImp.HandleMarkerRenamedEvent(this, oldValue);
                    }
                    else
                    {
                        _name = oldValue;
                    }
                }
            }

            public LocalThermalParams ThermalParams
            {
                get
                {
                    return _thermalParams;
                }
                set
                {
                    LocalThermalParams oldValue = _thermalParams;
                    _thermalParams = value;
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetMarker(this))
                    {
                        Reference reference = ownerImp.GetReference();
                        if (reference.Enabled && string.Equals(reference.Name, Name))
                        {
                            ownerImp.HandleReferenceChangedEvent();
                        }

                        ownerImp.HandleMarkerChangedEvent(this, MarkerChangeFlags.LocalThermalParams);
                    }
                    else
                    {
                        _thermalParams = oldValue;
                    }
                }
            }

            public abstract MarkerType Type { get; }

            internal float MaxInternal { get; set; }
            internal float MinInternal { get; set; }
            internal float AvgInternal { get; set; }
            internal Point HotSpotInternal { get; set; }
            internal Point ColdSpotInternal { get; set; }
            internal float MeasuredValueInternal { get; set; }

            public abstract void Sync();

            protected static bool Equals(Point[] srcPoints, Point[] destPoints)
            {
                if (srcPoints == destPoints)
                {
                    return true;
                }

                if (srcPoints != null && destPoints != null && srcPoints.Length == destPoints.Length)
                {
                    for (int i = 0; i < srcPoints.Length; i++)
                    {
                        if (srcPoints[i] != destPoints[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal class MarkerSpot : Marker, IMarkerSpot
        {
            private Point _point;

            public MarkerSpot(IThermalImageFile owner, string name, LocalThermalParams thermalParams, Point point)
                : base(owner, name, thermalParams)
            {
                _point = point;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Spot; }
            }

            public Point Point
            {
                get
                {
                    return _point;
                }
                set
                {
                    if (_point == value)
                    {
                        return;
                    }

                    Point oldValue = _point;
                    _point = value;
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetMarker(this))
                    {
                        Reference reference = ownerImp.GetReference();
                        if (reference.Enabled && string.Equals(reference.Name, Name))
                        {
                            ownerImp.HandleReferenceChangedEvent();
                        }

                        ownerImp.HandleMarkerChangedEvent(this, MarkerChangeFlags.Shape);
                    }
                    else
                    {
                        _point = oldValue;
                    }
                }
            }

            public float Value
            {
                get { return MaxInternal; }
            }

            public override void Sync()
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                IntPtr hImage = ownerImp.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
                irMarker.name = StringConvertHelper.StringToBytes(Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            LocalThermalParams thermalParams = new LocalThermalParams();
                            thermalParams.LocalParams = irMarker.localParams == TRUE;
                            thermalParams.Emissivity = irMarker.emissivity;
                            thermalParams.Distance = irMarker.distance;
                            thermalParams.ReflTemp = irMarker.reflTemp;
                            _thermalParams = thermalParams;

                            IRtekFileSDK.ir_file_point p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                            _point = new Point(p.x, p.y);

                            float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                            MaxInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                            MinInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                            AvgInternal = temp;
                            IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                            HotSpotInternal = new Point(irPoint.x, irPoint.y);
                            irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                            ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                            MeasuredValueInternal = irMarker.measuredValue;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                }
            }
        }

        internal class MarkerRectangle : Marker, IMarkerRectangle
        {
            private Rectangle _rectangle;

            public MarkerRectangle(ThermalImageFile owner, string name, LocalThermalParams thermalParams, Rectangle rectangle)
                : base(owner, name, thermalParams)
            {
                _rectangle = rectangle;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Rectangle; }
            }

            public Rectangle Rectangle
            {
                get
                {
                    return _rectangle;
                }
                set
                {
                    if (_rectangle == value)
                    {
                        return;
                    }

                    Rectangle oldValue = _rectangle;
                    _rectangle = value;
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetMarker(this))
                    {
                        Reference reference = ownerImp.GetReference();
                        if (reference.Enabled && string.Equals(reference.Name, Name))
                        {
                            ownerImp.HandleReferenceChangedEvent();
                        }

                        ownerImp.HandleMarkerChangedEvent(this, MarkerChangeFlags.Shape);
                    }
                    else
                    {
                        _rectangle = oldValue;
                    }
                }
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

            public override void Sync()
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                IntPtr hImage = ownerImp.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
                irMarker.name = StringConvertHelper.StringToBytes(Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            LocalThermalParams thermalParams = new LocalThermalParams();
                            thermalParams.LocalParams = irMarker.localParams == TRUE;
                            thermalParams.Emissivity = irMarker.emissivity;
                            thermalParams.Distance = irMarker.distance;
                            thermalParams.ReflTemp = irMarker.reflTemp;
                            _thermalParams = thermalParams;

                            Rectangle rect = new Rectangle();
                            IRtekFileSDK.ir_file_point p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                            Point p1 = new Point(p.x, p.y);
                            p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekFileSDK.ir_file_point));
                            Point p2 = new Point(p.x, p.y);
                            rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                            rect.Size = new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                            _rectangle = rect;

                            float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                            MaxInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                            MinInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                            AvgInternal = temp;
                            IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                            HotSpotInternal = new Point(irPoint.x, irPoint.y);
                            irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                            ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                            MeasuredValueInternal = irMarker.measuredValue;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                }
            }
        }

        internal class MarkerEllipse : Marker, IMarkerEllipse
        {
            private Rectangle _rectangle;

            public MarkerEllipse(ThermalImageFile owner, string name, LocalThermalParams thermalParams, Rectangle rectangle)
                : base(owner, name, thermalParams)
            {
                _rectangle = rectangle;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Ellipse; }
            }

            public Rectangle Rectangle
            {
                get
                {
                    return _rectangle;
                }
                set
                {
                    if (_rectangle == value)
                    {
                        return;
                    }

                    Rectangle oldValue = _rectangle;
                    _rectangle = value;
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetMarker(this))
                    {
                        Reference reference = ownerImp.GetReference();
                        if (reference.Enabled && string.Equals(reference.Name, Name))
                        {
                            ownerImp.HandleReferenceChangedEvent();
                        }

                        ownerImp.HandleMarkerChangedEvent(this, MarkerChangeFlags.Shape);
                    }
                    else
                    {
                        _rectangle = oldValue;
                    }
                }
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

            public override void Sync()
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                IntPtr hImage = ownerImp.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
                irMarker.name = StringConvertHelper.StringToBytes(Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            LocalThermalParams thermalParams = new LocalThermalParams();
                            thermalParams.LocalParams = irMarker.localParams == TRUE;
                            thermalParams.Emissivity = irMarker.emissivity;
                            thermalParams.Distance = irMarker.distance;
                            thermalParams.ReflTemp = irMarker.reflTemp;
                            base._thermalParams = thermalParams;

                            Rectangle rect = new Rectangle();
                            IRtekFileSDK.ir_file_point p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekFileSDK.ir_file_point));
                            Point p1 = new Point(p.x, p.y);
                            p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekFileSDK.ir_file_point));
                            Point p2 = new Point(p.x, p.y);
                            rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                            rect.Size = new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                            _rectangle = rect;

                            float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                            MaxInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                            MinInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                            AvgInternal = temp;
                            IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                            HotSpotInternal = new Point(irPoint.x, irPoint.y);
                            irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                            ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                            MeasuredValueInternal = irMarker.measuredValue;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                }
            }
        }

        internal class MarkerPolygon : Marker, IMarkerPolygon
        {
            private Point[] _points;

            public MarkerPolygon(ThermalImageFile owner, string name, LocalThermalParams thermalParams, Point[] points)
                : base(owner, name, thermalParams)
            {
                _points = points;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Polygon; }
            }

            public Point[] Points
            {
                get
                {
                    return (Point[])_points.Clone();
                }
                set
                {
                    if (Equals(_points, value))
                    {
                        return;
                    }

                    Point[] oldValue = _points;
                    _points = (Point[])value.Clone();
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetMarker(this))
                    {
                        Reference reference = ownerImp.GetReference();
                        if (reference.Enabled && string.Equals(reference.Name, Name))
                        {
                            ownerImp.HandleReferenceChangedEvent();
                        }

                        ownerImp.HandleMarkerChangedEvent(this, MarkerChangeFlags.Shape);
                    }
                    else
                    {
                        _points = oldValue;
                    }
                }
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

            public override void Sync()
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                IntPtr hImage = ownerImp.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
                irMarker.name = StringConvertHelper.StringToBytes(Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            LocalThermalParams thermalParams = new LocalThermalParams();
                            thermalParams.LocalParams = irMarker.localParams == TRUE;
                            thermalParams.Emissivity = irMarker.emissivity;
                            thermalParams.Distance = irMarker.distance;
                            thermalParams.ReflTemp = irMarker.reflTemp;
                            base._thermalParams = thermalParams;

                            Point[] points = new Point[irMarker.pointCount];
                            for (int i = 0; i < irMarker.pointCount; i++)
                            {
                                IRtekFileSDK.ir_file_point p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + i * size, typeof(IRtekFileSDK.ir_file_point));
                                points[i] = new Point(p.x, p.y);
                            }
                            this._points = points;

                            float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                            MaxInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                            MinInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                            AvgInternal = temp;
                            IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                            HotSpotInternal = new Point(irPoint.x, irPoint.y);
                            irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                            ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                            MeasuredValueInternal = irMarker.measuredValue;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                }
            }
        }

        internal class MarkerPolyline : Marker, IMarkerPolyline
        {
            private Point[] _points;

            public MarkerPolyline(ThermalImageFile owner, string name, LocalThermalParams thermalParams, Point[] points)
                : base(owner, name, thermalParams)
            {
                _points = points;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Polyline; }
            }

            public Point[] Points
            {
                get
                {
                    return (Point[])_points.Clone();
                }
                set
                {
                    if (Equals(_points, value))
                    {
                        return;
                    }

                    Point[] oldValue = _points;
                    _points = (Point[])value.Clone();
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetMarker(this))
                    {
                        Reference reference = ownerImp.GetReference();
                        if (reference.Enabled && string.Equals(reference.Name, Name))
                        {
                            ownerImp.HandleReferenceChangedEvent();
                        }

                        ownerImp.HandleMarkerChangedEvent(this, MarkerChangeFlags.Shape);
                    }
                    else
                    {
                        _points = oldValue;
                    }
                }
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

            public override void Sync()
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                IntPtr hImage = ownerImp.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekFileSDK.ir_file_marker irMarker = new IRtekFileSDK.ir_file_marker();
                irMarker.name = StringConvertHelper.StringToBytes(Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    try
                    {
                        int size = Marshal.SizeOf(typeof(IRtekFileSDK.ir_file_point));
                        irMarker.points = Marshal.AllocHGlobal(irMarker.pointCount * size);
                        result = IRtekFileSDK.ir_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekFileSDK.IR_FILE_EC_OK)
                        {
                            LocalThermalParams thermalParams = new LocalThermalParams();
                            thermalParams.LocalParams = irMarker.localParams == TRUE;
                            thermalParams.Emissivity = irMarker.emissivity;
                            thermalParams.Distance = irMarker.distance;
                            thermalParams.ReflTemp = irMarker.reflTemp;
                            _thermalParams = thermalParams;

                            Point[] points = new Point[irMarker.pointCount];
                            for (int i = 0; i < irMarker.pointCount; i++)
                            {
                                IRtekFileSDK.ir_file_point p = (IRtekFileSDK.ir_file_point)Marshal.PtrToStructure(irMarker.points + i * size, typeof(IRtekFileSDK.ir_file_point));
                                points[i] = new Point(p.x, p.y);
                            }
                            this._points = points;

                            float temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MAX];
                            MaxInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_MIN];
                            MinInternal = temp;
                            temp = irMarker.tempValues[IRtekFileSDK.IR_FILE_TEMP_AVG];
                            AvgInternal = temp;
                            IRtekFileSDK.ir_file_point irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_HOT];
                            HotSpotInternal = new Point(irPoint.x, irPoint.y);
                            irPoint = irMarker.tempSpots[IRtekFileSDK.IR_FILE_SPOT_COLD];
                            ColdSpotInternal = new Point(irPoint.x, irPoint.y);
                            MeasuredValueInternal = irMarker.measuredValue;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_marker error code {0}.", result));
                }
            }
        }

        internal class MarkerCollection : IMarkerCollection
        {
            private readonly List<IMarker> _entries = new List<IMarker>();

            public MarkerCollection(IThermalImage owner)
            {
                Owner = owner;
            }

            public IThermalImage Owner { get; private set; }

            public int Count
            {
                get { return _entries.Count; }
            }

            public IMarker this[int index]
            {
                get
                {
                    return _entries[index];
                }
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
                get
                {
                    return string.Format("{0}{1}", SPOT_PREFIX, GetNextIndex(SPOT_PREFIX));
                }
            }

            public string NextAreaName
            {
                get
                {
                    return string.Format("{0}{1}", AREA_PREFIX, GetNextIndex(AREA_PREFIX));
                }
            }

            public string NextPolylineName
            {
                get
                {
                    return string.Format("{0}{1}", POLYLINE_PREFIX, GetNextIndex(POLYLINE_PREFIX));
                }
            }

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

            public void Add(IMarker marker)
            {
                ThermalImageFile ownerImpl = Owner as ThermalImageFile;
                if (!ownerImpl.IsMarkersSynced)
                {
                    _entries.Add(marker);
                }
                else if (ownerImpl.SetMarker(marker))
                {
                    _entries.Add(marker);
                    ownerImpl.HandleMarkerAddedEvent(marker);
                }
                else
                {
                    throw new Exception("add failed");
                }
            }

            public bool Remove(string name)
            {
                int index = -1;
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (string.Equals(_entries[i].Name, name))
                    {
                        index = i;
                    }
                }

                if (index == -1)
                {
                    return false;
                }

                RemoveAt(index);

                return true;
            }

            public void RemoveAt(int index)
            {
                IMarker marker = _entries[index];
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                if (!ownerImp.IsMarkersSynced)
                {
                    _entries.RemoveAt(index);
                }
                else if (ownerImp.RemoveMarker(marker.Name))
                {
                    _entries.RemoveAt(index);
                    ownerImp.HandleMarkerRemovedEvent(marker);
                }
                else
                {
                    throw new Exception("remove failed");
                }
            }

            public void Clear(ClearType clearType)
            {
                switch (clearType)
                {
                    case ClearType.UserMarkers:
                        for (int i = _entries.Count - 1; i >= 0; i--)
                        {
                            IMarker marker = _entries[i];
                            if (!string.Equals(marker.Name, IRtekFileSDK.GLOBAL_MARKER))
                            {
                                Remove(marker.Name);
                            }
                        }
                        break;
                }
            }

            internal void Clear()
            {
                for (int i = _entries.Count - 1; i >= 0; i--)
                {
                    IMarker marker = _entries[i];
                    Remove(marker.Name);
                }
            }

            public IEnumerator GetEnumerator()
            {
                return _entries.GetEnumerator();
            }

            private int GetNextIndex(string prefix)
            {
                int index;
                SortedDictionary<int, int> idMap = new SortedDictionary<int, int>();
                for (int i = 0; i < _entries.Count; i++)
                {
                    string name = _entries[i].Name;
                    if (name.StartsWith(prefix))
                    {
                        if (int.TryParse(name.Substring(prefix.Length), out index))
                        {
                            idMap.Add(index, index);
                        }
                    }
                }

                index = 1;
                foreach (KeyValuePair<int, int> itr in idMap)
                {
                    if (itr.Key != index)
                    {
                        break;
                    }
                    index++;
                }
                return index;
            }
        }
        #endregion
    }
}
