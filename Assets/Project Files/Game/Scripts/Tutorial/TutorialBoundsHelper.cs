using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    // Computes the Rect (in a given mask RectTransform's local space) that encapsulates one or
    // more source RectTransforms — used to feed TutorialSpotlightMaskController.Show(), which
    // takes a single bounding rect rather than per-cell holes.
    // Lives in the Game assembly (not Modules/Tutorial's Watermelon.Tutorial.asmdef) because it
    // references MergeGrid/MergeCellBackground, which are Assembly-CSharp game types that an
    // explicit assembly definition cannot depend on.
    public static class TutorialBoundsHelper
    {
        private static readonly Vector3[] corners = new Vector3[4];

        public static Rect ComputeEncapsulatingRect(MergeGrid grid, IEnumerable<Vector2Int> cells, RectTransform maskRectTransform)
        {
            List<RectTransform> rects = new List<RectTransform>();
            foreach (Vector2Int cell in cells)
            {
                MergeCellBackground background = grid.GetBackground(cell);
                if (background != null)
                    rects.Add((RectTransform)background.transform);
            }
            return ComputeEncapsulatingRect(rects, maskRectTransform);
        }

        public static Rect ComputeEncapsulatingRect(IEnumerable<RectTransform> elements, RectTransform maskRectTransform)
        {
            bool hasBounds = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;

            foreach (RectTransform element in elements)
            {
                if (element == null) continue;

                element.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 local = maskRectTransform.InverseTransformPoint(corners[i]);
                    if (!hasBounds)
                    {
                        min = local;
                        max = local;
                        hasBounds = true;
                    }
                    else
                    {
                        min = Vector2.Min(min, local);
                        max = Vector2.Max(max, local);
                    }
                }
            }

            return hasBounds ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : new Rect();
        }
    }
}
