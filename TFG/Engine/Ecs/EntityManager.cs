using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Engine.Core;
using TFG;

namespace Engine.Ecs
{
    public class EntityManager<TEntity> where TEntity : EntityBase, new()
    {
        public const int MaxComponentTypes = 64;

        private SparseArray<TEntity> entities;
        private List<CmpStorageBase> cmps;
        private List<int> availableIds;
        private int nextEntityId;

        public EntityManager(int initialCapacity = 1)
        {
            entities     = new SparseArray<TEntity>(initialCapacity);
            cmps         = new List<CmpStorageBase>();
            availableIds = new List<int>();
            nextEntityId = 0;
        }

        #region Entity Management
        public TEntity CreateEntity()
        {
            int id;

            //Generate a new id if there is not one available
            if (availableIds.Count == 0)
            {
                nextEntityId++;
                id = nextEntityId;
            }
            else
            {
                id = availableIds.Last();
                availableIds.RemoveAt(availableIds.Count - 1);
            }

            TEntity entity = new TEntity();
            entity.Id = id;
            entities.Add(id, entity);

            return entity;
        }

        public TEntity GetEntity(int id)
        {
            entities.TryGet(id, out TEntity entity);

            return entity;
        }

        public TEntity GetEntityFromComponentIndex<TCmp>(int index)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            int entityId             = storage.Data.GetKey(index);

            return entities.Get(entityId);
        }

        public void RemoveEntity(TEntity entity)
        {
            Debug.Assert(entity != null && entity.IsValid,
                "Cannot remove invalid or null entity");

            //Remove all the components
            ulong flags = entity.CmpFlags;
            int index = 0;
            while (flags != 0)
            {
                //If it has the component remove it from the list
                if ((flags & 0x1) == 1)
                    cmps[index].Remove(entity.Id);

                index++;
                flags >>= 1;
            }

            entities.Remove(entity.Id);
            availableIds.Add(entity.Id);

            //Invalidate the entity
            entity.Id = EntityBase.NullId;
        }
        #endregion

        #region Component Management
        public int RegisterComponent<TCmp>()
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(id == cmps.Count, 
                "Component \"{0}\" has already been registered", 
                typeof(TCmp).Name);
            Debug.Assert(id < MaxComponentTypes, 
                "Maximun number of registered components reached");

            cmps.Add(new CmpStorage<TCmp>());

