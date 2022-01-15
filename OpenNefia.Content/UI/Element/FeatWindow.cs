﻿using Love;
using OpenNefia.Content.Inventory;
using OpenNefia.Content.Prototypes;
using OpenNefia.Content.UI.Element.List;
using OpenNefia.Core.Audio;
using OpenNefia.Core.Input;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.UI.Element;
using OpenNefia.Core.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = OpenNefia.Core.Maths.Color;

namespace OpenNefia.Content.UI.Element
{
    public class FeatWindow : UiElement
    {
        public abstract record FeatNameAndDesc
        {
            public virtual string Name { get; } = "";
            public virtual string Description { get; } = "";
            public virtual Color Color { get; } = Color.Black;
            public record Feat(FeatPrototype Prototype, int Level) : FeatNameAndDesc
            {
                public virtual int TotalLevel => Level + 1;
                public override string Name => Loc.GetPrototypeString(Prototype.GetStrongID(), $"{Math.Min(TotalLevel, Prototype.LevelMax)}.Name") 
                    + (Level >= Prototype.LevelMax ? $"({Loc.GetString("Elona.FeatMenu.FeatMax")})" : string.Empty);
                public override string Description => Loc.GetPrototypeString(Prototype.GetStrongID(), "MenuDesc")!;

                public override Color Color => Level switch
                {
                    > 0 => UiColors.CharaMakeAttrLevelBest,
                    < 0 => UiColors.CharaMakeAttrLevelSlight,
                    _ => base.Color,
                };
            }
            public record FeatHeader(string LocalizeText) : FeatNameAndDesc
            {
                public override string Name => Loc.GetString(LocalizeText);
            }
            public record GainedFeat : Feat
            {
                public override int TotalLevel => Level;
                public override string Name => $"[{Loc.GetString($"Elona.FeatMenu.FeatType.{Prototype.FeatType}")}]{Description}";

                public override string Description => Loc.GetPrototypeString(Prototype.GetStrongID(), $"{Math.Min(TotalLevel, Prototype.LevelMax)}.Desc");
                
                public GainedFeat(FeatPrototype Prototype, int Level) : base(Prototype, Level)
                {
                }
            }
        }

        public class FeatCell : UiListCell<FeatNameAndDesc>
        {
            private IAssetInstance FeatIcons;

            private IUiText DescriptionText;
            public FeatCell(FeatNameAndDesc data) 
                : base(data, new UiText())
            {
                FeatIcons = Assets.Get(Protos.Asset.FeatIcons);
                Text = data.Name;
                DescriptionText = new UiText(Data.Description);
                UiText.Color = data.Color;
                DescriptionText.Color = data.Color;
            }

            private int Offset => Data switch
            {
                FeatNameAndDesc.FeatHeader header => 35,
                FeatNameAndDesc.GainedFeat gained => -8,
                _ => 0,
            };

            public override void SetPosition(int x, int y)
            {
                base.SetPosition(x + Offset, y);
                UiText.SetPosition(UiText.X, UiText.Y + 4);
                DescriptionText.SetPosition(UiText.X + 200, UiText.Y);
            }

            //overriden because position needs to be slightly altered
            public override void DrawHighlight()
            {
                var width = Math.Clamp(UiText.TextWidth + AssetSelectKey.Width + 8 + XOffset, 10, 480);
                Graphics.SetBlendMode(BlendMode.Subtract);
                GraphicsEx.SetColor(ColorSelectedSub);
                Graphics.Rectangle(DrawMode.Fill, UiText.X - XOffset - 4, UiText.Y - 4, width, 19);
                Graphics.SetBlendMode(BlendMode.Add);
                GraphicsEx.SetColor(ColorSelectedAdd);
                Graphics.Rectangle(DrawMode.Fill, UiText.X - XOffset - 3, UiText.Y - 3, width - 2, 17);
                Graphics.SetBlendMode(BlendMode.Alpha);
                GraphicsEx.SetColor(Love.Color.White);
                AssetListBullet.Draw(UiText.X - XOffset - 5 + width - 20, UiText.Y - 0);
            }

            /// <summary>
            /// the FeatType enum is already ordered correctly, so the int value only needs to be converted
            /// to a string
            /// </summary>
            private string GetFeatIconRegion(FeatType type)
            {
                return $"{(int)type}";
            }

