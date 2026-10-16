
namespace Amur.ThermalImageFile
{
    public interface IImageFile
    {
        FileType FileType { get; }
        ushort FileVersion { get; }
        string FileName { get; }
        bool IsValid { get; }
        bool IsModified { get; }

        bool Open(string fileName);
        bool Create(string fileName, FileType fileType);
        bool Save();
        bool SaveAsFJPG(string fileName);
        bool SaveAs(string fileName);
        void Close();
    }
}