            return id;
        }

        public TCmp AddComponent<TCmp>(TEntity entity, TCmp cmp)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid, 
                "Cannot add component to invalid or null entity");
            Debug.Assert(id < cmps.Count, 
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            entity.AddCmpFlag<TCmp>();

            return storage.Data.Add(entity.Id, cmp);
        }

        public ReadOnlyMSA<TCmp> GetComponents<TCmp>()
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            return ((CmpStorage<TCmp>)cmps[id]).Data.AsReadOnly();
        }

        public TCmp GetComponent<TCmp>(TEntity entity, int index = 0)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid,
                "Cannot get component of invalid or null entity");
            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);
            Debug.Assert(entity.HasCmp<TCmp>(),
                "Entity with id \"{0}\" does not have the component \"{1}\"",
                entity.Id, typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            return storage.Data.Get(entity.Id, index);
        }

        public MSAItemList<TCmp> GetComponents<TCmp>(TEntity entity)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid,
                "Cannot get component of invalid or null entity");
            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);
            Debug.Assert(entity.HasCmp<TCmp>(),
                "Entity with id \"{0}\" does not have the component \"{1}\"",
                entity.Id, typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            return storage.Data.GetItemList(entity.Id);
        }

        public bool TryGetComponent<TCmp>(TEntity entity, out TCmp cmp, int index = 0)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid,
                "Cannot get component of invalid or null entity");
            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            return storage.Data.TryGet(entity.Id, index, out cmp);
        }

        public bool TryGetComponents<TCmp>(TEntity entity, out MSAItemList<TCmp> cmpList)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid,
                "Cannot get component of invalid or null entity");
            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            return storage.Data.TryGetItemList(entity.Id, out cmpList);
        }
        #endregion

        #region RemoveComponents
        public void RemoveComponent<TCmp>(TEntity entity)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid,
                "Cannot remove component of invalid or null entity");
            Debug.Assert(id < cmps.Count, 
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);
            Debug.Assert(entity.HasCmp<TCmp>(),
                "Entity with id \"{0}\" does not have the component \"{1}\"",
                entity.Id, typeof(TCmp).Name);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            storage.Data.Remove(entity.Id);
            entity.RemoveCmpFlag<TCmp>();
        }

        public void RemoveComponent<TCmp>(TEntity entity, int index, int count = 1)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(entity != null && entity.IsValid,
                "Cannot remove component of invalid or null entity");
            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);
            Debug.Assert(entity.HasCmp<TCmp>(),
                "Entity with id \"{0}\" does not have the component \"{1}\"",
                entity.Id, typeof(TCmp).Name);
            Debug.Assert(count > 0, "Invalid amount of components to remove ({0})", 
                count);

            CmpStorage<TCmp> storage = (CmpStorage<TCmp>)cmps[id];
            int componentsLeft       = storage.Data.Remove(entity.Id, index, count);

            if (componentsLeft == 0) entity.RemoveCmpFlag<TCmp>();
        }
        #endregion

        public void Clear()
        {
            nextEntityId = 0;
            availableIds.Clear();
            entities.Clear();

            for (int i = 0; i < cmps.Count; ++i)
                cmps[i].Clear();
        }

        #region ForEachComponent
        public void ForEachComponent<TCmp>(Action<TCmp> action)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            var cmpList = ((CmpStorage<TCmp>)cmps[id]).Data;
            for(int i = cmpList.Count - 1; i > -1; --i)
            {
                action(cmpList[i]);
            }
        }

        public void ForEachComponent<TCmp>(Action<TEntity, TCmp> action)
        {
            int id = CmpMetadataGenerator<TCmp>.Id;

            Debug.Assert(id < cmps.Count,
                "Component \"{0}\" has not been registered",
                typeof(TCmp).Name);

            var cmpList = ((CmpStorage<TCmp>)cmps[id]).Data;
            for (int i = cmpList.Count - 1; i > -1; --i)
            {
                TEntity entity = entities.Get(cmpList.GetKey(i));
                action(entity, cmpList[i]);
            }
        }
        #endregion

        #region ForEachEntity without passing components
        public void ForEachEntity(Action<TEntity> action)
        {
            for (int i = entities.Count - 1; i > -1; --i)
            {
                action(entities[i]);
            }
        }

        public void ForEachEntity<TCmp1>
            (Action<TEntity> action)
        {
            ulong flags = CmpMetadataGenerator<TCmp1>.Flag;
            ForEachEntityImpl(action, flags);
        }

        public void ForEachEntity<TCmp1, TCmp2>
            (Action<TEntity> action)
        {
            ulong flags = CmpMetadataGenerator<TCmp1>.Flag |
                          CmpMetadataGenerator<TCmp2>.Flag;
            ForEachEntityImpl(action, flags);
        }

        public void ForEachEntity<TCmp1, TCmp2, TCmp3>
            (Action<TEntity> action)
        {
            ulong flags = CmpMetadataGenerator<TCmp1>.Flag |
                          CmpMetadataGenerator<TCmp2>.Flag |
                          CmpMetadataGenerator<TCmp3>.Flag;
            ForEachEntityImpl(action, flags);
        }

        public void ForEachEntity<TCmp1, TCmp2, TCmp3, TCmp4>
            (Action<TEntity> action)
        {
            ulong flags = CmpMetadataGenerator<TCmp1>.Flag |
                          CmpMetadataGenerator<TCmp2>.Flag |
                          CmpMetadataGenerator<TCmp3>.Flag |
                          CmpMetadataGenerator<TCmp4>.Flag;
            ForEachEntityImpl(action, flags);
        }

        public void ForEachEntity<TCmp1, TCmp2, TCmp3, TCmp4, TCmp5>
            (Action<TEntity> action)
        {
            ulong flags = CmpMetadataGenerator<TCmp1>.Flag |
                          CmpMetadataGenerator<TCmp2>.Flag |
                          CmpMetadataGenerator<TCmp3>.Flag |
                          CmpMetadataGenerator<TCmp4>.Flag |
                          CmpMetadataGenerator<TCmp5>.Flag;
            ForEachEntityImpl(action, flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ForEachEntityImpl(Action<TEntity> action, ulong flags)
        {
            for (int i = entities.Count - 1; i > -1; --i)
            {
                TEntity entity = entities[i];
                if ((entity.CmpFlags & flags) != 0)
                    action(entity);
            }
        }
        #endregion

        #region ForEachEntity passing component lists
        public void ForEachEntity<TCmp1>(Action<TEntity, MSAItemList<TCmp1>> action)
        {
            Debug.Assert(CmpMetadataGenerator<TCmp1>.Id < cmps.Count, 
                "Component \"{0}\" has not been registered", typeof(TCmp1).Name);

            var cmps1 = ((CmpStorage<TCmp1>)cmps[CmpMetadataGenerator<TCmp1>.Id]).Data;
            var flags = CmpMetadataGenerator<TCmp1>.Flag;


            for(int i = entities.Count - 1; i > -1; --i)
            {
                TEntity entity = entities[i];
                if ((entity.CmpFlags & flags) != 0)
                    action(entity, cmps1.GetItemList(entity.Id));
            }
        }

        public void ForEachEntity<TCmp1, TCmp2>
            (Action<TEntity, MSAItemList<TCmp1>, MSAItemList<TCmp2>> action)
        {
            Debug.Assert(CmpMetadataGenerator<TCmp1>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp1).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp2>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp2).Name);

            var cmps1 = ((CmpStorage<TCmp1>)cmps[CmpMetadataGenerator<TCmp1>.Id]).Data;
            var cmps2 = ((CmpStorage<TCmp2>)cmps[CmpMetadataGenerator<TCmp2>.Id]).Data;
            var flags = CmpMetadataGenerator<TCmp1>.Flag |
                        CmpMetadataGenerator<TCmp2>.Flag;

            for (int i = entities.Count - 1; i > -1; --i)
            {
                TEntity entity = entities[i];
                if ((entity.CmpFlags & flags) != 0)
                    action(entity, cmps1.GetItemList(entity.Id),
                        cmps2.GetItemList(entity.Id));
            }
        }

        public void ForEachEntity<TCmp1, TCmp2, TCmp3>
            (Action<TEntity, MSAItemList<TCmp1>, MSAItemList<TCmp2>, 
                MSAItemList<TCmp3>> action)
        {
            Debug.Assert(CmpMetadataGenerator<TCmp1>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp1).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp2>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp2).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp3>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp3).Name);

            var cmps1 = ((CmpStorage<TCmp1>)cmps[CmpMetadataGenerator<TCmp1>.Id]).Data;
            var cmps2 = ((CmpStorage<TCmp2>)cmps[CmpMetadataGenerator<TCmp2>.Id]).Data;
            var cmps3 = ((CmpStorage<TCmp3>)cmps[CmpMetadataGenerator<TCmp3>.Id]).Data;
            var flags = CmpMetadataGenerator<TCmp1>.Flag |
                        CmpMetadataGenerator<TCmp2>.Flag |
                        CmpMetadataGenerator<TCmp3>.Flag;

            for (int i = entities.Count - 1; i > -1; --i)
            {
                TEntity entity = entities[i];
                if ((entity.CmpFlags & flags) != 0)
                    action(entity, cmps1.GetItemList(entity.Id),
                        cmps2.GetItemList(entity.Id), 
                        cmps3.GetItemList(entity.Id));
            }
        }

        public void ForEachEntity<TCmp1, TCmp2, TCmp3, TCmp4>
            (Action<TEntity, MSAItemList<TCmp1>, MSAItemList<TCmp2>,
                MSAItemList<TCmp3>, MSAItemList<TCmp4>> action)
        {
            Debug.Assert(CmpMetadataGenerator<TCmp1>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp1).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp2>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp2).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp3>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp3).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp4>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp4).Name);

            var cmps1 = ((CmpStorage<TCmp1>)cmps[CmpMetadataGenerator<TCmp1>.Id]).Data;
            var cmps2 = ((CmpStorage<TCmp2>)cmps[CmpMetadataGenerator<TCmp2>.Id]).Data;
            var cmps3 = ((CmpStorage<TCmp3>)cmps[CmpMetadataGenerator<TCmp3>.Id]).Data;
            var cmps4 = ((CmpStorage<TCmp4>)cmps[CmpMetadataGenerator<TCmp4>.Id]).Data;
            var flags = CmpMetadataGenerator<TCmp1>.Flag |
                        CmpMetadataGenerator<TCmp2>.Flag |
                        CmpMetadataGenerator<TCmp3>.Flag |
                        CmpMetadataGenerator<TCmp4>.Flag;

            for (int i = entities.Count - 1; i > -1; --i)
            {
                TEntity entity = entities[i];
                if ((entity.CmpFlags & flags) != 0)
                    action(entity, cmps1.GetItemList(entity.Id),
                        cmps2.GetItemList(entity.Id),
                        cmps3.GetItemList(entity.Id),
                        cmps4.GetItemList(entity.Id));
            }
        }

        public void ForEachEntity<TCmp1, TCmp2, TCmp3, TCmp4, TCmp5>
            (Action<TEntity, MSAItemList<TCmp1>, MSAItemList<TCmp2>,
                MSAItemList<TCmp3>, MSAItemList<TCmp4>,
                MSAItemList<TCmp5>> action)
        {
            Debug.Assert(CmpMetadataGenerator<TCmp1>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp1).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp2>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp2).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp3>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp3).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp4>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp4).Name);
            Debug.Assert(CmpMetadataGenerator<TCmp5>.Id < cmps.Count,
                "Component \"{0}\" has not been registered", typeof(TCmp5).Name);

            var cmps1 = ((CmpStorage<TCmp1>)cmps[CmpMetadataGenerator<TCmp1>.Id]).Data;
            var cmps2 = ((CmpStorage<TCmp2>)cmps[CmpMetadataGenerator<TCmp2>.Id]).Data;
            var cmps3 = ((CmpStorage<TCmp3>)cmps[CmpMetadataGenerator<TCmp3>.Id]).Data;
            var cmps4 = ((CmpStorage<TCmp4>)cmps[CmpMetadataGenerator<TCmp4>.Id]).Data;
            var cmps5 = ((CmpStorage<TCmp5>)cmps[CmpMetadataGenerator<TCmp5>.Id]).Data;
            var flags = CmpMetadataGenerator<TCmp1>.Flag |
                        CmpMetadataGenerator<TCmp2>.Flag |
                        CmpMetadataGenerator<TCmp3>.Flag |
                        CmpMetadataGenerator<TCmp4>.Flag |
                        CmpMetadataGenerator<TCmp5>.Flag;

            for (int i = entities.Count - 1; i > -1; --i)
            {
                TEntity entity = entities[i];
                if ((entity.CmpFlags & flags) != 0)
                    action(entity, cmps1.GetItemList(entity.Id),
                        cmps2.GetItemList(entity.Id),
                        cmps3.GetItemList(entity.Id),
                        cmps4.GetItemList(entity.Id),
                        cmps5.GetItemList(entity.Id));
            }
        }
        #endregion
    };
}
