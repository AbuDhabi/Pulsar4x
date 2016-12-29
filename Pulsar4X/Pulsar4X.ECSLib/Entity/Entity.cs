﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.ComponentModel;

namespace Pulsar4X.ECSLib
{
    public enum EntityChangedAction
    {
        DataBlobAdded,
        DataBlobRemoved,
        EntityDestroyed
    }

    public delegate void EntityChangedHandler(EntityChangedAction action, Type datablob);
    
    [JsonConverter(typeof(EntityConverter))]
    [PublicAPI]
    public sealed class Entity : ProtoEntity
    {
        /// <summary>
        /// Occurs when entity changed. fires on the primay (UI) thread, don't listen to this within ECSLib.
        /// if we find we need to listen to this from within ECSLib, then we can move the SynchContext stuff to the VM
        /// </summary>
        public event EntityChangedHandler EntityChanged;
        private readonly SynchronizationContext _context;


        // Index slot of this entity's datablobs in its EntityManager.
        internal int ID;

        [NotNull]
        [JsonIgnore]
        public EntityManager Manager { get; private set; }

        [NotNull]
        [PublicAPI]
        public new ReadOnlyCollection<BaseDataBlob> DataBlobs => IsValid ? new ReadOnlyCollection<BaseDataBlob>(Manager.GetAllDataBlobs(ID)) : new ReadOnlyCollection<BaseDataBlob>(new List<BaseDataBlob>());

        private static readonly EntityManager InvalidManager = EntityManager.InvalidManager;

        /// <summary>
        /// Static entity reference to an invalid entity.
        /// 
        /// Functions must never return a null entity. Instead, return InvalidEntity.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public static readonly Entity InvalidEntity = new Entity();

        #region Entity Constructors
        private Entity()
        {
            Manager = InvalidManager;
            _context = AsyncOperationManager.SynchronizationContext;
        }

        internal Entity([NotNull] EntityManager manager, IEnumerable<BaseDataBlob> dataBlobs = null) : this(manager, Guid.NewGuid(), dataBlobs) { }

        internal Entity([NotNull] EntityManager manager, Guid id, IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            
            Manager = manager;
            Guid = id;

            Entity checkEntity;
            while (Guid == Guid.Empty || manager.FindEntityByGuid(Guid, out checkEntity))
            {
                Guid = Guid.NewGuid();
            }

            ID = Manager.SetupEntity(this);
            _protectedDataBlobMask_ = Manager.EntityMasks[ID];

            if (dataBlobs == null)
            {
                return;
            }

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                if (dataBlob != null)
                {
                    SetDataBlob(dataBlob);
                }
            }
        }

        internal Entity([NotNull] EntityManager manager, [NotNull] ProtoEntity protoEntity) : this(manager, protoEntity.Guid, protoEntity.DataBlobs) { }
        #endregion

        #region Public API Functions
        /// <summary>
        /// Used to determine if an entity is valid.
        /// 
        /// Entities are considered valid if they are not the static InvalidEntity and are properly registered to a manager.
        /// </summary>
        [PublicAPI]
        public bool IsValid => this != InvalidEntity && Manager != InvalidManager && Manager.IsValidEntity(this);

        /// <summary>
        /// Creates a new entity with a randomly generated Guid, registered with the provided manager with the optionally provided dataBlobs.
        /// </summary>
        [PublicAPI]
        public static Entity Create([NotNull] EntityManager manager, [CanBeNull] IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            return new Entity(manager, dataBlobs);
        }

        public static Entity Create(EntityManager manager, ProtoEntity protoEntity)
        {
            return new Entity(manager, protoEntity.Guid, protoEntity.DataBlobs);
        }

