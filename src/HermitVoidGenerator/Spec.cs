using System;
using System.Collections.Generic;
using System.Globalization;

namespace HermitVoidGenerator
{
    /// <summary>一种液体的含量（单位：微升，1000 = 1 mL）。</summary>
    internal sealed class LiquidSlot
    {
        public string Id = "";
        public string Display = "";
        public int Parts;
    }

    /// <summary>某件物品当前的机制性规格快照。</summary>
    internal sealed class SpecInfo
    {
        public bool IsChem;
        public bool IsContainer;
        public bool IsChemProduct;
        public bool IsIngot;

        public int CapacityParts;
        public int TotalParts;

        public string ActiveIngredient = "";
        public string ActiveIngredientDisplay = "";
        public double ChemPurity;
        public double WaterPurity;
        public string IngotPurity = "";

        public readonly List<LiquidSlot> Slots = new List<LiquidSlot>();

        /// <summary>是否有液体成分可填。</summary>
        public bool EditsLiquid { get { return IsChem || IsContainer || IsChemProduct; } }

        /// <summary>这件物品有没有任何规格可改。</summary>
        public bool HasSpec { get { return EditsLiquid || IsIngot; } }
    }

    /// <summary>规格读写：液体成分 / 水纯度 / 化学制品纯度 / 金属锭纯度。</summary>
    internal static class Spec
    {
        public const string TagChem = "CHEM_ITEM_TAG";
        public const string TagContainer = "LIQUID_CONTAINER_TAG";
        public const string TagCapacity = "LIQUID_CONTAINER_CAPACITY";
        public const string TagChemProduct = "CHEMICAL_PRODUCT_TAG";
        public const string TagChemPurity = "CHEMICAL_PRODUCT_PURITY_TAG";
        public const string TagActive = "ACTIVATE_INGREDIENT_TAG";
        public const string TagIngot = "INGOT_PURITY_TAG";
        public const string TagWineContainer = "WINE_CONTAINER_TAG";

        public const string LiquidWater = "water";
        public const string LiquidStarch = "starch";

        /// <summary>微升上限，防止手滑写进天文数字把总价撑爆。</summary>
        public const int MaxParts = 100000000;

        public static readonly string[] IngotTags =
        {
            "INGOT_PURITY_PERFECT", "INGOT_PURITY_VERY_HIGH", "INGOT_PURITY_HIGH",
            "INGOT_PURITY_FAIR", "INGOT_PURITY_LOW", "INGOT_PURITY_VERY_LOW",
        };

        public static readonly string[] IngotLabels =
        {
            "完美纯度", "极高纯度", "高纯度", "普通纯度", "低纯度", "极低纯度",
        };

        private static List<Liquid> liquids;

        public static List<Liquid> Liquids
        {
            get
            {
                if (liquids == null || liquids.Count == 0)
                {
                    try
                    {
                        liquids = Liquid.Liquids;
                    }
                    catch (Exception e)
                    {
                        Log.Error("读取液体表失败: " + e.Message);
                    }
                }
                return liquids ?? new List<Liquid>();
            }
        }

        public static Liquid Find(string liquidId)
        {
            if (string.IsNullOrEmpty(liquidId))
            {
                return null;
            }
            List<Liquid> all = Liquids;
            for (int i = 0; i < all.Count; i++)
            {
                Liquid l = all[i];
                if (l != null && l.identifier == liquidId)
                {
                    return l;
                }
            }
            return null;
        }

        public static string DisplayOf(string liquidId)
        {
            Liquid l = Find(liquidId);
            if (l == null)
            {
                return liquidId;
            }
            return string.IsNullOrEmpty(l.displayName) ? l.identifier : l.displayName;
        }

        /// <summary>微升 -> "12.5 mL"。</summary>
        public static string FormatMl(int parts)
        {
            return (parts / 1000.0).ToString("0.###", CultureInfo.InvariantCulture) + " mL";
        }

