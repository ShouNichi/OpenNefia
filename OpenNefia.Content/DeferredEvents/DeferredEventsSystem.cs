﻿using OpenNefia.Content.Maps;
using OpenNefia.Content.TitleScreen;
using OpenNefia.Content.TurnOrder;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Log;
using OpenNefia.Core.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.DeferredEvents
{
    public delegate TurnResult DeferredEventDelegate(); // TODO change type to 'TurnResult?'

    public interface IDeferredEventsSystem : IEntitySystem
    {
        void Enqueue(DeferredEventDelegate fn, long priority = EventPriorities.Default);
        bool IsEventEnqueued();
    }

    public sealed class DeferredEventsSystem : EntitySystem, IDeferredEventsSystem
    {
        private readonly PriorityQueue<DeferredEventDelegate, long> _deferredEvents = new();

        public override void Initialize()
        {
            SubscribeBroadcast<MapBeforeTurnBeginEventArgs>(RunDeferredEvents);
            SubscribeBroadcast<BeforeMapLeaveEventArgs>(ClearDeferredEvents);
            SubscribeBroadcast<ActiveMapChangedEvent>(ClearDeferredEvents);
        }

        private void RunDeferredEvents(MapBeforeTurnBeginEventArgs ev)
        {
            if (ev.Handled)
                return;

            while (_deferredEvents.TryDequeue(out var cb, out _))
            {
                try
                {
                    var result = cb();
                    if (result != TurnResult.NoResult)
                    {
                        ev.Handle(result);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorS("deferredEvent", ex, $"Error running deferred event");
                }
            }
        }

        private void ClearDeferredEvents(BeforeMapLeaveEventArgs ev)
        {
            _deferredEvents.Clear();
        }

        private void ClearDeferredEvents(ActiveMapChangedEvent ev)
        {
            _deferredEvents.Clear();
        }

        public void Enqueue(DeferredEventDelegate fn, long priority = EventPriorities.Default)
        {
            _deferredEvents.Enqueue(fn, priority);
        }

        public bool IsEventEnqueued()
        {
            return _deferredEvents.Count > 0;
        }
    }
}
