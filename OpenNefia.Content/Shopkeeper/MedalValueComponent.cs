﻿using OpenNefia.Content.Prototypes;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;

namespace OpenNefia.Content.Shopkeeper
{
    [RegisterComponent]
    [ComponentUsage(ComponentTarget.Normal)]
    public sealed class MedalValueComponent : Component
    {
        public override string Name => "MedalValue";

        [DataField]
        public int MedalValue { get; set; } = 1;
    }
}