        public static string FormatPct(double percent)
        {
            return percent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        public static SpecInfo Describe(GameItem item)
        {
            SpecInfo info = new SpecInfo();
            if (item == null)
            {
                return info;
            }

            info.IsChem = HasTag(item, TagChem);
            info.IsContainer = HasTag(item, TagContainer);
            info.IsChemProduct = HasTag(item, TagChemProduct);
            info.IsIngot = HasTag(item, TagIngot);
            if (info.IsContainer)
            {
                info.CapacityParts = TagInt(item, TagCapacity);
            }

            int total = 0;
            int water = 0;
            List<Liquid> all = Liquids;
            for (int i = 0; i < all.Count; i++)
            {
                Liquid l = all[i];
                if (l == null || string.IsNullOrEmpty(l.currentPart))
                {
                    continue;
                }
                int parts = TagInt(item, l.currentPart);
                if (parts <= 0)
                {
                    continue;
                }
                total += parts;
                if (l.identifier == LiquidWater)
                {
                    water = parts;
                }
                LiquidSlot slot = new LiquidSlot();
                slot.Id = l.identifier;
                slot.Display = string.IsNullOrEmpty(l.displayName) ? l.identifier : l.displayName;
                slot.Parts = parts;
                info.Slots.Add(slot);
            }
            info.TotalParts = total;

            if (info.IsChemProduct)
            {
                info.ActiveIngredient = TagString(item, TagActive);
                info.ActiveIngredientDisplay = DisplayOf(info.ActiveIngredient);
                info.ChemPurity = TagDouble(item, TagChemPurity);
            }
            if (info.IsContainer && total > 0)
            {
                info.WaterPurity = water * 100.0 / total;
            }
            if (info.IsIngot)
            {
                info.IngotPurity = TagString(item, TagIngot);
            }

            return info;
        }

        public static int PartsOf(GameItem item, string liquidId)
        {
            Liquid l = Find(liquidId);
            if (l == null)
            {
                return 0;
            }
            return TagInt(item, l.currentPart);
        }

        /// <summary>设定一种液体的绝对含量（微升）。返回空串表示成功。</summary>
        public static string SetLiquid(GameItem item, string liquidId, int parts)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (Find(liquidId) == null)
            {
                return "游戏里没有液体 " + liquidId;
            }
            if (parts < 0)
            {
                parts = 0;
            }
            if (parts > MaxParts)
            {
                parts = MaxParts;
            }
            int capacity = TagInt(item, TagCapacity);
            if (capacity > 0 && parts > capacity)
            {
                parts = capacity;
            }
            try
            {
                ChemicalHelper.SetChemContentUL(item, liquidId, parts);
                Refresh(item);
            }
            catch (Exception e)
            {
                return "写成分失败: " + e.Message;
            }
            return "";
        }

        /// <summary>清空全部液体成分。</summary>
        public static string ClearLiquids(GameItem item)
        {
            if (item == null)
            {
                return "没有物品";
            }
            try
            {
                List<Liquid> all = Liquids;
                for (int i = 0; i < all.Count; i++)
                {
                    Liquid l = all[i];
                    if (l == null || string.IsNullOrEmpty(l.currentPart))
                    {
                        continue;
                    }
                    if (TagInt(item, l.currentPart) <= 0)
                    {
                        continue;
                    }
                    ChemicalHelper.SetChemContentUL(item, l.identifier, 0);
                }
                Refresh(item);
            }
            catch (Exception e)
            {
                return "清空成分失败: " + e.Message;
            }
            return "";
        }

        /// <summary>用清水填到容器容量（没有容量信息时填到 1000 mL）。</summary>
        public static string FillWater(GameItem item)
        {
            if (item == null)
            {
                return "没有物品";
            }
            SpecInfo info = Describe(item);
            int target = info.CapacityParts > 0 ? info.CapacityParts : 1000000;
            return SetLiquid(item, LiquidWater, target);
        }

        /// <summary>改水的纯度（水占液体总量的百分比），其余成分按比例缩放。</summary>
        public static string SetWaterPurity(GameItem item, double percent)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (!HasTag(item, TagContainer))
            {
                return "这件物品不是液体容器";
            }
            int total = Describe(item).TotalParts;
            if (total <= 0)
            {
                return "容器是空的，先填点液体";
            }
            Liquid water = Find(LiquidWater);
            if (water == null)
            {
                return "液体表里没有水";
            }
            if (percent < 0)
            {
                percent = 0;
            }
            if (percent > 100)
            {
                percent = 100;
            }

            int waterParts = (int)Math.Round(total * percent / 100.0);
            if (waterParts > total)
            {
                waterParts = total;
            }
            if (waterParts < 0)
            {
                waterParts = 0;
            }

            try
            {
                List<KeyValuePair<string, int>> others = OtherLiquids(item, LiquidWater);
                ChemicalHelper.SetChemContentUL(item, water.identifier, waterParts);
                ScaleOthers(item, others, total - waterParts);
                Refresh(item);
            }
            catch (Exception e)
            {
                return "改水纯度失败: " + e.Message;
            }
            return "";
        }

        /// <summary>改化学制品的纯度（主成分占液体总量的百分比）。</summary>
        public static string SetChemPurity(GameItem item, double percent)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (!HasTag(item, TagChemProduct))
            {
                return "这件物品不是化学制品";
            }
            string active = TagString(item, TagActive);
            if (Find(active) == null)
            {
                return "制品缺少主成分标记，先设置主成分";
            }
            int total = Describe(item).TotalParts;
            if (total <= 0)
            {
                return "制品里没有液体成分";
            }
            if (percent < 0)
            {
                percent = 0;
            }
            if (percent > 100)
            {
                percent = 100;
            }

            int activeParts = (int)Math.Round(total * percent / 100.0);
            if (activeParts > total)
            {
                activeParts = total;
            }
            int rest = total - activeParts;