        [PublicAPI]
        public void Destroy()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot destroy an invalid entity.");
            }

            Manager.RemoveEntity(this);
            Manager = InvalidManager;
            _protectedDataBlobMask_ = EntityManager.BlankDataBlobMask();

            if(EntityChanged != null && _context != null)
                _context.Post(s => EntityChanged(EntityChangedAction.EntityDestroyed, null), null);

        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than GetDataBlob(int typeIndex)
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        [PublicAPI]
        public override T GetDataBlob<T>()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot get a datablob from an invalid entity.");
            }
            return Manager.GetDataBlob<T>(ID);
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than directly accessing the DataBlob list.
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entityID is passed.</exception>
        [PublicAPI]
        public override T GetDataBlob<T>(int typeIndex)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot get a datablob from an invalid entity.");
            }
            return Manager.GetDataBlob<T>(ID, typeIndex);
        }

        /// <summary>
        /// Sets the dataBlob to this entity. Slightly slower than SetDataBlob(dataBlob, typeIndex);
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        /// <exception cref="ArgumentNullException">Thrown is dataBlob is null.</exception>
        [PublicAPI]
        public override void SetDataBlob<T>([NotNull] T dataBlob)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException(nameof(dataBlob), "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot set a datablob to an invalid entity.");
            }

            Manager.SetDataBlob(ID, dataBlob);
            if(EntityChanged != null && _context != null)
                _context.Post(s => EntityChanged(EntityChangedAction.DataBlobAdded, dataBlob.GetType()), null);
        }

        /// <summary>
        /// Sets the dataBlob to this entity. Slightly faster than SetDataBlob(dataBlob);
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown is dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if typeIndex is not a valid typeIndex.</exception>
        [PublicAPI]
        public override void SetDataBlob<T>([NotNull] T dataBlob, int typeIndex)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException(nameof(dataBlob), "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot set a datablob to an invalid entity.");
            }

            Manager.SetDataBlob(ID, dataBlob, typeIndex);
            if(EntityChanged != null && _context != null)
                _context.Post(s => EntityChanged(EntityChangedAction.DataBlobAdded, dataBlob.GetType()), null);
        }

        /// <summary>
        /// Removes a dataBlob from this entity. Slightly slower than RemoveDataBlob(typeIndex);
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        [PublicAPI]
        public override void RemoveDataBlob<T>()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot remove a datablob from an invalid entity.");
            }
            if (!HasDataBlob<T>())
            {
                throw new InvalidOperationException("Entity does not contain this datablob.");
            }
            Manager.RemoveDataBlob<T>(ID);
            if(EntityChanged != null && _context != null)
                _context.Post(s => EntityChanged(EntityChangedAction.DataBlobRemoved, typeof(T)), null);
        }

        /// <summary>
        /// Removes a dataBlob from this entity. Slightly faster than the generic RemoveDataBlob(); function.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if typeIndex is not a valid typeIndex.</exception>
        [PublicAPI]
        public override void RemoveDataBlob(int typeIndex)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot remove a datablob from an invalid entity.");
            } 
            if (!HasDataBlob(typeIndex))
            {
                throw new InvalidOperationException("Entity does not contain this datablob.");
            }
            Manager.RemoveDataBlob(ID, typeIndex);
            if(EntityChanged != null && _context != null)
                _context.Post(s => EntityChanged(EntityChangedAction.DataBlobRemoved, DataBlobMask[typeIndex].GetType()), null);
        }

        /// <summary>
        /// Checks if this entity has a DataBlob of type T.
        /// </summary>
        /// <typeparam name="T">Type of datablob to check for.</typeparam>
        /// <returns>True if the entity has the datablob.</returns>
        [PublicAPI]
        public bool HasDataBlob<T>() where T : BaseDataBlob
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot query an invalid entity.");
            }
            int typeIndex = EntityManager.GetTypeIndex<T>();
            return DataBlobMask[typeIndex];
        }

        /// <summary>
        /// Checks if this entity has a DataBlob of the type indicated by the provided typeIndex.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when typeIndex is not a valid typeIndex.</exception>
        /// <returns>True if the entity has the datablob.</returns>
        [PublicAPI]
        public bool HasDataBlob(int typeIndex)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot query an invalid entity.");
            }
            return DataBlobMask[typeIndex];
        }

        /// <summary>
        /// Clones the entity's dataBlobs into a new ProtoEntity.
        /// </summary>
        /// <returns>A new ProtoEntity with cloned datablobs from this entity.</returns>
        [PublicAPI]
        public ProtoEntity Clone()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot clone an invalid entity.");
            }
            List<BaseDataBlob> clonedDataBlobs = DataBlobs.Select(dataBlob => dataBlob.Clone()).Cast<BaseDataBlob>().ToList();
            return Create(Guid, clonedDataBlobs);
        }

        /// <summary>
        /// Clones the entity into a new Entity in the specified entit
        /// </summary>
        /// <param name="manager"></param>
        /// <returns>The newly created entity.</returns>
        [PublicAPI]
        public Entity Clone(EntityManager manager)
        {
            ProtoEntity clone = Clone();
            clone.Guid = Guid.NewGuid();
            return new Entity(manager, clone);
        }

        #endregion

        /// <summary>
        /// Simple override to display entities as their Guid.
        /// 
        /// Used mostly in debugging.
        /// </summary>
        public override string ToString()
        {
            return Guid.ToString();
        }

        /// <summary>
        /// Used to transfer an entity between managers.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to transfer the static InvalidEntity.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the provided manager is null.</exception>
        public void Transfer([NotNull] EntityManager newManager)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot transfer an invalid entity.");
            }
            if (newManager == null)
            {
                throw new ArgumentNullException(nameof(newManager));
            }
            // Store dataBlobs.
            List<BaseDataBlob> dataBlobs = Manager.GetAllDataBlobs(ID);

            // Remove myself from the old manager.
            Manager.RemoveEntity(this);

            // Add myself the the new manager.
            ID = newManager.SetupEntity(this);
            Manager = newManager;
            _protectedDataBlobMask_ = Manager.EntityMasks[ID];

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                SetDataBlob(dataBlob);
            }
        }

        /// <summary>
        /// EntityConverter is responsible for deserializing entities when they are encountered as references. 
        /// The EntityConverter must provide a proper reference to the object being deserialized.
        /// </summary>
        private class EntityConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Entity);
            }

            /// <summary>
            /// Returns a Entity object that represents the entity.
            /// If the Entity's manager has already deserialized the entity, then the EntityManager's reference is returned.
            /// If not, then we create the entity in the global manager, and when the EntityManager containing this entity deserializes,
            /// it will transfer the entity (that we create here) to itself. This will preserve all Entity references already deserialized.
            /// </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Game game = (Game)serializer.Context.Context;
                Entity entity;

                // Parse the Guid from the reader.
                Guid entityGuid = Guid.Parse(reader.Value.ToString());

                // Lookup the entity using a global Guid lookup.
                if (entityGuid == Guid.Empty)
                    return InvalidEntity;
                if (game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                    return entity;

                // If no entity was found, create a new entity in the global manager.
                entity = new Entity(game.GlobalManager, entityGuid);
                return entity;
            }

            /// <summary>
            /// Serializes the Entity objects. Entities are serialized as simple Guids in this method.
            /// Datablobs are saved during EntityManager serialization.
            /// </summary>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var entity = (Entity)value;
                serializer.Serialize(writer, entity.Guid);
            }
        }
    }
}
