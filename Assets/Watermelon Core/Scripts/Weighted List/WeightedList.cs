using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class WeightedList<T> where T : class
    {
        [SerializeField] List<WeightedItem<T>> items = new List<WeightedItem<T>>();
        public List<WeightedItem<T>> Items => items;

        public float TotalWeight => items.Sum(x => x.Weight);

        public WeightedList()
        {

        }

        public WeightedList(List<WeightedItem<T>> items)
        {
            this.items = items;
        }

        public void Add(WeightedItem<T> item)
        {
            items.Add(item);
        }

        public T GetRandomItem() => GetRandomItemWithChance().Item;

        // Returns the drawn item together with its own odds (0-100) within this list —
        // lets callers react to how rare the specific draw was (e.g. rare-item VFX/text)
        // without re-deriving weight/TotalWeight themselves.
        public (T Item, float ChancePercent) GetRandomItemWithChance()
        {
            if (items.IsNullOrEmpty())
            {
                Debug.LogError("Weighted list can't be empty");
                return (null, 0f);
            }

            float totalWeight = TotalWeight;
            if (totalWeight == 0f)
            {
                LogManager.LogWarning("All weights are 0. Returning element with index 0.", LogCategory.Systems);
                return (items[0].Item, 100f / items.Count);
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (WeightedItem<T> item in items)
            {
                currentWeight += item.Weight;

                if (currentWeight >= randomValue)
                {
                    return (item.Item, item.Weight / totalWeight * 100f);
                }
            }

            // probably impossible case, but we have to return something just in case
            WeightedItem<T> fallback = items[0];
            return (fallback.Item, fallback.Weight / totalWeight * 100f);
        }
    }

    [System.Serializable]
    public class WeightedItem<I>
    {
        [SerializeField] float weight = 1f;
        public float Weight => weight;

        [SerializeField] I item;
        public I Item => item;

        public WeightedItem(float weight, I item)
        {
            this.weight = weight;
            this.item = item;
        }
    }
}