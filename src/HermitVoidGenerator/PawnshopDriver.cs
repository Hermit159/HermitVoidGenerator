using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HermitVoidGenerator
{
    /// <summary>常驻驱动：热键开关面板 + 世界里点选物品。</summary>
    public sealed class PawnshopDriver : MonoBehaviour
    {
        public const KeyCode Hotkey = KeyCode.Insert;
        public const string HotkeyLabel = "Insert";

        private static PawnshopDriver instance;
        private static bool picking;
        private static float pickBlockUntil;

        public static bool IsPicking
        {
            get { return picking; }
        }

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Log.Error("帧更新异常: " + e);
            }
        }

        private void OnDisable()
        {
            picking = false;
            HintOverlay.Hide();
            if (instance == this)
            {
                instance = null;
            }
        }

        private static void Tick()
        {
            if (Input.GetKeyDown(Hotkey) && !IsTyping())
            {
                if (picking)
                {
                    CancelPick();
                }
                PawnshopWindow.Toggle();
                return;
            }

            if (!picking)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPick();
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }
            if (Time.unscaledTime < pickBlockUntil)
            {
                return;
            }
            if (IsPointerOverUI())
            {
                return;
            }

            GameItem item = HoveredItem();
            if (item == null)
            {
                HintOverlay.Show("这里没有物品 —— " + UiText.PickHint);
                return;
            }

            picking = false;
            HintOverlay.Hide();
            PawnshopWindow.OnItemPicked(item);
            PawnshopWindow.ShowWindow();
            Log.Info("已选中 " + item.identifier);
        }

        public static void BeginPick()
        {
            if (instance == null)
            {
                Log.Error("驱动组件不在场景里，无法点选");
                return;
            }
            picking = true;
            pickBlockUntil = Time.unscaledTime + 0.2f;
            PawnshopWindow.Hide();
            HintOverlay.Show(UiText.PickHint);
        }

        private static void CancelPick()
        {
            picking = false;
            HintOverlay.Hide();
            PawnshopWindow.ShowWindow();
            PawnshopWindow.SetStatus("已取消点选");
        }

        private static bool IsTyping()
        {
            EventSystem system = EventSystem.current;
            if (system == null)
            {
                return false;
            }
            GameObject selected = system.currentSelectedGameObject;
            if (selected == null)
            {
                return false;
            }
            TMP_InputField field = selected.GetComponent<TMP_InputField>();
            return field != null && field.isFocused;
        }

        private static bool IsPointerOverUI()
        {
            EventSystem system = EventSystem.current;
            return system != null && system.IsPointerOverGameObject();
        }

        private static GameItem HoveredItem()
        {
            try
            {
                StoreUIManager storeUi = StoreUIManager.Instance;
                if (storeUi != null && storeUi.hoveringStoreUI)
                {
                    return null;
                }
                ItemMouseDragHandler drag = ItemMouseDragHandler.current;
                if (drag != null && drag.IsDraggingItem)
                {
                    return null;
                }

                GameItem item = RenderHandler.RaycastElement<GameItem>(Input.mousePosition);
                if (item == null || item.isDebugMenu)
                {
                    return null;
                }
                return item;
            }
            catch (Exception e)
            {
                Log.Warn("拾取物品出错: " + e.Message);
                return null;
            }
        }
    }
}
