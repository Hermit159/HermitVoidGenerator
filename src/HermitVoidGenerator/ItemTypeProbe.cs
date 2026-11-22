using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 物品悬停标签的真实来源。玩家把鼠标挪到物品上看到的那串 [医疗品] [食物]，就是
    /// GameItem.itemTypes —— CommonTooltipHelper.CreateItemTypeTooltip 直接遍历
    /// item.GetGameItemType() 逐个拼 [TypeHelper.GetTypeDisplayName(x)]。而 itemTypes 是各个
    /// 物品目录的工厂里 SetGameItemType 一点点攒出来的，静态扫源码会漏：鱼种/鱼卵这类 id 是
    /// FishSpecies 字段拼出来的，工厂里还会先 Set 后 Remove 再 Set（植皮罗非鱼卵 = MEDICAL,FOOD）。
    ///
    /// 所以这里不猜 —— 直接在游戏里造一个探针物品，把它自己的 itemTypes 读出来。读到的就是
    /// 玩家看到的，包括先后顺序。探针用完立刻销毁（和 Catalog.IconOf 同一套清理流程）。
    /// 静态表 ItemTags 只在万探针造不出来时兜底。
    /// </summary>
    internal static class ItemTypeProbe
    {
        private static readonly Dictionary<string, string[]> cache = new Dictionary<string, string[]>(StringComparer.Ordinal);
        private static readonly string[] none = new string[0];
        private static readonly HashSet<string> failed = new HashSet<string>(StringComparer.Ordinal);
        private static bool dumped;

        /// <summary>已经探过的物品数（含没标签的）。</summary>
        public static int ProbedCount
        {
            get { return cache.Count; }
        }

        /// <summary>一件物品的标签常量数组，游戏内顺序；没有标签返回空数组（不返回 null）。</summary>
        public static string[] Of(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return none;
            }

            string[] cached;
            if (cache.TryGetValue(id, out cached))
            {
                return cached;
            }

            string[] types = Probe(id);
            if (types == null)
            {
                failed.Add(id);
                // 探针抛异常：退回静态表（可能也没有，那就当无标签）
                types = ItemTags.Of(id);
            }

            cache[id] = types;
            return types;
        }

        /// <summary>批量预热，顺手把耗时写进日志。UI 打开前跑一遍，后面全是字典命中。</summary>
        public static void Prewarm(IList<string> ids)
        {
            if (ids == null || ids.Count == 0 || cache.Count > 0)
            {
                return;
            }

            Stopwatch watch = Stopwatch.StartNew();
            int tagged = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (Of(ids[i]).Length > 0)
                {
                    tagged++;
                }
            }
            watch.Stop();
            Log.Info("物品标签探针：读过 " + ids.Count + " 件（" + tagged + " 件有标签），耗时 " + watch.ElapsedMilliseconds + " ms");
        }

        /// <summary>
        /// 把整张表写到 dll 旁边的 ItemTagDump.txt，用来核对哪些物品真的没标签、哪些探针造不出来。
        /// 排错用的，一次会话只写一遍。
        /// </summary>
        public static void Dump(IList<string> ids)
        {
            if (dumped || ids == null || ids.Count == 0)
            {
                return;
            }
            dumped = true;

            try
            {
                StringBuilder text = new StringBuilder();
                int tagged = 0;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (Of(ids[i]).Length > 0)
                    {
                        tagged++;
                    }
                }
                text.AppendLine("# 物品悬停标签（运行时探针读 GameItem.itemTypes）");
                text.AppendLine("# 共 " + ids.Count + " 件：" + tagged + " 件有标签 / " + (ids.Count - tagged) + " 件无标签 / " + failed.Count + " 件探针失败");
                text.AppendLine("# id\t标签（游戏内顺序）\t探针状态");
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    string[] types = Of(id);
                    text.Append(id).Append('\t');
                    for (int j = 0; j < types.Length; j++)
                    {
                        if (j > 0)
                        {
                            text.Append(',');
                        }
                        text.Append(types[j]);
                    }
                    text.Append('\t').AppendLine(types.Length == 0 ? (failed.Contains(id) ? "探针失败" : "游戏无标签") : "ok");
                }

                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = ".";
                }
                File.WriteAllText(Path.Combine(dir, "ItemTagDump.txt"), text.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Log.Warn("写标签清单失败: " + e.Message);
            }
        }

        /// <summary>造一次探针读 itemTypes。造不出来返回 null（调用方退静态表）。</summary>
        private static string[] Probe(string id)
        {
            GameItem probe = null;
            string[] result = null;
            try
            {
                // 和买东西时走同一条路：ItemSpawner.Spawn 会先查 PreBuiltItemHelper.preBuiltItems，
                // 再退到 DirectoryMaster.Item —— 这样拿到的就是玩家手里那件东西的标签。
                probe = ItemSpawner.Spawn(id);
                if (probe == null)
                {
                    probe = DirectoryMaster.Create<GameItem>(id);
                }
                if (probe == null)
                {
                    failed.Add(id);
                }
                if (probe != null)
                {
                    List<string> types = probe.GetGameItemType();
                    if (types == null || types.Count == 0)
                    {
                        result = none;
                    }
                    else
                    {
                        result = new string[types.Count];
                        for (int i = 0; i < types.Count; i++)
                        {
                            result[i] = types[i];
                        }
                    }
                }
            }
            catch
            {
                result = null;
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

            return result;
        }
    }
}
