using System;

namespace Watermelon
{
    public class EnergyController
    {
        private static EnergyController instance;

        public static int Current { get; private set; }
        public static int Max     { get; private set; }

        public static UnityEngine.Sprite Icon => instance?.data.Icon;

        public static event Action<int, int> OnEnergyChanged; // current, max

        public int RegenIntervalSeconds { get; private set; }

        /// <summary>Static accessor for systems that only have the static API (push reminders etc.).</summary>
        public static int RegenInterval => instance?.RegenIntervalSeconds ?? 120;

        private ResourcesSave save;
        private EnergyData data;

        public EnergyController(EnergyData data)
        {
            instance = this;
            this.data = data;

            Max = data.MaxEnergy;
            RegenIntervalSeconds = data.RegenIntervalSeconds;

            save = SaveController.GetSaveObject<ResourcesSave>("Resources");
            Current = save.Energy;

            if (save.EnergyTimestampBinary == 0 && Current == 0)
            {
                Current = Max;
                save.Energy = Max;
            }

            // Anchor regen timing on very first launch — without it the recover
            // countdown has no reference point and shows a frozen value until the first tick
            if (save.EnergyTimestampBinary == 0)
                save.EnergyTimestampBinary = DateTime.UtcNow.ToBinary();

            RecoverOffline();
        }

        // ─── Offline recovery ────────────────────────────────────────────────────

        // Recomputes energy earned while the app was closed/backgrounded. Safe to call
        // repeatedly (e.g. on app resume) — timestamp is advanced so gains aren't double-counted.
        public void RecoverOffline()
        {
            if (save.EnergyTimestampBinary == 0 || Current >= Max) return;

            var lastTick = DateTime.FromBinary(save.EnergyTimestampBinary);
            double elapsedSeconds = (DateTime.UtcNow - lastTick).TotalSeconds;
            int earned = (int)(elapsedSeconds / RegenIntervalSeconds);

            if (earned > 0)
                AddInternal(earned, saveTimestamp: true);
        }

        // Returns seconds remaining until next regen tick (for coroutine initial delay).
        public float GetInitialRegenDelay()
        {
            if (save.EnergyTimestampBinary == 0)
                return RegenIntervalSeconds;

            var lastTick = DateTime.FromBinary(save.EnergyTimestampBinary);
            float elapsed = (float)(DateTime.UtcNow - lastTick).TotalSeconds;
            float remaining = RegenIntervalSeconds - (elapsed % RegenIntervalSeconds);
            return remaining > 0 ? remaining : RegenIntervalSeconds;
        }

        // Seconds remaining until next energy point (0 when already full).
        public static float GetSecondsUntilNext()
        {
            if (instance == null || Current >= Max) return 0f;
            return instance.GetInitialRegenDelay();
        }

        // Seconds remaining until energy is fully restored (0 when already full).
        public static float GetSecondsUntilFull()
        {
            if (instance == null || Current >= Max) return 0f;
            return instance.GetInitialRegenDelay() + (Max - Current - 1) * instance.RegenIntervalSeconds;
        }

        // ─── Tick (called by EnergyRegenRunner coroutine) ────────────────────────

        public void Tick()
        {
            if (Current >= Max) return;
            AddInternal(1, saveTimestamp: true);
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        public static bool TrySpend(int amount)
        {
            if (instance == null || Current < amount) return false;

            // 先把累计回复结算到当前时刻——满格时 Tick/RecoverOffline 都不再推进时间戳，
            // 不结算的话时间戳会停在回满时刻，形成"时间银行"。
            instance.RecoverOffline();
            if (Current < amount) return false;

            bool wasFullOrOver = Current >= Max;

            Current -= amount;
            instance.save.Energy = Current;

            // 从满格（含 ignoreCap 超上限）扣费时把时间戳锚定到当前时刻，否则下次
            // RecoverOffline 会把"回满→扣费"之间的时间也算作回复量，白返能量。
            // 满格期间回复本就暂停，锚定不损失任何进行中的回复进度。
            if (wasFullOrOver)
                instance.save.EnergyTimestampBinary = DateTime.UtcNow.ToBinary();

            SaveController.MarkAsSaveIsRequired();
            OnEnergyChanged?.Invoke(Current, Max);
            return true;
        }

        public static void Add(int amount, bool ignoreCap = false)
        {
            instance?.AddInternal(amount, saveTimestamp: false, ignoreCap: ignoreCap);
        }

        public static void Set(int amount)
        {
            if (instance == null) return;

            Current = amount;
            instance.save.Energy = Current;

            SaveController.MarkAsSaveIsRequired();
            OnEnergyChanged?.Invoke(Current, Max);
        }

        // ─── Internal ────────────────────────────────────────────────────────────

        private void AddInternal(int amount, bool saveTimestamp, bool ignoreCap = false)
        {
            Current = ignoreCap ? Current + amount : System.Math.Min(Current + amount, Max);
            save.Energy = Current;

            if (saveTimestamp)
                save.EnergyTimestampBinary = DateTime.UtcNow.ToBinary();

            SaveController.MarkAsSaveIsRequired();
            OnEnergyChanged?.Invoke(Current, Max);
        }

        public void Unload()
        {
            instance = null;
        }
    }
}