            public override void Draw()
            {
                if (IndexInList % 2 == 0 && Data is not FeatNameAndDesc.FeatHeader)
                {
                    Love.Graphics.SetColor(UiColors.ListEntryAccent);
                    Love.Graphics.Rectangle(Love.DrawMode.Fill, X - 1, Y, 650 - Offset, 18);
                }
                switch (Data)
                {
                    case FeatNameAndDesc.GainedFeat feat:
                        GraphicsEx.SetColor(Color.White);
                        FeatIcons.DrawRegion(GetFeatIconRegion(feat.Prototype.FeatType), X - 7, Y - 5);
                        UiText.Draw();
                        break;
                    case FeatNameAndDesc.FeatHeader:
                        UiText.Draw();
                        break;
                    case FeatNameAndDesc.Feat feat:
                        GraphicsEx.SetColor(Color.White);
                        FeatIcons.DrawRegion(GetFeatIconRegion(feat.Prototype.FeatType), X - 27, Y - 5);
                        base.Draw();
                        DescriptionText.Draw();
                        break;
                }
            }
        }

        [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
        private const int ItemsPerPage = 15;
        private const int WindowHeight = 430;
        private const int WindowWidth = 740;
        private UiPagedList<FeatNameAndDesc> List;
        private UiTextTopic NameTopic;
        private UiTextTopic DetailTopic;
        private UiText FeatCountText;
        private UiWindow Window = new(keyHintXOffset: 64);

        protected IAssetDrawable AssetInventoryIcons;
        private IAssetDrawable AssetDecoFeatA;
        private IAssetDrawable AssetDecoFeatB;
        private IAssetDrawable AssetDecoFeatC;
        private IAssetDrawable AssetDecoFeatD;

        private Func<Dictionary<PrototypeId<FeatPrototype>, int>> GetGainedFeatsFunc;
        private Action<FeatNameAndDesc.Feat> SelectFeatAction;
        private Func<int> GetAvailableCountFunc;

        public FeatWindow(Func<Dictionary<PrototypeId<FeatPrototype>, int>> getGainedFeatsFunc, Action<FeatNameAndDesc.Feat> selectFeatAction, Func<int> getAvailableCountFunc) 
        {
            IoCManager.InjectDependencies(this);
            AssetInventoryIcons = InventoryHelpers.MakeIcon(InventoryIcon.Feat);
            AssetDecoFeatA = new AssetDrawable(Protos.Asset.DecoFeatA);
            AssetDecoFeatB = new AssetDrawable(Protos.Asset.DecoFeatB);
            AssetDecoFeatC = new AssetDrawable(Protos.Asset.DecoFeatC);
            AssetDecoFeatD = new AssetDrawable(Protos.Asset.DecoFeatD);

            NameTopic = new UiTextTopic(Loc.GetString("Elona.FeatMenu.Topic.Name"));
            DetailTopic = new UiTextTopic(Loc.GetString("Elona.FeatMenu.Topic.Detail"));
            FeatCountText = new UiText(UiFonts.WindowPage);

            Window.Title = Loc.GetString("Elona.FeatMenu.Window.Title");
            List = new UiPagedList<FeatNameAndDesc>(ItemsPerPage, this, new Vector2i(-55, -3));

            GetGainedFeatsFunc = getGainedFeatsFunc ?? (() => new Dictionary<PrototypeId<FeatPrototype>, int>());
            SelectFeatAction = selectFeatAction ?? (feat => { });
            GetAvailableCountFunc = getAvailableCountFunc ?? (() => 0);

            EventFilter = UIEventFilterMode.Pass;
            List.OnActivated += List_OnActivate;
            AddChild(List);

            RefreshData();
        }
        
        private void RefreshData(FeatPrototype? lastSelected = null)
        {
            List.Clear();
            var data = new List<FeatCell>();
            var prototypes = _prototypeManager.EnumeratePrototypes<FeatPrototype>().Where(x => x.FeatType == FeatType.Feat);
            var featCount = GetAvailableCountFunc();
            var gainedFeats = GetGainedFeatsFunc();

            if (featCount > 0)
            {
                data.Add(new FeatCell(new FeatNameAndDesc.FeatHeader("Elona.FeatMenu.Header.Available")));
                data.AddRange(prototypes.Select(x =>
                {
                    gainedFeats.TryGetValue(x.GetStrongID(), out var val);
                    return new FeatCell(new FeatNameAndDesc.Feat(x, val));
                }));
                FeatCountText.Text = Loc.GetString("Elona.FeatMenu.FeatCount", ("featsRemaining", featCount));
            }
            else
            {
                FeatCountText.Text = string.Empty;
            }
            data.Add(new FeatCell(new FeatNameAndDesc.FeatHeader("Elona.FeatMenu.Header.Gained")));
            data.AddRange(gainedFeats
                .Select(x => new FeatCell(new FeatNameAndDesc.GainedFeat(x.Key.ResolvePrototype(), x.Value)))
                .OrderBy(x => (x.Data as FeatNameAndDesc.GainedFeat)?.Prototype.FeatType));
            List.AddRange(data);

            if (lastSelected != null)
            {
                var selected = data.First(x => (x.Data as FeatNameAndDesc.GainedFeat)?.Prototype == lastSelected);
                var page = (data.IndexOf(selected)) / ItemsPerPage;
                List.SetPage(page, false);
                Sounds.Play(Protos.Sound.Ding3);
                var pageElements = List.DisplayedCells.ToList();
                List.Select(pageElements.IndexOf(pageElements.First(x => (x.Data as FeatNameAndDesc.GainedFeat)?.Prototype == lastSelected)));
            }
        }

        public override void GrabFocus()
        {
            base.GrabFocus();
            List.GrabFocus();
        }

        private void List_OnActivate(object? sender, UiListEventArgs<FeatNameAndDesc> args)
        {
            switch(args.SelectedCell.Data)
            {
                case FeatNameAndDesc.GainedFeat:
                    break;
                case FeatNameAndDesc.Feat feat:
                    if (feat.Level >= feat.Prototype.LevelMax)
                        break;

                    SelectFeatAction(feat);
                    RefreshData(feat.Prototype);
                    break;
            }
        }

        public override void GetPreferredSize(out Vector2i size)
        {
            size.X = WindowWidth;
            size.Y = WindowHeight;
        }

        public override void SetSize(int width, int height)
        {
            base.SetSize(WindowWidth, WindowHeight);
            Window.SetSize(Width, Height);
            List.SetPreferredSize();
            NameTopic.SetPreferredSize();
            DetailTopic.SetPreferredSize();
            FeatCountText.SetPreferredSize();
            AssetDecoFeatA.SetPreferredSize();
            AssetDecoFeatB.SetPreferredSize();
            AssetDecoFeatC.SetPreferredSize();
            AssetDecoFeatD.SetPreferredSize();
            AssetInventoryIcons.SetPreferredSize();
        }

        public override void SetPosition(int x, int y)
        {
            base.SetPosition(x, y);
            Window.SetPosition(x, y);
            List.SetPosition(Window.X + 55, Window.Y + 60);
            NameTopic.SetPosition(Window.X + 45, Window.Y + 32);
            DetailTopic.SetPosition(Window.X + 270, NameTopic.Y);
            FeatCountText.SetPosition(Window.X + (Window.Width / 2) + 10, Window.Y + Window.Height - 71);
            AssetDecoFeatA.SetPosition(Window.X + Window.Width - 56, Window.Y + Window.Height - 198);
            AssetDecoFeatB.SetPosition(Window.X, Window.Y);
            AssetDecoFeatC.SetPosition(Window.X + Window.Width - 108, Window.Y);
            AssetDecoFeatD.SetPosition(Window.X, Window.Y + Window.Height - 70);
            AssetInventoryIcons.SetPosition(Window.X + 46, Window.Y - 16);
        }

        public override void Draw()
        {
            base.Draw();
            Window.Draw();
            List.Draw();
            NameTopic.Draw();
            DetailTopic.Draw();
            FeatCountText.Draw();
            AssetDecoFeatA.Draw();
            AssetDecoFeatB.Draw();
            AssetDecoFeatC.Draw();
            AssetDecoFeatD.Draw();
            AssetInventoryIcons.Draw();
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            Window.Update(dt);
            List.Update(dt);
            NameTopic.Update(dt);
            DetailTopic.Update(dt);
            FeatCountText.Update(dt);
            AssetDecoFeatA.Update(dt);
            AssetDecoFeatB.Update(dt);
            AssetDecoFeatC.Update(dt);
            AssetDecoFeatD.Update(dt);
            AssetInventoryIcons.Update(dt);
        }

        public override void Dispose()
        {
            List.OnActivated -= List_OnActivate;
        }
    }
}
