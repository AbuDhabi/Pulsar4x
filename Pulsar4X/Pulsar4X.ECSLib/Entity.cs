﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [PublicAPI]
    public class ProtoEntity
    {
        [PublicAPI]
        public Guid Guid { get; protected set; }

        [PublicAPI]
        public List<BaseDataBlob> DataBlobs { get; set; }

        [NotNull]
        [PublicAPI]
        public ComparableBitArray DataBlobMask { get { return _protectedDataBlobMask_; } }
        protected ComparableBitArray _protectedDataBlobMask_ = EntityManager.BlankDataBlobMask();

        [PublicAPI]
        public static ProtoEntity Create(Guid guid, IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            ProtoEntity protoEntity = new ProtoEntity
            {
                DataBlobs = EntityManager.BlankDataBlobList(),
                Guid = guid
            };

            if (dataBlobs == null)
            {
                return protoEntity;
            }
            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                protoEntity.SetDataBlob(dataBlob);
            }

            return protoEntity;
        }

        [PublicAPI]
        public static ProtoEntity Create(IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            return Create(Guid.Empty, dataBlobs);
        }

        [PublicAPI]
        public virtual T GetDataBlob<T>() where T : BaseDataBlob
        {
            return (T)DataBlobs[EntityManager.GetTypeIndex<T>()];
        }

        [PublicAPI]
        public virtual T GetDataBlob<T>(int typeIndex) where T : BaseDataBlob
        {
            return (T)DataBlobs[typeIndex];
        }

        [PublicAPI]
        public virtual void SetDataBlob<T>(T dataBlob) where T : BaseDataBlob
        {
            int typeIndex;
            EntityManager.TryGetTypeIndex(dataBlob.GetType(), out typeIndex);
            DataBlobs[typeIndex] = dataBlob;
            _protectedDataBlobMask_[typeIndex] = true;
        }

        [PublicAPI]
        public virtual void SetDataBlob<T>(T dataBlob, int typeIndex) where T : BaseDataBlob
        {
            DataBlobs[typeIndex] = dataBlob;
            _protectedDataBlobMask_[typeIndex] = true;
        }

        [PublicAPI]
        public virtual void RemoveDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            DataBlobs[typeIndex] = null;
            _protectedDataBlobMask_[typeIndex] = false;
        }

        [PublicAPI]
        public virtual void RemoveDataBlob(int typeIndex)
        {
            DataBlobs[typeIndex] = null;
            _protectedDataBlobMask_[typeIndex] = false;
        }
    }

    [JsonConverter(typeof(EntityConverter))]
    public sealed class Entity : ProtoEntity
    {
        internal int ID;

        [NotNull]
        [JsonIgnore]
        public EntityManager Manager { get; private set; }

        [NotNull]
        [PublicAPI]
        public new ReadOnlyCollection<BaseDataBlob> DataBlobs { get { return IsValid ? new ReadOnlyCollection<BaseDataBlob>(Manager.GetAllDataBlobs(ID)) : new ReadOnlyCollection<BaseDataBlob>(new List<BaseDataBlob>()); } }

        private static readonly EntityManager InvalidManager = new EntityManager(null);

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

        internal Entity([NotNull] EntityManager manager, [NotNull] ProtoEntity protoEntity) : this(manager, Guid.NewGuid(), protoEntity.DataBlobs) { }
        #endregion

        #region Public API Functions
        /// <summary>
        /// Used to determine if an entity is valid.
        /// 
        /// Entities are considered valid if they are not the static InvalidEntity and are properly registered to a manager.
        /// </summary>
        [PublicAPI]
        public bool IsValid
        {
            get { return this != InvalidEntity && Manager != InvalidManager && Manager.IsValidEntity(this); }
        }

        /// <summary>
        /// Creates a new entity with a randomly generated Guid, registered with the provided manager with the optionally provided dataBlobs.
        /// </summary>
        [PublicAPI]
        public static Entity Create([NotNull] EntityManager manager, [CanBeNull] IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            return new Entity(manager, dataBlobs);
        }

        public static Entity Create(EntityManager manager, ProtoEntity protoEntity)
        {
            return Create(manager, protoEntity.DataBlobs);
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
                throw new ArgumentNullException("dataBlob", "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot set a datablob to an invalid entity.");
            }

            Manager.SetDataBlob(ID, dataBlob);
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
                throw new ArgumentNullException("dataBlob", "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot set a datablob to an invalid entity.");
            }

            Manager.SetDataBlob(ID, dataBlob, typeIndex);
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
            return ProtoEntity.Create(clonedDataBlobs);
        }

        /// <summary>
        /// Clones the entity into a new Entity in the specified entit
        /// </summary>
        /// <param name="manager"></param>
        /// <returns>The newly created entity.</returns>
        [PublicAPI]
        public Entity Clone(EntityManager manager)
        {
            return new Entity(manager, Clone());
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
                throw new ArgumentNullException("newManager");
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
        /// EntityConverter responsible for deserializng Entity objects that are not part of an EntityManager.
        /// EntityManagers serialize Entities directly.
        /// </summary>
        internal class EntityConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Entity);
            }

            /// <summary>
            /// Returns a Entity object that represents the entity.
            /// If the Entity's manager has already deserialized the entity, then the EntityManager's reference is returned.
            /// If not, then we create the entity in the global manager, and when the EntityManager containing this entity deserializes,
            /// it will transfer the entity to itself.
            /// </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Entity entity;

                // Parse the Guid from the reader.
                Guid entityGuid = Guid.Parse(reader.Value.ToString());

                // Lookup the entity using a global Guid lookup.
                if (entityGuid == Guid.Empty)
                    return InvalidEntity;
                if (SaveGame.CurrentGame.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                    return entity;

                // If no entity was found, create a new entity in the global manager.
                entity = new Entity(SaveGame.CurrentGame.GlobalManager, entityGuid);
                return entity;
            }

            /// <summary>
            /// Serializes the Entity objects. Entities are serialized as simple Guids in this method.
            /// Datablobs are saved during EntityManager serialization.
            /// </summary>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Entity entity = (Entity)value;
                serializer.Serialize(writer, entity.Guid);
            }
        }
    }
}
