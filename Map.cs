using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Faster.Map.Core;

namespace Faster.Map
{
    /// <summary>
    ///
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - calculates offset from original index
    /// - fibonacci hashing
    /// </summary>
    public class Map<TKey, TValue>
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public uint Count { get; private set; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public uint Size => (uint)_entries.Length;

        /// <summary>
        /// Gets all keys stored in this hashmap
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TKey> Keys => KeyEnumerator();

        /// <summary>
        /// Gets the stored values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        public IEnumerable<TValue> Values => ValueEnumerator();


        #endregion

        #region Fields

        private InfoByte[] _info;
        private Entry<TKey, TValue>[] _entries;
        private uint _maxlookups;
        private readonly double _loadFactor;
        private const uint Multiplier = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private byte _maxProbeSequenceLength;
        private readonly IEqualityComparer<TKey> _cmp;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        /// <param name="cmp">The CMP.</param>
        public Map(uint length = 16, double loadFactor = 0.5d, IEqualityComparer<TKey> cmp = null)
        {
            //default length is 16
            _maxlookups = length == 0 ? 16 : length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _maxProbeSequenceLength = loadFactor <= 0.5 ? Log2(size) : PslLimit(size);

            _cmp = cmp ?? EqualityComparer<TKey>.Default;

            _shift = _shift - Log2(_maxlookups) + 1;

            _entries = new Entry<TKey, TValue>[size + _maxProbeSequenceLength + 1];
            _info = new InfoByte[size + _maxProbeSequenceLength + 1];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts the specified value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Emplace(TKey key, TValue value)
        {
            if ((double)Count / _maxlookups > _loadFactor)
            {
                Resize();
            }

            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var initialIndex = index;

            //Check if key exists
            var maxDistance = index + _maxProbeSequenceLength;
            for (; index < maxDistance; index += 2)
            {
                var info = _info[index];
                if (info.IsEmpty())
                {
                    //slot is empty - key noy found
                    break;
                }

                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _cmp.Equals(key, entry.Key))
                {
                    return false;
                }

                if (entry.Next == hashcode)
                {
                    entry = _entries[index + 1];
                    if (_cmp.Equals(key, entry.Key))
                    {
                        return false;
                    }
                }
            }

            return EmplaceNew(key, value, ref hashcode, ref initialIndex);
        }

        /// <summary>
        /// Gets the value with Lthe corresponding key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Get(TKey key, out TValue value)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                value = default;
                return false;
            }

