// 自动生成，勿手改：wiki 物品分类（大分类 / 小分类）数据表。
// 来源 https://wiki.biligame.com/probablystolen/物品 —— 抓 17 个大节里 DPL 渲染出的表格，
// 中文标题按 Item_zh 表反查成物品 id；源码里硬编码英文名的少数物品靠生成脚本里的手工别名补。
// 同一件物品可以同时属于多个小分类（比如植皮罗非鱼卵：食品 + 动物）。
using System;
using System.Collections.Generic;

namespace HermitVoidGenerator
{
    internal static class WikiCategories
    {
        /// <summary>大分类，按 wiki 页顺序。</summary>
        public static readonly string[] Sections =
        {
            "食物与水", "日用品", "烟酒类", "医疗品", "材料", "工程",
            "工具", "农业", "化学", "武器装备", "物资箱", "艺术品",
            "奢侈品", "机器", "文档", "可鉴定物品", "其它",
        };

        /// <summary>[i,0]=大分类 [i,1]=小分类，按 wiki 页顺序。</summary>
        public static readonly string[,] Subs =
        {
            { "食物与水", "食品" }, { "食物与水", "零食" }, { "食物与水", "水" }, { "食物与水", "违禁食物" },
            { "日用品", "常规日用品" }, { "日用品", "净水用品" }, { "日用品", "水容器" }, { "日用品", "违禁日用品" },
            { "烟酒类", "常规烟酒类" }, { "烟酒类", "违禁烟酒类" }, { "医疗品", "常规医疗品" }, { "医疗品", "麻醉品" },
            { "医疗品", "违禁医疗品" }, { "材料", "材料" }, { "材料", "违禁材料" }, { "工程", "模组" },
            { "工程", "能量电池" }, { "工程", "工程违禁品" }, { "工具", "工具" }, { "工具", "刀具" },
            { "农业", "种子" }, { "化学", "化学用品" }, { "化学", "违禁化学用品" }, { "武器装备", "枪械" },
            { "武器装备", "弹药" }, { "武器装备", "非致命枪械" }, { "武器装备", "近战武器" }, { "武器装备", "军需品" },
            { "物资箱", "物资箱" }, { "物资箱", "识别卡" }, { "艺术品", "磁带" }, { "奢侈品", "奢侈品" },
            { "奢侈品", "违禁奢侈品" }, { "机器", "机器" }, { "机器", "储藏" }, { "文档", "鉴定指南" },
            { "文档", "教学手册" }, { "文档", "剧情信息" }, { "文档", "彩票" }, { "可鉴定物品", "可鉴定物品" },
            { "其它", "灯牌" }, { "其它", "动物" },
        };

