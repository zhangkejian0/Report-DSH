using System;
using System.Collections.Generic;
using Amur.ThermalImageFile;

namespace Amur.IRtekFileSDK
{
    internal partial class ThermalImage
    {
        #region InnerClasses
        internal class Delta : IDelta
        {
            #region Fields
            private DeltaParams _deltaParams;
            #endregion

            #region Constructors
            public Delta(string name, DeltaParams deltaParams)
            {
                Name = name;
                _deltaParams = deltaParams;
            }
            #endregion

            #region Properties
            public IThermalImage Owner { get; internal set; }
            public string Name { get; private set; }
            public float Value { get; internal set; }

            public DeltaParams DeltaParams
            {
                get { return _deltaParams; }
                set { throw new InvalidProgramException(); }
            }
            #endregion
        }

        internal class DeltaCollection : IDeltaCollection
        {
            #region Fields
            private readonly List<IDelta> _entries = new List<IDelta>();
            #endregion

            #region Constructors
            public DeltaCollection(ThermalImage owner)
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

            public IDelta this[int index]
            {
                get { return _entries[index]; }
            }

            public IDelta this[string name]
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

            public string NextDeltaName
            {
                get { throw new InvalidProgramException(); }
            }
            #endregion

            #region Methods
            internal void AddInternal(IDelta delta)
            {
                _entries.Add(delta);
            }

            public void Add(IDelta delta)
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
