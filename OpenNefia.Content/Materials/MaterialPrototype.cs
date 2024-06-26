﻿using OpenNefia.Content.Enchantments;
using OpenNefia.Content.Logic;
using OpenNefia.Content.Prototypes;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Random;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNefia.Content.Materials
{
    [Prototype("Elona.Material")]
    public class MaterialPrototype : IPrototype, IHspIds<int>
    {
        [IdDataField]
        public string ID { get; } = default!;
        
        /// <inheritdoc/>
        [DataField]
        [NeverPushInheritance]
        public HspIds<int>? HspIds { get; }

        [DataField]
        public float WeightModifier { get; } = 1f;

        [DataField]
        public float ValueModifier { get; } = 1f;

        [DataField]
        public int HitBonus { get; }

        [DataField]
        public int DamageBonus { get; }

        [DataField]
        public int DV { get; }

        [DataField]
        public int PV { get; }

        [DataField]
        public int DiceY { get; }

        [DataField]
        public Color Color { get; } = Color.White;

        [DataField]
        public bool GenerateOnFurniture { get; } = true;

        [DataField]
        public bool IsFireproof { get; }

        [DataField]
        public bool IsAcidproof { get; }

        [DataField]
        public bool IsColdproof { get; }

        [DataField("enchantments")]
        private List<EnchantmentSpecifer> _enchantments { get; set; } = new();
        public IReadOnlyList<EnchantmentSpecifer> Enchantments => _enchantments;
    }
}