        private static readonly string[,] Items =
        {
            { "100ml_cylinder", "常规日用品" }, { "1ml_dropper", "常规日用品" }, { "25ml_cylinder", "常规日用品" }, { "50ml_cylinder", "常规日用品" },
            { "5ml_dropper", "常规日用品" }, { "advanced_flux_agent", "材料" }, { "alarm_module_transmitter", "模组" }, { "aquascan", "净水用品" },
            { "auto_butcher", "机器" }, { "backpack_large", "储藏" }, { "backpack_large_military", "储藏" }, { "backpack_medium", "储藏" },
            { "backpack_medium_military", "储藏" }, { "backpack_small", "储藏" }, { "bandage_item", "常规医疗品" }, { "beis_icecream", "零食" },
            { "birth_control_pill", "常规医疗品" }, { "black_injector", "常规医疗品" }, { "black_injector", "奢侈品" }, { "black_lamp", "灯牌" },
            { "blank_keycard", "识别卡" }, { "blank_module", "模组" }, { "blender", "机器" }, { "blood_bag", "常规医疗品" },
            { "bloomberry_seed", "种子" }, { "blue_blood_bag", "常规医疗品" }, { "blue_blood_bag", "奢侈品" }, { "bottle_hot_sauce", "食品" },
            { "bottle_printer", "机器" }, { "bottled_water", "水容器" }, { "bottled_water_premium", "水" }, { "bottled_water_premium", "奢侈品" },
            { "box_cutter", "常规日用品" }, { "box_cutter", "刀具" }, { "box_cutter", "近战武器" }, { "box_tampon", "常规日用品" },
            { "box_tampon", "奢侈品" }, { "business_permit", "剧情信息" }, { "c4", "军需品" }, { "caffeine_pill", "常规日用品" },
            { "caffeine_pill", "常规医疗品" }, { "card_chem", "剧情信息" }, { "card_rev", "剧情信息" }, { "cassette1", "磁带" },
            { "cassette10", "磁带" }, { "cassette10d", "磁带" }, { "cassette10l", "磁带" }, { "cassette11", "磁带" },
            { "cassette11d", "磁带" }, { "cassette11l", "磁带" }, { "cassette12", "磁带" }, { "cassette12d", "磁带" },
            { "cassette12l", "磁带" }, { "cassette13", "磁带" }, { "cassette13d", "磁带" }, { "cassette13l", "磁带" },
            { "cassette14", "磁带" }, { "cassette14d", "磁带" }, { "cassette14l", "磁带" }, { "cassette1d", "磁带" },
            { "cassette1l", "磁带" }, { "cassette2", "磁带" }, { "cassette2d", "磁带" }, { "cassette2l", "磁带" },
            { "cassette3", "磁带" }, { "cassette3d", "磁带" }, { "cassette3l", "磁带" }, { "cassette4", "磁带" },
            { "cassette4d", "磁带" }, { "cassette4l", "磁带" }, { "cassette5", "磁带" }, { "cassette5d", "磁带" },
            { "cassette5l", "磁带" }, { "cassette6", "磁带" }, { "cassette6d", "磁带" }, { "cassette6l", "磁带" },
            { "cassette7", "磁带" }, { "cassette7d", "磁带" }, { "cassette7l", "磁带" }, { "cassette8", "磁带" },
            { "cassette8d", "磁带" }, { "cassette8l", "磁带" }, { "cassette9", "磁带" }, { "cassette9d", "磁带" },
            { "cassette9l", "磁带" }, { "cassette_player", "机器" }, { "cat_bar", "零食" }, { "caviar", "食品" },
            { "caviar", "奢侈品" }, { "cert1", "鉴定指南" }, { "cert2", "鉴定指南" }, { "cert3", "鉴定指南" },
            { "cert4", "鉴定指南" }, { "chem_finisher", "机器" }, { "chem_module", "模组" }, { "chemistry_guide", "教学手册" },
            { "cigarette_color", "鉴定指南" }, { "cigarette_guide", "鉴定指南" }, { "cmd_keycard", "识别卡" }, { "coffee_bean", "常规日用品" },
            { "coffee_bean", "种子" }, { "combat_knife", "刀具" }, { "combat_knife", "近战武器" }, { "combat_machete", "刀具" },
            { "combat_machete", "近战武器" }, { "common_chemical", "化学用品" }, { "common_electronic", "材料" }, { "common_medical", "常规医疗品" },
            { "common_ore", "材料" }, { "crowbar", "近战武器" }, { "cup_noodle", "食品" }, { "deed", "剧情信息" },
            { "desequencer", "机器" }, { "dna_manipulator_module_negative_filter", "模组" }, { "dna_manipulator_module_positive_filter", "模组" }, { "dna_manipulator_module_precision", "模组" },
            { "dna_manipulator_module_stabilizer", "模组" }, { "dream_cap", "违禁化学用品" }, { "dream_dust", "违禁烟酒类" }, { "dream_dust", "麻醉品" },
            { "dream_dust", "违禁医疗品" }, { "eel_fillet", "食品" }, { "eel_fillet", "奢侈品" }, { "eel_fillet", "动物" },
            { "energy_credit", "能量电池" }, { "energy_credit_breeder", "能量电池" }, { "energy_credit_dis", "能量电池" }, { "energy_credit_ext", "能量电池" },
            { "energy_credit_hyp", "能量电池" }, { "energy_drink", "零食" }, { "eng_box", "物资箱" }, { "eng_keycard", "识别卡" },
            { "evaporator", "机器" }, { "evaporator_module_solvent_recovery", "模组" }, { "evidence_box", "物资箱" }, { "fanny_pack", "储藏" },
            { "fanta_pill", "违禁烟酒类" }, { "fanta_pill", "麻醉品" }, { "fanta_pill", "违禁医疗品" }, { "farm_module_dream_cap", "模组" },
            { "farm_module_fillerweed_production", "模组" }, { "farm_module_hydroreed_filter", "模组" }, { "farm_module_kotton_loom", "模组" }, { "farm_module_nightmare_cap", "工程违禁品" },
            { "farm_module_nutrifruit", "模组" }, { "farm_module_smuggler", "模组" }, { "fat_meat", "食品" }, { "fillerweed", "食品" },
            { "fillerweed_seed", "种子" }, { "fish_fillet", "食品" }, { "flashbang_grenade", "军需品" }, { "flashlight", "工具" },
            { "flux_agent", "材料" }, { "fridge", "机器" }, { "fuel_guide", "鉴定指南" }, { "furnace", "机器" },
            { "furnace_module_blast", "模组" }, { "furnace_module_junk", "模组" }, { "galaxy_blend", "食品" }, { "gene_scanner", "工具" },
            { "glass_shard_shiv", "刀具" }, { "glass_shard_shiv", "近战武器" }, { "glock_receiver", "枪械" }, { "gun_part", "材料" },
            { "hand", "可鉴定物品" }, { "hand_guide", "鉴定指南" }, { "handmade_pistol", "枪械" }, { "hatchet", "近战武器" },
            { "heating_plate", "机器" }, { "heavy_handmade_pistol", "枪械" }, { "heavy_pistol_ammo", "弹药" }, { "heavy_pistol_ammo_nl", "弹药" },
            { "heavy_pistol_ammo_p", "弹药" }, { "hemostatic_bandage_item", "常规医疗品" }, { "hydroponic", "机器" }, { "hydroponic_guide", "教学手册" },
            { "hydroreed_filter", "净水用品" }, { "hydroreed_seed", "种子" }, { "hypertech_bay", "储藏" }, { "hypertech_electronic", "材料" },
            { "ingot_book", "教学手册" }, { "injector_guide", "鉴定指南" }, { "junk", "材料" }, { "kids_backpack", "储藏" },
            { "kids_backpack_large", "储藏" }, { "kitchen_cleaver", "常规日用品" }, { "kitchen_cleaver", "刀具" }, { "kitchen_cleaver", "近战武器" },
            { "kitchen_knife", "常规日用品" }, { "kitchen_knife", "刀具" }, { "kitchen_knife", "近战武器" }, { "kotton_fabric", "材料" },
            { "kotton_fiber", "材料" }, { "kotton_seed", "种子" }, { "large_bottled_water", "水容器" }, { "large_fish_fillet", "食品" },
            { "large_fish_fillet", "奢侈品" }, { "large_purple_injector", "常规医疗品" }, { "li_eat_snackbar", "零食" }, { "liquid_luck", "水" },
            { "livestock_feed_dispenser", "机器" }, { "logo_checker", "鉴定指南" }, { "lottery_info", "彩票" }, { "lottery_ticket", "彩票" },
            { "lottery_ticket_old", "彩票" }, { "luxury_supply_crate", "常规日用品" }, { "luxury_supply_crate", "奢侈品" }, { "machine_bay", "储藏" },
            { "machine_bay_ext", "储藏" }, { "makeshift_reactor", "机器" }, { "makeshift_storage_bay", "储藏" }, { "meal_cap", "食品" },
            { "med_bottle_blue", "常规日用品" }, { "med_bottle_orange", "常规日用品" }, { "med_bottle_red", "常规日用品" }, { "med_bottle_small_pink", "常规日用品" },
            { "med_bottle_violet", "常规日用品" }, { "med_box", "物资箱" }, { "med_keycard", "识别卡" }, { "metal_ingot", "材料" },
            { "metal_scanner", "工具" }, { "mind_reprogrammer", "工具" }, { "mini_bottle", "水容器" }, { "mirage_projector", "机器" },
            { "module_bay_expansion_kit", "工具" }, { "module_extractor", "工具" }, { "module_extractor_advanced", "工具" }, { "moisture_farm", "机器" },
            { "morsel", "食品" }, { "neuroactive_perfume", "奢侈品" }, { "newspaper", "剧情信息" }, { "nightmare_cap", "违禁医疗品" },
            { "nightmare_cap", "违禁化学用品" }, { "nightmare_dust", "违禁医疗品" }, { "node_medium", "模组" }, { "node_small", "模组" },
            { "nudka", "常规烟酒类" }, { "nutrifruit", "食品" }, { "nutrifruit_seed", "种子" }, { "nutrifruit_seed_packet", "种子" },
            { "nuts_metal", "材料" }, { "pack_condom", "常规日用品" }, { "packet_red_cigarette", "常规烟酒类" }, { "paper_towel", "常规日用品" },
            { "permit_gun_1", "剧情信息" }, { "permit_gun_2", "剧情信息" }, { "permit_gun_3", "剧情信息" }, { "phagimycin_pill", "常规医疗品" },
            { "pheromone_perfume", "违禁奢侈品" }, { "pink_alt_injector", "常规医疗品" }, { "pink_injector", "常规烟酒类" }, { "pink_injector", "常规医疗品" },
            { "pink_injector", "麻醉品" }, { "pipe_weapon", "近战武器" }, { "plant_analyser", "工具" }, { "plant_dna_manipulator", "机器" },
            { "plasma_fuel", "可鉴定物品" }, { "portable_water_purifier", "净水用品" }, { "portable_water_purifier", "奢侈品" }, { "powerblock", "机器" },
            { "printed_gun", "枪械" }, { "printer", "机器" }, { "printer_module_cnc", "模组" }, { "printer_module_melee_batch", "模组" },
            { "printer_module_metal", "模组" }, { "printer_plastic", "材料" }, { "processed_cheese", "食品" }, { "processed_juice", "食品" },
            { "processed_meat", "食品" }, { "processed_milk", "食品" }, { "procurement_slip", "剧情信息" }, { "pure_white_injector", "常规医疗品" },
            { "pure_white_injector", "麻醉品" }, { "rare_electronic", "材料" }, { "rare_ore", "材料" }, { "ras", "机器" },
            { "ras_water_tank", "常规日用品" }, { "rat", "动物" }, { "raw_meat", "食品" }, { "raw_meat", "奢侈品" },
            { "recharger", "机器" }, { "red_beer", "常规烟酒类" }, { "restricted_chemical", "违禁化学用品" }, { "revolver", "枪械" },
            { "ribwich", "可鉴定物品" }, { "ribwich_guide", "鉴定指南" }, { "rubbing_alcohol", "常规医疗品" }, { "salve", "常规医疗品" },
            { "satchel", "储藏" }, { "scav_token", "灯牌" }, { "sci_keycard", "识别卡" }, { "scrap_metal", "材料" },
            { "scratcher", "彩票" }, { "scratcher_stack", "彩票" }, { "screwdriver", "工具" }, { "sec_box", "物资箱" },
            { "sec_keycard", "识别卡" }, { "sec_slip", "剧情信息" }, { "security_alarm", "机器" }, { "self_refilling_neuroactive_perfume", "奢侈品" },
            { "ser_keycard", "识别卡" }, { "service_box", "物资箱" }, { "shampoo", "常规日用品" }, { "shotgun", "枪械" },
            { "sign_expedition", "灯牌" }, { "sign_firearm", "灯牌" }, { "sign_firearm_heavy", "灯牌" }, { "sign_food", "灯牌" },
            { "sign_household", "灯牌" }, { "sign_material", "灯牌" }, { "sign_medical", "灯牌" }, { "sign_water", "灯牌" },
            { "skincare_cream", "奢侈品" }, { "skinning_knife", "刀具" }, { "skinning_knife", "近战武器" }, { "slip", "剧情信息" },
            { "small_bottled_water", "水容器" }, { "small_eel_fillet", "食品" }, { "small_eel_fillet", "奢侈品" }, { "small_eel_fillet", "动物" },
            { "small_fish_fillet", "食品" }, { "small_morsel", "食品" }, { "small_pistol_ammo", "弹药" }, { "small_pistol_ammo_nl", "弹药" },
            { "small_pistol_ammo_p", "弹药" }, { "small_raw_meat", "食品" }, { "smelling_salt", "常规医疗品" }, { "smg", "枪械" },
            { "smoke_grenade", "军需品" }, { "smuggler_bay", "储藏" }, { "smuggler_bay_mini", "储藏" }, { "smuggler_bay_mod", "储藏" },
            { "soda_red", "零食" }, { "stamp_guide", "鉴定指南" }, { "storage_bay", "储藏" }, { "storage_bay_chem", "储藏" },
            { "storage_bay_expansion_kit", "工具" }, { "storage_bay_gun", "储藏" }, { "storage_bay_large", "储藏" }, { "storage_coordinator", "机器" },
            { "stun_baton", "近战武器" }, { "sturgeon_roe", "食品" }, { "sturgeon_roe", "奢侈品" }, { "sturgeon_roe", "动物" },
            { "sup_keycard", "识别卡" }, { "surgery_tool", "常规医疗品" }, { "system_capped_neural_core", "工程违禁品" }, { "system_module_corrupt", "模组" },
            { "system_module_eco", "模组" }, { "system_module_efficiancy", "模组" }, { "system_module_fineness", "模组" }, { "system_module_overclock", "模组" },
            { "system_module_performance", "模组" }, { "system_module_quality", "模组" }, { "system_module_ruined", "模组" }, { "system_module_streamlining", "模组" },
            { "system_uncapped_neural_core", "工程违禁品" }, { "tazer", "近战武器" }, { "tilapia_skin_graft", "常规医疗品" }, { "tilapia_skin_graft", "动物" },
            { "tiny_fish_fillet", "食品" }, { "toilet_paper", "常规日用品" }, { "toolbox", "储藏" }, { "toothpaste", "常规日用品" },
            { "topical_bandage_item", "常规医疗品" }, { "turbo_booster", "工具" }, { "turbo_booster_adv", "工具" }, { "turbo_booster_hypertech", "工具" },
            { "tutorial_book", "教学手册" }, { "unlicensed_phagimycin_pill", "违禁医疗品" }, { "uv_filter", "净水用品" }, { "vacuum_robot", "机器" },
            { "vending_fountain", "机器" }, { "vending_machine", "机器" }, { "wanted_paper", "剧情信息" }, { "water_filter", "净水用品" },
            { "water_filter_adv", "净水用品" }, { "water_filter_adv", "奢侈品" }, { "water_guide", "教学手册" }, { "water_jug", "水容器" },
            { "water_pitcher", "水容器" }, { "water_purifier", "机器" }, { "water_ration", "水" }, { "water_ration_small", "水" },
            { "water_tablet", "净水用品" }, { "water_tablet_container", "净水用品" }, { "water_tank", "水容器" }, { "water_test_container", "净水用品" },
            { "water_test_strip", "净水用品" }, { "welder", "工具" }, { "wine_berry", "食品" }, { "wine_bottle", "常规日用品" },
            { "wine_rack", "机器" }, { "wine_superyeast", "常规日用品" }, { "wine_yeast", "常规日用品" }, { "wine_yeast_infinite", "常规日用品" },
            { "wine_yeast_red", "违禁日用品" }, { "wire", "材料" }, { "wire_cutter", "工具" }, { "xray_scanner", "工具" },
            { "zerochew", "食品" }, { "zerochew", "常规日用品" }, { "zyanide_pill", "违禁医疗品" },
        };

