using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Map.Core;

namespace Faster.Map
{
    /// <summary>
    /// This hashmap uses the following
    /// Open addressing   
    /// Quadratic probing
    /// Fibonacci hashing
    /// Default loadfactor is 0.5
    /// </summary>
    public class DenseMap<TKey, TValue>
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
                for (int i = _metadata.Length - 1; i >= 0; --i)
                {
                    if (_metadata[i] >= 0)
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
                //iterate backwards so we can remove the jumpDistanceIndex item
                for (int i = _metadata.Length - 1; i >= 0; --i)
                {
                    if (_metadata[i] >= 0)
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
                for (int i = _metadata.Length - 1; i >= 0; --i)
                {
                    if (_metadata[i] >= 0)
                    {
                        yield return _entries[i].Value;
                    }
                }
            }
        }

        #endregion

        #region Fields

        private sbyte[] _metadata;
        private const sbyte _emptyBucket = -126;
        private const sbyte _tombstone = -125;

        private const sbyte _bitmask = (1 << 7) - 1;
        private Entry<TKey, TValue>[] _entries;
        private uint _length;
        private readonly double _loadFactor;
        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private int _shift = 32;
        private readonly IEqualityComparer<TKey> _comparer;
        private double _maxLookupsBeforeResize;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        public DenseMap() : this(8, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public DenseMap(uint length) : this(length, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public DenseMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public DenseMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            //default length is 8
            _length = length;
            _loadFactor = loadFactor;

            if (_length < 8)
            {
                _length = 8;
            }

            if (BitOperations.IsPow2(length))
            {
                _length = length;
            }
            else
            {
                _length = BitOperations.RoundUpToPowerOf2(_length);
            }

            _maxLookupsBeforeResize = (_length * loadFactor);
            _comparer = keyComparer ?? EqualityComparer<TKey>.Default;

            _shift = _shift - BitOperations.Log2(_length);
            _entries = new Entry<TKey, TValue>[_length];
            _metadata = new sbyte[_length];

            _metadata.AsSpan().Fill(_emptyBucket);
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
            uint index = hashcode * GoldenRatio >> _shift;

            uint jumpDistance = 1;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            do
            {
                //retrieve infobyte
                ref var metadata = ref _metadata[index];

                //Empty spot, add entry
                if (metadata == _emptyBucket || metadata == _tombstone)
                {
                    _entries[index].Key = key;
                    _entries[index].Value = value;

                    metadata = Unsafe.As<long, sbyte>(ref h2);

                    ++Count;
                    return true;
                }

                //validate hash
                if (h2 == metadata && _comparer.Equals(key, _entries[index].Key))
                {
                    return false;
                }

                //Probing is done by incrementing the current bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
                //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
                //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
                //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

                index += jumpDistance;

                if (index >= _length)
                {
                    // adding jumpdistance to the index will prevent endless loops.
                    // Every time this code block is entered jumpdistance will be different hence the index will be different too
                    // thus it will always look for an empty spot
                    index = BitOperations.RotateRight(hashcode, 31) + jumpDistance >> _shift;
                }

                jumpDistance += 1;

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
            uint index = hashcode * GoldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            uint jumpDistance = 1;

            do
            {
                //retrieve infobyte
                var metadata = GetArrayVal(_metadata, index);

                //Empty spot, add entry
                if (metadata == _emptyBucket)
                {
                    value = default;
                    return false;
                }

                var entry = GetArrayVal(_entries, index);

                if (h2 == metadata && _comparer.Equals(key, entry.Key))
                {
                    value = entry.Value;
                    return true;
                }

                index += jumpDistance;

                if (index >= _length)
                {
                    // adding jumpdistance to the index will prevent endless loops.
                    // Every time this code block is entered jumpdistance will be different hence the index will be different too
                    // thus it will always look for an empty spot
                    index = BitOperations.RotateRight(hashcode, 31) + jumpDistance >> _shift;
                }

                jumpDistance += 1;

            } while (true);
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
            uint index = hashcode * GoldenRatio >> _shift;
            //set initiale jump distance
            uint jumpDistance = 1;
            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            do
            {
                //retrieve infobyte
                var metadata = GetArrayVal(_metadata, index);

                //Empty spot, add entry
                if (metadata == _emptyBucket)
                {
                    return false;
                }

                ref var entry = ref GetArrayValRef(_entries, index);
                if (metadata == h2 && _comparer.Equals(key, entry.Key))
                {
                    entry.Value = value;
                    return true;
                }

                index += jumpDistance;

                if (index >= _length)
                {
                    // adding jumpdistance to the index will prevent endless loops.
                    // Every time this code block is entered jumpdistance will be different hence the index will be different too
                    // thus it will always look for an empty spot
                    index = BitOperations.RotateRight(hashcode, 31) + jumpDistance >> _shift;
                }

                jumpDistance += 1;

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
            uint index = hashcode * GoldenRatio >> _shift;

            uint jumpDistance = 1;

            var h2 = hashcode & _bitmask;

            do
            {
                //retrieve infobyte
                ref var metadata = ref GetArrayValRef(_metadata, index);

                //Empty spot, add entry
                if (metadata == _emptyBucket)
                {
                    return false;
                }

                var entry = GetArrayVal(_entries, index);
                if (h2 == metadata && _comparer.Equals(key, entry.Key))
                {
                    // Set tombstone
                    metadata = _tombstone;
                    --Count;
                    return true;
                }

                index += jumpDistance;

                if (index >= _length)
                {
                    // adding jumpdistance to the index will prevent endless loops.
                    // Every time this code block is entered jumpdistance will be different hence the index will be different too
                    // thus it will always look for an empty spot
                    index = BitOperations.RotateRight(hashcode, 31) + jumpDistance >> _shift;
                }

                jumpDistance += 1;

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
            uint index = hashcode * GoldenRatio >> _shift;

            uint jumpDistance = 1;

            var h2 = hashcode & _bitmask;

            do
            {
                //retrieve h2
                var metadata = GetArrayVal(_metadata, index);

                //Empty spot
                if (metadata == _emptyBucket)
                {
                    return false;
                }

                var entry = GetArrayVal(_entries, index);

                if (h2 == metadata && _comparer.Equals(key, entry.Key))
                {
                    return true;
                }

                index += jumpDistance;

                if (index >= _length)
                {
                    // adding jumpdistance to the index will prevent endless loops.
                    // Every time this code block is entered jumpdistance will be different hence the index will be different too
                    // thus it will always look for an empty spot
                    index = BitOperations.RotateRight(hashcode, 31) + jumpDistance >> _shift;
                }

                jumpDistance += 1;

            } while (true);
        }

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="denseMap">The map.</param>
        public void Copy(DenseMap<TKey, TValue> denseMap)
        {
            for (var i = 0; i < denseMap._entries.Length; ++i)
            {
                var info = denseMap._metadata[i];
                if (info < 0)
                {
                    continue;
                }

                var entry = denseMap._entries[i];
                Emplace(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_entries);
            Array.Fill(_metadata, _emptyBucket);

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
        private void EmplaceInternal(ref Entry<TKey, TValue> entry)
        {
            var hashcode = (uint)entry.Key.GetHashCode();

            uint index = hashcode * GoldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            uint jumpDistance = 1;

            do
            {
                //retrieve infobyte
                ref var metadata = ref _metadata[index];

                //Empty spot, add entry
                if (metadata == _emptyBucket)
                {
                    metadata = Unsafe.As<long, sbyte>(ref h2);
                    _entries[index] = entry;
                    return;
                }

                index += jumpDistance;

                if (index >= _length)
                {
                    // hashing to the top region of this hashmap always had some drawbacks
                    // even when the table was half full the table would resize when the last 16 slots were full
                    // and the jumpdistance exceeded the length of the array. this is not intended
                    // 
                    // when the index exceeds the length, which means all groups of 16 near the upper region of the map are full
                    // reset the index and try probing again from the start this will enforce a secure and trustable hashmap which will always
                    // resize when we reach a 90% load
                    // Note these entries will not be properly cache alligned but in the end its well worth it
                    //
                    // adding jumpdistance to the index will prevent endless loops.
                    // Every time this code block is entered jumpdistance will be different hence the index will be different too
                    // thus it will always look for an empty spot
                    index = BitOperations.RotateRight(hashcode, 31) + jumpDistance >> _shift;
                }

                ++jumpDistance;

            } while (true);
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _length = _length * 2;

            _maxLookupsBeforeResize = _length * _loadFactor;

            var oldEntries = _entries;
            var oldInfo = _metadata;

            var size = Unsafe.As<uint, int>(ref _length);

            _metadata = GC.AllocateUninitializedArray<sbyte>(size);
            _entries = GC.AllocateUninitializedArray<Entry<TKey, TValue>>(size);

            _metadata.AsSpan().Fill(_emptyBucket);

            for (var i = 0; i < oldEntries.Length; ++i)
            {
                ref var m = ref oldInfo[i];
                if (m < 0)
                {
                    continue;
                }

                EmplaceInternal(ref oldEntries[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetArrayVal<T>(T[] array, uint index)
        {
#if DEBUG
            return array[index];
#else
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return Unsafe.Add(ref arr0, index);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T GetArrayValRef<T>(T[] array, uint index)
        {
#if DEBUG
            return ref array[index];
#else
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
#endif
        }

        #endregion
    }
}