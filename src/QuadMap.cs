using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Map.QuadMap
{
    /// <summary>
    /// This hashmap uses the following
    /// Open addressing   
    /// Quadratic probing
    /// Fibonacci hashing
    /// Default loadfactor is 0.5
    /// </summary>
    public class QuadMap<TKey, TValue>
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
                //iterate backwards so we can remove the item
                for (int i = _entries.Length - 1; i >= 0; --i)
                {
                    var entry = _entries[i];
                    if (entry.Metadata >= 0)
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
                //iterate backwards so we can remove the jumpDistanceIndex item
                for (int i = _entries.Length - 1; i >= 0; --i)
                {
                    var entry = _entries[i];
                    if (entry.Metadata >= 0)
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
                    if (entry.Metadata >= 0)
                    {
                        yield return entry.Value;
                    }
                }
            }
        }

        #endregion

        #region Fields

        private const sbyte _emptyBucket = -126;
        private const sbyte _tombstone = -125;
        private const sbyte _bitmask = (1 << 7) - 1;
        private Entry[] _entries;
        private uint _length;
        private readonly double _loadFactor;
        private const uint _goldenRatio = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private readonly IEqualityComparer<TKey> _comparer;
        private double _maxLookupsBeforeResize;
        private uint _lengthMinusOne;
       
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        public QuadMap() : this(8, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public QuadMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public QuadMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public QuadMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            if (length < 8)
            {
                length = 8;
            }

            //default length is 8
            _length = NextPow2(length);
            _loadFactor = loadFactor;

            _maxLookupsBeforeResize = _length * loadFactor;
            _comparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _shift = _shift - BitOperations.Log2(_length);

            _entries = GC.AllocateUninitializedArray<Entry>((int)_length);
            _entries.AsSpan().Fill(new Entry(default, default, _emptyBucket));

            _lengthMinusOne = _length - 1;            
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
            if (Count > _maxLookupsBeforeResize)
            {
                Resize();
            }

            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = _goldenRatio * hashcode >> _shift;

            // Get 7 high bits
            long h2 = hashcode & _bitmask;
            byte jumpDistance = 0;

            do
            {
                //retrieve infobyte
                ref var entry = ref Find(_entries, index);

                //Empty spot, add entry
                if (entry.Metadata == _emptyBucket || entry.Metadata == _tombstone)
                {
                    entry.Key = key;
                    entry.Value = value;
                    entry.Metadata = Unsafe.As<long, sbyte>(ref h2);

                    ++Count;
                    return true;
                }

                //find duplicate entries
                if (h2 == entry.Metadata && _comparer.Equals(key, entry.Key))
                {
                    return false;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;
            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrUpdate(TKey key)
        {
            //Resize if loadfactor is reached
            if (Count >= _maxLookupsBeforeResize)
            {
                Resize();
            }

            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = _goldenRatio * hashcode >> _shift;

            // Get 7 high bits
            long h2 = hashcode & _bitmask;
            byte jumpDistance = 0;

            do
            {
                //retrieve infobyte
                ref var entry = ref Find(_entries, index);

                //Empty spot, add entry
                if (entry.Metadata == _emptyBucket || entry.Metadata == _tombstone)
                {
                    entry.Key = key;
                    entry.Metadata = Unsafe.As<long, sbyte>(ref h2);

                    ++Count;
                    return ref entry.Value;
                }

                //find duplicate entries
                if (h2 == entry.Metadata && _comparer.Equals(key, entry.Key))
                {
                    return ref entry.Value;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;
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
            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = _goldenRatio * hashcode >> _shift;

            long h2 = hashcode & _bitmask;

            uint jumpDistance = 0;

            while (true)
            {
                //retrieve entry
                ref var entry = ref Find(_entries, index);

                if (h2 == entry.Metadata && _comparer.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }
              
                //Empty spot
                if (entry.Metadata == _emptyBucket)
                {
                    value = default;
                    return false;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;
            }
        }

        /// <summary>
        ///Updates the value of a specific key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue value)
        {
            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * _goldenRatio >> _shift;
            //set initiale jump distance
            uint jumpDistance = 0;
            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            do
            {
                ref var entry = ref Find(_entries, index);

                if (h2 == entry.Metadata && _comparer.Equals(key, entry.Key))
                {
                    entry.Value = value;
                    return true;
                }

                //not found 
                if (entry.Metadata == _emptyBucket)
                {
                    return false;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;

            } while (true);
        }

        /// <summary>
        ///  Remove entry using a tombstone
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * _goldenRatio >> _shift;
            uint jumpDistance = 0;
            var h2 = hashcode & _bitmask;

            do
            {
                ref var entry = ref Find(_entries, index);

                if (h2 == entry.Metadata && _comparer.Equals(key, entry.Key))
                {
                    // Set tombstone
                    entry.Metadata = _tombstone;
                    --Count;
                    return true;
                }

                //not found 
                if (entry.Metadata == _emptyBucket)
                {
                    return false;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;
            } while (true);
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
            //Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * _goldenRatio >> _shift;
            uint jumpDistance = 0;
            var h2 = hashcode & _bitmask;

            do
            {
                ref var entry = ref Find(_entries, index);

                if (h2 == entry.Metadata && _comparer.Equals(key, entry.Key))
                {
                    return true;
                }

                //not found 
                if (entry.Metadata == _emptyBucket)
                {
                    return false;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;
            } while (true);
        }

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="denseMap">The map.</param>
        public void Copy(QuadMap<TKey, TValue> denseMap)
        {
            for (var i = 0; i < denseMap._entries.Length; ++i)
            {
                var entry = denseMap._entries[i];
                if (entry.Metadata < 0)
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
            _entries.AsSpan().Fill(new Entry(default, default, _emptyBucket));
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Emplaces a new entry without checking for key existence. Keys have already been checked and are unique
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="current">The current.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmplaceInternal(TKey key, TValue value)
        {
            var hashcode = (uint)key.GetHashCode();

            uint index = hashcode * _goldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            byte jumpDistance = 0;

            do
            {
                //retrieve infobyte
                ref var entry = ref Find(_entries, index);

                //Empty spot, add entry
                if (entry.Metadata == _emptyBucket)
                {
                    entry.Metadata = Unsafe.As<long, sbyte>(ref h2);
                    entry.Key = key;
                    entry.Value = value;
                    return;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & _lengthMinusOne;

            } while (true);
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _length = _length << 1;

            _maxLookupsBeforeResize = _length * _loadFactor;
            _lengthMinusOne = _length - 1;

            var oldEntries = _entries;

            var size = Unsafe.As<uint, int>(ref _length);

            _entries = GC.AllocateUninitializedArray<Entry>(size);
            _entries.AsSpan().Fill(new Entry(default, default, _emptyBucket));

            for (uint i = 0; i < oldEntries.Length; ++i)
            {
                ref var entry = ref Find(oldEntries, i);
                if (entry.Metadata < 0)
                {
                    continue;
                }

                EmplaceInternal(entry.Key, entry.Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, uint index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
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

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        internal struct Entry
        {
            public sbyte Metadata;
            public TKey Key;
            public TValue Value;

            public Entry(TKey key, TValue value, sbyte metadata)
            {
                Key = key;
                Value = value;
                Metadata = metadata;
            }
        }
    }
}