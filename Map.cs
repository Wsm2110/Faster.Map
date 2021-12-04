using System;
using System.Runtime.CompilerServices;

namespace Faster
{
    /// <summary>
    ///   This hashmap is heavily optimized to be used with numerical keys 
    ///   And is alot faster than Map which allows all sorts of keys :)
    /// 
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robing hood hash
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// - fixed uint key in order not having to call GetHashCode() which is an override... and overrides are not ideal in terms of performance
    /// - fibonacci hashing
    /// </summary>
    public class Map<TKey, TValue> where TKey : unmanaged, IComparable, IEquatable<TKey>
    {
        #region properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public uint EntryCount { get; private set; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public uint Size => (uint)_entries.Length;

        #endregion

        #region Fields

        private InfoByte[] _info;
        public int[] _keys;
        private uint _maxlookups;
        private readonly double _loadFactor;
        private Entry<TValue>[] _entries;
        private const uint _multiplier = 0x9E3779B9; // 2654435769; 
        private int _shift = 32;
        private uint _pslLimitor;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        public Map(uint length = 16, double loadFactor = 0.90d)
        {
            //default length is 16
            _maxlookups = length == 0 ? 16 : length;
            _loadFactor = loadFactor;

            var size = NextPow2(_maxlookups);
            _pslLimitor = PslLimit(size);

            _shift = _shift - (int)Log2(_maxlookups) + 1;
            _entries = new Entry<TValue>[size + _pslLimitor];
            _info = new InfoByte[size + _pslLimitor];
            _keys = new int[size + _pslLimitor];
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
            if ((double)EntryCount / _maxlookups > _loadFactor || EntryCount >= _maxlookups)
            {
                Resize();
            }

            int hashcode = key.GetHashCode();
            uint index = (uint)hashcode * _multiplier >> _shift;

            // validate if the key is unique
            if (KeyExists(index, hashcode))
            {
                return false;
            }

            return EmplaceNew(value, ref index, ref hashcode);
        }

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="index">The index.</param>
        /// <param name="hashcode">The hashcode.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EmplaceNew(TValue value, ref uint index, ref int hashcode)
        {
            var info = _info[index];
            ++info.Count; // increase count of the original hashed entry
            _info[index] = info; // update information about this entry

            var entry = _entries[index];
            if (entry.IsEmpty())
            {
                entry.Psl = 0;
                entry.Value = value;
                _entries[index] = entry;
                _keys[index] = hashcode; //ToDo store part?
                ++EntryCount;
                return true;
            }

            Entry<TValue> insertableEntry = default;

            byte psl = 1;
            ++index;

            insertableEntry.Value = value;
            insertableEntry.Psl = 0; // set not empty

            for (; ; ++psl, ++index)
            {
                entry = _entries[index];
                if (entry.IsEmpty())
                {
                    insertableEntry.Psl = psl;

                    //some weird stuff
                    var idx = (uint)hashcode * _multiplier >> _shift;
                    var ie = _info[idx];
                    ie.Offset = (byte)(index - idx);
                    _info[idx] = ie;

                    _entries[index] = insertableEntry;
                    _keys[index] = hashcode;
                    ++EntryCount;
                    return true;
                }

                if (psl > entry.Psl)
                {
                    insertableEntry.Psl = psl;

                    swap(ref insertableEntry, ref _entries[index]);

                    //calculate the offset from it's original position
                    var idx = (uint)hashcode * _multiplier >> _shift;
                    var ie = _info[idx];
                    ie.Offset = (byte)(index - idx);
                    _info[idx] = ie;

                    swap(ref hashcode, ref _keys[index]);
                    psl = insertableEntry.Psl;
                }

                if (psl == _pslLimitor)
                {
                    Resize();
                    var idx = (uint)hashcode * _multiplier >> _shift;
                    EmplaceNew(insertableEntry.Value, ref idx, ref hashcode);
                }
            }
        }

        /// <summary>
        /// Gets the value with the corresponding key
        /// </summary>
        /// <param name="key">The key.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(TKey key)
        {
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.Count == 0)
            {
                return default;
            }

            uint offset = index + info.Offset;

            for (; offset >= index; --offset)
            {
                var entry = _keys[offset];
                if (entry == hashcode)
                {
                    return _entries[offset].Value;
                }              
            }

            return default;
        }

        /// <summary>
        /// Find if key exists in the map
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="hashcode">The hashcode.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool KeyExists(uint index, int hashcode)
        {
            var info = _info[index];
            if (info.Count == 0)
            {
                //slot is  empty
                return false;
            }

            uint offset = index + info.Offset;
            if (offset == 0)
            {
                var entry = _keys[index];
                if (entry == hashcode)
                {
                    return true;
                }

                return false;
            }
            
            for (; offset > index; --offset)
            {
                var entry = _keys[offset];
                if (entry == hashcode)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// update the entry
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Update(TKey key, TValue value)
        {
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.Count == 0)
            {
                return false;
            }

            uint offset = index + info.Offset;

            for (; offset >= index; --offset)
            {
                var storedKey = _keys[offset];
                if (storedKey == hashcode)
                {
                    var entry = _entries[offset];

                    entry.Value = value;
                    _entries[offset] = entry;

                    return true;
                }      
            }
            return default;
        }

        ///// <summary>
        ///// Removes the current entry using a backshift removal
        ///// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            uint hashcode = (uint)key.GetHashCode();
            uint index = hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.Count == 0)
            {
                //key not found 
                return false;
            }

