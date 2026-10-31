using System;
using System.Text;

namespace HermitVoidGenerator
{
    /// <summary>词条悬停说明：这个词条做什么、改了会怎样、能填多少。</summary>
    internal static class TagDoc
    {
        private static readonly string[,] Meaning =
        {
            { "CURRENT_PART_", "某一种液体的含量（单位：微升，1000 微升 = 1 毫升）。改它等于改配方占比。" },
            { "PART_", "液体配方的内部标记，跟着 CURRENT_PART_ 一起用。" },
            { "DURABILITY_FAKE_TAG", "假耐久：摆在台面上给客人看的数字。跟真耐久差太多会被鉴定出来。" },
            { "MAX_DURABILITY_FAKE_TAG", "假耐久的上限，跟着假耐久一起改。" },
            { "MAX_DURABILITY_TAG", "耐久上限。先改上限再改当前耐久，否则当前值会被夹回去。" },
            { "DEFAULT_DURABILITY_TAG", "出厂耐久。用来算「磨损程度」对价格的影响，必须不小于耐久上限。" },
            { "DURABILITY_TAG", "当前耐久。改小会掉价，改大更值钱；上限由 MAX_DURABILITY_TAG 决定。" },
            { "DURABILITY_ZERO_VALUE_TAG", "耐久归零时这件物品还剩多少价值。" },
            { "DURABILITY_PRICING_MODE_TAG", "耐久定价模式：1 = 按比例折价，2 = 跌破零值才折价。" },
            { "GUN_CURRENT_", "派生值：由基础值 × 配件加成算出来，游戏重算时会被覆盖。改基础值更稳。" },
            { "GUN_", "枪械基础数值。写入后配件加成会重新算进 CURRENT_* 系列。" },
            { "POWER_SOURCE_ITEM_MAX_ENERGY", "电源的电量上限。先定上限，再改当前电量。" },
            { "POWER_SOURCE_ITEM_ENERGY", "电源的当前电量，写入时会夹到电量上限以内。" },
            { "POWER_SOURCE_ITEM_BANDWIDTH", "电源能同时带动的设备量（供电带宽）。" },
            { "POWER_SOURCE_ITEM_RECHARGE", "每次充电恢复的电量。" },
            { "POWER_SOURCE_ITEM", "是不是一件电源。" },
            { "LIQUID_CONTAINER_CAPACITY", "液体容器的容量（单位：微升，1000 微升 = 1 毫升）。改小会把多余液体挤掉。" },
            { "LIQUID_CONTAINER_TAG", "是不是液体容器。" },
            { "CHEMICAL_PRODUCT_PURITY_TAG", "制品纯度（百分比）。写入后会按阈值重算等级：≥99 实验室级、≥90 制药级、≥80 近制药级、≥60 街头级、≥45 掺过、≥30 次品、更低是淀粉货。" },
            { "ACTIVATE_INGREDIENT_TAG", "主成分：填液体 id（例如 water、caffeine）。纯度是按它占总体积的比例算的。" },
            { "CHEMICAL_PRODUCT_TAG", "是不是化学制品。" },
            { "CHEM_ITEM_TAG", "是不是化学品容器。没有它，液体总量会被算成 0，纯度会算错。" },
            { "BLENDABLE_TAG", "能不能被搅拌机混合。" },
            { "SOLUBLE_TAG", "能不能溶解。" },
            { "INGOT_PURITY_FAKE_TAG", "金属锭的假纯度，给客人看的档位。" },
            { "INGOT_PURITY_TAG", "金属锭纯度档位：perfect / very_high / high / fair / low / very_low。" },
            { "IS_OWNED_TAG", "是不是己方所有。关掉就等于来源不明，容易被当成赃物。" },
            { "STOLEN", "赃物标记。被查到时会影响交易。" },
            { "CONTRABAND", "违禁品标记。" },
            { "ILLEGAL", "非法物品标记。" },
            { "CUSTOM_NAME_TAG", "自定义名。填文字就显示这个名字，留空等于没有。" },
            { "TRADER_PRICE", "收购 / 出售时的价格倍率。" },
            { "PRICE", "价格相关词条。" },
            { "VALUE", "价值相关词条。" },
            { "KCAL", "热量（食物类）。" },
            { "FRESH", "新鲜度（食物类）。掉到底会变质。" },
            { "ANIMAL_", "养殖动物的属性。" },
            { "USE_COUNT", "使用次数相关。" },
            { "CURRENT_USE", "当前已用次数。" },
            { "MAX_USE", "最多能用几次。" },
            { "UPGRADE_COUNT", "升级等级。填几就是几级，不能超过上限；改完机器尺寸 / 耗电会跟着重算。" },
        };

