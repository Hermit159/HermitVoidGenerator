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
        private static List<string> groupLabels = new List<string> { "全部分类" };
        private static readonly Dictionary<string, string> names = new Dictionary<string, string>();
        private static Dictionary<string, int[]> groupOf;
        private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, string> descs = new Dictionary<string, string>();

        // 分类只有一层：游戏里悬停显示的那个标签本身（厨刀 = 武器 + 近战武器 + 日用品，
        // 所以它在三个标签下都出现）。大类那层已经拆掉。
        // sectionLabels / groupSection 留着是为了 Query / InFilter 的签名不变，实际只有「全部」这一项。
        public const string SectionAll = "全部大类";
        private static List<string> sectionLabels = new List<string> { SectionAll };
        private static List<int> groupSection = new List<int> { 0 };

        // 分类来源：游戏里悬停物品显示的那串标签（ItemTypeProbe 现读 itemTypes）。
        // 一件物品有几个标签就挂几个分组；一个标签都没有的，统一进「无标签」那一组。
        private static Dictionary<string, int> tagGroupIndex;
        private static readonly Dictionary<string, string> tagLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        private static int untaggedGroup;


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
            groupLabels = new List<string> { "全部分类" };
            sectionLabels = new List<string> { SectionAll };
            groupSection = new List<int> { 0 };
            tagLabels.Clear();

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

            BuildGroups();
            ItemTypeProbe.Dump(ids);

            Log.Info("目录已加载: " + ids.Count + " 件物品 / " + (groupLabels.Count - 1) + " 个标签（悬停标签探针读过 " + ItemTypeProbe.ProbedCount + " 件）");
        }

        /// <summary>
        /// 分类表：一层，就是游戏里悬停显示的那串标签。顺序照 ItemTagFamilies 的手工顺序来
        /// （常用的排在前面），没登记的新标签按字母排在后面。一个标签都没有的物品统一进「无标签」。
        /// </summary>
        private static void BuildGroups()
        {
            tagGroupIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            untaggedGroup = 0;
            groupOf = null;

            // 1) 本目录里实际出现过的标签（其余标签不占下拉位置）。
            //    标签从游戏里现读：造探针物品读 itemTypes，静态表只作兜底。
            ItemTypeProbe.Prewarm(ids);
            HashSet<string> usedTags = new HashSet<string>(StringComparer.Ordinal);
            bool anyUntagged = false;
            for (int i = 0; i < ids.Count; i++)
            {
                string[] tags = ItemTypeProbe.Of(ids[i]);
                if (tags.Length == 0)
                {
                    anyUntagged = true;
                    continue;
                }
                for (int j = 0; j < tags.Length; j++)
                {
                    usedTags.Add(tags[j]);
                }
            }

            // 2) 先按 ItemTagFamilies 的声明顺序排，剩下没登记的新标签按字母排在后面
            List<string> ordered = new List<string>();
            string[] sections = ItemTagFamilies.Sections;
            for (int s = 0; s < sections.Length; s++)
            {
                string[] tags = ItemTagFamilies.TagsOfSection(sections[s]);
                for (int k = 0; k < tags.Length; k++)
                {
                    if (usedTags.Contains(tags[k]))
                    {
                        ordered.Add(tags[k]);
                        usedTags.Remove(tags[k]);
                    }
                }
            }
            if (usedTags.Count > 0)
            {
                List<string> rest = new List<string>(usedTags);
                rest.Sort(StringComparer.Ordinal);
                ordered.AddRange(rest);
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                tagGroupIndex[ordered[i]] = groupLabels.Count;
                groupLabels.Add(TagLabel(ordered[i]));
                groupSection.Add(0);
            }

            // 3) 游戏里根本不给标签的物品：合成一个「无标签」组，不再按目录类细分
            if (anyUntagged)
            {
                untaggedGroup = groupLabels.Count;
                groupLabels.Add(ItemTagFamilies.SectionUntagged);
                groupSection.Add(0);
            }
        }

        /// <summary>标签的中文显示名：先向游戏要（本地化表 type_* 词条），拿不到再退 ItemTagFamilies 的兜底表。</summary>
        private static string TagLabel(string tag)
        {
            string cached;
            if (tagLabels.TryGetValue(tag, out cached))
            {
                return cached;
            }

            string label = "";
            try
            {
                label = TypeHelper.GetTypeDisplayName(tag);
            }
            catch
            {
                label = "";
            }

            if (string.IsNullOrEmpty(label) || label.IndexOf("type_", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label = ItemTagFamilies.ZhOf(tag);
            }
            if (string.IsNullOrEmpty(label))
            {
                label = tag;
            }

            tagLabels[tag] = label;
            return label;
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

                // 一件物品有几个悬停标签就挂几个小类（厨刀 = 武器 + 近战武器 + 日用品）
                string[] tags = ItemTypeProbe.Of(id);
                for (int j = 0; j < tags.Length; j++)
                {
                    int index;
                    if (tagGroupIndex != null && tagGroupIndex.TryGetValue(tags[j], out index) && !buffer.Contains(index))
                    {
                        buffer.Add(index);
                    }
                }

                // 一个标签都没有：统一挂到「无标签」那一组
                if (buffer.Count == 0 && untaggedGroup > 0)
                {
                    buffer.Add(untaggedGroup);
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

        /// <summary>大类已经拆掉了，只剩「全部」这一项；返回长度 1 的数组，[0] 是命中件数。</summary>
        public static int[] SectionCounts(string text)
        {
            return SectionCounts(null, text);
        }

        /// <summary>同上，但只数 subset 里的 id（null = 整个目录）。</summary>
        public static int[] SectionCounts(IList<string> subset, string text)
        {
            Ensure();
            int[] groups = GroupCounts(subset, text);
            int[] counts = new int[sectionLabels.Count];
            counts[0] = groups[0];
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

            // 悬停标签也参与搜索：搜「违禁」能捞出所有违禁品，搜「近战」能捞出所有近战武器
            string[] tags = ItemTypeProbe.Of(id);
            for (int i = 0; i < tags.Length; i++)
            {
                string label = TagLabel(tags[i]);
                if (label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
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

        /// <summary>一件物品的悬停标签（中文名），顺序和游戏里一样；没有标签返回空数组。</summary>
        public static string[] TagsOf(string id)
        {
            Ensure();
            string[] tags = ItemTypeProbe.Of(id);
            if (tags.Length == 0)
            {
                return tags;
            }

            string[] labels = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                labels[i] = TagLabel(tags[i]);
            }
            return labels;
        }

        /// <summary>
        /// 复刻游戏悬停的那行标签：每个标签写成 [中文名]，用的颜色也照游戏来
        /// （CommonTooltipHelper.CreateItemTypeTooltip：常规浅青，违禁品整行浅红）。
        /// 没有标签返回空串。
        /// </summary>
        public static string TagHint(string id)
        {
            Ensure();
            string[] tags = ItemTypeProbe.Of(id);
            if (tags.Length == 0)
            {
                return "";
            }

            bool contraband = false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], "CONTRABAND", StringComparison.Ordinal))
                {
                    contraband = true;
                    break;
                }
            }

            string text = "";
            for (int i = 0; i < tags.Length; i++)
            {
                if (i > 0)
                {
                    text += " ";
                }
                text += "[" + TagLabel(tags[i]) + "]";
            }

            string color = ((int)(contraband ? RenderHandler.ColorPalette.LightRed : RenderHandler.ColorPalette.LightCyan)).ToString("X6");
            return "<color=#" + color + ">" + text + "</color>";
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
