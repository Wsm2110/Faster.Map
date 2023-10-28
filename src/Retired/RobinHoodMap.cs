using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Core;

namespace Faster.Map.Retired
{
    /// <summary>
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robinghood hashing
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - Keeps track of the currentProbeCount which makes sure we can back out early eventhough the maxprobcount exceeds the cpc
    /// - fibonacci hashing
    /// </summary>
    public class RobinHoodMap<TKey, TValue>
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The EntryTwo count.
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
                        var EntryTwo = _entries[i];
                        yield return new KeyValuePair<TKey, TValue>(EntryTwo.Key, EntryTwo.Value);
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

        private Metabyte[] _info;
        private EntryTwo<TKey, TValue>[] _entries;
        private uint _length;
        private readonly double _loadFactor;
        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private byte _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private readonly IEqualityComparer<TKey> _keyCompare;
        private int _maxLookupsBeforeResize;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        public RobinHoodMap() : this(8, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public RobinHoodMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public RobinHoodMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public RobinHoodMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            //default length is 8
            _length = length;
            _loadFactor = loadFactor;

            var size = NextPow2(_length);
            _maxProbeSequenceLength = loadFactor <= 0.5 ? Log2(size) : PslLimit(size);

            _maxLookupsBeforeResize = (int)(size * loadFactor);

            _keyCompare = keyComparer ?? EqualityComparer<TKey>.Default;

            _shift = _shift - Log2(_length) + 1;

            _entries = new EntryTwo<TKey, TValue>[size + _maxProbeSequenceLength + 1];
            _info = new Metabyte[size + _maxProbeSequenceLength + 1];
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
            //Resize if loadfactor is reached
            if (Count >= _maxLookupsBeforeResize)
            {
                Resize();
            }

            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * GoldenRatio >> _shift;

            //check if key is unique
            if (ContainsKey(ref hashcode, index, key))
            {
                return false;
            }

            //create EntryTwo
            EntryTwo<TKey, TValue> EntryTwo = default;
            EntryTwo.Value = value;
            EntryTwo.Key = key;

            //Create default Metabyte
            Metabyte current = default;

            //Assign 0 to psl so it wont be seen as empty
            current.Psl = 0;

            //retrieve Metabyte
            ref var info = ref _info[index];

            do
            {
                //Increase _current probe sequence
                if (_currentProbeSequenceLength < current.Psl)
                {
                    _currentProbeSequenceLength = current.Psl;
                }

                //Empty spot, add EntryTwo
                if (info.IsEmpty())
                {
                    _entries[index] = EntryTwo;
                    info = current;
                    ++Count;
                    return true;
                }

                //Steal from the rich, give to the poor
                if (current.Psl > info.Psl)
                {
                    Swap(ref EntryTwo, ref _entries[index]);
                    Swap(ref current, ref info);
                    continue;
                }

                //max psl is reached, resize
                if (current.Psl == _maxProbeSequenceLength)
                {
                    ++Count;
                    Resize();
                    EmplaceInternal(EntryTwo, current);
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
            var hashcode = (uint)key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * GoldenRatio >> _shift;

            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //Get EntryTwo by ref
                var EntryTwo = FindEntryTwo(_entries, index);

                //validate hashcode
                if (hashcode == EntryTwo.Hashcode && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    value = EntryTwo.Value;
                    return true;
                }

                ++index;
                //increase index by one and validate if within bounds
            } while (index <= maxDistance);

            value = default;
            return false;
        }

        /// <summary>
        ///Updates the value of a specific key
        /// </summary>
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
                //Get EntryTwo by ref
                ref var EntryTwo = ref _entries[index];

                //validate hashcode
                if (hashcode == EntryTwo.Hashcode && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    EntryTwo.Value = value;
                    return true;
                }

                ++index;
                //increase index by one and validate if within bounds
            } while (index <= maxDistance);

