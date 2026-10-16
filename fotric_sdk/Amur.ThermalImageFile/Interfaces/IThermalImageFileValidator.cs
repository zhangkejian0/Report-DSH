namespace Amur.ThermalImageFile
{
    public interface IThermalImageFileValidator
    {
        string Name { get; }

        FileType IsValidFile(string fileName);

        IThermalImageFile Create();
    }
}
