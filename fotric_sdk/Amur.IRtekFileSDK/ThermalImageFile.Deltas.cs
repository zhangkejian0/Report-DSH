// Copyright(c) IRtek.Co.Ltd. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Amur.ThermalImageFile;

namespace Amur.IRtekFileSDK
{
    internal partial class ThermalImageFile
    {
        #region InnerClasses
        internal class Delta : IDelta
        {
            private DeltaParams _deltaParams;

            public Delta(IThermalImageFile owner, string name, DeltaParams deltaParams)
            {
                Owner = owner;
                Name = name;
                _deltaParams = deltaParams;
            }

            public IThermalImage Owner { get; private set; }
            public string Name { get; private set; }
            public float Value { get; internal set; }

            public DeltaParams DeltaParams
            {
                get
                {
                    return _deltaParams;
                }
                set
                {
                    DeltaParams oldValue = _deltaParams;
                    _deltaParams = value;
                    ThermalImageFile ownerImp = Owner as ThermalImageFile;
                    if (ownerImp.SetDelta(this))
                    {
                        ownerImp.HandleDeltaChangedEvent(this);
                    }
                    else
                    {
                        _deltaParams = oldValue;
                    }
                }
            }

            public void Sync()
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                IntPtr hImage = ownerImp.HImage;
                if (hImage == IntPtr.Zero)
                {
                    return;
                }

                IRtekFileSDK.ir_file_delta irDelta = new IRtekFileSDK.ir_file_delta();
                irDelta.name = StringConvertHelper.StringToBytes(Name, IRtekFileSDK.NAME_LEN, Encoding.UTF8);
                int result = IRtekFileSDK.ir_file_get_delta(hImage, ref irDelta);
                if (result == IRtekFileSDK.IR_FILE_EC_OK)
                {
                    DeltaParams deltaParams = new DeltaParams();
                    deltaParams.SrcName = StringConvertHelper.BytesToString(irDelta.srcName, Encoding.UTF8);
                    switch (irDelta.srcValueType)
                    {
                        case IRtekFileSDK.IR_FILE_MAX:
                        default:
                            deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Max;
                            break;
                        case IRtekFileSDK.IR_FILE_MIN:
                            deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Min;
                            break;
                        case IRtekFileSDK.IR_FILE_AVG:
                            deltaParams.SrcValueType = Amur.ThermalImageFile.ValueType.Avg;
                            break;
                    }
                    deltaParams.SrcTemp = irDelta.srcTemp;
                    deltaParams.DestName = StringConvertHelper.BytesToString(irDelta.destName, Encoding.UTF8);
                    switch (irDelta.destValueType)
                    {
                        case IRtekFileSDK.IR_FILE_MAX:
                        default:
                            deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Max;
                            break;
                        case IRtekFileSDK.IR_FILE_MIN:
                            deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Min;
                            break;
                        case IRtekFileSDK.IR_FILE_AVG:
                            deltaParams.DestValueType = Amur.ThermalImageFile.ValueType.Avg;
                            break;
                    }
                    deltaParams.DestTemp = irDelta.destTemp;
                    _deltaParams = deltaParams;
                    Value = irDelta.value;
                }
                else
                {
                    throw new Exception(string.Format("invoke ir_file_get_delta error code {0}.", result));
                }
            }
        }

        internal class DeltaCollection : IDeltaCollection
        {
            private readonly List<IDelta> _entries = new List<IDelta>();

            public DeltaCollection(IThermalImage owner)
            {
                Owner = owner;
            }

            public IThermalImage Owner { get; private set; }

            public int Count
            {
                get { return _entries.Count; }
            }

            public IDelta this[int index]
            {
                get
                {
                    return _entries[index];
                }
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
                get
                {
                    return string.Format("{0}{1}", DELTA_PREFIX, GetNextIndex(DELTA_PREFIX));
                }
            }

            public void Add(IDelta delta)
            {
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                if (!ownerImp.IsDeltasSynced)
                {
                    _entries.Add(delta);
                }
                else if (ownerImp.SetDelta(delta))
                {
                    _entries.Add(delta);
                    ownerImp.HandleDeltaAddedEvent(delta);
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
                IDelta delta = _entries[index];
                ThermalImageFile ownerImp = Owner as ThermalImageFile;
                if (!ownerImp.IsDeltasSynced)
                {
                    _entries.RemoveAt(index);
                }
                else if (ownerImp.RemoveDelta(delta.Name))
                {
                    _entries.RemoveAt(index);
                    ownerImp.HandleDeltaRemovedEvent(delta);
                }
                else
                {
                    throw new Exception("remove failed");
                }
            }

            public void Clear()
            {
                for (int i = _entries.Count - 1; i >= 0; i--)
                {
                    IDelta delta = _entries[i];
                    Remove(delta.Name);
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
