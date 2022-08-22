﻿using OpenNefia.Core.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNefia.Content.Skills;
using OpenNefia.Core.Prototypes;
using OpenNefia.Content.Food;
using OpenNefia.Core.Game;
using OpenNefia.Content.Resists;
using OpenNefia.Content.UI;
using OpenNefia.Content.Combat;
using OpenNefia.Content.Damage;
using OpenNefia.Content.Spells;
using OpenNefia.Content.GameObjects.EntitySystems.Tag;
using OpenNefia.Content.RandomGen;
using OpenNefia.Content.GameObjects;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Content.Logic;

namespace OpenNefia.Content.Enchantments
{
    public sealed partial class VanillaEnchantmentsSystem : EntitySystem
    {
        [Dependency] private readonly IRandom _rand = default!;
        [Dependency] private readonly IMessagesManager _mes = default!;
        [Dependency] private readonly ISkillsSystem _skills = default!;
        [Dependency] private readonly IGameSessionManager _gameSession = default!;
        [Dependency] private readonly IResistsSystem _resists = default!;
        [Dependency] private readonly IDamageSystem _damage = default!;
        [Dependency] private readonly ITagSystem _tags = default!;
        [Dependency] private readonly IPrototypeManager _protos = default!;
        [Dependency] private readonly ISpellSystem _spells = default!;

        public override void Initialize()
        {
            Initialize_Parameterized();
            Initialize_Unique();
        }
    }
}
