﻿using OpenNefia.Content.Charas;
using OpenNefia.Content.PCCs;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.ResourceManagement;
using static OpenNefia.Content.Prototypes.Protos;

namespace OpenNefia.Content.CharaAppearance
{
    public static class CharaAppearanceHelpers
    {
        public static CharaAppearanceData MakeDefaultAppearanceData(IPrototypeManager protos, IResourceCache resourceCache)
        {
            ChipPrototype chipProto = protos.Index(Chip.Default);
            PortraitPrototype portraitProto = protos.EnumeratePrototypes<PortraitPrototype>().Where(p => p.GetStrongID() != Portrait.Default).First();
            PCCDrawable pccDrawable = PCCHelpers.CreateDefaultPCCFromLayout(PCCConstants.DefaultPCCPartLayout, protos, resourceCache);

            var appearanceData = new CharaAppearanceData(chipProto, Color.White, portraitProto, pccDrawable, true);
            return appearanceData;
        }

        public static CharaAppearanceData MakeAppearanceDataFrom(EntityUid entity,
            IPrototypeManager protos,
            IEntityManager entityManager,
            IResourceCache resourceCache,
            IPCCSystem pccs)
        {
            ChipPrototype chipProto = protos.Index(Chip.Default);
            Color chipColor = Color.White;

            if (entityManager.TryGetComponent(entity, out ChipComponent chipComp)
                && protos.TryIndex(chipComp.ChipID, out var chipProtoFound))
            {
                chipProto = chipProtoFound;
                chipColor = chipComp.Color;
            }

            PortraitPrototype portraitProto = protos.Index(Portrait.Default);

            if (entityManager.TryGetComponent(entity, out PortraitComponent portraitComp)
                && protos.TryIndex(portraitComp.PortraitID, out var portraitProtoFound))
            {
                portraitProto = portraitProtoFound;
            }

            var usePCC = false;

            if (pccs.TryGetPCCDrawable(entity, out var pccDrawable))
            {
                usePCC = true;
            }
            else
            {
                pccDrawable = PCCHelpers.CreateDefaultPCCFromLayout(PCCConstants.DefaultPCCPartLayout, protos, resourceCache);
            }

            var appearanceData = new CharaAppearanceData(chipProto, chipColor, portraitProto, pccDrawable, usePCC);
            return appearanceData;
        }
    }
}