            int idx = -1;

            //delete entry
            uint offset = index + info.Offset;
            byte psl = 0;
            for (; offset >= index; --offset)
            {
                var entry = _keys[offset];
                if (entry == hashcode)
                {
                    //romove entry from list
                    var e = _entries[offset];
                    psl = e.Psl;
                    e = default; // reset entry
                    _entries[offset] = e;
                    idx = (int)offset;

                    //update info
                    --info.Count;
                    --info.Offset;
                    _info[index] = info;

                    //remove key from list
                    var oldKey = _keys[idx];
                    oldKey = default;
                    _keys[idx] = oldKey;
                    --EntryCount;

                    break;
                }        
            }

            if (idx == -1)
            {
                //abort removal
                return false;
            }

            var bounds = _maxlookups + _pslLimitor - 1;
            for (; idx < bounds ; ++idx)
            {
                var skey = _keys[idx + 1];
                if (skey == 0)
                {
                    //empty slot, stop backshift removal
                    break;                    
                }

                var sEntry = _entries[idx + 1];
                if (sEntry.Psl == 0)
                {
                    break;
                }

                if (sEntry.Psl >= psl)
                {
                    uint originalIndex = (uint)skey * _multiplier >> _shift;
                    var off = idx - originalIndex;
                    var i = _info[originalIndex];
                    i.Offset = (byte)off;
                    _info[originalIndex] = i;

                    //decrease psl
                    --sEntry.Psl;

                    swap(ref skey, ref _keys[idx]);
                    swap(ref sEntry, ref _entries[idx]);

                    //clear key
                    skey = default;
                    _keys[idx + 1] = skey;

                    //clear entry
                    sEntry = default;
                    _entries[idx + 1] = sEntry;
                }              
            }

            return true;
        }

        public bool ContainsKey(TKey key)
        {
            var hashcode = key.GetHashCode();
            uint index = (uint)hashcode * _multiplier >> _shift;

            var info = _info[index];
            if (info.Count == 0)
            {
                //slot is  empty
                return false;
            }

            uint offset = index + info.Offset;

            for (; offset >= index; offset -= 2)
            {
                var entry = _keys[offset];
                if (entry == hashcode)
                {
                    return true;
                }

                entry = _keys[offset - 1];
                if (entry == hashcode)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        private void swap(ref Entry<TValue> x, ref Entry<TValue> y)
        {
            var tmp = x;

            x.Value = y.Value;
            x.Psl = y.Psl;

            y.Value = tmp.Value;
            y.Psl = tmp.Psl;
        }

        private void swap(ref int x, ref int y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        private uint PslLimit(uint size)
        {
            switch (size)
            {
                case 16: return 5;
                case 32: return 8;
                case 64: return 10;
                case 128: return 12;
                case 256: return 16;
                case 512: return 20;
                case 1024: return 22;
                case 2048: return 26;
                case 4096: return 32;
                case 8192: return 36;
                case 16384: return 40;
                case 32768: return 44;
                case 65536: return 48;
                case 131072: return 52;
                case 262144: return 56;
                case 524288: return 60;
                case 1048576: return 66;
                case 2097152: return 70;
                case 4194304: return 74;
                case 8388608: return 78;
                case 16777216: return 82;
                case 33554432: return 86;
                case 67108864: return 90;
                case 134217728: return 94;
                case 268435456: return 98;
                case 536870912: return 102;
                default: return 10;
            }
        }

        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _maxlookups = NextPow2(_maxlookups + 1);
        
            var oldEntries = new Entry<TValue>[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldKeys = new int[_keys.Length];
            Array.Copy(_keys, oldKeys, _keys.Length);

            _pslLimitor = PslLimit(_maxlookups);
            
            _entries = new Entry<TValue>[_maxlookups + _pslLimitor];
            _info = new InfoByte[_maxlookups + _pslLimitor];
            _keys = new int[_maxlookups + _pslLimitor];

            EntryCount = 0;

            for (var i = 0; i < oldKeys.Length; i++)
            {
                var key = oldKeys[i];
                if (key == 0)
                {
                    continue;
                }

                var value = oldEntries[i].Value;
                uint index = (uint)key * _multiplier >> _shift;
                EmplaceNew(value, ref index, ref key);
            }
        }
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
        private static uint Log2(uint value)
        {
            uint c = 0;
            while (value > 0)
            {
                c++;
                value >>= 1;
            }

            return c;
        }

        #endregion

    }

}