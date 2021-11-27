﻿using OpenNefia.Core.GameObjects;

namespace OpenNefia.Core.Rendering
{
    public enum ShadowType
    {
        None,
        Normal,
        DropShadow
    }

    internal enum MemoryState
    {
        Added,
        InUse,
        Removed
    }

    [Serializable]
    public class MapObjectMemory
    {
        public EntityUid ObjectUid;
        public bool IsVisible;
        public string ChipIndex = string.Empty;
        public int ScreenXOffset;
        public int ScreenYOffset;
        public float Rotation;
        public Love.Color Color;
        public ShadowType ShadowType;

        internal int Index;
        internal int TileX;
        internal int TileY;
        internal int ZOrder;
        internal MemoryState State;
    }
}