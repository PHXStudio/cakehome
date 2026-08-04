using System;

namespace Watermelon
{
    [System.Serializable]
    public class OwnedCake
    {
        public string InstanceId;
        public string DefinitionId;
        public long PlacedAtBinary;

        public bool IsOnShelf => PlacedAtBinary != 0;

        public OwnedCake()
        {
        }

        public OwnedCake(string instanceId, string definitionId)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            PlacedAtBinary = 0;
        }

        public void PlaceNow()
        {
            PlacedAtBinary = DateTime.Now.ToBinary();
        }

        public void ClearPlacement()
        {
            PlacedAtBinary = 0;
        }

        public DateTime GetPlacedAt()
        {
            if (PlacedAtBinary == 0)
                return DateTime.MinValue;

            return DateTime.FromBinary(PlacedAtBinary);
        }

        public float GetHoursOnShelf(DateTime now)
        {
            if (PlacedAtBinary == 0)
                return 0f;

            TimeSpan span = now - GetPlacedAt();
            return (float)Math.Max(0.0, span.TotalHours);
        }
    }
}
