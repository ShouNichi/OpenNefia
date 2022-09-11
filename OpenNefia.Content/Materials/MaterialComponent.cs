﻿using OpenNefia.Content.Prototypes;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;

namespace OpenNefia.Content.Materials
{
    [RegisterComponent]
    [ComponentUsage(ComponentTarget.Normal)]
    public sealed class MaterialComponent : Component
    {
        public override string Name => "Material";

        [DataField]
        public PrototypeId<MaterialPrototype>? MaterialID { get; set; }

        [DataField]
        public int RandomSeed { get; set; }

        /// <summary>
        /// If true, do not apply material bonuses (except enchantments and refreshable stats).
        /// </summary>
        /// <remarks>
        /// In the HSP version, if an item is generated with a non-randomized material already
        /// defined on it (like the Blood Moon), then the stat bonuses and item flags from the
        /// material will *not* be applied, but the enchantments will. Presumably this is to force
        /// the item's stats like DV/PV to the predefined values instead of having the material add
        /// more on top.
        /// See shade2/item.hsp:531.
        /// 
        /// This is emulated in OpenNefia by setting this flag to true for those items with
        /// non-randomized materials defined.
        /// </remarks>
        [DataField]
        public bool NoMaterialEffects { get; set; } = false;
    }
}