            //EntryTwo not found
            return false;
        }

        /// <summary>
        ///  Remove EntryTwo with a backshift removal
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
                //unrolling loop twice seems to give a minor speedboost
                ref var EntryTwo = ref _entries[index];

                //validate hash en compare keys
                if (hashcode == EntryTwo.Hashcode && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    //remove EntryTwo from list
                    EntryTwo = default;
                    _info[index] = default;
                    --Count;
                    ShiftRemove(index);
                    return true;
                }

                ++index;
                //increase index by one and validate if within bounds
            } while (index <= maxDistance);

            // No entries removed
            return false;
        }

        /// <summary>
        /// Determines whether the specified key contains key.
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
                var EntryTwo = _entries[index];

                //validate hash
                if (hashcode == EntryTwo.Hashcode && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    return true;
                }

                //increase index by 1
                EntryTwo = _entries[++index];

                //validate hash
                if (hashcode == EntryTwo.Hashcode && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    return true;
                }

                //increase index by one and validate if within bounds
            } while (++index <= maxDistance);

            //not found
            return false;
        }

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="denseMap">The map.</param>
        public void Copy(RobinHoodMap<TKey, TValue> denseMap)
        {
            for (var i = 0; i < denseMap._entries.Length; ++i)
            {
                var info = denseMap._info[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var EntryTwo = denseMap._entries[i];
                Emplace(EntryTwo.Key, EntryTwo.Value);
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

        /// <summary>
        /// Gets or sets the value by using a Tkey
        /// </summary>
        /// <value>
        /// The 
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">
        /// Unable to find EntryTwo - {key.GetType().FullName} key - {key.GetHashCode()}
        /// or
        /// Unable to find EntryTwo - {key.GetType().FullName} key - {key.GetHashCode()}
        /// </exception>
        public TValue this[TKey key]
        {
            get
            {
                if (Get(key, out var result))
                {
                    return result;
                }

                throw new KeyNotFoundException($"Unable to find EntryTwo - {key.GetType().FullName} key - {key.GetHashCode()}");
            }
            set
            {
                if (!Update(key, value))
                {
                    throw new KeyNotFoundException($"Unable to find EntryTwo - {key.GetType().FullName} key - {key.GetHashCode()}");
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

                var EntryTwo = _entries[i];
                if (EntryTwo.Hashcode == key.GetHashCode() && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region Private Methods

        [MethodImpl(256)]
        private bool ContainsKey(ref uint hashcode, uint index, TKey key)
        {
            //Determine max distance
            var maxDistance = index + _currentProbeSequenceLength;

            do
            {
                //unrolling loop twice seems to give a minor speedboost
                ref var EntryTwo = ref _entries[index];

                //validate hash
                if (hashcode == EntryTwo.Hashcode && _keyCompare.Equals(key, EntryTwo.Key))
                {
                    return true;
                }

                ++index;
                //increase index by one and validate if within bounds
            } while (index <= maxDistance);


            return false;
        }

        /// <summary>
        /// Emplaces a new EntryTwo without checking for key existence. Keys have already been checked and are unique
        /// </summary>
        /// <param name="EntryTwo">The EntryTwo.</param>
        /// <param name="current">The current.</param>
        [MethodImpl(256)]
        private void EmplaceInternal(EntryTwo<TKey, TValue> EntryTwo, Metabyte current)
        {
            uint index = (uint)EntryTwo.Hashcode * GoldenRatio >> _shift;
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
                    _entries[index] = EntryTwo;
                    info = current;
                    return;
                }

                if (current.Psl > info.Psl)
                {
                    Swap(ref EntryTwo, ref _entries[index]);
                    Swap(ref current, ref info);
                    continue;
                }

                if (current.Psl == _maxProbeSequenceLength)
                {
                    Resize();
                    EmplaceInternal(EntryTwo, current);
                    return;
                }

                //increase index
                info = ref _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
        }

        private void ShiftRemove(uint index)
        {
            //Get next EntryTwo
            ref var next = ref _info[++index];

            while (!next.IsEmpty() && next.Psl != 0)
            {
                //decrease next psl by 1
                next.Psl--;
                //swap upper info with lower
                Swap(ref next, ref _info[index - 1]);
                //swap upper EntryTwo with lower
                Swap(ref _entries[index], ref _entries[index - 1]);
                //increase index by one
                next = ref _info[++index];
            }
        }

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref EntryTwo<TKey, TValue> x, ref EntryTwo<TKey, TValue> y)
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
        private void Swap(ref Metabyte x, ref Metabyte y)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T FindEntryTwo<T>(T[] array, uint index)
        {
#if DEBUG
            return ref array[index];
#else
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
#endif
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

            _maxLookupsBeforeResize = (int)(_length * _loadFactor);
            _currentProbeSequenceLength = 0;

            var oldEntries = new EntryTwo<TKey, TValue>[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldInfo = new Metabyte[_entries.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _entries = new EntryTwo<TKey, TValue>[_length + _maxProbeSequenceLength + 1];
            _info = new Metabyte[_length + _maxProbeSequenceLength + 1];

            for (var i = 0; i < oldEntries.Length; ++i)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var EntryTwo = oldEntries[i];

                EmplaceInternal(EntryTwo, info);
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


        /// <summary>
        /// Gets the first EntryTwo matching the specified key.
        /// If the same key is used for multiple entries we return the first EntryTwo matching the given criteria
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        internal Metabyte Get(TKey key)
        {
            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * GoldenRatio >> _shift;

            // Retrieve EntryTwo
            var info = _info[index];

            if (info.IsEmpty())
            {
                return default;
            }

            return info;
        }


        #endregion
    }
}