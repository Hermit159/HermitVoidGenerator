using System;
using System.Text;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 词条悬停说明。第一行永远是游戏原来的英文 tag，第二行说"这里怎么填"（填整数 / 能带小数 /
    /// 点开关就行 / 改不了），后面说这个词条管什么事、改了会怎样。
    /// 不写"类型：整型"这种只有写代码的人看得懂的话。
    /// </summary>
    internal static class TagDoc
    {
        private static readonly string[,] Meaning =
        {
            { "CURRENT_PART_", "某一种液体的含量（单位：微升，1000 微升 = 1 毫升）。改它等于改配方占比 —— 水里掺多少、酒精留多少，全看这几个数。" },
            { "PART_", "液体配方的内部标记，跟着 CURRENT_PART_ 一起用；单独改它没用。" },
            { "DURABILITY_FAKE_TAG", "假耐久：摆在台面上给客人看的数字。跟真耐久差太多，客人会看出猫腻。" },
            { "MAX_DURABILITY_FAKE_TAG", "假耐久的上限，跟着假耐久一起调，别比假耐久还小。" },
            { "MAX_DURABILITY_TAG", "耐久上限。先改上限再改当前耐久，否则当前值会被夹回去。" },
            { "DEFAULT_DURABILITY_TAG", "出厂耐久，用来算「磨损程度」对价格的影响。填得比耐久上限还小就会算成全新。" },
            { "DURABILITY_TAG", "当前耐久。改小会掉价甚至变成破烂，改大更值钱；能填到多高由耐久上限决定。" },
            { "DURABILITY_ZERO_VALUE_TAG", "耐久掉到 0 时这件东西还剩多少价值。填 0 = 用坏了一文不值。" },
            { "DURABILITY_PRICING_MODE_TAG", "耐久怎么折价：1 = 按剩余比例折，2 = 没跌破保底价值就不折。" },
            { "GUN_CURRENT_", "派生值：由基础值 × 配件加成算出来，游戏重算时会被覆盖。想改就改 GUN_ 开头的基础值。" },
            { "GUN_", "枪械基础数值。改完配件加成会重新算进 CURRENT_ 系列，所以只改这里就够。" },
            { "POWER_SOURCE_ITEM_MAX_ENERGY", "电源的电量上限。先定上限，再改当前电量，否则当前电量会被压回去。" },
            { "POWER_SOURCE_ITEM_ENERGY", "电源里还剩多少电。填超过上限会被夹到上限，填 0 就是没电。" },
            { "POWER_SOURCE_ITEM_BANDWIDTH", "这个电源能同时带动几台设备。填小了带不动的机器会断电。" },
            { "POWER_SOURCE_ITEM_RECHARGE", "每充一次电恢复多少。改大充一次就满。" },
            { "POWER_SOURCE_ITEM", "这件东西算不算电源。关掉以后它不再供电，充电面板也不认它。" },
            { "LIQUID_CONTAINER_CAPACITY", "能装多少液体（单位：微升，1000 微升 = 1 毫升）。改小会把多出来的液体挤掉。" },
            { "LIQUID_CONTAINER_TAG", "这件东西算不算液体容器。关掉以后往里倒液体计不上账，容量词条也失效。" },
            { "CHEMICAL_PRODUCT_PURITY_TAG", "制品纯度（百分比）。写入后按阈值重算等级：≥99 实验室级、≥90 制药级、≥80 近制药级、≥60 街头级、≥45 掺过、≥30 次品，更低就是淀粉货 —— 等级会直接改售价和客人反应。" },
            { "ACTIVATE_INGREDIENT_TAG", "主成分：填液体 id（例如 water、caffeine）。纯度就是按它占总体积的比例算的，填错纯度会算成 0。" },
            { "CHEMICAL_PRODUCT_TAG", "这件东西算不算化学制品。关掉以后纯度、等级都不再按化学制品算。" },
            { "CHEM_ITEM_TAG", "这件东西算不算化学品容器。没有它，液体总量会被算成 0，纯度跟着错。" },
            { "BLENDABLE_TAG", "能不能被搅拌机混合。关掉就搅不动。" },
            { "SOLUBLE_TAG", "能不能溶解。关掉就化不开。" },
            { "INGOT_PURITY_FAKE_TAG", "金属锭的假纯度档位，专门给客人看的。" },
            { "INGOT_PURITY_TAG", "金属锭真实纯度档位，填 perfect / very_high / high / fair / low / very_low 之一；档位越低卖得越便宜。" },
            { "IS_OWNED_TAG", "这件东西算不算自己的。关掉就变成来源不明，容易被当成赃物，也不好出手。" },
            { "STOLEN", "赃物标记。打开以后被查到会被收走，出手价也受影响。" },
            { "CONTRABAND", "违禁品标记。打开以后悬停标签里整行变红，被查到要没收。" },
            { "ILLEGAL", "非法物品标记。打开以后执法和交易规则都按非法品处理。" },
            { "ITEM_MEDICAL_TAG", "医疗品标记。打开以后它会被当医疗品：能当药使唤，悬停标签里也归进医疗品。" },
            { "FISH_FERTILIZED_TAG", "受精标记，鱼卵靠它才能孵。关掉这颗卵就孵不出鱼了。" },
            { "CUSTOM_NAME_TAG", "自定义名。填什么这物品就叫什么，留空就等于没改名。" },
            { "TRADER_PRICE", "价格倍率：1 = 按原价收，2 = 两倍价，0.5 = 半价。改它就直接改这件东西能卖多少钱。" },
            { "PRICE", "跟卖价有关的数值。改它会直接改这件东西值多少钱。" },
            { "VALUE", "跟价值有关的数值。改它会改这件东西折算成钱是多少。" },
            { "KCAL", "热量（食物类）。填大更顶饱，填 0 吃了不顶事。" },
            { "FRESH", "新鲜度（食物类）。掉到底就变质，吃了会出问题；改大等于重新变新鲜。" },
            { "ANIMAL_", "养殖动物的状态数值（年龄、产出、健康这些）。改完会影响它长得多快、产出多少。" },
            { "USE_COUNT", "使用次数相关。" },
            { "CURRENT_USE", "已经用掉几次。填到跟上限一样就是用完了。" },
            { "MAX_USE", "最多能用几次。改小会立刻变成已用超限（等于用坏）。" },
            { "UPGRADE_COUNT", "升级等级。填几就是几级，不能超过这条升级线的上限；改完机器尺寸、耗电会跟着重算。" },
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
            // 第一行永远是游戏原来的英文 tag，改不动
            sb.Append(row.Tag);
            sb.Append('\n').Append(HowToFill(row.Kind, Match(Range, row.Tag)));

            // 684 条词条各有一句写好的说明（TagDocs.g.cs），先查它；
            // 没登记的老词条再退前缀表，最后才用按类型的兜底话。
            string meaning = TagDocs.Of(row.Tag);
            if (string.IsNullOrEmpty(meaning))
            {
                meaning = Match(Meaning, row.Tag);
            }
            if (string.IsNullOrEmpty(meaning))
            {
                meaning = KindMeaning(row.Kind);
            }
            if (!string.IsNullOrEmpty(meaning))
            {
                sb.Append('\n').Append(meaning);
            }

            return sb.ToString();
        }

        /// <summary>怎么填：只讲用户往框里敲什么，不讲类型。</summary>
        private static string HowToFill(char kind, string range)
        {
            string head;
            switch (kind)
            {
                case 'b':
                    head = "点开关就行，不用填";
                    break;
                case 'i':
                    head = "填整数，不要小数点";
                    break;
                case 'f':
                case 'd':
                    head = "填数字，可以带小数";
                    break;
                case 's':
                    head = "填文字（一般是 id 或名字）";
                    break;
                default:
                    head = "这里改不了，只能看";
                    break;
            }

            if (string.IsNullOrEmpty(range))
            {
                return head;
            }
            return head + "；" + range.Replace("范围：", "取值 ");
        }

        /// <summary>没专门写说明的词条，按怎么填给一句通用说明。</summary>
        private static string KindMeaning(char kind)
        {
            switch (kind)
            {
                case 'b':
                    return "打开 = 让这件物品带上这个状态，关掉 = 去掉；改完立刻生效。";
                case 'i':
                    return "这件物品身上的一个数值，游戏在对应功能里读它。改完立刻生效，填回原来的数字就恢复原样。";
                case 'f':
                case 'd':
                    return "这件物品身上的一个数值，可以带小数。改完立刻生效，填回原来的数字就恢复原样。";
                case 's':
                    return "一段文本内容，一般是 id 或名字。填错会让对应功能找不到目标。";
                default:
                    return "游戏里没读它的值，这里只显示、改了也没用。";
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
