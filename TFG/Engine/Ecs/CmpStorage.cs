using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Ecs
{
    public abstract class CmpStorageBase
    {
        public abstract void Remove(int id);
        public abstract void Clear();
    }

    public class CmpStorage<T> : CmpStorageBase
    {
        public MultiSparseArray<T> Data;

        public CmpStorage()
        {
            Data = new MultiSparseArray<T>();
        }

        public override void Remove(int id)
        {
            Data.Remove(id);
        }

        public override void Clear()
        {
            Data.Clear();
        }
    }


}
