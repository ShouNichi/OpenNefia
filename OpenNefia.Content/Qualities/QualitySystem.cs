﻿using OpenNefia.Content.DisplayName;
using OpenNefia.Content.EntityGen;
using OpenNefia.Content.GameObjects;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Locale;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Qualities
{
    public interface IQualitySystem : IEntitySystem
    {
        Quality GetQuality(EntityUid ent, QualityComponent? quality = null);
    }

    public sealed class QualitySystem : EntitySystem, IQualitySystem
    {
        public override void Initialize()
        {
            SubscribeComponent<QualityComponent, EntityBeingGeneratedEvent>(SetQualityFromGenArgs, EventPriorities.Highest);
            SubscribeComponent<QualityComponent, GetBaseNameEventArgs>(AddQualityBrackets);
        }

        public Quality GetQuality(EntityUid ent, QualityComponent? quality = null)
        {
            if (!Resolve(ent, ref quality))
                return Quality.Bad;

            return quality.Quality.Buffed;
        }

        private void SetQualityFromGenArgs(EntityUid uid, QualityComponent component, ref EntityBeingGeneratedEvent args)
        {
            if (args.CommonArgs.Quality != null)
                component.Quality.Base = args.CommonArgs.Quality.Value;
        }

        private void AddQualityBrackets(EntityUid uid, QualityComponent quality, ref GetBaseNameEventArgs args)
        {
            if (quality.Quality.Buffed == Quality.Great)
            {
                args.OutBaseName = Loc.GetString("Elona.Quality.Brackets.Great", ("name", args.OutBaseName));
            }
            else if (quality.Quality.Buffed == Quality.God)
            {
                args.OutBaseName = Loc.GetString("Elona.Quality.Brackets.God", ("name", args.OutBaseName));
            }
        }
    }
}
