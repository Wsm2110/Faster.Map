using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Faster.Map.Core;
using System.Runtime.InteropServices;

namespace Faster.Map
{
    /// <summary>
    /// This hashmap uses the following
    /// - Open addressing
    /// - Quadratic probing 
    /// - loadfactor by default is 0.9 while maintaining an incredible speed
    /// - fibonacci hashing
    /// </summary>
    public class DenseMapSIMD<TKey, TValue>
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

        //Used to backout early while finding entries which are not in the map
        private int _maxDistance = 0;

        private const sbyte _emptyBucket = -127;
        private const sbyte _tombstone = -126;

        private static readonly Vector128<sbyte> _emptyBucketVector = Vector128.Create(_emptyBucket);
        private static readonly Vector128<sbyte> _emplaceBucketVector = Vector128.Create((sbyte)-125);

        private sbyte[] _metadata;
        private Entry<TKey, TValue>[] _entries;

        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private uint _length;

        private int _shift = 32;
        private double _maxLookupsBeforeResize;
        private readonly double _loadFactor;
        private readonly IEqualityComparer<TKey> _comparer;
        private const sbyte _bitmask = (1 << 7) - 1;
        private const byte num_jump_distances = 31;

        //Probing is done by incrementing the current bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
        private static ushort[] jump_distances = new ushort[num_jump_distances]
        {
           //    3,   6,  10, 15, 21, 28, 36, 45, 55, 66, 78, 91, 105, 120, 136, 153, 171, 190, 210, 231,
           //  253, 276, 300, 325, 351, 378, 406, 435, 465, 496, 528, 561, 595, 630,
        
           // results in
           // * 16 - 16 entrypoint

          32, 80, 144, 240, 320, 432, 560, 704, 864, 1040, 1232, 1440, 1664, 1905, 2160, 2432,
          2720, 3344, 3680, 4032, 4400, 4784, 5184, 5600, 6032, 6480, 6944, 7424, 7920, 8432, 8960,
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMapSIMD{TKey,TValue}"/> class.
        /// </summary>
        public DenseMapSIMD() : this(16, 0.90, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMapSIMD{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        public DenseMapSIMD(uint length) : this(length, 0.90, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DenseMapSIMD{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.9d)</param>
        public DenseMapSIMD(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.9d)</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public DenseMapSIMD(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            if (!Sse2.IsSupported)
            {
                throw new NotSupportedException("Simd SSe2 is not supported");
            }

            //default length is 16
            _length = length;
            _loadFactor = loadFactor;

            if (loadFactor > 0.9)
            {
                _loadFactor = 0.9;
            }

            if (BitOperations.IsPow2(length))
            {
                _length = length;
            }
            else
            {
                _length = BitOperations.RoundUpToPowerOf2(_length);
            }

            _maxLookupsBeforeResize = (uint)(_length * _loadFactor);
            _comparer = keyComparer ?? EqualityComparer<TKey>.Default;

            _shift = _shift - BitOperations.Log2(_length);
            _entries = new Entry<TKey, TValue>[_length + 16];
            _metadata = new sbyte[_length + 16];

            //fill metadata with emptybucket info
            Array.Fill(_metadata, _emptyBucket);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Insert a key and value in the hashmap
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>returns false if key already exists</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Emplace(TKey key, TValue value)
        {
            //Resize if loadfactor is reached
            if (Count > _maxLookupsBeforeResize)
            {
                Resize();
            }

            // Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            //Create vector of the 7 high bits
            var left = Vector128.Create(Unsafe.As<int, sbyte>(ref h2));

            start:

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Set initial jumpdistance index
            byte jumpDistanceIndex = 0;

            do
            {
                //load vector @ index
                var right = Vector128.LoadUnsafe(ref GetArrayValRef(_metadata, index));

                //compare vectors
                var comparison = Sse2.CompareEqual(left, right);

                //convert to int bitarray
                int result = Sse2.MoveMask(comparison);

                //Check if key is unique
                while (result != 0)
                {
                    var offset = BitOperations.TrailingZeroCount(result);
                    if (_comparer.Equals(_entries[index + offset].Key, key))
                    {
                        //duplicate key found
                        return false;
                    }

                    //clear bit
                    result &= ~(1 << offset);
                }

                //check for tombstones and empty entries
                result = Sse2.MoveMask(right);

                if (result != 0)
                {
                    var offset = BitOperations.TrailingZeroCount(result);
                    //calculate proper index
                    index += Unsafe.As<int, uint>(ref offset);

                    //retrieve entry
                    ref var current = ref _entries[index];

                    //set key and value
                    current.Key = key;
                    current.Value = value;

                    // add h2 to metadata
                    _metadata[index] = Unsafe.As<int, sbyte>(ref h2);
                    ++Count;
                    return true;
                }

                //Calculate jumpDistance
                index += GetArrayVal(jump_distances, jumpDistanceIndex);

                if (index > _length)
                {    
                    //reset index to 0 and start probing again forcing a 90% loadfactor                    
                    index = 0;
                    jumpDistanceIndex = 0;
                    continue;
                }

                ++jumpDistanceIndex;

                // Keep track of the max jump distance used to find an empty slot
                if (jumpDistanceIndex > _maxDistance)
                {
                    _maxDistance = jumpDistanceIndex;
                }

            } while (jumpDistanceIndex < num_jump_distances);

            //All jumpdistances are used -> resize since we couldnt find an empty slot
            Resize();
            //go to start and try again
            goto start;
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
            // Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            //Create vector of the 7 high bits
            var left = Vector128.Create(Unsafe.As<int, sbyte>(ref h2));

            //Set initial jumpdistance index
            byte jumpDistanceIndex = 0;

            do
            {
                //load vector @ index
                var right = Vector128.LoadUnsafe(ref GetArrayValRef(_metadata, index));

                //compare two vectors
                var comparison = Sse2.CompareEqual(left, right);

                //get result
                int result = Sse2.MoveMask(comparison);

                //Could be multiple bits which are set
                while (result != 0)
                {
                    //Retrieve offset 
                    var offset = BitOperations.TrailingZeroCount(result);

                    //Get index and eq
                    var entry = GetArrayVal(_entries, index + Unsafe.As<int, uint>(ref offset));

                    //Use EqualityComparer to find proper entry
                    if (_comparer.Equals(entry.Key, key))
                    {
                        value = entry.Value;
                        return true;
                    }

                    //clear bit
                    result &= ~(1 << offset);
                }

                result = Sse2.MoveMask(Sse2.CompareEqual(_emptyBucketVector, right));

                //Contains empty buckets;    
                if (result != 0)
                {
                    break;
                }

                //Calculate jumpDistance
                index += GetArrayVal(jump_distances, jumpDistanceIndex);

                if (index > _length)
                {    
                    index = 0;
                    jumpDistanceIndex = 0;
                    continue;
                }

                ++jumpDistanceIndex;

            } while (jumpDistanceIndex <= _maxDistance);

            value = default;
            return false;
        }

        /// <summary>
        /// Updates the value of a specific key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns> returns if update succeeded or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue value)
        {
            // Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            //Create vector of the 7 high bits
            var left = Vector128.Create(Unsafe.As<int, sbyte>(ref h2));

            //Set initial jumpdistance index
            byte jumpDistanceIndex = 0;

            do
            {
                //load vector @ index
                var right = Vector128.LoadUnsafe(ref GetArrayValRef(_metadata, index));

                //compare two vectors
                var comparison = Sse2.CompareEqual(left, right);        

                //get result
                int result = Sse2.MoveMask(comparison);

                //Could be multiple bits which are set
                while (result != 0)
                {
                    //retrieve offset 
                    var offset = BitOperations.TrailingZeroCount(result);

                    //get index and eq
                    ref var entry = ref _entries[index + offset];

                    if (_comparer.Equals(entry.Key, key))
                    {
                        entry.Value = value;
                        return true;
                    }

                    //clear bit
                    result &= ~(1 << offset);
                }

                comparison = Sse2.CompareEqual(_emptyBucketVector, right);
                result = Sse2.MoveMask(comparison);

                if (result != 0)
                {
                    //contains empty buckets - break;
                    break;
                }

                //Calculate jumpDistance
                index += GetArrayVal(jump_distances, jumpDistanceIndex);

                if (index > _length)
                {
                    index = 0;
                    jumpDistanceIndex = 0;
                    continue;
                }

                ++jumpDistanceIndex;

            } while (jumpDistanceIndex <= _maxDistance);

            //entry not found
            return false;
        }

        /// <summary>
        /// Removes a key and value from the map
        /// </summary>
        /// <param name="key"></param>
        /// <returns> returns if the removal succeeded </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            // Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            //Create vector of the 7 high bits
            var left = Vector128.Create(Unsafe.As<int, sbyte>(ref h2));

            //Set initial jumpdistance index
            byte jumpDistanceIndex = 0;

            do
            {
                //load vector @ index
                var right = Vector128.LoadUnsafe(ref GetArrayValRef(_metadata, index));

                var comparison = Sse2.CompareEqual(left, right);

                //get result
                var result = Sse2.MoveMask(comparison);

                //Could be multiple bits which are set
                while (result != 0)
                {
                    //retrieve offset 
                    var offset = BitOperations.TrailingZeroCount(result);

                    ref var entry = ref _entries[index + offset];

                    if (_comparer.Equals(entry.Key, key))
                    {
                        entry = default;
                        _metadata[index + offset] = _tombstone;
                        --Count;
                        return true;
                    }

                    //clear bit
                    result &= ~(1 << offset);
                }

                //find an empty spot, which means the key is not found
                comparison = Sse2.CompareEqual(_emptyBucketVector, right);
                result = Sse2.MoveMask(comparison);

                if (result != 0)
                {
                    //contains empty buckets - break;
                    break;
                }

                //Calculate jumpDistance
                index += GetArrayVal(jump_distances, jumpDistanceIndex);

                if (index > _length)
                {
                    index = 0;
                    jumpDistanceIndex = 0;
                    continue;
                }

                ++jumpDistanceIndex;

            } while (jumpDistanceIndex <= _maxDistance);

            return false;
        }

        /// <summary>
        /// determines if hashmap contains key x
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns> returns if a key is found </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TKey key)
        {
            // Get object identity hashcode
            var hashcode = key.GetHashCode();

            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            // Get 7 high bits
            var h2 = hashcode & _bitmask;

            //Create vector of the 7 high bits
            var left = Vector128.Create(Unsafe.As<int, sbyte>(ref h2));

            //Set initial jumpdistance index
            byte jumpDistanceIndex = 0;

            do
            {
                //load vector @ index
                var right = Vector128.LoadUnsafe(ref GetArrayValRef(_metadata, index));

                //compare two vectors
                var comparison = Sse2.CompareEqual(left, right);

                //get result
                int result = Sse2.MoveMask(comparison);

                //Could be multiple bits which are set
                while (result != 0)
                {
                    //retrieve offset 
                    var offset = BitOperations.TrailingZeroCount(result);

                    //get index and eq
                    var entry = _entries[index + offset];

                    if (_comparer.Equals(entry.Key, key))
                    {
                        return true;
                    }

                    //clear bit
                    result &= ~(1 << offset);
                }

                result = Sse2.MoveMask(Sse2.CompareEqual(_emptyBucketVector, right));
                if (result != 0)
                {
                    //contains empty buckets - break;  
                    return false;
                }

                //Calculate jumpDistance
                index += GetArrayVal(jump_distances, jumpDistanceIndex);

                if (index > _length)
                {
                    index = 0;
                    jumpDistanceIndex = 0;
                    continue;
                }

                ++jumpDistanceIndex;

            } while (jumpDistanceIndex <= _maxDistance);

            return false;
        }

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="denseMap">The map.</param>
        public void Copy(DenseMapSIMD<TKey, TValue> denseMap)
        {
            for (var i = 0; i < denseMap._entries.Length; ++i)
            {
                if (denseMap._metadata[i] < 0)
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
        /// <param name="current">The jumpDistanceIndex.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmplaceInternal(Entry<TKey, TValue> entry, sbyte h2)
        {
            //expensive if hashcode is slow, or when it`s not cached like strings
            var hashcode = entry.Key.GetHashCode();

            //calculate index by using object identity * fibonaci followed by a shift
            uint index = (uint)hashcode * GoldenRatio >> _shift;

            //Set initial jumpdistance index
            byte jumpDistanceIndex = 0;

            do
            {
                //check for empty entries
                int result = Sse2.MoveMask(Vector128.LoadUnsafe(ref GetArrayValRef(_metadata, index)));
                if (result != 0)
                {
                    var offset = BitOperations.TrailingZeroCount(result);

                    index += Unsafe.As<int, uint>(ref offset);

                    _metadata[index] = h2;
                    _entries[index] = entry;
                    return;
                }

                //Calculate jumpDistance
                index += GetArrayVal(jump_distances, jumpDistanceIndex);

                ++jumpDistanceIndex;

                // Keep track of the max jump distance used to find an empty slot
                if (jumpDistanceIndex > _maxDistance)
                {
                    _maxDistance = jumpDistanceIndex;
                }

            } while (true);
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>     
        private void Resize()
        {
            _shift--;
            _maxDistance = 0;

            //next power of 2
            _length = _length * 2;
            _maxLookupsBeforeResize = _length * _loadFactor;

            var oldEntries = _entries;
            var oldMetadata = _metadata;

            var size = Unsafe.As<uint, int>(ref _length) + 16;

            _metadata = GC.AllocateUninitializedArray<sbyte>(size);
            _entries = GC.AllocateUninitializedArray<Entry<TKey, TValue>>(size);

            _metadata.AsSpan().Fill(_emptyBucket);

            for (var i = 0; i < oldEntries.Length; ++i)
            {
                var m = oldMetadata[i];
                if (m < 0)
                {
                    continue;
                }

                EmplaceInternal(oldEntries[i], m);
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