        private static readonly string[,] Range =
        {
            { "DURABILITY_TAG", "范围：0 ~ 耐久上限" },
            { "MAX_DURABILITY_TAG", "范围：1 ~ 1000000" },
            { "DURABILITY_FAKE_TAG", "范围：0 ~ 假耐久上限" },
            { "CHEMICAL_PRODUCT_PURITY_TAG", "范围：0 ~ 100（百分比）" },
            { "LIQUID_CONTAINER_CAPACITY", "范围：0 ~ 100000000（微升）" },
            { "CURRENT_PART_", "范围：0 ~ 容器容量（微升）" },
            { "_UPGRADE_COUNT", "范围：0 ~ 该升级线的上限" },
            { "POWER_SOURCE_ITEM_ENERGY", "范围：0 ~ 电量上限" },
        };

        public static string Info(TagRow row)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(row.Tag);
            sb.Append('\n').Append("类型：").Append(KindName(row.Kind));
            if (!string.IsNullOrEmpty(row.Group))
            {
                sb.Append(" · ").Append(row.Group);
            }

            string meaning = Match(Meaning, row.Tag);
            if (meaning == null)
            {
                meaning = KindMeaning(row.Kind);
            }
            if (!string.IsNullOrEmpty(meaning))
            {
                sb.Append('\n').Append(meaning);
            }

            string range = Match(Range, row.Tag);
            if (!string.IsNullOrEmpty(range))
            {
                sb.Append('\n').Append(range);
            }

            return sb.ToString();
        }

        public static string KindName(char kind)
        {
            switch (kind)
            {
                case 'b': return "开关";
                case 'i': return "整数";
                case 'f': return "小数";
                case 'd': return "小数（双精度）";
                case 's': return "文本";
                default: return "未识别";
            }
        }

        private static string KindMeaning(char kind)
        {
            switch (kind)
            {
                case 'b': return "布尔开关。打开 = 这件物品带上这个状态，关掉 = 去掉。";
                case 'i': return "整数词条。直接改写这件物品上的数值，写错了再填回原值即可。";
                case 'f':
                case 'd': return "小数值词条。可以带小数点，直接改写。";
                case 's': return "文本词条。填字符串，通常填 id 或名字。";
                default: return "游戏里没读它的值，只能看不能改。";
            }
        }

        /// <summary>前缀表按顺序匹配，第一个命中的就用。</summary>
        private static string Match(string[,] table, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return null;
            }

            for (int i = 0; i < table.GetLength(0); i++)
            {
                string key = table[i, 0];
                bool hit;
                if (key.StartsWith("_", StringComparison.Ordinal))
                {
                    hit = tag.EndsWith(key, StringComparison.Ordinal) || tag.IndexOf(key, StringComparison.Ordinal) >= 0;
                }
                else
                {
                    hit = tag.StartsWith(key, StringComparison.Ordinal) || tag.IndexOf(key, StringComparison.Ordinal) >= 0;
                }
                if (hit)
                {
                    return table[i, 1];
                }
            }
            return null;
        }
    }
}
