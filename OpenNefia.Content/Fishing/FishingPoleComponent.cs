﻿using OpenNefia.Content.Prototypes;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;

namespace OpenNefia.Content.Fishing
{
    [RegisterComponent]
    [ComponentUsage(ComponentTarget.Normal)]
    public sealed class FishingPoleComponent : Component
    {
        [DataField]
        public PrototypeId<BaitPrototype>? BaitID { get; set; }

        [DataField]
        public int BaitAmount { get; set; }
    }
}