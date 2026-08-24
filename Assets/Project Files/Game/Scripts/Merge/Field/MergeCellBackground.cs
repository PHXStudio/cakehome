using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    [RequireComponent(typeof(Image))]
    public class MergeCellBackground : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private MergeCell cell;

        public Vector2Int Position => cell.Position;
        public MergeFieldObject Occupant => cell.Occupant;

        public void Init(MergeCell cell)
        {
            this.cell = cell;
        }

        // Unity still routes a click to the press-origin object when a drag just completed on
        // it; eventData.dragging is set for that pointer in that case, so skip the click —
        // otherwise this fires HandleCellClick on the now-emptied source cell right after EndDrag.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            MergeController.Instance?.HandleCellClick(this);
        }
        public void OnPointerDown(PointerEventData eventData)  => MergeController.Instance?.HandlePointerDown(this);
        public void OnBeginDrag(PointerEventData eventData)    => MergeController.Instance?.BeginDrag(this, eventData);
        public void OnDrag(PointerEventData eventData)         => MergeController.Instance?.UpdateDrag(eventData);
        public void OnEndDrag(PointerEventData eventData)      => MergeController.Instance?.EndDrag(eventData);
    }
}
