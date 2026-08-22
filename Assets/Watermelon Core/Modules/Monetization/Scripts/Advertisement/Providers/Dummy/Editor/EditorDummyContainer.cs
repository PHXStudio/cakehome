namespace Watermelon
{
    [AdsEditorContainer(typeof(AdDummyContainer))]
    public class EditorDummyContainer : EditorAdsContainer
    {
        protected override string ContainerDisplayName => "Dummy";

        protected override void SpecialButtons() { }
    }
}
