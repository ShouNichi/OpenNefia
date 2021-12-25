﻿using OpenNefia.Core.Audio;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Logic;
using OpenNefia.Content.Prototypes;
using OpenNefia.Core.Maps;
using OpenNefia.Content.FieldMap;
using OpenNefia.Core.Log;
using OpenNefia.Core.Prototypes;

namespace OpenNefia.Content.GameObjects
{
    public class WorldMapFieldsSystem : EntitySystem
    {
        [Dependency] private readonly IDynamicTypeFactory _dynTypes = default!;
        [Dependency] private readonly IAudioSystem _sounds = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly MapEntranceSystem _mapEntrances = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<WorldMapFieldsComponent, GetVerbsEventArgs>(HandleGetVerbs, nameof(HandleGetVerbs));
            SubscribeLocalEvent<ExecuteVerbEventArgs>(HandleExecuteVerb, nameof(HandleExecuteVerb));
        }

        private void HandleGetVerbs(EntityUid uid, WorldMapFieldsComponent component, GetVerbsEventArgs args)
        {
            args.Verbs.Add(new Verb(StairsSystem.VerbIDActivate));
        }

        private void HandleExecuteVerb(ExecuteVerbEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.Verb.ID)
            {
                case StairsSystem.VerbIDActivate:
                    args.Handle(EnterField(args.Target, args.Source));
                    break;
            }
        }

        private PrototypeId<FieldTypePrototype> GetFieldMapFromStoodTile(PrototypeId<TilePrototype> stoodTile)
        {
            // TODO
            return Protos.FieldMap.Plains;
        
        }
        private TurnResult EnterField(EntityUid mapEntity, EntityUid user,
            WorldMapFieldsComponent? worldMapFields = null,
            SpatialComponent? userSpatial = null)
        {
            if (!Resolve(mapEntity, ref worldMapFields))
                return TurnResult.Failed;

            if (!Resolve(user, ref userSpatial))
                return TurnResult.Failed;

            if (!_mapManager.TryGetMap(userSpatial.MapID, out var map))
                return TurnResult.Failed;

            var prevCoords = userSpatial.MapPosition;

            _sounds.Play(Protos.Sound.Exitmap1);

            var stoodTile = map.Tiles[prevCoords.X, prevCoords.Y].ResolvePrototype().GetStrongID();

            var gen = new FieldMapGenerator()
            {
                FieldMap = GetFieldMapFromStoodTile(stoodTile)
            };
            IoCManager.InjectDependencies(gen);
            var fieldMapId = gen.Generate(null, new MapGeneratorOptions()
            {
                Width = 34,
                Height = 22
            });

            if (fieldMapId == null)
            {
                Logger.WarningS("sys.field", "Map generation failed");
                return TurnResult.Failed;
            }

            var entrance = new MapEntrance()
            {
                DestinationMapId = fieldMapId.Value,
                StartLocation = new CenterMapLocation()
            };

            var turnResult = _mapEntrances.UseMapEntrance(user, entrance);
            _mapEntrances.SetPreviousMap(fieldMapId.Value, prevCoords);

            return turnResult;
        }
    }
}