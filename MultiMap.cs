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
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - Keeps track of the current probe count used
    /// - Multiple values can be inserted with the same key, minor drawback is that only 127 values can be added with the same key
    /// - fibonacci hashing
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class MultiMap<TKey, TValue>
    {
        #region Fields

        private uint _maxlookups;
        private readonly double _loadFactor;
        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private byte _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private readonly IEqualityComparer<TKey> _keyCompare;
        private readonly IEqualityComparer<TValue> _valueComparer;

        private InfoByte[] _info;
        private Entry<TKey, TValue>[] _entries;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public int Count { get; private set; }

        /// <summary>
        /// Returns all the entries as KeyValuePair objects
        /// </summary>
        /// <value>
        /// The keys.
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
        /// Returns all available keys
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
        /// Returns all available Values
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

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap{TKey,TValue}"/> class.
        /// </summary>
        public MultiMap() : this(16, 0.5d, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public MultiMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public MultiMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to resolve hashcollissions</param>
        public MultiMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer) : this(length, loadFactor, keyComparer, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to resolve hashcollissions</param>
        /// <param name="valueComparer">Used to retrieve a proper entry by validing if the value is the same</param>
        public MultiMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            //default length is 16
            _maxlookups = length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _maxProbeSequenceLength = Log2(size);

            _keyCompare = keyComparer ?? EqualityComparer<TKey>.Default;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            _shift = _shift - Log2(_maxlookups) + 1;
            _entries = new Entry<TKey, TValue>[_maxlookups + _maxProbeSequenceLength + 1];
            _info = new InfoByte[_maxlookups + _maxProbeSequenceLength + 1];
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Emplace(TKey key, TValue value)
        {
            //Resize if loadfactor is reached
            if ((double)Count / _maxlookups > _loadFactor)
            {
                Resize();
            }

            //Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //check if key is unique
            if (Contains(key, value))
            {
                return false;
            }

            //create entry
            Entry<TKey, TValue> entry = default;
            entry.Value = value;
            entry.Key = key;
            entry.Hashcode = hashcode;

            //Create default info byte
            InfoByte current = default;

            //Assign 0 to psl so it wont be seen as empty
            current.Psl = 0;

            //retrieve infobyte
            var info = _info[index];

            do
            {
                //Increase _current probe sequence
                if (_currentProbeSequenceLength < current.Psl)
                {
                    _currentProbeSequenceLength = current.Psl;
                }

                //Empty spot, add entry
                if (info.IsEmpty())
                {
                    _entries[index] = entry;
                    _info[index] = current;
                    ++Count;
                    return true;
                }

                //Steal from the rich, give to the poor
                if (current.Psl > info.Psl)
                {
                    Swap(ref entry, ref _entries[index]);
                    Swap(ref current, ref _info[index]);
                    continue;
                }

                //max psl is reached, resize
                if (current.Psl == _maxProbeSequenceLength)
                {
                    ++Count;
                    IncreaseMaxProbeSequence();
                    EmplaceInternal(entry, current);
                    return true;
                }

                //increase index
                info = _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
        }

        /// <summary>
        /// Gets the first entry matching the specified key.
        /// If the same key is used for multiple entries we return the first entry matching the given criteria
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Get(TKey key, out TValue value)
        {
            //Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                var entry = _entries[index];

                //validate hashcode
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }

                //increase index by 1
                entry = _entries[++index];

                //validate hashcode
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            value = default;

            //not found
            return false;
        }

        /// <summary>
        /// Get all entries matching the key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public IEnumerable<TValue> GetAll(TKey key)
        {
            //Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                var entry = _entries[index];

                //validate hashcode
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    yield return entry.Value;
                }

                //increase index by 1
                entry = _entries[++index];

                //validate hashcode
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    yield return entry.Value;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);
        }

        /// <summary>
        /// Locate the entry by using a key-value, update the entry by using a delegate
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">Update old value by invoking delegate</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Update(TKey key, TValue oldValue, Func<TValue, TValue> newValue)
        {
            //Get object identity hashcode
            var hashcode = key.GetHashCode();

            //Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                var entry = _entries[index];

                //validate hashcode
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key) && _valueComparer.Equals(oldValue, entry.Value))
                {
                    _entries[index].Value = newValue(entry.Value);
                    return true;
                }

                //increase index by 1
                entry = _entries[++index];

                //validate hashcode
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key) && _valueComparer.Equals(oldValue, entry.Value))
                {
                    _entries[index].Value = newValue(entry.Value);
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            //entry not found
            return false;
        }

        /// <summary>
        /// Removes entry using key and value
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool Remove(TKey key, TValue value)
        {
            //Get ObjectIdentity hashcode
            int hashcode = key.GetHashCode();

            //Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                var entry = _entries[index];

                //validate hash en compare keys
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key) && _valueComparer.Equals(value, entry.Value))
                {
                    //remove entry from list
                    _entries[index] = default;
                    _info[index] = default;
                    --Count;
                    ShiftRemove(index);
                    return true;
                }

                //increase index by 1
                entry = _entries[++index];

                //validate hash and compare keys
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key) && _valueComparer.Equals(value, entry.Value))
                {
                    //remove entry from list
                    _entries[index] = default;
                    _info[index] = default;
                    --Count;
                    ShiftRemove(index);
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            // No entries removed
            return false;
        }

        /// <summary>
        /// Determines whether the specified key is already inserted in the map
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(256)]
        public bool Contains(TKey key, TValue value)
        {
            //Get ObjectIdentity hashcode
            int hashcode = key.GetHashCode();

            //Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //backout early
            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                return false;
            }

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                var entry = _entries[index];

                //validate hash
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key) && _valueComparer.Equals(value, entry.Value))
                {
                    return true;
                }

                //increase index by 1
                entry = _entries[++index];

                //validate hash
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key) && _valueComparer.Equals(value, entry.Value))
                {
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            //not found
            return false;
        }

        /// <summary>
        /// Determines whether the specified key and value exists
        /// </summary>
        /// <param name="key">The key.</param>
       
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(256)]
        public bool Contains(TKey key)
        {
            //Get ObjectIdentity hashcode
            int hashcode = key.GetHashCode();

            //Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //backout early
            var info = _info[index];
            if (info.IsEmpty())
            {
                //Dont unnecessary iterate over the entries
                return false;
            }

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                var entry = _entries[index];

                //validate hash
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    return true;
                }

                //increase index by 1
                entry = _entries[++index];

                //validate hash
                if (hashcode == entry.Hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            //not found
            return false;
        }

        /// <summary>
        /// Locates the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public int IndexOf(TKey key)
        {
            var hashcode = key.GetHashCode();
            for (int i = 0; i < _entries.Length; i++)
            {
                var info = _info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = _entries[i];
                if (entry.Hashcode == hashcode && _keyCompare.Equals(key, entry.Key))
                {
                    return i;
                }
            }

            return -1;
        }

        ///// <summary>
        ///// Copies all entries from a different multimap
        ///// </summary>
        ///// <param name="valueMap">The map.</param>
        /// <summary>
        /// Copies the specified fast map.
        /// </summary>
        /// <param name="multimap">The multimap.</param>
        public void Copy(MultiMap<TKey, TValue> multimap)
        {
            for (var i = 0; i < multimap._entries.Length; ++i)
            {
                var info = multimap._info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = multimap._entries[i];
                Emplace(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _entries.Length; ++i)
            {
                _entries[i] = default;
                _info[i] = default;
            }

            Count = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Remove an entry by using a backshift removal
        /// </summary>
        /// <param name="index">The index.</param>
        [MethodImpl(256)]
        private void ShiftRemove(uint index)
        {
            //Get next entry
            var next = _info[++index];

            while (!next.IsEmpty() && next.Psl != 0)
            {
                //swap upper entry with lower
                Swap(ref _entries[index], ref _entries[index - 1]);

                //decrease next psl by 1
                _info[index].Psl--;

                //swap upper info with lower
                Swap(ref _info[index], ref _info[index - 1]);

                //increase index by one
                next = _info[++index];
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
            ++c;
            return c;
        }

        /// <summary>
        /// Used for set checking operations (using enumerables) that rely on counting
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _maxlookups = NextPow2(_maxlookups + 1);
            _maxProbeSequenceLength = Log2(_maxlookups);

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

                EmplaceInternal(oldEntries[i], info);
            }
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void IncreaseMaxProbeSequence()
        {
            _maxProbeSequenceLength += 2;

            var oldEntries = new Entry<TKey, TValue>[_entries.Length + 2];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var olfInfo = new InfoByte[_info.Length + 2];
            Array.Copy(_info, olfInfo, _entries.Length);

            _entries = oldEntries;
            _info = olfInfo;
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
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="current">The metadata.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private void EmplaceInternal(Entry<TKey, TValue> entry, InfoByte current)
        {
            uint index = (uint)entry.Hashcode * GoldenRatio >> _shift;
            current.Psl = 0;

            var info = _info[index];

            do
            {
                if (info.IsEmpty())
                {
                    _entries[index] = entry;
                    _info[index] = current;
                    return;
                }

                if (current.Psl > info.Psl)
                {
                    Swap(ref entry, ref _entries[index]);
                    Swap(ref current, ref _info[index]);
                    continue;
                }

                if (_currentProbeSequenceLength < current.Psl)
                {
                    _currentProbeSequenceLength = current.Psl;
                }

                if (current.Psl == _maxProbeSequenceLength)
                {
                    IncreaseMaxProbeSequence();
                    EmplaceInternal(entry, current);
                    return;
                }

                //increase index
                info = _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
        }

        /// <summary>
        /// Swaps the content of the specified values
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
        /// Swaps the content of the specified values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref InfoByte x, ref InfoByte y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }

        #endregion
    }
}
