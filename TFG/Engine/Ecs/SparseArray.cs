using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Engine.Core;

namespace Engine.Ecs
{
    public class SparseArray<TDataType>
    {
        public const int NullKey = -1;

        private List<TDataType> data;
        private int[] slots;
        private int[] remove;

        public int Count 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return data.Count; } 
        }

        public TDataType this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return data[index]; }
        }

        public SparseArray(int initialCapacity = 1)
        {
            data = new List<TDataType>(initialCapacity);
            slots = new int[initialCapacity];
            remove = new int[initialCapacity];

            Array.Fill(slots, NullKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySA<TDataType> AsReadOnly()
        {
            return new ReadOnlySA<TDataType>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int key)
        {
            return key < slots.Length && slots[key] != NullKey;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetKey(int index)
        {
            Debug.Assert(index > -1 && index < remove.Length && remove[index] != NullKey,
                "Cannot get key with index {0}. Index out of bounds", index);
            return remove[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDataType Get(int key)
        {
            Debug.Assert(Contains(key), 
                "Cannot get item with key {0}. Item not found", key);
            return data[slots[key]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int key, out TDataType item)
        {
            if (Contains(key))
            {
                item = data[slots[key]];
                return true;
            }

            item = default;
            return false;
        }

        public TDataType Add(int key, TDataType item)
        {
            Debug.Assert(!Contains(key), 
                "An item with the same key ({0}) has already been added.", key);

            if (key >= slots.Length)
                Resize(slots.Length + key + 1);

            int size = data.Count;
            slots[key] = size;
            remove[size] = key;

            data.Add(item);

            return item;
        }

        public void Remove(int key)
        {
            Debug.Assert(Contains(key), 
                "Cannot remove item with key {0}. Item not found", key);

            int index = slots[key];
            int lastIndex = data.Count - 1;

            if (index != lastIndex)
            {
                //Get the key of the last item, swap with the one being removed
                //and remove the last item
                int lastKey = remove[lastIndex];

                remove[lastIndex] = NullKey;
                slots[lastKey] = index;
                remove[index] = lastKey;

                data[index] = data[lastIndex];
            }

            data.RemoveAt(lastIndex);
            slots[key] = NullKey;
        }

        public void Clear()
        {
            data.Clear();
            Array.Fill(slots, NullKey);
        }

        private void Resize(int newCapacity)
        {
            int oldCapacity = slots.Length;

            data.Capacity = newCapacity;
            Array.Resize(ref slots, newCapacity);
            Array.Resize(ref remove, newCapacity);
            Array.Fill(slots, NullKey, oldCapacity, newCapacity - oldCapacity);
        }
    }

    public class ReadOnlySA<TDataType>
    {
        private readonly SparseArray<TDataType> array;

        public int Count 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return array.Count; } 
        }

        public ReadOnlySA(SparseArray<TDataType> array) 
        {
            this.array = array;
        }

        public TDataType this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return array[index]; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int key)
        {
            return array.Contains(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDataType Get(int key)
        {
            return array.Get(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int key, out TDataType item)
        {
            return array.TryGet(key, out item);
        }
    }
}
