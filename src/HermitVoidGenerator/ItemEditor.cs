using System;
using System.Collections.Generic;
using System.Globalization;

namespace HermitVoidGenerator
{
    /// <summary>界面上的原始输入，全部是字符串，交给 Apply 解析。</summary>
    internal sealed class EditInput
    {
        public string Name = "";
        public string CustomName = "";
        public string Count = "";
        public string UnitValue = "";
        public string BaseValue = "";
        public string LateValue = "";
        public string ShortDescription = "";
        public string CustomText = "";
        public string LongDescription = "";
        public string FlavorText = "";
        public bool BlankClears;
    }

    /// <summary>对选中的 GameItem 做字段改写 / 复制。</summary>
    internal static class ItemEditor
    {
        public static GameItem Target { get; private set; }

        public static void Select(GameItem item)
        {
            Target = item;
        }

        public static bool HasTarget
        {
            get { return Target != null; }
        }

        public static string Describe(GameItem item)
        {
            if (item == null)
            {
                return UiText.NoTarget;
            }

            string display = "";
            try
            {
                display = item.GetDisplayName(true);
            }
            catch
            {
                display = item.identifier;
            }

            string where = "不在任何容器里";
            try
            {
                if (item.parentInventory != null)
                {
                    where = string.IsNullOrEmpty(item.parentInventory.identifier)
                        ? item.parentInventory.GetType().Name
                        : item.parentInventory.identifier;
                }
            }
            catch
            {
                where = "未知位置";
            }

            long total = 0;
            try
            {
                total = item.GetValue(null, null, false);
            }
            catch
            {
                total = item.unitValue;
            }

            string owned = "";
            try
            {
                owned = GeneralHelper.IsItemOwned(item) ? "· 已归属" : "· 未归属";
            }
            catch
            {
                owned = "";
            }

            return display + "   [" + item.identifier + "]\n" +
                   "数量 " + item.unitCount + " · 单价 " + item.unitValue + " · 总值 " + total + " " + owned + "\n" +
                   "位置: " + where + " · uniqueId " + item.uniqueId;
        }

        public static string Apply(GameItem item, EditInput input)
        {
            if (item == null)
            {
                return "没有选中物品";
            }
            if (input == null)
            {
                return "没有读到界面输入";
            }

            int changed = 0;

            string name;
            if (WantText(input.Name, input.BlankClears, out name))
            {
                item.SetName(name);
                changed++;
            }

            string custom;
            if (WantText(input.CustomName, input.BlankClears, out custom))
            {
                GeneralHelper.SetCustomName(item, custom);
                changed++;
            }

            int count;
            string countError;
            if (TryInt(input.Count, 1, 99999, out count, out countError))
            {
                item.SetStack(count);
                changed++;
            }
            else if (countError != null)
            {
                return countError;
            }

            long value;
            string valueError;
            if (TryLong(input.UnitValue, out value, out valueError))
            {
                item.SetValue(value);
                changed++;
            }
            else if (valueError != null)
            {
                return valueError;
            }

            if (TryLong(input.BaseValue, out value, out valueError))
            {
                item.unitBaseValue = value;
                changed++;
            }
            else if (valueError != null)
            {
                return valueError;
            }

            if (TryLong(input.LateValue, out value, out valueError))
            {
                item.lateUnitValue = value;
                changed++;
            }
            else if (valueError != null)
            {
                return valueError;
            }

            string text;
            if (WantText(input.ShortDescription, input.BlankClears, out text))
            {
                item.shortDescription = text;
                changed++;
            }
            if (WantText(input.CustomText, input.BlankClears, out text))
            {
                item.customText = text;
                changed++;
            }
            if (WantText(input.LongDescription, input.BlankClears, out text))
            {
                item.longDescription = text;
                changed++;
            }
            if (WantText(input.FlavorText, input.BlankClears, out text))
            {
                item.flavorText = text;
                changed++;
            }

            try
            {
                item.Validate();
            }
            catch (Exception e)
            {
                return "改动已写入，但刷新显示时出错: " + e.Message;
            }

            return changed == 0 ? "没有可应用的改动" : "已写入 " + changed + " 项";
        }

        public static string ClearCustomName(GameItem item)
        {
            if (item == null)
            {
                return "没有选中物品";
            }
            try
            {
                GeneralHelper.RemoveCustomName(item);
                item.Validate();
                return "已清除自定义名";
            }
            catch (Exception e)
            {
                return "清除自定义名失败: " + e.Message;
            }
        }

        /// <summary>复制一件同款（带上所有被改过的字段），尽量放到原件旁边。</summary>
        public static string Duplicate(GameItem source)
        {
            if (source == null)
            {
                return "没有选中物品";
            }

            GameItem copy = null;
            try
            {
                copy = ItemSpawner.Spawn(source.identifier);
            }
            catch (Exception e)
            {
                return "复制失败: " + e.Message;
            }

            if (copy == null)
            {
                return "复制失败: 造不出 " + source.identifier;
            }

            try
            {
                copy.SetName(source.name);
                copy.shortDescription = source.shortDescription;
                copy.longDescription = source.longDescription;
                copy.flavorText = source.flavorText;
                copy.customText = source.customText;
                copy.unitValue = source.unitValue;
                copy.unitBaseValue = source.unitBaseValue;
                copy.lateUnitValue = source.lateUnitValue;
                copy.SetStack(source.unitCount < 1 ? 1 : source.unitCount);
                GeneralHelper.SetItemOwned(copy, true);

                if (GeneralHelper.HasCustomName(source))
                {
                    GeneralHelper.SetCustomName(copy, GeneralHelper.GetCustomName(source));
                }

                if (source.parentInventory != null && GraphUtils.CanAccept(source.parentInventory, copy))
                {
                    GraphUtils.TryAcceptAll(source.parentInventory, copy);
                }
                else
                {
                    GraphUtils.EmporiumTryAdd(copy);
                }
            }
            catch (Exception e)
            {
                return "复制出来的物品放置失败: " + e.Message;
            }

            return "已复制一件 " + (copy.GetDisplayName(true) ?? source.identifier);
        }

        private static bool WantText(string raw, bool blankClears, out string value)
        {
            value = raw ?? "";
            if (value.Length > 0)
            {
                return true;
            }
            if (blankClears)
            {
                value = "";
                return true;
            }
            return false;
        }

        private static bool TryInt(string raw, int min, int max, out int value, out string error)
        {
            value = 0;
            error = null;
            if (string.IsNullOrEmpty(raw) || raw.Trim().Length == 0)
            {
                return false;
            }
            int parsed;
            if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                error = "数量不是整数: " + raw;
                return false;
            }
            if (parsed < min)
            {
                parsed = min;
            }
            if (parsed > max)
            {
                parsed = max;
            }
            value = parsed;
            return true;
        }

        private static bool TryLong(string raw, out long value, out string error)
        {
            value = 0;
            error = null;
            if (string.IsNullOrEmpty(raw) || raw.Trim().Length == 0)
            {
                return false;
            }
            long parsed;
            if (!long.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                error = "价格不是整数: " + raw;
                return false;
            }
            if (parsed < 0)
            {
                parsed = 0;
            }
            value = parsed;
            return true;
        }
    }
}
