using System;
using System.Collections.Generic;
using System.Globalization;

namespace HermitVoidGenerator
{
    /// <summary>一条种子性状。游戏里就是 PLANT_TRAIT_&lt;id&gt; 这个整数槽，等级 0 等于没有。</summary>
    internal sealed class GeneRow
    {
        public string Id = "";
        public string Display = "";
        public string Effect = "";
        public string RankText = "";
        public int Rank;
        public int MaxRank = 1;
        public bool Unlimited;
        public bool Addable;
        public string BlockReason = "";
    }

    /// <summary>一颗种子当前的基因状态。</summary>
    internal sealed class GeneInfo
    {
        public bool IsSeed;
        public string PlantId = "";
        public string PlantName = "";
        public bool Modified;
        public int Complexity;
        public int Heat;

        public readonly List<GeneRow> Owned = new List<GeneRow>();
        public readonly List<GeneRow> Missing = new List<GeneRow>();

        /// <summary>DNA 操作真正看的复杂度：复杂度 + 基因热量。</summary>
        public int Effective { get { return Complexity + Heat; } }
    }

    /// <summary>
    /// 种子基因读写。性状是 PLANT_TRAIT_&lt;id&gt; 整数槽，写值必须走
    /// PlantGeneticsHelper.SetTraitRank —— 它顺手刷新 MODIFIED_SEED 物品类型和营养果的品系，
    /// 自己动 tag 会漏这两步，界面上看着就不对。
    /// </summary>
    internal static class Genes
    {
        public const string TagSeed = "SEED_ITEM_TAG";
        public const string TagPlantId = "PLANT_ID_STRING";
        public const string TagHeat = "GENETIC_HEAT_INT";
        public const string TraitTagPrefix = "PLANT_TRAIT_";

        /// <summary>等级填写的上限。游戏里「无上限」的性状实际上也没人堆过一千级。</summary>
        public const int RankCap = 999;

        public static bool IsSeed(GameItem item)
        {
            if (item == null)
            {
                return false;
            }
            try
            {
                return item.IsTag(TagSeed);
            }
            catch
            {
                return false;
            }
        }

        public static string PlantIdOf(GameItem item)
        {
            try
            {
                if (item == null || !item.IsTag(TagPlantId))
                {
                    return "";
                }
                string id = item.GetTagReadonly(TagPlantId).GetString();
                return id == null ? "" : id;
            }
            catch
            {
                return "";
            }
        }

        public static PlantDefinition PlantOf(GameItem item)
        {
            try
            {
                string id = PlantIdOf(item);
                return string.IsNullOrEmpty(id) ? null : PlantRegistry.Get(id);
            }
            catch
            {
                return null;
            }
        }

        public static string PlantNameOf(GameItem item)
        {
            PlantDefinition plant = PlantOf(item);
            if (plant == null)
            {
                string raw = PlantIdOf(item);
                return string.IsNullOrEmpty(raw) ? "（未知品种）" : raw;
            }
            if (!string.IsNullOrEmpty(plant.displayName))
            {
                return plant.displayName;
            }
            return plant.id;
        }

        public static int RankOf(GameItem item, string traitId)
        {
            try
            {
                return item == null ? 0 : PlantGeneticsHelper.GetTraitRank(item, traitId);
            }
            catch
            {
                return 0;
            }
        }

        public static int HeatOf(GameItem item)
        {
            try
            {
                return item == null ? 0 : PlantGeneticsHelper.GetHeat(item);
            }
            catch
            {
                return 0;
            }
        }

