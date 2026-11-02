using System;
using System.Collections.Generic;
using System.Globalization;

namespace HermitVoidGenerator
{
    /// <summary>一条可编辑的物品词条（game tag）。</summary>
    internal sealed class TagRow
    {
        public string Tag;
        public string Label;
        public string Group;
        public char Kind;
        public string Display;
    }

    /// <summary>
    /// 物品词条总表。类型来自 TagTypes（扫描反编译源码的读取点生成），
    /// 中文名 / 分组是人工标注的常用项，其余一律用原始 tag 名落到「其它」组。
    /// 只写值槽、不改游戏判定逻辑：别的地方该重算的（枪械当前值、电池图标）在这里补。
    /// </summary>
    internal static class Tags
    {
        public const string GroupEnergy = "能源";
        public const string GroupUse = "使用次数";
        public const string GroupGun = "武器";
        public const string GroupArmor = "护甲";
        public const string GroupFood = "食物";
        public const string GroupLife = "生物";
        public const string GroupStore = "存储";
        public const string GroupTrade = "归属与交易";
        public const string GroupOther = "其它";

        private static readonly string[] GroupOrder =
        {
            GroupEnergy, GroupUse, GroupGun, GroupArmor, GroupFood, GroupLife, GroupStore, GroupTrade, GroupOther
        };

        private static Dictionary<string, string[]> named;

