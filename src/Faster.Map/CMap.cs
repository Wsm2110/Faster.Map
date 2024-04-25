using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.Linq;
using System.Diagnostics;

namespace Faster.Map.QuadMap
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
        public uint Count { get => _count; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public uint Size => (uint)_table.Entries.Length;

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

                for (int i = _table.Metadata.Length - 1; i >= 0; i--)
                {
                    var meta = Volatile.Read(ref _table.Metadata[i]);
                    if (meta > -1)
                    {
                        var entry = Volatile.Read(ref _table.Entries[i]);
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
                    var meta = Volatile.Read(ref _table.Metadata[i]);
                    if (meta > -1)
                    {
                        yield return Volatile.Read(ref _table.Entries[i]).Key;
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
                        var entry = Volatile.Read(ref _table.Entries[i]);
                        yield return entry.Value;
                    }
                }
            }
        }

        #endregion

        #region Fields
        private int _resizeInProgress;
        private const sbyte _emptyBucket = -126;
        private const sbyte _tombstone = -125;
        private const sbyte _resizeBucket = -124;
        private double _loadFactor;
        private uint _count = 0;
        private readonly IEqualityComparer<TKey> _comparer;
        private volatile Table _migrationTable;
        private volatile Table _table;

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

            if (initialCapacity < 8)
            {
                initialCapacity = 8;
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

            Interlocked.Increment(ref _count);

            start:

            var table = _table;

            //Resize if threshold is reached
            if (_count > table.Threshhold)
            {
                Resize();
                goto start;
            }

            var hashcode = (uint)key.GetHashCode();
            var index = table.GetIndex(hashcode);
            var h2 = table.H2(hashcode);
            byte jumpDistance = 0;

            do
            {
                // Retrieve metadata
                var meta = Volatile.Read(ref Find(table.Metadata, index));
                if (meta is _resizeBucket)
                {
                    Resize();
                    goto start;
                }

                // Try to claim emptybucket
                if (Interlocked.CompareExchange(ref Find(table.Metadata, index), h2, _emptyBucket) is _emptyBucket)
                {
                    // Successfully claimed tomsto the bucket, add the entry
                    Interlocked.Exchange(ref table.Entries[index], new Entry(key, value));
                    return true;
                }

                // Try to claim tombstone bucket
                if (Interlocked.CompareExchange(ref Find(table.Metadata, index), h2, _tombstone) is _tombstone)
                {
                    // Successfully claimed the bucket, add the entry
                    Interlocked.Exchange(ref table.Entries[index], new Entry(key, value));
                    return true;
                }


                //Claiming a metadata slot, and insert in an entry are 2 instructions which can never be threadsafe;
                // while resizing, a metadata slot can have a h2 hash but the entry can be null.....
                var entry = Volatile.Read(ref Find(table.Entries, index));

                if (entry == null)
                {
                    goto start;
                }

                // Bucket is occupied, check if key matches
                if (h2 == meta && _comparer.Equals(key, entry.Key))
                {

                    Interlocked.Decrement(ref _count);
                    // Key has already been added to the map
                    return false;
                }

                // Retry due to collision or another thread claiming the bucket
                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne;
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

        private volatile uint _resizeIndex;

        /// <summary>
        /// This method is designed to be used in a highly concurrent environment where minimizing locking and blocking is crucial.
        /// The use of lock-free programming techniques helps to maximize scalability and performance by allowing multiple threads to operate in parallel with minimal interference.
        /// </summary>
        public void Resize()
        {
            // These lines read the current table and its properties.
            // The use of local variables here is thread - safe as they only capture the state at a specific point in time and do not modify shared state.
            var table = _table;
            var length = table.Length;
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
            // Here, all threads, regardless of whether they initialized the resize, help with the migration process.
            // This shared responsibility model enhances performance and reduces the time the table is in an inconsistent state during resizing.
            if (table == ctable)
            {
                return;
            }

            // Avoid unnecessary work if the migration has already been completed by another thread.
            // This check is read-safe assuming that MigrationCompleted is either volatile or accessed through atomic operations internally.
            if (ctable.MigrationCompleted == 1)
            {
                return;
            }

            table.AssistMigration(ctable);

            // This atomic operation attempts to update the main table reference from the old table to the new (migrated) table.
            // It ensures that this change happens only if the current table is still the one that was originally read into table.
            // This prevents lost updates and ensures that all threads see the new table once the migration is complete.
            // Emphasize that the operation only succeeds if _table still references table, thereby preventing conflicts from concurrent resize operations.
            if (Interlocked.CompareExchange(ref _table, ctable, table) == table)
            {

            }

            table.CompleteMigration();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, uint index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        internal class Entry
        {
            public TKey Key;
            public TValue Value;

            public Entry(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        internal class Table
        {
            #region Fields

            private volatile byte _shift;
            private const sbyte _bitmask = (1 << 7) - 1;
            private const uint _goldenRatio = 0x9E3779B9; //2654435769;
            private volatile byte _completed = 0;

            #endregion

            #region Properties

            public volatile sbyte[] Metadata;
            public volatile Entry[] Entries;
            public volatile uint LengthMinusOne;
            public volatile uint Threshhold;
            public volatile uint Length;

            public byte MigrationCompleted => _completed;

            #endregion
            /// <summary>
            /// Creates a snapshot of the current hashmap state
            /// </summary>
            /// <param name="entries"></param>
            /// <param name="metadata"></param>
            public Table(uint length, double _loadFactor)
            {
                Length = length;
                _shift = (byte)(32 - BitOperations.Log2(Length));

                LengthMinusOne = Length - 1;
                Threshhold = (uint)(Length * _loadFactor);

                // Allocate a new, larger metadata and entry array
                Entries = new Entry[Length];
                Metadata = new sbyte[Length];
                Metadata.AsSpan().Fill(_emptyBucket);
            }

            public void AssistMigration(Table next)
            {
                for (uint i = 0; i < Metadata.Length; i++)
                {
                    if (_completed == 1)
                    {
                        return;
                    }
                    var meta = Metadata[i];

                    // Claim and sweep the bucket
                    if (Interlocked.CompareExchange(ref Metadata[i], _resizeBucket, meta) == meta)
                    {
                        // Make sure we have to latest. Threads can still add, update or remove entries while resizing
                        var entry = Volatile.Read(ref Find(Entries, i));
                        if (entry == null)
                        {
                            SpinWait.SpinUntil(() =>  Find(Entries, i) != null);
                        }

                        next.Move(entry, meta);
                        continue;
                    }

                    // Claim empty bucket and set resize market
                    if (Interlocked.CompareExchange(ref Metadata[i], _resizeBucket, _emptyBucket) == _emptyBucket)
                    {
                        continue;
                    }

                    // Claim tombstone bucket and set resize marker
                    Interlocked.CompareExchange(ref Metadata[i], _resizeBucket, _tombstone);

                }

                var markers = Metadata.Count(i => i != _resizeBucket);
                if (markers > 0)
                {
                    Debugger.Launch();
                }
            }

            private void Move(Entry entry, sbyte meta)
            {
                var hashcode = (uint)entry.Key.GetHashCode();
                var index = GetIndex(hashcode);
                byte jumpDistance = 0;

                do
                {
                    var nMeta = Volatile.Read(ref Metadata[index]);
                    if (nMeta is _resizeBucket)
                    {
                        //resize is already in progress
                        return;
                    }


                    //Claim empty bucket
                    if (Interlocked.CompareExchange(ref Metadata[index], meta, _emptyBucket) is _emptyBucket)
                    {
                        //Claim current Bucket
                        Interlocked.Exchange(ref Entries[index], entry);
                        return;
                    }


                    //spot taken i
                    jumpDistance += 1;
                    index += jumpDistance;
                    index &= LengthMinusOne;


                } while (true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="hashcode"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetIndex(uint hashcode) => _goldenRatio * hashcode >> _shift;

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

            internal void CompleteMigration() => Interlocked.CompareExchange(ref _completed, 1, 0);
        }
    }
}

