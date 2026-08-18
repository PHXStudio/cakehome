namespace Watermelon
{
    /// <summary>
    /// One bottom-nav gameplay pillar. Owns enter/exit of its world + UI.
    /// Modules must not call each other — only HubModuleRouter switches them.
    /// </summary>
    public interface IHubModule
    {
        MainHubTab Tab { get; }
        bool IsActive { get; }

        void Enter();
        void Exit();
    }
}
