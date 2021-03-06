﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;

namespace MonoGame.Extended.Gui.Controls
{
    public abstract class GuiItemsControl : GuiControl
    {
        protected GuiItemsControl()
            : this(null)
        {
        }

        protected GuiItemsControl(GuiSkin skin) 
            : base(skin)
        {
        }

        private int _selectedIndex = -1;
        public virtual int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public virtual List<object> Items { get; } = new List<object>();
        public virtual Color SelectedTextColor { get; set; } = Color.White;
        public virtual Color SelectedItemColor { get; set; } = Color.CornflowerBlue;
        public virtual Thickness ItemPadding { get; set; } = new Thickness(4, 2);
        public virtual string NameProperty { get; set; }

        public event EventHandler SelectedIndexChanged;

        protected int FirstIndex;

        public object SelectedItem
        {
            get { return SelectedIndex >= 0 && SelectedIndex <= Items.Count - 1 ? Items[SelectedIndex] : null; }
            set { SelectedIndex = Items.IndexOf(value); }
        }

        public override void OnKeyPressed(IGuiContext context, KeyboardEventArgs args)
        {
            base.OnKeyPressed(context, args);

            if (args.Key == Keys.Down) ScrollDown();
            if (args.Key == Keys.Up) ScrollUp();
        }

        public override void OnScrolled(int delta)
        {
            base.OnScrolled(delta);

            if (delta < 0) ScrollDown();
            if (delta > 0) ScrollUp();
        }

        private void ScrollDown()
        {
            if (SelectedIndex < Items.Count - 1)
                SelectedIndex++;
        }

        private void ScrollUp()
        {
            if (SelectedIndex > 0)
                SelectedIndex--;
        }

        public override void OnPointerDown(IGuiContext context, GuiPointerEventArgs args)
        {
            base.OnPointerDown(context, args);

            var contentRectangle = GetContentRectangle(context);

            for (var i = FirstIndex; i < Items.Count; i++)
            {
                var itemRectangle = GetItemRectangle(context, i - FirstIndex, contentRectangle);

                if (itemRectangle.Contains(args.Position))
                {
                    SelectedIndex = i;
                    OnItemClicked(context, args);
                    break;
                }
            }
        }

        protected virtual void OnItemClicked(IGuiContext context, GuiPointerEventArgs args) { }

        protected TextInfo GetItemTextInfo(IGuiContext context, Rectangle itemRectangle, object item, Rectangle? clippingRectangle)
        {
            var textRectangle = new Rectangle(itemRectangle.X + ItemPadding.Left, itemRectangle.Y + ItemPadding.Top,
                itemRectangle.Width - ItemPadding.Right, itemRectangle.Height - ItemPadding.Bottom);
            var itemTextInfo = GetTextInfo(context, GetItemName(item), textRectangle, HorizontalAlignment.Left, VerticalAlignment.Top, clippingRectangle);
            return itemTextInfo;
        }

        private string GetItemName(object item)
        {
            if (item != null)
            {
                if (NameProperty != null)
                {
                    return item.GetType()
                        .GetRuntimeProperty(NameProperty)
                        .GetValue(item)
                        ?.ToString() ?? string.Empty;
                }

                return item.ToString();
            }

            return string.Empty;
        }

        protected Rectangle GetItemRectangle(IGuiContext context, int index, Rectangle contentRectangle)
        {
            var font = Font ?? context.DefaultFont;
            var itemHeight = font.LineHeight + ItemPadding.Top + ItemPadding.Bottom;
            return new Rectangle(contentRectangle.X, contentRectangle.Y + itemHeight * index, contentRectangle.Width, itemHeight);
        }

        protected void ScrollIntoView(IGuiContext context)
        {
            var contentRectangle = GetContentRectangle(context);
            var selectedItemRectangle = GetItemRectangle(context, SelectedIndex - FirstIndex, contentRectangle);

            if (selectedItemRectangle.Bottom > ClippingRectangle.Bottom)
                FirstIndex++;

            if (selectedItemRectangle.Top < ClippingRectangle.Top && FirstIndex > 0)
                FirstIndex--;
        }

        protected Size2 GetItemSize(IGuiContext context, Size2 availableSize, object item)
        {
            var text = GetItemName(item);
            var textInfo = GetTextInfo(context, text, new Rectangle(0, 0, (int)availableSize.Width, (int)availableSize.Height), HorizontalAlignment.Left, VerticalAlignment.Top);
            var itemWidth = textInfo.Size.X + ItemPadding.Size.Height;
            var itemHeight = textInfo.Size.Y + ItemPadding.Size.Width;

            return new Size2(itemWidth, itemHeight);
        }

        protected abstract Rectangle GetContentRectangle(IGuiContext context);

        protected void DrawItemList(IGuiContext context, IGuiRenderer renderer)
        {
            var contentRectangle = GetContentRectangle(context);

            for (var i = FirstIndex; i < Items.Count; i++)
            {
                var item = Items[i];
                var itemRectangle = GetItemRectangle(context, i - FirstIndex, contentRectangle);
                var itemTextInfo = GetItemTextInfo(context, itemRectangle, item, contentRectangle);
                var textColor = i == SelectedIndex ? SelectedTextColor : itemTextInfo.Color;

                if (SelectedIndex == i)
                    renderer.FillRectangle(itemRectangle, SelectedItemColor, contentRectangle);

                renderer.DrawText(itemTextInfo.Font, itemTextInfo.Text, itemTextInfo.Position + TextOffset, textColor,
                    itemTextInfo.ClippingRectangle);
            }
        }
    }
}