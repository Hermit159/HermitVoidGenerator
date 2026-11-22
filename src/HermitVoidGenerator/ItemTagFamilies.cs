using System;
using System.Collections.Generic;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 标签的顺序表 + 中文兜底。分类只有一层（标签本身），这里不再是大类：
    /// Rows 里那个"组名"只决定下拉里标签的先后（常用的排前面），同名连续写就是同一段。
    /// 中文名不写死：运行时向游戏要（TypeHelper.GetTypeDisplayName），这里的 Zh 只做兜底。
    /// </summary>
    internal static class ItemTagFamilies
    {
        /// <summary>违禁品 / 管制等级 / 品级这类"属性型"标签排在最后一段。</summary>
        public const string SectionAttributes = "属性标签";

        /// <summary>游戏没给任何悬停标签的物品，合成一组放在下拉最后。</summary>
        public const string SectionUntagged = "无标签";

        // [i,0]=大类 [i,1]=游戏标签常量。声明顺序就是下拉里的顺序：
        // 大类按首次出现的先后排，同一个大类的标签必须连着写。
        // 标签常量全集见 ItemTags.g.cs / 本地化表的 type_* 词条。
        private static readonly string[,] Rows =
        {
            // 食物与水
            { "食物与水", "FOOD" },
            { "食物与水", "FRESH_FOOD" },
            { "食物与水", "PROCESSED_FOOD" },
            { "食物与水", "TREAT" },
            { "食物与水", "WATER" },
            { "食物与水", "WATER_RATION" },
            // 日用品
            { "日用品", "HOUSEHOLD_GOOD" },
            { "日用品", "WATER_PURIFICATION_SUPPLY" },
            { "日用品", "FILTER" },
            { "日用品", "OXYGEN_SUPPLY" },
            // 烟酒类
            { "烟酒类", "SUBSTANCE" },
            { "烟酒类", "BREVAGE" },
            { "烟酒类", "ALCOHOL" },
            { "烟酒类", "NARCOTIC" },
            { "烟酒类", "POISON" },
            // 医疗品
            { "医疗品", "MEDICAL" },
            { "医疗品", "MEDICAL_SUPPLIES" },
            { "医疗品", "MEDICAL_TOOL" },
            { "医疗品", "ORGAN" },
            // 材料
            { "材料", "MATERIAL" },
            { "材料", "BASIC_MATERIAL" },
            { "材料", "REFINED_MATERIAL" },
            { "材料", "JUNK" },
            // 工程
            { "工程", "MODULE" },
            { "工程", "HYDROPONIC_MODULE" },
            { "工程", "PRINTER_MODULE" },
            { "工程", "EVAPORATOR_MODULE" },
            { "工程", "ALARM_MODULE" },
            { "工程", "ENERGY_CELL" },
            { "工程", "NODE" },
            // 工具
            { "工具", "TOOL" },
            // 农业
            { "农业", "SEED" },
            { "农业", "MODIFIED_SEED" },
            { "农业", "FARMING_SUPPLIES" },
            // 化学
            { "化学", "CHEMICAL_SUPPLIES" },
            // 武器装备
            { "武器装备", "WEAPON" },
            { "武器装备", "FIREARM" },
            { "武器装备", "MELEE_WEAPON" },
            { "武器装备", "AMMUNITION" },
            { "武器装备", "MUNITION" },
            { "武器装备", "GUN_MOD" },
            { "武器装备", "MODDABLE" },
            { "武器装备", "ARMOR" },
            // 物资箱
            { "物资箱", "SUPPLY_CRATE" },
            { "物资箱", "STORAGE" },
            { "物资箱", "ACCESS_CARD" },
            // 艺术品 / 奢侈品 / 机器 / 文档
            { "艺术品", "ART" },
            { "奢侈品", "LUXURY_ITEM" },
            { "机器", "MACHINE" },
            { "文档", "DOCUMENT" },
            { "文档", "LOTTERY" },
            // 属性标签：违禁与管制先来，再按品级
            { SectionAttributes, "CONTRABAND" },
            { SectionAttributes, "RESTRICTED_1" },
            { SectionAttributes, "RESTRICTED_2" },
            { SectionAttributes, "RESTRICTED_3" },
            { SectionAttributes, "HIGHEND" },
            { SectionAttributes, "RARE" },
            { SectionAttributes, "LOWTECH" },
            { SectionAttributes, "MIDLINE" },
            { SectionAttributes, "MAKESHIFT" },
            { SectionAttributes, "NOT_FOR_RESALE" },
        };

        // 兜底中文（游戏本地化表的 type_* 词条）。正常运行走 TypeHelper.GetTypeDisplayName，
        // 这里只在本地化拿不到时顶上，免得下拉里露出英文常量。
        private static readonly string[,] Zh =
        {
            { "FOOD", "食物" }, { "FRESH_FOOD", "新鲜食物" }, { "PROCESSED_FOOD", "加工食品" },
            { "TREAT", "零食" }, { "WATER", "水" }, { "WATER_RATION", "水配给" },
            { "SUBSTANCE", "烟酒类" }, { "BREVAGE", "饮料" }, { "ALCOHOL", "酒精" },
            { "NARCOTIC", "麻醉品" }, { "POISON", "毒药" },
            { "MEDICAL", "医疗品" }, { "MEDICAL_SUPPLIES", "医疗用品" }, { "MEDICAL_TOOL", "医疗工具" },
            { "ORGAN", "器官" },
            { "HOUSEHOLD_GOOD", "日用品" }, { "WATER_PURIFICATION_SUPPLY", "净水用品" },
            { "FILTER", "过滤器" }, { "OXYGEN_SUPPLY", "氧气供应" },
            { "MATERIAL", "材料" }, { "BASIC_MATERIAL", "基础材料" }, { "REFINED_MATERIAL", "精炼材料" },
            { "JUNK", "垃圾" },
            { "MODULE", "模组" }, { "HYDROPONIC_MODULE", "水培模组" }, { "PRINTER_MODULE", "打印机模组" },
            { "EVAPORATOR_MODULE", "蒸馏器模组" }, { "ALARM_MODULE", "警报模组" },
            { "ENERGY_CELL", "能量电池" }, { "NODE", "节点" },
            { "TOOL", "工具" },
            { "SEED", "种子" }, { "MODIFIED_SEED", "改良种子" }, { "FARMING_SUPPLIES", "农业用品" },
            { "CHEMICAL_SUPPLIES", "化学用品" },
            { "WEAPON", "武器" }, { "FIREARM", "枪械" }, { "MELEE_WEAPON", "近战武器" },
            { "AMMUNITION", "弹药" }, { "MUNITION", "军需品" }, { "GUN_MOD", "枪械模组" },
            { "MODDABLE", "可改装" }, { "ARMOR", "护甲" },
            { "SUPPLY_CRATE", "物资箱" }, { "STORAGE", "储藏" }, { "ACCESS_CARD", "识别卡" },
            { "ART", "艺术品" }, { "LUXURY_ITEM", "奢侈品" }, { "MACHINE", "机器" },
            { "DOCUMENT", "文档" }, { "LOTTERY", "彩票" },
            { "CONTRABAND", "违禁品" }, { "RESTRICTED_1", "一级管制" }, { "RESTRICTED_2", "二级管制" },
            { "RESTRICTED_3", "三级管制" }, { "HIGHEND", "高端" }, { "RARE", "稀有" },
            { "LOWTECH", "低科技" }, { "MIDLINE", "中级" }, { "MAKESHIFT", "简易自制" },
            { "NOT_FOR_RESALE", "非卖品" },
        };

        /// <summary>大类顺序（按 Rows 里首次出现），末尾不含无标签兜底。</summary>
        public static readonly string[] Sections;

        private static readonly Dictionary<string, string> sectionOfTag =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly Dictionary<string, string[]> tagsOfSection =
            new Dictionary<string, string[]>(StringComparer.Ordinal);

        private static readonly Dictionary<string, string> zhOfTag =
            new Dictionary<string, string>(StringComparer.Ordinal);

        static ItemTagFamilies()
        {
            List<string> order = new List<string>();
            List<List<string>> groups = new List<List<string>>();
            Dictionary<string, int> indexOfSection = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < Rows.GetLength(0); i++)
            {
                string section = Rows[i, 0];
                string tag = Rows[i, 1];
                if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                int slot;
                if (!indexOfSection.TryGetValue(section, out slot))
                {
                    slot = order.Count;
                    indexOfSection[section] = slot;
                    order.Add(section);
                    groups.Add(new List<string>());
                }

                groups[slot].Add(tag);
                sectionOfTag[tag] = section;
            }

            Sections = order.ToArray();
            for (int i = 0; i < order.Count; i++)
            {
                tagsOfSection[order[i]] = groups[i].ToArray();
            }

            for (int i = 0; i < Zh.GetLength(0); i++)
            {
                zhOfTag[Zh[i, 0]] = Zh[i, 1];
            }
        }

        /// <summary>标签属于哪个大类；未登记返回空串。</summary>
        public static string SectionOf(string tag)
        {
            string section;
            if (!string.IsNullOrEmpty(tag) && sectionOfTag.TryGetValue(tag, out section))
            {
                return section;
            }
            return "";
        }

        /// <summary>某个大类下的标签，声明顺序。</summary>
        public static string[] TagsOfSection(string section)
        {
            string[] tags;
            if (!string.IsNullOrEmpty(section) && tagsOfSection.TryGetValue(section, out tags))
            {
                return tags;
            }
            return new string[0];
        }

        /// <summary>兜底中文名；没登记返回空串。</summary>
        public static string ZhOf(string tag)
        {
            string zh;
            if (!string.IsNullOrEmpty(tag) && zhOfTag.TryGetValue(tag, out zh))
            {
                return zh;
            }
            return "";
        }
    }
}