        private static Dictionary<string, string[]> Named()
        {
            if (named != null)
            {
                return named;
            }

            Dictionary<string, string[]> d = new Dictionary<string, string[]>(StringComparer.Ordinal);

            Add(d, GroupEnergy, "power_source_item", "是电源");
            Add(d, GroupEnergy, "power_source_item_energy", "剩余储电");
            Add(d, GroupEnergy, "power_source_item_max_energy", "储电上限");
            Add(d, GroupEnergy, "power_source_item_bandwidth", "供能带宽");
            Add(d, GroupEnergy, "power_source_item_recharge", "自充速率");
            Add(d, GroupEnergy, "CURRENT_CHARGE_INT", "当前电量");
            Add(d, GroupEnergy, "MAX_CHARGE_INT", "电量上限");
            Add(d, GroupEnergy, "CHARGE_RATE_INT", "充电速率");
            Add(d, GroupEnergy, "HYPERTECH_CHARGE_RATE_TAG", "高科充电速率");
            Add(d, GroupEnergy, "POWER_SOURCE_ITEM_VALUE_PERCENT_INT", "剩余电量 %");
            Add(d, GroupEnergy, "POWER_USE_INT", "功耗");
            Add(d, GroupEnergy, "POWER_USE_BASE_INT", "基础功耗");
            Add(d, GroupEnergy, "MIND_REPROGRAMMER_CHARGE_INT", "精神重编器充能");
            Add(d, GroupEnergy, "REACTOR_INTEGRITY_INT", "反应堆完整度");
            Add(d, GroupEnergy, "REACTOR_MAX_INTEGRITY_INT", "反应堆完整度上限");
            Add(d, GroupEnergy, "REACTOR_UPGRADES_INT", "反应堆升级数");
            Add(d, GroupEnergy, "REACTOR_WASTE_PROGRESS_INT", "反应堆废料进度");
            Add(d, GroupEnergy, "LOW_POWER_MODE_BOOL", "低功耗模式");
            Add(d, GroupEnergy, "POWER_SWITCH_BOOL", "电源开关");
            Add(d, GroupEnergy, "is_machinery_powered", "已接电");
            Add(d, GroupEnergy, "machinery_drawn_power", "正在取电");
            Add(d, GroupEnergy, "machinery_received_power", "正在供电");

            Add(d, GroupUse, "CURRENT_USE_COUNT_INT", "已用次数");
            Add(d, GroupUse, "MAX_USE_COUNT_INT", "可用次数上限");
            Add(d, GroupUse, "USE_COUNT_VALUE_PER_USE_INT", "每次消耗");
            Add(d, GroupUse, "USE_COUNT_BASE_VALUE_INT", "基础值");
            Add(d, GroupUse, "USE_COUNT_REFILL_DAYS_INT", "补充间隔（天）");
            Add(d, GroupUse, "USE_COUNT_REFILL_PROGRESS_INT", "补充进度");
            Add(d, GroupUse, "USE_COUNT_DESTROY_ON_EMPTY_BOOL", "用完即毁");
            Add(d, GroupUse, "USE_COUNT_TRANSFERABLE_BOOL", "可转移");

            Add(d, GroupGun, "GUN_FIREPOWER_TAG", "火力");
            Add(d, GroupGun, "GUN_ACCURACY_TAG", "精度");
            Add(d, GroupGun, "GUN_RECOIL_TAG", "后坐力");
            Add(d, GroupGun, "GUN_RELIABILITY_TAG", "可靠性");
            Add(d, GroupGun, "GUN_ERGONOMICS_TAG", "人机工效");
            Add(d, GroupGun, "GUN_STYLE_TAG", "美观");
            Add(d, GroupGun, "GUN_REPAIRBILITY_TAG", "可维修度");
            Add(d, GroupGun, "GUN_CURRENT_REPAIRBILITY_TAG", "已维修量");
            Add(d, GroupGun, "GUN_CALIBER_TAG", "口径");
            Add(d, GroupGun, "GUN_SUPPRESSED_TAG", "消音等级");

            Add(d, GroupArmor, "armor_protection", "护甲值");
            Add(d, GroupArmor, "armor_mitigation", "减伤");
            Add(d, GroupArmor, "armor_resilience", "韧性");
            Add(d, GroupArmor, "armor_efficiency", "效率");
            Add(d, GroupArmor, "armor_accuracy_modifier", "精度修正");
            Add(d, GroupArmor, "armor_dodge_modifier", "闪避修正");
            Add(d, GroupArmor, "current_armor_value", "当前护甲");
            Add(d, GroupArmor, "current_max_armor_value", "护甲上限");
            Add(d, GroupArmor, "default_armor_value", "默认护甲");

            Add(d, GroupFood, "FRESHNESS_TAG", "新鲜度");
            Add(d, GroupFood, "CALORIE_VALUE_TAG", "卡路里");
            Add(d, GroupFood, "STORED_KCAL_TAG", "储存千卡");
            Add(d, GroupFood, "cooked", "已煮熟");

            Add(d, GroupLife, "ANIMAL_HEALTH_TAG", "动物健康");
            Add(d, GroupLife, "ANIMAL_HUNGER_TAG", "动物饥饿");
            Add(d, GroupLife, "ANIMAL_IS_DEAD_TAG", "已死亡");
            Add(d, GroupLife, "ANIMAL_PREGNANT_TAG", "怀孕中");
            Add(d, GroupLife, "ANIMAL_PREGNANT_PROGRESS_TAG", "孕程");
            Add(d, GroupLife, "ANIMAL_DISEASED_TAG", "患病");
            Add(d, GroupLife, "ANIMAL_NEUTERED_TAG", "已绝育");
            Add(d, GroupLife, "FISH_HEALTH_TAG", "鱼健康");
            Add(d, GroupLife, "FISH_HUNGER_TAG", "鱼饥饿");
            Add(d, GroupLife, "FISH_IS_DEAD_TAG", "鱼已死");
            Add(d, GroupLife, "ORGAN_HEALTH_INT", "器官健康");
            Add(d, GroupLife, "ORGAN_HEALTH_MAX_INT", "器官健康上限");
            Add(d, GroupLife, "PLANT_HEALTH_INT", "植株健康");
            Add(d, GroupLife, "PLANT_TASTE_PERCENT_INT", "风味 %");

            Add(d, GroupStore, "TOOLBOX_UNLOCKED_SLOTS_INT", "工具箱解锁槽");
            Add(d, GroupStore, "STORAGE_EXPANSION_TABS_INT", "收纳页数");
            Add(d, GroupStore, "current_storage", "当前存储");
            Add(d, GroupStore, "max_storage", "存储上限");

            Add(d, GroupTrade, "IS_OWNED_TAG", "已拥有");
            Add(d, GroupTrade, "STOLEN_TAG", "赃物");
            Add(d, GroupTrade, "STOLEN_VALUE_INT", "赃物估值");
            Add(d, GroupTrade, "BOUGHT_PRICE_TAG", "入手价");
            Add(d, GroupTrade, "ITEM_HIDDEN_TAG", "隐藏");
            Add(d, GroupTrade, "ITEM_BEHIND_LOCK_TAG", "锁在柜后");
            Add(d, GroupTrade, "LOCKED_TAG", "上锁");
            Add(d, GroupTrade, "PINNED_TAG", "已钉住");
            Add(d, GroupTrade, "CONTRABAND_ITEM_TAG", "违禁品");
            Add(d, GroupTrade, "LOTTERY_NUMBER_TAG", "彩票号码");
            Add(d, GroupTrade, "WINE_SCORE_INT", "酒评分");
            Add(d, GroupTrade, "WINE_COUNTERFEIT_TAG", "假酒");

            named = d;
            return named;
        }

