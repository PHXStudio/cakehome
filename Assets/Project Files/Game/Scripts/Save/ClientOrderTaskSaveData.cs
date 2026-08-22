using System;
using System.Collections.Generic;

namespace Watermelon
{
    [Serializable]
    public class ClientOrderTaskSaveData
    {
        public string TaskId;
        public string CharacterId;
        public int    CoinsReward;
        public List<OrderItemSaveData> Items = new List<OrderItemSaveData>();
    }
}
