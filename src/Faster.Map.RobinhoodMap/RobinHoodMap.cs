using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#nullable enable

namespace Faster.Map.RobinhoodMap
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
    public class RobinhoodMap<TKey, TValue> where TKey : notnull
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// 
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
                for (int i = _entries.Length - 1; i >= 0; --i)
                {
                    var entry = _entries[i];
                    if (entry.Psl is not 0)
                    {
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
                for (int i = _entries.Length - 1; i >= 0; --i)
                {
                    var entry = _entries[i];
                    if (entry.Psl != 0)
                    {
                        yield return entry.Key;
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
                for (int i = _entries.Length - 1; i >= 0; --i)
                {
                    var entry = _entries[i];
                    if (entry.Psl is not 0)
                    {
                        yield return entry.Value;
                    }
                }
            }
        }

        #endregion

        #region Fields

        private Entry[] _entries;
        private uint _length;
        private readonly double _loadFactor;
        private const uint _goldenRatio = 0x9E3779B9; //2654435769;
        private byte _shift = 32;
        private byte _maxProbeSequenceLength;
        private readonly IEqualityComparer<TKey> _keyComparer;

        private int _maxLookupsBeforeResize;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        public RobinhoodMap() : this(8, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public RobinhoodMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public RobinhoodMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public RobinhoodMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            //default length is 8
            _length = NextPow2(length);
            _loadFactor = loadFactor;

            _maxProbeSequenceLength = (byte)BitOperations.Log2(_length);
            _maxLookupsBeforeResize = (int)(_length * loadFactor);
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _shift = (byte)(_shift - BitOperations.Log2(_length));

            var size = (int)_length + _maxProbeSequenceLength;
            _entries = GC.AllocateArray<Entry>(size);
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
            //Resize if loadfactor is reached
            if (Count >= _maxLookupsBeforeResize)
            {
                Resize();
            }

            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            // create default entry
            var insert = new Entry(key, value, 1);

            do
            {
                ref var entry = ref Find(_entries, index);

                //Empty spot, add Entry
                if (entry.Psl == 0)
                {
                    entry = insert;
                    ++Count;
                    return true;
                }

                //equals check
                if (_keyComparer.Equals(insert.Key, entry.Key))
                {
                    return false;
                }

                //Steal from the rich, give to the poor
                if (insert.Psl > entry.Psl)
                {
                    Swap(ref insert, ref entry);
                }

                //increase probe sequence length
                ++insert.Psl;
                ++index;
            } while (true);
        }

        /// <summary>
        /// Gets the value with the corresponding key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            var maxDistance = index + _maxProbeSequenceLength;
            do
            {
                var entry = Find(_entries, index);

                if (_keyComparer.Equals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }

            } while (++index < maxDistance);

            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value for the specified key, or, if the key is not present,
        /// adds an entry and returns the value by ref. This makes it possible to
        /// add or update a value in a single look up operation.
        ///
        /// Will only use one lookup instead of two
        ///
        /// * Example *
        ///
        /// var counterMap = new RobinhoodMap<uint, uint>(16, 0.5);
        /// ref var counter = ref counterMap.GetOrUpdate(1);
        ///
        /// ++counter;
        /// 
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <returns>Reference to the existing value</returns>    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrUpdate(TKey key)
        {
            //Resize if loadfactor is reached
            if (Count >= _maxLookupsBeforeResize)
            {
                Resize();
            }

            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            // create default entry
            var insert = new Entry(key, default, 1);

            do
            {
                ref var entry = ref Find(_entries, index);

                //Empty spot, add Entry
                if (entry.Psl == 0)
                {
                    entry = insert;
                    ++Count;
                    return ref entry.Value;
                }

                //equals check 
                if (_keyComparer.Equals(insert.Key, entry.Key))
                {
                    //update
                    return ref entry.Value;
                }

                //Steal from the rich, give to the poor
                if (insert.Psl > entry.Psl)
                {
                    Swap(ref insert, ref entry);
                }

                //increase probe sequence length
                ++insert.Psl;
                ++index;
            } while (true);
        }

        /// <summary>
        ///Updates the value of a specific key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            var maxDistance = index + _maxProbeSequenceLength;

            do
            {
                ref var entry = ref Find(_entries, index);

                if (_keyComparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return true;
                }

            } while (++index < maxDistance);

            //EntryTwo not found
            return false;
        }

        /// <summary>
        ///  Remove EntryTwo with a backshift removal
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            var maxDistance = index + _maxProbeSequenceLength;

            do
            {
                ref var entry = ref _entries[index];

                //validate hash en compare keys
                if (_keyComparer.Equals(key, entry.Key))
                {
                    //Get next EntryTwo
                    ref var next = ref Find(_entries, ++index);

                    entry = default;

                    while (next.Psl > 1)
                    {
                        //decrease next psl by 1
                        next.Psl--;
                        //swap upper EntryTwo with lower
                        Swap(ref next, ref _entries[index - 1]);
                        //increase index by one
                        next = ref Find(_entries, ++index);
                    }

                    --Count;
                    return true;
                }

                ++index;
                //increase index by one and validate if within bounds
            } while (index < maxDistance);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TKey key)
        {
            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            var maxDistance = index + _maxProbeSequenceLength;

            do
            {
                var entry = Find(_entries, index);
                if (_keyComparer.Equals(entry.Key, key))
                {
                    return true;
                }

            } while (++index < maxDistance);

            //not found
            return false;
        }

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="denseMap">The map.</param>
        public void Copy(RobinhoodMap<TKey, TValue> denseMap)
        {
            for (var i = 0; i < denseMap._entries.Length; ++i)
            {
                var entry = denseMap._entries[i];
                if (entry.Psl is 0)
                {
                    continue;
                }

                Emplace(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_entries);
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
        /// Unable to find Entry - {key.GetType().FullName} key - {key.GetHashCode()}
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

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Entry Find(Entry[] array, uint index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        } 

        private void EmplaceInternal(TKey key, TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            var index = (_goldenRatio * hashcode) >> _shift;
            // create default entry
            var insert = new Entry(key, value, 1);

            do
            {
                ref var entry = ref Find(_entries, index);

                //Empty spot, add Entry
                if (entry.Psl == 0)
                {
                    entry = insert;
                    return;
                }

                //Steal from the rich, give to the poor
                if (insert.Psl > entry.Psl)
                {
                    Swap(ref insert, ref entry);
                }

                //increase probe sequence length
                ++insert.Psl;
                ++index;
            } while (true);
        }

        /// <summary>
        /// Swaps the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref Entry x, ref Entry y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        private void Resize()
        {
            _length = _length << 1;
            _shift--;

            _maxProbeSequenceLength = (byte)BitOperations.Log2(_length);
            _maxLookupsBeforeResize = (int)(_length * _loadFactor);

            var size = Unsafe.As<uint, int>(ref _length) + _maxProbeSequenceLength;

            var oldEntries = _entries;

            _entries = GC.AllocateArray<Entry>(size);

            for (uint i = 0; i < oldEntries.Length; ++i)
            {
                var entry = Find(oldEntries, i);
                if (entry.Psl == 0)
                {
                    continue;
                }

                EmplaceInternal(entry.Key, entry.Value);
            }
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

        [DebuggerDisplay("{Key} {Value} {Psl}")]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Entry
        {
            public byte Psl;
            public TKey Key;
            public TValue Value;

            public Entry(TKey key, TValue value, byte psl)
            {
                Key = key;
                Value = value;
                Psl = psl;
            }
        }

        #endregion
    }
}

#nullable disable