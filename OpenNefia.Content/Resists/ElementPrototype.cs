﻿using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;

namespace OpenNefia.Content.Resists
{
    [Prototype("Element")]
    public class ElementPrototype : IPrototype
    {
        /// <inheritdoc />
        [DataField("id", required: true)]
        public string ID { get; private set; } = default!;

        [DataField]
        public bool CanResist { get; set; } = false;
    }
}
