using System;
using System.Collections.Generic;
using System.Text;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 物品名中文兜底。游戏本地化表只覆盖了一部分物品，剩下的会直接显示英文原文，
    /// 这里用手工表 + 词素表把常见的那批补成中文。名字里已经带中文的不受影响。
    /// </summary>
    internal static class ItemNames
    {
        // 手工表：整件物品的固定译名（词素拼不出来的、或者拼出来别扭的）
        private static readonly string[,] Manual =
        {
            { "save_bag", "寄存袋" },
            { "back_inv_save_bag", "后仓寄存袋" },
            { "hidden_save_bag", "隐藏寄存袋" },
            { "showcase_save_bag", "展示柜寄存袋" },
            { "sold_save_bag", "售出寄存袋" },
            { "deal_maker", "成交大师" },
            { "box_dispenser", "箱子分配器" },
            { "tutorial_page", "导师手记" },
            { "plastic_bag", "塑料袋" },
            { "aug_chip", "义体芯片" },
            { "sign_ruined_machine", "报废机器标牌" },
            { "lootbox_small", "小型战利品箱" },
            { "lootbox_medium", "中型战利品箱" },
            { "lootbox_large", "大型战利品箱" },
            { "technician_backpack", "技师背包" },
            { "firstaid_box", "急救箱" },
            { "pill_bottle", "药瓶" },
            { "bicarsole_pill_bottle", "比卡索药瓶" },
            { "ocutol_pill_bottle", "奥库托药瓶" },
            { "fixalin_pill_bottle", "菲克斯林药瓶" },
            { "phagimycin_pill_bottle", "吞噬霉素药瓶" },
            { "unlicensed_phagimycin_pill_bottle", "无证吞噬霉素药瓶" },
            { "oxycodone_pill_bottle", "羟考酮药瓶" },
            { "pouch_injector", "注射器袋" },
            { "pouch_injector_large", "大型注射器袋" },
            { "pouch_injector_large_double", "大型双层注射器袋" },
            { "pouch_medical", "医疗袋" },
            { "pouch_general", "小袋" },
            { "pouch_general_large", "大袋" },
            { "pouch_hydration", "补水袋" },
            { "organ_transport_container", "器官运输箱" },
            { "loan_contract", "贷款合同" },
            { "lockpicks", "开锁工具" },
            { "exotic_fuel", "异种燃料电池" },
            { "box_nail", "钉子盒" },
            { "trash_pile", "垃圾堆" },
            { "wrench_orange", "扳手" },
            { "steel_sheet", "钢板" },
            { "spectral_analyser", "光谱分析仪" },
            { "backpack_small_modular", "小型模块化背包" },
            { "backpack_medium_modular", "模块化背包" },
            { "backpack_large_modular", "大型模块化背包" },
            { "molotov_cocktail", "燃烧瓶" },
            { "safe", "保险箱" },
            { "large_storage_bay", "大型储藏舱" },
            { "aloe_vera", "芦荟枝" },
            { "aloe_vera_gel", "芦荟凝胶瓶" },
            { "aloe_vera_heal", "芦荟愈合贴" },
            { "antique_grenade", "古董破片手雷" },
            { "sec_card", "安保名片" },
            { "inspection_book", "鉴定手册" },
            { "organ_heart", "心脏" },
            { "organ_lung", "肺" },
            { "organ_kidney", "肾" },
            { "organ_liver", "肝" },
            { "organ_stomach", "胃" },
            { "organ_eye", "眼球" },
            { "organ_brain", "大脑" },
            { "revolver_short_tac", "兰斯韦尔战术短管左轮" },
            { "revolver_long", "兰斯韦尔 7 英寸左轮" },
            { "revolver_short", "兰斯韦尔短管左轮" },
            { "ammo_box", "弹药盒（未拆封）" },
            { "codebook", "密码本" },
            { "hemostatic_agent", "止血粉瓶" },
            { "test_water", "测试用水" },
            { "test_water2", "测试用水 2（蓝底）" },
            { "test_water3", "测试用水 3（红底）" },
            { "heavy_handmade_pistol_s", "铁工 10S" },
            { "heavy_handmade_pistol", "铁工 10" },
            { "armor_titan10", "泰坦 X 系列护甲板" },
            { "armor_titan10l", "泰坦 X 系列护甲板（轻）" },
            { "armor_titan10h", "泰坦 X 系列护甲板（重）" },
            { "armor_titan10s", "泰坦 X 系列护甲板（小）" },
            { "armor_titan9", "泰坦 IX 系列护甲板" },
            { "feces_small", "小型粪便" },
            { "feces_medium", "中型粪便" },
            { "feces_large", "大型粪便" },
            { "vacuum_robot", "真空清洁机器人" },
            { "rat", "老鼠" },
            { "plant_dna_disk", "植物基因磁盘" },
            { "serum_red", "红色血清" },
            { "serum_green", "绿色血清" },
            { "test_gizmo", "测试装置" },
            { "hemostatic_bandage_item", "止血绷带" },
            { "bandage_item", "绷带" },
            { "topical_bandage_item", "外敷绷带" },
            { "energy_credit_hyp", "高科能量币" },
            { "hand_guide", "入门手册" },
            { "aug_id_guide", "义体指南" },
            { "plant_dna_manipulator", "植物基因操纵器" },
            { "dna_manipulator_module_stabilizer", "基因操纵器模块：稳定器" },
            { "dna_manipulator_module_positive_filter", "基因操纵器模块：正向筛选" },
            { "dna_manipulator_module_negative_filter", "基因操纵器模块：负向筛选" },
            { "dna_manipulator_module_precision", "基因操纵器模块：精度" },
            { "mirage_generator", "幻影生成器" },
            { "hand", "手" },
            { "rev_codeword", "复活口令" },
            { "medical_proficiency_2", "医疗熟练度 II" },
            { "perk_discret", "天赋：机密" },
            { "mark", "标记" },
            { "mark_ability", "标记能力" },
            { "brace_ability", "支撑能力" },
            { "dev_damage", "开发者：伤害" },
            { "dev_damage_all", "开发者：全体伤害" },
            { "bandage_stop_bleed", "绷带：止血" },
            { "bandage_stop_light_bleed", "绷带：止轻伤流血" },
            { "stop_bleed", "止血" },
            { "energy_hand_canon_normal", "能量手炮：普通" },
            { "energy_hand_canon_charged", "能量手炮：充能" },
            { "combat_stim_acc_buff", "战斗兴奋剂：命中增益" },
            { "combat_stim_cc_buff", "战斗兴奋剂：控制增益" },
            { "bicarsole_heal", "比卡索：治疗" },
            { "bicarsole_pill", "比卡索药片" },
            { "ocutol_acc", "奥库托：命中" },
            { "ocutol_critical", "奥库托：暴击" },
            { "fixalin_dodge", "菲克斯林：闪避" },
            { "fixalin_speed", "菲克斯林：速度" },
            { "mirage_dodge", "幻影：闪避" },
            { "mark_dodge_debuff", "标记：闪避减益" },
            { "mark_priority_increase", "标记：优先度提升" },
            { "take_aim_acc_buff", "瞄准：命中增益" },
            { "brace_priority_decrease", "支撑：优先度降低" },
            { "brace_dodge_buff", "支撑：闪避增益" },
            { "blood_red_max_health", "血红：生命上限" },
            { "xeno_warcry_acc_buff", "异种战吼：命中增益" },
            { "xeno_mark_dodge_debuff", "异种标记：闪避减益" },
            { "xeno_mark_priority_increase", "异种标记：优先度提升" },
            { "xeno_prey_acc_buff", "异种猎食：命中增益" },
            { "xeno_irritant_acc_debuff", "异种刺激：命中减益" },
            { "xeno_healing_spray_heal", "异种治疗喷雾：治疗" },
            { "spa_acc", "温泉：命中" },
            { "spa_cc", "温泉：控制" },
            { "spa_max_hp", "温泉：生命上限" },
            { "perk_center_mass_acc_buff", "天赋中心弹：命中增益" },
            { "perk_center_mass_cc_debuff", "天赋中心弹：控制减益" },
        };

        // 词素表：id 按 _ 拆开逐个翻，能全翻出来才用
        private static readonly string[,] Words =
        {
            { "item", "" }, { "tag", "" }, { "the", "" },
            { "attack", "攻击" }, { "damage", "伤害" }, { "heal", "治疗" }, { "healing", "治疗" },
            { "combat", "战斗" }, { "ability", "能力" }, { "effect", "效果" }, { "buff", "增益" },
            { "debuff", "减益" }, { "stun", "眩晕" }, { "knockdown", "击倒" }, { "taunt", "嘲讽" },
            { "berserk", "狂暴" }, { "warcry", "战吼" }, { "empower", "强化" }, { "reduce", "降低" },
            { "decrease", "降低" }, { "increase", "提升" }, { "resist", "抗性" }, { "resist", "抗性" },
            { "dodge", "闪避" }, { "accuracy", "精准" }, { "aim", "瞄准" }, { "crit", "暴击" },
            { "critical", "暴击" }, { "chance", "几率" }, { "reaction", "反应" }, { "speed", "速度" },
            { "health", "生命" }, { "hp", "生命" }, { "energy", "能量" }, { "charge", "充能" },
            { "power", "电力" }, { "shield", "护盾" }, { "armor", "护甲" }, { "resist", "抗性" },
            { "xeno", "异形" }, { "perk", "天赋" }, { "ability2", "能力二" },
            { "stim", "兴奋剂" }, { "pill", "药丸" }, { "bottle", "瓶" }, { "serum", "血清" },
            { "injector", "注射器" }, { "bandage", "绷带" }, { "hemostatic", "止血" },
            { "topical", "外用" }, { "medical", "医疗" }, { "medic", "医疗兵" },
            { "bleed", "流血" }, { "laceration", "撕裂伤" }, { "scratch", "抓伤" },
            { "toxin", "毒素" }, { "venom", "毒素" }, { "irritant", "刺激物" },
            { "burn", "灼烧" }, { "smoke", "烟雾" }, { "dead", "死亡" }, { "death", "死亡" },
            { "blood", "血液" }, { "consume", "消耗" }, { "restorative", "恢复" },
            { "metabolism", "代谢" }, { "heal2", "治疗二" }, { "spa", "疗养" },
            { "red", "红" }, { "green", "绿" }, { "blue", "蓝" }, { "white", "白" },
            { "large", "大型" }, { "small", "小型" }, { "medium", "中型" }, { "heavy", "重型" },
            { "light", "轻型" }, { "double", "双层" }, { "big", "大型" }, { "short", "短" },
            { "long", "长" }, { "normal", "普通" }, { "charged", "充能" }, { "safe", "保险" },
            { "modular", "模块化" }, { "general", "通用" }, { "priority", "优先" },
            { "pouch", "袋" }, { "bag", "袋" }, { "backpack", "背包" }, { "box", "箱" },
            { "container", "容器" }, { "bottle2", "瓶" }, { "cage", "笼子" },
            { "storage", "储藏" }, { "bay", "舱" }, { "rack", "架" }, { "trap", "陷阱" },
            { "dispenser", "分配器" }, { "generator", "发电机" }, { "printer", "打印机" },
            { "machine", "机器" }, { "ruined", "报废" }, { "gizmo", "装置" },
            { "drone", "无人机" }, { "robot", "机器人" }, { "vacuum", "真空" },
            { "weapon", "武器" }, { "melee", "近战" }, { "blunt", "钝器" },
            { "knife", "刀" }, { "cleaver", "砍刀" }, { "machete", "开山刀" },
            { "hatchet", "短柄斧" }, { "fireaxe", "消防斧" }, { "crowbar", "撬棍" },
            { "baseball", "棒球" }, { "bat", "棍" }, { "baton", "警棍" }, { "pipe", "钢管" },
            { "lead", "铅" }, { "taser", "电击枪" }, { "revolver", "左轮" }, { "pistol", "手枪" },
            { "handmade", "手工" }, { "glock", "格洛克" }, { "ump", "UMP" }, { "mp9", "MP9" },
            { "laser", "激光" }, { "autocanon", "自动加农炮" }, { "canon", "加农炮" },
            { "grenade", "手雷" }, { "molotov", "燃烧瓶" }, { "cocktail", "燃烧瓶" },
            { "flashbang", "闪光弹" }, { "bomb", "炸弹" }, { "c4", "C4" }, { "frag", "破片" },
            { "ammo", "弹药" }, { "breaching", "破门" }, { "antique", "古董" },
            { "aloe", "芦荟" }, { "vera", "芦荟" }, { "gel", "凝胶" }, { "salt", "盐" },
            { "plant", "植物" }, { "dna", "基因" }, { "gene", "基因" }, { "disk", "磁盘" },
            { "manipulator", "操纵器" }, { "stabilizer", "稳定器" }, { "precision", "精密" },
            { "filter", "过滤器" }, { "positive", "正向" }, { "negative", "负向" },
            { "scanner", "扫描仪" }, { "analyser", "分析仪" }, { "spectral", "光谱" },
            { "nutrient", "营养" }, { "tablet", "药片" }, { "food", "食物" }, { "feed", "饲料" },
            { "mushroom", "蘑菇" }, { "berry", "浆果" }, { "lifeweed", "生命草" },
            { "animal", "动物" }, { "livestock", "家畜" }, { "feces", "粪便" },
            { "rat", "鼠" }, { "mouse", "鼠" }, { "hunter", "猎人" }, { "prey", "猎物" },
            { "organ", "器官" }, { "heart", "心脏" }, { "lung", "肺" }, { "kidney", "肾" },
            { "liver", "肝" }, { "agent", "药剂" }, { "water", "水" },
            { "wine", "酒" }, { "beercase", "啤酒箱" }, { "transport", "运输" },
            { "contract", "合同" }, { "loan", "贷款" }, { "lockpicks", "开锁器" },
            { "exotic", "异种" }, { "fuel", "燃料" }, { "nail", "钉子" }, { "trash", "垃圾" },
            { "pile", "堆" }, { "pile1", "堆一" }, { "pile2", "堆二" }, { "pile3", "堆三" },
            { "debris", "残骸" }, { "wrench", "扳手" }, { "orange", "橙色" },
            { "steel", "钢" }, { "sheet", "板" }, { "plastic", "塑料" }, { "chip", "芯片" },
            { "aug", "义体" }, { "card", "卡" }, { "sec", "安保" }, { "inspection", "鉴定" },
            { "book", "书" }, { "codebook", "密码本" }, { "guide", "指南" }, { "page", "页" },
            { "tutorial", "教程" }, { "sign", "标牌" }, { "test", "测试" },
            { "save", "寄存" }, { "hidden", "隐藏" }, { "showcase", "展示柜" },
            { "sold", "已售" }, { "back", "后仓" }, { "inv", "库存" },
            { "lootbox", "战利品箱" }, { "take", "获取" }, { "maker", "大师" },
            { "deal", "成交" }, { "credit", "信用点" }, { "hyp", "高科" },
            { "firstaid", "急救" }, { "retaliate", "反击" }, { "overwatch", "警戒" },
            { "assist", "辅助" }, { "diversion", "牵制" }, { "spotter", "观察手" },
            { "scavenge", "拾荒" }, { "scav", "拾荒" }, { "event", "事件" }, { "storm", "风暴" },
            { "mass", "群体" }, { "all", "全体" }, { "self", "自身" }, { "refilling", "补充" },
            { "neuroactive", "神经活性" }, { "perfume", "香水" }, { "smelling", "嗅" },
            { "remove", "移除" }, { "wait", "等待" }, { "null", "空" }, { "nothing", "无" },
            { "guard", "守卫" }, { "merchant", "商人" }, { "herald", "传令官" },
            { "brewer", "酿酒师" }, { "rookie", "新兵" }, { "veteran", "老兵" },
            { "chief", "首领" }, { "marine", "陆战队员" }, { "deserter", "逃兵" },
            { "pirate", "海盗" }, { "raider", "掠夺者" }, { "technician", "技师" },
            { "annoying", "烦人" }, { "healthy", "健康" }, { "sickly", "病弱" },
            { "tenacious", "坚韧" }, { "hemophilia", "血友病" }, { "thrombophilia", "血栓倾向" },
            { "phagophobia", "吞咽恐惧" }, { "trypanophobia", "针头恐惧" }, { "crafty", "狡猾" },
            { "pyromaniac", "纵火癖" }, { "adapdation", "适应" }, { "proficiency", "熟练" },
            { "center", "中心" }, { "priority2", "优先" }, { "tap", "水龙头" },
            { "albert", "阿尔伯特" }, { "benjamin", "本杰明" }, { "charlotte", "夏洛特" },
            { "daniel", "丹尼尔" }, { "phagimycin", "吞噬霉素" }, { "unlicensed", "无证" },
            { "oxycodone", "羟考酮" }, { "hydration", "补水" }, { "anticipate", "预判" },
        };

        private static Dictionary<string, string> manual;
        private static Dictionary<string, string> words;

        public static string Zh(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            Ensure();
            string hit;
            if (manual.TryGetValue(id, out hit))
            {
                return hit;
            }
            return Compose(id);
        }

        private static void Ensure()
        {
            if (manual != null)
            {
                return;
            }

            manual = new Dictionary<string, string>(Manual.GetLength(0), StringComparer.Ordinal);
            for (int i = 0; i < Manual.GetLength(0); i++)
            {
                manual[Manual[i, 0]] = Manual[i, 1];
            }

            words = new Dictionary<string, string>(Words.GetLength(0), StringComparer.Ordinal);
            for (int i = 0; i < Words.GetLength(0); i++)
            {
                words[Words[i, 0]] = Words[i, 1];
            }
        }

        private static string Compose(string id)
        {
            string[] parts = id.Split('_');
            StringBuilder sb = new StringBuilder();
            bool any = false;

            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i];
                if (token.Length == 0)
                {
                    continue;
                }

                string zh;
                if (!words.TryGetValue(token, out zh))
                {
                    // 有词素拼不出来就放弃合成，交给调用方保留原文
                    return null;
                }
                sb.Append(zh);
                any = true;
            }

            if (!any)
            {
                return null;
            }

            string text = sb.ToString();
            return text.Length == 0 ? null : text;
        }
    }
}
