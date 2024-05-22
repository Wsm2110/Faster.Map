using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.Diagnostics;

namespace Faster.Map
{
    /// <summary>
    /// This hashmap uses the following
    /// Open addressing  
    /// Quadratic probing
    /// Fibonacci hashing
    /// Default loadfactor is 0.5
    /// </summary>
    public class CMap<TKey, TValue> where TKey : notnull
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public uint Count { get => (uint)_table._count; }

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
                var table = _table;

                for (int i = table.Metadata.Length - 1; i >= 0; i--)
                {
                    var meta = table.Metadata[i];
                    if (meta > -1)
                    {
                        var entry = table.Entries[i];
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
                for (int i = _table.Metadata.Length - 1; i >= 0; i--)
                {
                    var meta = _table.Metadata[i];
                    if (meta > -1)
                    {
                        yield return Find(_table.Entries, (uint)i).Key;
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
                //iterate backwards so we can remove the jumpDistanceIndex item
                for (int i = _table.Metadata.Length - 1; i >= 0; i--)
                {
                    var meta = Volatile.Read(ref _table.Metadata[i]);
                    if (meta > -1)
                    {
                        var entry = _table.Entries[i];
                        yield return entry.Value;
                    }
                }
            }
        }

        #endregion

        #region Fields

        internal volatile Table _migrationTable;
        internal volatile Table _table;
        private const sbyte _emptyBucket = -127;
        private const sbyte _tombstone = -126;
        private const sbyte _resizeBucket = -125;
        private const sbyte _inProgressMarker = -124;
        private double _loadFactor;

        private readonly IEqualityComparer<TKey> _comparer;

        volatile uint[] _powersOfTwo = {
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

#if DEBUG
        Table[] _migrationTables = new Table[31];
#endif

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CMap{TKey,TValue}"/> class.
        /// </summary>
        public CMap() : this(8, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="initialCapacity">The length of the hashmap. Will always take the closest power of two</param>
        public CMap(uint initialCapacity) : this(initialCapacity, 0.5d, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CMap{TKey,TValue}"/> class.
        /// </summary>
        /// <param name="initialCapacity">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        public CMap(uint initialCapacity, double loadFactor) : this(initialCapacity, loadFactor, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="initialCapacity">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public CMap(uint initialCapacity, double loadFactor, IEqualityComparer<TKey>? keyComparer)
        {
            if (initialCapacity <= 0 || loadFactor <= 0 || loadFactor > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity) + " or " + nameof(loadFactor));
            }

            if (initialCapacity < 16)
            {
                initialCapacity = 16;
            }
            _loadFactor = loadFactor;
            _comparer = EqualityComparer<TKey>.Default;
            _table = new Table(BitOperations.RoundUpToPowerOf2(initialCapacity), _loadFactor);
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
            var hashcode = (uint)key.GetHashCode();
            var h2 = _table.H2(hashcode);
            uint jumpDistance = 0;

            start:

            //Resize if threshold is reached        
            if (_table._count >= _table.Threshold)
            {
                Resize();
            }

            var table = _table;
            var index = table.GetBucket(hashcode);
         
            do
            {
                // Retrieve metadata
                ref var location = ref Find(table.Metadata, index);

                // Try to claim emptybucket
                if (_emptyBucket == location && _emptyBucket == Interlocked.CompareExchange(ref location, _inProgressMarker, _emptyBucket))
                {   
                    ref var entry = ref Find(table.Entries, index);

                    entry.Key = key;
                    entry.Value = value;

                    //set h2 metadata indicating adding has finished
                    Interlocked.Exchange(ref location, h2);
                    Interlocked.Increment(ref table._count);

                    if (!Unsafe.AreSame(ref _table, ref table))
                    {
                        //Resize happened, try again with new table
                        jumpDistance = 0;
                        goto start;
                    }

                    return true;
                }

                // Bucket is occupied, check if key matches
                if (h2 == location && _comparer.Equals(key, Find(table.Entries, index).Key))
                {
                    return false;
                }

                if (location == _resizeBucket)
                {
                    Resize(index);
                    jumpDistance = 0;
                    goto start;
                }

                // Retry due to collision or another thread claiming the bucket
                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne;
            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            var h2 = _table.H2(hashcode);
            uint jumpDistance = 0;

            start:

            var table = _table;
            var index = table.GetBucket(hashcode);         

            do
            {
                // Retrieve metadata
                var location = Find(table.Metadata, index);
                if (h2 == location && _comparer.Equals(key, Find(table.Entries, index).Key))
                {
                    value = Find(table.Entries, index).Value;
                    return true;
                }

                if (_emptyBucket == location)
                {
                    value = default;
                    return false;
                }

                if (location == _resizeBucket && Unsafe.AreSame(ref _table, ref table))
                {
                    Resize(index);
                    jumpDistance = 0;
                    goto start;
                }                          

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & table.LengthMinusOne;
            } while (true);
        }

        /// <summary>
        ///Updates the value of a specific key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue value)
        {
            var hashcode = (uint)key.GetHashCode();
            var h2 = _table.H2(hashcode);
            uint jumpDistance = 0;

            start:

            var table = _table;
            var index = table.GetBucket(hashcode);

            do
            {
                // Retrieve metadata
                var location = Find(table.Metadata, index);

                if (h2 == location && _comparer.Equals(key, Find(table.Entries, index).Key))
                {
                    // Claim bucket
                    if (h2 == Interlocked.CompareExchange(ref location, _inProgressMarker, _emptyBucket))
                    {
                        ref var entry = ref Find(table.Entries, index);
                        
                        entry.Value = value;

                        //set h2 metadata indicating adding has finished
                        Interlocked.Exchange(ref location, h2);
                   
                        if (!Unsafe.AreSame(ref _table, ref table))
                        {
                            //Resize happened, try again with new table
                            jumpDistance = 0;
                            goto start;
                        }

                        return true;
                    }                
                }

                if (_emptyBucket == location)
                {
                    value = default;
                    return false;
                }

                if (location == _resizeBucket && Unsafe.AreSame(ref _table, ref table))
                {
                    Resize(index);
                    jumpDistance = 0;
                    goto start;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                jumpDistance += 1;
                index += jumpDistance;
                index = index & table.LengthMinusOne;
            } while (true);
        }


        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _table.Clear();
            // Count = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method is designed to be used in a highly concurrent environment where minimizing locking and blocking is crucial.
        /// The use of lock-free programming techniques helps to maximize scalability and performance by allowing multiple threads to operate in parallel with minimal interference.
        /// </summary>
        public void Resize(uint mindex = 0)
        {
            // These lines read the current table and its properties.
            // The use of local variables here is thread - safe as they only capture the state at a specific point in time and do not modify shared state.
            var table = _table;
            var length = table.Length;
            var index = BitOperations.Log2(length);

            if (table._count < table.Threshold)
            {
                // resized
                return;
            }

            // Interlocked.CompareExchange is used to ensure that the resize operation initializes only once.
            // This operation is atomic and ensures that only one thread can set _powersOfTwo[index] from length to 0 at a time, which effectively controls the initialization of the new migration table.
            if (_powersOfTwo[index] > 0 && table._count > table.Threshold)
            {
                if (Interlocked.CompareExchange(ref _powersOfTwo[index], 0, length) == length)
                {
                    // Create new snapshot using the metadata, entries array
                    var migrationTable = new Table(length * 2, _loadFactor);
                    //Interlocked.Exchange safely publishes the migrationTable to _migrationTable, ensuring visibility to other threads, which is crucial for the correctness of the migration.
                    Interlocked.Exchange(ref _migrationTable, migrationTable);
                    // Debug purposes
#if DEBUG
                    Interlocked.Exchange(ref _migrationTables[index], migrationTable);
#endif
                }
            }

            var ctable = _migrationTable;

            // There could be a scenario where ctable is null when accessed. The check if (ctable == null) is vital and must be retained to ensure thread safety.
            // This can only happen when threads are racing. one allocating and the others dont
            if (ctable == null || table.Length == ctable.Length)
            {
                //waiting for a new allocated table
                return;
            }

            if (table != _table)
            {
                //already resized
                return;
            }

            if (table._completed == 1)
            {
                return;
            }

            table.Migrate(ctable, mindex);

            if (table != _table)
            {
                //already resized
                return;
            }

            // This atomic operation attempts to update the main table reference from the old table to the new (migrated) table.
            // It ensures that this change happens only if the current table is still the one that was originally read into table.
            // This prevents lost updates and ensures that all threads see the new table once the migration is complete.
            // Emphasize that the operation only succeeds if _table still referencestable, thereby preventing conflicts from concurrent resize operations.

            if (table._count == ctable._count && Interlocked.CompareExchange(ref _table, ctable, table) == table)
            {
                table._completed = 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, uint index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("key = {Key};  value = {Value};")]
        public struct Entry(TKey key, TValue value)
        {
            public TKey Key = key;
            public TValue Value = value;
        }

        internal class Table
        {
            #region Fields

            private byte _shift;
            private const sbyte _bitmask = (1 << 7) - 1;
            private const uint _goldenRatio = 0x9E3779B9; //2654435769; 
            internal byte _completed = 0;
            internal uint _migrationCount = 0;

            #endregion

            #region Properties

            public sbyte[] Metadata;
            public Entry[] Entries;
            public uint LengthMinusOne;
            public uint Threshold;
            public uint Length;
            public uint _count;

            #endregion

            /// <summary>
            /// Creates a snapshot of the current state
            /// </summary>
            /// <param name="entries"></param>
            /// <param name="metadata"></param>
            public Table(uint length, double _loadFactor)
            {
                Length = length;
                _shift = (byte)(32 - BitOperations.Log2(Length));

                LengthMinusOne = length - 1;
                Threshold = (uint)(Length * _loadFactor);

                Entries = GC.AllocateArray<Entry>((int)length);
                Metadata = GC.AllocateArray<sbyte>((int)length);

                // Fill arrays
                Metadata.AsSpan().Fill(_emptyBucket);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="hashcode"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetBucket(uint hashcode)
            {
                // return   _goldenRatio* hashcode >> _shift;

                // xor-shift some upper bits down, in case if variations are mostly in high bits
                // and scatter the bits a little to break up clusters if hashes are periodic (like 42, 43, 44, ...)
                // long clusters can cause long reprobes. small clusters are ok though.
                hashcode ^= hashcode >> 15;
                hashcode ^= hashcode >> 8;
                hashcode += (hashcode >> 3) * _goldenRatio;

                return hashcode & LengthMinusOne;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="hashcode"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public sbyte H2(uint hashcode)
            {
                var h2 = hashcode & _bitmask;
                return Unsafe.As<long, sbyte>(ref h2);
            }

            internal void Clear() => Array.Clear(Metadata);

            internal void Migrate(Table mTable, uint index)
            {
                for (; index < Metadata.Length; index++)
                {
                    if (_completed == 1)
                    {
                        return;
                    }

                    ref var location = ref Find(Metadata, index);
                    if (location == _resizeBucket || location == _inProgressMarker)
                    {
                        continue;
                    }

                    // Claim and sweep the bucket
                    var result = Interlocked.Exchange(ref location, _resizeBucket);
                    // Check if we succesfully claimed the bucket
                    if (result > -1)
                    {
                        mTable.EmplaceInternal(ref Find(Entries, index), result);
                    }
                }
            }

            internal bool EmplaceInternal(ref Entry entry, sbyte meta)
            {
                uint jumpDistance = 0;
                var hashcode = (uint)entry.Key.GetHashCode();
                var index = GetBucket(hashcode);

                do
                {
                    ref var location = ref Find(Metadata, index);
                    //Claim empty bucket
                    if (_emptyBucket == Interlocked.CompareExchange(ref location, meta, _emptyBucket))
                    {
                        Find(Entries, index) = entry;
                        Interlocked.Increment(ref _count);
                        return true;
                    }

                    //spot taken i
                    jumpDistance += 1;
                    index += jumpDistance;
                    index &= LengthMinusOne;
                } while (true);
            }
        }
    }
}