            uint maxDistance = index + _maxProbeSequenceLength;
            for (; index < maxDistance; index +=2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _cmp.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    var next = _entries[index + 1];
                    if (_cmp.Equals(key, next.Key))
                    {
                        value = next.Value;
                        return true;
                    }
                }
            }

            value = default;
            return default;
        }

        /// <summary>
        /// update the entry
        /// </summary>
        [MethodImpl(256)]
        public bool Update(TKey key, TValue value)
        {
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;
            
            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                return false;
            }

            var maxDistance = index + _maxProbeSequenceLength;

            for (; index < maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _cmp.Equals(key, entry.Key))
                {
                    _entries[index].Value = value;
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    var next = _entries[index + 1];
                    if (_cmp.Equals(key, next.Key))
                    {
                        _entries[index + 1].Value = value;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Remove(TKey key)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                return false;
            }


            int foundAtIndex = -1;

            //delete entry
            var maxDistance = index + _maxProbeSequenceLength;

            for (; index < maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _cmp.Equals(key, entry.Key))
                {
                    foundAtIndex = (int)index;
                    //remove entry from list
                    _entries[index] = default;

                    //remove next from previous entry
                    if (index > 0)
                    {
                        _entries[index - 1].Next = default;
                    }

                    _info[index] = default;
                    --Count;
                    break;
                }

                if (entry.Next == hashcode)
                {
                    foundAtIndex = (int)index + 1;
                    var next = _entries[foundAtIndex];
                    if (_cmp.Equals(key, next.Key))
                    {
                        _entries[foundAtIndex] = default;
                       
                        if (index > 0)
                        {
                            _entries[index].Next = default;
                        }

                        _info[foundAtIndex] = default;
                        --Count;
                        break;
                    }
                }
            }

            if (foundAtIndex == -1)
            {
                return false;
            }

            for (; ; ++foundAtIndex)
            {
                var next = _info[foundAtIndex + 1];
                if (next.IsEmpty())
                {
                    break;
                }

                if (next.Psl <= 0)
                {
                    break;
                }

                if (next.Psl > 0)
                {
                    //decrease psl
                    --next.Psl;
                    Swap(ref _entries[foundAtIndex + 1], ref _entries[foundAtIndex]);
                    Swap(ref next, ref _info[foundAtIndex]);

                    if (foundAtIndex > 0)
                    {
                        _entries[foundAtIndex - 1].Next = _entries[foundAtIndex].Hashcode;
                    }

                    _entries[foundAtIndex + 1] = default;
                    _info[foundAtIndex + 1] = default;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(256)]
        public bool ContainsKey(TKey key)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                return false;
            }

            var maxDistance = index + _maxProbeSequenceLength;
            for (; index < maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _cmp.Equals(key, entry.Key))
                {
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    entry = _entries[index + 1];
                    if (_cmp.Equals(entry.Key, key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="map">The map.</param>
        public void Copy(Map<TKey, TValue> map)
        {
            for (var i = 0; i < map._entries.Length; i++)
            {
                var info = map._info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = map._entries[i];
                Emplace(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                var info = _info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                _entries[i] = default;
                _info[i] = default;
            }
        }

        /// <summary>
        /// Gets or sets the  with the specified key.
        /// </summary>
        /// <value>
        /// The 
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">
        /// Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}
        /// or
        /// Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}
        /// </exception>
        public TValue this[TKey key]
        {
            get
            {
                if (Get(key, out var result))
                {
                    return result;
                }

                throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
            }
            set
            {
                if (!Update(key, value))
                {
                    throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
                }
            }
        }


#if DEBUG

        /// <summary>
        /// Locates the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public int Locate(TKey key)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                var info = _info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = _entries[i];
                if (entry.Hashcode == key.GetHashCode() && _cmp.Equals(key, entry.Key))
                {
                    return i;
                }
            }
            return -1;
        }
#endif

        #endregion

        #region Private Methods

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="key">The value.</param>
        /// <param name="value">The value.</param>
        /// <param name="hashcode">The hashcode.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private bool EmplaceNew(TKey key, TValue value, ref int hashcode, ref uint index)
        {
            Entry<TKey, TValue> insertableEntry = default;

            insertableEntry.Key = key;
            insertableEntry.Value = value;
            insertableEntry.Hashcode = hashcode;

            InfoByte insertInfoByte = default;
            insertInfoByte.Psl = 0;

            for (; ; ++insertInfoByte.Psl, ++index)
            {
                var info = _info[index];
                if (info.IsEmpty())
                {
                    _info[index] = insertInfoByte;
                    if (index > 0)
                    {
                        _entries[index - 1].Next = insertableEntry.Hashcode;
                    }

                    Merge(insertableEntry, ref _entries[index]);

                    ++Count;
                    return true;
                }

                if (insertInfoByte.Psl > info.Psl)
                {
                    insertableEntry.Next = _entries[index + 1].Hashcode;

                    Swap(ref insertableEntry, ref _entries[index]);
                    Swap(ref insertInfoByte, ref _info[index]);

                    _entries[index - 1].Next = _entries[index].Hashcode;

                    continue;
                }

                if (insertInfoByte.Psl == _maxProbeSequenceLength)
                {
                    Resize();

                    var hc = insertableEntry.Hashcode;
                    uint idx = (uint)hc * Multiplier >> _shift;
                    EmplaceNew(insertableEntry.Key, insertableEntry.Value, ref hc, ref idx);
                    return true;
                }
            }
        }

        private void Merge(Entry<TKey, TValue> x, ref Entry<TKey, TValue> y)
        {
            var tmp = x;
            tmp.Next = y.Next;
            y = tmp;
        }

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref Entry<TKey, TValue> x, ref Entry<TKey, TValue> y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref InfoByte x, ref InfoByte y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }


        /// <summary>
        /// PSLs the limit.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private byte PslLimit(uint size)
        {
            switch (size)
            {
                case 16: return 6;
                case 32: return 8;
                case 64: return 12;
                case 128: return 16;
                case 256: return 20;
                case 512: return 24;
                case 1024: return 32;
                case 2048: return 36;
                case 4096: return 40;
                case 8192: return 50;
                case 16384: return 60;
                case 32768: return 65;
                case 65536: return 70;
                case 131072: return 75;
                case 262144: return 80;
                case 524288: return 85;
                case 1048576: return 90;
                case 2097152: return 94;
                case 4194304: return 98;
                case 8388608: return 102;
                case 16777216: return 104;
                case 33554432: return 108;
                case 67108864: return 112;
                case 134217728: return 116;
                case 268435456: return 120;
                case 536870912: return 124;
                default: return 10;
            }
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _maxlookups = NextPow2(_maxlookups + 1);

            var oldEntries = new Entry<TKey, TValue>[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldInfo = new InfoByte[_entries.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _maxProbeSequenceLength = _loadFactor <= 0.5 ? Log2(_maxlookups) : PslLimit(_maxlookups);
            _entries = new Entry<TKey, TValue>[_maxlookups + _maxProbeSequenceLength + 1];
            _info = new InfoByte[_maxlookups + _maxProbeSequenceLength + 1];

            Count = 0;

            for (var i = 0; i < oldEntries.Length; i++)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = oldEntries[i];
                entry.Next = 0;

                var hashcode = entry.Hashcode;
                uint idx = (uint)hashcode * Multiplier >> _shift;
                EmplaceNew(entry.Key, entry.Value, ref hashcode, ref idx);
            }
        }

        /// <summary>
        /// calculates next power of 2
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        ///
        [MethodImpl(256)]
        private static uint NextPow2(uint c)
        {
            c--;
            c |= c >> 1;
            c |= c >> 2;
            c |= c >> 4;
            c |= c >> 8;
            c |= c >> 16;
            return ++c;
        }

        // used for set checking operations (using enumerables) that rely on counting
        private static byte Log2(uint value)
        {
            byte c = 0;
            while (value > 0)
            {
                c++;
                value >>= 1;
            }

            return c;
        }

        private IEnumerable<TKey> KeyEnumerator()
        {
            for (var index = 0; index < _info.Length; ++index)
            {
                if (!_info[index].IsEmpty())
                {
                    yield return _entries[index].Key;
                }
            }
        }

        private IEnumerable<TValue> ValueEnumerator()
        {
            for (var index = 0; index < _info.Length; ++index)
            {
                if (!_info[index].IsEmpty())
                {
                    yield return _entries[index].Value;
                }
            }
        }

        #endregion
    }
}
