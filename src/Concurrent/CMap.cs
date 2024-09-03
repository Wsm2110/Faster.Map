// Copyright (c) 2024, Wiljan Ruizendaal. All rights reserved. <wruizendaal@gmail.com> 
// Distributed under the MIT Software License, Version 1.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Faster.Map.Concurrent
{
    /// <summary>
    /// The CMap<TKey, TValue> class is a high-performance, thread-safe concurrent hashmap implemented using open addressing, quadratic probing, and Fibonacci hashing.
    /// It efficiently handles concurrent access and minimizes contention between threads.
    /// Key Features:
    /// Thread Safety: The class is designed for concurrent environments, using various techniques like atomic operations(Interlocked) and spin locks to ensure thread safety during insertions, deletions, and updates.
    /// Quadratic Probing: This technique is used to resolve collisions in the hash table, ensuring that even under high load, the table can efficiently find an empty slot for a new entry.
    /// Fibonacci Hashing: This method is used to calculate the initial bucket index, helping to distribute keys more uniformly across the table.
    /// Dynamic Resizing: The hashmap dynamically resizes itself when the jumpdistance exceeds maxJumpdistance. Note that we dont use a loadfactor 
    /// Metadata and State Management: Each entry in the hashmap includes metadata to track its state(e.g., empty, claimed, tombstone) and locks to manage concurrent updates.
    /// Optimized for Performance: The implementation uses techniques such as exponential backoff in spin locks and memory barriers to ensure that operations are fast and safe, even on multi-core systems.
    /// </summary>
    public class CMap<TKey, TValue> where TKey : notnull
    {
        #region Properties

        /// <summary>
        /// Gets the number of elements stored in the map.
        /// </summary>
        public int Count => (int)_counter.Sum();

        /// <summary>
        /// Returns all keys.
        /// </summary>
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
        /// Returns all values.
        /// </summary>
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

        /// <summary>
        /// Returns all the entries as KeyValuePair objects.
        /// </summary>
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

        #endregion

        #region Fields

        internal Table _migrationTable;
        internal Table _table;
        private const sbyte _emptyBucket = -127;
        private const sbyte _tombstone = -126;
        private const sbyte _resizeBucket = -125;
        private const sbyte _groupResized = -123;
        private const sbyte _claimed = -124;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly uint[] _powersOfTwo = {
            0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80,
            0x100, 0x200, 0x400, 0x800, 0x1000, 0x2000,
            0x4000, 0x8000, 0x10000, 0x20000, 0x40000,
            0x80000, 0x100000, 0x200000, 0x400000, 0x800000,
            0x1000000, 0x2000000, 0x4000000, 0x8000000,
            0x10000000, 0x20000000, 0x40000000, 0x80000000
        };
        private Counter _counter;

#if DEBUG
        private Table[] _migrationTables = new Table[31];
#endif

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CMap{TKey,TValue}"/> class with default settings.
        /// </summary>
        public CMap() : this(16, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CMap{TKey,TValue}"/> class with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The length of the hashmap. Will always take the closest power of two.</param>
        public CMap(uint initialCapacity) : this(initialCapacity, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CMap{TKey,TValue}"/> class with the specified settings.
        /// </summary>
        /// <param name="initialCapacity">The length of the hashmap. Will always take the closest power of two.</param>
        /// <param name="loadFactor">The load factor determines when the hashmap will resize (default is 0.5).</param>
        /// <param name="keyComparer">Used to compare keys to resolve hash collisions.</param>
        public CMap(uint initialCapacity, IEqualityComparer<TKey> keyComparer)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            if (initialCapacity < 16)
            {
                initialCapacity = 16;
            }

            _keyComparer = keyComparer;
            _table = new Table(BitOperations.RoundUpToPowerOf2(initialCapacity));
            _counter = new Counter();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts a key-value pair into the hash table. Ensures thread safety and manages collisions through quadratic probing. Handles dynamic resizing of the table when a threshold is reached.
        /// </summary>
        /// <param name="key">The key to insert.</param>
        /// <param name="value">The value to insert.</param>
        /// <returns>True if the key-value pair was inserted, false if the key already exists.</returns>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// bool inserted = cmap.Emplace(1, "Value1");
        /// if (inserted) Console.WriteLine("Inserted successfully.");
        /// </code>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Emplace(TKey key, TValue value)
        {
            var hashcode = key.GetHashCode(); // Get the hashcode of the key
            var h2 = _table.H2(hashcode); // Calculate secondary hash for the entry metadata

            start:
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing
            var table = _table; // Get the current table
            var index = table.GetIndex(hashcode); // Calculate initial bucket index

            do
            {
                // Retrieve the metadata for the current entry
                ref var entry = ref Find(table.Entries, index);
                // Check if the bucket is empty and try to claim it by setting a _claimed marker
                if (_emptyBucket == entry.Meta && Interlocked.CompareExchange(ref entry.Meta, _claimed, _emptyBucket) == _emptyBucket)
                {
                    // Place the key and value in the entry
                    entry.Key = key;
                    entry.Value = value;
                    entry.Meta = h2;

                    // _counter.Increment() uses a Interlocked.Increment() which provides a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                    // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                    // Note this also means we dont need any explicit memorybarriers.
                    // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.
                    _counter.Increment();
                    return true; // Successfully inserted the entry
                }
                // Check if the bucket contains a tombstone and try to claim it
                if (_tombstone == entry.Meta && Interlocked.CompareExchange(ref entry.Meta, _claimed, _tombstone) == _tombstone)
                {
                    entry.Key = key;
                    entry.Value = value;
                    entry.Meta = h2;

                    // _counter.Increment() uses a Interlocked.Increment() which provides a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                    // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                    // Note this also means we dont need any explicit memorybarriers.
                    // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.
                    _counter.Increment();
                    return true; // Successfully inserted the entry
                }
                // Check if the bucket is occupied by an entry with the same key
                if (h2 == entry.Meta && _keyComparer.Equals(key, entry.Key))
                {
                    return false;
                }

                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne;
            } while (jumpDistance <= table.MaxJumpDistance);

            // Trigger resize if necessary and restart insertion
            Resize(table);
            goto start;
        }

        /// <summary>
        /// Retrieves a value from the hash table based on the key.
        /// </summary>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <param name="value">The value associated with the key, if found.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap.Emplace(1, "Value1");
        /// if (cmap.Get(1, out var value))
        /// {
        ///     Console.WriteLine($"Retrieved value: {value}");
        /// }
        /// </code>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(TKey key, out TValue value)
        {
            var hashcode = key.GetHashCode();
            var h2 = _table.H2(hashcode);
            byte jumpDistance = 0;

            start:
            var table = _table;
            var index = table.GetIndex(hashcode);   

            do
            {
                // Retrieve the entry from the table at the calculated index
                var entry = Find(table.Entries, index);
                if (h2 == entry.Meta && _keyComparer.Equals(key, entry.Key))
                {
                    // If the entry's metadata and key match, return the value
                    value = entry.Value;
                    return true;
                }
                // If the entry is an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {                 
                    value = default;
                    return false;
                }
                // If the entry indicates a resize operation, help other threads resizing
                if (entry.Meta == _resizeBucket)
                {
                    Resize(table);
                    jumpDistance = 0;
                    goto start;
                }

                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size preventing out of bounds exceptions
            } while (true);
        }

        /// <summary>
        /// Updates the value associated with a key in the hash table. Ensures thread safety and manages the ABA problem.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="newValue">The new value to associate with the key.</param>
        /// <returns>True if the key was found and updated, false otherwise.</returns>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap.Emplace(1, "Value1");
        /// bool updated = cmap.Update(1, "UpdatedValue1");
        /// if (updated) Console.WriteLine("Updated successfully.");
        /// </code>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue newValue)
        {
            var hashcode = key.GetHashCode();
            byte jumpDistance = 0;
            var h2 = _table.H2(hashcode);

            start:
            var table = _table;
            var index = table.GetIndex(hashcode);

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);
                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _keyComparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
                    entry.Enter();

                    var result = false;

                    // There is a possibility meta has changed while locking this entry
                    if (h2 == entry.Meta)
                    {
                        // Perform the critical section: update the value
                        entry.Value = newValue;
                        result = true;
                    }

                    // Release lock 
                    entry.Exit();

                    return result;
                }

                // If the entry indicates an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {
                    return false;
                }

                // If the entry indicates a resize operation, help other threads resizing
                if (entry.Meta == _resizeBucket)
                {
                    Resize(table);
                    jumpDistance = 0;
                    goto start;
                }

                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size, preventing out of bounds exceptions
            } while (true);
        }

        /// <summary>
        /// Updates the value associated with a key if it matches the comparison value. Ensures thread safety.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="newValue">The new value to associate with the key.</param>
        /// <param name="comparisonValue">The value to compare against the current value.</param>
        /// <returns>True if the key was found and the value was updated, false otherwise.</returns>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap.Emplace(1, "Value1");
        /// bool updated = cmap.Update(1, "UpdatedValue1", "Value1");
        /// if (updated) Console.WriteLine("Conditionally updated successfully.");
        /// </code>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue newValue, TValue comparisonValue)
        {
            var hashcode = key.GetHashCode();
            var h2 = _table.H2(hashcode);
            byte jumpDistance = 0;

            start:   
            var table = _table;
            var index = table.GetIndex(hashcode);

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);
                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _keyComparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
                    entry.Enter();
                    bool result = false;
                    if (EqualityComparer<TValue>.Default.Equals(entry.Value, comparisonValue))
                    {
                        // Perform the critical section: update the value
                        entry.Value = newValue;
                        result = true;
                    }
                    entry.Exit();
                    return result;
                }
                // If the entry indicates a resize operation, perform the resize helping other threads 
                if (entry.Meta == _resizeBucket)
                {
                    Resize(table);
                    jumpDistance = 0;
                    goto start;
                }
                // If the entry indicates an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {
                    return false;
                }

                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size, preventing out of bounds eceptions
            } while (true);
        }

        /// <summary>
        /// Removes the entry associated with the specified key from the hash table.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was found and removed, false otherwise.</returns>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap.Emplace(1, "Value1");
        /// bool removed = cmap.Remove(1);
        /// if (removed) Console.WriteLine("Removed successfully.");
        /// </code>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            var hashcode = key.GetHashCode();
            var h2 = _table.H2(hashcode);
            byte jumpDistance = 0;

            start:
            var table = _table;         
            var index = table.GetIndex(hashcode);

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);
                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _keyComparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
                    entry.Enter();
                    bool result = false;

                    if (h2 == entry.Meta)
                    {
                        // reset current entry
                        entry.Meta = _tombstone;
                        entry.Key = default;
                        entry.Value = default;

                        // _counter.Decrement() uses a Interlocked.Decrement() which provides a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                        // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                        // Note this also means we dont need any explicit memorybarriers.
                        // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.
                        _counter.Decrement();
                        result = true;
                    }

                    // Release lock
                    entry.Exit();

                    return result;
                }

                // If the entry indicates an empty bucket, the key does not exist in the table
                if (_emptyBucket == entry.Meta)
                {
                    return false;
                }
                // If the entry indicates a resize operation, perform the resize helping other threads in the process
                if (entry.Meta == _resizeBucket)
                {
                    Resize(table); // Resize the table
                    jumpDistance = 0; // Reset the jump distance
                    goto start; // Restart the update process with the new table
                }

                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size, preventing out of bounds exceptions
            } while (true);
        }

        /// <summary>
        /// Removes the entry associated with the specified key from the hash table and outputs the value.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">The value associated with the key, if found.</param>
        /// <returns>True if the key was found and removed, false otherwise.</returns>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap.Emplace(1, "Value1");
        /// if (cmap.Remove(1, out var value))
        /// {
        ///     Console.WriteLine($"Removed successfully, value was: {value}");
        /// }
        /// </code>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key, out TValue value)
        {
            var hashcode = key.GetHashCode(); // Calculate the hash code for the given key
            byte jumpDistance = 0; // Initialize jump distance for quadratic probing
            var h2 = _table.H2(hashcode); // Calculate the secondary hash

            start:
            var table = _table; // Get the current state of the table
            var index = table.GetIndex(hashcode); // Calculate the initial bucket index

            do
            {
                // Retrieve the entry from the table at the calculated index
                ref var entry = ref Find(table.Entries, index);
                // If the entry's metadata and key match, proceed with the update
                if (h2 == entry.Meta && _keyComparer.Equals(key, entry.Key))
                {
                    // Guarantee that only one thread can access the critical section at a time
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
                        // _counter.Decrement() uses a Interlocked.Decrement() which provides a full memory fence, meaning they ensure all preceding memory writes are completed and visible to other threads before the Interlocked operation completes.
                        // This means that when you perform an Interlocked operation, it guarantees that any changes made to other variables(not just the variable involved in the Interlocked operation) are also visible to other threads.
                        // Note this also means we dont need any explicit memorybarriers.
                        // This code, using Interlocked operations, will also work correctly on ARM architectures without needing additional explicit memory barriers.The memory ordering and visibility are managed by the Interlocked methods.
                        _counter.Decrement();
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
                if (entry.Meta == _resizeBucket)
                {
                    Resize(table); // Resize the table
                    jumpDistance = 0; // Reset the jump distance
                    goto start; // Restart the update process with the new table
                }

                jumpDistance += 1;
                index += jumpDistance;
                index &= table.LengthMinusOne; // Ensure the index wraps around the table size, preventing out of bounds exceptions           
            } while (true);
        }

        /// <summary>
        /// Clears all entries from the hash table.
        /// </summary>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap.Emplace(1, "Value1");
        /// cmap.Clear();
        /// Console.WriteLine("Map cleared.");
        /// </code>
        public void Clear()
        {
            Interlocked.Exchange(ref _table, new Table(_table.Length));
            Interlocked.Exchange(ref _counter, new Counter());
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to get or set the value for.</param>
        /// <returns>The value associated with the specified key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key is not found when getting or setting the value.</exception>
        /// 
        /// Example:
        /// <code>
        /// var cmap = new CMap<int, string>();
        /// cmap[1] = "Value1";
        /// Console.WriteLine($"Value for key 1: {cmap[1]}");
        /// </code>
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
                if (!Emplace(key, value))
                {
                    throw new KeyNotFoundException($"Unable to find entry - {key.GetType().FullName} key - {key.GetHashCode()}");
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Resizes the hash table to accommodate more entries. Ensures thread safety during the resize operation.
        /// Note. Will only allocate once while resizing.
        /// </summary>
        /// <param name="table">The current table to resize.</param>
        internal void Resize(Table table)
        {
            // Ensure that the current table is not larger than the new table. Indicates it already resized
            if (table.Length < _table.Length)
            {
                return;
            }
            // Calculate the log base 2 of the table length to find the power of two index
            var index = BitOperations.Log2(table.Length);
            // Check if the resize has already been initiated for this size
            if (_powersOfTwo[index] > 0)
            {
                // Atomically set the power of two index to 0 if it matches the table length
                if (Interlocked.CompareExchange(ref _powersOfTwo[index], 0, table.Length) == table.Length)
                {
                    // Create a new migration table with double the current table's length
                    var migrationTable = new Table(table.Length << 1);
                    // Atomically update the migration table reference
                    Interlocked.Exchange(ref _migrationTable, migrationTable);
#if DEBUG            
                    Interlocked.Exchange(ref _migrationTables[index], migrationTable);
#endif
                }
            }
            // Retrieve the current migration table reference
            var ctable = _migrationTable;
            // Ensure the migration table is not null and has a different length from the current table
            if (ctable == null || table.Length == ctable.Length)
            {
                return;
            }
            // Migrate the entries from the current table to the migration table
            table.Migrate(ctable);
            // Check if the migration is complete
            if (table._depletedCounter == table._depleted)
            {
                // Atomically update the main table reference to the new migrated table
                Interlocked.CompareExchange(ref _table, ctable, table);
            }
        }

        /// <summary>
        /// Finds an element in the array at the specified index.
        /// </summary>
        /// <typeparam name="T">The type of element in the array.</typeparam>
        /// <param name="array">The array to search.</param>
        /// <param name="index">The index to find the element at.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, uint index)
        {
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
        }

        #endregion

        /// <summary>
        /// Represents an entry in the hash table.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("key = {Key}; value = {Value}; meta = {Meta};")]
        internal struct Entry
        {
            /// <summary>
            /// The state of the entry, used for locking. 0 indicates unlocked, 1 indicates locked.
            /// </summary>
            internal byte state;

            /// <summary>
            /// Metadata associated with the entry. This is used to indicate the state of the entry,
            /// such as whether it is empty, a tombstone, in-progress, or not empty (H2 hash)
            /// </summary>
            internal sbyte Meta;

            /// <summary>
            /// The key of the entry.
            /// </summary>
            internal TKey Key;

            /// <summary>
            /// The value of the entry.
            /// </summary>
            internal TValue Value;

            /// <summary>
            /// Enters a critical section by acquiring a lock. Ensures thread safety.
            /// This method uses a spin lock with exponential backoff to reduce contention.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Enter()
            {
                int spinCount = 1; // Initialize spin count for exponential backoff
                while (true)
                {
                    // Attempt to acquire the lock by setting the state to 1 if it is currently 0
                    if (Interlocked.CompareExchange(ref state, 1, 0) == 0)
                    {
                        return; // Lock acquired successfully, exit the method
                    }
                    // Perform a spin wait to allow other threads to progress
                    Thread.SpinWait(spinCount);
                    // Increment the spin count exponentially, but cap it to prevent excessive delays
                    if (spinCount < 1024)
                    {
                        spinCount *= 2;
                    }
                }
            }

            /// <summary>
            /// Exits the critical section by releasing the lock.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Exit() => Interlocked.Exchange(ref state, 0);
        }

        internal class Table
        {
            #region Fields

            /// <summary>
            /// The bit shift value used in hash calculation to determine bucket index.
            /// </summary>
            private byte _shift = 32;

            /// <summary>
            /// The bitmask value used for secondary hash calculation.
            /// </summary>
            private const sbyte _bitmask = (1 << 7) - 1;

            /// <summary>
            /// A constant value based on the golden ratio, used for hash calculation.
            /// </summary>
            private const uint _goldenRatio = 0x9E3779B9;

            /// <summary>
            /// The size of each group of entries for migration purposes.
            /// </summary>
            internal uint _groupSize;

            /// <summary>
            /// The total number of groups in the table.
            /// </summary>
            internal int _depleted;

            /// <summary>
            /// The current index of the group being processed during migration.
            /// </summary>
            private uint _groupIndex;

            #endregion

            #region Properties

            /// <summary>
            /// The array of entries in the hash table.
            /// </summary>
            public Entry[] Entries;

            /// <summary>
            /// Length of the table minus one, used for efficient modulus operations.
            /// </summary>
            public uint LengthMinusOne;

            /// <summary>
            /// The current length of the table.
            /// </summary>
            public uint Length;

            /// <summary>
            /// The number of groups minus one, used for group indexing.
            /// </summary>
            private uint _groupsMinusOne;

            /// <summary>
            /// Counter for the number of depleted groups during migration.
            /// </summary>
            internal int _depletedCounter;

            /// <summary>
            /// The maximum distance to jump during quadratic probing.
            /// </summary>
            public byte MaxJumpDistance { get; internal set; }

            #endregion

            #region Constructor

            /// <summary>
            /// Creates a new table with the specified length and load factor.
            /// </summary>
            /// <param name="length">The length of the table.</param>
            /// <param name="_loadFactor">The load factor of the table.</param>

            #endregion

            /// <summary>
            /// Creates a new table with the specified length and load factor.
            /// </summary>
            /// <param name="length">The length of the table.</param>
            /// <param name="_loadFactor">The load factor of the table.</param>
            public Table(uint length)
            {
                Length = length;
                LengthMinusOne = length - 1;
                _shift = (byte)(_shift - BitOperations.Log2(length));

                Entries = GC.AllocateUninitializedArray<Entry>((int)length);
                Entries.AsSpan().Fill(new Entry { Meta = _emptyBucket });

                _groupSize = DetermineChunkSize((uint)BitOperations.Log2(length), Environment.ProcessorCount);
                _groupsMinusOne = (length / _groupSize) - 1;
                _depleted = (int)(length / _groupSize);
                MaxJumpDistance = (byte)(BitOperations.Log2(length) + 1);
            }

            /// <summary>
            /// Determines the chunk size based on the length and number of CPU cores.
            /// </summary>
            /// <param name="length">The length of the table.</param>
            /// <param name="cpuCores">The number of CPU cores.</param>
            /// <returns>The chunk size.</returns>
            private static uint DetermineChunkSize(uint length, int cpuCores)
            {
                return length switch
                {
                    4 => 16,
                    5 => 32,
                    6 => 64,
                    7 => 128,
                    8 => 256,
                    9 => 512,
                    10 => 1024,
                    11 => 1024,
                    12 => 1024,
                    13 => 1024,
                    14 => 1024,
                    15 => 1024,
                    16 => 1024,
                    17 => 2048,
                    18 => 2048,
                    19 => 4096,
                    20 => 8192,
                    21 => 8192,
                    22 => 16384,
                    23 => 16384,
                    24 => 16384,
                    25 => 16384,
                    26 => 16384,
                    27 => 16384,
                    28 => 16384,
                    29 => 16384,
                    30 => 16384,
                    31 => 16384,
                    _ => 16,
                };
            }

            /// <summary>
            /// Calculates the initial bucket index using the golden ratio.
            /// </summary>
            /// <param name="hashcode">The hash code of the key.</param>
            /// <returns>The bucket index.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal uint GetIndex(int hashcode) => _goldenRatio * (uint)hashcode >> _shift;

            /// <summary>
            /// Migrates entries from the current table to the specified table.
            /// </summary>
            /// <param name="mTable">The table to migrate entries to.</param>
            /// <summary>
            /// Migrates entries from the current table to the specified migration table.
            /// This method ensures thread safety by using atomic operations to claim groups of entries
            /// and move them to the new table, while other threads may still be accessing the old table.
            /// </summary>
            /// <param name="mTable">The migration table to move entries to.</param>
            internal void Migrate(Table mTable)
            {
                // Continue migrating until all groups are depleted of resources
                while (_depletedCounter < _depleted)
                {
                    // Atomically increment the group index and wrap around if necessary
                    uint groupIndex = Interlocked.Increment(ref _groupIndex) & _groupsMinusOne;
                    uint index = groupIndex * _groupSize;
                    uint end = index + _groupSize;
                    // Find the first entry in the group
                    ref var entry = ref Find(Entries, index);
                    var meta = entry.Meta;
                    // Check if the group has already been resized
                    if (entry.Meta != _groupResized)
                    {
                        // Atomically mark the group as being resized
                        var result = Interlocked.CompareExchange(ref entry.Meta, _groupResized, meta);
                        if (result != meta)
                        {
                            // Another thread has claimed this group, exit to retry another group
                            return;
                        }
                        // If the entry is valid having an meta > 1 migrate it to the new table
                        if (result > -1)
                        {
                            mTable.EmplaceInternal(ref entry, result);
                        }
                        // Move to the next entry in the group
                        ++index;
                        // Process all entries in the group
                        do
                        {
                            entry = ref Find(Entries, index);
                            meta = entry.Meta;
                            // Skip entries that are in progress
                            if (meta == _claimed)
                            {
                                // Note we arent actually increasing the index, we just retry
                                continue;
                            }
                            // Atomically mark the entry as being resized
                            result = Interlocked.CompareExchange(ref entry.Meta, _resizeBucket, meta);
                            // If the entry is valid and the compare-and-swap succeeded, migrate the entry
                            if (result == meta && result > -1)
                            {
                                mTable.EmplaceInternal(ref entry, result);
                            }
                            // If the compare-and-swap failed, retry the current entry
                            if (result != meta)
                            {
                                continue;
                            }
                            // Move to the next entry in the group
                            ++index;
                        } while (index < end);
                        // Increment the depleted counter once the entire group is processed
                        Interlocked.Increment(ref _depletedCounter);
                    }
                }
            }

            /// <summary>
            /// Inserts an entry into the hash table during migration. This method ensures thread safety by using atomic operations
            /// to place entries into empty buckets and handles collisions with quadratic probing.
            /// </summary>
            /// <param name="entry">The entry to insert.</param>
            /// <param name="meta">The metadata associated with the entry.</param>
            /// <returns>True if the entry was successfully inserted, false otherwise.</returns>
            internal bool EmplaceInternal(ref Entry entry, sbyte meta)
            {
                byte jumpDistance = 0; // Initialize jump distance for quadratic probing
                var hashcode = entry.Key.GetHashCode(); // Calculate the hashcode of the key
                var index = GetIndex(hashcode); // Calculate the initial bucket index using the hashcode

                do
                {
                    // Retrieve the location in the table at the calculated index
                    ref var location = ref Find(Entries, index);
                    // Check if the bucket is empty and try to claim it
                    if (location.Meta == _emptyBucket && _emptyBucket == Interlocked.CompareExchange(ref location.Meta, meta, _emptyBucket))
                    {
                        // Place the entry in the bucket
                        location = entry;
                        location.Meta = meta;
                        return true; // Successfully inserted the entry
                    }

                    jumpDistance += 1;
                    index += jumpDistance;
                    index &= LengthMinusOne; // Ensure the index wraps around the table size, preventing out of bounds exceptions       
                } while (true);
            }

            /// <summary>
            /// Calculates the secondary hash value for the hash code.
            /// </summary>
            /// <param name="hashcode">The hash code of the key.</param>
            /// <returns>The secondary hash value.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal sbyte H2(int hashcode)
            {
                var result = hashcode & _bitmask;
                return Unsafe.As<int, sbyte>(ref result);
            }
        }
    }
}