using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Amur.ThermalImageFile
{
    public interface IPaletteCreator
    {
        List<PresetPalette> GetPresetPalettes();

        Image GetPresetPaletteImage(byte refNo);
    }
}
