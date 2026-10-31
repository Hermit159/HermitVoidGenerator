using TMPro;
using UnityEngine;

namespace HermitVoidGenerator
{
    /// <summary>点选物品时的屏幕提示文字（不拦截鼠标）。</summary>
    internal static class HintOverlay
    {
        private static GameObject root;
        private static TextMeshProUGUI label;

        public static void Show(string message)
        {
            Ensure();
            if (root == null)
            {
                return;
            }
            if (label != null)
            {
                label.text = message ?? "";
            }
            root.SetActive(true);
        }

        public static void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public static void Destroy()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }
            root = null;
            label = null;
        }

        private static void Ensure()
        {
            if (root != null)
            {
                return;
            }

            try
            {
                root = new GameObject("HermitVoidGeneratorHint", typeof(RectTransform), typeof(Canvas));
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 950;
                CustomUIManager.AddScreenScaler(root);
                Object.DontDestroyOnLoad(root);

                GameObject textObject = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(root.transform, false);
                RectTransform rect = textObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 200f);
                rect.sizeDelta = new Vector2(1100f, 60f);

                label = textObject.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = UiText.PickHint;
                    label.fontSize = 26f;
                    label.alignment = TextAlignmentOptions.Center;
                    label.raycastTarget = false;
                    label.color = new Color(1f, 0.92f, 0.65f, 1f);
                }
            }
            catch (System.Exception e)
            {
                Log.Warn("提示浮层创建失败（不影响点选）: " + e.Message);
                root = null;
                label = null;
            }
        }
    }
}
