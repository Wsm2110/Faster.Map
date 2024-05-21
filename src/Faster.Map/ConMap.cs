using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;

namespace Faster.Map.ConMap
{
    /// <summary>
    /// This hashmap uses the following
    /// - open-addressing
    /// - Quadratic probing 
    /// - Loadfactor by default is 0.9 while maintaining an incredible speed
    /// - Fibonacci hashing
    /// - Searches in parallel using SIMD
    /// - First-come-first-serve collision resolution    
    /// - Tombstones to avoid backshifts
    /// </summary>
    public class ConMap<TKey, TValue>
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public int Count { get => _count; set => _count = value; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        //public uint Size => (uint)_entries.Length;

        /// <summary>
        /// Returns all the entries as KeyValuePair objects
        /// </summary>
        /// <value>
        /// The entries.
        /// </value>
        //public IEnumerable<KeyValuePair<TKey, TValue>> Entries
        //{
        //    get
        //    {
        //        //iterate backwards so we can remove the item
        //        for (int i = _metadata.Length - 1; i >= 0; --i)
        //        {
        //            if (_metadata[i] >= 0)
        //            {
        //                var entry = _entries[i];
        //                yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// Returns all keys
        ///// </summary>
        ///// <value>
        ///// The keys.
        ///// </value>
        //public IEnumerable<TKey> Keys
        //{
        //    get
        //    {
        //        //iterate backwards so we can remove the jumpDistanceIndex item
        //        for (int i = _metadata.Length - 1; i >= 0; --i)
        //        {
        //            if (_metadata[i] >= 0)
        //            {
        //                yield return _entries[i].Key;
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// Returns all Values
        ///// </summary>
        ///// <value>
        ///// The keys.
        ///// </value>
        //public IEnumerable<TValue> Values
        //{
        //    get
        //    {
        //        for (int i = _metadata.Length - 1; i >= 0; --i)
        //        {
        //            if (_metadata[i] >= 0)
        //            {
        //                yield return _entries[i].Value;
        //            }
        //        }
        //    }
        //}

        #endregion

        #region Fields

        private const sbyte _emptyBucket = -127;
        private const sbyte _tombstone = -126;
        private static readonly Vector128<sbyte> _emptyBucketVector = Vector128.Create(_emptyBucket);
        private const uint _goldenRatio = 0x9E3779B9; //2654435769;
        private readonly double _loadFactor;
        private readonly IEqualityComparer<TKey> _comparer;
        private volatile Table _table;
        private int _count;

        uint[] _powersOfTwo = {
            0x1,       // 2^0
            0x2,       // 2^1
            0x4,       // 2^2
            0x8,       // 2^3
            0x10,      // 2^4
            0x20,      // 2^5
            0x40,      // 2^6
            0x80,      // 2^7
            0x100,     // 2^8
            0x200,     // 2^9
            0x400,     // 2^10
            0x800,     // 2^11
            0x1000,    // 2^12
            0x2000,    // 2^13
            0x4000,    // 2^14
            0x8000,    // 2^15
            0x10000,   // 2^16
            0x20000,   // 2^17
            0x40000,   // 2^18
            0x80000,   // 2^19
            0x100000,  // 2^20
            0x200000,  // 2^21
            0x400000,  // 2^22
            0x800000,  // 2^23
            0x1000000, // 2^24
            0x2000000, // 2^25
            0x4000000, // 2^26
            0x8000000, // 2^27
            0x10000000,// 2^28
            0x20000000,// 2^29
            0x40000000,// 2^30
            0x80000000 // 2^31
        };
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ConMap{TKey,TValue}"/> class.
        /// </summary>
        public ConMap() : this(16, 0.90, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.9d)</param>
        public ConMap(uint length) : this(length, 0.90, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.9d)</param>
        public ConMap(uint length, double loadFactor) : this(length, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.9d)</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public ConMap(uint length, double loadFactor, IEqualityComparer<TKey> keyComparer)
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                throw new NotSupportedException("Your hardware does not support acceleration for 128 bit vectors");
            }

