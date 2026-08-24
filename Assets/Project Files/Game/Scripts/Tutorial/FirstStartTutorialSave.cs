namespace Watermelon
{
    // Step-level resume state for FirstStartTutorial, separate from the inherited
    // TutorialBaseSave.isFinished (BaseTutorial.save is hardcoded to that type, which only
    // tracks finished/not-finished with no step granularity).
    public class FirstStartTutorialSave : ISaveObject
    {
        public FirstStartTutorial.Step currentStep;
        public string injectedTaskId;
        public string injectedTaskId2;
        public string injectedTaskId3;

        public void OnBeforeSave() { }
    }
}