        private static void Add(Dictionary<string, string[]> d, string group, string tag, string label)
        {
            d[tag] = new[] { group, label };
        }

        /// <summary>这件物品身上带着的所有词条，按分组排好序。</summary>
        public static List<TagRow> Rows(GameItem item)
        {
            List<TagRow> rows = new List<TagRow>();
            if (item == null)
            {
                return rows;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string tag in Keys(item))
            {
                if (string.IsNullOrEmpty(tag) || !seen.Add(tag))
                {
                    continue;
                }

                char kind = TagTypes.Kind(tag);
                if (kind == '\0')
                {
                    continue;
                }

                TagRow row = new TagRow();
                row.Tag = tag;
                row.Kind = kind;
                row.Group = GroupOther;
                row.Label = tag;

                string[] meta;
                if (Named().TryGetValue(tag, out meta))
                {
                    row.Group = meta[0];
                    row.Label = meta[1];
                }
                else
                {
                    string zh = TagLabels.Name(tag);
                    if (!string.IsNullOrEmpty(zh))
                    {
                        row.Label = zh;
                    }

                    row.Group = GroupOf(tag);
                }

                row.Display = Read(item, row);
                rows.Add(row);
            }

            rows.Sort(delegate(TagRow a, TagRow b)
            {
                int ga = Array.IndexOf(GroupOrder, a.Group);
                int gb = Array.IndexOf(GroupOrder, b.Group);
                if (ga != gb)
                {
                    return ga.CompareTo(gb);
                }
                return string.CompareOrdinal(a.Label, b.Label);
            });

            return rows;
        }

        /// <summary>没人工标注过的词条按前缀猜分组，让「其它」不至于变成一锅粥。</summary>
        private static readonly string[][] GroupPrefixes =
        {
            new[] { GroupLife, "ANIMAL_", "FISH_", "ORGAN_", "PLANT_", "EGG_", "MEAT_", "CARCASS", "SPECIES", "TRAIT_" },
            new[] { GroupGun, "GUN_", "AMMO", "MELEE", "WEAPON", "BULLET", "SUPPRESS" },
            new[] { GroupArmor, "armor", "ARMOR", "CONDITION_REINFORCED" },
            new[] { GroupFood, "FOOD", "KCAL", "CALORIE", "FRESH", "WINE", "cooked", "COOKED", "RIBWICH", "CIGARETTE", "DRINK", "BREVAGE", "ALCOHOL" },
            new[] { GroupEnergy, "POWER", "power", "ENERGY", "CHARGE", "BATTERY", "REACTOR", "PLASMA", "HYPERTECH_CHARGE", "MIND_REPROGRAMMER_CHARGE" },
            new[] { GroupUse, "USE_COUNT", "CURRENT_USE", "MAX_USE" },
            new[] { GroupStore, "CONTAINER", "STORAGE", "INVENTORY", "BOX", "CRATE", "BACKPACK", "HOPPER", "TANK_" },
            new[] { GroupTrade, "IS_OWNED", "OWNED", "STOLEN", "PRICE", "BOUGHT", "TRADER", "LOTTERY", "CONTRABAND", "ILLEGAL", "HIDDEN", "PINNED", "BLACKLISTED", "CUSTOM", "IMPORTANT", "ENDANGERMENT", "TAINTED", "COUNTERFEIT" },
        };

        private static string GroupOf(string tag)
        {
            for (int g = 0; g < GroupPrefixes.Length; g++)
            {
                string[] row = GroupPrefixes[g];
                for (int i = 1; i < row.Length; i++)
                {
                    if (tag.StartsWith(row[i], StringComparison.Ordinal))
                    {
                        return row[0];
                    }
                }
            }

            return GroupOther;
        }

