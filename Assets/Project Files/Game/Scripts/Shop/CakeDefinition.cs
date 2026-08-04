using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class CakeDefinition
    {
        [SerializeField] string id = "cake";
        [SerializeField] string displayName = "Cake";
        [SerializeField] CakeElement[] elements = { CakeElement.Cream };
        [SerializeField] float creditsPerHour = 10f;
        [SerializeField] Color displayColor = Color.white;
        [SerializeField] GameObject displayPrefab;

        public string Id => id;
        public string DisplayName => displayName;
        public CakeElement[] Elements => elements;
        public float CreditsPerHour => creditsPerHour;
        public Color DisplayColor => displayColor;
        public GameObject DisplayPrefab => displayPrefab;

        public CakeDefinition()
        {
        }

        public CakeDefinition(string id, string displayName, CakeElement[] elements, float creditsPerHour, Color displayColor)
        {
            this.id = id;
            this.displayName = displayName;
            this.elements = elements;
            this.creditsPerHour = creditsPerHour;
            this.displayColor = displayColor;
        }

        public bool HasElement(CakeElement element)
        {
            if (elements == null)
                return false;

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == element)
                    return true;
            }

            return false;
        }
    }
}
