﻿using OpenNefia.Core.Maths;
using OpenNefia.Core.UI.Element;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.UI.Element
{
    public enum UiBoxFit
    {
        /// <summary>
        /// Center the element, but perform no scaling.
        /// </summary>
        None,

        /// <summary>
        /// Scale the element to be contained within the viewport.
        /// </summary>
        Contain
    }

    /// <summary>
    /// <para>
    /// A point within a rectangle.
    /// </para>
    /// <para>
    /// (0.0, 0.0) is center of the rectangle. (-1.0, -1.0) is the top left.
    /// (1.0, 1.0) is the bottom right.
    /// </para>
    /// </summary>
    public struct UiAlignment
    {
        /// <summary>
        /// Distance fraction in the vertical direction.
        /// </summary>
        public float X;

        /// <summary>
        /// Distance fraction in the horizontal direction.
        /// </summary>
        public float Y;

        /// <summary>
        /// The center point, both horizontally and vertically.
        /// </summary>
        public static readonly UiAlignment Center = new(0f, 0f);

        public UiAlignment(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns a box of the given size aligned within the given box with this
        /// alignment.
        /// </summary>
        // TODO needs to be UIBox2
        public UIBox2i Inscribe(Vector2 size, UIBox2i box)
        {
            float halfWidth = (box.Width - size.X) / 2.0F;
            float halfHeight = (box.Height - size.Y) / 2.0F;
            return UIBox2i.FromDimensions(new Vector2i((int)(box.Left + halfWidth + X * halfWidth),
                                                       (int)(box.Top + halfHeight + Y * halfHeight)), 
                                          (Vector2i)size);
        }
    }

    /// <param name="SourceSize">Subpart of the child element to show.</param>
    /// <param name="DestinationSize">Subpart of the parent element to show the child element in.</param>
    public record struct FittedSizes(Vector2 SourceSize, Vector2 DestinationSize);

    /// <summary>
    /// Container element for fitting a child UI element using size constraints.
    /// </summary>
    /// <remarks>
    /// Inspired by Flutter's BoxFit/FittedBox.
    /// </remarks>
    public class UiFittedBox : UiElement
    {
        private IUiElement? _child;
        public IUiElement? Child 
        {
            get => _child; 
            set
            {
                _child = value;
                RelayoutChild();
            }
        }

        public UiBoxFit BoxFit { get; set; }

        public UiAlignment Alignment { get; set; }

        public UiFittedBox(IUiElement? child = null)
        {
            Child = child;
        }

        public override void SetSize(int width, int height)
        {
            base.SetSize(width, height);
        }

        public override void SetPosition(int x, int y)
        {
            base.SetPosition(x, y);

            RelayoutChild();
        }

        private void RelayoutChild()
        {
            if (Child == null)
                return;

            Child.GetPreferredSize(out var preferredChildSize);
            var fitted = UiUtils.ApplyBoxFit(BoxFit, preferredChildSize, PixelSize);
            var aligned = Alignment.Inscribe(fitted.DestinationSize, GlobalPixelBounds);

            Child.SetPosition(aligned.Left, aligned.Top);
            Child.SetSize(aligned.Width, aligned.Height);
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            Child?.Update(dt);
        }

        public override void Draw()
        {
            base.Draw();
            Child?.Draw();
        }

        public override void Dispose()
        {
            Child?.Dispose();
        }
    }
}
