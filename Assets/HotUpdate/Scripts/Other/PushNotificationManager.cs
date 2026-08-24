using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 本地推送通知（海外渠道次留召回）。
    /// 编辑器/PC 不触发真实通知；真机（Android/iOS）生效。
    /// 触发点：体力满、能量回满、每日任务/签到刷新。
    /// </summary>
    public static class PushNotificationManager
    {
        private const string DAILY_REMINDER_HOUR = "18"; // 每日 18:00 刷新提醒

        public static void Init()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                if (!Unity.Notifications.NotificationCenter.CheckAuthorizationStatus())
                    Unity.Notifications.NotificationCenter.RequestAuthorization();

                ScheduleDailyResetReminder();
                ScheduleEnergyFullReminder();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Push] Init failed: " + e.Message);
            }
#else
            Debug.Log("[Push] Local notifications only active on Android/iOS.");
#endif
        }

        /// <summary>能量回满时提醒（合成玩法）。</summary>
        public static void ScheduleEnergyFullReminder()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                if (EnergyController.Current >= EnergyController.Max)
                    return;

                double etaSeconds = (EnergyController.Max - EnergyController.Current) * (double)EnergyController.RegenInterval;

                Unity.Notifications.Notification n = new Unity.Notifications.Notification
                {
                    Title = "能量已满！",
                    Text = "你的能量已经回满，快回店里继续合成吧！",
                    FireTime = DateTime.Now.AddSeconds(etaSeconds)
                };
                Unity.Notifications.NotificationCenter.ScheduleNotification(n);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Push] Energy reminder failed: " + e.Message);
            }
#endif
        }

        /// <summary>每日任务/签到刷新提醒（次日 18:00）。</summary>
        public static void ScheduleDailyResetReminder()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                DateTime fireTime = DateTime.Now.Date.AddDays(1).AddHours(18);
                Unity.Notifications.Notification n = new Unity.Notifications.Notification
                {
                    Title = "每日奖励刷新！",
                    Text = "登录领取今日签到与任务奖励！",
                    FireTime = fireTime
                };
                Unity.Notifications.NotificationCenter.ScheduleNotification(n);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Push] Daily reminder failed: " + e.Message);
            }
#endif
        }
    }
}
