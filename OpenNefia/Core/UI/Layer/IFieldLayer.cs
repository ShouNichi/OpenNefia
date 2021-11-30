﻿using OpenNefia.Core.Maps;
using OpenNefia.Core.UI.Hud;

namespace OpenNefia.Core.UI.Layer
{
    public interface IFieldLayer : IUiLayerWithResult<UiNoResult>
    {
        Camera Camera { get; }
        HudLayer Hud { get; }

        void SetMap(IMap map);
        void RefreshScreen();
    }
}