using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 布局小工具：CustomUIBuilder 只管把控件摆出来，这里负责把它们的尺寸、
    /// 内边距、装饰条调到设计稿的位置上。
    /// </summary>
    internal static class UiKit
    {
        // ================= 尺寸 =================

        public static LayoutElement Le(RectTransform rt)
        {
            if (rt == null)
            {
                return null;
            }
            LayoutElement le = rt.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = rt.gameObject.AddComponent<LayoutElement>();
            }
            return le;
        }

        /// <summary>把矩形的四边贴满父级（父级还有布局组的话会被覆盖，这里主要用于根容器）。</summary>
        public static void Stretch(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>固定高度。</summary>
        public static void Height(CustomUIElement el, float h)
        {
            if (el == null)
            {
                return;
            }
            Height(el.Rect, h);
        }

        public static void Height(RectTransform rt, float h)
        {
            LayoutElement le = Le(rt);
            if (le == null)
            {
                return;
            }
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleHeight = 0f;
        }

        /// <summary>吃掉剩余高度，minHeight 保证不会被压没。</summary>
        public static void FlexHeight(CustomUIElement el, float flex, float min)
        {
            if (el == null)
            {
                return;
            }
            FlexHeight(el.Rect, flex, min);
        }

        public static void FlexHeight(RectTransform rt, float flex, float min)
        {
            LayoutElement le = Le(rt);
            if (le == null)
            {
                return;
            }
            le.minHeight = min;
            le.preferredHeight = min;
            le.flexibleHeight = flex;
        }

        /// <summary>吃掉剩余宽度（横向弹性），minWidth 兜住不会被压没。</summary>
        public static void FlexWidth(CustomUIElement el, float flex, float min)
        {
            if (el == null)
            {
                return;
            }
            FlexWidth(el.Rect, flex, min);
        }

        public static void FlexWidth(RectTransform rt, float flex, float min)
        {
            LayoutElement le = Le(rt);
            if (le == null)
            {
                return;
            }
            le.minWidth = min;
            le.preferredWidth = min;
            le.flexibleWidth = flex;
        }

        /// <summary>固定宽度。</summary>
        public static void Width(CustomUIElement el, float w)
        {
            if (el == null)
            {
                return;
            }
            Width(el.Rect, w);
        }

        public static void Width(RectTransform rt, float w)
        {
            LayoutElement le = Le(rt);
            if (le == null)
            {
                return;
            }
            le.minWidth = w;
            le.preferredWidth = w;
            le.flexibleWidth = 0f;
        }

        /// <summary>保留理想宽度，但允许被父级拉伸/压缩（min 是压缩下限）。</summary>
        public static void Grow(CustomUIElement el, float flex, float min)
        {
            if (el == null)
            {
                return;
            }
            Grow(el.Rect, flex, min);
        }

        public static void Grow(RectTransform rt, float flex, float min)
        {
            LayoutElement le = Le(rt);
            if (le == null)
            {
                return;
            }
            le.minWidth = min;
            le.flexibleWidth = flex;
        }

        /// <summary>宽度铺满父级。</summary>
        public static void FillWidth(CustomUIElement el, float min)
        {
            if (el == null)
            {
                return;
            }
            FillWidth(el.Rect, min);
        }

        public static void FillWidth(RectTransform rt, float min)
        {
            LayoutElement le = Le(rt);
            if (le == null)
            {
                return;
            }
            le.minWidth = min;
            le.preferredWidth = min;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;
        }

        /// <summary>改布局组的 padding / spacing。</summary>
        public static void Tune(RectTransform rt, int left, int right, int top, int bottom, float spacing)
        {
            if (rt == null)
            {
                return;
            }
            HorizontalOrVerticalLayoutGroup group = rt.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group == null)
            {
                return;
            }
            group.padding = new RectOffset(left, right, top, bottom);
            group.spacing = spacing;
        }

        public static void Align(RectTransform rt, TextAnchor anchor)
        {
            if (rt == null)
            {
                return;
            }
            HorizontalOrVerticalLayoutGroup group = rt.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group != null)
            {
                group.childAlignment = anchor;
            }
        }

        // ================= 装饰件 =================

        /// <summary>
        /// 在指定父级下挂一张纯装饰图，不参与布局。
        /// anchorMin/anchorMax 传同一个点就是定尺寸，传 0/1 就是贴满。
        /// </summary>
        public static Image Decor(RectTransform parent, string name, Sprite sprite, Vector2 size, Vector2 pos, Vector2 anchor)
        {
            return Decor(parent, name, sprite, size, pos, anchor, anchor, new Vector2(0.5f, 1f), Color.white);
        }

        public static Image Decor(RectTransform parent, string name, Sprite sprite, Vector2 size, Vector2 pos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
        {
            if (parent == null)
            {
                return null;
            }
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            if (anchorMin == anchorMax)
            {
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
            }
            else
            {
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>贴满父级的底板，放在最底层当卡片背景。</summary>
        public static Image Backdrop(RectTransform parent, string name, Sprite sprite, Color color)
        {
            Image img = Decor(parent, name, sprite, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), color);
            if (img == null)
            {
                return null;
            }
            img.type = Image.Type.Sliced;
            img.transform.SetAsFirstSibling();
            return img;
        }

        /// <summary>按钮底部那条会亮起来的下划线。</summary>
        public static Image Underline(CustomUIElement target, Color color)
        {
            if (target == null || !target.IsAlive)
            {
                return null;
            }
            Image img = Decor(
                target.Rect,
                "ds_tab_underline",
                UiSkin.White(),
                new Vector2(0f, 2f),
                new Vector2(0f, 1f),
                new Vector2(0.14f, 0f),
                new Vector2(0.86f, 0f),
                new Vector2(0.5f, 0f),
                color);
            return img;
        }

        /// <summary>图标衬底：比格子底板亮一点的小方槽。</summary>
        public static Image IconWell(CustomUIElement cell, float size, Color color)
        {
            if (cell == null || !cell.IsAlive)
            {
                return null;
            }
            return IconWell(cell.Rect, size, color);
        }

        public static Image IconWell(RectTransform cell, float size, Color color)
        {
            if (cell == null)
            {
                return null;
            }
            Image img = Decor(
                cell,
                "ds_cell_well",
                UiSkin.Plate(6, 0.55f, 0),
                new Vector2(size, size),
                new Vector2(0f, -6f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                color);
            if (img != null)
            {
                img.type = Image.Type.Sliced;
                img.transform.SetSiblingIndex(1);
            }
            return img;
        }

        // ================= 组合件 =================

        /// <summary>
        /// 小节标题：左侧一根黄铜竖条 + 拉字距的小字。
        /// 宽度传 0 表示让它自己吃满。
        /// </summary>
        public static CustomUIElement Section(CustomUIBuilder b, string title, float width)
        {
            b.BeginRow(9f, TextAnchor.MiddleLeft);
            UiKit.Height(b.CurrentContainer, 22f);

            b.AddImage(UiSkin.White(), 3f, 13f);
            CustomUIElement bar = b.Last;
            if (bar != null)
            {
                if (bar.image != null)
                {
                    bar.image.type = Image.Type.Simple;
                    bar.image.preserveAspect = false;
                    bar.image.raycastTarget = false;
                }
                bar.SetImageColor(UiSkin.Brass);
            }

            b.AddLabel(title ?? "");
            CustomUIElement label = b.Last;
            if (label != null)
            {
                UiSkin.Eyebrow(label);
                if (width > 0f)
                {
                    label.SetPreferredSize(Mathf.Max(40f, width - 12f), 22f);
                }
                else
                {
                    UiKit.FillWidth(label, 40f);
                }
            }
            b.End();
            return label;
        }

        /// <summary>两端淡出的分割线。</summary>
        public static CustomUIElement Rule(CustomUIBuilder b, float width)
        {
            b.AddImage(UiSkin.Rule(), width > 0f ? width : 200f, 2f);
            CustomUIElement el = b.Last;
            if (el != null)
            {
                if (el.image != null)
                {
                    el.image.type = Image.Type.Simple;
                    el.image.preserveAspect = false;
                    el.image.raycastTarget = false;
                }
                el.SetImageColor(new Color(1f, 1f, 1f, 0.55f));
                if (width <= 0f)
                {
                    UiKit.FillWidth(el, 40f);
                }
                else
                {
                    el.SetPreferredSize(width, 2f);
                }
            }
            return el;
        }

        /// <summary>字段名，右对齐贴到输入框左边。</summary>
        public static CustomUIElement Caption(CustomUIBuilder b, string text, float width, float height)
        {
            b.AddLabel(text ?? "");
            CustomUIElement el = b.Last;
            if (el != null)
            {
                UiSkin.Caption(el);
                el.SetPreferredSize(width, height);
                if (el.text != null)
                {
                    el.text.alignment = TextAlignmentOptions.Left;
                }
            }
            return el;
        }

        /// <summary>
        /// 一行"说明 + 输入框 + 写入按钮"。调用前自己开 Row，调用后自己 End。
        /// 返回输入框，方便写回 fields。
        /// </summary>
        public static CustomUIElement Setting(CustomUIBuilder b, string caption, float captionW, string value, float inputW, Action onWrite, string buttonText)
        {
            Caption(b, caption, captionW, 28f);

            b.AddInput("", value ?? "", null, null, null);
            CustomUIElement input = b.Last;
            if (input != null)
            {
                input.SetPreferredSize(inputW, 28f);
            }

            b.AddButton(buttonText ?? UiText.BasicWrite, onWrite, null);
            CustomUIElement button = b.Last;
            if (button != null)
            {
                button.SetPreferredSize(52f, 28f);
                UiSkin.Ghost(button);
            }
            return input;
        }
    }
}
