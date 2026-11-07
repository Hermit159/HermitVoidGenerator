using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HermitVoidGenerator
{
    /// <summary>「Hermit的虚空生成器」主面板：无中生有（生成）+ 物品编辑。</summary>
    internal static partial class PawnshopWindow
    {
        private const string WindowId = "yg_deep_space_pawnshop_window";

        // ---- 窗口尺寸：1240 宽时 contentRoot 实测 1224、页区 1216（见 Player.log 布局 dump）
        //      两张卡按 754 + 8 + 450 = 1212 排，留 4px 余量；卡片不再带 flexibleWidth，禁止被布局压缩
        private const float WinW = 1240f;
        private const float WinH = 848f;
        private const float LeftW = 754f;
        private const float RightW = 450f;
        private const float InnerW = 400f;

        // 面板内边距（四边同值），左右栏的内容宽度都按它算出来
        private const float Pad = 12f;
        private const float LeftInner = LeftW - Pad * 2f;
        private const float RightInner = RightW - Pad * 2f;
        private const float Gap = 8f;

        private const int GridColumns = 4;
        private const float CellW = 172f;
        private const float CellH = 126f;
        private const int MaxCells = 160;

        private static readonly Color DimColor = new Color(0.604f, 0.580f, 0.651f, 1f);

        private static CustomUIWindow window;

        /// <summary>所有左侧/右侧主卡片。布局稳定后按卡片实测内宽回收子元素宽度，兜住一切越界。</summary>
        private static readonly List<CustomUIElement> cards = new List<CustomUIElement>();

        /// <summary>首次打开时打一次内容树，之后不再刷日志。</summary>
        private static bool dumpOnce = true;

        // ---- 外壳：页签 + 状态栏
        private static readonly List<CustomUIElement> tabButtons = new List<CustomUIElement>();
        private static readonly List<Image> tabUnderlines = new List<Image>();
        private static readonly List<RectTransform> pageRoots = new List<RectTransform>();
        private static CustomUIElement statusText;
        private static Image statusDot;
        private static int activeTab;

        // ---- 生成页
        private static RectTransform createGridHost;
        private static RectTransform createSpecHost;
        private static CustomUIElement draftTitle;
        private static CustomUIElement draftCount;
        private static CustomUIElement draftPrice;
        private static CustomUIElement draftWhere;

        private static CustomUIElement createGroupDropdown;
        private static CustomUIElement createSectionDropdown;
        private static readonly List<int> groupMap = new List<int>();
        private static readonly List<int> sectionMap = new List<int>();
        private static bool groupLock;
        private static bool sectionLock;

        private static string createFilter = "";
        private static int createGroupIndex;
        private static int createSectionIndex;
        private static string selectedId = "";
        private static GameItem draft;
        private static int whereIndex;
        private static Coroutine iconRoutine;

        // ---- 规格输入缓存（重建面板不丢打字）
        private static string pendingLiquidId = "";
        private static string liquidAmountText = "";
        private static string pendingActiveId = "";
        private static string waterPurityText = "";
        private static string chemPurityText = "";
        private static int pendingIngotIndex;

        // ---- 编辑页
        private static RectTransform editGridHost;
        private static RectTransform editFormHost;
        private static RectTransform editSpecHost;
        private static CustomUIElement editSpecScroll;
        private static CustomUIElement editInfo;
        private static CustomUIElement editIncludeStore;

        private static CustomUIElement editGroupDropdown;
        private static CustomUIElement editSearch;
        private static CustomUIElement editSectionDropdown;
        private static readonly List<int> editGroupMap = new List<int>();
        private static readonly List<int> editSectionMap = new List<int>();
        private static bool editGroupLock;
        private static bool editSectionLock;

        private static CustomUIElement fCount;
        private static CustomUIElement fValue;
        private static CustomUIElement fBase;
        private static CustomUIElement fLate;

        private static string editFilter = "";
        private static int editGroupIndex;
        private static int editSectionIndex;
        private static bool includeStore;

        // ---- 基础修改页
        private static CustomUIElement bCash;
        private static CustomUIElement bRent;
        private static CustomUIElement bRentDays;
        private static CustomUIElement bLoan;
        private static CustomUIElement bLoanDays;
        private static CustomUIElement bAttr;
        private static CustomUIElement bProjector;
        private static CustomUIElement bRetail;
        private static CustomUIElement bContraLow;
        private static CustomUIElement bContraMid;
        private static CustomUIElement bContraHigh;
        private static CustomUIElement bContraCrit;
        private static CustomUIElement bWound;
        private static CustomUIElement bWoundStable;
        private static CustomUIElement bFavor;
        private static CustomUIElement bCommissaryCred;
        // ---- 事件触发
        private static CustomUIElement bEventPick;
        private static CustomUIElement bEventDays;
        private static CustomUIElement bEventInfo;
        private static CustomUIElement bEventLive;
        private static List<EventCatalog.Entry> eventEntries;
        private static int eventIndex;
        private static readonly Dictionary<string, CustomUIElement> facInputs = new Dictionary<string, CustomUIElement>();
        private static readonly Dictionary<string, CustomUIElement> facTiers = new Dictionary<string, CustomUIElement>();
        private static int woundIndex;
        private static bool woundStable = true;

        // ---- 液体下拉的选项表
        private static List<string> liquidIds;
        private static List<string> liquidLabels;

        private sealed class CellRef
        {
            public string Id;
            public CustomUIElement Image;
            public CustomUIElement Label;
            public CellClickHandler Handler;
        }

        // ================= 对外入口 =================

        public static bool IsOpen
        {
            get { return window != null && window.IsAlive && window.IsVisible; }
        }

        public static void Toggle()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                ShowWindow();
            }
        }

        public static void ShowWindow()
        {
            try
            {
                EnsureBuilt();
                if (window == null || !window.IsAlive)
                {
                    return;
                }
                window.Show();
                RefreshCreateList();
                RebuildCreateSpec();
                RefreshEditList();
                RebuildEditForm();
                RebuildEditSpec();
            }
            catch (Exception e)
            {
                Log.Error("打开面板失败: " + e);
            }
        }

        public static void Hide()
        {
            try
            {
                if (window != null && window.IsAlive)
                {
                    window.Hide();
                }
            }
            catch (Exception e)
            {
                Log.Warn("关闭面板失败: " + e.Message);
            }
        }

        /// <summary>三个页面共用一个底部状态条，同一句提示不再各说一遍。</summary>
        public static void SetStatus(string text)
        {
            string value = text ?? "";
            try
            {
                if (statusText != null && statusText.IsAlive)
                {
                    statusText.SetText(value);
                    statusText.SetTextColor(UiSkin.StatusInk(value));
                }
                if (statusDot != null)
                {
                    statusDot.color = UiSkin.StatusLamp(value);
                }
            }
            catch
            {
            }
        }

        public static void OnItemPicked(GameItem item)
        {
            if (item == null)
            {
                return;
            }
            ItemEditor.Select(item);
            try
            {
                // 先让它在列表里露出来（筛选挡住就清掉筛选），再重建表单
                RevealInEditList(item);
                RebuildEditForm();
                RebuildEditSpec();
                RefreshEditList();
            }
            catch (Exception e)
            {
                Log.Error("显示拾取到的物品失败: " + e.Message);
            }
            string name = "";
            try
            {
                name = item.GetDisplayName(true);
            }
            catch
            {
                name = item.identifier;
            }
            SetStatus("已选中 " + name);
        }

        /// <summary>mod 关闭时调用。</summary>
        public static void Destroy()
        {
            StopIcons();
            DestroyDraft();
            try
            {
                if (window != null && window.IsAlive)
                {
                    window.Close();
                }
            }
            catch (Exception e)
            {
                Log.Warn("销毁面板失败: " + e.Message);
            }
            window = null;
            ClearHosts();
        }

        private static void ClearHosts()
        {
            createGridHost = null;
            createSpecHost = null;
            draftTitle = null;
            draftCount = null;
            draftPrice = null;
            draftWhere = null;
            editGridHost = null;
            editFormHost = null;
            editSpecHost = null;
            editSpecScroll = null;
            editInfo = null;
            editIncludeStore = null;
            fCount = null;
            fValue = null;
            fBase = null;
            fLate = null;
            bCash = null;
            bRent = null;
            bRentDays = null;
            bLoan = null;
            bLoanDays = null;
            bAttr = null;
            bProjector = null;
            bRetail = null;
            bContraLow = null;
            bContraMid = null;
            bContraHigh = null;
            bContraCrit = null;
            bWound = null;
            bWoundStable = null;
            bFavor = null;
            bCommissaryCred = null;
            bEventPick = null;
            bEventDays = null;
            bEventInfo = null;
            bEventLive = null;
            eventEntries = null;
            statusText = null;
            statusDot = null;
            tabButtons.Clear();
            tabUnderlines.Clear();
            pageRoots.Clear();
            cards.Clear();
            facInputs.Clear();
            facTiers.Clear();
        }

        // ================= 构建 =================

        private static void EnsureBuilt()
        {
            if (window != null && window.IsAlive)
            {
                return;
            }

            window = null;
            ClearHosts();
            try
            {
                Build();
            }
            catch (Exception e)
            {
                Log.Error("构建面板失败: " + e);
                window = null;
                try
                {
                    CustomUIManager manager = CustomUIManager.Instance;
                    CustomUIWindow dead = manager == null ? null : manager.GetWindow(WindowId);
                    if (dead != null)
                    {
                        dead.Close();
                    }
                }
                catch
                {
                    // 清理失败就算了
                }
                throw;
            }
        }

        private static void RebuildWindow()
        {
            try
            {
                if (window != null && window.IsAlive)
                {
                    window.Close();
                }
            }
            catch (Exception e)
            {
                Log.Warn("重建面板时关闭旧窗口失败: " + e.Message);
            }
            window = null;
            ClearHosts();
            try
            {
                EnsureBuilt();
            }
            catch (Exception e)
            {
                Log.Error("重建面板失败: " + e);
            }
        }

        private static void Build()
        {
            ClearHosts();

            string layer = CustomUIManager.HasLayer(CustomUIManager.LAYER_OVERLAY)
                ? CustomUIManager.LAYER_OVERLAY
                : CustomUIManager.LAYER_REAR;
            CustomUIBuilder b = CustomUIManager.Window(WindowId, UiText.Title, layer);
            if (b == null)
            {
                Log.Warn("拿不到窗口构建器，放弃建面板");
                return;
            }

            window = b.Handle;
            b.SetSize(WinW, WinH).SetDraggable(true).SetCloseOnEscape(true).OnClose(OnWindowClosed);
            UiSkin.StyleWindow(window);

            // 只对齐，不动几何：窗口 prefab 自带的留白要留着
            RectTransform root = b.CurrentContainer;
            if (root != null)
            {
                UiKit.Align(root, TextAnchor.UpperLeft);
            }

            BuildTabStrip(b);

            b.AddSpace(8f);
            UiKit.Rule(b, LeftW + RightW + Gap);
            b.AddSpace(10f);

            BuildPages(b);

            b.AddSpace(8f);
            BuildStatusBar(b);

            b.Show();
            window.Center();
            FitWindowToContent();
            SelectTab(0);
            UiSkin.Polish(window.ContentRoot);
            SetStatus(UiText.StatusReady);
            Log.Info("面板已构建");
        }

        /// <summary>
        /// 窗口 prefab 自带留白，实际可用宽度只能量。量出来不够放两张卡就把窗子加宽，
        /// 让内容永远刚好铺满——比赌一个写死的窗口尺寸可靠。
        /// </summary>
        private static void FitWindowToContent()
        {
            CustomUIManager mgr = CustomUIManager.Instance;
            if (mgr == null || !mgr.isActiveAndEnabled)
            {
                return;
            }
            mgr.StartCoroutine(FitWindowCo());
        }

        private static IEnumerator FitWindowCo()
        {
            yield return null;
            if (window == null || !window.IsAlive)
            {
                yield break;
            }

            RectTransform content = window.ContentRoot;
            if (content == null)
            {
                yield break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            // 先量卡片真实内宽，把写死超宽的横排收回来，再谈窗口要不要加宽
            ClampCards();
            yield return null;
            if (window == null || !window.IsAlive)
            {
                yield break;
            }
            content = window.ContentRoot;
            if (content == null)
            {
                yield break;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float share = content.rect.width;
            float shareH = content.rect.height;
            float needed = LeftW + Gap + RightW;
            float neededH = 34f + 8f + 2f + 10f + 790f + 8f + 30f;
            if (share <= 1f)
            {
                Log.Warn("量不到内容宽度，跳过自适应");
                yield break;
            }

            Log.Info("内容 " + share.ToString("0.#") + "x" + shareH.ToString("0.#")
                + "，需要 " + needed.ToString("0.#") + "x" + neededH.ToString("0.#"));

            float addW = needed - share;
            float addH = neededH - shareH;
            if (addW > 0.5f || addH > 0.5f)
            {
                float widerWin = WinW + (addW > 0f ? addW : 0f);
                float tallerWin = WinH + (addH > 0f ? addH : 0f);
                if (tallerWin > 980f)
                {
                    tallerWin = 980f;
                }
                window.SetSize(widerWin, tallerWin);
                window.Center();
            }

            yield return null;
            yield return new WaitForEndOfFrame();
            if (window == null || !window.IsAlive)
            {
                yield break;
            }
            ClampCards();
            LayoutRebuilder.ForceRebuildLayoutImmediate(window.ContentRoot);

            if (dumpOnce)
            {
                dumpOnce = false;
                int budget = 120;
                Log.Info("=== 内容树 ===");
                DumpTree(window.ContentRoot, "", 0, 0f, ref budget);
                Log.Info("=== 内容树结束 剩余预算 " + budget + " ===");
            }
        }

        /// <summary>顶栏：三枚页签 + 右侧热键提示。</summary>
        private static void BuildTabStrip(CustomUIBuilder b)
        {
            b.BeginRow(10f, TextAnchor.MiddleLeft);
            RectTransform strip = b.CurrentContainer;
            UiKit.Height(strip, 34f);
            UiKit.FillWidth(strip, 600f);

            AddTabButton(b, UiText.TabBasic, 0);
            AddTabButton(b, UiText.TabCreate, 1);
            AddTabButton(b, UiText.TabEdit, 2);

            b.AddFlexibleSpace();

            b.AddLabel(UiText.HintGlobal);
            CustomUIElement hint = b.Last;
            if (hint != null)
            {
                UiKit.Width(hint, 260f);
                UiKit.Height(hint, 20f);
                if (hint.text != null)
                {
                    hint.text.text = UiText.HintGlobal;
                    hint.text.color = UiSkin.Mute;
                    hint.text.fontSize = 12f;
                    hint.text.enableAutoSizing = false;
                    hint.text.enableWordWrapping = false;
                    hint.text.characterSpacing = 1f;
                    hint.text.alignment = TextAlignmentOptions.Right;
                    hint.text.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            b.End();
        }

        private static void AddTabButton(CustomUIBuilder b, string label, int index)
        {
            b.AddButton(label, delegate { SelectTab(index); }, null);
            CustomUIElement tab = b.Last;
            if (tab == null)
            {
                return;
            }
            UiKit.Width(tab, 132f);
            UiKit.Height(tab, 34f);
            UiSkin.Tab(tab, index == activeTab);
            tabButtons.Add(tab);
            tabUnderlines.Add(UiKit.Underline(tab, UiSkin.Brass));
        }

        /// <summary>切页：只留一页可见，页签跟着换色，当前页签带一道下划线。</summary>
        private static void SelectTab(int index)
        {
            if (index < 0 || index >= pageRoots.Count)
            {
                return;
            }

            activeTab = index;

            for (int i = 0; i < pageRoots.Count; i++)
            {
                RectTransform page = pageRoots[i];
                if (page != null)
                {
                    page.gameObject.SetActive(i == index);
                }
            }

            for (int i = 0; i < tabButtons.Count; i++)
            {
                CustomUIElement tab = tabButtons[i];
                if (tab != null && tab.IsAlive)
                {
                    UiSkin.Tab(tab, i == index);
                }
                if (i < tabUnderlines.Count && tabUnderlines[i] != null)
                {
                    tabUnderlines[i].enabled = i == index;
                }
            }

            CustomUIManager mgr = CustomUIManager.Instance;
            if (mgr != null && mgr.isActiveAndEnabled)
            {
                Log.Info("=== 布局诊断 tab=" + index + " ===");
                mgr.StartCoroutine(DumpLayoutCo());
            }
        }

        /// <summary>临时诊断：编辑页越界时把关键 rect 的真实尺寸写进日志。</summary>
        private static IEnumerator DumpLayoutCo()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            Log.Info("=== 布局诊断 开始 ===");
            LogRect("windowRoot", window == null ? null : window.Rect);
            RectTransform rt = window == null ? null : window.ContentRoot;
            LogRect("contentRoot", rt);
            if (rt != null)
            {
                for (int i = 0; i < rt.childCount; i++)
                {
                    LogRect("  child[" + i + "]", rt.GetChild(i) as RectTransform);
                }
            }
            if (pageRoots.Count > 2)
            {
                LogRect("pageEdit", pageRoots[2]);
            }
            LogRect("editFormHost", editFormHost);
            LogRect("editSpecScroll", editSpecScroll == null ? null : editSpecScroll.Rect);
            if (editSpecScroll != null && editSpecScroll.root != null)
            {
                ScrollRect sr = editSpecScroll.root.GetComponent<ScrollRect>();
                if (sr != null)
                {
                    LogRect("  viewport", sr.viewport);
                    LogRect("  content", sr.content);
                }
            }
            LogRect("editSpecHost", editSpecHost);
            if (editSpecHost != null)
            {
                int n = editSpecHost.childCount < 16 ? editSpecHost.childCount : 16;
                for (int i = 0; i < n; i++)
                {
                    LogRect("    spec[" + i + "]", editSpecHost.GetChild(i) as RectTransform);
                }
            }
            Log.Info("=== 布局诊断 结束 ===");
        }

        private static void LogRect(string tag, RectTransform t)
        {
            if (t == null)
            {
                Log.Info(tag + ": null");
                return;
            }
            Rect r = t.rect;
            Vector3 pos = t.localPosition;
            LayoutElement le = t.GetComponent<LayoutElement>();
            string extra = le == null
                ? "le=-"
                : "minW=" + le.minWidth.ToString("0.#") + " prefW=" + le.preferredWidth.ToString("0.#") + " flexW=" + le.flexibleWidth.ToString("0.#");
            Log.Info(tag + ": w=" + r.width.ToString("0.#") + " h=" + r.height.ToString("0.#")
                + " x=" + pos.x.ToString("0.#") + " px=" + t.pivot.x.ToString("0.##")
                + " " + extra + " name=" + t.name);
        }

        /// <summary>
        /// 兜底：布局稳定后量每张卡片的真实内宽，把所有超宽的 LayoutElement 收回来。
        /// 卡内表单行宽度是按常量写死的（LeftInner / RightInner / InnerW），一旦面板被压缩或 prefab 留白
        /// 比预想的大，写死的行就会顶出卡外、连带顶出窗口。这里统一按实测宽度回收，宁可挤一点也不能漏出去。
        /// </summary>
        private static void ClampCards()
        {
            float narrowest = 0f;
            for (int i = 0; i < cards.Count; i++)
            {
                CustomUIElement el = cards[i];
                if (el == null || !el.IsAlive || el.Rect == null)
                {
                    continue;
                }
                float inner = el.Rect.rect.width - Pad * 2f;
                if (inner < 80f)
                {
                    continue;
                }
                if (narrowest < 1f || inner < narrowest)
                {
                    narrowest = inner;
                }
                ClampWidths(el.Rect, inner);
            }
            if (narrowest > 1f)
            {
                Log.Info("卡片实测内宽最小 " + narrowest.ToString("0.#") + "（预算 LeftInner " + LeftInner.ToString("0.#")
                    + " / RightInner " + RightInner.ToString("0.#") + " / InnerW " + InnerW.ToString("0.#") + "）");
            }
        }

        private static void ClampWidths(RectTransform host, float maxW)
        {
            if (host == null)
            {
                return;
            }
            LayoutElement[] all = host.GetComponentsInChildren<LayoutElement>(true);
            int fixedCount = 0;
            for (int i = 0; i < all.Length; i++)
            {
                LayoutElement le = all[i];
                if (le == null)
                {
                    continue;
                }
                if (le.minWidth > maxW)
                {
                    le.minWidth = maxW;
                    fixedCount++;
                }
                if (le.preferredWidth > maxW)
                {
                    le.preferredWidth = maxW;
                }
            }
            if (fixedCount > 0)
            {
                Log.Warn("回收了 " + fixedCount + " 个超宽元素到 " + maxW.ToString("0.#") + "px");
            }
        }

        /// <summary>临时诊断：整棵内容树带路径打印，能一眼看出谁逃出了父容器。</summary>
        private static void DumpTree(RectTransform t, string path, int depth, float parentW, ref int budget)
        {
            if (t == null || budget <= 0 || depth > 5)
            {
                return;
            }
            budget--;
            Rect r = t.rect;
            LayoutElement le = t.GetComponent<LayoutElement>();
            string extra = le == null
                ? ""
                : " minW=" + le.minWidth.ToString("0.#") + " prefW=" + le.preferredWidth.ToString("0.#") + " flexW=" + le.flexibleWidth.ToString("0.#");
            string flag = parentW > 1f && r.width > parentW + 0.5f ? "  <<< 超出父级" : "";
            Log.Info("布局 " + path + "/" + t.name + " w=" + r.width.ToString("0.#") + " h=" + r.height.ToString("0.#")
                + " x=" + t.localPosition.x.ToString("0.#") + extra + flag);
            for (int i = 0; i < t.childCount; i++)
            {
                DumpTree(t.GetChild(i) as RectTransform, path + "/" + t.name, depth + 1, r.width, ref budget);
            }
        }

        /// <summary>三页并排叠放，只留一页可见（不用游戏自带的 tabs：它会把当前页签置灰）。</summary>
        private static void BuildPages(CustomUIBuilder b)
        {
            b.BeginColumn(0f, TextAnchor.UpperLeft);
            RectTransform host = b.CurrentContainer;
            UiKit.FlexHeight(host, 1f, 790f);
            UiKit.FlexWidth(host, 1f, 600f);

            AddPage(b, BuildBasicPane);
            AddPage(b, BuildCreatePane);
            AddPage(b, BuildEditPane);

            b.End();
        }

        private static void AddPage(CustomUIBuilder b, Action<CustomUIBuilder> build)
        {
            b.BeginColumn(0f, TextAnchor.UpperLeft);
            RectTransform page = b.CurrentContainer;
            UiKit.FlexHeight(page, 1f, 790f);
            UiKit.FlexWidth(page, 1f, 600f);
            build(b);
            b.End();
            pageRoots.Add(page);
        }

        /// <summary>底部状态条：一枚状态灯 + 一句话。</summary>
        private static void BuildStatusBar(CustomUIBuilder b)
        {
            b.BeginRow(9f, TextAnchor.MiddleLeft);
            RectTransform bar = b.CurrentContainer;
            UiKit.Height(bar, 30f);
            UiKit.FillWidth(bar, 600f);

            Image bg = bar.gameObject.AddComponent<Image>();
            bg.sprite = UiSkin.Plate(8, 0.72f, 1);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.13f, 0.11f, 0.18f, 0.55f);
            bg.raycastTarget = false;

            b.AddImage(UiSkin.Dot(), 9f, 9f, null);
            statusDot = b.Last == null ? null : b.Last.image;
            if (statusDot != null)
            {
                statusDot.color = UiSkin.Mute;
                statusDot.raycastTarget = false;
                statusDot.preserveAspect = true;
            }

            b.AddLabel(UiText.StatusReady);
            statusText = b.Last;
            if (statusText != null)
            {
                UiKit.FlexWidth(statusText, 1f, 200f);
                UiKit.Height(statusText, 20f);
                if (statusText.text != null)
                {
                    statusText.text.text = UiText.StatusReady;
                    statusText.text.color = UiSkin.Dim;
                    statusText.text.fontSize = 12.5f;
                    statusText.text.enableAutoSizing = false;
                    statusText.text.enableWordWrapping = false;
                    statusText.text.alignment = TextAlignmentOptions.Left;
                    statusText.text.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            b.End();
        }

        private static void OnWindowClosed()
        {
            StopIcons();
            DestroyDraft();
            window = null;
            ClearHosts();
        }

        // ================= 基础修改 =================

        /// <summary>两张主卡片：左边目录/表单，右边规格/草稿。统一底色、圆角、内边距。</summary>
        private static void Card(CustomUIBuilder b, Color tint)
        {
            CustomUIElement el = b.Last;
            if (el == null)
            {
                return;
            }
            UiSkin.CardLook(el, tint);
            if (!cards.Contains(el))
            {
                cards.Add(el);
            }
            UiKit.FlexHeight(el.Rect, 1f, 520f);
            // 注意：这里绝不加 flexibleWidth。卡片是定宽容器，一旦允许被压缩，
            // 卡内写死宽度的表单行就会顶出卡外、连带顶出窗口（右边那排编辑框就是这么跑出去的）。
        }

        /// <summary>开一条表单行（定宽定高，不靠父级推算）。</summary>
        private static void BeginFormRow(CustomUIBuilder b, float width)
        {
            b.BeginRow(6f, TextAnchor.MiddleLeft);
            UiKit.Height(b.CurrentContainer, 30f);
            UiKit.Width(b.CurrentContainer, width);
        }

        private static CustomUIElement InputBox(CustomUIBuilder b, string value, float width)
        {
            b.AddInput("", value ?? "", null, null, null);
            CustomUIElement input = b.Last;
            if (input != null)
            {
                UiKit.Width(input, width);
                UiKit.Height(input, 28f);
            }
            return input;
        }

        private static void WriteButton(CustomUIBuilder b, Action onClick, float width)
        {
            b.AddButton(UiText.BasicWrite, onClick, null);
            CustomUIElement btn = b.Last;
            if (btn == null)
            {
                return;
            }
            UiKit.Width(btn, width);
            UiKit.Height(btn, 28f);
            UiSkin.Ghost(btn);
        }

        /// <summary>小节标题：一根黄铜竖条 + 字距拉开的标题，右侧可以挂一个动作按钮。</summary>
        private static void SectionLine(CustomUIBuilder b, string title, string actionLabel, Action action)
        {
            b.BeginRow(9f, TextAnchor.MiddleLeft);
            UiKit.Height(b.CurrentContainer, 24f);
            // 不写死宽度：左卡 692、右卡 400，跟着父级走才不会顶出面板
            UiKit.Grow(b.CurrentContainer, 1f, 0f);

            b.AddImage(UiSkin.White(), 3f, 13f, null);
            CustomUIElement mark = b.Last;
            if (mark != null)
            {
                if (mark.image != null)
                {
                    mark.image.color = UiSkin.Brass;
                    mark.image.raycastTarget = false;
                }
                UiKit.Width(mark, 3f);
                UiKit.Height(mark, 13f);
            }

            b.AddLabel(title);
            CustomUIElement head = b.Last;
            if (head != null)
            {
                UiSkin.Eyebrow(head);
                UiKit.FlexWidth(head, 1f, 60f);
                UiKit.Height(head, 20f);
            }

            if (action != null)
            {
                b.AddButton(actionLabel, action, null);
                CustomUIElement btn = b.Last;
                if (btn != null)
                {
                    UiKit.Width(btn, 84f);
                    UiKit.Height(btn, 22f);
                    if (btn.text != null)
                    {
                        btn.text.fontSize = 11.5f;
                        btn.text.enableAutoSizing = false;
                    }
                    UiSkin.Ghost(btn);
                }
            }

            b.End();
        }

        private static void BuildBasicPane(CustomUIBuilder b)
        {
            b.BeginRow(Gap, TextAnchor.UpperLeft);
            UiKit.FlexHeight(b.CurrentContainer, 1f, 760f);
            UiKit.FlexWidth(b.CurrentContainer, 1f, 600f);

            BuildBasicLeft(b);
            BuildBasicRight(b);

            b.End();
        }

        private static void BuildBasicLeft(CustomUIBuilder b)
        {
            b.BeginPanel(LeftW, -1f, TextAnchor.UpperLeft, 12, 7, null);
            Card(b, UiSkin.CardTint);

            // ---- 钱与账目
            SectionLine(b, UiText.BasicMoney, UiText.BasicRefresh, OnRefreshBasic);

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicCash, 96f, 28f);
            bCash = InputBox(b, BasicStats.CashText(), 190f);
            b.AddFlexibleSpace();
            WriteButton(b, OnWriteCash, 56f);
            b.End();

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicRent, 96f, 28f);
            bRent = InputBox(b, BasicStats.RentText(), 110f);
            UiKit.Caption(b, UiText.BasicDaysLeft, 76f, 28f);
            bRentDays = InputBox(b, BasicStats.RentDaysText(), 64f);
            b.AddFlexibleSpace();
            WriteButton(b, OnWriteRent, 56f);
            b.End();

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicLoan, 96f, 28f);
            bLoan = InputBox(b, BasicStats.LoanText(), 110f);
            UiKit.Caption(b, UiText.BasicDaysLeft, 76f, 28f);
            bLoanDays = InputBox(b, BasicStats.LoanDaysText(), 64f);
            b.AddFlexibleSpace();
            WriteButton(b, OnWriteLoan, 56f);
            b.End();

            b.AddSpace(4f);
            UiKit.Rule(b, LeftInner);

            // ---- 店铺
            SectionLine(b, UiText.BasicStore, null, null);

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicAttr, 96f, 28f);
            bAttr = InputBox(b, BasicStats.AttractText(), 110f);
            WriteButton(b, OnWriteAttr, 56f);
            b.AddSpace(22f);
            UiKit.Caption(b, UiText.BasicProjector, 72f, 28f);
            bProjector = InputBox(b, BasicStats.ProjectorText(), 110f);
            WriteButton(b, OnWriteProjector, 56f);
            b.End();

            b.AddSpace(4f);
            UiKit.Rule(b, LeftInner);

            // ---- 定价加成
            SectionLine(b, UiText.BasicPricing, null, null);

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicRetail, 120f, 28f);
            bRetail = InputBox(b, BasicStats.RetailMarkupText(), 110f);
            WriteButton(b, OnWriteRetail, 56f);
            b.AddFlexibleSpace();
            UiKit.Caption(b, UiText.BasicContraLow, 120f, 28f);
            bContraLow = InputBox(b, BasicStats.ContrabandLowText(), 110f);
            WriteButton(b, OnWriteContraLow, 56f);
            b.End();

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicContraMid, 120f, 28f);
            bContraMid = InputBox(b, BasicStats.ContrabandMidText(), 110f);
            WriteButton(b, OnWriteContraMid, 56f);
            b.AddFlexibleSpace();
            UiKit.Caption(b, UiText.BasicContraHigh, 120f, 28f);
            bContraHigh = InputBox(b, BasicStats.ContrabandHighText(), 110f);
            WriteButton(b, OnWriteContraHigh, 56f);
            b.End();

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicContraCrit, 120f, 28f);
            bContraCrit = InputBox(b, BasicStats.ContrabandCriticalText(), 110f);
            WriteButton(b, OnWriteContraCrit, 56f);
            b.AddFlexibleSpace();
            b.End();

            b.AddSpace(4f);
            UiKit.Rule(b, LeftInner);

            // ---- 恩惠与配给
            SectionLine(b, UiText.BasicRelation, null, null);

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicFavor, 96f, 28f);
            bFavor = InputBox(b, BasicStats.FavorText(), 110f);
            bFavor.SetTooltip("王尔德的恩惠（PlayerStore.wildFavor）。夜里找乔·王尔德买服务时优先拿它抵扣，不够的部分才从现金补。");
            WriteButton(b, OnWriteFavor, 56f);
            b.AddSpace(18f);
            UiKit.Caption(b, UiText.BasicCommissary, 120f, 28f);
            bCommissaryCred = InputBox(b, BasicStats.CommissaryCreditText(), 110f);
            bCommissaryCred.SetTooltip("配给站券余额（PlayerStore.commissaryData.redeemedCredit）。在配给站兑换配给券会累加到这里，配给站买东西只扣这个数。");
            WriteButton(b, OnWriteCommissaryCredit, 56f);
            b.End();

            // ---- 事件
            BuildEventSection(b);

            b.End();
        }

        // ==================== 事件触发 ====================

        private static EventCatalog.Entry CurrentEvent()
        {
            if (eventEntries == null || eventEntries.Count == 0)
            {
                return null;
            }
            if (eventIndex < 0 || eventIndex >= eventEntries.Count)
            {
                return null;
            }
            return eventEntries[eventIndex];
        }

        /// <summary>事件区：下拉选游戏本体事件、看简介、填天数、一键塞进日历。</summary>
        private static void BuildEventSection(CustomUIBuilder b)
        {
            b.AddSpace(4f);
            UiKit.Rule(b, LeftInner);

            SectionLine(b, UiText.BasicEvent, UiText.BasicEventRefresh, OnRefreshEventStatus);

            eventEntries = EventCatalog.All();
            if (eventEntries == null || eventEntries.Count == 0)
            {
                eventIndex = 0;
                b.AddLabel(UiText.BasicEventNoSave);
                CustomUIElement empty = b.Last;
                if (empty != null)
                {
                    UiSkin.Note(empty);
                    UiKit.Width(empty, LeftInner);
                    UiKit.Height(empty, 24f);
                }
                return;
            }
            if (eventIndex < 0 || eventIndex >= eventEntries.Count)
            {
                eventIndex = 0;
            }

            List<string> labels = new List<string>(eventEntries.Count);
            for (int i = 0; i < eventEntries.Count; i++)
            {
                labels.Add(EventCatalog.Label(eventEntries[i]));
            }
            EventCatalog.Entry current = CurrentEvent();

            BeginFormRow(b, LeftInner);
            UiKit.Caption(b, UiText.BasicEventPick, 34f, 28f);

            b.AddDropdown(labels, eventIndex, OnEventChanged, null);
            bEventPick = b.Last;
            if (bEventPick != null)
            {
                UiKit.Width(bEventPick, 312f);
                UiKit.Height(bEventPick, 30f);
            }

            UiKit.Caption(b, UiText.BasicEventDays, 62f, 28f);
            bEventDays = InputBox(b, current == null ? "1" : current.DefaultDays.ToString(), 56f);
            if (bEventDays != null)
            {
                bEventDays.SetTooltip(UiText.BasicEventDaysHint);
            }

            b.AddFlexibleSpace();

            b.AddButton(UiText.BasicEventTrigger, OnTriggerEvent, null);
            CustomUIElement trigger = b.Last;
            if (trigger != null)
            {
                UiKit.Width(trigger, 72f);
                UiKit.Height(trigger, 30f);
                UiSkin.Primary(trigger);
            }
            b.End();

            b.AddLabel(EventCatalog.InfoText(current));
            bEventInfo = b.Last;
            if (bEventInfo != null)
            {
                UiSkin.Note(bEventInfo);
                UiKit.Width(bEventInfo, LeftInner);
                UiKit.Height(bEventInfo, 104f);
            }

            b.AddLabel(UiText.BasicEventActive + EventCatalog.ActiveSummary());
            bEventLive = b.Last;
            if (bEventLive != null)
            {
                UiKit.Width(bEventLive, LeftInner);
                UiKit.Height(bEventLive, 18f);
                if (bEventLive.text != null)
                {
                    bEventLive.text.color = UiSkin.Brass;
                    bEventLive.text.fontSize = 11.5f;
                    bEventLive.text.enableAutoSizing = false;
                    bEventLive.text.enableWordWrapping = false;
                    bEventLive.text.alignment = TextAlignmentOptions.Left;
                    bEventLive.text.overflowMode = TextOverflowModes.Ellipsis;
                }
            }
        }

        private static void OnEventChanged(int index)
        {
            eventIndex = index;
            EventCatalog.Entry entry = CurrentEvent();
            if (entry == null)
            {
                return;
            }
            if (bEventDays != null)
            {
                bEventDays.SetString(entry.DefaultDays.ToString());
            }
            if (bEventInfo != null)
            {
                bEventInfo.SetText(EventCatalog.InfoText(entry));
            }
            SetBasicStatus(EventCatalog.Label(entry));
        }

        private static void OnTriggerEvent()
        {
            string message;
            bool ok = EventCatalog.Trigger(CurrentEvent(), TextOf(bEventDays), out message);
            ApplyBasic(ok ? null : message, message);
            if (!ok)
            {
                return;
            }
            RefreshEventLive();
        }

        private static void OnRefreshEventStatus()
        {
            RefreshEventLive();
            SetBasicStatus(EventCatalog.ActiveSummary());
        }

        private static void RefreshEventLive()
        {
            if (bEventLive != null)
            {
                bEventLive.SetText(UiText.BasicEventActive + EventCatalog.ActiveSummary());
            }
        }

        private static void BuildBasicRight(CustomUIBuilder b)
        {
            b.BeginPanel(RightW, -1f, TextAnchor.UpperLeft, 12, 7, null);
            Card(b, UiSkin.CardTintAlt);

            // ---- 阵营声望
            SectionLine(b, UiText.BasicFactions, null, null);

            List<BasicStats.FactionRow> factions = BasicStats.Factions();
            if (factions == null || factions.Count == 0)
            {
                b.AddLabel(UiText.BasicFactionNone);
                CustomUIElement none = b.Last;
                if (none != null)
                {
                    UiSkin.Note(none);
                    UiKit.Width(none, RightInner);
                    UiKit.Height(none, 26f);
                }
            }
            else
            {
                for (int i = 0; i < factions.Count; i++)
                {
                    BasicStats.FactionRow faction = factions[i];
                    string fid = faction.Id;

                    BeginFormRow(b, RightInner);
                    UiKit.Height(b.CurrentContainer, 28f);

                    b.AddLabel(faction.Name);
                    CustomUIElement name = b.Last;
                    if (name != null)
                    {
                        UiSkin.Caption(name);
                        UiKit.Width(name, 88f);
                        UiKit.Height(name, 26f);
                        name.SetTooltip(fid);
                    }

                    CustomUIElement repBox = InputBox(b, BasicStats.RepText(fid), 62f);
                    facInputs[fid] = repBox;

                    b.AddButton(UiText.BasicWrite, delegate { OnWriteRep(fid); }, null);
                    CustomUIElement btn = b.Last;
                    if (btn != null)
                    {
                        UiKit.Width(btn, 44f);
                        UiKit.Height(btn, 26f);
                        if (btn.text != null)
                        {
                            btn.text.fontSize = 12f;
                            btn.text.enableAutoSizing = false;
                        }
                        UiSkin.Ghost(btn);
                    }

                    b.AddLabel(BasicStats.RepTierText(fid));
                    CustomUIElement tier = b.Last;
                    if (tier != null)
                    {
                        UiKit.Width(tier, RightInner - 216f);
                        UiKit.Height(tier, 26f);
                        if (tier.text != null)
                        {
                            tier.text.text = BasicStats.RepTierText(fid);
                            tier.text.fontSize = 11.5f;
                            tier.text.color = UiSkin.Mute;
                            tier.text.enableAutoSizing = false;
                            tier.text.enableWordWrapping = false;
                            tier.text.alignment = TextAlignmentOptions.Right;
                            tier.text.overflowMode = TextOverflowModes.Ellipsis;
                        }
                    }

                    facTiers[fid] = tier;
                    b.End();
                }
            }

            b.AddSpace(4f);
            UiKit.Rule(b, RightInner);

            // ---- 伤势
            SectionLine(b, UiText.BasicHealth, null, null);

            woundIndex = BasicStats.WoundOptionIndex();
            woundStable = BasicStats.WoundStable();

            b.AddDropdown(BasicStats.WoundOptions, woundIndex, OnWoundChanged, null);
            bWound = b.Last;
            if (bWound != null)
            {
                UiKit.Width(bWound, RightInner);
                UiKit.Height(bWound, 32f);
            }

            b.AddToggle(UiText.BasicWoundStable, woundStable, OnWoundStableChanged, null);
            bWoundStable = b.Last;
            if (bWoundStable != null)
            {
                UiKit.Width(bWoundStable, RightInner);
                UiKit.Height(bWoundStable, 28f);
            }

            b.AddButton(UiText.BasicWrite, OnWriteWound, null);
            CustomUIElement woundBtn = b.Last;
            if (woundBtn != null)
            {
                UiKit.Width(woundBtn, 120f);
                UiKit.Height(woundBtn, 30f);
                UiSkin.Ghost(woundBtn);
            }

            b.AddSpace(2f);
            b.AddLabel(UiText.BasicHint);
            CustomUIElement hint = b.Last;
            if (hint != null)
            {
                UiSkin.Note(hint);
                UiKit.Width(hint, RightInner);
                UiKit.FlexHeight(hint.Rect, 1f, 96f);
                if (hint.text != null)
                {
                    hint.text.text = UiText.BasicHint;
                }
            }

            b.End();
        }

        private static void OnWoundChanged(int index)
        {
            woundIndex = index;
        }

        private static void OnWoundStableChanged(bool value)
        {
            woundStable = value;
        }

        private static void OnRefreshBasic()
        {
            // 存档晚于面板载入时，阵营列表要重新构建整页才拿得到
            if (facInputs.Count == 0 && BasicStats.Factions().Count > 0)
            {
                RebuildWindow();
                SetBasicStatus("已重新读取当前数值");
                return;
            }
            RefreshBasicValues();
            SetBasicStatus("已重新读取当前数值");
        }

        private static void RefreshBasicValues()
        {
            if (bCash != null) bCash.SetString(BasicStats.CashText());
            if (bRent != null) bRent.SetString(BasicStats.RentText());
            if (bRentDays != null) bRentDays.SetString(BasicStats.RentDaysText());
            if (bLoan != null) bLoan.SetString(BasicStats.LoanText());
            if (bLoanDays != null) bLoanDays.SetString(BasicStats.LoanDaysText());
            if (bAttr != null) bAttr.SetString(BasicStats.AttractText());
            if (bProjector != null) bProjector.SetString(BasicStats.ProjectorText());
            if (bRetail != null) bRetail.SetString(BasicStats.RetailMarkupText());
            if (bContraLow != null) bContraLow.SetString(BasicStats.ContrabandLowText());
            if (bContraMid != null) bContraMid.SetString(BasicStats.ContrabandMidText());
            if (bContraHigh != null) bContraHigh.SetString(BasicStats.ContrabandHighText());
            if (bContraCrit != null) bContraCrit.SetString(BasicStats.ContrabandCriticalText());
            woundIndex = BasicStats.WoundOptionIndex();
            woundStable = BasicStats.WoundStable();
            if (bFavor != null) bFavor.SetString(BasicStats.FavorText());
            if (bCommissaryCred != null) bCommissaryCred.SetString(BasicStats.CommissaryCreditText());
            if (bWound != null) bWound.SetIndex(woundIndex);
            if (bWoundStable != null) bWoundStable.SetBool(woundStable);
            foreach (KeyValuePair<string, CustomUIElement> pair in facInputs)
            {
                if (pair.Value != null) pair.Value.SetString(BasicStats.RepText(pair.Key));
            }
            foreach (KeyValuePair<string, CustomUIElement> pair in facTiers)
            {
                if (pair.Value != null) pair.Value.SetText(BasicStats.RepTierText(pair.Key));
            }
        }

        private static void SetBasicStatus(string text)
        {
            SetStatus(text);
        }

        private static void ApplyBasic(string error, string okText)
        {
            SetBasicStatus(error ?? okText);
        }

        private static void OnWriteCash()
        {
            string err = BasicStats.SetCash(TextOf(bCash));
            if (err == null && bCash != null) bCash.SetString(BasicStats.CashText());
            ApplyBasic(err, "现金已写入");
        }

        private static void OnWriteRent()
        {
            string err = BasicStats.SetRent(TextOf(bRent));
            if (err == null) BasicStats.SetRentDays(TextOf(bRentDays));
            if (err == null && bRent != null) bRent.SetString(BasicStats.RentText());
            if (err == null && bRentDays != null) bRentDays.SetString(BasicStats.RentDaysText());
            ApplyBasic(err, "房租与剩余天数已写入");
        }

        private static void OnWriteLoan()
        {
            string err = BasicStats.SetLoan(TextOf(bLoan));
            if (err == null) BasicStats.SetLoanDays(TextOf(bLoanDays));
            if (err == null && bLoan != null) bLoan.SetString(BasicStats.LoanText());
            if (err == null && bLoanDays != null) bLoanDays.SetString(BasicStats.LoanDaysText());
            ApplyBasic(err, "贷款与剩余天数已写入");
        }

        private static void OnWriteAttr()
        {
            string err = BasicStats.SetAttract(TextOf(bAttr));
            if (err == null && bAttr != null) bAttr.SetString(BasicStats.AttractText());
            ApplyBasic(err, "吸引力已写入");
        }

        private static void OnWriteProjector()
        {
            string err = BasicStats.SetProjector(TextOf(bProjector));
            if (err == null && bProjector != null) bProjector.SetString(BasicStats.ProjectorText());
            ApplyBasic(err, "投影加成已写入");
        }

        private static void OnWriteFavor()
        {
            string err = BasicStats.SetFavor(TextOf(bFavor));
            if (err == null && bFavor != null) bFavor.SetString(BasicStats.FavorText());
            ApplyBasic(err, "恩惠已写入");
        }

        private static void OnWriteCommissaryCredit()
        {
            string err = BasicStats.SetCommissaryCredit(TextOf(bCommissaryCred));
            if (err == null && bCommissaryCred != null) bCommissaryCred.SetString(BasicStats.CommissaryCreditText());
            ApplyBasic(err, "配给券余额已写入");
        }

        private static void OnWriteRetail()
        {
            string err = BasicStats.SetRetailMarkup(TextOf(bRetail));
            if (err == null && bRetail != null) bRetail.SetString(BasicStats.RetailMarkupText());
            ApplyBasic(err, "零售加价已写入");
        }

        private static void OnWriteContraLow()
        {
            string err = BasicStats.SetContrabandLow(TextOf(bContraLow));
            if (err == null && bContraLow != null) bContraLow.SetString(BasicStats.ContrabandLowText());
            ApplyBasic(err, "违禁加价·低 已写入");
        }

        private static void OnWriteContraMid()
        {
            string err = BasicStats.SetContrabandMid(TextOf(bContraMid));
            if (err == null && bContraMid != null) bContraMid.SetString(BasicStats.ContrabandMidText());
            ApplyBasic(err, "违禁加价·中 已写入");
        }

        private static void OnWriteContraHigh()
        {
            string err = BasicStats.SetContrabandHigh(TextOf(bContraHigh));
            if (err == null && bContraHigh != null) bContraHigh.SetString(BasicStats.ContrabandHighText());
            ApplyBasic(err, "违禁加价·高 已写入");
        }

        private static void OnWriteContraCrit()
        {
            string err = BasicStats.SetContrabandCritical(TextOf(bContraCrit));
            if (err == null && bContraCrit != null) bContraCrit.SetString(BasicStats.ContrabandCriticalText());
            ApplyBasic(err, "违禁加价·极高 已写入");
        }

        private static void OnWriteRep(string factionId)
        {
            CustomUIElement box;
            if (!facInputs.TryGetValue(factionId, out box) || box == null)
            {
                return;
            }
            string error = BasicStats.SetRep(factionId, box.GetString());
            if (error != null)
            {
                SetBasicStatus(error);
                return;
            }
            box.SetString(BasicStats.RepText(factionId));
            CustomUIElement tier;
            if (facTiers.TryGetValue(factionId, out tier) && tier != null)
            {
                tier.SetText(BasicStats.RepTierText(factionId));
            }
            SetBasicStatus(BasicStats.RepNameOf(factionId) + " 已写入：" + BasicStats.RepText(factionId));
        }

        private static void OnWriteWound()
        {
            string err = BasicStats.ApplyWound(woundIndex, woundStable);
            ApplyBasic(err, "伤势已写入：" + BasicStats.WoundOptions[Mathf.Clamp(woundIndex, 0, BasicStats.WoundOptions.Length - 1)]);
        }

        private static void BuildCreatePane(CustomUIBuilder b)
        {
            b.BeginRow(Gap, TextAnchor.UpperLeft);
            UiKit.FlexHeight(b.CurrentContainer, 1f, 520f);
            UiKit.FlexWidth(b.CurrentContainer, 1f, 600f);

            // ---- 左：目录
            b.BeginPanel(LeftW, -1f, TextAnchor.UpperLeft, 12, 6, null);
            Card(b, UiSkin.CardTint);

            BeginFormRow(b, LeftInner);
            UiKit.Height(b.CurrentContainer, 32f);

            b.AddInput(UiText.SearchHint, createFilter, null, null, OnSearchTyping);
            CustomUIElement search = b.Last;
            if (search != null)
            {
                UiKit.FlexWidth(search, 1f, 200f);
                UiKit.Height(search, 32f);
            }

            List<string> sectionOptions = BuildSectionOptions(createFilter, null, sectionMap);
            int sectionPos = sectionMap.IndexOf(createSectionIndex);
            b.AddDropdown(sectionOptions, sectionPos < 0 ? 0 : sectionPos, OnSectionChanged, null);
            createSectionDropdown = b.Last;
            if (createSectionDropdown != null)
            {
                UiKit.Width(createSectionDropdown, 158f);
                UiKit.Height(createSectionDropdown, 32f);
            }

            List<string> groupOptions = BuildGroupOptions(createFilter, createSectionIndex, null, groupMap);
            int groupPos = groupMap.IndexOf(createGroupIndex);
            b.AddDropdown(groupOptions, groupPos < 0 ? 0 : groupPos, OnGroupChanged, null);
            createGroupDropdown = b.Last;
            if (createGroupDropdown != null)
            {
                UiKit.Width(createGroupDropdown, 158f);
                UiKit.Height(createGroupDropdown, 32f);
            }

            b.AddButton(UiText.RefreshCatalog, OnRefreshCatalog, null);
            CustomUIElement refresh = b.Last;
            if (refresh != null)
            {
                UiKit.Width(refresh, 84f);
                UiKit.Height(refresh, 30f);
                UiSkin.Ghost(refresh);
            }

            b.End();

            b.AddLabel(UiText.ListHint);
            CustomUIElement listHint = b.Last;
            if (listHint != null)
            {
                UiKit.Width(listHint, LeftInner);
                UiKit.Height(listHint, 16f);
                if (listHint.text != null)
                {
                    listHint.text.text = UiText.ListHint;
                    listHint.text.color = UiSkin.Mute;
                    listHint.text.fontSize = 11.5f;
                    listHint.text.enableAutoSizing = false;
                    listHint.text.enableWordWrapping = false;
                    listHint.text.alignment = TextAlignmentOptions.Left;
                    listHint.text.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            b.BeginScrollFlexible(240f);
            UiKit.FlexWidth(b.Last, 1f, LeftInner);
            createGridHost = b.CurrentContainer;
            b.End();

            b.End();

            // ---- 右：草稿
            b.BeginPanel(RightW, -1f, TextAnchor.UpperLeft, 12, 6, null);
            Card(b, UiSkin.CardTintAlt);

            b.AddHeader(UiText.DraftNone);
            draftTitle = b.Last;
            if (draftTitle != null)
            {
                UiKit.Width(draftTitle, RightInner);
                UiKit.Height(draftTitle, 24f);
            }

            BeginFormRow(b, RightInner);
            UiKit.Caption(b, UiText.CountLabel, 40f, 28f);
            draftCount = InputBox(b, "1", 64f);
            b.AddSpace(12f);
            UiKit.Caption(b, UiText.PriceLabel, 64f, 28f);
            draftPrice = InputBox(b, "", 128f);
            b.AddFlexibleSpace();
            b.End();

            BeginFormRow(b, RightInner);
            UiKit.Caption(b, UiText.WhereLabel, 64f, 28f);
            b.AddDropdown(UiText.SpawnTargets, whereIndex, OnWhereChanged, null);
            draftWhere = b.Last;
            if (draftWhere != null)
            {
                UiKit.Width(draftWhere, 150f);
                UiKit.Height(draftWhere, 30f);
            }
            b.AddFlexibleSpace();
            b.AddButton(UiText.SpawnButton, OnSpawn, null);
            CustomUIElement spawn = b.Last;
            if (spawn != null)
            {
                UiKit.Width(spawn, 104f);
                UiKit.Height(spawn, 30f);
                UiSkin.Primary(spawn);
            }
            b.End();

            b.AddSpace(4f);
            UiKit.Rule(b, RightInner);
            b.AddSpace(4f);

            b.BeginScrollFlexible(200f);
            UiKit.FlexWidth(b.Last, 1f, RightInner);
            createSpecHost = b.CurrentContainer;
            b.End();

            b.End();

            b.End();
        }

        private static void BuildEditPane(CustomUIBuilder b)
        {
            b.BeginRow(Gap, TextAnchor.UpperLeft);
            UiKit.FlexHeight(b.CurrentContainer, 1f, 520f);
            UiKit.FlexWidth(b.CurrentContainer, 1f, 600f);

            // ---- 左：台面
            b.BeginPanel(LeftW, -1f, TextAnchor.UpperLeft, 12, 6, null);
            Card(b, UiSkin.CardTint);

            BeginFormRow(b, LeftInner);
            UiKit.Height(b.CurrentContainer, 30f);

            b.AddButton(UiText.RefreshStore, OnRefreshEdit, null);
            CustomUIElement reload = b.Last;
            if (reload != null)
            {
                UiKit.Width(reload, 96f);
                UiKit.Height(reload, 30f);
                UiSkin.Ghost(reload);
            }

            b.AddButton(UiText.PickButton, OnPick, null);
            CustomUIElement pick = b.Last;
            if (pick != null)
            {
                UiKit.Width(pick, 96f);
                UiKit.Height(pick, 30f);
                UiSkin.Ghost(pick);
            }

            b.AddToggle(UiText.IncludeStore, includeStore, OnIncludeStoreChanged, null);
            editIncludeStore = b.Last;
            if (editIncludeStore != null)
            {
                UiKit.FlexWidth(editIncludeStore, 1f, 180f);
                UiKit.Height(editIncludeStore, 30f);
            }
            b.End();

            BeginFormRow(b, LeftInner);
            UiKit.Height(b.CurrentContainer, 32f);

            b.AddInput(UiText.EditSearchHint, editFilter, null, null, OnEditSearchTyping);
            editSearch = b.Last;
            if (editSearch != null)
            {
                UiKit.FlexWidth(editSearch, 1f, 200f);
                UiKit.Height(editSearch, 32f);
            }

            List<string> editSectionOptions = BuildSectionOptions(editFilter, EditIds(), editSectionMap);
            int editSectionPos = editSectionMap.IndexOf(editSectionIndex);
            b.AddDropdown(editSectionOptions, editSectionPos < 0 ? 0 : editSectionPos, OnEditSectionChanged, null);
            editSectionDropdown = b.Last;
            if (editSectionDropdown != null)
            {
                UiKit.Width(editSectionDropdown, 158f);
                UiKit.Height(editSectionDropdown, 32f);
            }

            List<string> editGroupOptions = BuildGroupOptions(editFilter, editSectionIndex, EditIds(), editGroupMap);
            int editGroupPos = editGroupMap.IndexOf(editGroupIndex);
            b.AddDropdown(editGroupOptions, editGroupPos < 0 ? 0 : editGroupPos, OnEditGroupChanged, null);
            editGroupDropdown = b.Last;
            if (editGroupDropdown != null)
            {
                UiKit.Width(editGroupDropdown, 158f);
                UiKit.Height(editGroupDropdown, 32f);
            }
            b.End();

            b.AddLabel("");
            editInfo = b.Last;
            if (editInfo != null)
            {
                UiKit.Width(editInfo, LeftInner);
                UiKit.Height(editInfo, 16f);
                if (editInfo.text != null)
                {
                    editInfo.text.color = UiSkin.Cyan;
                    editInfo.text.fontSize = 11.5f;
                    editInfo.text.enableAutoSizing = false;
                    editInfo.text.enableWordWrapping = false;
                    editInfo.text.alignment = TextAlignmentOptions.Left;
                    editInfo.text.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            b.BeginScrollFlexible(200f);
            UiKit.FlexWidth(b.Last, 1f, LeftInner);
            editGridHost = b.CurrentContainer;
            b.End();

            b.End();

            // ---- 右：编辑表单
            b.BeginPanel(RightW, -1f, TextAnchor.UpperLeft, 12, 6, null);
            Card(b, UiSkin.CardTintAlt);

            b.BeginColumn(4f, TextAnchor.UpperLeft);
            editFormHost = b.CurrentContainer;
            UiKit.FlexWidth(editFormHost, 1f, RightInner);
            b.End();

            b.BeginScrollFlexible(200f);
            editSpecScroll = b.Last;
            if (editSpecScroll != null)
            {
                UiKit.FlexWidth(editSpecScroll, 1f, 240f);
            }
            editSpecHost = b.CurrentContainer;
            b.End();

            b.End();

            b.End();
        }

        // ================= 生成页：目录 =================

        private static void OnSearchTyping(string text)
        {
            createFilter = text ?? "";
            RebuildFilterOptions();
            RefreshCreateList();
        }

        private static void OnSectionChanged(int index)
        {
            if (sectionLock)
            {
                return;
            }
            createSectionIndex = index >= 0 && index < sectionMap.Count ? sectionMap[index] : 0;
            // 换了大类，小类回到「全部小类」
            createGroupIndex = 0;
            RebuildGroupOptions();
            RefreshCreateList();
        }

        private static void OnGroupChanged(int index)
        {
            if (groupLock)
            {
                return;
            }
            createGroupIndex = index >= 0 && index < groupMap.Count ? groupMap[index] : 0;
            RefreshCreateList();
        }

        private static void OnRefreshCatalog()
        {
            Catalog.Ensure(true);
            Catalog.ClearCaches();
            RebuildFilterOptions();
            RefreshCreateList();
            SetStatus("目录已刷新，共 " + Catalog.Count + " 件物品");
        }

        private static void RebuildFilterOptions()
        {
            RebuildSectionOptions();
            RebuildGroupOptions();
        }

        /// <summary>大类下拉：全部分类 + 17 个 wiki 大类（没货的不出），括号里是当前搜索命中的件数。subset 非空时只数里面的 id（编辑页只数台面上那几件）。</summary>
        private static List<string> BuildSectionOptions(string filter, IList<string> subset, List<int> map)
        {
            List<string> labels = new List<string>();
            map.Clear();

            int[] counts;
            try
            {
                counts = Catalog.SectionCounts(subset, filter);
            }
            catch (Exception e)
            {
                Log.Warn("统计大类数量失败: " + e.Message);
                counts = new int[] { subset == null ? Catalog.Count : subset.Count };
            }

            labels.Add(UiText.SectionAll + " (" + counts[0] + ")");
            map.Add(0);

            List<string> all = Catalog.SectionLabels;
            for (int i = 1; i < counts.Length && i < all.Count; i++)
            {
                if (counts[i] <= 0)
                {
                    continue;
                }
                labels.Add(all[i] + " (" + counts[i] + ")");
                map.Add(i);
            }
            return labels;
        }

        /// <summary>小类下拉：只列当前大类下的小类；同一件物品可能同时出现在几个小类里。subset 非空时只数里面的 id。</summary>
        private static List<string> BuildGroupOptions(string filter, int sectionIndex, IList<string> subset, List<int> map)
        {
            List<string> labels = new List<string>();
            map.Clear();

            int[] counts;
            int total;
            try
            {
                counts = Catalog.GroupCounts(subset, filter);
                int[] sectionCounts = Catalog.SectionCounts(subset, filter);
                total = sectionIndex > 0 && sectionIndex < sectionCounts.Length
                    ? sectionCounts[sectionIndex]
                    : counts[0];
            }
            catch (Exception e)
            {
                Log.Warn("统计分类数量失败: " + e.Message);
                counts = new int[] { subset == null ? Catalog.Count : subset.Count };
                total = counts[0];
            }

            labels.Add(UiText.GroupAll + " (" + total + ")");
            map.Add(0);

            List<string> all = Catalog.GroupLabels;
            for (int i = 1; i < counts.Length && i < all.Count; i++)
            {
                if (counts[i] <= 0)
                {
                    continue;
                }
                if (sectionIndex > 0 && Catalog.SectionOfGroup(i) != sectionIndex)
                {
                    continue;
                }
                labels.Add(all[i] + " (" + counts[i] + ")");
                map.Add(i);
            }
            return labels;
        }

        /// <summary>把新选项塞进大类下拉；当前选中项被筛掉了就回到「全部大类」。</summary>
        private static void ReloadSectionOptions(CustomUIElement dropdown, List<int> map, ref int index, ref bool lockFlag, string filter, IList<string> subset)
        {
            List<string> labels = BuildSectionOptions(filter, subset, map);

            int pos = map.IndexOf(index);
            if (pos < 0)
            {
                index = 0;
                pos = 0;
            }

            if (dropdown == null || !dropdown.IsAlive)
            {
                return;
            }

            bool previous = lockFlag;
            lockFlag = true;
            try
            {
                dropdown.SetOptions(labels);
                dropdown.SetIndex(pos);
            }
            catch (Exception e)
            {
                Log.Warn("刷新大类下拉失败: " + e.Message);
            }
            finally
            {
                lockFlag = previous;
            }
        }

        /// <summary>搜索词 / 大类变了以后重建小类下拉：只剩有货的小类，标题后面跟着匹配数量。</summary>
        private static void ReloadGroupOptions(CustomUIElement dropdown, List<int> map, ref int index, ref bool lockFlag, string filter, int sectionIndex, IList<string> subset)
        {
            List<string> labels = BuildGroupOptions(filter, sectionIndex, subset, map);

            int pos = map.IndexOf(index);
            if (pos < 0)
            {
                index = 0;
                pos = 0;
            }

            if (dropdown == null || !dropdown.IsAlive)
            {
                return;
            }

            bool previous = lockFlag;
            lockFlag = true;
            try
            {
                dropdown.SetOptions(labels);
                dropdown.SetIndex(pos);
            }
            catch (Exception e)
            {
                Log.Warn("刷新分类下拉失败: " + e.Message);
            }
            finally
            {
                lockFlag = previous;
            }
        }

        private static void RebuildSectionOptions()
        {
            ReloadSectionOptions(createSectionDropdown, sectionMap, ref createSectionIndex, ref sectionLock, createFilter, null);
        }

        private static void RebuildGroupOptions()
        {
            ReloadGroupOptions(createGroupDropdown, groupMap, ref createGroupIndex, ref groupLock, createFilter, createSectionIndex, null);
        }

        /// <summary>编辑页：台面物品变了（换页 / 勾选含店内库存 / 刷新）时，两个下拉的件数要跟着重算。</summary>
        private static void RebuildEditFilterOptions()
        {
            List<string> ids = EditIds();
            ReloadSectionOptions(editSectionDropdown, editSectionMap, ref editSectionIndex, ref editSectionLock, editFilter, ids);
            ReloadGroupOptions(editGroupDropdown, editGroupMap, ref editGroupIndex, ref editGroupLock, editFilter, editSectionIndex, ids);
        }

        private static void OnWhereChanged(int index)
        {
            whereIndex = index < 0 ? 0 : index;
        }

        private static void RefreshCreateList()
        {
            if (createGridHost == null || !HostAlive(createGridHost) || window == null)
            {
                return;
            }

            StopIcons();
            CustomUIBuilder.ClearContainer(createGridHost);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, window, createGridHost);

            List<string> ids = Catalog.Query(createFilter, createSectionIndex, createGroupIndex);
            int shown = ids.Count > MaxCells ? MaxCells : ids.Count;
            List<CellRef> cells = new List<CellRef>(shown);

            if (shown == 0)
            {
                b.AddLabel(UiText.EmptyList);
            }
            else
            {
                b.BeginGrid(GridColumns, CellW, CellH, 4f);
                for (int i = 0; i < shown; i++)
                {
                    string id = ids[i];
                    AddTile(b, id, "", selectedId == id, delegate { SelectDraft(id); }, cells);
                }
                b.End();
            }

            CustomUIBuilder.RebuildLayout(createGridHost);
            StartIcons(cells);

            if (ids.Count > shown)
            {
                SetStatus("匹配 " + ids.Count + " 件，只显示前 " + shown + " 件。缩小关键词再找。");
            }
        }

        private static void AddTile(CustomUIBuilder b, string tooltip, string label, bool selected, Action onClick, List<CellRef> cells)
        {
            b.BeginColumn(2f, TextAnchor.UpperCenter);
            RectTransform cell = b.CurrentContainer;
            UiKit.Width(cell, CellW);
            UiKit.Height(cell, CellH);
            UiKit.Tune(cell, 6, 6, 10, 6, 2f);

            // 整格底衬：九宫格圆角，悬停会亮一档；点击和 tooltip 都挂在它身上，
            // 图标和文字一律不吃射线，免得挡在最上面把点击吞掉。
            CellClickHandler handler = AddCellChrome(cell, selected, tooltip);
            if (handler != null)
            {
                handler.onLeftClick = onClick;
            }

            UiKit.IconWell(cell, 66f, new Color(0f, 0f, 0f, 0.3f));

            b.AddImage((Sprite)null, 56f, 56f, null);
            CustomUIElement image = b.Last;
            if (image != null)
            {
                if (image.image != null)
                {
                    image.image.raycastTarget = false;
                    image.image.preserveAspect = true;
                }
                image.SetImageColor(Color.white);
            }

            b.AddLabel(label ?? "");
            CustomUIElement text = b.Last;
            if (text != null)
            {
                StyleCellLabel(text, selected);
                if (text.text != null)
                {
                    text.text.raycastTarget = false;
                    text.text.text = label ?? "";
                }
            }

            b.End();

            if (cells != null)
            {
                cells.Add(new CellRef
                {
                    Id = tooltip,
                    Image = image,
                    Label = text,
                    Handler = handler
                });
            }
        }

        /// <summary>格子底衬并挂上点击/悬停处理，返回挂好组件的 handler。</summary>
        private static CellClickHandler AddCellChrome(RectTransform cell, bool selected, string tooltip)
        {
            if (cell == null)
            {
                return null;
            }

            GameObject go = new GameObject("ds_cell_plate", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(cell, false);
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                return null;
            }
            img.sprite = UiSkin.Plate(9, 0.72f, 1);
            img.type = Image.Type.Sliced;
            img.color = selected ? UiSkin.TileTintOn : UiSkin.TileTint;
            img.raycastTarget = true;

            CellClickHandler handler = go.AddComponent<CellClickHandler>();
            handler.plate = img;
            handler.tintNormal = selected ? UiSkin.TileTintOn : UiSkin.TileTint;
            handler.tintHover = selected ? UiSkin.TileTintOnHover : UiSkin.TileTintHover;

            if (!string.IsNullOrEmpty(tooltip))
            {
                CustomUITooltipTrigger trigger = go.AddComponent<CustomUITooltipTrigger>();
                trigger.tooltipText = tooltip;
            }
            return handler;
        }

        /// <summary>文字自动适配：长标签自动换行并在 9~原字号之间缩小，避免撑破右栏。</summary>
        private static void FitTexts(RectTransform host)
        {
            if (host == null)
            {
                return;
            }
            // 规格面板、编辑表单是运行时才重建的，这里顺手把游戏自带的 header / button 再统一一次样式
            UiSkin.Polish(host);
            try
            {
                TextMeshProUGUI[] texts = host.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TextMeshProUGUI t = texts[i];
                    if (t == null)
                    {
                        continue;
                    }
                    if (t.GetComponentInParent<TMP_InputField>() != null || t.GetComponentInParent<TMP_Dropdown>() != null)
                    {
                        continue;
                    }
                    float max = t.fontSize > 0f ? t.fontSize : 15f;
                    t.enableWordWrapping = true;
                    t.enableAutoSizing = true;
                    t.fontSizeMax = max;
                    t.fontSizeMin = Mathf.Min(9f, max);
                    t.overflowMode = TextOverflowModes.Truncate;
                }
            }
            catch (Exception e)
            {
                Log.Warn("字体自适应失败: " + e.Message);
            }
        }

        private static void StyleCellLabel(CustomUIElement el, bool selected)
        {
            if (el == null)
            {
                return;
            }
            if (el.text != null)
            {
                el.text.alignment = TextAlignmentOptions.Center;
                el.text.enableWordWrapping = true;
                el.text.enableAutoSizing = true;
                el.text.fontSizeMax = 12f;
                el.text.fontSizeMin = 8f;
                el.text.lineSpacing = 0f;
                el.text.color = selected ? UiSkin.Brass : UiSkin.Bone;
                el.text.overflowMode = TextOverflowModes.Ellipsis;
            }
            el.SetPreferredSize(CellW - 10f, 30f);
        }

        private static void StartIcons(List<CellRef> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return;
            }
            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null)
            {
                return;
            }
            try
            {
                iconRoutine = manager.StartCoroutine(FillCells(cells));
            }
            catch (Exception e)
            {
                Log.Warn("启动图标填充失败: " + e.Message);
            }
        }

        private static void StopIcons()
        {
            if (iconRoutine == null)
            {
                return;
            }
            try
            {
                CustomUIManager manager = CustomUIManager.Instance;
                if (manager != null)
                {
                    manager.StopCoroutine(iconRoutine);
                }
            }
            catch
            {
                // 忽略
            }
            iconRoutine = null;
        }

        private static IEnumerator FillCells(List<CellRef> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (window == null || !window.IsAlive)
                {
                    iconRoutine = null;
                    yield break;
                }

                CellRef cell = cells[i];
                if (cell == null || cell.Image == null || !cell.Image.IsAlive)
                {
                    continue;
                }

                if (cell.Label != null && cell.Label.IsAlive)
                {
                    cell.Label.SetText(Shorten(Catalog.NameOf(cell.Id), 18));
                }

                // 图标探针顺带抓到游戏原文描述，挂到整格底衬上（图标/文字不吃射线，否则会吞掉点击）
                string tip = TileTooltip(cell.Id);
                if (cell.Handler != null)
                {
                    CustomUITooltipTrigger trigger = cell.Handler.GetComponent<CustomUITooltipTrigger>();
                    if (trigger != null)
                    {
                        trigger.tooltipText = tip;
                    }
                }
                if (cell.Label != null && cell.Label.IsAlive && cell.Label.text != null)
                {
                    cell.Label.text.raycastTarget = false;
                }
                if (cell.Image != null && cell.Image.IsAlive && cell.Image.image != null)
                {
                    cell.Image.image.raycastTarget = false;
                }

                Sprite icon = Catalog.IconOf(cell.Id);
                if (icon != null)
                {
                    cell.Image.SetSprite(icon);
                }

                if (i % 10 == 9)
                {
                    yield return null;
                }
            }
            iconRoutine = null;
        }

        /// <summary>格子提示：第一行是英文 id，空一行接游戏原本的描述（有风味文本再加一行）。</summary>
        private static string TileTooltip(string id)
        {
            string tip = id ?? "";
            string desc = Catalog.DescOf(id);
            if (desc.Length > 0)
            {
                tip = tip + "\n\n" + desc;
            }
            return tip;
        }

        // ================= 生成页：草稿 =================

        private static void SelectDraft(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }
            if (draft != null && selectedId == id)
            {
                return;
            }

            DestroyDraft();
            selectedId = id;
            draft = Catalog.CreateDraft(id);

            if (draftTitle != null && draftTitle.IsAlive)
            {
                draftTitle.SetText(draft == null
                    ? UiText.DraftNone
                    : Catalog.NameOf(id) + "   [" + id + "]");
            }

            RefreshCreateList();
            RebuildCreateSpec();

            if (draft == null)
            {
                SetStatus("造不出 " + id + " —— 这条 id 可能只是目录占位");
            }
            else
            {
                SetStatus("已选中 " + Catalog.NameOf(id) + "，右边填参数后点生成");
            }
        }

        private static void DestroyDraft()
        {
            GameItem old = draft;
            draft = null;
            if (old == null)
            {
                return;
            }
            try
            {
                old.Destroy();
            }
            catch (Exception e)
            {
                Log.Warn("清理草稿失败: " + e.Message);
            }
        }

        private static void RebuildCreateSpec()
        {
            if (createSpecHost == null || !HostAlive(createSpecHost) || window == null)
            {
                return;
            }

            CustomUIBuilder.ClearContainer(createSpecHost);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, window, createSpecHost);

            if (draft == null)
            {
                b.AddLabel(UiText.DraftNone);
            }
            else
            {
                b.AddLabel(UiText.DraftHint);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, 18f);
                BuildSpecPanel(b, draft, RebuildCreateSpec);
            }

            CustomUIBuilder.RebuildLayout(createSpecHost);
            FitTexts(createSpecHost);
        }

        private static void OnSpawn()
        {
            if (draft == null)
            {
                SetStatus("先在左边点一件物品");
                return;
            }

            int count = 1;
            int parsed;
            if (TryParseInt(TextOf(draftCount), out parsed) && parsed >= 1)
            {
                count = parsed > 9999 ? 9999 : parsed;
            }

            long price = 0;
            long parsedLong;
            if (TryParseLong(TextOf(draftPrice), out parsedLong) && parsedLong > 0)
            {
                price = parsedLong;
            }

            string id = draft.identifier;
            string name = Catalog.NameOf(id);

            try
            {
                if (count > 1)
                {
                    draft.SetStack(count);
                }
                if (price > 0)
                {
                    draft.SetValue(price);
                }
            }
            catch (Exception e)
            {
                SetStatus("写数量/价格失败: " + e.Message);
                return;
            }

            string error = Catalog.Place(draft, whereIndex);
            if (!string.IsNullOrEmpty(error))
            {
                SetStatus(error);
                return;
            }

            string where = whereIndex >= 0 && whereIndex < UiText.SpawnTargets.Length ? UiText.SpawnTargets[whereIndex] : "店里";
            SetStatus("已生成 " + name + " ×" + count + " → " + where);

            // 刚落到台面上的东西，编辑页那边也该立刻看见；筛选的件数跟着重算
            try
            {
                RebuildEditFilterOptions();
                RefreshEditList();
            }
            catch (Exception e)
            {
                Log.Warn("刷新编辑页列表失败: " + e.Message);
            }

            // 造下一件同样的东西，规格照抄，方便连续生成
            GameItem previous = draft;
            draft = Catalog.CreateDraft(id);
            if (draft != null)
            {
                CopySpec(previous, draft);
                try
                {
                    if (count > 1)
                    {
                        draft.SetStack(count);
                    }
                }
                catch
                {
                    // 数量写不进去就算了
                }
            }
            RebuildCreateSpec();
        }

        private static void CopySpec(GameItem from, GameItem to)
        {
            if (from == null || to == null || from == to)
            {
                return;
            }
            try
            {
                SpecInfo info = Spec.Describe(from);
                for (int i = 0; i < info.Slots.Count; i++)
                {
                    LiquidSlot slot = info.Slots[i];
                    Spec.SetLiquid(to, slot.Id, slot.Parts);
                }
                if (info.IsIngot && !string.IsNullOrEmpty(info.IngotPurity))
                {
                    Spec.SetIngotPurity(to, info.IngotPurity);
                }
            }
            catch (Exception e)
            {
                Log.Warn("复制规格失败: " + e.Message);
            }

            string geneError = Genes.Copy(from, to);
            if (!string.IsNullOrEmpty(geneError))
            {
                Log.Warn(geneError);
            }
        }

        // ================= 规格面板（两个页签共用） =================

        private static void BuildSpecPanel(CustomUIBuilder b, GameItem item, Action refresh)
        {
            SpecInfo info = Spec.Describe(item);

            b.AddHeader(UiText.SpecTitle);

            if (info.IsContainer)
            {
                b.AddLabel(UiText.SpecCapacity + " " + Spec.FormatMl(info.CapacityParts)
                    + "   " + UiText.SpecVolume + " " + Spec.FormatMl(info.TotalParts));
                b.Last.SetPreferredSize(InnerW, 18f);
            }
            else if (info.EditsLiquid)
            {
                b.AddLabel(UiText.SpecVolume + " " + Spec.FormatMl(info.TotalParts));
                b.Last.SetPreferredSize(InnerW, 18f);
            }

            if (!info.HasSpec)
            {
                b.AddLabel(UiText.SpecNone);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, 32f);
                BuildGenePanel(b, item, refresh);
                BuildStampPanel(b, item, refresh);
                BuildWearPanel(b, item, refresh);
                BuildUpgradePanel(b, item, refresh);
                BuildTagPanel(b, item, refresh);
                return;
            }

            if (info.EditsLiquid)
            {
                b.AddSeparator(null, 2);
                b.AddLabel(UiText.SpecLiquidHint);
                b.Last.SetPreferredSize(InnerW, 18f);

                if (info.Slots.Count == 0)
                {
                    b.AddLabel(UiText.SpecNoLiquid);
                    b.Last.SetTextColor(DimColor);
                    b.Last.SetPreferredSize(InnerW, 18f);
                }

                for (int i = 0; i < info.Slots.Count; i++)
                {
                    LiquidSlot slot = info.Slots[i];
                    string liquidId = slot.Id;
                    b.BeginRow(4f, TextAnchor.MiddleLeft);
                    b.AddLabel(slot.Display);
                    b.Last.SetPreferredSize(150f, 22f);
                    b.AddLabel(Spec.FormatMl(slot.Parts));
                    b.Last.SetPreferredSize(90f, 22f);
                    b.AddButton(UiText.SpecClear, delegate
                    {
                        ApplySpec(item, Spec.SetLiquid(item, liquidId, 0), refresh);
                    }, null);
                    b.Last.SetPreferredSize(66f, 26f);
                    b.End();
                }

                EnsureLiquidOptions();
                int pendingIndex = IndexOfLiquid(pendingLiquidId);
                if (pendingIndex < 0)
                {
                    pendingIndex = 0;
                    if (liquidIds.Count > 0)
                    {
                        pendingLiquidId = liquidIds[0];
                    }
                }

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddDropdown(liquidLabels, pendingIndex, OnPickLiquid, null);
                b.Last.SetPreferredSize(170f, 28f);
                b.AddInput(UiText.SpecMlPlaceholder, liquidAmountText, null, null, delegate(string v)
                {
                    liquidAmountText = v ?? "";
                });
                b.Last.SetPreferredSize(80f, 28f);
                b.AddButton(UiText.SpecSet, delegate
                {
                    double ml;
                    if (!TryParseDouble(liquidAmountText, out ml) || ml < 0)
                    {
                        SetStatus("毫升要填个非负数字");
                        return;
                    }
                    int parts = (int)Math.Round(ml * 1000.0);
                    ApplySpec(item, Spec.SetLiquid(item, pendingLiquidId, parts), refresh);
                }, null);
                b.Last.SetPreferredSize(70f, 28f);
                b.End();

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddButton(UiText.SpecFillWater, delegate
                {
                    ApplySpec(item, Spec.FillWater(item), refresh);
                }, null);
                b.Last.SetPreferredSize(140f, 26f);
                b.AddButton(UiText.SpecClearAll, delegate
                {
                    ApplySpec(item, Spec.ClearLiquids(item), refresh);
                }, null);
                b.Last.SetPreferredSize(140f, 26f);
                b.End();
            }

            if (info.IsContainer)
            {
                b.AddSeparator(null, 2);
                b.AddLabel(UiText.SpecWaterPurity + " · 现在 " +
                    (info.TotalParts > 0 ? Spec.FormatPct(info.WaterPurity) : "空"));
                b.Last.SetPreferredSize(InnerW, 18f);

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddInput(UiText.SpecPurityPlaceholder, waterPurityText, null, null, delegate(string v)
                {
                    waterPurityText = v ?? "";
                });
                b.Last.SetPreferredSize(90f, 28f);
                b.AddButton(UiText.SpecApplyWaterPurity, delegate
                {
                    double percent;
                    if (!TryParsePercent(waterPurityText, out percent))
                    {
                        SetStatus("纯度要填 0-100 的数字");
                        return;
                    }
                    ApplySpec(item, Spec.SetWaterPurity(item, percent), refresh);
                }, null);
                b.Last.SetPreferredSize(120f, 28f);
                b.End();
            }
            else if (info.IsChemProduct)
            {
                b.AddLabel(UiText.SpecNotContainer);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, 18f);
            }

            if (info.IsChemProduct)
            {
                b.AddSeparator(null, 2);
                b.AddLabel(UiText.SpecActive + ": " + info.ActiveIngredientDisplay
                    + "   " + UiText.SpecChemPurity + " " + Spec.FormatPct(info.ChemPurity));
                b.Last.SetPreferredSize(InnerW, 18f);

                EnsureLiquidOptions();
                int activeIndex = IndexOfLiquid(string.IsNullOrEmpty(pendingActiveId) ? info.ActiveIngredient : pendingActiveId);
                if (activeIndex < 0)
                {
                    activeIndex = 0;
                    if (liquidIds.Count > 0)
                    {
                        pendingActiveId = liquidIds[0];
                    }
                }
                else
                {
                    pendingActiveId = liquidIds[activeIndex];
                }

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddDropdown(liquidLabels, activeIndex, OnPickActive, null);
                b.Last.SetPreferredSize(170f, 28f);
                b.AddButton(UiText.SpecApplyActive, delegate
                {
                    ApplySpec(item, Spec.SetActiveIngredient(item, pendingActiveId), refresh);
                }, null);
                b.Last.SetPreferredSize(90f, 28f);
                b.End();

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddInput(UiText.SpecPurityPlaceholder, chemPurityText, null, null, delegate(string v)
                {
                    chemPurityText = v ?? "";
                });
                b.Last.SetPreferredSize(90f, 28f);
                b.AddButton(UiText.SpecApplyChemPurity, delegate
                {
                    double percent;
                    if (!TryParsePercent(chemPurityText, out percent))
                    {
                        SetStatus("纯度要填 0-100 的数字");
                        return;
                    }
                    ApplySpec(item, Spec.SetChemPurity(item, percent), refresh);
                }, null);
                b.Last.SetPreferredSize(120f, 28f);
                b.End();
            }

            if (info.IsIngot)
            {
                b.AddSeparator(null, 2);
                b.AddLabel(UiText.SpecIngot + ": " + IngotLabelOf(info.IngotPurity));
                b.Last.SetPreferredSize(InnerW, 18f);

                int current = IndexOfIngot(info.IngotPurity);
                if (pendingIngotIndex < 0 || pendingIngotIndex >= Spec.IngotLabels.Length)
                {
                    pendingIngotIndex = current < 0 ? 0 : current;
                }

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddDropdown(Spec.IngotLabels, pendingIngotIndex, delegate(int v)
                {
                    pendingIngotIndex = v;
                }, null);
                b.Last.SetPreferredSize(170f, 28f);
                b.AddButton(UiText.SpecApplyIngot, delegate
                {
                    int index = pendingIngotIndex;
                    if (index < 0 || index >= Spec.IngotTags.Length)
                    {
                        SetStatus("先选一个纯度等级");
                        return;
                    }
                    ApplySpec(item, Spec.SetIngotPurity(item, Spec.IngotTags[index]), refresh);
                }, null);
                b.Last.SetPreferredSize(100f, 28f);
                b.End();
            }

            BuildGenePanel(b, item, refresh);
            BuildStampPanel(b, item, refresh);
            BuildWearPanel(b, item, refresh);
            BuildUpgradePanel(b, item, refresh);
            BuildTagPanel(b, item, refresh);
        }

        /// <summary>种子基因区：已拥有的性状改等级，没有的加进去（受限的也能强写）。</summary>
        private static void BuildGenePanel(CustomUIBuilder b, GameItem item, Action refresh)
        {
            GeneInfo info = Genes.Describe(item);
            if (!info.IsSeed)
            {
                return;
            }

            b.AddSeparator(null, 2);
            b.AddHeader(UiText.GeneTitle);

            string head = UiText.GenePlant + " " + info.PlantName + "  ·  "
                + (info.Modified ? UiText.GeneModified : UiText.GenePure) + "  ·  "
                + UiText.GeneComplexity + " " + info.Complexity.ToString(CultureInfo.InvariantCulture)
                + "  ·  " + UiText.GeneHeat + " " + info.Heat.ToString(CultureInfo.InvariantCulture);
            if (info.Heat != 0)
            {
                head = head + "  ·  " + UiText.GeneEffective + " " + info.Effective.ToString(CultureInfo.InvariantCulture);
            }
            b.AddLabel(head);
            b.Last.SetPreferredSize(InnerW, 20f);

            b.AddLabel(UiText.GeneHint);
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 32f);

            b.AddLabel("— " + UiText.GeneOwned + " · " + info.Owned.Count.ToString(CultureInfo.InvariantCulture) + " —");
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 18f);

            if (info.Owned.Count == 0)
            {
                b.AddLabel(UiText.GeneOwnedNone);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, 18f);
            }

            for (int i = 0; i < info.Owned.Count; i++)
            {
                GeneRow row = info.Owned[i];
                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddLabel(row.Display + "  " + row.RankText);
                b.Last.SetPreferredSize(200f, 26f);
                b.Last.SetTooltip(GeneTooltip(row));
                b.AddInput(UiText.GeneRankPlaceholder, row.Rank.ToString(CultureInfo.InvariantCulture), null, null, null);
                CustomUIElement input = b.Last;
                input.SetPreferredSize(76f, 26f);
                b.AddButton(UiText.GeneWrite, delegate
                {
                    ApplyGene(item, row.Id, TextOf(input), row.Rank, refresh);
                }, null);
                b.Last.SetPreferredSize(58f, 26f);
                b.End();
            }

            b.AddLabel("— " + UiText.GeneMissing + " · " + info.Missing.Count.ToString(CultureInfo.InvariantCulture) + " —");
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 18f);

            for (int i = 0; i < info.Missing.Count; i++)
            {
                GeneRow row = info.Missing[i];
                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddLabel(row.Addable ? row.Display : row.Display + UiText.GeneBlockedMark);
                b.Last.SetPreferredSize(200f, 26f);
                b.Last.SetTooltip(GeneTooltip(row));
                if (!row.Addable)
                {
                    b.Last.SetTextColor(DimColor);
                }
                b.AddInput(UiText.GeneRankPlaceholder, "1", null, null, null);
                CustomUIElement input = b.Last;
                input.SetPreferredSize(76f, 26f);
                b.AddButton(row.Addable ? UiText.GeneAdd : UiText.GeneForce, delegate
                {
                    ApplyGene(item, row.Id, TextOf(input), 1, refresh);
                }, null);
                b.Last.SetPreferredSize(58f, 26f);
                b.End();
            }

            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddButton(UiText.GeneReset, delegate
            {
                string error = Genes.Reset(item);
                if (!string.IsNullOrEmpty(error))
                {
                    SetStatus(error);
                    return;
                }
                SetStatus("已恢复原种");
                RefreshSpecPanel(refresh);
            }, null);
            b.Last.SetPreferredSize(120f, 26f);
            b.AddButton(UiText.GeneClearHeat, delegate
            {
                string error = Genes.SetHeat(item, 0);
                if (!string.IsNullOrEmpty(error))
                {
                    SetStatus(error);
                    return;
                }
                SetStatus("基因热量已清零");
                RefreshSpecPanel(refresh);
            }, null);
            b.Last.SetPreferredSize(120f, 26f);
            b.End();
        }

        private static void ApplyGene(GameItem item, string traitId, string text, int fallback, Action refresh)
        {
            int rank;
            if (!TryParseInt(text, out rank))
            {
                rank = fallback;
            }
            if (rank < 0)
            {
                rank = 0;
            }

            string error = Genes.SetRank(item, traitId, rank);
            if (!string.IsNullOrEmpty(error))
            {
                SetStatus(error);
                return;
            }

            SetStatus(rank <= 0 ? "性状已去掉" : "性状等级改成 " + Genes.RankLabel(rank));
            RefreshSpecPanel(refresh);
        }

        private static string GeneTooltip(GeneRow row)
        {
            string text = row.Display;
            if (!string.IsNullOrEmpty(row.Effect))
            {
                text = text + "\n" + row.Effect;
            }
            if (row.Rank > 0)
            {
                text = text + "\n" + UiText.GeneRankPlaceholder + " " + row.RankText;
            }
            if (row.Unlimited)
            {
                text = text + "\n" + UiText.GeneLimitUnlimited;
            }
            else
            {
                text = text + "\n" + "游戏内上限 " + row.MaxRank.ToString(CultureInfo.InvariantCulture) + " 级";
            }
            if (!string.IsNullOrEmpty(row.BlockReason))
            {
                text = text + "\n" + UiText.GeneRestricted + row.BlockReason;
            }
            return text;
        }

        private static void RefreshSpecPanel(Action refresh)
        {
            if (refresh == null)
            {
                return;
            }
            try
            {
                refresh();
            }
            catch (Exception e)
            {
                Log.Warn("刷新规格面板失败: " + e.Message);
            }
        }

        /// <summary>配给券区：面值 + 真伪。</summary>
        private static void BuildStampPanel(CustomUIBuilder b, GameItem item, Action refresh)
        {
            if (!Stamps.IsStamp(item))
            {
                return;
            }

            b.AddSeparator(null, 2);
            b.AddHeader(UiText.StampTitle);
            b.Last.SetPreferredSize(InnerW, 24f);

            b.AddLabel(Stamps.Describe(item));
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 20f);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.StampFace);
            b.Last.SetPreferredSize(56f, 28f);
            b.AddInput("", Stamps.AmountText(item), null, null, null);
            CustomUIElement amountBox = b.Last;
            amountBox.SetPreferredSize(78f, 28f);
            b.AddDropdown(Stamps.AuthLabels, Stamps.AuthIndex(item), null, null);
            CustomUIElement authDrop = b.Last;
            authDrop.SetPreferredSize(158f, 28f);
            b.AddButton(UiText.StampWrite, delegate
            {
                string amount = amountBox == null ? "" : amountBox.GetString();
                int auth = authDrop == null ? 0 : authDrop.GetIndex();
                string error = Stamps.Apply(item, amount, auth);
                ApplySpec(item, error, refresh);
                if (error == null)
                {
                    SetStatus("配给券已更新：" + Stamps.Describe(item));
                }
            }, null);
            b.Last.SetPreferredSize(76f, 28f);
            b.End();

            b.AddLabel(UiText.StampHint);
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 40f);
        }

        /// <summary>耐久区：当前 / 上限 / 假耐久。</summary>
        private static void BuildWearPanel(CustomUIBuilder b, GameItem item, Action refresh)
        {
            if (!Wear.Has(item))
            {
                return;
            }

            b.AddSeparator(null, 2);
            b.AddHeader(UiText.DurTitle);

            b.AddLabel(Wear.Describe(item));
            b.Last.SetPreferredSize(InnerW, 18f);

            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.DurCurrent);
            b.Last.SetPreferredSize(40f, 28f);
            b.AddInput("", Wear.Real(item).ToString(CultureInfo.InvariantCulture), null, null, null);
            CustomUIElement realInput = b.Last;
            realInput.SetPreferredSize(64f, 28f);
            b.AddLabel("/");
            b.Last.SetPreferredSize(12f, 28f);
            b.AddInput("", Wear.Max(item).ToString(CultureInfo.InvariantCulture), null, null, null);
            CustomUIElement maxInput = b.Last;
            maxInput.SetPreferredSize(64f, 28f);
            b.AddButton(UiText.DurApply, delegate
            {
                int real;
                int max;
                if (!TryParseInt(TextOf(realInput), out real) || !TryParseInt(TextOf(maxInput), out max))
                {
                    SetStatus("耐久要填数字");
                    return;
                }
                int fake = fakeOf(item, real);
                ApplySpec(item, Wear.Apply(item, real, fake, max), refresh);
            }, null);
            b.Last.SetPreferredSize(70f, 28f);
            b.End();

            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.DurFake);
            b.Last.SetPreferredSize(56f, 28f);
            b.AddInput("", Wear.Fake(item).ToString(CultureInfo.InvariantCulture), null, null, null);
            CustomUIElement fakeInput = b.Last;
            fakeInput.SetPreferredSize(64f, 28f);
            b.AddButton(UiText.DurApply, delegate
            {
                int fake;
                int real;
                int max;
                if (!TryParseInt(TextOf(fakeInput), out fake))
                {
                    SetStatus("耐久要填数字");
                    return;
                }
                if (!TryParseInt(TextOf(realInput), out real))
                {
                    real = Wear.Real(item);
                }
                if (!TryParseInt(TextOf(maxInput), out max))
                {
                    max = Wear.Max(item);
                }
                ApplySpec(item, Wear.Apply(item, real, fake, max), refresh);
            }, null);
            b.Last.SetPreferredSize(70f, 28f);
            b.AddButton(UiText.DurFill, delegate
            {
                ApplySpec(item, Wear.Fill(item), refresh);
            }, null);
            b.Last.SetPreferredSize(80f, 28f);
            b.End();
        }

        private static int fakeOf(GameItem item, int real)
        {
            try
            {
                return Wear.Fake(item);
            }
            catch
            {
                return real;
            }
        }

        /// <summary>升级等级区：这台机器身上有几条升级线就列几行。</summary>
        private static void BuildUpgradePanel(CustomUIBuilder b, GameItem item, Action refresh)
        {
            List<UpgradeChannel> channels = Upgrades.ChannelsOf(item);
            if (channels.Count == 0)
            {
                return;
            }

            b.AddSeparator(null, 2);
            b.AddHeader(UiText.UpTitle);

            for (int i = 0; i < channels.Count; i++)
            {
                UpgradeChannel channel = channels[i];
                int current = Upgrades.Level(item, channel);

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddLabel(channel.Label);
                b.Last.SetPreferredSize(126f, 28f);
                b.AddLabel(current + " / " + channel.Max);
                b.Last.SetPreferredSize(58f, 28f);
                b.AddInput("", current.ToString(CultureInfo.InvariantCulture), null, null, null);
                CustomUIElement levelInput = b.Last;
                levelInput.SetPreferredSize(56f, 28f);
                b.AddButton(UiText.UpApply, delegate
                {
                    int level;
                    if (!TryParseInt(TextOf(levelInput), out level))
                    {
                        SetStatus("等级要填数字");
                        return;
                    }
                    ApplySpec(item, Upgrades.Apply(item, channel, level), refresh);
                }, null);
                b.Last.SetPreferredSize(80f, 28f);
                b.End();
            }
        }

        /// <summary>词条区：这件物品身上带着的每条 tag，按类型给一个能写的输入框。</summary>
        private static void BuildTagPanel(CustomUIBuilder b, GameItem item, Action refresh)
        {
            List<TagRow> rows = Tags.Rows(item);

            b.AddSeparator(null, 2);
            b.AddHeader(UiText.TagTitle);

            if (rows.Count == 0)
            {
                b.AddLabel(UiText.TagNone);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, 18f);
                return;
            }

            b.AddLabel(UiText.TagHint);
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 18f);

            string group = null;
            for (int i = 0; i < rows.Count; i++)
            {
                TagRow row = rows[i];

                if (row.Group != group)
                {
                    group = row.Group;
                    b.AddLabel("— " + group + " —");
                    b.Last.SetTextColor(DimColor);
                    b.Last.SetPreferredSize(InnerW, 18f);
                }

                if (row.Kind == 'b')
                {
                    b.BeginRow(4f, TextAnchor.MiddleLeft);
                    b.AddLabel(row.Label);
                    b.Last.SetPreferredSize(180f, 26f);
                    b.Last.SetTooltip(TagDoc.Info(row));
                    b.AddToggle("", item.IsTag(row.Tag), delegate(bool on)
                    {
                        ApplyTag(item, row, on ? "有" : "无", refresh);
                    }, null);
                    b.Last.SetPreferredSize(54f, 26f);
                    b.End();
                    continue;
                }

                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddLabel(row.Label);
                b.Last.SetPreferredSize(180f, 26f);
                b.Last.SetTooltip(TagDoc.Info(row));
                b.AddInput("", row.Display, null, null, null);
                CustomUIElement input = b.Last;
                input.SetPreferredSize(84f, 26f);
                b.AddButton(UiText.TagWrite, delegate
                {
                    ApplyTag(item, row, TextOf(input), refresh);
                }, null);
                b.Last.SetPreferredSize(58f, 26f);
                b.End();
            }

            List<string> unrated = Tags.Unrated(item);
            if (unrated.Count > 0)
            {
                List<string> shown = new List<string>();
                for (int i = 0; i < unrated.Count; i++)
                {
                    string zh = TagLabels.Name(unrated[i]);
                    shown.Add(string.IsNullOrEmpty(zh) ? unrated[i] : zh);
                }

                string text = UiText.TagUnrated + " " + string.Join("、", shown.ToArray());
                b.AddLabel(text);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, Mathf.Max(44f, 20f * Mathf.CeilToInt(text.Length / 20f)));
                b.Last.SetTooltip(string.Join("\u000A", unrated.ToArray()));
            }
        }

        private static void ApplyTag(GameItem item, TagRow row, string text, Action refresh)
        {
            string error = Tags.Write(item, row, text);
            if (!string.IsNullOrEmpty(error))
            {
                SetStatus(error);
                return;
            }

            SetStatus("词条已写入：" + row.Label);
            if (refresh != null)
            {
                try
                {
                    refresh();
                }
                catch (Exception e)
                {
                    Log.Warn("刷新词条面板失败: " + e.Message);
                }
            }
        }

        private static void OnPickLiquid(int index)
        {
            if (liquidIds == null || index < 0 || index >= liquidIds.Count)
            {
                return;
            }
            pendingLiquidId = liquidIds[index];
        }

        private static void OnPickActive(int index)
        {
            if (liquidIds == null || index < 0 || index >= liquidIds.Count)
            {
                return;
            }
            pendingActiveId = liquidIds[index];
        }

        private static void ApplySpec(GameItem item, string error, Action refresh)
        {
            if (!string.IsNullOrEmpty(error))
            {
                SetStatus(error);
                return;
            }
            SetStatus("规格已更新");
            if (refresh != null)
            {
                try
                {
                    refresh();
                }
                catch (Exception e)
                {
                    Log.Warn("刷新规格面板失败: " + e.Message);
                }
            }
        }

        private static void EnsureLiquidOptions()
        {
            if (liquidIds != null && liquidIds.Count > 0 && liquidLabels != null && liquidLabels.Count == liquidIds.Count)
            {
                return;
            }

            liquidIds = new List<string>();
            liquidLabels = new List<string>();
            List<Liquid> all = Spec.Liquids;
            for (int i = 0; i < all.Count; i++)
            {
                Liquid l = all[i];
                if (l == null || string.IsNullOrEmpty(l.identifier))
                {
                    continue;
                }
                liquidIds.Add(l.identifier);
                liquidLabels.Add(string.IsNullOrEmpty(l.displayName) ? l.identifier : l.displayName);
            }
        }

        private static int IndexOfLiquid(string liquidId)
        {
            if (liquidIds == null || string.IsNullOrEmpty(liquidId))
            {
                return -1;
            }
            for (int i = 0; i < liquidIds.Count; i++)
            {
                if (liquidIds[i] == liquidId)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int IndexOfIngot(string purityTag)
        {
            if (string.IsNullOrEmpty(purityTag))
            {
                return -1;
            }
            for (int i = 0; i < Spec.IngotTags.Length; i++)
            {
                if (Spec.IngotTags[i] == purityTag)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string IngotLabelOf(string purityTag)
        {
            int index = IndexOfIngot(purityTag);
            return index < 0 ? "未鉴定" : Spec.IngotLabels[index];
        }

        // ================= 编辑页 =================

        private static void OnRefreshEdit()
        {
            RebuildEditFilterOptions();
            RefreshEditList();
            RebuildEditForm();
            RebuildEditSpec();
            SetStatus("台面已刷新");
        }

        private static void OnIncludeStoreChanged(bool value)
        {
            includeStore = value;
            RebuildEditFilterOptions();
            RefreshEditList();
        }

        private static void OnPick()
        {
            PawnshopDriver.BeginPick();
        }

        /// <summary>台面上现有物品的 id 列表，只给两个下拉数件数用。同一个 id 出现几次就数几次，和台面上看到的一致。</summary>
        private static List<string> EditIds()
        {
            List<string> ids = new List<string>();
            List<GameItem> items = Catalog.CounterItems(includeStore);
            for (int i = 0; i < items.Count; i++)
            {
                GameItem item = items[i];
                if (item == null)
                {
                    continue;
                }
                try
                {
                    ids.Add(item.identifier);
                }
                catch
                {
                    // 个别物品取不到 id，跳过就是
                }
            }
            return ids;
        }

        private static void OnEditSearchTyping(string text)
        {
            editFilter = text ?? "";
            RebuildEditFilterOptions();
            RefreshEditList();
        }

        private static void OnEditSectionChanged(int index)
        {
            if (editSectionLock)
            {
                return;
            }
            editSectionIndex = index >= 0 && index < editSectionMap.Count ? editSectionMap[index] : 0;
            // 换了大类，小类回到「全部小类」
            editGroupIndex = 0;
            RebuildEditGroupOptions();
            RefreshEditList();
        }

        private static void OnEditGroupChanged(int index)
        {
            if (editGroupLock)
            {
                return;
            }
            editGroupIndex = index >= 0 && index < editGroupMap.Count ? editGroupMap[index] : 0;
            RefreshEditList();
        }

        private static void RebuildEditGroupOptions()
        {
            ReloadGroupOptions(editGroupDropdown, editGroupMap, ref editGroupIndex, ref editGroupLock, editFilter, editSectionIndex, EditIds());
        }

        /// <summary>点选进来的物品要是被当前筛选挡住，就把筛选清掉——选中了却在列表里看不见很难受。</summary>
        private static void RevealInEditList(GameItem item)
        {
            string id;
            try
            {
                id = item.identifier;
            }
            catch
            {
                return;
            }
            if (Catalog.InFilter(id, editFilter, editSectionIndex, editGroupIndex))
            {
                return;
            }

            editFilter = "";
            editSectionIndex = 0;
            editGroupIndex = 0;
            if (editSearch != null && editSearch.IsAlive)
            {
                try
                {
                    editSearch.SetString("");
                }
                catch (Exception e)
                {
                    Log.Warn("清空搜索框失败: " + e.Message);
                }
            }
            RebuildEditFilterOptions();
            RefreshEditList();
        }

        private static void RefreshEditList()
        {
            if (editGridHost == null || !HostAlive(editGridHost) || window == null)
            {
                return;
            }

            CustomUIBuilder.ClearContainer(editGridHost);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, window, editGridHost);

            List<GameItem> items = Catalog.CounterItems(includeStore);
            List<GameItem> shown = new List<GameItem>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                GameItem item = items[i];
                if (item == null)
                {
                    continue;
                }
                string id;
                try
                {
                    id = item.identifier;
                }
                catch
                {
                    continue;
                }
                if (Catalog.InFilter(id, editFilter, editSectionIndex, editGroupIndex))
                {
                    shown.Add(item);
                }
            }

            GameItem current = ItemEditor.Target;
            bool filtered = editFilter.Length > 0 || editSectionIndex > 0 || editGroupIndex > 0;

            if (shown.Count == 0)
            {
                b.AddLabel(items.Count == 0 ? UiText.NoStoreItems : UiText.EditEmptyList);
            }
            else
            {
                b.BeginGrid(GridColumns, CellW, CellH, 4f);
                for (int i = 0; i < shown.Count && i < MaxCells; i++)
                {
                    GameItem item = shown[i];
                    string label = "";
                    string id = "";
                    try
                    {
                        label = item.GetDisplayName(true);
                        id = item.identifier;
                    }
                    catch
                    {
                        continue;
                    }
                    AddTile(b, id + "  ×" + SafeCount(item), label, current == item, delegate
                    {
                        SelectEditItem(item);
                    }, null);
                }
                b.End();
            }

            CustomUIBuilder.RebuildLayout(editGridHost);

            if (editInfo != null && editInfo.IsAlive)
            {
                string info = "台面上 " + items.Count + " 件" + (includeStore ? "（含店内库存）" : "");
                if (filtered)
                {
                    info += "，筛出 " + shown.Count + " 件";
                }
                editInfo.SetText(info);
            }

            if (shown.Count > MaxCells)
            {
                SetStatus("匹配 " + shown.Count + " 件，只显示前 " + MaxCells + " 件。缩小关键词再找。");
            }
        }

        private static void SelectEditItem(GameItem item)
        {
            if (item == null)
            {
                return;
            }
            ItemEditor.Select(item);
            RebuildEditForm();
            RebuildEditSpec();
            RefreshEditList();
            string name = "";
            try
            {
                name = item.GetDisplayName(true);
            }
            catch
            {
                name = item.identifier;
            }
            SetStatus("已选中 " + name);
        }

        private static void RebuildEditForm()
        {
            if (editFormHost == null || !HostAlive(editFormHost) || window == null)
            {
                return;
            }

            CustomUIBuilder.ClearContainer(editFormHost);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, window, editFormHost);

            GameItem item = ItemEditor.Target;
            if (item == null)
            {
                b.AddLabel(UiText.NoTarget);
                CustomUIBuilder.RebuildLayout(editFormHost);
                FitTexts(editFormHost);
                ReflowEditForm();
                return;
            }

            string name = "";
            try
            {
                name = item.GetDisplayName(true);
            }
            catch
            {
                name = item.identifier;
            }

            b.AddHeader(name);
            b.Last.SetPreferredSize(InnerW, 26f);
            b.AddLabel("[" + item.identifier + "]  " + ItemEditor.Describe(item).Replace("\n", "  ·  "));
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 20f);

            b.AddSeparator(null, 2);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            fCount = AddCompactField(b, UiText.FieldCount, SafeCountText(item));
            fValue = AddCompactField(b, UiText.FieldUnitValue, SafeValueText(item));
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            fBase = AddCompactField(b, UiText.FieldBaseValue, item.unitBaseValue.ToString(CultureInfo.InvariantCulture));
            fLate = AddCompactField(b, UiText.FieldLateValue, item.lateUnitValue.ToString(CultureInfo.InvariantCulture));
            b.End();

            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddButton(UiText.Apply, OnApply, null);
            b.Last.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.Reload, OnReload, null);
            b.Last.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.Duplicate, OnDuplicate, null);
            b.Last.SetPreferredSize(90f, 30f);
            b.End();

            CustomUIBuilder.RebuildLayout(editFormHost);
            FitTexts(editFormHost);
            ReflowEditForm();
        }

        /// <summary>表单区不滚动：先给每段说明量出真实行高，再让父级重排，三个按钮永远排在最后露着。</summary>
        private static void ReflowEditForm()
        {
            if (!HostAlive(editFormHost))
            {
                return;
            }

            FitLabelHeights(editFormHost);
            CustomUIBuilder.RebuildLayout(editFormHost);

            RectTransform parent = editFormHost.parent as RectTransform;
            if (parent != null)
            {
                CustomUIBuilder.RebuildLayout(parent);
            }
        }

        /// <summary>把自动换行的文字按实际宽度重新量一遍高度，长说明不会被裁成两行。</summary>
        private static void FitLabelHeights(RectTransform host)
        {
            if (host == null)
            {
                return;
            }

            TextMeshProUGUI[] texts = host.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI t = texts[i];
                if (t == null || string.IsNullOrEmpty(t.text))
                {
                    continue;
                }
                if (t.GetComponentInParent<TMP_InputField>() != null || t.GetComponentInParent<TMP_Dropdown>() != null)
                {
                    continue;
                }

                LayoutElement le = t.GetComponent<LayoutElement>();
                if (le == null)
                {
                    continue;
                }

                float width = t.rectTransform.rect.width;
                if (width <= 1f)
                {
                    width = InnerW;
                }

                float h;
                try
                {
                    h = t.GetPreferredValues(t.text, width, 0f).y;
                }
                catch
                {
                    continue;
                }
                if (h > 0f)
                {
                    le.preferredHeight = h + 2f;
                }
            }
        }

        private static void RebuildEditSpec()
        {
            if (editSpecHost == null || !HostAlive(editSpecHost) || window == null)
            {
                return;
            }

            CustomUIBuilder.ClearContainer(editSpecHost);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, window, editSpecHost);

            GameItem item = ItemEditor.Target;
            if (item == null)
            {
                b.AddLabel(UiText.NoTarget);
            }
            else
            {
                BuildSpecPanel(b, item, RebuildEditSpec);
            }

            CustomUIBuilder.RebuildLayout(editSpecHost);
            FitTexts(editSpecHost);
        }

        private static void OnApply()
        {
            GameItem item = ItemEditor.Target;
            if (item == null)
            {
                SetStatus(UiText.NoTarget);
                return;
            }

            EditInput input = new EditInput();
            input.Count = TextOf(fCount);
            input.UnitValue = TextOf(fValue);
            input.BaseValue = TextOf(fBase);
            input.LateValue = TextOf(fLate);
            input.BlankClears = false;

            string error = ItemEditor.Apply(item, input);
            SetStatus(error);
            RebuildEditForm();
            RefreshEditList();
        }

        private static void OnReload()
        {
            RebuildEditForm();
            RebuildEditSpec();
            SetStatus("已重新载入物品当前的值");
        }

        private static void OnDuplicate()
        {
            GameItem item = ItemEditor.Target;
            if (item == null)
            {
                SetStatus(UiText.NoTarget);
                return;
            }
            SetStatus(ItemEditor.Duplicate(item));
            RefreshEditList();
        }

        private static void OnClearCustomName()
        {
            GameItem item = ItemEditor.Target;
            if (item == null)
            {
                SetStatus(UiText.NoTarget);
                return;
            }
            SetStatus(ItemEditor.ClearCustomName(item));
            RebuildEditForm();
            RefreshEditList();
        }

        /// <summary>紧凑一格：小标题 + 输入框，直接加进当前 Row（两格一行，不再各占一整行）。</summary>
        private static CustomUIElement AddCompactField(CustomUIBuilder b, string label, string value)
        {
            b.AddLabel(label);
            b.Last.SetPreferredSize(64f, 28f);
            b.AddInput("", value ?? "", null, null, null);
            CustomUIElement field = b.Last;
            field.SetPreferredSize(104f, 28f);
            return field;
        }

        // ================= 小工具 =================

        private static bool HostAlive(RectTransform host)
        {
            return host != null && host.gameObject != null;
        }

        private static string TextOf(CustomUIElement element)
        {
            if (element == null || !element.IsAlive)
            {
                return "";
            }
            try
            {
                return element.GetString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string SafeName(GameItem item)
        {
            try
            {
                return item.name ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string SafeCustomName(GameItem item)
        {
            try
            {
                return GeneralHelper.HasCustomName(item) ? (GeneralHelper.GetCustomName(item) ?? "") : "";
            }
            catch
            {
                return "";
            }
        }

        private static string SafeCountText(GameItem item)
        {
            try
            {
                return item.unitCount.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                return "";
            }
        }

        private static int SafeCount(GameItem item)
        {
            try
            {
                return item.unitCount;
            }
            catch
            {
                return 0;
            }
        }

        private static string SafeValueText(GameItem item)
        {
            try
            {
                return item.unitValue.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                return "";
            }
        }

        private static string SafeText(string text)
        {
            return text ?? "";
        }

        private static string Shorten(string text, int max)
        {
            string value = text ?? "";
            if (value.Length <= max)
            {
                return value;
            }
            return value.Substring(0, max) + "…";
        }

        private static bool TryParseInt(string raw, out int value)
        {
            value = 0;
            string text = (raw ?? "").Trim();
            if (text.Length == 0)
            {
                return false;
            }
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseLong(string raw, out long value)
        {
            value = 0;
            string text = (raw ?? "").Trim();
            if (text.Length == 0)
            {
                return false;
            }
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseDouble(string raw, out double value)
        {
            value = 0;
            string text = (raw ?? "").Trim();
            if (text.Length == 0)
            {
                return false;
            }
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParsePercent(string raw, out double value)
        {
            string text = (raw ?? "").Trim();
            if (text.EndsWith("%"))
            {
                text = text.Substring(0, text.Length - 1).Trim();
            }
            if (!TryParseDouble(text, out value))
            {
                return false;
            }
            if (value < 0)
            {
                value = 0;
            }
            if (value > 100)
            {
                value = 100;
            }
            return true;
        }
    }

    /// <summary>格子上的点击回调（左键选中 / 右键备用）。</summary>
    internal sealed class CellClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Action onLeftClick;
        public Action onRightClick;
        public Image plate;
        public Color tintNormal;
        public Color tintHover;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (onLeftClick != null)
                {
                    onLeftClick();
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (onRightClick != null)
                {
                    onRightClick();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (plate != null)
            {
                plate.color = tintHover;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (plate != null)
            {
                plate.color = tintNormal;
            }
        }
    }
}
