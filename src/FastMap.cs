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
    /// - Keeps track of the currentProbeCount which makes sure we can back out early eventhough the maxprobcount exceeds the cpc
    /// - loadfactor can easily be increased to 0.9 while maintaining an incredible speed
    /// - use numerical values as keys
    /// - fibonacci hashing
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
        public int Count { get; private set; }

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
        private uint _length;
        private readonly double _loadFactor;
        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private uint _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private uint _maxlengthBeforeResize;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FastMap{TKey,TValue}"/> class.
        /// </summary>
        public FastMap() : this(8, 0.5d) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastMap{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public FastMap(uint length) : this(length, 0.5d) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public FastMap(uint length, double loadFactor)
        {
            //default length is 8
            _length = length == 0 ? 8 : length;
            _loadFactor = loadFactor;

           _length = NextPow2(_length);      

            _maxProbeSequenceLength = _loadFactor <= 0.5 ? Log2(_length) : PslLimit(_length);

            _maxlengthBeforeResize = (uint)(_length * loadFactor);

            _shift = _shift - Log2(_length) + 1;
            _entries = new Entry<TKey, TValue>[_length + _maxProbeSequenceLength + 1];
            _info = new InfoByte[_length + _maxProbeSequenceLength + 1];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts a value using a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Emplace(TKey key, TValue value)
        {
            //Resize if loadfactor is reached
            if (Count >= _maxlengthBeforeResize)
            {
                Resize();
            }

            //Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a
            uint index = ((uint)hashcode * GoldenRatio) >> _shift;

            //check if key is unique
            if (ContainsKey(ref hashcode, index))
            {
                return false;
            }

            //Create entry
            Entry<TKey, TValue> fastEntry = default;
            fastEntry.Value = value;
            fastEntry.Key = key;

            //Create default info byte
            InfoByte current = default;

            //Assign 0 to psl so it wont be seen as empty
            current.Psl = 0;

            //retrieve infobyte
            ref var info = ref _info[index];

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
                    _entries[index] = fastEntry;
                    info = current;
                    ++Count;
                    return true;
                }

                //Steal from the rich, give to the poor
                if (current.Psl > info.Psl)
                {
                    Swap(ref fastEntry, ref _entries[index]);
                    Swap(ref current, ref info);
                    continue;
                }

                //max psl is reached, resize
                if (current.Psl == _maxProbeSequenceLength)
                {
                    ++Count;
                    Resize();
                    EmplaceInternal(ref fastEntry, ref current);
                    return true;
                }

                //increase index
                info = ref _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
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
            //Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = ((uint)hashcode * GoldenRatio) >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //Get entry by ref
                ref var entry = ref _entries[index];

                //validate hashcode
                if (hashcode == entry.Key.GetHashCode())
                {
                    value = entry.Value;
                    return true;
                }

                ++index;
                //increase index by one and validate if within bounds
            } while (index <= maxDistance);

            value = default;
            return false;
        }

        /// <summary>
        /// Update entry using a key and value
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Update(TKey key, TValue value)
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
                ref var entry = ref _entries[index];

                //validate hashcode
                if (hashcode == entry.Key.GetHashCode())
                {
                    _entries[index].Value = value;
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            //entry not found
            return false;
        }

        /// <summary>
        /// Removes entry using a backshift removal
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Operation succeeded yes or no</returns>
        [MethodImpl(256)]
        public bool Remove(TKey key)
        {
            //Get ObjectIdentity hashcode
            int hashcode = key.GetHashCode();

            //Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                ref var entry = ref _entries[index];

                //validate hash
                if (hashcode == entry.Key.GetHashCode())
                {
                    //remove entry
                    entry = default;
                    //remove infobyte
                    _info[index] = default;
                    //remove entry from list
                    --Count;
                    ShiftRemove(ref index);
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            // No entries removed
            return false;
        }

        /// <summary>
        /// Determines whether the specified key exists in the hashmap
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

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                ref var entry = ref _entries[index];

                //validate hash
                if (hashcode == entry.Key.GetHashCode())
                {
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            //not found
            return false;
        }

        /// <summary>
        /// Copies all entries from source to target
        /// </summary>
        /// <param name="fastMap">The fast map.</param>
        [MethodImpl(256)]
        public void Copy(FastMap<TKey, TValue> fastMap)
        {
            for (var i = 0; i < fastMap._entries.Length; i++)
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
        /// Returns the current index of Tkey
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        [MethodImpl(256)]
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

                if (_entries[i].Key.GetHashCode() == hashcode)
                {
                    return i;
                }
            }

            //Return -1 which indicates not found
            return -1;
        }

        /// <summary>
        /// Set default state of all entries
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                _info[i] = default;
                _entries[i] = default;
            }

            Count = 0;
        }

        /// <summary>
        /// Gets or sets the entry by using a TKey
        /// </summary>
        /// <value>
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}
        /// or
        /// Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}</exception>
        /// <exception cref="KeyNotFoundException">Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}
        /// or
        /// Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}</exception>
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
        /// Shift remove will shift all entries backwards until there is an empty entry
        /// </summary>
        /// <param name="index">The index.</param>
        [MethodImpl(256)]
        private void ShiftRemove(ref uint index)
        {
            //Get next entry
            ref var next = ref _info[++index];

            while (!next.IsEmpty() && next.Psl != 0)
            {
                //decrease next psl by 1
                next.Psl--;
                //swap upper info with lower
                Swap(ref next, ref _info[index - 1]);
                //swap upper entry with lower
                Swap(ref _entries[index], ref _entries[index - 1]);
                //increase index by one
                next = ref _info[++index];
            }
        }

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="entry">The fast entry.</param>
        /// <param name="current">The information byte.</param>
        [MethodImpl(256)]
        private void EmplaceInternal(ref Entry<TKey, TValue> entry, ref InfoByte current)
        {
            //get objectidentiy
            var hashcode = entry.Key.GetHashCode();

            uint index = ((uint)hashcode * GoldenRatio) >> _shift;

            //reset psl
            current.Psl = 0;

            ref var info = ref _info[index];

            do
            {
                if (_currentProbeSequenceLength < current.Psl)
                {
                    _currentProbeSequenceLength = current.Psl;
                }

                if (info.IsEmpty())
                {
                    _entries[index] = entry;
                    info = current;
                    return;
                }

                if (current.Psl > info.Psl)
                {
                    Swap(ref entry, ref _entries[index]);
                    Swap(ref current, ref _info[index]);
                    continue;
                }              

                //increase index
                info = ref _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
        }

        /// <summary>
        /// Swaps the content of the specified FastEntry values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [MethodImpl(256)]
        private void Swap(ref Entry<TKey, TValue> x, ref Entry<TKey, TValue> y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        /// <summary>
        /// Swaps the content of the specified Infobyte values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [MethodImpl(256)]
        private void Swap(ref InfoByte x, ref InfoByte y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        /// <summary>
        /// Returns a power of two probe sequence lengthzz
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private uint PslLimit(uint size)
        {
            switch (size)
            {
                case 16: return 4;
                case 32: return 5;
                case 64: return 6;
                case 128: return 7;
                case 256: return 8;
                case 512: return 9;
                case 1024: return 12;
                case 2048: return 15;
                case 4096: return 20;
                case 8192: return 25;
                case 16384: return 30;
                case 32768: return 35;
                case 65536: return 40;
                case 131072: return 45;
                case 262144: return 50;
                case 524288: return 55;
                case 1048576: return 60;
                case 2097152: return 65;
                case 4194304: return 70;
                case 8388608: return 75;
                case 16777216: return 80;
                case 33554432: return 85;
                case 67108864: return 90;
                case 134217728: return 95;
                case 268435456: return 100;
                case 536870912: return 105;
                default: return 10;
            }
        }

        [MethodImpl(256)]
        private bool ContainsKey(ref int hashcode, uint index)
        {
            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                ref var entry = ref _entries[index];

                //validate hash
                if (hashcode == entry.Key.GetHashCode())
                {
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            return false;
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _length = NextPow2(_length + 1);

            _maxProbeSequenceLength = _loadFactor <= 0.5 ? Log2(_length) : PslLimit(_length);
            _maxlengthBeforeResize = (uint)(_length * _loadFactor);                       
            _currentProbeSequenceLength = 0;

            var oldEntries = _entries.Clone() as Entry<TKey, TValue>[];
            var oldInfo = _info.Clone() as InfoByte[]; 

            _entries = new Entry<TKey, TValue>[_length + _maxProbeSequenceLength + 1];
            _info = new InfoByte[_length + _maxProbeSequenceLength + 1];

            for (var i = 0; i < oldEntries.Length; ++i)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = oldEntries[i];
                EmplaceInternal(ref entry, ref info);
            }
        }

        /// <summary>
        /// Calculates next power of 2
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

        // Used for set checking operations (using enumerables) that rely on counting
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