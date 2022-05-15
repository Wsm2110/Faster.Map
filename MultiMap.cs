using Faster.Map.Core;
using Faster.Map.Exceptions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Faster.Map
{
    /// <summary>
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size) 
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
        private const uint Multiplier = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private byte _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly IEqualityComparer<TValue> _valueComparer;

        private MetaByte[] _info;
        private MultiEntry<TKey, TValue>[] _entries;
        private uint _size;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public uint Count { get; private set; }

        /// <summary>
        /// returns all entries in a key value manner
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
        /// returns all keys
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
        /// returns all Values
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
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}"/> class.
        /// </summary>
        public MultiMap() : this(16, 0.5d, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        public MultiMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        public MultiMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        /// <param name="keyComparer">The key comparer.</param>
        public MultiMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer) : this(length, loadFactor, keyComparer, EqualityComparer<TValue>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap{TKey, TValue}"/> class.
        /// </summary>
        public MultiMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            //default length is 16
            _maxlookups = length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _maxProbeSequenceLength = length < 127 ? Log2(size) : (byte)127;

            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            _shift = _shift - Log2(_maxlookups) + 1;
            _size = 0;

            _size = _maxlookups + _maxProbeSequenceLength;

            _entries = new MultiEntry<TKey, TValue>[_size];
            _info = new MetaByte[_size];
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
            if ((double)Count / _maxlookups > _loadFactor)
            {
                Resize();
            }

            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            if (KeyValueExists(key, value))
            {
                return false;
            }

            MultiEntry<TKey, TValue> entry = default;
            entry.Value = value;
            entry.Key = key;

            MetaByte metadata = default;
            metadata.Psl = 0;
            metadata.Hashcode = hashcode;

            for (; ; ++metadata.Psl, ++index)
            {
                if (_currentProbeSequenceLength < metadata.Psl)
                {
                    _currentProbeSequenceLength = metadata.Psl;
                }

                var info = _info[index];
                if (info.IsEmpty())
                {
                    _entries[index] = entry;
                    _info[index] = metadata;
                    ++Count;
                    return true;
                }

                if (info.Hashcode == metadata.Hashcode)
                {
                    ++Count;
                    //make sure same hashcodes are in line
                    StartSwapping(index, ref entry, ref metadata);
                    return true;
                }

                if (metadata.Psl > info.Psl)
                {
                    //steal from the rich, give to the poor
                    Swap(ref entry, ref _entries[index]);
                    Swap(ref metadata, ref _info[index]);
                    continue;
                }

                if (metadata.Psl == _maxProbeSequenceLength)
                {
                    if (metadata.Psl == 127)
                    {
                        throw new MultiMapException("Only 127 values can be stored with 1 unique key");
                    }

                    Resize();
                    //Make sure after a resize to insert the current entry
                    EmplaceInternal(ref entry, ref metadata);
                    return true;
                }
            }
        }

        /// <summary>
        /// Swap all entries until there is an empty entry
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="entry">The entry.</param>
        /// <param name="data">The data.</param>
        private void StartSwapping(uint index, ref MultiEntry<TKey, TValue> entry, ref MetaByte data)
        {

        Start:

         
            if (data.IsEmpty())
            {
                return;
            }

            ++data.Psl;

            if (index == _maxlookups)
            {
                Resize();
                EmplaceInternal(ref entry, ref data);
                return;
            }
            
            if (_currentProbeSequenceLength < data.Psl)
            {
                _currentProbeSequenceLength = data.Psl;
            }


            //swap lower with upper
            Swap(ref entry, ref _entries[index + 1]);
            Swap(ref data, ref _info[index + 1]);

            ++index;

            goto Start;
        }

        /// <summary>
        /// Gets the first entry matching the specified key.
        /// If the same key is used for multiple entries we return the first entry matching the given criteria
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public unsafe bool Get(TKey key, out TValue value)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            fixed (MetaByte* ptr = &_info[index])
            {
                MetaByte* p = ptr;

                if (p->IsEmpty())
                {
                    value = default;
                    return false;
                }

                var maxDistance = index + _currentProbeSequenceLength;
                byte exit = 0;
                do
                {
                    var entry = _entries[index];
                    if (p->Hashcode == hashcode)
                    {
                        exit = 1;
                        if (_keyComparer.Equals(entry.Key, key))
                        {
                            value = entry.Value;
                            return true;
                        }

                        goto Next;
                    }

                    if (exit == 1)
                    {
                        value = default;
                        return true;
                    }

                Next:
                    ++index;
                    ++p;

                    //127 is the max allowed probe sequence length
                } while (index <= maxDistance && !p->IsEmpty());
            }


            value = default;
            return false;
        }

        /// <summary>
        /// Get all entries matching the key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public IEnumerable<TValue> GetAll(TKey key)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                yield break;
            }

            var maxDistance = index + _currentProbeSequenceLength;
            byte exit = 0;
            do
            {
                if (hashcode == info.Hashcode)
                {
                    exit = 1;
                    var entry = _entries[index];
                    if (_keyComparer.Equals(entry.Key, key))
                    {
                        yield return entry.Value;
                    }

                    goto Next;
                }

                if (exit == 1)
                {
                    yield break;
                }

            Next:
                info = _info[++index];
            } while (index <= maxDistance && !info.IsEmpty());
        }

        /// <summary>
        /// Swap old value with new value
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Update(TKey key, TValue oldValue, TValue newValue)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                return false;
            }

            var maxDistance = index + _currentProbeSequenceLength;
            byte exit = 0;

            do
            {
                if (hashcode == info.Hashcode)
                {
                    exit = 1;
                    var entry = _entries[index];
                    if (_keyComparer.Equals(entry.Key, key) && _valueComparer.Equals(oldValue, entry.Value))
                    {
                        entry.Value = newValue;
                        _entries[index] = entry;
                        return true;
                    }

                    goto Next;
                }

                if (exit == 1)
                {
                    return false;
                }

            Next:
                info = _info[++index];

            } while (index <= maxDistance && !info.IsEmpty());

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
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                return false;
            }

            uint maxDistance = index + _currentProbeSequenceLength;
            byte exit = 0;
            do
            {
                if (hashcode == info.Hashcode)
                {
                    exit = 1;
                    var entry = _entries[index];
                    if (_keyComparer.Equals(entry.Key, key) && _valueComparer.Equals(entry.Value, value))
                    {
                        _entries[index] = default; // clear entry
                        _info[index] = default; //clear metadata
                        --Count;
                        ShiftRemove(index);
                        return true;
                    }

                    goto Next;
                }

                if (exit == 1)
                {
                    return false;
                }

            Next:
                info = _info[++index];
            } while (index <= maxDistance && !info.IsEmpty());

            return false;
        }

        /// <summary>
        /// Removes all entries matching key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool RemoveAll(TKey key)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                return true;
            }

            byte removed = 0;
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                if (hashcode == info.Hashcode)
                {
                    var entry = _entries[index];
                    if (_keyComparer.Equals(entry.Key, key))
                    {
                        removed = 1;
                        _entries[index] = default;
                        _info[index] = default;
                        --Count;

                        if (ShiftRemove(index))
                        {
                            info = _info[index];
                            --maxDistance;
                            continue;
                        }

                        break;
                    }
                }

                info = _info[++index];

            } while (index <= maxDistance && !info.IsEmpty());

            return removed == 1;
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
                return false;
            }

            var maxDistance = index + _currentProbeSequenceLength;

            byte exit = 0;
            do
            {
                if (hashcode == info.Hashcode)
                {
                    exit = 1;
                    if (_keyComparer.Equals(_entries[index].Key, key))
                    {
                        return true;
                    }

                    goto Next;
                }

                if (exit == 1)
                {
                    return false;
                }

            Next:
                info = _info[++index];

            } while (index <= maxDistance && !info.IsEmpty());

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

                if (info.Hashcode == hashcode && _keyComparer.Equals(key, _entries[i].Key))
                {
                    return i;
                }
            }

            return -1;
        }

        ///// <summary>
        ///// Copies entries from one map to another
        ///// </summary>
        ///// <param name="valueMap">The map.</param>
        /// <summary>
        /// Copies the specified fast map.
        /// </summary>
        /// <param name="fastMap">The fast map.</param>
        public void Copy(MultiMap<TKey, TValue> fastMap)
        {
            for (var i = 0; i < fastMap._entries.Length; ++i)
            {
                var info = fastMap._info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = fastMap._entries[i];
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
                --Count;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Shifts the remove.
        /// </summary>
        /// <param name="index">The index.</param>
        [MethodImpl(256)]
        private bool ShiftRemove(uint index)
        {
            bool shifted = false;

        Start:

            var metaByte = _info[index + 1];
            if (metaByte.IsEmpty())
            {
                return shifted;
            }

            if (metaByte.Psl == 0)
            {
                return shifted;
            }

            //swap upper with lower
            Swap(ref _entries[index + 1], ref _entries[index]);

            _info[index + 1].Psl--;

            Swap(ref _info[index + 1], ref _info[index]);

            ++index;

            shifted = true;

            goto Start;
        }

        [MethodImpl(256)]
        private bool KeyValueExists(TKey key, TValue value)
        {
            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * Multiplier >> _shift;

            var info = _info[index];
            if (info.IsEmpty())
            {
                return false;
            }

            var maxDistance = index + _currentProbeSequenceLength;
            byte exit = 0;

            do
            {
                if (hashcode == info.Hashcode)
                {
                    exit = 1;
                    var entry = _entries[index];
                    if (_keyComparer.Equals(entry.Key, key) && _valueComparer.Equals(entry.Value, value))
                    {
                        return true;
                    }

                    goto Next;
                }

                if (exit == 1)
                {
                    return false;
                }

            Next:
                info = _info[++index];

            } while (index <= maxDistance && !info.IsEmpty());

            return false;
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
            _maxProbeSequenceLength = _maxlookups < 127 ? Log2(_maxlookups) : (byte)127;

            var oldEntries = new MultiEntry<TKey, TValue>[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldInfo = new MetaByte[_entries.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _size = _maxlookups + _maxProbeSequenceLength;

            _entries = new MultiEntry<TKey, TValue>[_size];
            _info = new MetaByte[_size];

            Count = 0;

            for (var i = 0; i < oldEntries.Length; i++)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                EmplaceInternal(ref oldEntries[i], ref info);
            }
        }

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private void EmplaceInternal(ref MultiEntry<TKey, TValue> entry, ref MetaByte metadata)
        {
            // Calculate index
            uint index = (uint)metadata.Hashcode * Multiplier >> _shift;

            // Reset psl
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
                    _entries[index] = entry;
                    _info[index] = metadata;
                    ++Count;
                    return;
                }

                if (info.Hashcode == metadata.Hashcode)
                {
                    ++Count;
                    //make sure same hashcodes are in line
                    StartSwapping(index, ref entry, ref metadata);
                    return;
                }

                if (metadata.Psl > info.Psl)
                {
                    Swap(ref entry, ref _entries[index]);
                    Swap(ref metadata, ref _info[index]);
                    continue;
                }

                if (metadata.Psl == _maxProbeSequenceLength)
                {
                    if (metadata.Psl == 127)
                    {
                        throw new MultiMapException("Only 127 values can be stored with 1 unique key");
                    }

                    Resize();
                    //Make sure after a resize to insert the current entry
                    EmplaceInternal(ref entry, ref metadata);
                    return;
                }
            }
        }

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref MultiEntry<TKey, TValue> x, ref MultiEntry<TKey, TValue> y)
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
        private void Swap(ref MetaByte x, ref MetaByte y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }

        #endregion
    }
}
