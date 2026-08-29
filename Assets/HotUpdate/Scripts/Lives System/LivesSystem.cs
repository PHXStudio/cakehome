using System;
using System.Collections;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 体力门票适配层 + 无限模式容器（体力已统一为能量池，见 EnergyController）。
    /// 门票语义：进关预扣能量（LockLife），通关/回主菜单返还（UnlockLife(false)），
    /// 失败/中途退出不返还（UnlockLife(true)）。锁存续期间连续 NextLevel 只算一张票。
    /// 无限模式期间进关免门票（合成生成器照常耗能）。
    /// </summary>
    [StaticUnload]
    public static class LivesSystem
    {
        public const string TEXT_FULL = "FULL!";
        public const string TEXT_TIMESPAN_FORMAT = "{0:mm\\:ss}";
        public const string TEXT_LONG_TIMESPAN_FORMAT = "{0:hh\\:mm\\:ss}";

        /// <summary>进一关的门票能量。</summary>
        public const int LEVEL_ENERGY_COST = 10;
        /// <summary>D4 半价重试的门票能量。</summary>
        public const int HALF_PRICE_ENERGY_COST = 5;

        private static LivesSave save;

        public static LivesStatus Status { get; private set; }

        /// <summary>当前能量（兼容旧 Lives 语义的读取方；UI 请直接用 EnergyController.Current）。</summary>
        public static int Lives => EnergyController.Current;

        public static bool InfiniteMode { get => Status.InfiniteMode; }

        /// <summary>能量是否已满（供 UI 兼容）。</summary>
        public static bool IsFull => EnergyController.Current >= EnergyController.Max;

        private static Coroutine infiniteModeCoroutine;

        // 当前锁的那张票花了多少能量（0 = 无限模式免票）。锁存续期间连续进关不重复扣。
        private static int lockedTicketCost;

        public static event StatusChangedDelegate StatusChanged;

        // LivesData 资产已随能量统一废弃（数值全在 Energy Data）——Init 不再接收它
        public static void Init()
        {
            Status = new LivesStatus();

            save = SaveController.GetSaveObject<LivesSave>("Lives");
            save.Init(Status);

            lockedTicketCost = 0;

            // 杀进程时票已预扣、锁未解：门票不退（等同原来的"锁中离局扣一命"），只清锁标记
            if (save.LifeLocked)
            {
                save.LifeLocked = false;
                SaveController.MarkAsSaveIsRequired();
            }

            if (save.InfiniteLives)
            {
                DateTime date = DateTime.FromBinary(save.InfiniteLivesDateBinary);
                if (date > DateTime.Now)
                {
                    TimeSpan span = date - DateTime.Now;

                    EnableInfiniteMode(span.TotalSeconds);
                }
                else
                {
                    Status.SetInfiniteModeState(false);
                }
            }

            UpdateStatus();
        }

        /// <summary>进关打锁并预扣门票能量（无限模式免票）。锁存续期间重复调用不再扣（NextLevel 连胜只算一票）。返回 false = 能量不足，未打锁。</summary>
        public static bool LockLife(bool halfPrice = false)
        {
            if (!save.LifeLocked)
            {
                lockedTicketCost = InfiniteMode ? 0 : (halfPrice ? HALF_PRICE_ENERGY_COST : LEVEL_ENERGY_COST);

                // 扣费失败不能打锁——否则通关 UnlockLife(false) 会返还从未支付的门票能量（净 +10）
                if (lockedTicketCost > 0 && !EnergyController.TrySpend(lockedTicketCost))
                {
                    lockedTicketCost = 0;
                    return false;
                }

                save.LifeLocked = true;
                SaveController.MarkAsSaveIsRequired();
            }
            return true;
        }

        /// <summary>解锁结算：decrease=false（通关/回主菜单）返还门票；decrease=true（失败/中途退出）不返还。</summary>
        public static void UnlockLife(bool decrease)
        {
            if (!save.LifeLocked) return;

            save.LifeLocked = false;

            SaveController.MarkAsSaveIsRequired();

            if (!decrease && lockedTicketCost > 0)
                EnergyController.Add(lockedTicketCost, ignoreCap: true);

            lockedTicketCost = 0;
        }

        /// <summary>半价重试可用性：无限模式，或能量够半价票。</summary>
        public static bool CanStartHalfPrice()
        {
            return InfiniteMode || EnergyController.Current >= HALF_PRICE_ENERGY_COST;
        }

        public static void EnableInfiniteMode(double seconds)
        {
            if (InfiniteMode) return;

            TimeSpan time = TimeSpan.FromSeconds(seconds);

            Status.SetInfiniteModeState(true);
            Status.SetInfiniteModeTime(time);
            Status.SetInfiniteModeDate(DateTime.Now + time);

            Status.SetNewLifeTimerState(false);

            infiniteModeCoroutine = Tween.InvokeCoroutine(InfiniteLivesCoroutine());
        }

        public static void DisableInfiniteMode()
        {
            if (!InfiniteMode) return;

            Status.SetInfiniteModeState(false);

            if(infiniteModeCoroutine != null)
            {
                Tween.StopCustomCoroutine(infiniteModeCoroutine);

                infiniteModeCoroutine = null;
            }

            UpdateStatus();
        }

        private static IEnumerator InfiniteLivesCoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.25f);
            while (DateTime.Now < Status.InfiniteModeDate)
            {
                TimeSpan time = Status.InfiniteModeDate - DateTime.Now;

                Status.SetInfiniteModeTime(time);

                UpdateStatus();

                SaveController.MarkAsSaveIsRequired();

                yield return wait;
            }

            Status.SetInfiniteModeState(false);

            UpdateStatus();

            SaveController.MarkAsSaveIsRequired();

            infiniteModeCoroutine = null;
        }

        public static string GetFormatedTime(TimeSpan time)
        {
            if (time.Hours > 0)
                return string.Format(TEXT_LONG_TIMESPAN_FORMAT, time);

            return string.Format(TEXT_TIMESPAN_FORMAT, time);
        }

        private static void UpdateStatus()
        {
            if (!Status.RequireUpdate) return;

            Status.MarkAsUpdated();

            StatusChanged?.Invoke(Status);
        }

        private static void UnloadStatic()
        {
            StatusChanged = null;
            Status = null;
            save = null;

            infiniteModeCoroutine = null;
            lockedTicketCost = 0;
        }

        public delegate void StatusChangedDelegate(LivesStatus status);
    }
}
