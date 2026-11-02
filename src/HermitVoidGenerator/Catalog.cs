using System;
using System.Collections.Generic;
using UnityEngine;

namespace HermitVoidGenerator
{
    /// <summary>物品目录：枚举 / 筛选 / 名称 / 图标 / 生成。</summary>
    internal static class Catalog
    {
        private static bool loaded;
        private static List<string> ids = new List<string>();
        private static List<string> groupClasses = new List<string>();
        private static List<string> groupLabels = new List<string> { "全部分类" };
        private static readonly Dictionary<string, string> names = new Dictionary<string, string>();
        private static Dictionary<string, int[]> groupOf;
        private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, string> descs = new Dictionary<string, string>();

        // 两级分类：wiki 大分类（17 个）→ 小分类（42 个）。一件物品可以挂在多个小分类下。
        public const string SectionAll = "全部大类";
        public const string SectionGameDir = "游戏目录";
        private static List<string> sectionLabels = new List<string> { SectionAll };
        private static List<int> groupSection = new List<int> { 0 };

        // 游戏里挂着 GameItem 的目录类（GetDictionaryClassNameList 的返回值），
        // 去掉 ItemDirectory / Directory 后缀后就是 key。
        private static readonly Dictionary<string, string> GroupZh = new Dictionary<string, string>
        {
            { "Amenities", "生活用品" }, { "Armor", "护甲" }, { "Audio", "音频" },
            { "CombatAbilityEffectPermanent", "战斗效果·永久" },
            { "CombatAbilityEffectInstant", "战斗效果·即时" },
            { "CombatAbilityEffectStandard", "战斗效果·标准" },
            { "Character", "角色" }, { "Construction", "建材" }, { "Container", "容器" }, { "Drop", "掉落物" },
            { "DropTableFourniture", "家具掉落表" }, { "DropTable", "掉落表" },
            { "Equipment", "装备" }, { "Explosive", "爆炸物" }, { "Food", "食品" },
            { "Furniture", "家具" }, { "GunMod", "枪械配件" }, { "Guns", "枪械" },
            { "Husbandry", "养殖" }, { "Hydroponic", "水培" }, { "Key", "钥匙" },
            { "Material", "材料" }, { "MeleeWeapon", "近战武器" }, { "Misc", "杂项" },
            { "Meds", "药品" }, { "Mod", "改装件" }, { "Module", "模块" },
            { "NpcCharacter", "NPC 角色" }, { "Organ", "器官" }, { "Perk", "天赋" },
            { "PlayerCharacter", "玩家角色" }, { "PlayerAbility", "玩家能力" },
            { "RuinedMachine", "报废机器" }, { "Ship", "飞船" }, { "ShipSystem", "飞船系统" },
            { "StandardCombatAbility", "战斗能力" }, { "TechnicianBackpack", "技师背包" },
            { "Tool", "工具" }, { "Unused", "未使用" }, { "Wine", "酒" },
            // 兜底（版本变了也不会露出英文）
            { "Weapon", "武器" }, { "Clothing", "衣物" }, { "Toy", "玩具" },
            { "Book", "书籍" }, { "Media", "影音" }, { "Drug", "药品" },
            { "Drink", "饮品" }, { "Trinket", "小玩意" }, { "Component", "零件" },
            { "Part", "零件" }, { "Ammo", "弹药" }, { "Attachment", "配件" },
            { "Cyber", "义体" }, { "Junk", "破烂" }, { "Currency", "通货" },
            { "Blueprint", "图纸" }, { "Paint", "涂装" }, { "Decor", "装饰" },
            { "Art", "艺术品" }, { "Gem", "宝石" }, { "Seed", "种子" },
            { "Plant", "植物" }, { "Fish", "鱼" },
        };

        // 分类顺序：先走 wiki 百科的大/小分类（见 WikiCategories.g.cs），wiki 没收录的物品
        // 退回游戏自己的目录类。两套都算不出来才不归组。
        private static Dictionary<string, string[]> wikiSubs;
        private static Dictionary<string, int> wikiGroupIndex;
        private static List<int> dirIndex;
        private static readonly Dictionary<string, int> dirOf = new Dictionary<string, int>(StringComparer.Ordinal);

        public static int Count { get { Ensure(); return ids.Count; } }
        public static List<string> GroupLabels { get { Ensure(); return groupLabels; } }
        public static List<string> SectionLabels { get { Ensure(); return sectionLabels; } }

        /// <summary>分组属于哪个大类（返回 SectionLabels 的下标）。</summary>
        public static int SectionOfGroup(int groupIndex)
        {
            Ensure();
            if (groupIndex <= 0 || groupIndex >= groupSection.Count)
            {
                return 0;
            }
            return groupSection[groupIndex];
        }

