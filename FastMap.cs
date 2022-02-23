using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Faster.Map.Core;

namespace Faster.Map
{
    /// <summary>
    /// This hashmap is heavily optimized to be used with numerical keys
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - Calculates offset from original index
    /// - fibonacci hashing
    /// - only usable with valuetype keys
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class FastMap<TKey, TValue> where TKey : unmanaged
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
        /// Gets the stored values. Iterating Values and removing them at the same time requires a .ToArray() Call, this due too the backshift removal of values
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        public IEnumerable<TValue> Values => ValueEnumerator();

        #endregion

        #region Fields

        private InfoByte[] _info;
        private FastEntry<TKey, TValue>[] _entries;
        private uint _maxlookups;
        private readonly double _loadFactor;
        private const uint Multiplier = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private byte _maxProbeSequenceLength;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        public FastMap(uint length = 16, double loadFactor = 0.5d)
        {
            //default length is 16
            _maxlookups = length == 0 ? 16 : length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _maxProbeSequenceLength = loadFactor <= 0.5 ? Log2(size) : PslLimit(size);

            _shift = _shift - Log2(_maxlookups) + 1;
            _entries = new FastEntry<TKey, TValue>[size + _maxProbeSequenceLength + 1];
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

            var initial = index;

            var maxDistance = index + _maxProbeSequenceLength;
            for (; index <= maxDistance; index += 2)
            {
                var info = _info[index];
                if (info.IsEmpty())
                {
                    break;
                }

                var entry = _entries[index];
                if (hashcode == entry.Key.GetHashCode())
                {
                    return false;
                }

                entry = _entries[index + 1];
                if (hashcode == entry.Key.GetHashCode())
                {
                    return false;
                }
            }

            return EmplaceNew(key, value, ref initial);
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
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                value = default;
                return false;
            }

            var maxDistance = index + _maxProbeSequenceLength;
            do
            {
                var entry = _entries[index];
                if (hashcode == entry.Key.GetHashCode())
                {
                    value = entry.Value;
                    return true;
                }

                ++index;

            } while (index <= maxDistance);

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
            do
            {
                var entry = _entries[index];
                if (hashcode == entry.Key.GetHashCode())
                {
                    _entries[index].Value = value;
                    return true;
                }
            } while (++index <= maxDistance);


            return false;
        }

        // <summary>
        // Removes the current entry using a backshift removal
        // </summary>
        [MethodImpl(256)]
        public bool Remove(TKey key)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;
            int foundAtIndex = -1;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                return false;
            }

            //delete entry
            var maxDistance = index + _maxProbeSequenceLength;
            
            for (; index <= maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (hashcode == entry.Key.GetHashCode())
                {
                    //remove entry from list
                    _entries[index] = default;
                    _info[index] = default;
                    --Count;
                    foundAtIndex = (int)index;
                    break;
                }

                entry = _entries[index + 1];
                if (hashcode == entry.Key.GetHashCode())
                {
                    _entries[index + 1] = default;
                    _info[index + 1] = default;
                    --Count;
                    foundAtIndex = (int)index + 1;
                    break;
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

                if (next.Psl == 0)
                {
                    break;
                }

                if (next.Psl > 0)
                {
                    //decrease psl
                    --next.Psl;
                    //swap upper with lower
                    Swap(ref _entries[foundAtIndex + 1], ref _entries[foundAtIndex]);
                    swap(ref next, ref _info[foundAtIndex]);

                    _entries[foundAtIndex + 1] = default;
                    _info[foundAtIndex + 1] = default;
                }
            }

            return true;
        }

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
            do
            {
                var entry = _entries[index];
                if (hashcode == entry.Key.GetHashCode())
                {
                    return true;
                }

                ++index;

            } while (index <= maxDistance);


            return false;
        }

        ///// <summary>
        ///// Copies entries from one map to another
        ///// </summary>
        ///// <param name="valueMap">The map.</param>
        public void Copy(FastMap<TKey, TValue> fastMap)
        {
            for (var i = 0; i < fastMap._entries.Length; i++)
            {
                var info = fastMap._info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                if ((double)Count / _maxlookups > _loadFactor)
                {
                    Resize();
                }

                var entry = _entries[i];
                var hashcode = entry.Key.GetHashCode();
                uint index = (uint)hashcode * Multiplier >> _shift;
                EmplaceNew(entry.Key, entry.Value, ref index);
            }
        }

#if DEBUG

        public int Locate(TKey key)
        {
            var hashcode = key.GetHashCode();
            for (int i = 0; i < _entries.Length; i++)
            {
                var info = _info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                if (_entries[i].Key.GetHashCode() == hashcode)
                {
                    return i;
                }
            }

            return -1;
        }

#endif

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

                _info[i] = default;
                _entries[i] = default;
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
        /// <param name="key">The value.</param>
        /// <param name="value">The value.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private bool EmplaceNew(TKey key, TValue value, ref uint index)
        {
            FastEntry<TKey, TValue> insertableFastEntry = default;
            insertableFastEntry.Value = value;
            insertableFastEntry.Key = key;
            
            InfoByte insertableInfo = default;
            insertableInfo.Psl = 0;

            for (; ; ++insertableInfo.Psl, ++index)
            {
                var info = _info[index];
                if (info.IsEmpty())
                {
                    _entries[index] = insertableFastEntry;
                    _info[index] = insertableInfo;
                    ++Count;
                    return true;
                }

                if (insertableInfo.Psl > info.Psl)
                {
                    Swap(ref insertableFastEntry, ref _entries[index]);
                    swap(ref insertableInfo, ref _info[index]);
                    continue;
                }

                if (insertableInfo.Psl == _maxProbeSequenceLength)
                {
                    Resize();
                    var hc = insertableFastEntry.Key.GetHashCode();
                    uint idx = (uint)hc * Multiplier >> _shift;
                    EmplaceNew(insertableFastEntry.Key, insertableFastEntry.Value, ref idx);
                    return true;
                }
            }
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

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref FastEntry<TKey, TValue> x, ref FastEntry<TKey, TValue> y)
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
        private void swap(ref InfoByte x, ref InfoByte y)
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

            var oldEntries = new FastEntry<TKey, TValue>[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldInfo = new InfoByte[_info.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _maxProbeSequenceLength = _loadFactor <= 0.5 ? Log2(_maxlookups) : PslLimit(_maxlookups);
            _entries = new FastEntry<TKey, TValue>[_maxlookups + _maxProbeSequenceLength + 1];
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
                var hashcode = entry.Key.GetHashCode();
                uint idx = (uint)hashcode * Multiplier >> _shift;
                EmplaceNew(entry.Key, entry.Value, ref idx);
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

        #endregion
    }
}