        private static Dictionary<string, string[]> byItem;
        private static Dictionary<string, string[]> bySection;
        private static Dictionary<string, string> sectionOf;

        /// <summary>这件物品属于哪些小分类；wiki 没收录返回 null。</summary>
        public static string[] SubsOfItem(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            Dictionary<string, string[]> index = ByItem();
            string[] subs;
            return index.TryGetValue(id, out subs) ? subs : null;
        }

        /// <summary>这个大类下面有哪些小分类；未知大类返回空数组。</summary>
        public static string[] SubsOfSection(string section)
        {
            if (string.IsNullOrEmpty(section))
            {
                return new string[0];
            }
            Dictionary<string, string[]> index = BySection();
            string[] subs;
            return index.TryGetValue(section, out subs) ? subs : new string[0];
        }

        /// <summary>小分类属于哪个大分类；未知返回 null。</summary>
        public static string SectionOf(string sub)
        {
            if (string.IsNullOrEmpty(sub))
            {
                return null;
            }
            Dictionary<string, string> index = SectionOfMap();
            string section;
            return index.TryGetValue(sub, out section) ? section : null;
        }

        public static int SubCount { get { return Subs.GetLength(0); } }
        public static int ItemCount { get { return Items.GetLength(0); } }

        private static Dictionary<string, string[]> ByItem()
        {
            if (byItem != null)
            {
                return byItem;
            }
            byItem = new Dictionary<string, string[]>(StringComparer.Ordinal);
            Dictionary<string, List<string>> acc = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < Items.GetLength(0); i++)
            {
                List<string> list;
                if (!acc.TryGetValue(Items[i, 0], out list))
                {
                    list = new List<string>();
                    acc[Items[i, 0]] = list;
                }
                if (!list.Contains(Items[i, 1]))
                {
                    list.Add(Items[i, 1]);
                }
            }
            foreach (KeyValuePair<string, List<string>> pair in acc)
            {
                byItem[pair.Key] = pair.Value.ToArray();
            }
            return byItem;
        }

        private static Dictionary<string, string[]> BySection()
        {
            if (bySection != null)
            {
                return bySection;
            }
            bySection = new Dictionary<string, string[]>(StringComparer.Ordinal);
            Dictionary<string, List<string>> acc = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < Subs.GetLength(0); i++)
            {
                List<string> list;
                if (!acc.TryGetValue(Subs[i, 0], out list))
                {
                    list = new List<string>();
                    acc[Subs[i, 0]] = list;
                }
                list.Add(Subs[i, 1]);
            }
            foreach (KeyValuePair<string, List<string>> pair in acc)
            {
                bySection[pair.Key] = pair.Value.ToArray();
            }
            return bySection;
        }

        private static Dictionary<string, string> SectionOfMap()
        {
            if (sectionOf != null)
            {
                return sectionOf;
            }
            sectionOf = new Dictionary<string, string>(Subs.GetLength(0), StringComparer.Ordinal);
            for (int i = 0; i < Subs.GetLength(0); i++)
            {
                sectionOf[Subs[i, 1]] = Subs[i, 0];
            }
            return sectionOf;
        }
    }
}
