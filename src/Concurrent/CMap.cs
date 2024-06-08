using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.Diagnostics;

namespace Faster.Map.Concurrent
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
        public int Count { get => (int)_table._count; }

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

                for (int i = table.Entries.Length - 1; i >= 0; i--)
                {
                    var entry = table.Entries[i];
                    if (entry.Meta > -1)
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
                var table = _table;
                for (int i = table.Entries.Length - 1; i >= 0; i--)
                {
                    var entry = table.Entries[i];
                    if (entry.Meta > -1)
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
                var table = _table;
                for (int i = table.Entries.Length - 1; i >= 0; i--)
                {
                    var entry = table.Entries[i];
                    if (entry.Meta > -1)
                    {
                        yield return entry.Value;
                    }
                }
            }
        }

        #endregion

        #region Fields

        internal Table _migrationTable;
        internal Table _table;
        private const int _emptyBucket = -127;
        private const int _tombstone = -126;
        private const int _resizeBucket = -125;
        private const int _inProgressMarker = -124;
        private double _loadFactor;

        private readonly IEqualityComparer<TKey> _comparer;

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
        /// This method, Emplace, is designed to insert a key-value pair into a hash table while ensuring thread safety and managing collisions through quadratic probing. 
        /// It also handles dynamic resizing of the table when a certain threshold is reached.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Emplace(TKey key, TValue value)
        {
            var hashcode = key.GetHashCode(); // Get the hashcode of the key
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing

            // Resize the table if the count exceeds the threshold
            if (_table._count >= _table.Threshold)
            {
                Resize(); // Resize the table to accommodate more entries
            }

            start: // Label for restarting the insertion process after resizing
            var table = _table; // Get the current table
            var index = table.GetBucket(hashcode); // Calculate initial bucket index
            var h2 = table.H2(hashcode); // Calculate secondary hash for the entry metadata

            do
            {
                // Retrieve the metadata for the current entry
                ref var entry = ref Find(table.Entries, index);

                // Check if the bucket is empty and try to claim it
                if (_emptyBucket == entry.Meta && Interlocked.CompareExchange(ref entry.Meta, _inProgressMarker, _emptyBucket) == _emptyBucket)
                {
                    // Place the key and value in the entry
                    entry.Key = key;
                    entry.Value = value;
                    entry.Meta = h2;

                    // Interlocked operations provide a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                    // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                    // Note this also means we dont need any explicit memorybarriers.
                    // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.

                    Interlocked.Increment(ref table._count);

                    // Check if the table has been resized during the operation
                    if (_table != table)
                    {
                        // If resized, restart with the new table
                        jumpDistance = 0;
                        goto start;
                    }

                    // Resize the table if the count exceeds the threshold
                    if (table._count >= table.Threshold)
                    {
                        Resize(); // Resize the table to accommodate more entries
                    }

                    return true; // Successfully inserted the entry
                }

                // Check if the bucket contains a tombstone and try to claim it
                if (_tombstone == entry.Meta && Interlocked.CompareExchange(ref entry.Meta, _inProgressMarker, _tombstone) == _tombstone)
                {
                    entry.Key = key;
                    entry.Value = value;
                    entry.Meta = h2;

                    // Interlocked operations provide a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                    // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                    // Note this also means we dont need any explicit memorybarriers.
                    // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.

                    Interlocked.Increment(ref table._count);

                    // Check if the table has been resized during the operation
                    if (_table != table)
                    {
                        // If resized, restart with the new table
                        jumpDistance = 0;
                        goto start;
                    }

                    // Resize the table if the count exceeds the threshold
                    if (table._count >= table.Threshold)
                    {
                        Resize(); // Resize the table to accommodate more entries
                    }

                    return true; // Successfully inserted the entry
                }

                // Check if the bucket is occupied by an entry with the same key
                if (h2 == entry.Meta && _comparer.Equals(key, entry.Key))
                {
                    return false; // Key already exists, insertion failed
                }

                // If the bucket is marked for resizing, perform the resize operation
                if (entry.Meta == _resizeBucket)
                {
                    Resize(index); // Resize the table starting from the current index
                    jumpDistance = 0; // Reset jump distance
                    goto start; // Restart insertion process after resizing
                }

                // Retry insertion due to a collision or another thread claiming the bucket
                jumpDistance += 1; // Increment jump distance for quadratic probing
                index += jumpDistance; // Calculate new index with jump distance
                index &= table.LengthMinusOne; // Ensure the index is within table bounds
            } while (true); // Continue retrying until insertion is successful
        }

        /// <summary>
        /// The Get method retrieves a value from a concurrent hash table based on a key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue value)
        {
            // Calculate the hashcode for the given key
            var hashcode = key.GetHashCode();
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing

            start:

            // Get the current state of the table
            var table = _table;
            var index = table.GetBucket(hashcode); // Calculate the initial bucket index
            var h2 = table.H2(hashcode); // Calculate the secondary hash

            do
            {
                // Retrieve the entry from the table at the calculated index
                var entry = Find(table.Entries, index);
                if (h2 == entry.Meta && _comparer.Equals(key, entry.Key))
                {
                    // If the entry's metadata and key match, return the value
                    value = entry.Value;
                    return true;
                }

                if (_emptyBucket == entry.Meta)
                {
                    // If the entry is an empty bucket, the key does not exist in the table
                    value = default;
                    return false;
                }

                if (entry.Meta == _resizeBucket)
                {
                    Resize(index); // Perform the resize operation
                    jumpDistance = 0; // Reset the jump distance
                    goto start; // Restart the lookup process with the new table
                }

                // Increment the jump distance and calculate the next index using triangular probing
                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size
            } while (true); // Continue probing until a result is found or the table is exhausted
        }

        /// <summary>
        /// This method demonstrates a sophisticated approach to updating values in a concurrent hash table, leveraging quadratic probing, atomic operations, and handle the ABA problem effectively. 
        /// The use of aggressive inlining and careful memory management ensures that the method performs efficiently even under high concurrency.
        /// </summary>         
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue newValue)
        {
            // Calculate the hash code for the given key
            var hashcode = key.GetHashCode();
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing

            start:

            // Get the current state of the table
            var table = _table;
            var index = table.GetBucket(hashcode); // Calculate the initial bucket index
            var h2 = table.H2(hashcode); // Calculate the secondary hash

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);

                // If the entry indicates a resize operation, perform the resize
                if (_resizeBucket == entry.Meta)
                {
                    Resize(index); // Resize the table
                    jumpDistance = 0; // Reset the jump distance
                    goto start; // Restart the update process with the new table
                }

                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _comparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
                    // the enter method uses Interlocked.CompareExchange and thus provides a full memory fence, ensuring thread safety
                    // And ensures that the changes made by one thread are visible to others
                    entry.Enter();

                    if (h2 == entry.Meta)
                    {
                        // Perform the critical section: update the value
                        entry.Value = newValue;
                        entry.Exit();
                        return true;
                    }

                    // Check if the table has been resized during the operation
                    if (_table != table)
                    {
                        // If resized, restart with the new table
                        jumpDistance = 0;
                        entry.Exit();
                        goto start;
                    }
                }

                // If the entry indicates an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {
                    return false;
                }

                // Increment the jump distance and calculate the next index using triangular probing
                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size
            } while (true); // Continue probing until a matching entry is found or the table is exhausted
        }

        /// <summary>
        /// The Update method is designed to update the value associated with a given key in a concurrent hash table.
        /// The method uses aggressive inlining for performance optimization.
        /// It calculates the hash code for the key and uses quadratic probing to find the correct bucket in the table.
        /// If a matching entry is found, the method performs an atomic compare-and-swap operation to ensure thread safety. 
        /// If the value matches the comparison value, it updates the value; otherwise, it retries or exits as necessary.
        /// </summary>         
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue newValue, TValue comparisonValue)
        {
            // Calculate the hash code for the given key
            var hashcode = key.GetHashCode();
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing

            start:

            // Get the current state of the table
            var table = _table;
            var index = table.GetBucket(hashcode); // Calculate the initial bucket index
            var h2 = table.H2(hashcode); // Calculate the secondary hash

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);

                // If the entry indicates a resize operation, perform the resize
                if (_resizeBucket == entry.Meta)
                {
                    Resize(index); // Resize the table
                    jumpDistance = 0; // Reset the jump distance
                    goto start; // Restart the update process with the new table
                }

                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _comparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
                    // the enter method uses Interlocked.CompareExchange and thus provides a full memory fence, ensuring thread safety
                    // And ensures that the changes made by one thread are visible to others
                    entry.Enter();

                    if (h2 == entry.Meta)
                    {
                        // A value can be changed multiple times between the reading and writing of the value by a thread.
                        // This can lead to incorrect assumptions about the state of the value.
                        // A common way to solve this problem is to track changes to the value.
                        if (EqualityComparer<TValue>.Default.Equals(entry.Value, comparisonValue))
                        {
                            // Perform the critical section: update the value
                            entry.Value = newValue;
                            entry.Exit();
                            return true;
                        }

                        entry.Exit();
                        return false;
                    }

                    // Check if the table has been resized during the operation
                    if (_table != table)
                    {
                        // If resized, restart with the new table
                        jumpDistance = 0;
                        entry.Exit();
                        goto start;
                    }
                }

                // If the entry indicates an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {
                    return false;
                }

                // Increment the jump distance and calculate the next index using triangular probing
                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size
            } while (true); // Continue probing until a matching entry is found or the table is exhausted
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key, out TValue value)
        {
            // Calculate the hash code for the given key
            var hashcode = key.GetHashCode();
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing

            start:

            // Get the current state of the table
            var table = _table;
            var index = table.GetBucket(hashcode); // Calculate the initial bucket index
            var h2 = table.H2(hashcode); // Calculate the secondary hash

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);

                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _comparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
                    // the enter method uses Interlocked.CompareExchange and thus provides a full memory fence, ensuring thread safety
                    // And ensures that the changes made by one thread are visible to others
                    entry.Enter();

                    // Double-checked locking to prevent multiple threads from removing simultaneously
                    if (h2 == entry.Meta)
                    {
                        value = entry.Value;

                        // Perform the critical section
                        entry.Meta = _tombstone;
                        entry.Key = default;
                        entry.Value = default;

                        // Release the lock
                        entry.Exit();
                        Interlocked.Decrement(ref table._count);
                        return true;
                    }

                    // Release the lock
                    entry.Exit();
                    value = default;
                    return false;
                }

                // If the entry indicates an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {
                    value = default;
                    return false;
                }

                // If the entry indicates a resize operation, perform the resize
                if (_resizeBucket == entry.Meta)
                {
                    Resize(index); // Resize the table
                    jumpDistance = 0; // Reset the jump distance
                    goto start; // Restart the update process with the new table
                }

                // Increment the jump distance and calculate the next index using triangular probing
                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size

                // Continue probing until a matching entry is found or the table is exhausted
            } while (true);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            var table = new Table(_table.Length, _loadFactor);
            Interlocked.Exchange(ref _table, table);
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
                //Change to add or update
                if (!Emplace(key, value))
                {
                    throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
                }
            }
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

            if (table._completed == 1)
            {
                return;
            }

            if (table != _table)
            {
                //already resized
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

            if (table._completed == 0 && table._count == ctable._count && Interlocked.CompareExchange(ref _table, ctable, table) == table)
            {
                Interlocked.Exchange(ref table._completed, 1);
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
        [DebuggerDisplay("key = {Key};  value = {Value}; meta {Meta};")]
        internal struct Entry(TKey key, TValue value)
        {
            internal byte state;
            internal sbyte Meta;
            internal TKey Key = key;
            internal TValue Value = value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Enter()
            {
                int spinCount = 1; // Initial spin count

                while (true)
                {
                    // Attempt to set state to 1 (locked) if it is currently 0 (unlocked)
                    if (Interlocked.CompareExchange(ref state, 1, 0) == 0)
                    {
                        return;
                    }

                    // Optional: Exponential backoff to reduce contention
                    Thread.SpinWait(spinCount);

                    // Exponential backoff: Increase the spin count, but cap it to prevent excessive delays
                    if (spinCount < 1024)
                    {
                        spinCount *= 2;
                    }
                }
            }

            /// <summary>
            /// Release the lock by setting lockByte to 0 (unlocked)
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Exit() => Interlocked.Exchange(ref state, 0);

        }

        internal class Table
        {
            #region Fields
            private byte _shift = 32;
            private const sbyte _bitmask = (1 << 6) - 1;
            private const uint _goldenRatio = 0x9E3779B9; //2654435769; 
            internal byte _completed = 0;
            internal uint _migrationCount = 0;

            #endregion

            #region Properties

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
                LengthMinusOne = length - 1;
                Threshold = (uint)(Length * _loadFactor);
                _shift = (byte)(_shift - BitOperations.Log2(length));

                Entries = GC.AllocateUninitializedArray<Entry>((int)length);
                Entries.AsSpan().Fill(new Entry { Key = default, Value = default, Meta = _emptyBucket });
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="hashcode"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint GetBucket(int hashcode) => _goldenRatio * (uint)hashcode >> _shift;


            internal void Migrate(Table mTable, uint index)
            {
                for (; index < Entries.Length; index++)
                {
                    if (_completed == 1)
                    {
                        return;
                    }

                    ref var entry = ref Find(Entries, index);

                    // Entry is already resized, is in progress or the entry is locked
                    if (entry.Meta is _resizeBucket or _inProgressMarker && entry.state == 1)
                    {
                        continue;
                    }

                    // Sweep the bucket including empty and tombstones
                    var result = Interlocked.Exchange(ref entry.Meta, _resizeBucket);
                    // Only process the buckets with h2 data
                    if (result > -1)
                    {
                        mTable.EmplaceInternal(entry, result);
                    }
                }
            }

            internal bool EmplaceInternal(Entry entry, sbyte meta)
            {
                byte jumpDistance = 0;
                var hashcode = entry.Key.GetHashCode();
                var index = GetBucket(hashcode);

                do
                {
                    ref var location = ref Find(Entries, index);
                    //Claim empty bucket
                    if (_emptyBucket == Interlocked.CompareExchange(ref location.Meta, meta, _emptyBucket))
                    {
                        entry.Meta = meta;
                        location = entry;
                        Interlocked.Increment(ref _count);
                        return true;
                    }

                    //spot taken i
                    jumpDistance += 1;
                    index += jumpDistance;
                    index &= LengthMinusOne;
                } while (true);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal sbyte H2(int hashcode)
            {
                var result = hashcode & _bitmask;
                return Unsafe.As<int, sbyte>(ref result);
            }
        }
    }
}