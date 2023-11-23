using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Engine.Core;

namespace Engine.Ecs
{
    public class MultiSparseArray<TDataType>
    {
        public struct Slot
        {
            public int Index;
            public int Count;
        }

        public const int NullKey = -1;

        private Slot[] slots;
        private List<TDataType> data;
        private List<int> remove;

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

        public MultiSparseArray(int initialCapacity = 1)
        {
            slots = new Slot[initialCapacity];
            data = new List<TDataType>(initialCapacity);
            remove = new List<int>(initialCapacity);

            Array.Fill(slots, new Slot { Index = NullKey, Count = 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMSA<TDataType> AsReadOnly()
        {
            return new ReadOnlyMSA<TDataType>(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int key)
        {
            return key < slots.Length && slots[key].Count != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetKey(int index)
        {
            Debug.Assert(index > -1 && index < remove.Count && remove[index] != NullKey,
                "Cannot get key with index {0}. Index out of bounds", index);
            return remove[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetItemCount(int key)
        {
            Debug.Assert(Contains(key),
                "Cannot get item count with key {0}. Key not found", key);

            return slots[key].Count;
        }

        public MSAItemList<TDataType> GetItemList(int key)
        {
            Debug.Assert(Contains(key),
                "Cannot get item list with key {0}. Key not found", key);

            Slot slot = slots[key];
            return new MSAItemList<TDataType>(data, slot.Count, slot.Index);
        }

        public bool TryGetItemList(int key,
            out MSAItemList<TDataType> list)
        {
            if (Contains(key))
            {
                Slot slot = slots[key];
                list = new MSAItemList<TDataType>(data, slot.Count, slot.Index);
                return true;
            }

            list = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDataType Get(int key)
        {
            Debug.Assert(Contains(key),
                "Cannot get item with key {0}. Key not found", key);

            return data[slots[key].Index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDataType Get(int key, int index)
        {
            Debug.Assert(Contains(key),
                "Cannot get item with key {0}. Key not found", key);
            Debug.Assert(index > -1 && index < slots[key].Count,
                "Cannot get item with key {0} at index {1}. " +
                "Index out of bounds", key, index);

            return data[slots[key].Index + index];
        }

        public bool TryGet(int key, out TDataType item)
        {
            if (Contains(key))
            {
                item = data[slots[key].Index];
                return true;
            }

            item = default;
            return false;
        }

        public bool TryGet(int key, int index, out TDataType item)
        {
            if (Contains(key))
            {
                Slot slot = slots[key];
                if (index < slot.Count)
                {
                    item = data[slots[key].Index + index];
                    return true;
                }
            }

            item = default;
            return false;
        }

        public TDataType Add(int key, TDataType item)
        {
            if (key >= slots.Length)
                Resize(slots.Length + key + 1);

            Slot slot = slots[key];

            //Is new data
            if (slot.Count == 0)
            {
                slot.Index = data.Count;
                slot.Count = 1;

                slots[key] = slot;
                data.Add(item);
                remove.Add(key);
            }
            else
            {
                //Increase the index by one of every slot that is next
                //to the one that is being modified
                int insertIndex = slot.Index + slot.Count;
                int removeIndex = insertIndex;
                while (removeIndex < remove.Count)
                {
                    int slotIndex = remove[removeIndex];
                    Slot nextSlot = slots[slotIndex];
                    nextSlot.Index++;
                    slots[slotIndex] = nextSlot;

                    removeIndex += nextSlot.Count;
                }


                data.Insert(insertIndex, item);
                remove.Insert(insertIndex, key);

                slot.Count++;
                slots[key] = slot;

            }

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Remove(int key)
        {
            Debug.Assert(Contains(key),
                "Cannot remove item with key {0}. Key not found", key);

            Slot slot = slots[key];
            return RemoveRange(slot, key, 0, slot.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Remove(int key, int index)
        {
            Debug.Assert(Contains(key),
                "Cannot remove item with key {0}. Key not found", key);

            Slot slot = slots[key];
            return RemoveRange(slot, key, index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Remove(int key, int index, int count)
        {
            Debug.Assert(Contains(key),
                "Cannot remove item with key {0}. Key not found", key);

            Slot slot = slots[key];
            return RemoveRange(slot, key, index, count);
        }

        private int RemoveRange(Slot slot, int key, int index, int count)
        {
            Debug.Assert(count > -1, "Invalid count value of {0}", count);
            Debug.Assert(index > -1, "Invalid index value of {0}", index);
            Debug.Assert(index + count <= slot.Count,
                "Cannot remove {0} items at key {1} starting at index {2}. " +
                "Key only has {3} items",
                count, key, index, slot.Count);

            //Decrease the index of every slot next to the one being removed
            //by the amount of items being removed
            int removeIndex = slot.Index + slot.Count;
            while (removeIndex < remove.Count)
            {
                int slotIndex = remove[removeIndex];
                Slot nextSlot = slots[slotIndex];
                nextSlot.Index -= count;
                slots[slotIndex] = nextSlot;

                removeIndex += nextSlot.Count;
            }

            data.RemoveRange(slot.Index + index, count);
            remove.RemoveRange(slot.Index + index, count);

            slot.Count -= count;
            slots[key] = slot;

            return slot.Count;
        }

        public void Clear()
        {
            data.Clear();
            remove.Clear();
            Array.Fill(slots, new Slot { Index = NullKey, Count = 0 });
        }

        private void Resize(int newCapacity)
        {
            int oldCapacity = slots.Length;

            data.Capacity = newCapacity;
            remove.Capacity = newCapacity;

            Array.Resize(ref slots, newCapacity);
            Array.Fill(slots, new Slot { Index = NullKey, Count = 0 },
                oldCapacity, newCapacity - oldCapacity);
        }
    }

    public class ReadOnlyMSA<TDataType>
    {
        private MultiSparseArray<TDataType> array;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return array.Count; }
        }

        public TDataType this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return array[index]; }
        }

        internal ReadOnlyMSA(MultiSparseArray<TDataType> array)
        {
            this.array = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int key)
        {
            return array.Contains(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetKey(int index)
        {
            return array.GetKey(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetItemCount(int key)
        {
            return array.GetItemCount(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MSAItemList<TDataType> GetItemList(int key)
        {
            return array.GetItemList(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetItemList(int key,
            out MSAItemList<TDataType> list)
        {
            return array.TryGetItemList(key, out list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDataType Get(int key)
        {
            return array.Get(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDataType Get(int key, int index)
        {
            return array.Get(key, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int key, out TDataType item)
        {
            return array.TryGet(key, out item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int key, int index, out TDataType item)
        {
            return array.TryGet(key, index, out item);
        }
    }

    public class MSAItemList<TDataType>
    {
        private List<TDataType> data;
        private int count;
        private int startIndex;

        public int Count { get { return count; } }

        public TDataType this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(index < count, "Index {0} out of range", index);
                return data[startIndex + index];
            }
        }

        internal MSAItemList(List<TDataType> data, int count, int startIndex)
        {
            this.data = data;
            this.count = count;
            this.startIndex = startIndex;
        }
    }
}
