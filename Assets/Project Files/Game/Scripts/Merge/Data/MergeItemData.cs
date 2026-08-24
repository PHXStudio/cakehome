using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class MergeItemData
    {
        [SerializeField] string typeId;
        [SerializeField] MergeItemType itemType;
        [SerializeField] GameObject prefab;
        [SerializeField] MergeGradeData[] grades;

        public string TypeId => typeId;
        public MergeItemType ItemType => itemType;
        public GameObject Prefab => prefab;
        public int MaxGrade => grades != null ? grades.Length : 0;
        public MergeGradeData[] Grades => grades;

        public MergeGradeData GetGradeData(int grade)
        {
            if (grades == null || grades.Length == 0)
            {
                Debug.LogError($"[MergeItemData] GetGradeData: null returned, no grades defined for typeId: {typeId}");

                return null;
            }

            int index = Mathf.Clamp(grade - 1, 0, grades.Length - 1);
            return grades[index];
        }
    }
}
