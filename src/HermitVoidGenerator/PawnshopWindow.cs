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
    internal static class PawnshopWindow
    {
        private const string WindowId = "yg_deep_space_pawnshop_window";
        private const float WinW = 1240f;
        private const float WinH = 800f;
        private const float LeftW = 716f;
        private const float RightW = 424f;
        private const float PaneH = 706f;
        private const float InnerW = 390f;

        private const int GridColumns = 4;
        private const float CellW = 166f;
        private const float CellH = 112f;
        private const int MaxCells = 180;

        private static readonly Color CellNormal = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color CellSelected = new Color(1f, 0.78f, 0.30f, 0.95f);
        private static readonly Color CellGlow = new Color(1f, 0.78f, 0.30f, 0.22f);
        private static readonly Color IconDim = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color StatusColor = new Color(0.86f, 0.84f, 0.72f, 1f);
        private static readonly Color DimColor = new Color(0.72f, 0.70f, 0.62f, 1f);

        private static CustomUIWindow window;

        // ---- 生成页
        private static RectTransform createGridHost;
        private static RectTransform createSpecHost;
        private static CustomUIElement createStatus;
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
        private static CustomUIElement editStatus;
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
        private static CustomUIElement basicStatus;
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

        public static void SetStatus(string text)
        {
            string value = text ?? "";
            try
            {
                if (createStatus != null && createStatus.IsAlive)
                {
                    createStatus.SetText(value);
                }
                if (editStatus != null && editStatus.IsAlive)
                {
                    editStatus.SetText(value);
                }
            }
            catch
            {
                // 状态栏没了不影响主流程
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
            createStatus = null;
            draftTitle = null;
            draftCount = null;
            draftPrice = null;
            draftWhere = null;
            editGridHost = null;
            editFormHost = null;
            editSpecHost = null;
            editSpecScroll = null;
            editStatus = null;
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
            basicStatus = null;
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
            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null)
            {
                throw new InvalidOperationException("CustomUIManager 还没准备好");
            }

            string layer = CustomUIManager.HasLayer(CustomUIManager.LAYER_OVERLAY)
                ? CustomUIManager.LAYER_OVERLAY
                : CustomUIManager.LAYER_REAR;

            CustomUIBuilder b = CustomUIManager.Window(WindowId, UiText.Title, layer);
            if (b == null || b.Handle == null)
            {
                throw new InvalidOperationException("窗口没造出来");
            }

            window = b.Handle;
            b.SetSize(WinW, WinH).SetDraggable(true).SetCloseOnEscape(true).OnClose(OnWindowClosed);

            b.BeginTabs();
            b.BeginTab(UiText.TabBasic);
            BuildBasicPane(b);
            b.End();
            b.BeginTab(UiText.TabCreate);
            BuildCreatePane(b);
            b.End();
            b.BeginTab(UiText.TabEdit);
            BuildEditPane(b);
            b.End();
            b.EndTabs();

            b.Show();
            if (window != null && window.Rect != null)
            {
                window.Center();
            }
            Log.Info("面板已构建");
        }

        private static void OnWindowClosed()
        {
            StopIcons();
            DestroyDraft();
            window = null;
            ClearHosts();
        }

        // ================= 基础修改 =================

        private static void BuildBasicPane(CustomUIBuilder b)
        {
            b.BeginRow(6f, TextAnchor.UpperLeft);

            // ---- 左：钱与账目 / 店铺 / 定价
            b.BeginPanel(LeftW, PaneH, TextAnchor.UpperLeft, 8f, 4f);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddHeader(UiText.BasicMoney);
            b.Last.SetPreferredSize(320f, 26f);
            b.AddButton(UiText.BasicRefresh, OnRefreshBasic, null);
            b.Last.SetPreferredSize(110f, 26f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicCash);
            b.Last.SetPreferredSize(60f, 30f);
            b.AddInput("", BasicStats.CashText(), null, null, null);
            bCash = b.Last;
            bCash.SetPreferredSize(180f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteCash, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicRent);
            b.Last.SetPreferredSize(60f, 30f);
            b.AddInput("", BasicStats.RentText(), null, null, null);
            bRent = b.Last;
            bRent.SetPreferredSize(120f, 30f);
            b.AddLabel(UiText.BasicDaysLeft);
            b.Last.SetPreferredSize(80f, 30f);
            b.AddInput("", BasicStats.RentDaysText(), null, null, null);
            bRentDays = b.Last;
            bRentDays.SetPreferredSize(70f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteRent, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicLoan);
            b.Last.SetPreferredSize(60f, 30f);
            b.AddInput("", BasicStats.LoanText(), null, null, null);
            bLoan = b.Last;
            bLoan.SetPreferredSize(120f, 30f);
            b.AddLabel(UiText.BasicDaysLeft);
            b.Last.SetPreferredSize(80f, 30f);
            b.AddInput("", BasicStats.LoanDaysText(), null, null, null);
            bLoanDays = b.Last;
            bLoanDays.SetPreferredSize(70f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteLoan, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.AddSeparator(null, 6);

            b.AddHeader(UiText.BasicStore);
            b.Last.SetPreferredSize(320f, 26f);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicAttr);
            b.Last.SetPreferredSize(60f, 30f);
            b.AddInput("", BasicStats.AttractText(), null, null, null);
            bAttr = b.Last;
            bAttr.SetPreferredSize(110f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteAttr, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.AddLabel(UiText.BasicProjector);
            b.Last.SetPreferredSize(80f, 30f);
            b.AddInput("", BasicStats.ProjectorText(), null, null, null);
            bProjector = b.Last;
            bProjector.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteProjector, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.AddSeparator(null, 6);

            b.AddHeader(UiText.BasicPricing);
            b.Last.SetPreferredSize(320f, 26f);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicRetail);
            b.Last.SetPreferredSize(90f, 30f);
            b.AddInput("", BasicStats.RetailMarkupText(), null, null, null);
            bRetail = b.Last;
            bRetail.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteRetail, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicContraLow);
            b.Last.SetPreferredSize(110f, 30f);
            b.AddInput("", BasicStats.ContrabandLowText(), null, null, null);
            bContraLow = b.Last;
            bContraLow.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteContraLow, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.AddLabel(UiText.BasicContraMid);
            b.Last.SetPreferredSize(110f, 30f);
            b.AddInput("", BasicStats.ContrabandMidText(), null, null, null);
            bContraMid = b.Last;
            bContraMid.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteContraMid, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.AddSeparator(null, 6);

            b.AddHeader(UiText.BasicRelation);
            b.Last.SetPreferredSize(320f, 26f);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicFavor);
            b.Last.SetPreferredSize(110f, 30f);
            b.Last.SetTooltip("王尔德的恩惠（PlayerStore.wildFavor）。夜里找乔·王尔德买服务时优先拿它抵扣，不够的部分才从现金补。");
            b.AddInput("", BasicStats.FavorText(), null, null, null);
            bFavor = b.Last;
            bFavor.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteFavor, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.AddLabel(UiText.BasicCommissary);
            b.Last.SetPreferredSize(110f, 30f);
            b.Last.SetTooltip("配给站券余额（PlayerStore.commissaryData.redeemedCredit）。在配给站兑换配给券会累加到这里，配给站买东西只扣这个数。");
            b.AddInput("", BasicStats.CommissaryCreditText(), null, null, null);
            bCommissaryCred = b.Last;
            bCommissaryCred.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteCommissaryCredit, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.BasicContraHigh);
            b.Last.SetPreferredSize(110f, 30f);
            b.AddInput("", BasicStats.ContrabandHighText(), null, null, null);
            bContraHigh = b.Last;
            bContraHigh.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteContraHigh, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.AddLabel(UiText.BasicContraCrit);
            b.Last.SetPreferredSize(110f, 30f);
            b.AddInput("", BasicStats.ContrabandCriticalText(), null, null, null);
            bContraCrit = b.Last;
            bContraCrit.SetPreferredSize(90f, 30f);
            b.AddButton(UiText.BasicWrite, OnWriteContraCrit, null);
            b.Last.SetPreferredSize(70f, 30f);
            b.End();

            b.End();

            // ---- 右：阵营声望 + 伤势
            b.BeginPanel(RightW, PaneH, TextAnchor.UpperLeft, 8f, 4f);

            b.AddHeader(UiText.BasicFactions);
            b.Last.SetPreferredSize(InnerW, 26f);

            List<BasicStats.FactionRow> factions = BasicStats.Factions();
            if (factions.Count == 0)
            {
                b.AddLabel(UiText.BasicFactionNone);
                b.Last.SetTextColor(DimColor);
                b.Last.SetPreferredSize(InnerW, 24f);
            }
            else
            {
                foreach (BasicStats.FactionRow faction in factions)
                {
                    string fid = faction.Id;
                    b.BeginRow(6f, TextAnchor.MiddleLeft);
                    b.AddLabel(faction.Name);
                    b.Last.SetPreferredSize(96f, 28f);
                    b.Last.SetTooltip(fid);
                    b.AddInput("", BasicStats.RepText(fid), null, null, null);
                    CustomUIElement repBox = b.Last;
                    repBox.SetPreferredSize(62f, 28f);
                    facInputs[fid] = repBox;
                    b.AddButton(UiText.BasicWrite, delegate { OnWriteRep(fid); }, null);
                    b.Last.SetPreferredSize(56f, 28f);
                    b.AddLabel(BasicStats.RepTierText(fid));
                    b.Last.SetTextColor(DimColor);
                    b.Last.SetPreferredSize(140f, 28f);
                    facTiers[fid] = b.Last;
                    b.End();
                }
            }

            b.AddSeparator(null, 6);

            b.AddHeader(UiText.BasicHealth);
            b.Last.SetPreferredSize(InnerW, 26f);

            woundIndex = BasicStats.WoundOptionIndex();
            woundStable = BasicStats.WoundStable();
            b.AddDropdown(BasicStats.WoundOptions, woundIndex, OnWoundChanged, null);
            bWound = b.Last;
            bWound.SetPreferredSize(InnerW, 32f);

            b.AddToggle(UiText.BasicWoundStable, woundStable, OnWoundStableChanged, null);
            bWoundStable = b.Last;
            bWoundStable.SetPreferredSize(InnerW, 28f);

            b.AddButton(UiText.BasicWrite, OnWriteWound, null);
            b.Last.SetPreferredSize(110f, 30f);

            b.AddLabel("");
            basicStatus = b.Last;
            basicStatus.SetTextColor(StatusColor);
            basicStatus.SetPreferredSize(InnerW, 32f);

            b.AddSeparator(null, 6);

            b.AddLabel(UiText.BasicHint);
            b.Last.SetTextColor(DimColor);
            b.Last.SetPreferredSize(InnerW, 130f);
            if (b.Last.text != null)
            {
                b.Last.text.enableWordWrapping = true;
                b.Last.text.enableAutoSizing = true;
                b.Last.text.fontSizeMin = 11f;
                b.Last.text.fontSizeMax = 15f;
            }

            b.End();
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
            if (basicStatus == null) return;
            basicStatus.SetText(text ?? "");
            basicStatus.SetTextColor(StatusColor);
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
            b.BeginRow(6f, TextAnchor.UpperLeft);

            // ---- 左：目录
            b.BeginPanel(LeftW, PaneH, TextAnchor.UpperLeft, 8f, 4f);
            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddInput(UiText.SearchHint, createFilter, null, null, OnSearchTyping);
            b.Last.SetPreferredSize(300f, 32f);
            List<string> sectionOptions = BuildSectionOptions(createFilter, null, sectionMap);
            int sectionPos = sectionMap.IndexOf(createSectionIndex);
            b.AddDropdown(sectionOptions, sectionPos < 0 ? 0 : sectionPos, OnSectionChanged, null);
            createSectionDropdown = b.Last;
            b.Last.SetPreferredSize(190f, 32f);
            List<string> groupOptions = BuildGroupOptions(createFilter, createSectionIndex, null, groupMap);
            int groupPos = groupMap.IndexOf(createGroupIndex);
            b.AddDropdown(groupOptions, groupPos < 0 ? 0 : groupPos, OnGroupChanged, null);
            createGroupDropdown = b.Last;
            b.Last.SetPreferredSize(190f, 32f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.ListHint);
            b.Last.SetPreferredSize(LeftW - 148f, 18f);
            b.AddButton(UiText.RefreshCatalog, OnRefreshCatalog, null);
            b.Last.SetPreferredSize(110f, 24f);
            b.End();

            b.BeginScroll(584f);
            createGridHost = b.CurrentContainer;
            b.End();

            b.AddLabel("");
            createStatus = b.Last;
            createStatus.SetTextColor(StatusColor);
            createStatus.SetPreferredSize(LeftW - 20f, 36f);
            b.End();

            // ---- 右：生成参数 + 规格
            b.BeginPanel(RightW, PaneH, TextAnchor.UpperLeft, 8f, 4f);

            b.AddHeader(UiText.DraftNone);
            draftTitle = b.Last;
            draftTitle.SetPreferredSize(RightW - 20f, 26f);

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.CountLabel);
            b.Last.SetPreferredSize(40f, 30f);
            b.AddInput("1", "1", null, null, null);
            draftCount = b.Last;
            draftCount.SetPreferredSize(70f, 30f);
            b.AddLabel(UiText.PriceLabel);
            b.Last.SetPreferredSize(150f, 30f);
            b.AddInput("", "", null, null, null);
            draftPrice = b.Last;
            draftPrice.SetPreferredSize(110f, 30f);
            b.End();

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(UiText.WhereLabel);
            b.Last.SetPreferredSize(80f, 30f);
            b.AddDropdown(UiText.SpawnTargets, whereIndex, OnWhereChanged, null);
            draftWhere = b.Last;
            draftWhere.SetPreferredSize(140f, 30f);
            b.AddButton(UiText.SpawnButton, OnSpawn, null);
            b.Last.SetPreferredSize(100f, 30f);
            b.End();

            b.BeginScroll(548f);
            createSpecHost = b.CurrentContainer;
            b.End();

            b.End();
            b.End();
        }

        private static void BuildEditPane(CustomUIBuilder b)
        {
            b.BeginRow(6f, TextAnchor.UpperLeft);

            b.BeginPanel(LeftW, PaneH, TextAnchor.UpperLeft, 8f, 4f);
            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddButton(UiText.RefreshStore, OnRefreshEdit, null);
            b.Last.SetPreferredSize(110f, 32f);
            b.AddButton(UiText.PickButton, OnPick, null);
            b.Last.SetPreferredSize(110f, 32f);
            b.AddToggle(UiText.IncludeStore, includeStore, OnIncludeStoreChanged, null);
            editIncludeStore = b.Last;
            b.Last.SetPreferredSize(220f, 32f);
            b.End();

            // 搜索 + 大/小分类：和生成页同一套，只是筛的是台面上这几件
            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddInput(UiText.EditSearchHint, editFilter, null, null, OnEditSearchTyping);
            editSearch = b.Last;
            b.Last.SetPreferredSize(300f, 32f);
            List<string> editSectionOptions = BuildSectionOptions(editFilter, EditIds(), editSectionMap);
            int editSectionPos = editSectionMap.IndexOf(editSectionIndex);
            b.AddDropdown(editSectionOptions, editSectionPos < 0 ? 0 : editSectionPos, OnEditSectionChanged, null);
            editSectionDropdown = b.Last;
            b.Last.SetPreferredSize(190f, 32f);
            List<string> editGroupOptions = BuildGroupOptions(editFilter, editSectionIndex, EditIds(), editGroupMap);
            int editGroupPos = editGroupMap.IndexOf(editGroupIndex);
            b.AddDropdown(editGroupOptions, editGroupPos < 0 ? 0 : editGroupPos, OnEditGroupChanged, null);
            editGroupDropdown = b.Last;
            b.Last.SetPreferredSize(190f, 32f);
            b.End();

            b.AddLabel("");
            editInfo = b.Last;
            editInfo.SetTextColor(DimColor);
            editInfo.SetPreferredSize(LeftW - 20f, 18f);

            b.BeginScroll(508f);
            editGridHost = b.CurrentContainer;
            b.End();

            b.AddLabel("");
            editStatus = b.Last;
            editStatus.SetTextColor(StatusColor);
            editStatus.SetPreferredSize(LeftW - 20f, 36f);
            b.End();

            b.BeginPanel(RightW, PaneH, TextAnchor.UpperLeft, 8f, 4f);
            // 表单区不套滚动：内容多高就多高，三个按钮永远在最下面露着
            b.BeginColumn();
            editFormHost = b.CurrentContainer;
            if (editFormHost != null)
            {
                LayoutElement formLayout = editFormHost.GetComponent<LayoutElement>();
                if (formLayout == null)
                {
                    formLayout = editFormHost.gameObject.AddComponent<LayoutElement>();
                }
                formLayout.flexibleWidth = 1f;
                formLayout.flexibleHeight = 0f;
            }
            b.End();
            b.BeginScrollFlexible(200f);
            editSpecScroll = b.Last;
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
            RectTransform cellRoot = b.CurrentContainer;

            b.AddImage((Sprite)null, 56f, 56f);
            CustomUIElement image = b.Last;
            image.SetImageColor(selected ? Color.white : IconDim);
            if (image.image != null)
            {
                image.image.raycastTarget = true;
                image.image.preserveAspect = true;
            }
            image.SetTooltip(tooltip);
            if (image.root != null)
            {
                CellClickHandler handler = image.root.AddComponent<CellClickHandler>();
                handler.onLeftClick = onClick;
            }

            b.AddLabel(label ?? "");
            CustomUIElement text = b.Last;
            StyleCellLabel(text);
            text.SetTooltip(tooltip);
            if (selected)
            {
                text.SetTextColor(CellSelected);
            }

            b.End();

            AddCellGlow(cellRoot, selected);

            if (cells != null)
            {
                CellRef cell = new CellRef();
                cell.Id = tooltip;
                cell.Image = image;
                cell.Label = text;
                cells.Add(cell);
            }
        }

        /// <summary>格子底衬：贴着图标的方块，未选中是淡淡一层，选中是金色光晕。只衬在图标后面，不遮挡图片本身。</summary>
        private static void AddCellGlow(RectTransform cell, bool selected)
        {
            if (cell == null)
            {
                return;
            }

            GameObject go = new GameObject("ds_cell_glow", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(cell, false);
            rt.SetSiblingIndex(0);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            float size = selected ? 64f : 60f;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(0f, selected ? -2f : -4f);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            Image img = go.GetComponent<Image>();
            img.color = selected ? CellGlow : CellNormal;
            img.raycastTarget = false;
        }

        /// <summary>文字自动适配：长标签自动换行并在 9~原字号之间缩小，避免撑破右栏。</summary>
        private static void FitTexts(RectTransform host)
        {
            if (host == null)
            {
                return;
            }
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

        private static void StyleCellLabel(CustomUIElement el)
        {
            if (el == null || !el.IsAlive)
            {
                return;
            }
            if (el.text != null)
            {
                el.text.alignment = TextAlignmentOptions.Center;
                el.text.enableWordWrapping = false;
                el.text.fontSizeMax = 14f;
                el.text.fontSizeMin = 1f;
                el.text.enableAutoSizing = true;
            }
            el.SetPreferredSize(CellW - 10f, 20f);
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

                // 图标探针顺带抓到游戏原文描述，挪上去就能看到 id 下面那段说明
                string tip = TileTooltip(cell.Id);
                if (cell.Label != null && cell.Label.IsAlive)
                {
                    cell.Label.SetTooltip(tip);
                }
                if (cell.Image != null && cell.Image.IsAlive)
                {
                    cell.Image.SetTooltip(tip);
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
    internal sealed class CellClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public Action onLeftClick;
        public Action onRightClick;

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
    }
}
