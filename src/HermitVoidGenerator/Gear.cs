using System;
using System.Collections.Generic;

namespace HermitVoidGenerator
{
    /// <summary>耐久读写。游戏侧 DurabilityHelper 要求 feature 先存在，否则 FindItemFeatureByID 直接空引用。</summary>
    internal static class Wear
    {
        public const string TagDurability = "DURABILITY_TAG";
        public const int CapMax = 1000000;

        public static bool Has(GameItem item)
        {
            if (item == null)
            {
                return false;
            }
            try
            {
                return item.IsTag(TagDurability);
            }
            catch
            {
                return false;
            }
        }

        public static int Real(GameItem item)
        {
            try
            {
                return item == null ? 0 : DurabilityHelper.GetRealDurability(item);
            }
            catch
            {
                return 0;
            }
        }

        public static int Fake(GameItem item)
        {
            try
            {
                return item == null ? 0 : DurabilityHelper.GetFakeDurability(item);
            }
            catch
            {
                return 0;
            }
        }

        public static int Max(GameItem item)
        {
            try
            {
                return item == null ? 0 : DurabilityHelper.GetMaxDurability(item);
            }
            catch
            {
                return 0;
            }
        }

        public static int DefaultValue(GameItem item)
        {
            try
            {
                return item == null ? 0 : DurabilityHelper.GetDefDurability(item);
            }
            catch
            {
                return 0;
            }
        }

        public static string Describe(GameItem item)
        {
            if (!Has(item))
            {
                return "无耐久";
            }
            int real = Real(item);
            int max = Max(item);
            int fake = Fake(item);
            string text = real + " / " + max;
            if (fake != real)
            {
                text = text + "（假 " + fake + "）";
            }
            return text;
        }

        /// <summary>real/fake/max 一起写。返回空串表示成功。</summary>
        public static string Apply(GameItem item, int real, int fake, int max)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (max < 1)
            {
                max = 1;
            }
            if (max > CapMax)
            {
                max = CapMax;
            }
            if (real < 0)
            {
                real = 0;
            }
            if (real > max)
            {
                real = max;
            }
            if (fake < 0)
            {
                fake = 0;
            }
            if (fake > max)
            {
                fake = max;
            }

            // DEFAULT 必须 >= MAX 且非零，否则 GetValueModifier 里 max*100/def 会除零
            int def = DefaultValue(item);
            if (def < max)
            {
                def = max;
            }
            if (def < 1)
            {
                def = max;
            }

            try
            {
                if (!item.IsAlreadyContainFeatureWithId(TagDurability))
                {
                    DurabilityHelper.InitItemWithDurability(item, max);
                }
                else if (!item.IsTag(TagDurability))
                {
                    item.EnableTag(TagDurability);
                }
                // real != fake 时游戏那条路径不写 DEFAULT，缺了它 GetValueModifier 会 max*100/def 除零
                item.ModifyTag("DEFAULT_DURABILITY_TAG", delegate(TagState state)
                {
                    state.Enable().SetInt(def);
                });
                DurabilityHelper.SetDurability(item, real, fake, max, def);

                // 标签要当场显示出来，不然 tooltip 里那条耐久看不见
                ItemFeature feature = item.FindItemFeatureByID(TagDurability);
                if (feature != null && !feature.IsFeatureDiscovered())
                {
                    feature.SetFeatureDiscovered(true);
                }
                item.Validate();
            }
            catch (Exception e)
            {
                return "写耐久失败: " + e.Message;
            }
            return "";
        }

