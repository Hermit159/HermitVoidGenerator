using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 面板皮肤：调色板 + 全部贴图都是运行时现画的（Texture2D + Sprite.Create）。
    /// 模块没有文件读取能力，所以一个字节的外部资源都不依赖。
    /// </summary>
    internal static class UiSkin
    {
        // ================= 调色板 =================

        /// <summary>黄铜主色：选中、页签高亮、主按钮。</summary>
        public static readonly Color Brass = Col(0xE9, 0xC1, 0x79, 1f);
        public static readonly Color BrassSoft = Col(0xE9, 0xC1, 0x79, 0.30f);
        public static readonly Color BrassGlow = Col(0xE9, 0xC1, 0x79, 0.12f);

        /// <summary>冷青：副标题、编号、数据类文字。</summary>
        public static readonly Color Cyan = Col(0x7F, 0xD3, 0xE0, 1f);

        /// <summary>正文暖白。</summary>
        public static readonly Color Bone = Col(0xE7, 0xE1, 0xD2, 1f);

        /// <summary>说明文字。</summary>
        public static readonly Color Dim = Col(0x9A, 0x94, 0xA6, 1f);

        /// <summary>最弱一级：占位、脚注。</summary>
        public static readonly Color Mute = Col(0x6E, 0x69, 0x7B, 1f);

        public static readonly Color Ok = Col(0x8F, 0xD1, 0x8A, 1f);
        public static readonly Color Warn = Col(0xF0, 0xB4, 0x63, 1f);
        public static readonly Color Err = Col(0xE0, 0x7A, 0x76, 1f);

        /// <summary>卡片底色（乘在贴图上，所以略亮一点）。</summary>
        public static readonly Color CardTint = Col(0x2C, 0x25, 0x3F, 0.94f);
        public static readonly Color CardTintAlt = Col(0x24, 0x1E, 0x34, 0.92f);

        /// <summary>格子底板。</summary>
        public static readonly Color TileTint = Col(0x28, 0x22, 0x3A, 0.90f);
        public static readonly Color TileTintHover = Col(0x3A, 0x31, 0x54, 0.95f);
        public static readonly Color TileTintOn = Col(0xE9, 0xC1, 0x79, 0.52f);
        public static readonly Color TileTintOnHover = Col(0xE9, 0xC1, 0x79, 0.70f);

        // ================= 贴图工厂 =================

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static Color Col(int r, int g, int b, float a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        private static Sprite FromCache(string key, Func<Sprite> make)
        {
            Sprite found;
            if (cache.TryGetValue(key, out found) && found != null)
            {
                return found;
            }
            Sprite made = make();
            cache[key] = made;
            return made;
        }

        private static Texture2D NewTexture(int w, int h, string name)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        private static Sprite Bake(Texture2D tex, Vector4 border)
        {
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>纯白方块：给色块、竖条、进度底做遮罩用。</summary>
        public static Sprite White()
        {
            return FromCache("white", delegate
            {
                Texture2D tex = NewTexture(4, 4, "ds_white");
                Color32[] px = new Color32[16];
                for (int i = 0; i < px.Length; i++)
                {
                    px[i] = new Color32(255, 255, 255, 255);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return Bake(tex, Vector4.zero);
            });
        }

        /// <summary>
        /// 圆角矩形。fill 是填充亮度（1 = 和描边同色，0.7 = 填充比描边暗三成），
        /// borderPx 是描边像素宽度。九宫格切好，缩放不变形。
        /// </summary>
        public static Sprite Plate(int radius, float fill, int borderPx)
        {
            int r = Mathf.Clamp(radius, 1, 22);
            int b = Mathf.Clamp(borderPx, 0, 4);
            string key = "plate/" + r + "/" + Mathf.RoundToInt(fill * 100f) + "/" + b;
            return FromCache(key, delegate { return BuildPlate(r, fill, b); });
        }

        private static Sprite BuildPlate(int radius, float fill, int borderPx)
        {
            int inset = 3;
            int border = radius + inset + 2;
            int size = border * 2 + 8;
            float half = size * 0.5f;
            float extent = half - 0.5f - inset;

            Texture2D tex = NewTexture(size, size, "ds_plate");
            Color32[] px = new Color32[size * size];
            byte flat = (byte)Mathf.RoundToInt(Mathf.Clamp01(fill) * 255f);

            for (int y = 0; y < size; y++)
            {
                float py = (y + 0.5f) - half;
                for (int x = 0; x < size; x++)
                {
                    float pxx = (x + 0.5f) - half;
                    float sdf = RoundRect(pxx, py, extent, extent, radius);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - sdf) * 255f);
                    byte v = flat;
                    if (borderPx > 0)
                    {
                        float d = Mathf.Abs(sdf + borderPx * 0.5f) - (borderPx * 0.5f - 0.5f);
                        float t = Mathf.Clamp01(0.5f - d);
                        if (t > 0f)
                        {
                            v = (byte)Mathf.RoundToInt(Mathf.Lerp(flat, 255f, t));
                        }
                    }
                    px[y * size + x] = new Color32(v, v, v, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Bake(tex, new Vector4(border, border, border, border));
        }

        /// <summary>带符号距离场：小于 0 在圆角矩形里面。</summary>
        private static float RoundRect(float x, float y, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(x) - (halfW - radius);
            float qy = Mathf.Abs(y) - (halfH - radius);
            float ax = Mathf.Max(qx, 0f);
            float ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>两端淡出的细线。</summary>
        public static Sprite Rule()
        {
            return FromCache("rule", delegate
            {
                int w = 160;
                int h = 2;
                Texture2D tex = NewTexture(w, h, "ds_rule");
                Color32[] px = new Color32[w * h];
                for (int x = 0; x < w; x++)
                {
                    float t = (x + 0.5f) / w;
                    float a = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.55f);
                    Color32 c = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    px[x] = c;
                    px[w + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return Bake(tex, Vector4.zero);
            });
        }

        /// <summary>小圆点：状态灯。</summary>
        public static Sprite Dot()
        {
            return FromCache("dot", delegate
            {
                int size = 20;
                Texture2D tex = NewTexture(size, size, "ds_dot");
                Color32[] px = new Color32[size * size];
                float half = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    float dy = (y + 0.5f) - half;
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) - half;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) - (half - 2f);
                        byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - d) * 255f);
                        px[y * size + x] = new Color32(255, 255, 255, a);
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return Bake(tex, Vector4.zero);
            });
        }

        // ================= 现成组合 =================

        public static void CardLook(CustomUIElement el)
        {
            CardLook(el, CardTint);
        }

        public static void CardLook(CustomUIElement el, Color tint)
        {
            if (el == null || !el.IsAlive || el.image == null)
            {
                return;
            }
            el.image.sprite = Plate(10, 0.72f, 1);
            el.image.type = Image.Type.Sliced;
            el.image.color = tint;
            el.image.raycastTarget = false;
        }

        /// <summary>把元素换成九宫格圆角底，颜色和圆角自己定。</summary>
        public static void Plate(CustomUIElement el, int radius, float fill, int borderPx, Color tint)
        {
            if (el == null || !el.IsAlive || el.image == null)
            {
                return;
            }
            el.image.sprite = Plate(radius, fill, borderPx);
            el.image.type = Image.Type.Sliced;
            el.image.color = tint;
        }

        // ================= 文字 =================

        public static void Heading(CustomUIElement el, Color color, float size)
        {
            if (el == null || !el.IsAlive || el.text == null)
            {
                return;
            }
            el.text.color = color;
            el.text.fontSize = size;
            el.text.enableAutoSizing = false;
            el.text.fontStyle = FontStyles.Bold;
            el.text.characterSpacing = 2f;
            el.text.enableWordWrapping = true;
            el.text.overflowMode = TextOverflowModes.Ellipsis;
            el.text.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>小节标题：小号、拉开字距、黄铜色。</summary>
        public static void Eyebrow(CustomUIElement el)
        {
            if (el == null || !el.IsAlive || el.text == null)
            {
                return;
            }
            el.text.color = Brass;
            el.text.fontSize = 12.5f;
            el.text.enableAutoSizing = false;
            el.text.fontStyle = FontStyles.Bold;
            el.text.characterSpacing = 6f;
            el.text.enableWordWrapping = false;
            el.text.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>字段名。</summary>
        public static void Caption(CustomUIElement el)
        {
            if (el == null || !el.IsAlive || el.text == null)
            {
                return;
            }
            el.text.color = Dim;
            el.text.fontSize = 12.5f;
            el.text.enableAutoSizing = false;
            el.text.fontStyle = FontStyles.Normal;
            el.text.characterSpacing = 0f;
            el.text.enableWordWrapping = false;
            el.text.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>脚注 / 提示。</summary>
        public static void Note(CustomUIElement el)
        {
            if (el == null || !el.IsAlive || el.text == null)
            {
                return;
            }
            el.text.color = Mute;
            el.text.enableAutoSizing = true;
            el.text.fontSizeMin = 10f;
            el.text.fontSizeMax = 12.5f;
            el.text.enableWordWrapping = true;
            el.text.alignment = TextAlignmentOptions.TopLeft;
        }

        // ================= 按钮 =================

        /// <summary>次级按钮：淡底、灰字，按下去只是微微亮一下。</summary>
        public static void Ghost(CustomUIElement el)
        {
            if (el == null || !el.IsAlive)
            {
                return;
            }
            if (el.text != null)
            {
                el.text.color = Dim;
                el.text.fontSize = 12.5f;
                el.text.enableAutoSizing = false;
                el.text.fontStyle = FontStyles.Normal;
                el.text.characterSpacing = 1f;
            }
            if (el.image != null)
            {
                el.image.color = new Color(1f, 1f, 1f, 0.09f);
            }
            ApplyColors(el.button, 0.65f, 1f, 0.85f);
        }

        /// <summary>主按钮：黄铜实心、墨色字。</summary>
        public static void Primary(CustomUIElement el)
        {
            if (el == null || !el.IsAlive)
            {
                return;
            }
            if (el.root != null)
            {
                SkinMark mark = el.root.GetComponent<SkinMark>();
                if (mark == null)
                {
                    mark = el.root.AddComponent<SkinMark>();
                }
                mark.role = "primary";
            }
            if (el.text != null)
            {
                el.text.color = Col(0x1A, 0x14, 0x24, 1f);
                el.text.fontSize = 13f;
                el.text.enableAutoSizing = false;
                el.text.fontStyle = FontStyles.Bold;
                el.text.characterSpacing = 2f;
            }
            if (el.image != null)
            {
                el.image.sprite = Plate(9, 0.88f, 1);
                el.image.type = Image.Type.Sliced;
                el.image.color = Brass;
            }
            ApplyColors(el.button, 1f, 1f, 0.82f);
        }

        private static void ApplyColors(Button btn, float normalA, float hiA, float pressA)
        {
            if (btn == null)
            {
                return;
            }
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, normalA);
            cb.highlightedColor = new Color(1f, 1f, 1f, hiA);
            cb.pressedColor = new Color(1f, 1f, 1f, pressA);
            cb.selectedColor = new Color(1f, 1f, 1f, normalA);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;
            // Selectable 只在 OnEnable 里应用一次配色，这里逼它重算
            if (btn.gameObject.activeInHierarchy)
            {
                btn.enabled = false;
                btn.enabled = true;
            }
        }

        /// <summary>页签外观：active 是黄铜，非 active 是压暗的轮廓。</summary>
        public static void Tab(CustomUIElement el, bool active)
        {
            if (el == null || !el.IsAlive)
            {
                return;
            }
            if (el.root != null)
            {
                SkinMark mark = el.root.GetComponent<SkinMark>();
                if (mark == null)
                {
                    mark = el.root.AddComponent<SkinMark>();
                }
                mark.role = "tab";
            }
            if (el.text != null)
            {
                el.text.color = active ? Brass : Dim;
                el.text.enableAutoSizing = false;
                el.text.fontSize = 13.5f;
                el.text.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                el.text.characterSpacing = active ? 4f : 2f;
                el.text.alignment = TextAlignmentOptions.Center;
            }
            if (el.image != null)
            {
                el.image.sprite = Plate(11, 0.72f, 1);
                el.image.type = Image.Type.Sliced;
                el.image.color = active ? BrassSoft : new Color(1f, 1f, 1f, 0.05f);
            }
            ApplyColors(el.button, active ? 1f : 0.6f, 1f, 0.85f);
        }

        // ================= 全局统一的修饰 =================

        /// <summary>
        /// 扫一遍容器，把游戏自带的 header / separator / button 统一成本面板的风格。
        /// 名字前缀来自 CustomUIManager.SpawnElement 的命名规则（kind 的小写）。
        /// </summary>
        public static void Polish(Transform host)
        {
            if (host == null)
            {
                return;
            }
            try
            {
                Transform[] all = host.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t == null)
                    {
                        continue;
                    }
                    string name = t.name;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    if (t.GetComponentInParent<TMP_InputField>() != null || t.GetComponentInParent<TMP_Dropdown>() != null)
                    {
                        continue;
                    }
                    if (name.StartsWith("header", StringComparison.Ordinal))
                    {
                        StyleHeaderAt(t);
                    }
                    else if (name.StartsWith("separator", StringComparison.Ordinal))
                    {
                        StyleSeparatorAt(t);
                    }
                    else if (name.StartsWith("button", StringComparison.Ordinal))
                    {
                        if (t.GetComponent<SkinMark>() == null)
                        {
                            StyleGhostAt(t);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("界面修饰失败: " + e.Message);
            }
        }

        private static void StyleHeaderAt(Transform t)
        {
            TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return;
            }
            label.color = Brass;
            label.fontSize = 14f;
            label.enableAutoSizing = false;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 3f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.Left;
        }

        private static void StyleSeparatorAt(Transform t)
        {
            Image img = t.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0.07f);
                img.raycastTarget = false;
            }
        }

        private static void StyleGhostAt(Transform t)
        {
            Button btn = t.GetComponent<Button>();
            if (btn == null)
            {
                btn = t.GetComponentInChildren<Button>(true);
            }
            if (btn == null)
            {
                return;
            }
            TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = Dim;
                label.enableAutoSizing = false;
                label.fontStyle = FontStyles.Normal;
                label.characterSpacing = 1f;
            }
            Image img = t.GetComponent<Image>();
            if (img == null && btn.targetGraphic != null)
            {
                img = btn.targetGraphic as Image;
            }
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0.09f);
            }
            ApplyColors(btn, 0.65f, 1f, 0.85f);
        }

        /// <summary>按文字内容挑一个状态色：失败红、完成绿、其余暖白。</summary>
        public static Color StatusInk(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Dim;
            }
            if (LooksBad(text))
            {
                return Err;
            }
            if (LooksGood(text))
            {
                return Ok;
            }
            return Bone;
        }

        public static Color StatusLamp(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Mute;
            }
            if (LooksBad(text))
            {
                return Err;
            }
            if (LooksGood(text))
            {
                return Ok;
            }
            return Brass;
        }

        private static bool LooksBad(string text)
        {
            return text.IndexOf("失败", StringComparison.Ordinal) >= 0
                || text.IndexOf("错误", StringComparison.Ordinal) >= 0
                || text.IndexOf("不能", StringComparison.Ordinal) >= 0
                || text.IndexOf("要填", StringComparison.Ordinal) >= 0
                || text.IndexOf("无效", StringComparison.Ordinal) >= 0
                || text.IndexOf("造不出", StringComparison.Ordinal) >= 0
                || text.IndexOf("只在", StringComparison.Ordinal) >= 0
                || text.IndexOf("没有", StringComparison.Ordinal) >= 0
                || text.IndexOf("先", StringComparison.Ordinal) == 0;
        }

        private static bool LooksGood(string text)
        {
            return text.IndexOf("已", StringComparison.Ordinal) >= 0
                || text.IndexOf("完成", StringComparison.Ordinal) >= 0;
        }

        /// <summary>窗口标题：黄铜 + 拉字距。</summary>
        public static void StyleWindow(CustomUIWindow win)
        {
            if (win == null)
            {
                return;
            }
            CustomUIWindowFrame frame = win.frame;
            if (frame == null || frame.titleText == null)
            {
                return;
            }
            frame.titleText.text = UiText.Title;
            frame.titleText.color = Brass;
            frame.titleText.fontSize = 19f;
            frame.titleText.fontStyle = FontStyles.Bold;
            frame.titleText.characterSpacing = 4f;
            frame.titleText.enableAutoSizing = false;
            frame.titleText.enableWordWrapping = false;
        }
    }

    /// <summary>给元素打个记号，Polish 看到就不再粗暴改它。</summary>
    internal sealed class SkinMark : MonoBehaviour
    {
        public string role;
    }
}
