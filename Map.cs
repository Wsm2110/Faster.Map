using System;
using System.Runtime.CompilerServices;

namespace Faster
{
    /// <summary>
    /// The default dictionary is no match for this implementation of a hashmap...
    /// 
    /// it`s speed and memory footprint is thorougly tested in Servicelocator.Tests.Benchmark.
    ///
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - fixed uint key in order not having to call GetHashCode() which is an override... and overrides are not ideal in terms of performance
    /// - fibonacci hashing
    /// </summary>
    public class Map<TKey, TValue>
    {
        #region properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public uint EntryCount { get; private set; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public uint Size => (uint)_entries.Length;

        #endregion

        #region Fields

        private uint _maxLoopUps;
        private readonly double _loadFactor;
        private Entry<TKey, TValue>[] _entries;
        private const uint GoldenRatio = 0x9E3779B9; // 2654435769; 
        private int _shift = 32;
        private uint _probeSequenceLength;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Map" /> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        public Map(uint length = 16, double loadFactor = 0.88d)
        {
            //default length is 16
            _maxLoopUps = length == 0 ? 16 : length;

            _probeSequenceLength = Log2(_maxLoopUps);
            _loadFactor = loadFactor;

            var powerOfTwo = NextPow2(_maxLoopUps);
            _shift = _shift - (int)_probeSequenceLength + 1;
            _entries = new Entry<TKey, TValue>[powerOfTwo + _probeSequenceLength];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts the specified value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Emplace(TKey key, TValue value)
        {
            if ((double)EntryCount / _maxLoopUps > _loadFactor || EntryCount >= _maxLoopUps)
            {
                Resize();
            }

            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * GoldenRatio >> _shift;

            //validate if the key is unique
            if (KeyExists(key, index, hashcode))
            {
                return false;
            }

            return EmplaceNew(key, value, index);
        }

        /// <summary>
        /// Emplaces a new entry without checking for keyu existence
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EmplaceNew(TKey key, TValue value, uint index)
        {
            var entry = _entries[index];
            if (entry.IsEmpty())
            {
                entry.Psl = 0;
                entry.Key = key;
                entry.Value = value;
                _entries[index] = entry;
                ++EntryCount;
                return true;
            }

            Entry<TKey, TValue> insertableEntry = default;

            byte psl = 1;
            ++index;

            insertableEntry.Key = key;
            insertableEntry.Value = value;
            insertableEntry.Psl = 0; // set not empty

            for (; ; ++psl, ++index)
            {
                entry = _entries[index];
                if (entry.IsEmpty())
                {
                    insertableEntry.Psl = psl;
                    _entries[index] = insertableEntry;
                    ++EntryCount;
                    return true;
                }

                if (psl > entry.Psl)
                {
                    insertableEntry.Psl = psl;
                    swap(ref insertableEntry, ref _entries[index]);
                    psl = insertableEntry.Psl;
                }
                else
                {
                    if (psl == _probeSequenceLength - 1)
                    {
                        Resize();
                        Emplace(insertableEntry.Key, insertableEntry.Value);
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Find if key exists in the map
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="hashcode">The hashcode.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool KeyExists(TKey key, uint index, uint hashcode)
        {
            var currentEntry = _entries[index];
            if (currentEntry.IsEmpty())
            {
                return false;
            }

            if (hashcode == (uint)currentEntry.Key.GetHashCode()
                && currentEntry.Key.Equals(key))
            {
                return true;
            }

            var maxDistance = index + _probeSequenceLength - 1;
            ++index;

            for (; index < maxDistance; ++index)
            {
                currentEntry = _entries[index];
                if (currentEntry.IsEmpty())
                {
                    return false;
                }

                if (hashcode == (uint)currentEntry.Key.GetHashCode()
                    && currentEntry.Key.Equals(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// update the entry
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(TKey key, TValue value)
        {
            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * GoldenRatio >> _shift;

            var currentEntry = _entries[index];

            if (hashcode == (uint)currentEntry.Key.GetHashCode()
                && currentEntry.Key.Equals(key))
            {
                currentEntry.Value = value;
                _entries[index] = currentEntry;
                return;
            }

            var maxDistance = index + _probeSequenceLength - 1;
            ++index;

            for (; index < maxDistance; ++index)
            {
                currentEntry = _entries[index];
                if (hashcode == (uint)currentEntry.Key.GetHashCode()
                    && currentEntry.Key.Equals(key))
                {
                    currentEntry.Value = value;
                    _entries[index] = currentEntry;
                    return;
                }
            }
        }

        /// <summary>
        /// Removes tehe current entry using a backshift removal
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(TKey key)
        {
            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * GoldenRatio >> _shift;
            byte psl = 0;
            bool found = false;
            var bounds = index + _probeSequenceLength;

            for (; index < bounds; ++index)
            {
                var currentEntry = _entries[index];

                if (hashcode == (uint)currentEntry.Key.GetHashCode()
                    && currentEntry.Key.Equals(key))
                {
                    _entries[index] = default; //reset current entry
                    EntryCount--;
                    psl = currentEntry.Psl;
                    found = true;
                    continue;
                }

                if (found && currentEntry.Psl >= psl)
                {
                    --currentEntry.Psl;
                    _entries[index - 1] = currentEntry;
                    _entries[index] = default;
                }
                else if (found)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Gets the value with the corresponding key, will returns true or false if the key is found or not
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            uint index = hashcode * GoldenRatio >> _shift;

            var currentEntry = _entries[index];
            if (hashcode == (uint)currentEntry.Key.GetHashCode() && currentEntry.Key.Equals(key))
            {
                value = currentEntry.Value;
                return true;
            }

            var maxDistance = index + _probeSequenceLength - 1;
            ++index;

            for (; index < maxDistance; ++index)
            {
                currentEntry = _entries[index];
                var currentKey = currentEntry.Key;
                if (currentKey != null &&
                    (uint)currentKey.GetHashCode() == hashcode &&
                    currentKey.Equals(key))
                {
                    value = currentEntry.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value with the corresponding key, will actually throw if the key isnt found, if this is an issue you should just use Get()
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public TValue this[TKey key]
        {
            get
            {
                if (Get(key, out var result)) 
                {
                    return result;
                }

                throw new InvalidOperationException($"Unable to find {key} in hashmap");
            }
        } 

        #endregion

        #region Private Methods

        private void swap(ref Entry<TKey, TValue> x, ref Entry<TKey, TValue> y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }
        private void Resize()
        {
            _shift--;
            _maxLoopUps = NextPow2(_maxLoopUps + 1);
            _probeSequenceLength = Log2(_maxLoopUps);

            var oldEntries = new Entry<TKey, TValue>[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);
            _entries = new Entry<TKey, TValue>[_maxLoopUps + _probeSequenceLength];
            EntryCount = 0;

            for (var i = 0; i < oldEntries.Length; i++)
            {
                var entry = oldEntries[i];
                if (entry.IsEmpty())
                {
                    continue;
                }

                var hashcode = (uint)entry.Key.GetHashCode();
                uint index = hashcode * GoldenRatio >> _shift;

                EmplaceNew(entry.Key, entry.Value, index);
            }
        }

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