        public static string Fill(GameItem item)
        {
            int max = Max(item);
            if (max < 1)
            {
                max = 100;
            }
            return Apply(item, max, max, max);
        }
    }

    /// <summary>一条升级线（一台机器可能有几条）。</summary>
    internal sealed class UpgradeChannel
    {
        public string Label;
        public string Tag;
        public int Max;
        public Func<GameItem, bool> Detect;
        public Action<GameItem> Recalc;
    }

    /// <summary>机器升级等级。等级本身就是物品上的一个 int 标签。</summary>
    internal static class Upgrades
    {
        public const string ConverterTag = "CURRENT_CONVERTER_UPGRADE_COUNT_INT";
        public const string BatteryTag = "HYPERTECH_BATTERY_UPGRADE_COUNT_INT";

        private const string TagRas = "RAS_TAG";
        private const string TagFridge = "FRIDGE_TAG";
        private const string TagBattery = "HYPERTECH_BATTERY_TAG";
        private const string TagConverter = "CURRENT_CONVERTER_TAG";

        private static List<UpgradeChannel> table;

        public static List<UpgradeChannel> Table()
        {
            if (table != null)
            {
                return table;
            }

            List<UpgradeChannel> list = new List<UpgradeChannel>();
            list.Add(Make(UiText.UpRasWidth, "RAS_WIDTH_UPGRADE_COUNT_TAG", 4, TagRas, RasRecalc));
            list.Add(Make(UiText.UpRasHeight, "RAS_HEIGHT_UPGRADE_COUNT_TAG", 4, TagRas, RasRecalc));
            list.Add(Make(UiText.UpRasPower, "RAS_POWER_UPGRADE_COUNT_TAG", 12, TagRas, RasPowerRecalc));
            list.Add(Make(UiText.UpFridgeStorage, "FRIDGE_STORAGE_UPGRADE_COUNT_TAG", 7, TagFridge, FridgeRecalc));
            list.Add(Make(UiText.UpFridgePower, "FRIDGE_POWER_UPGRADE_COUNT_TAG", 7, TagFridge, FridgePowerRecalc));
            list.Add(Make(UiText.UpPrinter, "BOTTLE_PRINTER_UPGRADE_COUNT_TAG", 9, null, null));
            list.Add(Make(UiText.UpConverter, ConverterTag, 5, TagConverter, null));
            list.Add(Make(UiText.UpBattery, BatteryTag, 99, TagBattery, null));
            table = list;
            return table;
        }

        private static UpgradeChannel Make(string label, string tag, int max, string detectTag, Action<GameItem> recalc)
        {
            UpgradeChannel channel = new UpgradeChannel();
            channel.Label = label;
            channel.Tag = tag;
            channel.Max = max;
            channel.Recalc = recalc;
            if (detectTag == null)
            {
                channel.Detect = delegate(GameItem item)
                {
                    return SafeId(item) == "bottle_printer";
                };
            }
            else
            {
                channel.Detect = delegate(GameItem item)
                {
                    return HasTag(item, detectTag);
                };
            }
            return channel;
        }

        /// <summary>这件物品身上能改的升级线。</summary>
        public static List<UpgradeChannel> ChannelsOf(GameItem item)
        {
            List<UpgradeChannel> result = new List<UpgradeChannel>();
            if (item == null)
            {
                return result;
            }
            List<UpgradeChannel> all = Table();
            for (int i = 0; i < all.Count; i++)
            {
                UpgradeChannel channel = all[i];
                bool hit;
                try
                {
                    hit = channel.Detect != null && channel.Detect(item);
                }
                catch
                {
                    hit = false;
                }
                if (hit)
                {
                    result.Add(channel);
                }
            }
            return result;
        }

        public static int Level(GameItem item, UpgradeChannel channel)
        {
            if (item == null || channel == null)
            {
                return 0;
            }
            try
            {
                return item.GetTagReadonly(channel.Tag).GetInt();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>设定等级。返回空串表示成功。</summary>
        public static string Apply(GameItem item, UpgradeChannel channel, int level)
        {
            if (item == null)
            {
                return "没有物品";
            }
            if (channel == null)
            {
                return "没有升级线";
            }
            if (level < 0)
            {
                level = 0;
            }
            if (level > channel.Max)
            {
                level = channel.Max;
            }

            int oldLevel = Level(item, channel);
            if (level == oldLevel)
            {
                return "";
            }

            try
            {
                // 这几条升级线的副作用是改另一个标签，必须按增量补
                if (channel.Tag == ConverterTag)
                {
                    float rate = item.GetTagReadonly("CURRENT_CONVERTER_RATE_FLOAT").GetFloat();
                    if (rate <= 0f)
                    {
                        rate = 1.5f;
                    }
                    float newRate = rate - 0.1f * (level - oldLevel);
                    if (newRate < 0f)
                    {
                        newRate = 0f;
                    }
                    item.ModifyTag("CURRENT_CONVERTER_RATE_FLOAT", delegate(TagState state)
                    {
                        state.Enable().SetFloat(newRate);
                    });
                }
                else if (channel.Tag == BatteryTag)
                {
                    int output = item.GetTagReadonly("HYPERTECH_BATTERY_OUTPUT_INT").GetInt();
                    output += 10 * (level - oldLevel);
                    if (output < 0)
                    {
                        output = 0;
                    }
                    item.ModifyTag("HYPERTECH_BATTERY_OUTPUT_INT", delegate(TagState state)
                    {
                        state.Enable().SetInt(output);
                    });
                }

                int target = level;
                item.ModifyTag(channel.Tag, delegate(TagState state)
                {
                    state.Enable().SetInt(target);
                });

                if (channel.Recalc != null)
                {
                    channel.Recalc(item);
                }
                item.Validate();
            }
            catch (Exception e)
            {
                return "写升级等级失败: " + e.Message;
            }
            return "";
        }

        private static void RasRecalc(GameItem item)
        {
            MachineRAS.ApplyTankSize(item);
            MachineRAS.UpdatePowerUsage(item);
        }

        private static void RasPowerRecalc(GameItem item)
        {
            MachineRAS.UpdatePowerUsage(item);
        }

        private static void FridgeRecalc(GameItem item)
        {
            MachineFridge.ApplyStorageSize(item);
            MachineFridge.UpdateFridgePowerUsage(item);
        }

        private static void FridgePowerRecalc(GameItem item)
        {
            MachineFridge.UpdateFridgePowerUsage(item);
        }

        private static bool HasTag(GameItem item, string tag)
        {
            try
            {
                return item != null && item.IsTag(tag);
            }
            catch
            {
                return false;
            }
        }

        private static string SafeId(GameItem item)
        {
            try
            {
                return item == null ? "" : (item.identifier ?? "");
            }
            catch
            {
                return "";
            }
        }
    }
}
