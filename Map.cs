using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Faster.Map.Core;

namespace Faster.Map
{
    /// <summary>
    ///   This hashmap is heavily optimized to be used with numerical keys 
    ///   And is alot faster than GneericMap which allows all sorts of keys :)
    /// 
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - Calculates offset from original index
    /// - fibonacci hashing
    /// </summary>
    public class Map<TKey, TValue> where TKey : unmanaged, IComparable, IEquatable<TKey>
    {
        #region properties

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

        #endregion

        #region Fields

        private InfoByte[] _info;
        private Entry<TKey, TValue>[] _entries;
        private uint _maxlookups;
        private readonly double _loadFactor;
        private const uint _multiplier = 0x9E3779B9; // 2654435769; 
        private int _shift = 32;
        private uint _pslLimitor;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        public Map(uint length = 16, double loadFactor = 0.95d)
        {
            //default length is 16
            _maxlookups = length == 0 ? 16 : length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _pslLimitor = PslLimit(size);

            _shift = _shift - (int)Log2(_maxlookups) + 1;
            _entries = new Entry<TKey, TValue>[size + _pslLimitor];
            _info = new InfoByte[size + _pslLimitor];
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

            if (KeyExists(key, out var index))
            {
                return false;
            }

            return EmplaceNew(key, value, ref index);
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
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.NotMapped())
            {
                value = default;
                return false;
            }

            int offset = (int)index + info.Offset;

            for (; offset >= index; offset -= 2)
            {
                //unroll loop twice
                var entry = _entries[offset];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    value = entry.Value;
                    return true;
                }

                entry = _entries[offset - 1]; //1
                if (entry.Key.GetHashCode() == hashcode)
                {
                    value = entry.Value;
                    return true;
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
            uint index = (uint)hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.NotMapped())
            {
                return false;
            }

            int offset = (int)index + info.Offset;

            for (; offset >= index; offset -= 2)
            {
                var entry = _entries[offset];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    _entries[offset].Value = value;
                    return true;
                }

                if (offset == 0)
                {
                    break;
                }

