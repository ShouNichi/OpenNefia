﻿using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.SaveGames;
using System.Diagnostics.CodeAnalysis;

namespace OpenNefia.Core.Maps
{
    public enum MapLoadType
    {
        /// <summary>
        /// This map is being initialized but will not be entered. Only used when a new game is started.
        /// </summary>
        InitializeOnly,

        /// <summary>
        /// This map was loaded as part of a save game, so do not run all initialization events
        /// (geometry renewal, etc.).
        /// </summary>
        GameLoaded,

        /// <summary>
        /// The same as <see cref="Full"/>, but does not clear the message window when the map is entered.
        /// </summary>
        Traveled,

        /// <summary>
        /// Run all initialization events.
        /// </summary>
        Full
    }
    
    /// <summary>
    /// What behavior to run when a map is unloaded. This will control which events are
    /// fired on the map entity to indicate resource cleanup.
    /// </summary>
    public enum MapUnloadType
    {
        /// <summary>
        /// The map can be loaded at a later date.
        /// </summary>
        Unload,

        /// <summary>
        /// The map will be deleted and never loaded again.
        /// </summary>
        Delete
    }

    public delegate void ActiveMapChangedDelegate(IMap newMap, IMap? oldMap, MapLoadType loadType);

    public interface IMapManager
    {
        IMap? ActiveMap { get; }

        /// <summary>
        /// All maps that are currently loaded in memory.
        /// </summary>
        IReadOnlyDictionary<MapId, IMap> LoadedMaps { get; }

        event ActiveMapChangedDelegate? OnActiveMapChanged;

        void SetActiveMap(MapId mapId, MapLoadType loadType = MapLoadType.Full);

        bool IsMapInitialized(MapId mapId);

        bool MapIsLoaded(MapId mapId);
        IMap CreateMap(int width, int height, PrototypeId<EntityPrototype>? mapEntityProto = null);
        IMap CreateMap(int width, int height, MapId mapId, PrototypeId<EntityPrototype>? mapEntityProto = null);

        /// <summary>
        /// Sets the MapEntity(root node) for a given map. If an entity is already set, it will be deleted
        /// before the new one is set.
        /// </summary>
        void SetMapEntity(MapId mapId, EntityUid newMapEntityId);

        IMap GetMap(MapId mapId);
        IMap GetMapOfEntity(EntityUid entity);

        bool TryGetMap(MapId mapId, [NotNullWhen(true)] out IMap? map);
        bool TryGetMapOfEntity(EntityUid entity, [NotNullWhen(true)] out IMap? map);

        void UnloadMap(MapId mapId, MapUnloadType unloadType = MapUnloadType.Unload);

        void RefreshVisibility(IMap map);

        /// <summary>
        /// Allocates a new MapID, incrementing the highest ID counter.
        /// </summary>
        MapId GenerateMapId();
    }
}