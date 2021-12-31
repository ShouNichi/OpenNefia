﻿using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.ElonaAI
{
    /// <summary>
    /// State relevant to vanilla Elona's AI logic.
    /// </summary>
    [RegisterComponent]
    public class ElonaAIComponent : Component
    {
        public override string Name => "ElonaAI";

        /// <summary>
        /// Current hostile target.
        /// </summary>
        [DataField]
        public EntityUid? CurrentTarget { get; set; }

        /// <summary>
        /// How much aggro this entity carries towards its current target.
        /// </summary>
        [DataField]
        public int Aggro { get; set; }

        /// <summary>
        /// The entity that most recently attacked this entity.
        /// </summary>
        [DataField]
        public EntityUid? LastAttacker { get; set; }

        /// <summary>
        /// Distance the AI will try to maintain when engaging a target.
        /// </summary>
        [DataField("targetDistance")]
        public int TargetDistance { get; set; }

        /// <summary>
        /// Chance the AI will move towards/away from the target if it's outside 
        /// the <see cref="TargetDistance"/>.
        /// </summary>
        [DataField("moveChance")]
        public float MoveChance { get; set; }

        /// <summary>
        /// How many turns this entity will wait before moving again. Typically
        /// incremented when this entity was blocked by something when it tried
        /// to move.
        /// </summary>
        [DataField]
        public int TurnsUntilMovement { get; set; }

        /// <summary>
        /// Position in the current map this entity wants to move to.
        /// </summary>
        // TODO can't serialize MapCoordinates...
        [DataField]
        public Vector2i DesiredMovePosition { get; set; }

        /// <summary>
        /// Action this entity will take if there are no hostiles.
        /// </summary>
        // TODO make this support ECS
        [DataField]
        public ElonaAICalmAction CalmAction { get; set; } = ElonaAICalmAction.Roam;
    }

    public enum ElonaAICalmAction
    {
        None = 0,
        Roam = 1,
        Dull = 2,
        FollowPlayer = 4
    }
}