        /// <summary>
        /// 物品身上带着、但类型未知（扫描时没读到过）的词条，只读展示。
        /// PLANT_TRAIT_* 不算在内：那是种子基因，归「种子基因」区管。
        /// </summary>
        public static List<string> Unrated(GameItem item)
        {
            List<string> list = new List<string>();
            foreach (string tag in Keys(item))
            {
                if (string.IsNullOrEmpty(tag) || TagTypes.Kind(tag) != '\0')
                {
                    continue;
                }
                if (tag.StartsWith(Genes.TraitTagPrefix, StringComparison.Ordinal))
                {
                    continue;
                }
                list.Add(tag);
            }
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static IEnumerable<string> Keys(GameItem item)
        {
            List<string> keys = new List<string>();
            try
            {
                if (item.state != null && item.state.dict != null)
                {
                    foreach (string key in item.state.dict.Keys)
                    {
                        keys.Add(key);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("读取词条表失败: " + ex.Message);
            }
            return keys;
        }

        public static string Read(GameItem item, TagRow row)
        {
            try
            {
                switch (row.Kind)
                {
                    case 'i':
                        return item.GetTagReadonly(row.Tag).GetInt().ToString(CultureInfo.InvariantCulture);
                    case 'f':
                        return item.GetTagReadonly(row.Tag).GetFloat().ToString("0.###", CultureInfo.InvariantCulture);
                    case 'd':
                        return item.GetTagReadonly(row.Tag).GetDouble().ToString("0.###", CultureInfo.InvariantCulture);
                    case 's':
                        return item.GetTagReadonly(row.Tag).GetString() ?? "";
                    case 'b':
                        return item.IsTag(row.Tag) ? "有" : "无";
                }
            }
            catch (Exception ex)
            {
                Log.Warn("读词条 " + row.Tag + " 失败: " + ex.Message);
            }
            return "";
        }

        /// <summary>写一条词条，返回错误文案；成功返回 null。</summary>
        public static string Write(GameItem item, TagRow row, string text)
        {
            if (item == null || row == null)
            {
                return "没有目标物品";
            }

            text = (text ?? "").Trim();

            try
            {
                switch (row.Kind)
                {
                    case 'i':
                    {
                        string error = WriteInt(item, row.Tag, text);
                        if (error != null)
                        {
                            return error;
                        }
                        break;
                    }
                    case 'f':
                    case 'd':
                    {
                        double value;
                        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                            && !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                        {
                            return "要填数字";
                        }
                        float f = (float)value;
                        if (row.Kind == 'f')
                        {
                            item.ModifyTag(row.Tag, delegate(TagState s) { s.Enable().SetFloat(f); });
                        }
                        else
                        {
                            item.ModifyTag(row.Tag, delegate(TagState s) { s.Enable().SetDouble(value); });
                        }
                        break;
                    }
                    case 's':
                        item.ModifyTag(row.Tag, delegate(TagState s) { s.Enable().SetString(text); });
                        break;
                    case 'b':
                    {
                        bool on = IsTruthy(text);
                        if (on)
                        {
                            item.EnableTag(row.Tag);
                        }
                        else
                        {
                            item.DisableTag(row.Tag);
                        }
                        return null;
                    }
                    default:
                        return "这条词条看不懂，改不了";
                }

                AfterWrite(item, row.Tag);
                item.Validate();
                return null;
            }
            catch (Exception ex)
            {
                Log.Warn("写词条 " + row.Tag + " 失败: " + ex.Message);
                return "写入失败：" + ex.Message;
            }
        }

        private static string WriteInt(GameItem item, string tag, string text)
        {
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                double d;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                {
                    return "要填整数";
                }
                value = (int)Math.Round(d);
            }

            // 电池余额有专门的写入入口，顺手会把图标刷新成对的那张。
            if (tag == "power_source_item_energy")
            {
                int max = item.GetTagReadonly("power_source_item_max_energy").GetInt();
                if (max > 0 && value > max)
                {
                    value = max;
                }
                if (value < 0)
                {
                    value = 0;
                }
                PowerHelper.SetPowerSourceAt(item, value);
                return null;
            }

            int exact = value;
            item.ModifyTag(tag, delegate(TagState s) { s.Enable().SetInt(exact); });
            return null;
        }

        private static void AfterWrite(GameItem item, string tag)
        {
            try
            {
                if (tag.StartsWith("GUN_", StringComparison.Ordinal) && item.IsTag("ADV_GUN_TAG"))
                {
                    GunHelper.UpdateCurrentGunStats(item);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("词条写后重算失败: " + ex.Message);
            }
        }

        private static bool IsTruthy(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            switch (text.ToLowerInvariant())
            {
                case "1":
                case "y":
                case "yes":
                case "true":
                case "on":
                case "有":
                case "开":
                case "是":
                    return true;
                default:
                    return false;
            }
        }
    }
}
