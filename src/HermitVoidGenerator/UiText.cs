namespace HermitVoidGenerator
{
    /// <summary>所有界面文案集中在这里，方便以后换语言/改字。</summary>
    internal static class UiText
    {
        public const string Title = "Hermit的虚空生成器";

        public const string TabBasic = "基础修改";
        public const string TabCreate = "物品添加";
        public const string TabEdit = "物品编辑";

        // ---- 通用
        public const string Ok = "好了";
        public const string Footer = "Insert 开关面板 · 生成时先点左边的物品，再在右边填参数 · 规格改动即时生效";

        // ---- 左栏（目录 / 台面）
        public const string SearchHint = "输入物品 ID 或名称筛选…";
        public const string RefreshCatalog = "刷新目录";
        public const string EmptyList = "没有匹配的物品。换个关键词，或者把大/小分类切回全部。";
        public const string GroupAll = "全部小类";
        public const string SectionAll = "全部大类";
        public const string ListHint = "左键点一件物品 → 右侧填生成参数";

        // ---- 生成页右栏
        public const string DraftNone = "先在左边点一件物品。\n这里会显示它的数量、落点和全部规格。";
        public const string DraftTitle = "生成参数";
        public const string CountLabel = "数量";
        public const string PriceLabel = "单价（留空=默认）";
        public const string WhereLabel = "生成到哪里";
        public const string SpawnButton = "生成";
        public const string DraftHint = "规格会写进生成出来的那一件。";

        // ---- 规格区
        public const string SpecTitle = "规格";
        public const string SpecNone = "这件物品没有可改的规格（不是液体容器、化学制品或金属锭）。";
        public const string SpecLiquidHint = "成分（毫升）";
        public const string SpecNoLiquid = "（目前是空的）";
        public const string SpecVolume = "总量";
        public const string SpecCapacity = "容量";
        public const string SpecAddLiquid = "添加 / 修改成分";
        public const string SpecMlPlaceholder = "毫升";
        public const string SpecSet = "设定";
        public const string SpecClear = "清零";
        public const string SpecClearAll = "清空全部成分";
        public const string SpecFillWater = "灌满清水";
        public const string SpecWaterPurity = "水纯度（水占液体总量的百分比）";
        public const string SpecPurityPlaceholder = "例如 98";
        public const string SpecApplyWaterPurity = "改水纯度";
        public const string SpecChemPurity = "制品纯度（主成分占比）";
        public const string SpecApplyChemPurity = "改制品纯度";
        public const string SpecActive = "主成分";
        public const string SpecApplyActive = "换主成分";
        public const string SpecIngot = "金属纯度";
        public const string SpecApplyIngot = "改金属纯度";
        public const string SpecNotContainer = "不是液体容器，改不了水纯度";
        public const string SpecNotChemProduct = "不是化学制品，没有制品纯度";

        // ---- 配给券
        public const string StampTitle = "配给券";
        public const string StampHint = "面值决定在配给站能换多少额度；假券带一处对不上的防伪特征，被鉴定或兑换时当场揭穿。真伪都走游戏自己的生成逻辑。";
        public const string StampFace = "面值";
        public const string StampWrite = "写入";

        // ---- 编辑页
        public const string PickButton = "点选物品";
        public const string IncludeStore = "含店内库存";
        public const string PickHint = "左键点选桌面上的物品 · 右键 / Esc 取消";
        public const string RefreshStore = "刷新台面";
        public const string NoTarget = "未选中物品 —— 在左边点一件，或者用「点选物品」在世界里点。";
        public const string NoStoreItems = "（前台、后柜台、展示柜上都没有东西）";
        public const string EditSearchHint = "在台面物品里按 ID / 名称筛…";
        public const string EditEmptyList = "台面上没有匹配的物品。换个关键词，或者把大/小分类切回全部。";
        public const string EditListHint = "左键点一件 → 右侧改它";
        public const string EmptyItemTitle = "还没有选中物品";
        public const string EditTitle = "物品信息";
        public const string BlankClears = "空白字段=清空";
        public const string Apply = "应用修改";
        public const string Reload = "重新载入";
        public const string Duplicate = "复制一件";
        public const string ClearCustomName = "清除自定义名";

        public const string FieldName = "名称";
        public const string FieldCustomName = "自定义名（显示在物品上）";
        public const string FieldCount = "数量";
        public const string FieldUnitValue = "单价";
        public const string FieldBaseValue = "基础价";
        public const string FieldLateValue = "迟价";
        public const string FieldShortDesc = "短描述";
        public const string FieldCustomText = "自定义文本";
        public const string FieldLongDesc = "长描述";
        public const string FieldFlavor = "风味文本";

        // ---- 耐久
        public const string TagTitle = "物品词条";
        public const string TagHint = "这件物品身上带着的所有词条。改动点「写入」立刻生效。";
        public const string TagNone = "这件物品身上没有可编辑的词条";
        public const string TagUnrated = "另有这些词条类型未知，只能看不能改：";
        public const string TagWrite = "写入";
        public const string DurTitle = "耐久";
        public const string DurCurrent = "当前";
        public const string DurMax = "上限";
        public const string DurFake = "假耐久";
        public const string DurApply = "写入";
        public const string DurFill = "满耐久";
        public const string DurNone = "这件物品没有耐久标签";
        public const string DurInit = "加耐久";

        // ---- 升级等级
        public const string UpTitle = "升级等级";
        public const string UpLevel = "等级";
        public const string UpApply = "设定等级";
        public const string UpNone = "这件物品不能升级";
        public const string UpRasWidth = "RAS 储罐宽度";
        public const string UpRasHeight = "RAS 储罐高度";
        public const string UpRasPower = "RAS 省电";
        public const string UpFridgeStorage = "冰箱容积";
        public const string UpFridgePower = "冰箱省电";
        public const string UpPrinter = "制瓶机";
        public const string UpConverter = "电流转换器";
        public const string UpBattery = "高科电池输出";

        // ---- 种子基因
        public const string GeneTitle = "种子基因";
        public const string GenePlant = "品种";
        public const string GenePure = "原种";
        public const string GeneModified = "已变异";
        public const string GeneComplexity = "复杂度";
        public const string GeneHeat = "基因热量";
        public const string GeneEffective = "有效复杂度";
        public const string GeneHint = "等级 0 = 去掉这个性状；填几级就是几级，加成按等级折算。带「受限」的性状游戏本体不让加，强加会直接写进槽里。";
        public const string GeneOwned = "已有性状";
        public const string GeneOwnedNone = "（身上没有任何性状，可能不是正常长出来的种子）";
        public const string GeneMissing = "未拥有性状";
        public const string GeneBlockedMark = "（受限）";
        public const string GeneWrite = "写入";
        public const string GeneAdd = "加入";
        public const string GeneForce = "强加";
        public const string GeneReset = "恢复原种";
        public const string GeneClearHeat = "清空热量";
        public const string GeneRankPlaceholder = "等级";
        public const string GeneLimitUnlimited = "游戏内无等级上限";
        public const string GeneRestricted = "受限原因：";

        // ---- 基础修改页
        public const string BasicMoney = "钱与账目";
        public const string BasicCash = "现金";
        public const string BasicRent = "房租";
        public const string BasicLoan = "贷款";
        public const string BasicDaysLeft = "剩余天数";
        public const string BasicWrite = "写入";
        public const string BasicStore = "店铺";
        public const string BasicAttr = "吸引力";
        public const string BasicProjector = "投影加成";
        public const string BasicPricing = "定价加成（百分比）";
        public const string BasicRetail = "零售加价";
        public const string BasicContraLow = "违禁加价·低";
        public const string BasicContraMid = "违禁加价·中";
        public const string BasicContraHigh = "违禁加价·高";
        public const string BasicContraCrit = "违禁加价·极高";
        public const string BasicRelation = "恩惠与配给";
        public const string BasicFavor = "恩惠";
        public const string BasicCommissary = "配给券余额";
        public const string BasicFactions = "阵营声望";
        public const string BasicFactionNone = "还没有载入存档，读不到阵营列表。";
        public const string BasicHealth = "伤势";
        public const string BasicWoundStable = "伤势稳定（夜里愈合，不打稳会恶化）";
        public const string BasicRefresh = "刷新数值";
        public const string BasicHint = "这些是存档里的全局数值，写入立刻生效。声望范围 -200 到 200，改完立刻按游戏自己的档位重算阵营特权（每个阵营的 perk 按阈值解锁/锁回）。伤势 0 到 10，5 以上算重伤：重伤会掉行动能力，稳定状态决定夜里是愈合还是继续恶化。";

        public static readonly string[] SpawnTargets = { "店内库存", "前台柜台", "展示柜", "后柜台", "自动安置" };
    }
}
