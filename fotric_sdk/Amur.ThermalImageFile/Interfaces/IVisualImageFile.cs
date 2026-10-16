using System;
using System.Drawing;

namespace Amur.ThermalImageFile
{
    public interface IVisualImageFile : IImageFile
    {
        Bitmap CoverImage { get; }
        GeoLocation GeoLocation { get; }
        string NoteAnnotation { get; set; }
        DateTime ShotTime { get; }
    }
}