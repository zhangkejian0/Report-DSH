using System.Collections;

namespace Amur.ThermalImageFile
{
    public interface IDelta
    {
        IThermalImage Owner { get; }

        string Name { get; }
        DeltaParams DeltaParams { get; set; }
        float Value { get; }
    }

    public interface IDeltaCollection : IEnumerable
    {
        IThermalImage Owner { get; }
        
        IDelta this[int index] { get; }
        IDelta this[string name] { get; }

        int Count { get; }
        string NextDeltaName { get; }
        
        void Add(IDelta delta);
        bool Remove(string name);
        void RemoveAt(int index);
    }
}
