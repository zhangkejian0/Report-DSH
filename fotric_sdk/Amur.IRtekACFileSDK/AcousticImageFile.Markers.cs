// Copyright(c) IRtek.Co.Ltd.All rights reserved.

using System;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Amur.IRtekACFileSDK
{
    partial class AcousticImageFile
    {
        public abstract class Marker
        {
            private string name;

            public Marker(AcousticImageFile owner, string name)
            {
                Owner = owner;
                this.name = name;

                MaxInternal = 313.15F;
                MaxSpotInternal = Point.Empty;
                MeasuredValueInternal = 0.0F;
            }

            public AcousticImageFile Owner { get; private set; }
            public abstract MarkerType Type { get; }

            public string Name
            {
                get
                {
                    return name;
                }
                set
                {
                    if (string.Equals(name, value))
                    {
                        return;
                    }

                    string oldValue = name;
                    name = value;
                    if (Owner.RenameMarker(oldValue, name))
                    {
                        Owner.HandleMarkerRenamedEvent(oldValue, this);
                    }
                    else
                    {
                        name = oldValue;
                    }
                }
            }

            public float MaxInternal { get; set; }
            public Point MaxSpotInternal { get; set; }
            public float MeasuredValueInternal { get; set; }

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

        public class MarkerSpot : Marker
        {
            private Point point;

            public MarkerSpot(AcousticImageFile owner, string name, Point point)
                : base(owner, name)
            {
                this.point = point;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Spot; }
            }

            public Point Point
            {
                get
                {
                    return point;
                }
                set
                {
                    if (point == value)
                    {
                        return;
                    }

                    Point oldValue = point;
                    point = value;
                    if (Owner.SetMarker(this))
                    {
                        Owner.HandleMarkerChangedEvent(this);
                    }
                    else
                    {
                        point = oldValue;
                    }
                }
            }

            public float Value
            {
                get { return MaxInternal; }
            }

            public override void Sync()
            {
                IntPtr hImage = Owner.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekACFileSDK.ac_file_marker irMarker = new IRtekACFileSDK.ac_file_marker();
                irMarker.name = StringToBytes(Name, IRtekACFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekACFileSDK.ac_file_get_marker(hImage, ref irMarker);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    try
                    {
                        int size = 0;
                        unsafe
                        {
                            size = sizeof(IRtekACFileSDK.ac_file_point);
                        }

                        irMarker.points = Marshal.AllocHGlobal(irMarker.point_count * size);
                        result = IRtekACFileSDK.ac_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                        {
                            IRtekACFileSDK.ac_file_point p = (IRtekACFileSDK.ac_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekACFileSDK.ac_file_point));
                            point = new Point(p.x, p.y);

                            float temp = irMarker.db_value;
                            MaxInternal = temp;
                            MeasuredValueInternal = irMarker.db_value;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ac_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_marker error code {0}.", result));
                }
            }
        }

        public class MarkerEllipse : Marker
        {
            private Rectangle rectangle;

            public MarkerEllipse(AcousticImageFile owner, string name, Rectangle rectangle)
                : base(owner, name)
            {
                this.rectangle = rectangle;
            }

            public override MarkerType Type
            {
                get { return MarkerType.Ellipse; }
            }

            public Rectangle Rectangle
            {
                get
                {
                    return rectangle;
                }
                set
                {
                    if (rectangle == value)
                    {
                        return;
                    }

                    Rectangle oldValue = rectangle;
                    rectangle = value;
                    if (Owner.SetMarker(this))
                    {
                        Owner.HandleMarkerChangedEvent(this);
                    }
                    else
                    {
                        rectangle = oldValue;
                    }
                }
            }

            public float Max
            {
                get { return MaxInternal; }
            }

            public Point HotSpot
            {
                get { return MaxSpotInternal; }
            }

            public float Area
            {
                get { return MeasuredValueInternal; }
            }

            public override void Sync()
            {
                IntPtr hImage = Owner.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekACFileSDK.ac_file_marker irMarker = new IRtekACFileSDK.ac_file_marker();
                irMarker.name = StringToBytes(Name, IRtekACFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekACFileSDK.ac_file_get_marker(hImage, ref irMarker);
                if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                {
                    try
                    {
                        int size = 0;
                        unsafe
                        {
                            size = sizeof(IRtekACFileSDK.ac_file_point);
                        }

                        irMarker.points = Marshal.AllocHGlobal(irMarker.point_count * size);
                        result = IRtekACFileSDK.ac_file_get_marker(hImage, ref irMarker);
                        if (result == IRtekACFileSDK.AC_FILE_EC_OK)
                        {
                            Rectangle rect = new Rectangle();
                            IRtekACFileSDK.ac_file_point p = (IRtekACFileSDK.ac_file_point)Marshal.PtrToStructure(irMarker.points, typeof(IRtekACFileSDK.ac_file_point));
                            Point p1 = new Point(p.x, p.y);
                            p = (IRtekACFileSDK.ac_file_point)Marshal.PtrToStructure(irMarker.points + size, typeof(IRtekACFileSDK.ac_file_point));
                            Point p2 = new Point(p.x, p.y);
                            rect.Location = new Point(p1.X <= p2.X ? p1.X : p2.X, p1.Y <= p2.Y ? p1.Y : p2.Y);
                            rect.Size = new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                            rectangle = rect;

                            MaxInternal = irMarker.db_value;
                        }
                        else
                        {
                            throw new Exception(string.Format("invoke ac_file_get_marker error code {0}.", result));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(irMarker.points);
                    }
                }
                else
                {
                    throw new Exception(string.Format("invoke ac_file_get_marker error code {0}.", result));
                }
            }
        }

        public class MarkerCollection : IEnumerable
        {
            private readonly List<Marker> Entries = new List<Marker>();

            public MarkerCollection(AcousticImageFile owner)
            {
                Owner = owner;
            }

            public AcousticImageFile Owner { get; private set; }

            public int Count
            {
                get { return Entries.Count; }
            }

            public Marker this[int index]
            {
                get
                {
                    return Entries[index];
                }
            }

            public Marker this[string name]
            {
                get
                {
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        if (string.Equals(Entries[i].Name, name))
                        {
                            return Entries[i];
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

            public int IndexOf(string name)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (string.Equals(Entries[i].Name, name))
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void Add(Marker marker)
            {
                if (!Owner._isMarkersSynced)
                {
                    Entries.Add(marker);
                }
                else if (Owner.SetMarker(marker))
                {
                    Entries.Add(marker);
                    Owner.HandleMarkerAddedEvent(marker);
                }
                else
                {
                    throw new Exception("add failed");
                }
            }

            public bool Remove(string name)
            {
                int index = -1;
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (string.Equals(Entries[i].Name, name))
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
                Marker marker = Entries[index];
                if (!Owner._isMarkersSynced)
                {
                    Entries.RemoveAt(index);
                }
                else if (Owner.RemoveMarker(marker.Name))
                {
                    Entries.RemoveAt(index);
                    Owner.HandleMarkerRemovedEvent(marker);
                }
                else
                {
                    throw new Exception("remove failed");
                }
            }

            internal void Clear()
            {
                for (int i = Entries.Count - 1; i >= 0; i--)
                {
                    Marker marker = Entries[i];
                    Remove(marker.Name);
                }
            }

            public IEnumerator GetEnumerator()
            {
                return Entries.GetEnumerator();
            }

            private int GetNextIndex(string prefix)
            {
                int index;
                SortedDictionary<int, int> idMap = new SortedDictionary<int, int>();
                for (int i = 0; i < Entries.Count; i++)
                {
                    string name = Entries[i].Name;
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

        public delegate void MarkerEventHandler(object sender, MarkerEventArgs e);

        public class MarkerEventArgs : EventArgs
        {
            public MarkerEventArgs(Marker marker)
            {
                OldName = marker.Name;
                Marker = marker;
            }

            public MarkerEventArgs(string oldName, Marker marker)
            {
                OldName = oldName;
                Marker = marker;
            }

            public string OldName { get; private set; }
            public Marker Marker { get; private set; }
        }
    }
}