            try
            {
                List<KeyValuePair<string, int>> others = OtherLiquids(item, active);
                ChemicalHelper.SetChemContentUL(item, active, activeParts);
                if (others.Count > 0)
                {
                    ScaleOthers(item, others, rest);
                }
                else if (rest > 0 && Find(LiquidStarch) != null)
                {
                    ChemicalHelper.SetChemContentUL(item, LiquidStarch, rest);
                }
                Refresh(item);
            }
            catch (Exception e)
            {
                return "改纯度失败: " + e.Message;
            }
            return "";
        }

        /// <summary>换化学制品的主成分（决定纯度按谁算）。</summary>
        public static string SetActiveIngredient(GameItem item, string liquidId)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (!HasTag(item, TagChemProduct))
            {
                return "这件物品不是化学制品";
            }
            if (Find(liquidId) == null)
            {
                return "游戏里没有液体 " + liquidId;
            }
            try
            {
                item.ModifyTag(TagActive, delegate(TagState s)
                {
                    s.Enable();
                    s.SetString(liquidId);
                });
                Refresh(item);
            }
            catch (Exception e)
            {
                return "换主成分失败: " + e.Message;
            }
            return "";
        }

        public static string SetIngotPurity(GameItem item, string purityTag)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (!HasTag(item, TagIngot))
            {
                return "这件物品不是金属锭";
            }
            try
            {
                if (item.FindItemFeatureByID(TagIngot) == null)
                {
                    IngotPurityHelper.InitIngotItem(item);
                }
                IngotPurityHelper.SetPurity(item, purityTag);
            }
            catch (Exception e)
            {
                return "改金属纯度失败: " + e.Message;
            }
            return "";
        }

        /// <summary>改完成分/纯度后，让游戏自己重算价格、特征、显示文字。</summary>
        public static void Refresh(GameItem item)
        {
            if (item == null)
            {
                return;
            }
            try
            {
                if (HasTag(item, TagChemProduct) && Describe(item).TotalParts > 0)
                {
                    ChemicalProductHelper.GradeProduct(item);
                }
            }
            catch (Exception e)
            {
                Log.Warn("重算化学纯度失败: " + e.Message);
            }

            if (!HasTag(item, TagContainer) || HasTag(item, TagWineContainer))
            {
                return;
            }
            try
            {
                WaterHelper.UpdateContainerGoodType(item);
            }
            catch (Exception e)
            {
                Log.Warn("更新容器类型失败: " + e.Message);
            }
            try
            {
                WaterFeatureHelper.InitWaterFeature(item, false);
            }
            catch (Exception e)
            {
                Log.Warn("刷新水特征失败: " + e.Message);
            }
        }

        private static List<KeyValuePair<string, int>> OtherLiquids(GameItem item, string skipId)
        {
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
            List<Liquid> all = Liquids;
            for (int i = 0; i < all.Count; i++)
            {
                Liquid l = all[i];
                if (l == null || string.IsNullOrEmpty(l.currentPart) || l.identifier == skipId)
                {
                    continue;
                }
                int parts = TagInt(item, l.currentPart);
                if (parts <= 0)
                {
                    continue;
                }
                list.Add(new KeyValuePair<string, int>(l.identifier, parts));
            }
            return list;
        }

        /// <summary>把其余成分按比例缩放到合计 target，最后一项吸收取整误差。</summary>
        private static void ScaleOthers(GameItem item, List<KeyValuePair<string, int>> others, int target)
        {
            if (others == null || others.Count == 0)
            {
                return;
            }
            if (target < 0)
            {
                target = 0;
            }

            int sum = 0;
            for (int i = 0; i < others.Count; i++)
            {
                sum += others[i].Value;
            }
            if (sum <= 0)
            {
                return;
            }

            int allocated = 0;
            for (int i = 0; i < others.Count; i++)
            {
                int value;
                if (i == others.Count - 1)
                {
                    value = target - allocated;
                }
                else
                {
                    value = (int)Math.Round(others[i].Value * (double)target / sum);
                }
                if (value < 0)
                {
                    value = 0;
                }
                allocated += value;
                ChemicalHelper.SetChemContentUL(item, others[i].Key, value);
            }
        }

        private static bool HasTag(GameItem item, string tag)
        {
            if (item == null || string.IsNullOrEmpty(tag))
            {
                return false;
            }
            try
            {
                return item.IsTag(tag);
            }
            catch
            {
                return false;
            }
        }

        private static int TagInt(GameItem item, string tag)
        {
            if (item == null || string.IsNullOrEmpty(tag))
            {
                return 0;
            }
            try
            {
                TagState state = item.GetTagReadonly(tag);
                return state == null ? 0 : state.GetInt();
            }
            catch
            {
                return 0;
            }
        }

        private static double TagDouble(GameItem item, string tag)
        {
            if (item == null || string.IsNullOrEmpty(tag))
            {
                return 0;
            }
            try
            {
                TagState state = item.GetTagReadonly(tag);
                return state == null ? 0 : state.GetDouble();
            }
            catch
            {
                return 0;
            }
        }

        private static string TagString(GameItem item, string tag)
        {
            if (item == null || string.IsNullOrEmpty(tag))
            {
                return "";
            }
            try
            {
                TagState state = item.GetTagReadonly(tag);
                if (state == null || !state.IsEnabled())
                {
                    return "";
                }
                return state.GetString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
