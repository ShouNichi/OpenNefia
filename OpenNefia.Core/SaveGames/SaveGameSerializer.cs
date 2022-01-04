﻿using OpenNefia.Core.ContentPack;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Log;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Reflection;
using OpenNefia.Core.Serialization.Manager;
using OpenNefia.Core.Serialization.Manager.Attributes;
using OpenNefia.Core.Serialization.Markdown;
using OpenNefia.Core.Serialization.Markdown.Mapping;
using OpenNefia.Core.Serialization.Markdown.Value;
using OpenNefia.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Core.SaveGames
{
    public delegate void GameSavedDelegate(ISaveGameHandle save);
    public delegate void GameLoadedDelegate(ISaveGameHandle save);

    /// <summary>
    /// Handles converting the serializable data of the current game state
    /// into a loadable format. This wraps <see cref="ISaveGameManager"/> by adding
    /// game save format support.
    /// </summary>
    public interface ISaveGameSerializer
    {
        event GameSavedDelegate OnGameSaved;
        event GameLoadedDelegate OnGameLoaded;

        /// <summary>
        /// Saves the game state to the provided save.
        /// </summary>
        void SaveGame(ISaveGameHandle save);

        /// <summary>
        /// Loads the game state from the provided save.
        /// </summary>
        void LoadGame(ISaveGameHandle save);
    }

    internal interface ISaveGameSerializerInternal : ISaveGameSerializer
    {
        void Initialize();

        void SaveGlobalData(ISaveGameHandle save);
        void LoadGlobalData(ISaveGameHandle save);
    }

    /// <summary>
    /// Global, engine-internal data that isn't part of either a map or an entity system.
    /// These are spread across various IoC dependencies and manually set up on save/load.
    /// </summary>
    [DataDefinition]
    internal class SessionData
    {
        /// <summary>
        /// Current player at the time of saving.
        /// </summary>
        /// <see cref="IGameSessionManager"/>
        // TODO: no EntityUid serializer...
        [DataField(required: true)]
        public int PlayerUid { get; set; }

        /// <summary>
        /// Current map at the time of saving.
        /// </summary>
        /// <see cref="IMapManager"/>
        [DataField(required: true)]
        public MapId ActiveMapId { get; set; }

        /// <summary>
        /// Highest free map ID at the time of saving.
        /// </summary>
        /// <see cref="IMapManagerInternal"/>
        [DataField(required: true)]
        public MapId HighestMapId { get; set; }
    }

    public sealed class SaveGameSerializer : ISaveGameSerializerInternal
    {
        [Dependency] private readonly IEntityManagerInternal _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IGameSessionManager _gameSessionManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;

        public const string SawmillName = "save.game";

        public event GameSavedDelegate? OnGameSaved;
        public event GameLoadedDelegate? OnGameLoaded;

        private class SaveDataRegistration
        {
            public AbstractFieldInfo FieldInfo { get; }
            public object Parent { get; }

            public Type Type => FieldInfo.FieldType;

            public SaveDataRegistration(AbstractFieldInfo propertyInfo, object parent)
            {
                FieldInfo = propertyInfo;
                Parent = parent;
            }
        }

        private Dictionary<string, SaveDataRegistration> _trackedSaveData = new();

        public void Initialize()
        {
            foreach (var type in _entitySystemManager.SystemTypes)
            {
                if (_entitySystemManager.TryGetEntitySystem(type, out var sys))
                {
                    foreach (var info in sys.GetType().GetAllPropertiesAndFields())
                    {
                        if (info.TryGetAttribute(out RegisterSaveDataAttribute? attr))
                        {
                            var value = info.GetValue(sys);
                            if (value == null)
                                throw new InvalidDataException($"Expected non-nullable reference for save data '{attr.Key}', got null.");

                            RegisterSaveData(attr.Key, info, sys);
                        }
                    }
                }
            }
        }

        private void RegisterSaveData(string key, AbstractFieldInfo field, object parent)
        {
            var ty = field.FieldType;

            if (field is SpecificPropertyInfo propertyInfo)
            {
                if (propertyInfo.PropertyInfo.GetMethod == null)
                {
                    throw new ArgumentException($"Property {propertyInfo} is annotated with {nameof(RegisterSaveDataAttribute)} but has no getter");
                }
                else if (propertyInfo.PropertyInfo.SetMethod == null)
                {
                    if (!propertyInfo.HasBackingField())
                    {
                        throw new ArgumentException($"Property {propertyInfo} in type {propertyInfo.DeclaringType} is annotated with {nameof(RegisterSaveDataAttribute)} as non-readonly but has no auto-setter");
                    }
                }
            }
            if (_trackedSaveData.ContainsKey(key))
            {
                throw new ArgumentException($"Save data reference '{key}' was already registered (type: {ty})", nameof(key));
            }
            if (!_serializationManager.HasDataDefinition(ty))
            {
                throw new ArgumentException($"Type '{ty}' does not have a data definition.", nameof(ty));
            }

            Logger.DebugS(SawmillName, $"Registering global save data: {key} ({ty})");
            _trackedSaveData.Add(key, new SaveDataRegistration(field, parent));
        }

        /// <inheritdoc/>
        public void SaveGame(ISaveGameHandle save)
        {
            SaveGlobalData(save);
            SaveSession(save);
            SaveActiveMap(save);

            OnGameSaved?.Invoke(save);

            save.Files.Commit();
        }

        private void SaveSession(ISaveGameHandle save)
        {
            var activeMap = _mapManager.ActiveMap;

            if (activeMap == null)
            {
                throw new InvalidOperationException("No active map to save");
            }

            var player = _gameSessionManager.Player;
            if (player == null)
            {
                throw new InvalidOperationException("No active player");
            }

            if (player.Spatial.MapID != activeMap.Id)
            {
                throw new InvalidOperationException($"Player was not in the active map ({activeMap.Id}) at the time of saving!");
            }

            var sessionData = new SessionData()
            {
                ActiveMapId = activeMap.Id,
                PlayerUid = (int)player.Uid,
                HighestMapId = _mapManager.HighestMapId
            };

            var session = new ResourcePath("/session.yml");
            save.Files.WriteSerializedData(session, sessionData, _serializationManager, alwaysWrite: true);
        }

        public void SaveActiveMap(ISaveGameHandle save)
        {
            _mapLoader.SaveMap(_mapManager.ActiveMap!.Id, save);
        }

        public void SaveGlobalData(ISaveGameHandle save)
        {
            // Save all the global data not tied to maps.
            var mapping = new MappingDataNode();

            foreach (var (key, reg) in _trackedSaveData)
            {
                Logger.DebugS(SawmillName, $"Saving global data: {key} ({reg.Type})");

                var data = reg.FieldInfo.GetValue(reg.Parent);

                var node = _serializationManager.WriteValue(reg.Type, data, alwaysWrite: true);
                mapping.Add(key, node);
            }

            var global = new ResourcePath("/global.yml");
            var root = new MappingDataNode();
            root.Add("data", mapping);

            save.Files.WriteAllYaml(global, root.ToYaml());
        }

        /// <inheritdoc/>
        public void LoadGame(ISaveGameHandle save)
        {
            save.Files.ClearTemp();

            ResetGameState();
            LoadSession(save);
            LoadGlobalData(save);

            OnGameLoaded?.Invoke(save);
        }

        /// <summary>
        /// Unloads absolutely everything from the entity/map managers.
        /// </summary>
        private void ResetGameState()
        {
            _entityManager.FlushEntities();
            _mapManager.FlushMaps();
        }

        private void LoadSession(ISaveGameHandle save)
        {
            var session = new ResourcePath("/session.yml");
            var sessionData = save.Files.ReadSerializedData<SessionData>(session, _serializationManager, skipHook: true)!;

            _mapManager.HighestMapId = sessionData.HighestMapId;
            var map = _mapLoader.LoadMap(sessionData.ActiveMapId, save);
            _mapManager.SetActiveMap(map.Id);

            var playerUid = new EntityUid(sessionData.PlayerUid);
            if (!_entityManager.TryGetEntity(playerUid, out var player))
            {
                throw new InvalidDataException($"Active player '{sessionData.PlayerUid}' was not in saved active map {map.Id}!");
            }

            _gameSessionManager.Player = player;
        }

        public void LoadGlobalData(ISaveGameHandle save)
        {
            var global = new ResourcePath("/global.yml");
            var stream = save.Files.ReadAllYaml(global);
            var node = stream.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();

            var data = (MappingDataNode)node.Get("data");

            foreach (var (keyNode, rawNode) in data.Children)
            {
                var key = ((ValueDataNode)keyNode).Value;

                if (!_trackedSaveData.TryGetValue(key, out var reg))
                {
                    Logger.WarningS(SawmillName, $"Found global save data with key '{key}', but it wasn't registered with the save game serializer.");
                    continue;
                }

                Logger.DebugS(SawmillName, $"Loading global data: {key} ({reg.Type})");

                var value = _serializationManager.ReadValue(reg.Type, rawNode, skipHook: true);

                if (reg.FieldInfo.TryGetBackingField(out var backingField))
                {
                    backingField.SetValue(reg.Parent, value);
                }
                else
                {
                    reg.FieldInfo.SetValue(reg.Parent, value);
                }
            }
        }
    }
}