            //default length is 16
            _loadFactor = loadFactor;
            _comparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _table = new Table(length, _loadFactor);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// Inserts a key and value in the hashmap
        ///
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>returns false if key already exists</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Emplace(TKey key, TValue value)
        {
            start:
            // Get object identity hashcode
            var table = _table;

            //Resize if loadfactor is reached
            if (_count >= table._maxLookupsBeforeResize)
            {
                Resize();
                goto start;
            }

            var hashcode = (uint)key.GetHashCode();
            var lockIndex = table.GetLockIndex(hashcode);

            Monitor.Enter(table._locks[lockIndex]);

            // GEt 7 low bits
            var h2 = H2(hashcode);
            //Create vector of the 7 low bits
            var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = table.GetBucketIndex(hashcode);
            //Set initial jumpdistance index
            uint jumpDistance = 0;

            while (true)
            {
                //Load vector @ index
                var source = Vector128.LoadUnsafe(ref Find(table._metadata, index));
                //Get a bit sequence for matched hashcodes (h2s)
                var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();
                //Check if key is unique
                while (mask != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    var entry = Find(table._entries, index + Unsafe.As<int, uint>(ref bitPos));

                    if (_comparer.Equals(entry.Key, key))
                    {
                        //duplicate key found
                        return false;
                    }

                    //clear bit
                    mask = ResetLowestSetBit(mask);
                }

                mask = source.ExtractMostSignificantBits();
                //check for tombstones and empty entries 
                if (mask != 0)
                {
                    var BitPos = BitOperations.TrailingZeroCount(mask);
                    //calculate proper index
                    index += Unsafe.As<int, uint>(ref BitPos);

                    Find(table._metadata, index) = Unsafe.As<uint, sbyte>(ref h2);

                    //retrieve entry
                    ref var currentEntry = ref Find(table._entries, index);

                    //set key and value
                    currentEntry.Key = key;
                    currentEntry.Value = value;

                    _count++;

                    Monitor.Exit(table._locks[lockIndex]);

                    return true;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
                //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
                //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
                //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
                jumpDistance += 16;
                index += jumpDistance;
                index = index & table._length - 1;
            }
        }

        /// <summary>
        /// 
        /// Tries to emplace a key-value pair into the map
        ///
        /// If the map already contains this key, update the existing KeyValuePair
        ///
        /// * Example *
        ///
        /// var map = new DenseMapSIMD<uint, uint>(16, 0.5);
        ///
        /// map.AddOrUpdate(1, 50);
        /// map.AddOrUpdate(1, 60);
        ///
        /// var result = map.Get(1, out var result)
        ///
        /// Assert.AreEqual(60U, result)
        /// 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void AddOrUpdate(TKey key, TValue value)
        //{
        //    //Resize if loadfactor is reached
        //    if (Count > _maxLookupsBeforeResize)
        //    {
        //        Resize();
        //    }

        //    var hashcode = (uint)key.GetHashCode();
        //    // GEt 7 low bits
        //    var h2 = H2(hashcode);
        //    //Create vector of the 7 low bits
        //    var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        //    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
        //    uint index = (_goldenRatio * hashcode) >> _shift;
        //    //Set initial jumpdistance index
        //    uint jumpDistance = 0;

        //    while (true)
        //    {
        //        //load vector @ index
        //        var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
        //        //get a bit sequence for matched hashcodes (h2s)
        //        var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
        //        //Check if key is unique
        //        while (mask != 0)
        //        {
        //            var bitPos = BitOperations.TrailingZeroCount(mask);
        //            ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

        //            if (_comparer.Equals(entry.Key, key))
        //            {
        //                //Key found, update existing key
        //                entry.Value = value;
        //                return;
        //            }

        //            //clear bit
        //            mask = ResetLowestSetBit(mask);
        //        }

        //        mask = source.ExtractMostSignificantBits();
        //        //check for tombstones and empty entries 
        //        if (mask > 0)
        //        {
        //            var bitPos = BitOperations.TrailingZeroCount(mask);
        //            //calculate proper index
        //            index += Unsafe.As<int, uint>(ref bitPos);

        //            //retrieve entry
        //            ref var currentEntry = ref Find(_entries, index);

        //            //set key and value
        //            currentEntry.Key = key;
        //            currentEntry.Value = value;

        //            ref var metadata = ref Find(_metadata, index);

        //            // add h2 to metadata
        //            metadata = Unsafe.As<uint, sbyte>(ref h2);

        //            ++Count;
        //            return;
        //        }

        //        //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
        //        jumpDistance += 16;
        //        index += jumpDistance;
        //        index = index & _length - 1;
        //    }
        //}

        /// <summary>
        /// Tries to find the key in the map
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns false if the key is not found</returns>       
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool Get(TKey key, out TValue value)
        //{
        //    var hashcode = (uint)key.GetHashCode();
        //    // Get 7 low bits
        //    var h2 = H2(hashcode);
        //    // Create vector of the 7 low bits
        //    var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        //    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
        //    uint index = (_goldenRatio * hashcode) >> _shift;
        //    // Set initial jumpdistance index
        //    uint jumpDistance = 0;

        //    while (true)
        //    {
        //        //load vector @ index
        //        var source = Vector128.LoadUnsafe(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_metadata), index));
        //        //get a bit sequence for matched hashcodes (h2s)
        //        var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
        //        //Could be multiple bits which are set
        //        while (mask > 0)
        //        {
        //            //Retrieve offset 
        //            var bitPos = BitOperations.TrailingZeroCount(mask);
        //            //Get index and eq
        //            var entry = Find(_entries, index + Unsafe.As<int, byte>(ref bitPos));
        //            //Use EqualityComparer to find proper entry
        //            if (_comparer.Equals(entry.Key, key))
        //            {
        //                value = entry.Value;
        //                return true;
        //            }

        //            //clear bit
        //            mask = ResetLowestSetBit(mask);
        //        }

        //        //Contains empty buckets    
        //        if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() > 0)
        //        {
        //            value = default;
        //            return false;
        //        }

        //        //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
        //        jumpDistance += 16;
        //        index += jumpDistance;
        //        index = index & _lengthMinusOne;
        //    }
        //}

        /// <summary>
        /// Gets the value for the specified key, or, if the key is not present,
        /// adds an entry and returns the value by ref. This makes it possible to
        /// add or update a value in a single look up operation.
        ///
        /// Will only use one lookup instead of two
        ///
        /// * Example *
        ///
        /// var counterMap = new DenseMapSIMD<uint, uint>(16, 0.5);
        /// ref var counter = ref counterMap.GetOrAddValueRef(1);
        ///
        /// ++counter;
        /// 
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <returns>Reference to the new or existing value</returns>    
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public ref TValue GetOrUpdate(TKey key)
        //{
        //    //Resize if loadfactor is reached
        //    if (Count >= _maxLookupsBeforeResize)
        //    {
        //        Resize();
        //    }

        //    // Get object identity hashcode
        //    var hashcode = (uint)key.GetHashCode();
        //    // GEt 7 low bits
        //    var h2 = H2(hashcode);
        //    //Create vector of the 7 low bits
        //    var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        //    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
        //    uint index = (_goldenRatio * hashcode) >> _shift;
        //    uint jumpDistance = 0;

        //    while (true)
        //    {
        //        //load vector @ index
        //        var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
        //        //get a bit sequence for matched hashcodes (h2s)
        //        var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
        //        //Could be multiple bits which are set
        //        while (mask != 0)
        //        {
        //            //Retrieve offset 
        //            var bitPos = BitOperations.TrailingZeroCount(mask);

        //            //Get index and eq
        //            ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

        //            //Use EqualityComparer to find proper entry
        //            if (_comparer.Equals(entry.Key, key))
        //            {
        //                return ref entry.Value;
        //            }

        //            //clear bit
        //            mask = ResetLowestSetBit(mask);
        //        }

        //        mask = source.ExtractMostSignificantBits();
        //        //Empty entry, add key              
        //        if (mask > 0)
        //        {
        //            var bitPos = BitOperations.TrailingZeroCount(mask);
        //            //calculate proper index
        //            index += Unsafe.As<int, uint>(ref bitPos);

        //            //retrieve entry
        //            ref var currentEntry = ref Find(_entries, index);

        //            //set key and value
        //            currentEntry.Key = key;
        //            currentEntry.Value = default;

        //            ref var metadata = ref Find(_metadata, index);

        //            // add h2 to metadata
        //            metadata = Unsafe.As<uint, sbyte>(ref h2);

        //            ++Count;

        //            return ref currentEntry.Value;
        //        }

        //        //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
        //        jumpDistance += 16;
        //        index += jumpDistance;
        //        index = index & _lengthMinusOne;
        //    }
        //}

        /// <summary>
        /// Tries to find the key in the map and updates the value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns> returns if update succeeded or not</returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool Update(TKey key, TValue value)
        //{
        //    // Get object identity hashcode
        //    var hashcode = (uint)key.GetHashCode();
        //    // GEt 7 low bits
        //    var h2 = H2(hashcode);
        //    //Create vector of the 7 low bits
        //    var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        //    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
        //    uint index = (_goldenRatio * hashcode) >> _shift;
        //    //Set initial jumpdistance index
        //    uint jumpDistance = 0;

        //    while (true)
        //    {
        //        //load vector @ index
        //        var source = Vector128.LoadUnsafe(ref Find(_metadata, index));

        //        //get a bit sequence for matched hashcodes (h2s)
        //        var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();

        //        //Could be multiple bits which are set
        //        while (mask != 0)
        //        {
        //            //retrieve offset 
        //            var bitPos = BitOperations.TrailingZeroCount(mask);

        //            //get index and eq
        //            ref var entry = ref Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

        //            if (_comparer.Equals(entry.Key, key))
        //            {
        //                entry.Value = value;
        //                return true;
        //            }

        //            //clear bit
        //            mask = ResetLowestSetBit(mask);
        //        }

        //        //get a bit sequence for matched empty buckets                
        //        if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() > 0)
        //        {
        //            //contains empty buckets - break;
        //            return false;
        //        }

        //        //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.

        //        jumpDistance += 16;
        //        index += jumpDistance;
        //        index = index & _length - 1;
        //    }

        //}

        /// <summary>
        /// Removes a key and value from the map
        /// </summary>
        /// <param name="key"></param>
        /// <returns> returns if the removal succeeded </returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool Remove(TKey key)
        //{
        //    // Get object identity hashcode
        //    var hashcode = (uint)key.GetHashCode();
        //    // GEt 7 low bits
        //    var h2 = H2(hashcode);
        //    //Create vector of the 7 low bits
        //    var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        //    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
        //    uint index = (_goldenRatio * hashcode) >> _shift;
        //    //Set initial jumpdistance index
        //    uint jumpDistance = 0;

        //    while (true)
        //    {
        //        //load vector @ index
        //        var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
        //        //get a bit sequence for matched hashcodes (h2s)
        //        var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
        //        //Could be multiple bits which are set
        //        while (mask != 0)
        //        {
        //            //retrieve offset 
        //            var bitPos = BitOperations.TrailingZeroCount(mask);

        //            if (_comparer.Equals(Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)).Key, key))
        //            {
        //                Find(_metadata, index + Unsafe.As<int, uint>(ref bitPos)) = _tombstone;
        //                --Count;
        //                return true;
        //            }

        //            //clear bit
        //            mask = ResetLowestSetBit(mask);
        //        }

        //        //find an empty spot, which means the key is not found             
        //        if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() != 0)
        //        {
        //            //contains empty buckets - break;
        //            return false;
        //        }

        //        //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
        //        jumpDistance += 16;
        //        index += jumpDistance;
        //        index = index & _lengthMinusOne;
        //    }
        //}

        /// <summary>
        /// determines if hashmap contains key x
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns> returns if a key is found </returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool Contains(TKey key)
        //{
        //    // Get object identity hashcode
        //    var hashcode = (uint)key.GetHashCode();
        //    // GEt 7 low bits
        //    var h2 = H2(hashcode);
        //    //Create vector of the 7 low bits
        //    var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
        //    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
        //    uint index = (_goldenRatio * hashcode) >> _shift;
        //    //Set initial jumpdistance index
        //    uint jumpDistance = 0;

        //    while (true)
        //    {
        //        //load vector @ index
        //        var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
        //        //get a bit sequence for matched hashcodes (h2s)
        //        var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
        //        //Could be multiple bits which are set
        //        while (mask != 0)
        //        {
        //            //retrieve offset 
        //            var bitPos = BitOperations.TrailingZeroCount(mask);
        //            if (_comparer.Equals(Find(_entries, index + Unsafe.As<int, uint>(ref bitPos)).Key, key))
        //            {
        //                return true;
        //            }

        //            //clear bit
        //            mask = ResetLowestSetBit(mask);
        //        }

        //        if (Vector128.Equals(_emptyBucketVector, source).ExtractMostSignificantBits() != 0)
        //        {
        //            //contains empty buckets - break;  
        //            return false;
        //        }

        //        //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
        //        //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
        //        //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
        //        //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
        //        //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
        //        jumpDistance += 16;
        //        index += jumpDistance;
        //        index = index & _lengthMinusOne;
        //    }
        //}

        /// <summary>
        /// Copies entries from one map to another
        /// </summary>
        /// <param name="denseMap">The map.</param>
        //public void Copy(ConMap<TKey, TValue> denseMap)
        //{
        //    for (var i = 0; i < denseMap._entries.Length; ++i)
        //    {
        //        if (denseMap._metadata[i] < 0)
        //        {
        //            continue;
        //        }

        //        var entry = denseMap._entries[i];
        //        Emplace(entry.Key, entry.Value);
        //    }
        //}

        /// <summary>
        /// Removes all entries from this map and sets the count to 0
        /// </summary>
        //public void Clear()
        //{
        //    Array.Clear(_entries);
        //    Array.Fill(_metadata, _emptyBucket);

        //    Count = 0;
        //}

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
        //public TValue this[TKey key]
        //{
        //    get
        //    {
        //        if (Get(key, out var result))
        //        {
        //            return result;
        //        }

        //        throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        //    }
        //    set
        //    {
        //        if (!Update(key, value))
        //        {
        //            throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
        //        }
        //    }
        //}

        #endregion

        #region Private Methods

        private volatile Table _migrationTable;

        /// <summary>
        /// Resizes this instance.
        /// </summary>     
        private void Resize()
        {
            var table = _table;
            var length = table._length;
            var index = BitOperations.Log2(length);

            // Interlocked.CompareExchange is used to ensure that the resize operation initializes only once.
            // This operation is atomic and ensures that only one thread can set _powersOfTwo[index] from length to 0 at a time, which effectively controls the initialization of the new migration table.
            if (Interlocked.CompareExchange(ref _powersOfTwo[index], 0, length) == length)
            {
                System.Diagnostics.Debug.WriteLine($"length {length}");

                // Create new snapshot using the metadata, entries array
                var migrationTable = new Table(length * 2, _loadFactor);
                //Interlocked.Exchange safely publishes the migrationTable to _migrationTable, ensuring visibility to other threads, which is crucial for the correctness of the migration.
                Interlocked.Exchange(ref _migrationTable, migrationTable);
            }

            var ctable = _migrationTable;

            // There could be a scenario where ctable is null when accessed. The check if (ctable == null) is vital and must be retained to ensure thread safety.
            // This can only happen when threads are racing. one allocating and the others dont
            if (ctable == null)
            {
                return;
            }

            if (table != _table)
            {
                return;
            }

            int locksAcquired = 0;
            try
            {

                // The thread that first obtains _locks[0] will be the one doing the resize operation
                table.AcquireFirstLock(ref locksAcquired);

                if (table != _table)
                {
                    return;
                }

                table.AcquirePostFirstLock(ref locksAcquired);

                table.Migrate(ctable, ref locksAcquired);

                Interlocked.CompareExchange(ref _table, ctable, table);
            }
            finally
            {
                Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId} release all locks");

                table.ReleaseLocks(locksAcquired);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, uint index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        }

        /// <summary>
        /// Retrieve 7 low bits from hashcode
        /// </summary>
        /// <param name="hashcode"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint H2(uint hashcode) => hashcode & 0b01111111;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref sbyte Find(sbyte[] array, uint index)
        {
            return ref array[index];
            //ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            //return ref Unsafe.Add(ref arr0, index);
        }

        /// <summary>
        /// Reset the lowest significant bit in the given value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ResetLowestSetBit(uint value)
        {
            // It's lowered to BLSR on x86
            return value & value - 1;
        }

        #endregion

        private sealed class Table
        {
            internal readonly sbyte[] _metadata;
            /// <summary>A singly-linked list for each bucket.</summary>
            internal readonly Entry[] _entries;
            /// <summary>A set of locks, each guarding a section of the table.</summary>
            internal readonly object[] _locks;
            /// <summary>
            /// 
            /// </summary>
            private readonly uint _concurrencyLevel;

            internal byte _shift = 32;
            internal uint _lengthMinusOne;
            internal uint _maxLookupsBeforeResize;
            internal uint _length;

            internal Table(uint length, double loadfactor)
            {
                _shift = (byte)(32 - BitOperations.Log2(length));
                _lengthMinusOne = length - 1;
                _maxLookupsBeforeResize = (uint)(length * loadfactor);

                _length = length;
                var size = length + 16;

                _concurrencyLevel = size / 16;

                _locks = new object[_concurrencyLevel];
                _locks.AsSpan().Fill(new object());

                _metadata = GC.AllocateArray<sbyte>((int)size);
                _entries = GC.AllocateArray<Entry>((int)size);
                _metadata.AsSpan().Fill(_emptyBucket);
            }

            internal void Migrate(Table next, ref int locksAquired)
            {
                for (uint i = 0; i < _metadata.Length; ++i)
                {
                    var h2 = Find(_metadata, i);
                    if (h2 < 0)
                    {
                        continue;
                    }

                    var entry = Find(_entries, i);

                    // Get object identity hashcode
                    var hashcode = (uint)entry.Key.GetHashCode();
                    // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
                    uint index = (_goldenRatio * hashcode) >> next._shift;
                    //Set initial jumpdistance index
                    uint jumpDistance = 0;

                    while (true)
                    {
                        //check for empty entries
                        var mask = Vector128.LoadUnsafe(ref Find(next._metadata, index)).ExtractMostSignificantBits();
                        if (mask != 0)
                        {
                            var BitPos = BitOperations.TrailingZeroCount(mask);

                            index += Unsafe.As<int, uint>(ref BitPos);

                            Find(next._metadata, index) = h2;
                            Find(next._entries, index) = entry;
                            break;
                        }

                        jumpDistance += 16;
                        index += jumpDistance;
                        index &= next._lengthMinusOne;
                    }
                }

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal uint GetBucketIndex(uint hashcode)
            {
                return (_goldenRatio * hashcode) >> _shift;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal uint GetLockIndex(uint hashcode)
            {
                return hashcode & _concurrencyLevel - 1;
            }

            /// <summary>
            /// Acquires all locks for this hash table, and increments locksAcquired by the number
            /// of locks that were successfully acquired. The locks are acquired in an increasing
            /// order.
            /// </summary>
            internal void AcquireAllLocks(ref int locksAcquired)
            {
                // First, acquire lock 0, then acquire the rest. _tables won't change after acquiring lock 0.
                AcquireFirstLock(ref locksAcquired);
                AcquirePostFirstLock(ref locksAcquired);
                Debug.Assert(locksAcquired == _locks.Length);
            }

            /// <summary>Acquires the first lock.</summary>
            /// <param name="locksAcquired">The number of locks acquired. It should be 0 on entry and 1 on exit.</param>
            /// <remarks>
            /// Once the caller owns the lock on lock 0, _tables._locks will not change (i.e., grow),
            /// so a caller can safely snap _tables._locks to read the remaining locks. When the locks array grows,
            /// even though the array object itself changes, the locks from the previous array are kept.
            /// </remarks>
            internal void AcquireFirstLock(ref int locksAcquired)
            {
                Monitor.Enter(_locks[0]);

                locksAcquired = 1;
            }

            /// <summary>Acquires all of the locks after the first, which must already be acquired.</summary>
            /// <param name="tables">The tables snapped after the first lock was acquired.</param>
            /// <param name="locksAcquired">
            /// The number of locks acquired, which should be 1 on entry.  It's incremented as locks
            /// are taken so that the caller can reliably release those locks in a finally in case
            /// of exception.
            /// </param>
            internal void AcquirePostFirstLock(ref int locksAcquired)
            {
                Debug.Assert(Monitor.IsEntered(_locks[0]));
                Debug.Assert(locksAcquired == 1);

                for (int i = 1; i < _locks.Length; i++)
                {
                    Monitor.Enter(_locks[i]);
                    locksAcquired++;
                }

                Debug.Assert(locksAcquired == _locks.Length);
            }

            /// <summary>Releases all of the locks up to the specified number acquired.</summary>
            /// <param name="locksAcquired">The number of locks acquired.  All lock numbers in the range [0, locksAcquired) will be released.</param>
            internal void ReleaseLocks(int locksAcquired)
            {
                for (int i = 0; i < locksAcquired; i++)
                {
                    Monitor.Exit(_locks[i]);
                }

                Debug.Assert(locksAcquired >= 0);
            }

        }

        public struct Entry
        {
            public TKey Key;
            public TValue Value;
        }
    }
}