        public static string RankLabel(int rank)
        {
            try
            {
                return PlantGeneticsHelper.RankLabel(rank);
            }
            catch
            {
                return rank.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>整颗种子的基因现状，给面板用。</summary>
        public static GeneInfo Describe(GameItem item)
        {
            GeneInfo info = new GeneInfo();
            if (item == null)
            {
                return info;
            }

            info.IsSeed = IsSeed(item);
            info.PlantId = PlantIdOf(item);
            info.PlantName = PlantNameOf(item);
            if (!info.IsSeed)
            {
                return info;
            }

            PlantDefinition plant = PlantOf(item);
            try
            {
                info.Modified = PlantGeneticsHelper.IsModified(item);
            }
            catch (Exception e)
            {
                Log.Warn("判定变异失败: " + e.Message);
            }
            try
            {
                info.Complexity = PlantGeneticsHelper.GetComplexity(item);
                info.Heat = PlantGeneticsHelper.GetHeat(item);
            }
            catch (Exception e)
            {
                Log.Warn("读取基因复杂度失败: " + e.Message);
            }

            IReadOnlyList<PlantTrait> all = null;
            try
            {
                all = PlantTraitRegistry.All;
            }
            catch (Exception e)
            {
                Log.Warn("读取性状表失败: " + e.Message);
            }
            if (all == null)
            {
                return info;
            }

            for (int i = 0; i < all.Count; i++)
            {
                PlantTrait trait = all[i];
                if (trait == null || string.IsNullOrEmpty(trait.id))
                {
                    continue;
                }

                GeneRow row = RowOf(item, trait, plant);
                if (row.Rank > 0)
                {
                    info.Owned.Add(row);
                }
                else
                {
                    info.Missing.Add(row);
                }
            }

            info.Missing.Sort(CompareMissing);
            return info;
        }

        /// <summary>把 from 的全套基因抄到 to 上。同品种才抄，返回空串表示抄完。</summary>
        public static string Copy(GameItem from, GameItem to)
        {
            if (!IsSeed(from) || !IsSeed(to) || from == to)
            {
                return "";
            }
            if (!string.Equals(PlantIdOf(from), PlantIdOf(to), StringComparison.Ordinal))
            {
                return "";
            }

            try
            {
                IReadOnlyList<PlantTrait> all = PlantTraitRegistry.All;
                for (int i = 0; i < all.Count; i++)
                {
                    PlantTrait trait = all[i];
                    if (trait == null || string.IsNullOrEmpty(trait.id))
                    {
                        continue;
                    }
                    PlantGeneticsHelper.SetTraitRank(to, trait.id, RankOf(from, trait.id));
                }
                SetHeat(to, HeatOf(from));
                return "";
            }
            catch (Exception e)
            {
                Log.Warn("复制基因失败: " + e.Message);
                return "复制基因失败：" + e.Message;
            }
        }

        /// <summary>写一个性状的等级。0 等于去掉，返回空串表示成功。</summary>
        public static string SetRank(GameItem item, string traitId, int rank)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (!IsSeed(item))
            {
                return "这不是种子，没有基因可改";
            }
            if (string.IsNullOrEmpty(traitId))
            {
                return "没有这个性状";
            }

            if (rank < 0)
            {
                rank = 0;
            }
            if (rank > RankCap)
            {
                rank = RankCap;
            }

            try
            {
                PlantGeneticsHelper.SetTraitRank(item, traitId, rank);
                return "";
            }
            catch (Exception e)
            {
                Log.Warn("写性状失败: " + e.Message);
                return "写性状失败：" + e.Message;
            }
        }

        public static string SetHeat(GameItem item, int heat)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (heat < 0)
            {
                heat = 0;
            }

            try
            {
                int amount = heat;
                item.ModifyTag(TagHeat, delegate(TagState s)
                {
                    s.Enable().SetInt(amount);
                });
                return "";
            }
            catch (Exception e)
            {
                Log.Warn("写基因热量失败: " + e.Message);
                return "写基因热量失败：" + e.Message;
            }
        }

        /// <summary>把这颗种子洗回品种自带的那套先天性状，热量清零。</summary>
        public static string Reset(GameItem item)
        {
            if (!IsSeed(item))
            {
                return "这不是种子，没有基因可改";
            }
            PlantDefinition plant = PlantOf(item);
            if (plant == null)
            {
                return "读不到这个品种的定义";
            }

            try
            {
                IReadOnlyList<PlantTrait> all = PlantTraitRegistry.All;
                for (int i = 0; i < all.Count; i++)
                {
                    PlantTrait trait = all[i];
                    if (trait == null || string.IsNullOrEmpty(trait.id))
                    {
                        continue;
                    }
                    PlantGeneticsHelper.SetTraitRank(item, trait.id, plant.GetInnateRank(trait.id));
                }
                PlantGeneticsHelper.ClearHeat(item);
                return "";
            }
            catch (Exception e)
            {
                Log.Warn("恢复原种失败: " + e.Message);
                return "恢复原种失败：" + e.Message;
            }
        }

        private static GeneRow RowOf(GameItem item, PlantTrait trait, PlantDefinition plant)
        {
            GeneRow row = new GeneRow();
            row.Id = trait.id;
            row.Display = TraitLabel(trait);
            row.Rank = RankOf(item, trait.id);
            row.Unlimited = trait.IsUnlimited;
            row.MaxRank = row.Unlimited ? RankCap : trait.maxRank;
            if (row.MaxRank < 1)
            {
                row.MaxRank = 1;
            }
            if (row.Rank > 0)
            {
                row.RankText = RankLabel(row.Rank);
            }

            try
            {
                row.Effect = PlantGeneticsHelper.DescribeEffect(trait, Math.Max(1, row.Rank));
            }
            catch (Exception e)
            {
                Log.Warn("读性状说明失败: " + e.Message);
            }

            if (row.Rank > 0 || plant == null)
            {
                if (row.Rank <= 0)
                {
                    row.BlockReason = "读不到这个品种的定义";
                }
                return row;
            }

            string reason = "";
            bool addable = false;
            try
            {
                addable = PlantGeneticsHelper.CanApplyTrait(item, trait, out reason);
            }
            catch (Exception e)
            {
                Log.Warn("判定性状可加性失败: " + e.Message);
            }

            row.Addable = addable;
            if (!addable)
            {
                row.BlockReason = string.IsNullOrEmpty(reason) ? "游戏本体不让加这个性状" : reason;
            }
            return row;
        }

        private static string TraitLabel(PlantTrait trait)
        {
            string text = null;
            try
            {
                text = trait.displayName;
            }
            catch
            {
                text = null;
            }
            if (string.IsNullOrEmpty(text))
            {
                text = trait.englishName;
            }
            if (string.IsNullOrEmpty(text))
            {
                text = trait.id;
            }
            return text;
        }

        private static int CompareMissing(GeneRow a, GeneRow b)
        {
            if (a.Addable != b.Addable)
            {
                return a.Addable ? -1 : 1;
            }
            return string.CompareOrdinal(a.Display, b.Display);
        }
    }
}
