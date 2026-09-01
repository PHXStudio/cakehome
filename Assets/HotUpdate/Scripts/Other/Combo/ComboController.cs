using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 三消爽感：连击（Combo）+ Fever 高潮系统。
    /// 短时间（3s）内连续消除 → 连击叠加 → 分数倍率递增；连击达 5 → 10s Fever（双倍分）。
    /// 挂 DockBehavior.MatchCombined 事件；生命周期由 GameController 进出关卡控制，Fever 到期由 LevelController.Update 驱动。
    /// </summary>
    public static class ComboController
    {
        private const float COMBO_WINDOW = 3f;        // 连击判定窗口（秒）
        private const int FEVER_THRESHOLD = 5;        // 触发 Fever 的连击数
        private const float FEVER_DURATION = 10f;     // Fever 持续时长（秒）
        private const float MULT_STEP = 0.5f;         // 每连击增加的倍率
        private const float MAX_MULT = 5f;            // 倍率封顶

        private static int combo;
        private static float lastMatchTime = -100f;
        private static bool isFever;
        private static float feverEndTime;
        private static bool isInLevel;

        // 表现统计（供结算页表现加成：消除棋子数 × 最高连击）
        private static int totalMatched;
        private static int maxCombo;

        public static int Combo => combo;
        public static bool IsFever => isFever;
        /// <summary>本局累计消除棋子数（结算表现加成用）。</summary>
        public static int TotalMatched => totalMatched;
        /// <summary>本局最高连击（结算表现加成用）。</summary>
        public static int MaxCombo => maxCombo;

        /// <summary>连击变化：combo 数 + 当前倍率。</summary>
        public static event Action<int, float> OnComboChanged;
        /// <summary>Fever 开始(true)/结束(false)。</summary>
        public static event Action<bool> OnFeverChanged;

        /// <summary>当前分数倍率：Fever 时 5x，否则 1 + combo×0.5（封顶 5x）。</summary>
        public static float GetMultiplier()
        {
            if (isFever) return MAX_MULT;
            return Mathf.Min(1f + combo * MULT_STEP, MAX_MULT);
        }

        public static void OnLevelStarted()
        {
            combo = 0;
            isFever = false;
            isInLevel = true;
            lastMatchTime = -100f;
            totalMatched = 0;
            maxCombo = 0;
            OnComboChanged?.Invoke(0, 1f);
            OnFeverChanged?.Invoke(false);
        }

        public static void OnLevelEnded()
        {
            isInLevel = false;
            if (isFever)
            {
                isFever = false;
                OnFeverChanged?.Invoke(false);
            }
            if (combo > 0)
            {
                combo = 0;
                OnComboChanged?.Invoke(0, 1f);
            }
        }

        /// <summary>消除回调（DockBehavior.MatchCombined）：连击判定 + Fever 触发。</summary>
        public static void RegisterMatch(int count)
        {
            if (!isInLevel || count < 3) return;

            float now = Time.time;
            combo = (now - lastMatchTime <= COMBO_WINDOW) ? combo + 1 : 1;
            lastMatchTime = now;

            // 表现统计（结算加成用）
            totalMatched += count;
            if (combo > maxCombo) maxCombo = combo;

            if (combo >= FEVER_THRESHOLD && !isFever)
            {
                isFever = true;
                feverEndTime = now + FEVER_DURATION;
                OnFeverChanged?.Invoke(true);
            }

            OnComboChanged?.Invoke(combo, GetMultiplier());
        }

        /// <summary>每帧驱动：Fever 到期结束（由 LevelController.Update 调用）。</summary>
        public static void Update()
        {
            if (isFever && Time.time > feverEndTime)
            {
                isFever = false;
                OnFeverChanged?.Invoke(false);
                OnComboChanged?.Invoke(combo, GetMultiplier());
            }
        }
    }
}
