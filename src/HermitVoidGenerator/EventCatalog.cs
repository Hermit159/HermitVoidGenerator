using System;
using System.Collections.Generic;
using System.Text;

namespace HermitVoidGenerator
{
    /// <summary>
    /// 游戏本体事件目录。列表里每一项都用游戏自己的工厂函数造事件，触发走
    /// StoreEventManager.ActivateEvent —— 日历、报纸、议价逻辑和自然发生完全一致。
    /// </summary>
    internal static class EventCatalog
    {
        public const int MinDays = 1;
        public const int MaxDays = 365;

        internal sealed class Entry
        {
            public string Group;
            public Func<StoreEvent> Make;
            public StoreEvent Preview;
            public string Id;
            public string Name;
            public string Summary;
            public string Effects;
            public string Area;
            public int DefaultDays;
            public bool OnCalendar;
            public bool Permanent;
            public bool Lockdown;
        }

        /// <summary>按当前游戏语言重建整张表。每次开面板重建，切语言立刻跟上。</summary>
        public static List<Entry> All()
        {
            List<Entry> list = new List<Entry>();

            // ---- 常规：报纸 + 日历上的日常事件
            Add(list, "常规", () => EventListNormal.CreateNutrifruitHarvest());
            Add(list, "常规", () => EventListNormal.CreateBadNutrifruitHarvest());
            Add(list, "常规", () => EventListNormal.CreateSeedHeist());
            Add(list, "常规", () => EventListNormal.CreateCounterfeitMedicalSuppliesDiscovered());
            Add(list, "常规", () => EventListNormal.CreateWaterTreatmentPlantBreakdown());
            Add(list, "常规", () => EventListNormal.CreateScheduledTreatmentPlantMaintenance());
            Add(list, "常规", () => EventListNormal.CreateMinerReturnFromIceAsteroid());
            Add(list, "常规", () => EventListNormal.CreateLowerLevelMobJustice());
            Add(list, "常规", () => EventListNormal.CreateStoreRoomExcavated());
            Add(list, "常规", () => EventListNormal.CreateMiningExpeditionReturn());
            Add(list, "常规", () => EventListNormal.CreateConstructionProject());
            Add(list, "常规", () => EventListNormal.CreateMeteorShower());
            Add(list, "常规", () => EventListNormal.CreatePowerShortage());
            Add(list, "常规", () => EventListNormal.SchedulePowerPlantMaintenance());
            Add(list, "常规", () => EventListNormal.StationWideBlackout());
            Add(list, "常规", () => EventListNormal.CruiseShipDocked());
            Add(list, "常规", () => EventListNormal.CreateOxymoreWave());

            // ---- 特殊
            Add(list, "特殊", () => EventListSpecial.LotteryDraw());
            Add(list, "特殊", () => EventListUnique.CreateEvidenceLockerBreakIn());

            // ---- 派系行动：安全部 / 黑市 / 革命派 / 下层 / 上层
            Add(list, "派系", () => OperationListSec.IncreasePatrols());
            Add(list, "派系", () => OperationListSec.Crackdown());
            Add(list, "派系", () => OperationListSec.RaidBlackMarket());
            Add(list, "派系", () => OperationListSec.RaidRevolution());
            Add(list, "派系", () => OperationListSec.MartialLaw());
            Add(list, "派系", () => OperationListSec.BudgetIncrease());
            Add(list, "派系", () => OperationListBM.GangWar());
            Add(list, "派系", () => OperationListBM.IllegalWeaponShipment());
            Add(list, "派系", () => OperationListRev.MobilizationSecret());
            Add(list, "派系", () => OperationListRev.Mobilization());
            Add(list, "派系", () => OperationListRev.RaidUpperLevel());
            Add(list, "派系", () => OperationListRev.RaidSecurity());
            Add(list, "派系", () => OperationListLL.Riot());
            Add(list, "派系", () => OperationListLL.WorkerStrike());
            Add(list, "派系", () => OperationListUL.MedicalAid());
            Add(list, "派系", () => OperationListUL.FoodDrive());
            Add(list, "派系", () => OperationListUL.HouseholdRelief());

            // ---- 例行：游戏每 7 天自己排一次，列表里只是给你手动重排
            Add(list, "例行", () => EventListInnate.CreateRentDayEvent());
            Add(list, "例行", () => EventListInnate.CreateMortgagePaymentEvent());
            Add(list, "例行", () => EventListInnate.CreateCartelVisitEvent());

            return list;
        }

