// Copyright(c) IRtek.Co.Ltd. All rights reserved.
using System;
using System.Drawing;

namespace Amur.ThermalImageFile
{
    public interface IThermalImageFile : IThermalImage, IImageFile, IPaletteCreator, IMarkerFactory, IDeltaFactory
    {
        event MarkerEventHandler MarkerAdded;
        event MarkerEventHandler MarkerChanged;
        event MarkerEventHandler MarkerRemoved;
        event DeltaEventHandler DeltaAdded;
        event DeltaEventHandler DeltaChanged;
        event DeltaEventHandler DeltaRemoved;
        event EventHandler ThermalParamsChanged;
        event EventHandler ImageChanged;
        event EventHandler PaletteChanged;
        event EventHandler GainChanged;
        event EventHandler RotationTypeChanged;
        event EventHandler MirrorTypeChanged;
        event EventHandler ViewInfoChanged;
        event EventHandler MappingInfoChanged;
        event EventHandler AlarmChanged;
        event EventHandler ReferenceChanged;
        event EventHandler FrameIndexChanged;
        event EventHandler ModifiedStatusChanged;

        CameraInfo GetCameraInfo();
        bool SetCameraInfo(CameraInfo value);
        LensInfo GetLensInfo();
        bool SetLensInfo(LensInfo value);
        TempRange GetTempRange();
        bool SetTempRange(TempRange value);
        Size GetThermalSize();
        bool SetThermalSize(Size value);
        GeoLocation GetGeoLocation();
        bool SetGeoLocation(GeoLocation value);
        string GetNoteAnnotation();
        bool SetNoteAnnotation(string value);
        AudioAnnotation GetAudioAnnotation();
        bool SetAudioAnnotation(AudioAnnotation value);
        string GetTagAnnotation();
        bool SetTagAnnotation(string tagValue);
        bool GetFavorite();
        bool SetFavorite(bool value);
        string[] GetGroupFiles();
        bool SetGroupFiles(string[] value);
        TimerFileInfo GetTimerFileInfo();
        bool SetTimerFileInfo(TimerFileInfo value);
        string[] GetUserDataKeys();
        string GetUserData(string key);
        bool SetUserData(string key, string value);
        DateTime GetModifiedTime();

        Gain GetGain();
        bool SetGain(Gain value);
        Alarm GetAlarm();
        bool SetAlarm(Alarm value);
        Palette GetPalette();
        PaletteV2 GetPaletteV2();
        bool SetPalette(Palette value);
        bool SetPaletteV2(PaletteV2 value);
        ViewInfo GetViewInfo();
        bool SetViewInfo(ViewInfo value);
        MappingInfo GetMappingInfo();
        bool SetMappingInfo(MappingInfo value);
        ZoomInfo GetZoomInfo();
        bool SetZoomInfo(ZoomInfo value);
        ColorDistributionType GetColorDistribution();
        bool SetColorDistribution(ColorDistributionType value);
        void Mirror(MirrorType mirror);
        void Rotate(RotateType rotation);
        Image GetCoverImage();
        bool SetCoverImage(Image value);
        Image GetVisualImage();
        bool SetVisualImage(Image value);
        Image GetPaletteImage();
        Image GetThermalImage();

        ThermalParams GetThermalParams();
        bool SetThermalParams(ThermalParams value);
        Reference GetReference();
        float GetReferenceTemperature();
        bool SetReference(Reference value);
        float GetSpotValue(Point point);
        float GetSpotValue(Point point, LocalThermalParams thermalParams);
        TemperaturePoint[] GetRectangleValues(Rectangle rectangle);
        TemperaturePoint[] GetRectangleValues(Rectangle rectangle, LocalThermalParams thermalParams);
        TemperaturePoint[] GetEllipseValues(Rectangle rectangle);
        TemperaturePoint[] GetEllipseValues(Rectangle rectangle, LocalThermalParams thermalParams);
        TemperaturePoint[] GetPolylineValues(Point[] points);
        TemperaturePoint[] GetPolylineValues(Point[] points, LocalThermalParams thermalParams);
        TemperaturePoint[] GetPolygonValues(Point[] points);
        TemperaturePoint[] GetPolygonValues(Point[] points, LocalThermalParams thermalParams);

        int GetFrameCount();
        float GetFrameRate();
        bool SetFrameRate(float value);

        int GetFrameIndex();
        bool SetFrameIndex(int index);
        bool SaveFrameAs(string fileName, Image coverImage);

        Frame GetFrame();
        IThermalImage GetFrame(int index);

        Frame GetRawFrame(int index);
        bool AppendRawFrame(Frame value);
        LUT GetLUT();
        bool SetLUT(LUT value);
        LUT GetCalculatedLUT();
    }
}