﻿using OpenNefia.Content.Combat;
using OpenNefia.Content.CurseStates;
using OpenNefia.Content.GameObjects;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Serialization.Manager.Attributes;
using OpenNefia.Core.Utility;

namespace OpenNefia.Content.Effects
{
    [ImplicitDataDefinitionForInheritors]
    public interface IEffect
    {
        TurnResult Apply(EntityUid source, EntityUid target, EntityCoordinates coords, EntityUid? verb, EffectArgSet args);

        void GetDice(EntityUid source, EntityUid target, EntityCoordinates coords, EntityUid? verb, EffectArgSet args,
            ref Dictionary<string, IDice> result) {}
    }
    
    public abstract class Effect : IEffect
    {
        [Dependency] protected readonly IEntityManager EntityManager = default!;
        
        public abstract TurnResult Apply(EntityUid source, EntityUid target, EntityCoordinates coords, EntityUid? verb, EffectArgSet args);

        public virtual void GetDice(EntityUid source, EntityUid target, EntityCoordinates coords, EntityUid? verb,
            EffectArgSet args,
            ref Dictionary<string, IDice> result)
        {
        }
    }

    public sealed class NullEffect : Effect
    {
        public override TurnResult Apply(EntityUid source, EntityUid target, EntityCoordinates coords, EntityUid? verb, EffectArgSet args)
        {
            return TurnResult.Succeeded;
        }
    }
    
    [ImplicitDataDefinitionForInheritors]
    public abstract class EffectArgs
    {
    }

    // TODO: this exists since how to serialize effect args and copy them on every effect invocation should be revisited.
    // For now power and curse state are the only important fields. In the future the rest might be desirable to make
    // declarative.
    [DataDefinition]
    public struct ImmutableEffectArgSet
    {
        public ImmutableEffectArgSet() {}
        
        public ImmutableEffectArgSet(int power, CurseState curseState)
        {
            Power = power;
            CurseState = curseState;
        }

        [DataField]
        public int Power { get; } = 1;

        [DataField]
        public CurseState CurseState { get; } = CurseState.Normal;
    }

    public sealed class EffectArgSet : Blackboard<EffectArgs>
    {
        public EffectCommonArgs CommonArgs => Ensure<EffectCommonArgs>();
        public int Power
        {
            get => CommonArgs.Power;
            set => CommonArgs.Power = value;
        }
        public int SkillLevel
        {
            get => CommonArgs.SkillLevel;
            set => CommonArgs.SkillLevel = value;
        }
        public int TileRange
        {
            get => CommonArgs.TileRange;
            set => CommonArgs.TileRange = value;
        }
        public CurseState CurseState
        {
            get => CommonArgs.CurseState;
            set => CommonArgs.CurseState = value;
        }

        public static EffectArgSet Make(params EffectArgs[] rest)
        {
            var result = new EffectArgSet();

            foreach (var param in rest)
                result.Add(param);

            return result;
        }

        public static EffectArgSet FromImmutable(ImmutableEffectArgSet args, params EffectArgs[] rest)
        {
            var result = Make(rest);
            //result.Power = args.Power;
            //result.CurseState = args.CurseState;
            return result;
        }
    }

    [DataDefinition]
    public sealed class EffectCommonArgs : EffectArgs
    {
        [DataField]
        public int Power { get; set; } = 1;

        [DataField]
        public int SkillLevel { get; set; } = 1;

        /// <summary>
        /// Curse state of the item that cast this effect.
        /// Can be overridden here to invoke the same curse state effect, but without any items involved.
        /// </summary>
        [DataField]
        public CurseState CurseState { get; set; } = CurseState.Normal;

        /// <summary>
        /// How this effect was triggered initially (e.g. by casting a spell, drinking a potion, traps, etc.). This is mostly used for message display.
        /// </summary>
        [DataField]
        public string EffectSource { get; set; } = EffectSources.Default;
        
        /// <summary>
        /// How many tiles this spell can reach. Used by ball magic.
        /// </summary>
        [DataField]
        public int TileRange { get; set; } = 1;

        /// <summary>
        /// If <see cref="SourceItem"/> is non-null, the item's curse state will be automatically
        /// inherited into <see cref="CurseState"/>.
        /// Set this to <c>false</c> to prevent this and use the value for <see cref="CurseState"/>
        /// that was given.
        /// </summary>
        [DataField]
        public bool NoInheritItemCurseState { get; set; } = false;

        /// <summary>
        /// Item responsible for the effect, like the scroll/wand/potion.
        /// </summary>
        public EntityUid? SourceItem { get; set; } = null;

        /// <summary>
        /// Item that is the target of the effect (identification, uncurse, etc.)
        /// </summary>
        public EntityUid? TargetItem { get; set; } = null;

        /// <summary>
        /// If set to true after casting a spell, the thing holding the spell should be identified.
        /// </summary>
        /// <remarks>
        /// The default is <c>true</c>, set to <c>false</c> to prevent identification.
        /// </remarks>
        public bool OutEffectWasObvious { get; set; } = true;
    }

    public static class EffectSources
    {
        public const string Default = "Default";
        public const string Spell = "Spell";
        public const string Action = "Action";
        public const string Scroll = "Scroll";
        public const string Wand = "Wand";
        public const string PotionDrunk = "PotionDrunk";
        public const string PotionThrown = "PotionThrown";
        public const string PotionSpilt = "PotionSpilt";
        public const string Trap = "Trap";
    }
}
