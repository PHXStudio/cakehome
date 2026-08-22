using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ImageStep : DialogStep
    {
        [SerializeField] public Sprite image;

        public override bool IsSkippable => true;
    }
}
