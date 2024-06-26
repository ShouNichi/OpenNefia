﻿using OpenNefia.Content.ConfigMenu.UICell;
using OpenNefia.Content.UI;
using OpenNefia.Content.UI.Element;
using OpenNefia.Content.UI.Element.List;
using OpenNefia.Core;
using OpenNefia.Core.Audio;
using OpenNefia.Core.Configuration;
using OpenNefia.Core.Input;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.UI;
using OpenNefia.Core.UI.Element;
using OpenNefia.Core.UI.Layer;
using OpenNefia.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenNefia.Content.Prototypes.Protos;

namespace OpenNefia.Content.ConfigMenu
{
    [Localize("Elona.Config.Layer")]
    public sealed class ConfigMenuLayer : UiLayerWithResult<ConfigMenuLayer.Args, UINone>
    {
        public class Args
        {
            public PrototypeId<ConfigMenuItemPrototype> PrototypeId { get; }
            public ConfigSubmenuMenuNode Submenu { get; }

            public Args(PrototypeId<ConfigMenuItemPrototype> prototypeId, ConfigSubmenuMenuNode submenu)
            {
                PrototypeId = prototypeId;
                Submenu = submenu;
            }
        }

        [Dependency] private readonly IConfigMenuUICellFactory _cellFactory = default!;

        [Child] [Localize("Topic.Menu")] private UiText TextTopicMenu = new UiTextTopic();
        [Child] private AssetDrawable AssetG2;

        // The UI cells are generic based on the config option type, so UINone is
        // used to have them all in one list.
        // FIXME: #35
        // It should be BaseConfigMenuUICell instead.
        [Child] private UiPagedList<UINone> List;
        [Child] private UiWindow Window = new();

        private Vector2i _menuSize = new();
        private PrototypeId<ConfigMenuItemPrototype> _protoId;

        public ConfigMenuLayer()
        {
            List = new UiPagedList<UINone>(elementForPageText: Window);
            AssetG2 = new AssetDrawable(Asset.G2, color: new(255, 255, 255, 50));

            OnKeyBindDown += HandleKeyBindDown;
            List.OnActivated += HandleListActivate;
            List.OnPageChanged += HandleListPageChanged;
        }

        public override void GrabFocus()
        {
            base.GrabFocus();
            List.GrabFocus();
        }

        public override void Initialize(Args args)
        {
            var cells = new List<BaseConfigMenuUICell>();
            foreach (var child in args.Submenu.Items)
            {
                cells.Add(_cellFactory.CreateUICellFor(child));
            }
            List.SetCells(cells);

            _menuSize = args.Submenu.MenuSize;
            _protoId = args.PrototypeId;

            Window.Title = Loc.GetPrototypeString(_protoId, "Name");
            Window.KeyHints = MakeKeyHints();

            RefreshConfigValueDisplay();
        }

        public override void Localize(LocaleKey key)
        {
            base.Localize(key);

            Window.Title = Loc.GetPrototypeString(_protoId, "Name");
            Window.KeyHints = MakeKeyHints();
        }

        private void RefreshConfigValueDisplay()
        {
            // FIXME: #35
            foreach (var cell in List.Cast<BaseConfigMenuUICell>())
            {
                cell.RefreshConfigValueDisplay();
                cell.ValueText.Text = cell.ValueText.Text.WideSubstring(0, 15);
            }
        }

        private void HandleListActivate(object? sender, UiListEventArgs<UINone> evt)
        {
            var selected = (BaseConfigMenuUICell)evt.SelectedCell;
            if (selected.CanActivate())
            {
                Sounds.Play(Sound.Ok1);
                selected.HandleActivated();
                RefreshConfigValueDisplay();
            }
        }

        private void HandleListPageChanged(int newPage, int newPageCount)
        {
            // Recenter the window based on the new item count.
            SetPreferredSize();
            SetPosition(X, Y);
        }

        private void HandleKeyBindDown(GUIBoundKeyEventArgs evt)
        {
            if (evt.Function == EngineKeyFunctions.UICancel)
            {
                Finish(new());
            }

            if (List.SelectedCell is not BaseConfigMenuUICell selected)
                return;

            var (leftEnabled, rightEnabled) = selected.CanChange();

            if (evt.Function == EngineKeyFunctions.UILeft)
            {
                if (leftEnabled)
                {
                    Sounds.Play(Sound.Ok1);
                    selected.HandleChanged(-1);
                    RefreshConfigValueDisplay();
                }
            }
            else if (evt.Function == EngineKeyFunctions.UIRight)
            {
                if (rightEnabled)
                {
                    Sounds.Play(Sound.Ok1);
                    selected.HandleChanged(1);
                    RefreshConfigValueDisplay();
                }
            }
        }

        public override List<UiKeyHint> MakeKeyHints()
        {
            var keyHints = base.MakeKeyHints();

            keyHints.AddRange(List.MakeKeyHints());
            keyHints.Add(new(UiKeyHints.Close, EngineKeyFunctions.UICancel));

            return keyHints;
        }

        public override void GetPreferredBounds(out UIBox2 bounds)
        {
            var height = _menuSize.Y;

            if (List.DisplayedCells.Count >= 8)
            {
                height += 10 + 30 * (List.DisplayedCells.Count - 8);
            }

            UiUtils.GetCenteredParams(_menuSize.X, height, out bounds, yOffset: -12);
        }

        public override void SetSize(float width, float height)
        {
            base.SetSize(width, height);
            Window.SetSize(Width, Height);
            TextTopicMenu.SetPreferredSize();
            AssetG2.SetSize(Window.Width / 5 * 3, Window.Height - 80);
            List.SetSize(Window.Width - 56, Window.Height - 66);
        }

        public override void SetPosition(float x, float y)
        {
            base.SetPosition(x, y);
            Window.SetPosition(X, Y);
            TextTopicMenu.SetPosition(Window.X + 34, Window.Y + 36);
            AssetG2.SetPosition(Window.X + Window.Width / 3, Window.Y + Window.Height / 2);
            AssetG2.Centered = true;
            List.SetPosition(Window.X + 56, Window.Y + 66);
        }

        public override void Update(float dt)
        {
            Window.Update(dt);
            TextTopicMenu.Update(dt);
            AssetG2.Update(dt);
            List.Update(dt);
        }

        public override void Draw()
        {
            Window.Draw();
            TextTopicMenu.Draw();
            AssetG2.Draw();
            List.Draw();
        }
    }
}
