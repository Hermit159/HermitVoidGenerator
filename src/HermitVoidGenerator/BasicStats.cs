using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HermitVoidGenerator
{
    /// <summary>「基础修改」页的读写层：存档里的全局数值 + 伤势状态。</summary>
    internal static class BasicStats
    {
        public const int CashCap = 999999999;
        public const int RepMin = -200;
        public const int RepMax = 200;
        public const int ValueCap = 9999999;
        public const int PercentCap = 9999;

        // 伤口状态是 0..10 的整数，>5 算重伤（HealthData.SERIOUS_WOUND_THRESHOLD）
        public static readonly int[] WoundValues = { 0, 2, 4, 5, 6, 8, 10 };
        public static readonly string[] WoundOptions =
        {
            "无伤（0）",
            "轻伤 · 轻微（2）",
            "轻伤 · 中等（4）",
            "轻伤 · 临界（5）",
            "重伤 · 轻微（6）",
            "重伤 · 严重（8）",
            "重伤 · 濒危（10）"
        };

        private static PlayerStore Store
        {
            get { return PlayerStore.Instance; }
        }

        private static bool Ready()
        {
            return Store != null;
        }

        // ================= 读 =================

        public static string CashText()
        {
            return Ready() ? Store.playerCash.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string RentText()
        {
            return Ready() ? Store.rentValue.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string RentDaysText()
        {
            return Ready() ? Store.dayUntilRent.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string LoanText()
        {
            return Ready() ? Store.loanValue.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string LoanDaysText()
        {
            return Ready() ? Store.dayUntilLoan.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string AttractText()
        {
            return Ready() ? Store.baseStoreAttractiveness.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string ProjectorText()
        {
            return Ready() ? Store.projectorAttractivenessBonus.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string RetailMarkupText()
        {
            return Ready() ? Store.retailMarkup.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string ContrabandLowText()
        {
            return Ready() ? Store.contrabandMarkupLow.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string ContrabandMidText()
        {
            return Ready() ? Store.contrabandMarkupMid.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string ContrabandHighText()
        {
            return Ready() ? Store.contrabandMarkupHigh.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string ContrabandCriticalText()
        {
            return Ready() ? Store.contrabandMarkupCritial.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static int WoundValue()
        {
            HealthData h = Health();
            return h == null ? 0 : Mathf.Clamp(h.woundState, 0, 10);
        }

        public static bool WoundStable()
        {
            HealthData h = Health();
            return h == null || h.isWoundStable;
        }

        public static int WoundOptionIndex()
        {
            int value = WoundValue();
            int best = 0;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < WoundValues.Length; i++)
            {
                int diff = Math.Abs(WoundValues[i] - value);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = i;
                }
            }
            return best;
        }

        private static HealthData Health()
        {
            return Ready() ? Store.healthData : null;
        }

        // ================= 写 =================

        public static string SetCash(string text)
        {
            if (!Ready())
            {
                return "游戏还没准备好";
            }
            int value;
            string error = ParseInt(text, 0, CashCap, "现金", out value);
            if (error != null)
            {
                return error;
            }
            Store.playerCash = value;
            return null;
        }

        public static string SetRent(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "房租", delegate (int v) { Store.rentValue = v; });
        }

        public static string SetRentDays(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "房租剩余天数", delegate (int v) { Store.dayUntilRent = v; });
        }

        public static string SetLoan(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "贷款", delegate (int v) { Store.loanValue = v; });
        }

        public static string SetLoanDays(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "贷款剩余天数", delegate (int v) { Store.dayUntilLoan = v; });
        }

        public static string SetAttract(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "吸引力", delegate (int v) { Store.baseStoreAttractiveness = v; });
        }

        public static string SetProjector(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "投影加成", delegate (int v) { Store.projectorAttractivenessBonus = v; });
        }

        public static string SetRetailMarkup(string text)
        {
            return SetStoreValue(text, 0, PercentCap, "零售加价", delegate (int v) { Store.retailMarkup = v; });
        }

        public static string SetContrabandLow(string text)
        {
            return SetStoreValue(text, 0, PercentCap, "违禁品加价·低", delegate (int v) { Store.contrabandMarkupLow = v; });
        }

        public static string SetContrabandMid(string text)
        {
            return SetStoreValue(text, 0, PercentCap, "违禁品加价·中", delegate (int v) { Store.contrabandMarkupMid = v; });
        }

        public static string SetContrabandHigh(string text)
        {
            return SetStoreValue(text, 0, PercentCap, "违禁品加价·高", delegate (int v) { Store.contrabandMarkupHigh = v; });
        }

        public static string SetContrabandCritical(string text)
        {
            return SetStoreValue(text, 0, PercentCap, "违禁品加价·极高", delegate (int v) { Store.contrabandMarkupCritial = v; });
        }

        /// <summary>写入伤势：选项下标 → woundState 数值，附稳定状态。</summary>
        public static string ApplyWound(int optionIndex, bool stable)
        {
            HealthData h = Health();
            if (h == null)
            {
                return "游戏还没准备好";
            }
            int index = Mathf.Clamp(optionIndex, 0, WoundValues.Length - 1);
            h.woundState = WoundValues[index];
            h.isWoundStable = stable;
            h.isWoundedFresh = false;
            return null;
        }

        // ---- 阵营声望（amount 是 private double，用 SetReputation 写；它内部 clamp 到 ±200 并重算 perks）

        public sealed class FactionRow
        {
            public string Id;
            public string Name;
        }

        private static readonly string[] FactionFallbackIds =
        {
            "FACTION_LOWER_LEVEL", "FACTION_UPPER_LEVEL", "FACTION_SECURITY",
            "FACTION_BLACK_MARKET", "FACTION_REVOLUTION", "FACTION_CARTEL"
        };

        private static readonly string[] FactionFallbackNames =
        {
            "下层区", "上层区", "治安部", "黑市", "革命军", "13区卡特尔"
        };

        /// <summary>当前存档里存在的所有阵营（含只有毒贩开局才有的卡特尔）。</summary>
        public static List<FactionRow> Factions()
        {
            List<FactionRow> list = new List<FactionRow>();
            if (!Ready() || Store.storeReputations == null)
            {
                return list;
            }
            foreach (StoreReputation rep in Store.storeReputations)
            {
                if (rep == null || string.IsNullOrEmpty(rep.factionId))
                {
                    continue;
                }
                FactionRow row = new FactionRow();
                row.Id = rep.factionId;
                row.Name = FactionName(rep);
                list.Add(row);
            }
            return list;
        }

        private static string FactionName(StoreReputation rep)
        {
            string display = rep.factionDisplay;
            if (!string.IsNullOrEmpty(display) && HasCjk(display))
            {
                return display;
            }
            for (int i = 0; i < FactionFallbackIds.Length; i++)
            {
                if (FactionFallbackIds[i] == rep.factionId)
                {
                    return FactionFallbackNames[i];
                }
            }
            return string.IsNullOrEmpty(display) ? rep.factionId : display;
        }

        private static bool HasCjk(string text)
        {
            foreach (char c in text)
            {
                if ((c >= 0x3400 && c <= 0x9FFF) || (c >= 0xF900 && c <= 0xFAFF))
                {
                    return true;
                }
            }
            return false;
        }

        private static StoreReputation FindRep(string id)
        {
            if (!Ready() || Store.storeReputations == null || string.IsNullOrEmpty(id))
            {
                return null;
            }
            foreach (StoreReputation rep in Store.storeReputations)
            {
                if (rep != null && rep.factionId == id)
                {
                    return rep;
                }
            }
            return null;
        }

        public static string RepText(string id)
        {
            StoreReputation rep = FindRep(id);
            return rep == null ? "" : rep.GetReputation().ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>游戏自己的声望档位名（中立 / 小有名气 / 声名远扬 … 或负面那套）。</summary>
        public static string RepTierText(string id)
        {
            StoreReputation rep = FindRep(id);
            if (rep == null)
            {
                return "";
            }
            try
            {
                string tier = StoreReputation.GetReputationStatus(rep.GetReputation());
                return tier ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>直接设定阵营声望。走 SetReputation：内部 clamp ±200 并重算所有 perk 解锁状态。</summary>
        public static string SetRep(string id, string text)
        {
            StoreReputation rep = FindRep(id);
            if (rep == null)
            {
                return "找不到这个阵营";
            }
            int value;
            string error = ParseInt(text, RepMin, RepMax, RepNameOf(id), out value);
            if (error != null)
            {
                return error;
            }
            rep.SetReputation(value);
            return null;
        }

        public static string RepNameOf(string id)
        {
            StoreReputation rep = FindRep(id);
            return rep == null ? "声望" : FactionName(rep) + "声望";
        }
        private static string SetStoreValue(string text, int min, int max, string label, Action<int> apply)
        {
            if (!Ready())
            {
                return "游戏还没准备好";
            }
            int value;
            string error = ParseInt(text, min, max, label, out value);
            if (error != null)
            {
                return error;
            }
            apply(value);
            return null;
        }

        // ---- 恩惠（王尔德）与配给站券余额

        public static string FavorText()
        {
            return Ready() ? Store.wildFavor.ToString(CultureInfo.InvariantCulture) : "";
        }

        public static string SetFavor(string text)
        {
            return SetStoreValue(text, 0, ValueCap, "恩惠", delegate (int v) { Store.wildFavor = v; });
        }

        private static CommissaryData Commissary()
        {
            if (!Ready())
            {
                return null;
            }
            try
            {
                return Store.commissaryData;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string CommissaryCreditText()
        {
            CommissaryData data = Commissary();
            return data == null ? "" : data.redeemedCredit.ToString(CultureInfo.InvariantCulture);
        }

        public static string SetCommissaryCredit(string text)
        {
            CommissaryData data = Commissary();
            if (data == null)
            {
                return "配给站数据还没准备好";
            }
            int value;
            string error = ParseInt(text, 0, ValueCap, "配给券余额", out value);
            if (error != null)
            {
                return error;
            }
            data.redeemedCredit = value;
            return null;
        }

        private static string ParseInt(string text, int min, int max, string label, out int value)
        {
            value = 0;
            if (text != null)
            {
                text = text.Trim();
            }
            if (string.IsNullOrEmpty(text))
            {
                return label + "不能为空";
            }
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return label + "要填整数";
            }
            if (value < min || value > max)
            {
                return label + "要在 " + min + " 到 " + max + " 之间";
            }
            return null;
        }
    }
}
