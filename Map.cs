using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Faster.Map.Core;
namespace Faster.Map
{
    /// <summary>
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robinghood hashing
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
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
        /// Returns all the entries as KeyValuePair objects
        /// </summary>
        /// <value>
        /// The entries.
        /// </value>
        public IEnumerable<KeyValuePair<TKey, TValue>> Entries
        {
            get
            {
                //iterate backwards so we can remove the current item
                for (int i = _info.Length - 1; i >= 0; --i)
                {
                    if (!_info[i].IsEmpty())
                    {
                        var entry = _entries[i];
                        yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Returns all keys
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public IEnumerable<TKey> Keys
        {
            get
            {
                //iterate backwards so we can remove the current item
                for (int i = _info.Length - 1; i >= 0; --i)
                {
                    if (!_info[i].IsEmpty())
                    {
                        yield return _entries[i].Key;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all Values
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public IEnumerable<TValue> Values
        {
            get
            {
                for (int i = _info.Length - 1; i >= 0; --i)
                {
                    if (!_info[i].IsEmpty())
                    {
                        yield return _entries[i].Value;
                    }
                }
            }
        }

        #endregion

        #region Fields

        private InfoByte[] _info;
        private Entry<TKey, TValue>[] _entries;
        private uint _maxlookups;
        private readonly double _loadFactor;
        private const uint Multiplier = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private byte _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private readonly IEqualityComparer<TKey> _keyComparer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}"/> class.
        /// </summary>
        public Map() : this(16, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public Map(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public Map(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public Map(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            //default length is 16
            _maxlookups = length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _maxProbeSequenceLength = loadFactor <= 0.5 ? Log2(size) : PslLimit(size);

            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

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

            if (KeyValueExists(key, hashcode, index))
            {
                return false;
            }

            Entry<TKey, TValue> entry = default;
            entry.Value = value;
            entry.Key = key;
            entry.Hashcode = hashcode;

            InfoByte metadata = default;
            metadata.Psl = 0;

            for (; ; ++metadata.Psl, ++index)
            {
                if (_currentProbeSequenceLength < metadata.Psl)
                {
                    _currentProbeSequenceLength = metadata.Psl;
                }

                var info = _info[index];
                if (info.IsEmpty())
                {
                    _info[index] = metadata;
                    if (index > 0)
                    {
                        _entries[index - 1].Next = entry.Hashcode;
                    }

                    Merge(entry, ref _entries[index]);

                    ++Count;
                    return true;
                }

                if (metadata.Psl > info.Psl)
                {
                    entry.Next = _entries[index + 1].Hashcode;

                    Swap(ref entry, ref _entries[index]);
                    Swap(ref metadata, ref _info[index]);

                    _entries[index - 1].Next = _entries[index].Hashcode;

                    continue;
                }

                if (metadata.Psl == _maxProbeSequenceLength)
                {
                    IncreaseMaxProbeSequence();
                    Resize();
                    EmplaceInternal(ref entry, ref metadata);
                    return true;
                }
            }
        }

        /// <summary>
        /// Gets the value with the corresponding key
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
                //Don't iterate over the entries if unnecessary
                value = default;
                return false;
            }

            uint maxDistance = index + _maxProbeSequenceLength;
            for (; index < maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _keyComparer.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    var next = _entries[index + 1];
                    if (_keyComparer.Equals(key, next.Key))
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
        ///Updates the value of a specific key
        /// </summary>
        [MethodImpl(256)]
        public bool Update(TKey key, TValue value)
        {
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Don't iterate over the entries if unnecessary
                return false;
            }

            var maxDistance = index + _maxProbeSequenceLength;

            for (; index < maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _keyComparer.Equals(key, entry.Key))
                {
                    _entries[index].Value = value;
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    var next = _entries[index + 1];
                    if (_keyComparer.Equals(key, next.Key))
                    {
                        _entries[index + 1].Value = value;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///  Remove entry with a backshift removal
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(256)]
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
        public bool Remove(TKey key)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                //Don't iterate over the entries if unnecessary
                return false;
            }

            var maxDistance = index + _maxProbeSequenceLength;
            for (; index < maxDistance; index += 2)
            {
                var entry = _entries[index];
                if (entry.Hashcode == hashcode && _keyComparer.Equals(key, entry.Key))
                {
                    //Remove entry from list
                    _entries[index] = default;

                    //Remove next from previous entry
                    if (index > 0)
                    {
                        _entries[index - 1].Next = default;
                    }

                    _info[index] = default;

                    ShiftRemove(index);
                    --Count;
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    var next = _entries[index + 1];
                    if (_keyComparer.Equals(key, next.Key))
                    {
                        _entries[index + 1] = default;

                        if (index > 0)
                        {
                            _entries[index].Next = default;
                        }

                        _info[index + 1] = default;
                        ShiftRemove(index + 1);
                        --Count;
                        return true;
                    }
                }
            }

            return false;
        }
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high

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
                if (entry.Hashcode == hashcode && _keyComparer.Equals(key, entry.Key))
                {
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    entry = _entries[index + 1];
                    if (_keyComparer.Equals(entry.Key, key))
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
                _entries[i] = default;
                _info[i] = default;
            }

            Count = 0;
        }

        /// <summary>
        /// Gets or sets the value by using a Tkey
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

        /// <summary>
        /// Returns an index of the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public int IndexOf(TKey key)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                var info = _info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = _entries[i];
                if (entry.Hashcode == key.GetHashCode() && _keyComparer.Equals(key, entry.Key))
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Emplaces a new entry without checking for key existence. Keys have already been checked and are unique
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="meta">The meta.</param>
        [MethodImpl(256)]
        private void EmplaceInternal(ref Entry<TKey, TValue> entry, ref InfoByte meta)
        {
            uint index = (uint)entry.Hashcode * Multiplier >> _shift;

            meta.Psl = 0;

            for (; ; ++meta.Psl, ++index)
            {
                if (_currentProbeSequenceLength < meta.Psl)
                {
                    _currentProbeSequenceLength = meta.Psl;
                }

                var info = _info[index];
                if (info.IsEmpty())
                {
                    _info[index] = meta;
                    if (index > 0)
                    {
                        _entries[index - 1].Next = entry.Hashcode;
                    }

                    Merge(entry, ref _entries[index]);

                    ++Count;
                    return;
                }

                if (meta.Psl > info.Psl)
                {
                    entry.Next = _entries[index + 1].Hashcode;

                    Swap(ref entry, ref _entries[index]);
                    Swap(ref meta, ref _info[index]);

                    _entries[index - 1].Next = _entries[index].Hashcode;

                    continue;
                }

                if (meta.Psl == _maxProbeSequenceLength)
                {
                    IncreaseMaxProbeSequence();
                    Resize();
                    EmplaceInternal(ref entry, ref meta);
                    return;
                }
            }
        }

        private void ShiftRemove(uint index)
        {

        Start:

            var next = _info[index + 1];
            if (next.IsEmpty())
            {
                return;
            }

            if (next.Psl == 0)
            {
                return;
            }

            //decrease psl

            Swap(ref _entries[index + 1], ref _entries[index]);

            _info[index + 1].Psl--;

            Swap(ref _info[index + 1], ref _info[index]);

            if (index > 0)
            {
                _entries[index].Next = _entries[index + 1].Hashcode;
            }

            ++index;
#pragma warning disable S907 // "goto" statement should not be used
            goto Start;
#pragma warning restore S907 // "goto" statement should not be used
        }

        [MethodImpl(256)]
        private bool KeyValueExists(TKey key, int hashcode, uint index)
        {
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
                if (entry.Hashcode == hashcode && _keyComparer.Equals(key, entry.Key))
                {
                    return true;
                }

                if (entry.Next == hashcode)
                {
                    entry = _entries[index + 1];
                    if (_keyComparer.Equals(key, entry.Key))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private void IncreaseMaxProbeSequence()
        {
            if (_loadFactor <= 0.5d)
            {
                ++_maxProbeSequenceLength;
            }
            else
            {
                _maxProbeSequenceLength = _loadFactor <= 0.5
                    ? Log2(_maxlookups)
                    : PslLimit(_maxlookups);
            }
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

                EmplaceInternal(ref entry, ref info);
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
