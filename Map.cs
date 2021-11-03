using System;

namespace Faster
{
    /// <summary>
    /// 
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
        private const uint Prime = 0x9E3779B9; // 2654435769; 
        private int _shift = 32;
        private uint _probeSequenceLength;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Map" /> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        public Map(uint length, double loadFactor = 0.88d)
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
        public bool Emplace(TKey key, TValue value)
        {
            if ((double)EntryCount / _maxLoopUps > _loadFactor || EntryCount >= _maxLoopUps)
            {
                Resize();
            }

            uint hashcode = (uint)key.GetHashCode();
                       
            uint index = hashcode * Prime >> _shift;

            //validate if the key is unique
            if (KeyExists(index, hashcode))
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
            index++;

            insertableEntry.Key = key;
            insertableEntry.Value = value;
            insertableEntry.Psl = psl;

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

                if (entry.Psl < psl)
                {
                    swap(ref insertableEntry, ref _entries[index]);
                    psl = insertableEntry.Psl; // reset psl;
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
        private bool KeyExists(uint index, uint hashcode)
        {
            var currentEntry = _entries[index];
            if (currentEntry.IsEmpty())
            {
                return false;
            }

            if (currentEntry.Key.GetHashCode() == hashcode)
            {
                return true;
            }
            
            byte psl = currentEntry.Psl;

            ++index;

            for (; currentEntry.Psl >= psl; ++index, ++psl)
            {
                currentEntry = _entries[index];
                if (currentEntry.IsEmpty())
                {
                    return false;
                }

                if (currentEntry.Psl >= psl && currentEntry.Key.GetHashCode() == hashcode)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// update the entry
        /// </summary>
        public void Update(TKey key, TValue value)
        {
            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * Prime >> _shift;

            var currentEntry = _entries[index];
            if (currentEntry.Key.GetHashCode() == hashcode)
            {
                currentEntry.Value = value;
                _entries[index] = currentEntry;
                return;
            }

            byte psl = currentEntry.Psl;

            ++index;
            for (; currentEntry.Psl >= psl; ++index, ++psl)
            {
                currentEntry = _entries[index];
                if (currentEntry.Key.GetHashCode() == hashcode)
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
        public void Remove(TKey key)
        {
            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * Prime >> _shift;
            byte psl = 0;
            bool found = false;
            var bounds = index + _probeSequenceLength;

            for (;index < bounds ; ++index)
            {
                var currentEntry = _entries[index];
                if (currentEntry.Key.GetHashCode() == hashcode)
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
        /// Gets the value with the corresponding key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Get(TKey key, out TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            uint index = hashcode * Prime >> _shift;

            var currentEntry = _entries[index];
            if (currentEntry.Key.GetHashCode() == hashcode)
            {
                value = currentEntry.Value;
                return;
            }

            ++index; // increase index since the initial index is already checked

            byte psl = currentEntry.Psl;
            for (; currentEntry.Psl >= psl; ++index, ++psl)
            {
                currentEntry = _entries[index];
                if (currentEntry.Key.GetHashCode() == hashcode)
                {
                    value = currentEntry.Value;
                    return;
                }
            }

            value = default;
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
                uint index = hashcode * Prime >> _shift;

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
