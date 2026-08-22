using UnityEngine;

namespace Watermelon
{
    // 模板(合并玩法)新手引导在蛋糕版中不启用。
    // 此处仅保留 API 外壳供模板代码编译引用(Step 枚举被 FirstStartTutorialSave 序列化使用,
    // IsCompleted 被 BuildingUpgradeHintTutorial 调用)。
    // TODO(模板迁移): 如需启用,恢复完整实现(/tmp 备份或重新从模板包导入)并接入蛋糕版 UIGame/UIMainMenu。
    public class FirstStartTutorial : BaseTutorial
    {
        public enum Step
        {
            IntroDialog,
            HighlightBoardButton,
            TeachFirstMerge,
            TeachGrayItemMerge,
            TeachChainMerge,
            TeachSpawnerTap,
            TeachSpawnedMerge,
            TeachGiveTask,
            SecondOrder,
            ThirdOrder,
            GoToMainMenu,
            HighlightBuildButton,
            TeachBuildingUpgrade,
            HighlightBackButton,
        }

        /// <summary>蛋糕版中该引导永不运行,恒视为已完成(不阻塞其他引导)。</summary>
        public static bool IsCompleted() => true;

        protected override void OnInitialised() { }
        protected override void OnStarted() { }
        protected override void OnFinished() { }
    }
}
