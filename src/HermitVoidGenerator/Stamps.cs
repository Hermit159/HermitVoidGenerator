using System;
using System.Globalization;

namespace HermitVoidGenerator
{
    /// <summary>配给券（food_stamp）：面值决定在配给站能换多少额度，真伪决定能不能换。</summary>
    internal static class Stamps
    {
        public const string TagStamp = "FOOD_STAMP_TAG";
        public const string TagAmount = "STAMP_AMOUNT";
        public const string CategoryGenuine = "CATEGORY_GENUINE_STAMP";
        public const string ConditionCounterfeit = "counterfeitStamp";
        public const string ItemId = "food_stamp";
        public const int MinAmount = 1;
        public const int MaxAmount = 1000000;

        public static readonly string[] AuthLabels = { "真券", "假券（可被当场揭穿）" };

        private static readonly string[] IssueTags =
        {
            "STAMP_SEAL_ISSUE", "STAMP_LOGO_ISSUE", "STAMP_CHIP_ISSUE",
            "STAMP_LETTERING_ISSUE", "STAMP_AMOUNT_ISSUE"
        };

        public static bool IsStamp(GameItem item)
        {
            if (item == null)
            {
                return false;
            }
            try
            {
                return item.IsTag(TagStamp) || item.identifier == ItemId;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int Amount(GameItem item)
        {
            if (item == null)
            {
                return 0;
            }
            try
            {
                return item.GetTagReadonly(TagAmount).GetInt();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string AmountText(GameItem item)
        {
            int amount = Amount(item);
            return amount > 0 ? amount.ToString(CultureInfo.InvariantCulture) : "";
        }

        /// <summary>真券：真伪两个条件都是 genuineStamp。假券的 realCondition 也是真券，要等揭穿才翻。</summary>
        public static bool IsGenuine(GameItem item)
        {
            if (item == null)
            {
                return true;
            }
            try
            {
                ItemFeature feature = item.FindItemFeatureByCategory(CategoryGenuine);
                if (feature == null || feature.realCondition == null)
                {
                    return true;
                }
                return feature.realCondition.identifier != ConditionCounterfeit;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static int AuthIndex(GameItem item)
        {
            return IsGenuine(item) ? 0 : 1;
        }

        /// <summary>写面值与真伪。真券走 SetGenuine，假券走 SetCounterfeit（会随机挑一处防伪特征对不上）。</summary>
        public static string Apply(GameItem item, string amountText, int authIndex)
        {
            if (!IsStamp(item))
            {
                return "这件物品不是配给券";
            }

            int amount;
            string error = ParseAmount(amountText, out amount);
            if (error != null)
            {
                return error;
            }

            bool genuine = authIndex != 1;
            try
            {
                for (int i = 0; i < IssueTags.Length; i++)
                {
                    try
                    {
                        item.DisableTag(IssueTags[i]);
                    }
                    catch (Exception)
                    {
                        // 这条本来就没开，忽略
                    }
                }

                ItemFeature old = item.FindItemFeatureByCategory(CategoryGenuine);
                if (old != null)
                {
                    item.RemoveItemFeature(old);
                }

                if (genuine)
                {
                    InsStampHelper.SetGenuine(item, amount);
                    item.AddItemFeature(ItemFeatureList.StampAuthenticity());
                }
                else
                {
                    InsStampHelper.SetCounterfeit(item, amount);
                    item.AddItemFeature(ItemFeatureList.StampFakeAuthenticity());
                }

                item.Validate();
                return null;
            }
            catch (Exception e)
            {
                Log.Warn("写配给券失败: " + e.Message);
                return "写配给券失败：" + e.Message;
            }
        }

        public static string Describe(GameItem item)
        {
            int amount = Amount(item);
            string state = IsGenuine(item) ? "真券" : "假券";
            return "面值 " + amount.ToString(CultureInfo.InvariantCulture) + " · " + state;
        }

        private static string ParseAmount(string text, out int value)
        {
            value = 0;
            if (text != null)
            {
                text = text.Trim();
            }
            if (string.IsNullOrEmpty(text))
            {
                return "面值不能为空";
            }
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return "面值要填整数";
            }
            if (value < MinAmount || value > MaxAmount)
            {
                return "面值要在 " + MinAmount + " 到 " + MaxAmount + " 之间";
            }
            return null;
        }
    }
}