namespace Watermelon
{
    // UI elements under the tutorial canvas that need explicit initialization.
    // They may start inactive in the scene, so Awake is not guaranteed to run —
    // TutorialCanvasController.Init calls Init on every child element instead.
    public interface ITutorialCanvasElement
    {
        void Init();
    }
}
