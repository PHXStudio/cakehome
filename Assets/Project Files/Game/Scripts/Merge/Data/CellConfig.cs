using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class CellConfig
    {
        [SerializeField] string typeId;
        [SerializeField] int grade = 1;
        [SerializeField] CellLockState lockState;

        public string TypeId => typeId;
        public int Grade => grade;
        public bool IsHalfLocked => lockState == CellLockState.HalfLocked;
        public bool IsLocked => lockState == CellLockState.Locked;
        public bool IsEmpty => string.IsNullOrEmpty(typeId) && lockState != CellLockState.Locked;
    }
}