        private static void Add(List<Entry> list, string group, Func<StoreEvent> make)
        {
            StoreEvent ev;
            try
            {
                ev = make();
            }
            catch (Exception e)
            {
                // 比如彩票新闻要读当前中奖号，没存档时会炸
                Log.Warn("事件目录跳过一项（构造失败）: " + e.Message);
                return;
            }
            if (ev == null || string.IsNullOrEmpty(ev.identifier))
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id == ev.identifier)
                {
                    return;
                }
            }

            Entry entry = new Entry();
            entry.Group = group;
            entry.Make = make;
            entry.Preview = ev;
            entry.Id = ev.identifier;
            entry.Name = SafeName(ev);
            entry.Summary = Clean(ev.newsDescription);
            entry.Effects = Effects(ev);
            entry.Area = AreaName(ev.eventArea);
            entry.DefaultDays = ClampDays(ev.duration);
            entry.OnCalendar = ev.eventType != StoreEvent.EventType.COSMETIC;
            entry.Permanent = ev.isPermanent;
            entry.Lockdown = ev.isLockDown;
            list.Add(entry);
        }

        // ================= 文案 =================

        public static string Label(Entry entry)
        {
            if (entry == null)
            {
                return "";
            }
            return entry.Group + " · " + entry.Name;
        }

        /// <summary>选中事件后写在下面的简介块。只写玩家关心的东西。</summary>
        public static string InfoText(Entry entry)
        {
            if (entry == null)
            {
                return UiText.BasicEventNoPick;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(entry.Name).Append('\n');
            sb.Append(entry.Summary.Length > 0 ? entry.Summary : UiText.BasicEventNoSummary).Append('\n');
            sb.Append(UiText.BasicEventEffects);
            sb.Append(entry.Effects.Length > 0 ? entry.Effects : UiText.BasicEventNoEffects);

            if (entry.Permanent)
            {
                sb.Append('\n').Append(UiText.BasicEventPermanent);
            }
            if (entry.Lockdown)
            {
                sb.Append('\n').Append(UiText.BasicEventLockdownNote);
            }
            return sb.ToString();
        }

        /// <summary>日历上正在跑的事件，一行写完。</summary>
        public static string ActiveSummary()
        {
            try
            {
                StoreStation station = StoreStation.Instance;
                if (station == null || station.storeEventManager == null)
                {
                    return UiText.BasicEventNoSave;
                }
                StoreEventManager manager = station.storeEventManager;
                List<StoreEvent> active = manager.GetActiveEvents();
                if (active == null || active.Count == 0)
                {
                    return UiText.BasicEventNothing;
                }

                bool newsletterOnly = NewsletterOnly();
                StringBuilder sb = new StringBuilder();
                int shown = 0;
                int skipped = 0;
                int day = station.dayCounter;
                for (int i = 0; i < active.Count; i++)
                {
                    StoreEvent ev = active[i];
                    if (ev == null)
                    {
                        continue;
                    }
                    if (ev.eventType == StoreEvent.EventType.COSMETIC)
                    {
                        skipped++;
                        continue;
                    }
                    if (newsletterOnly && ev.eventType != StoreEvent.EventType.INNATE)
                    {
                        skipped++;
                        continue;
                    }
                    if (shown >= 4)
                    {
                        skipped++;
                        continue;
                    }
                    if (shown > 0)
                    {
                        sb.Append("、");
                    }
                    sb.Append(ev.GetDisplayName());
                    if (ev.isPermanent)
                    {
                        sb.Append("（永久）");
                    }
                    else
                    {
                        int left = ev.totalDuration - (day - ev.startDay);
                        if (left < 0)
                        {
                            left = 0;
                        }
                        sb.Append("（剩 ").Append(left).Append(" 天）");
                    }
                    shown++;
                }

                if (shown == 0)
                {
                    return UiText.BasicEventNothing;
                }
                if (skipped > 0)
                {
                    sb.Append(" 等");
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Log.Warn("读活动事件失败: " + e.Message);
                return UiText.BasicEventNoSave;
            }
        }

        // ================= 触发 =================

        /// <summary>触发选中的事件。失败时 message 里是给玩家看的原因。</summary>
        public static bool Trigger(Entry entry, string daysText, out string message)
        {
            message = null;
            if (entry == null)
            {
                message = UiText.BasicEventNoPick;
                return false;
            }

            StoreStation station = StoreStation.Instance;
            if (station == null || station.storeEventManager == null)
            {
                message = UiText.BasicEventNoSave;
                return false;
            }
            StoreEventManager manager = station.storeEventManager;

            int days = 0;
            string raw = (daysText ?? "").Trim();
            if (raw.Length > 0 && !int.TryParse(raw, out days))
            {
                message = UiText.BasicEventBadDays;
                return false;
            }
            bool clamped = false;
            if (raw.Length == 0)
            {
                days = entry.DefaultDays;
                clamped = true;
            }
            if (days < MinDays)
            {
                days = MinDays;
                clamped = true;
            }
            if (days > MaxDays)
            {
                days = MaxDays;
                clamped = true;
            }

            StoreEvent ev;
            try
            {
                ev = entry.Make();
            }
            catch (Exception e)
            {
                Log.Warn("构造事件失败: " + e);
                message = "这次没能生成事件。";
                return false;
            }
            if (ev == null)
            {
                message = "这次没能生成事件。";
                return false;
            }

            // 已经在跑就先收掉，重新从今天开始计时（否则 force 会塞进第二份）
            bool restart = false;
            try
            {
                if (manager.IsEventActive(ev.identifier))
                {
                    StoreEvent running = manager.GetActiveEvent(ev.identifier);
                    if (running != null)
                    {
                        manager.DeactivateEvent(running);
                        restart = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("结束旧事件失败: " + e.Message);
            }

            bool permanent = ev.isPermanent;
            if (!permanent)
            {
                ev.duration = days;
            }

            try
            {
                manager.ActivateEvent(ev, false, true);
            }
            catch (Exception e)
            {
                Log.Warn("触发事件失败: " + e);
                message = "触发失败。";
                return false;
            }

            bool live;
            try
            {
                live = manager.IsEventActive(ev.identifier);
            }
            catch
            {
                live = false;
            }
            if (!live)
            {
                message = "游戏没接住这个事件，可能正赶上封锁。";
                return false;
            }

            RefreshCalendar();

            StringBuilder sb = new StringBuilder();
            sb.Append(restart ? UiText.BasicEventRestarted : UiText.BasicEventTriggered);
            sb.Append(ev.GetDisplayName());
            if (permanent)
            {
                sb.Append("（永久）");
            }
            else
            {
                sb.Append(" · ").Append(ev.duration).Append(" 天");
            }
            if (clamped)
            {
                sb.Append("（天数已收到 ").Append(MinDays).Append(" 到 ").Append(MaxDays).Append("）");
            }
            if (!entry.OnCalendar)
            {
                sb.Append("（这类事件不会显示在日历上）");
            }
            if (NewsletterOnly())
            {
                sb.Append(UiText.BasicEventPerkNote);
            }
            message = sb.ToString();
            return true;
        }

        /// <summary>日历开着就地重画；没开就等玩家自己打开。</summary>
        public static void RefreshCalendar()
        {
            try
            {
                AdvCalendarUIManager cal = AdvCalendarUIManager.Instance;
                if (cal == null || cal.UIPanel == null || !cal.UIPanel.activeInHierarchy)
                {
                    return;
                }
                cal.OpenUI();
            }
            catch (Exception e)
            {
                Log.Warn("刷新日历失败: " + e.Message);
            }
        }


        // ================= 小工具 =================

        private static bool NewsletterOnly()
        {
            try
            {
                return StartingPerk.IsUnsubscribed();
            }
            catch
            {
                return false;
            }
        }

        private static string SafeName(StoreEvent ev)
        {
            try
            {
                string name = ev.GetDisplayName();
                if (!string.IsNullOrEmpty(name))
                {
                    return Clean(name);
                }
            }
            catch
            {
                // 名字读不到就用 identifier
            }
            return ev.identifier;
        }

        private static string Effects(StoreEvent ev)
        {
            try
            {
                string text = ev.GetEventPriceEffect();
                if (string.IsNullOrEmpty(text))
                {
                    return "";
                }
                string[] lines = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0)
                    {
                        continue;
                    }
                    if (sb.Length > 0)
                    {
                        sb.Append("、");
                    }
                    sb.Append(line);
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Log.Warn("读事件价格影响失败: " + e.Message);
                return "";
            }
        }

        private static string AreaName(StoreEvent.EventArea area)
        {
            if (area == StoreEvent.EventArea.UPPER)
            {
                return "上层";
            }
            if (area == StoreEvent.EventArea.LOWER)
            {
                return "下层";
            }
            return "全站";
        }

        private static int ClampDays(int days)
        {
            if (days < MinDays)
            {
                return MinDays;
            }
            return days > MaxDays ? MaxDays : days;
        }

        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            return text.Replace("\r", "").Replace("\n", " ").Trim();
        }
    }
}