        public static void Ensure(bool force = false)
        {
            if (loaded && !force)
            {
                return;
            }

            loaded = true;
            groupOf = null;
            ids = new List<string>();
            groupClasses = new List<string>();
            groupLabels = new List<string> { "全部分类" };

            try
            {
                List<string> raw = DirectoryMaster.GetIdentifierList<GameItem>();
                if (raw != null)
                {
                    for (int i = 0; i < raw.Count; i++)
                    {
                        string id = raw[i];
                        if (string.IsNullOrEmpty(id) || ids.Contains(id))
                        {
                            continue;
                        }
                        if (ItemSpawnerHiddenList.IsHidden(id))
                        {
                            continue;
                        }
                        ids.Add(id);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("读取物品目录失败: " + e.Message);
            }

            try
            {
                List<string> classes = DirectoryMaster.GetDictionaryClassNameList<GameItem>();
                if (classes != null)
                {
                    for (int i = 0; i < classes.Count; i++)
                    {
                        string className = classes[i];
                        if (string.IsNullOrEmpty(className) || LabelFor(className).Length == 0)
                        {
                            continue;
                        }
                        if (!groupClasses.Contains(className))
                        {
                            groupClasses.Add(className);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("读取物品分类失败: " + e.Message);
            }

            BuildGroups();

            Log.Info("目录已加载: " + ids.Count + " 件物品 / " + sectionLabels.Count + " 个大类 / " + groupLabels.Count + " 个小类（wiki " + WikiCategories.ItemCount + " 条分类关系）");
        }

        /// <summary>分类标签表：wiki 小分类在前（按 wiki 百科大小类顺序，只留真有物品的），游戏目录分类兜底在后。</summary>
        private static void BuildGroups()
        {
            wikiSubs = new Dictionary<string, string[]>(StringComparer.Ordinal);
            wikiGroupIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            dirIndex = new List<int>(groupClasses.Count);
            dirOf.Clear();
            groupOf = null;

            HashSet<string> usedSubs = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++)
            {
                string[] subs = WikiCategories.SubsOfItem(ids[i]);
                if (subs == null || subs.Length == 0)
                {
                    continue;
                }
                wikiSubs[ids[i]] = subs;
                for (int j = 0; j < subs.Length; j++)
                {
                    usedSubs.Add(subs[j]);
                }
            }

            // 1) wiki 小分类，按百科的大类顺序逐节铺开；空小节不进下拉
            for (int s = 0; s < WikiCategories.Sections.Length; s++)
            {
                string section = WikiCategories.Sections[s];
                string[] subs = WikiCategories.SubsOfSection(section);
                int secIndex = 0;
                for (int k = 0; k < subs.Length; k++)
                {
                    string sub = subs[k];
                    if (string.IsNullOrEmpty(sub) || !usedSubs.Contains(sub))
                    {
                        continue;
                    }
                    if (secIndex == 0)
                    {
                        secIndex = sectionLabels.Count;
                        sectionLabels.Add(section);
                    }
                    wikiGroupIndex[sub] = groupLabels.Count;
                    groupLabels.Add(sub);
                    groupSection.Add(secIndex);
                }
            }

            // 2) 游戏目录类兜底：只收 wiki 完全没收录的物品
            int dirSection = 0;
            for (int i = 0; i < groupClasses.Count; i++)
            {
                List<string> members = IdentifiersOf(groupClasses[i]);
                bool used = false;
                for (int j = 0; j < members.Count && !used; j++)
                {
                    string id = members[j];
                    used = !string.IsNullOrEmpty(id) && ids.Contains(id) && !wikiSubs.ContainsKey(id);
                }
                if (!used)
                {
                    dirIndex.Add(0);
                    continue;
                }

                if (dirSection == 0)
                {
                    dirSection = sectionLabels.Count;
                    sectionLabels.Add(SectionGameDir);
                }

                string label = LabelFor(groupClasses[i]);
                if (groupLabels.Contains(label))
                {
                    // 和 wiki 分类撞名（工具、材料这类），退到括号里区分
                    label += "（其他）";
                }
                int index = groupLabels.Count;
                dirIndex.Add(index);
                groupLabels.Add(label);
                groupSection.Add(dirSection);
                for (int j = 0; j < members.Count; j++)
                {
                    string id = members[j];
                    if (!string.IsNullOrEmpty(id) && !dirOf.ContainsKey(id))
                    {
                        dirOf[id] = index;
                    }
                }
            }
        }

        private static string LabelFor(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return "";
            }

            string key = className;
            if (key.EndsWith("ItemDirectory"))
            {
                key = key.Substring(0, key.Length - "ItemDirectory".Length);
            }
            else if (key.EndsWith("Directory"))
            {
                key = key.Substring(0, key.Length - "Directory".Length);
            }

            string zh;
            if (GroupZh.TryGetValue(key, out zh))
            {
                return zh;
            }
            // 没登记的目录类也不让它露出英文
            return "其他物品";
        }

        private static List<string> IdentifiersOf(string className)
        {
            List<string> list = new List<string>();
            try
            {
                List<string> raw = DirectoryMaster.GetIdentifierList<GameItem>(className);
                if (raw != null)
                {
                    list.AddRange(raw);
                }
            }
            catch
            {
                // 分类名失效时退回"全部分类"，不影响主流程
            }
            return list;
        }

        /// <summary>按大类 / 小类筛选。groupIndex &gt; 0 时按小类，否则按 sectionIndex 筛整个大类（0 = 全部）。</summary>
        public static List<string> Query(string text, int sectionIndex, int groupIndex)
        {
            Ensure();
            string filter = (text ?? "").Trim();
            Dictionary<string, int[]> map = (groupIndex > 0 || sectionIndex > 0) ? GroupMap() : null;

            List<string> result = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (map != null)
                {
                    int[] groups;
                    if (!map.TryGetValue(id, out groups) || !Hit(groups, sectionIndex, groupIndex))
                    {
                        continue;
                    }
                }
                if (filter.Length > 0 && !Matches(id, filter))
                {
                    continue;
                }
                result.Add(id);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static bool Hit(int[] groups, int sectionIndex, int groupIndex)
        {
            if (groups == null || groups.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < groups.Length; i++)
            {
                int g = groups[i];
                if (groupIndex > 0)
                {
                    if (g == groupIndex)
                    {
                        return true;
                    }
                }
                else if (sectionIndex > 0 && g < groupSection.Count && groupSection[g] == sectionIndex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>id → 一组分类序号（1 起，可以多个；空数组表示没归类）。懒建，随目录刷新失效。</summary>
        private static Dictionary<string, int[]> GroupMap()
        {
            if (groupOf != null)
            {
                return groupOf;
            }

            Ensure();
            groupOf = new Dictionary<string, int[]>(StringComparer.Ordinal);
            List<int> buffer = new List<int>();

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                buffer.Clear();

                // wiki 小分类优先，一件物品可以命中多个（植皮罗非鱼卵 = 食品 + 动物）
                string[] subs;
                if (wikiSubs != null && wikiSubs.TryGetValue(id, out subs))
                {
                    for (int j = 0; j < subs.Length; j++)
                    {
                        int index;
                        if (wikiGroupIndex.TryGetValue(subs[j], out index) && !buffer.Contains(index))
                        {
                            buffer.Add(index);
                        }
                    }
                }

                // wiki 没收录才退到游戏目录类
                if (buffer.Count == 0)
                {
                    int dir;
                    if (dirOf.TryGetValue(id, out dir) && dir > 0)
                    {
                        buffer.Add(dir);
                    }
                }

                groupOf[id] = buffer.ToArray();
            }
            return groupOf;
        }

        /// <summary>按当前搜索词统计每个小分类里有几件。长度同 GroupLabels，[0] 是全部。</summary>
        public static int[] GroupCounts(string text)
        {
            return GroupCounts(null, text);
        }

        /// <summary>同上，但只数 subset 里的 id（null = 整个目录）。编辑页只数台面上那几件。</summary>
        public static int[] GroupCounts(IList<string> subset, string text)
        {
            Ensure();
            Dictionary<string, int[]> map = GroupMap();
            int[] counts = new int[groupLabels.Count];
            string filter = (text ?? "").Trim();
            int total = subset == null ? ids.Count : subset.Count;
            for (int i = 0; i < total; i++)
            {
                string id = subset == null ? ids[i] : subset[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                if (filter.Length > 0 && !Matches(id, filter))
                {
                    continue;
                }
                counts[0]++;
                int[] groups;
                if (!map.TryGetValue(id, out groups))
                {
                    continue;
                }
                for (int j = 0; j < groups.Length; j++)
                {
                    int group = groups[j];
                    if (group > 0 && group < counts.Length)
                    {
                        counts[group]++;
                    }
                }
            }
            return counts;
        }

        /// <summary>按当前搜索词统计每个大分类里有几件。长度同 SectionLabels，[0] 是全部。</summary>
        public static int[] SectionCounts(string text)
        {
            return SectionCounts(null, text);
        }

        /// <summary>同上，但只数 subset 里的 id（null = 整个目录）。</summary>
        public static int[] SectionCounts(IList<string> subset, string text)
        {
            Ensure();
            Dictionary<string, int[]> map = GroupMap();
            int[] counts = new int[sectionLabels.Count];
            string filter = (text ?? "").Trim();
            int total = subset == null ? ids.Count : subset.Count;
            List<int> seen = new List<int>();
            for (int i = 0; i < total; i++)
            {
                string id = subset == null ? ids[i] : subset[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                if (filter.Length > 0 && !Matches(id, filter))
                {
                    continue;
                }
                counts[0]++;
                int[] groups;
                if (!map.TryGetValue(id, out groups))
                {
                    continue;
                }
                seen.Clear();
                for (int j = 0; j < groups.Length; j++)
                {
                    int g = groups[j];
                    if (g <= 0 || g >= groupSection.Count)
                    {
                        continue;
                    }
                    int s = groupSection[g];
                    if (s <= 0 || seen.Contains(s))
                    {
                        continue;
                    }
                    seen.Add(s);
                    if (s < counts.Length)
                    {
                        counts[s]++;
                    }
                }
            }
            return counts;
        }

        private static bool Matches(string id, string filter)
        {
            if (id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            string cached;
            if (names.TryGetValue(id, out cached) && !string.IsNullOrEmpty(cached) &&
                cached.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // 还没渲染过的格子没有缓存名；用词素表拼一个便宜的兜底名参与匹配（不建实例）
            if (!names.ContainsKey(id))
            {
                string guess = ItemNames.Zh(id);
                if (!string.IsNullOrEmpty(guess) && guess.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>单个 id 是否命中当前筛选（搜索词 + 大类 / 小类）。编辑页只有台面上那几件，逐件过比整目录筛便宜。</summary>
        public static bool InFilter(string id, string text, int sectionIndex, int groupIndex)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            string filter = (text ?? "").Trim();
            if (sectionIndex > 0 || groupIndex > 0)
            {
                int[] groups;
                if (!GroupMap().TryGetValue(id, out groups) || !Hit(groups, sectionIndex, groupIndex))
                {
                    return false;
                }
            }
            return filter.Length == 0 || Matches(id, filter);
        }

        /// <summary>取显示名。走游戏自己的缓存；游戏只给英文名时用本地词素表再兜一层。</summary>
        public static string NameOf(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "";
            }
            string cached;
            if (names.TryGetValue(id, out cached))
            {
                return cached;
            }

            string name = id;
            bool chinese = false;
            try
            {
                string display = GeneralHelper.GetDisplayNameFromID(id);
                if (!string.IsNullOrEmpty(display) && display != "[Unknown Item]" && display != "[Invalid ID]")
                {
                    name = display;
                    chinese = HasChinese(display);
                }
            }
            catch
            {
                // 留 identifier 兜底
            }

            if (!chinese)
            {
                string zh = ItemNames.Zh(id);
                if (!string.IsNullOrEmpty(zh))
                {
                    name = zh;
                }
            }

            names[id] = name;
            return name;
        }

        /// <summary>名字里有没有汉字。判 false 时才认为游戏没给中文。</summary>
        public static bool HasChinese(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            for (int i = 0; i < text.Length; i++)
            {
                int code = text[i];
                if (code >= 0x3400 && code <= 0x9FFF)
                {
                    return true;
                }
                if (code >= 0xF900 && code <= 0xFAFF)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasName(string id)
        {
            return !string.IsNullOrEmpty(id) && names.ContainsKey(id);
        }

        /// <summary>取图标。临时造一个实例取 sprite，然后连子物品一起销毁。</summary>
        public static Sprite IconOf(string id)
        {
            Sprite cached;
            if (icons.TryGetValue(id, out cached))
            {
                return cached;
            }

            Sprite sprite = null;
            GameItem probe = null;
            try
            {
                probe = DirectoryMaster.Create<GameItem>(id);
                descs[id] = ProbeDesc(probe);
                GameItemElement element = probe as GameItemElement;
                if (element != null && element.background != null && element.background.image != null)
                {
                    sprite = element.background.image.sprite;
                }
            }
            catch
            {
                sprite = null;
            }
            finally
            {
                if (probe != null)
                {
                    try
                    {
                        List<GameItem> children = GraphUtils.FindAllChildrenType<GameItem>(probe);
                        if (children != null)
                        {
                            for (int i = 0; i < children.Count; i++)
                            {
                                if (children[i] != null)
                                {
                                    children[i].Destroy();
                                }
                            }
                        }
                        probe.Destroy();
                    }
                    catch
                    {
                        // 清理失败不阻塞 UI
                    }
                }
            }

            icons[id] = sprite;
            return sprite;
        }

        /// <summary>取游戏原本的物品描述（shortDescription + flavorText）。只在 IconOf 造探针时顺手抓，不额外造实例。</summary>
        public static string DescOf(string id)
        {
            string desc;
            if (!string.IsNullOrEmpty(id) && descs.TryGetValue(id, out desc))
            {
                return desc ?? "";
            }
            return "";
        }

        /// <summary>从探针上读游戏原文描述。读不到就返回空串，不阻塞 UI。</summary>
        private static string ProbeDesc(GameItem probe)
        {
            if (probe == null)
            {
                return "";
            }

            string desc = "";
            string flavor = "";
            try
            {
                desc = probe.shortDescription ?? "";
            }
            catch
            {
                desc = "";
            }
            try
            {
                flavor = probe.flavorText ?? "";
            }
            catch
            {
                flavor = "";
            }

            desc = desc.Trim();
            flavor = flavor.Trim();
            if (flavor.Length == 0 || flavor == desc)
            {
                return desc;
            }
            if (desc.Length == 0)
            {
                return flavor;
            }
            return desc + "\n" + flavor;
        }

        public static void ClearCaches()
        {
            icons.Clear();
            descs.Clear();
            names.Clear();
            groupOf = null;
        }

        private static GraphNodeStorage NodeFor(int targetIndex)
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry == null)
            {
                return null;
            }

            switch (targetIndex)
            {
                case 0: return entry.invElement;
                case 1: return entry.frontInvinvElement;
                case 2: return entry.showcaseElement;
                case 3: return entry.backInvinvElement;
                default: return null;
            }
        }

        /// <summary>造一件还没进店的物品。失败返回 null。</summary>
        public static GameItem CreateDraft(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            try
            {
                return ItemSpawner.Spawn(id);
            }
            catch (Exception e)
            {
                Log.Error("造 " + id + " 失败: " + e.Message);
                return null;
            }
        }

        /// <summary>把一件已经造好的物品放进店里。返回空字符串表示成功。</summary>
        public static string Place(GameItem item, int targetIndex)
        {
            if (item == null)
            {
                return "没有可放置的物品";
            }
            try
            {
                GeneralHelper.SetItemOwned(item, true);

                GraphNodeStorage node = NodeFor(targetIndex);
                if (node != null && GraphUtils.CanAccept(node, item))
                {
                    int left = GraphUtils.TryAcceptAll(node, item);
                    if (left > 0)
                    {
                        GraphUtils.EmporiumTryAdd(item);
                    }
                }
                else
                {
                    GraphUtils.EmporiumTryAdd(item);
                }
            }
            catch (Exception e)
            {
                return "放置 " + item.identifier + " 时出错: " + e.Message;
            }

            return "";
        }

        /// <summary>生成物品。返回空字符串表示成功，否则是给玩家看的错误说明。</summary>
        public static string Spawn(string id, int count, int targetIndex)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "物品 ID 为空";
            }
            if (count < 1)
            {
                count = 1;
            }
            if (count > 9999)
            {
                count = 9999;
            }

            GameItem item = CreateDraft(id);
            if (item == null)
            {
                return "游戏里没有 id 为 " + id + " 的物品";
            }

            try
            {
                if (count > 1)
                {
                    item.SetStack(count);
                }
            }
            catch (Exception e)
            {
                return "设置数量失败: " + e.Message;
            }

            return Place(item, targetIndex);
        }

        /// <summary>台面上的物品：前台柜台 + 后柜台 + 展示柜（可选带上店内库存）。</summary>
        public static List<GameItem> CounterItems(bool includeStore)
        {
            List<GameItem> list = new List<GameItem>();
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry == null)
            {
                return list;
            }

            AddShallow(entry.frontInvinvElement, list);
            AddShallow(entry.backInvinvElement, list);
            AddShallow(entry.showcaseElement, list);
            if (includeStore)
            {
                AddShallow(entry.invElement, list);
            }
            return list;
        }

        private static void AddShallow(GameInventory inv, List<GameItem> list)
        {
            if (inv == null)
            {
                return;
            }

            List<GameItem> children = null;
            try
            {
                children = inv.childItems;
            }
            catch
            {
                return;
            }
            if (children == null)
            {
                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                GameItem item = children[i];
                if (item == null || list.Contains(item))
                {
                    continue;
                }
                list.Add(item);
            }
        }
    }
}
