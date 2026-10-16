using System;

namespace Amur.ThermalImageFile
{
    [Flags]
    public enum MarkerChangeFlags
    {
        Default = 0,
        Name = 1,
        Shape = 2,
        ThermalParams = 4,
        LocalThermalParams = 8,
        Reference = 16,
        FrameIndex = 32
    }

    public delegate void MarkerEventHandler(object sender, MarkerEventArgs e);

    public class MarkerEventArgs : EventArgs
    {
        public MarkerEventArgs(IMarker marker, MarkerChangeFlags changeFlags)
        {
            Marker = marker;
            ChangeFlags = changeFlags;
            OldName = marker.Name;
        }

        public MarkerEventArgs(IMarker marker, MarkerChangeFlags changeFlags, string oldName)
        {
            Marker = marker;
            ChangeFlags = changeFlags;
            OldName = oldName;
        }

        public IMarker Marker { get; private set; }
        public MarkerChangeFlags ChangeFlags { get; private set; }
        public string OldName { get; private set; }
    }

    public delegate void DeltaEventHandler(object sender, DeltaEventArgs e);

    public class DeltaEventArgs : EventArgs
    {
        public DeltaEventArgs(IDelta delta)
        {
            OldName = delta.Name;
            Delta = delta;
        }

        public DeltaEventArgs(string oldName, IDelta delta)
        {
            OldName = oldName;
            Delta = delta;
        }

        public string OldName { get; private set; }
        public IDelta Delta { get; private set; }
    }
}