                entry = _entries[offset - 1];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    _entries[offset].Value = value;
                    return true;
                }
            }

            return default;
        }

        ///// <summary>
        ///// Removes the current entry using a backshift removal
        ///// </summary>
        [MethodImpl(256)]
        public bool Remove(TKey key)
        {
            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.NotMapped())
            {
                return false;
            }

            int idx = -1;

            //delete entry
            int offset = (int)index + info.Offset;
            byte psl = 0;
            for (; offset >= index; offset -= 2)
            {
                var entry = _entries[offset];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    //romove entry from list
                    psl = _info[offset].Psl;
                    _entries[offset] = default;
                    idx = offset;

                    //update info
                    --info.Offset;
                    _info[index] = info;
                    --Count;

                    break;
                }

                if (offset == 0)
                {
                    break;
                }

                entry = _entries[offset - 1];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    //romove entry from list
                    psl = _info[offset - 1].Psl;
                    _entries[offset - 1] = default;
                    idx = offset - 1;

                    //update info
                    --info.Offset;
                    _info[index] = info;
                    --Count;

                    break;
                }
            }

            if (idx == -1)
            {
                //abort removal
                return false;
            }

            var bounds = _maxlookups + _pslLimitor;

            for (; idx < bounds; ++idx)
            {
                info = _info[idx + 1];

                if (info.IsEmpty())
                {
                    return true;
                }

                if (info.Psl == 0)
                {
                    return true;
                }

                if (info.Psl >= psl)
                {
                    //decrease psl
                    --info.Psl;

                    swap(ref info, ref _info[idx]);
                    swap(ref _entries[idx + 1], ref _entries[idx]);

                    //clear entry
                    info = default;
                    _info[idx + 1] = info;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(256)]
        public bool ContainsKey(TKey key)
        {
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.NotMapped())
            {
                //slot is  empty
                return false;
            }

            int offset = (int)index + info.Offset;

            for (; offset >= index; offset -= 2)
            {
                var entry = _entries[offset];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    return true;
                }

                if (offset == 0)
                {
                    break;
                }

                entry = _entries[offset - 1];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all values stored in this hashmap
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TValue> Values
        {
            get
            {
                for (var index = 0; index < _info.Length; ++index)
                {
                    var info = _info[index];
                    if (!info.IsEmpty())
                    {
                        yield return _entries[index].Value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all keys stored in this hashmap
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TKey> Keys
        {
            get
            {
                for (var index = 0; index < _info.Length; ++index)
                {
                    var info = _info[index];
                    if (!info.IsEmpty())
                    {
                        yield return _entries[index].Key;
                    }
                }
            }
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

                EmplaceInternal(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="TValue"/> with the specified key.
        /// </summary>
        /// <value>
        /// The <see cref="TValue"/>.
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="key">The value.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private bool EmplaceNew(TKey key, TValue value, ref uint index)
        {
            _info[index].Offset |= 1 << 7; // map index;

            var infoByte = _info[index];
            if (infoByte.IsEmpty())
            {
                infoByte.Psl = 0;
                _info[index] = infoByte;
                _entries[index].Value = value;
                _entries[index].Key = key;
                ++Count;
                return true;
            }

            byte psl = 1;

            if (infoByte.Offset != 0)
            {
                index += (uint)infoByte.Offset + 1;
                psl += infoByte.Offset;
            }
            else
            {
                ++index;
            }

            InfoByte info = default;
            info.Psl = 0; // set not empty

            Entry<TKey, TValue> entry = default;
            entry.Value = value;
            entry.Key = key;

            for (; ; ++psl, ++index)
            {
                infoByte = _info[index];
                if (infoByte.IsEmpty())
                {
                    info.Psl = psl;
                    _info[index - psl].Offset = psl;    //calculate the offset from it's original position
                    _info[index] = info;
                    _entries[index] = entry;
                    ++Count;
                    return true;
                }

                if (psl > infoByte.Psl)
                {
                    info.Psl = psl;
                    swap(ref info, ref _info[index]);
                    swap(ref entry, ref _entries[index]);

                    _info[index - psl].Offset = psl;  //calculate the offset from it's original position
                    psl = info.Psl;
                }

                if (psl == _pslLimitor)
                {
                    Resize();

                    var idx = (uint)entry.Key.GetHashCode() * _multiplier >> _shift;
                    EmplaceNew(entry.Key, entry.Value, ref idx);
                    return true;
                }
            }
        }
        
        [MethodImpl(256)]
        private bool KeyExists(TKey key, out uint idx)
        {
            var hashcode = key.GetHashCode();
            idx = (uint)hashcode * _multiplier >> _shift;

            var info = _info[idx];
            if (info.NotMapped())
            {
                //slot is  empty
                return false;
            }

            int offset = (int)idx + info.Offset;
            for (; offset >= idx; offset -= 2)
            {
                var entry = _entries[offset];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    return true;
                }

                if (offset == 0)
                {
                    break;
                }

                entry = _entries[offset - 1];
                if (entry.Key.GetHashCode() == hashcode)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void swap(ref InfoByte x, ref InfoByte y)
        {
            var tmp = x;

            x.Psl = y.Psl;
            y.Psl = tmp.Psl;
        }

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void swap(ref Entry<TKey, TValue> x, ref Entry<TKey, TValue> y)
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
        private uint PslLimit(uint size)
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

            var oldInfo = new InfoByte[_info.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _pslLimitor = PslLimit(_maxlookups);

            _entries = new Entry<TKey, TValue>[_maxlookups + _pslLimitor];
            _info = new InfoByte[_maxlookups + _pslLimitor];

            Count = 0;

            for (var i = 0; i < oldInfo.Length; i++)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = oldEntries[i];
                uint index = (uint)entry.Key.GetHashCode() * _multiplier >> _shift;
                EmplaceNew(entry.Key, entry.Value, ref index);
            }
        }

        /// <summary>
        /// Inserts the specified value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private void EmplaceInternal(TKey key, TValue value)
        {
            if ((double)Count / _maxlookups > _loadFactor)
            {
                Resize();
            }

            uint index = (uint)key.GetHashCode() * _multiplier >> _shift;
            EmplaceNew(key, value, ref index);
        }

        /// <summary>
        /// calculates next power of 2
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        ///
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        private static uint Log2(uint value)
        {
            uint c = 0;
            while (value > 0)
            {
                c++;
                value >>= 1;
            }

            return c;
        }

        #endregion
    }
}
