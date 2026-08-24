using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Interactive timeline editor for <see cref="HapticPattern"/>.
    /// Open via the "Edit" button in the <see cref="HapticPatternPropertyDrawer"/>.
    /// <para>
    /// Each event is rendered as a П-shape. Drag the top-right handle right to change Duration,
    /// up/down to change Intensity. Drag the gap handle between events to shift the next event's StartTime.
    /// </para>
    /// </summary>
    public class HapticPatternEditorWindow : EditorWindow
    {
        // ── Layout ───────────────────────────────────────────────────────────

        private const float TOOLBAR_H  = 22f;
        private const float CANVAS_H   = 180f;
        private const float DETAILS_H  = 64f;
        private const float PADDING_H  = 20f;
        private const float PADDING_V  = 14f;
        private const float BORDER_W   = 2f;
        private const float HANDLE_R   = 5f;
        private const float MIN_DUR    = 0.01f;

        // ── Colors ────────────────────────────────────────────────────────────

        private static readonly Color COL_CANVAS     = new(0.13f, 0.13f, 0.13f);
        private static readonly Color COL_GRID       = new(0.22f, 0.22f, 0.22f);
        private static readonly Color COL_GRID_LABEL = new(0.45f, 0.45f, 0.45f);
        private static readonly Color COL_DETAILS_BG = new(0.18f, 0.18f, 0.18f);
        private static readonly Color COL_SELECTED   = new(1.00f, 0.60f, 0.10f);
        private static readonly Color COL_HANDLE     = new(0.90f, 0.90f, 0.90f);
        private static readonly Color COL_HANDLE_HOV = new(1.00f, 0.85f, 0.10f);

        // Sharpness 0 → soft cyan, Sharpness 1 → sharp near-white
        internal static readonly Color COL_LINE_SOFT  = new(0.25f, 0.75f, 0.90f);
        internal static readonly Color COL_LINE_SHARP = new(0.92f, 0.95f, 1.00f);
        internal static readonly Color COL_FILL_SOFT  = new(0.18f, 0.58f, 0.70f, 0.35f);
        internal static readonly Color COL_FILL_SHARP = new(0.78f, 0.88f, 1.00f, 0.35f);

        // ── Target ────────────────────────────────────────────────────────────

        private SerializedObject serializedObject;
        private string           propertyPath;

        // ── Interaction ───────────────────────────────────────────────────────

        private enum DragMode    { None, EventHandle, SharpnessLine, MoveEvent }
        private enum HoverTarget { None, EventHandle, SharpnessLine, MoveEvent }

        private DragMode    dragMode;
        private HoverTarget hoveredTarget;
        private int         dragIndex;
        private int         hoveredEvent  = -1;
        private int         selectedEvent = -1;

        private Vector2 dragStartMouse;
        private float   dragStartDuration;
        private float   dragStartIntensity;
        private float   dragStartTime;
        private float   dragStartSharpness;
        private float   dragStartEh;        // event height at drag start (needed for sharpness delta)

        // ── View ─────────────────────────────────────────────────────────────

        private float pixelsPerSecond = 200f;
        private float scrollX;
        private Rect  audioFromBtnRect;
        private Rect  deviceBtnRect;
        private string lastDiscoveredIP = "";

        // ── Audio import ──────────────────────────────────────────────────────

        private AudioClip audioClip;
        private float     audioThreshold = 0.25f;
        private float     audioMinGapMs  = 50f;

        // ─────────────────────────────────────────────────────────────────────

        public static void Open(SerializedObject so, string path)
        {
            var w = GetWindow<HapticPatternEditorWindow>("Haptic Pattern Editor");
            w.serializedObject = so;
            w.propertyPath     = path;
            w.minSize          = new Vector2(480f, TOOLBAR_H + CANVAS_H + DETAILS_H + 2f);
            w.Show();
        }

        // ── Unity callbacks ───────────────────────────────────────────────────

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            HapticNetworkSender.StartDiscovery();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            HapticNetworkSender.StopDiscovery();
        }

        // Repaint when a new device is discovered so the button label updates
        private void Update()
        {
            string ip = HapticNetworkSender.DiscoveredIP;
            if (ip != lastDiscoveredIP) { lastDiscoveredIP = ip; Repaint(); }
        }

        private void OnUndoRedo()
        {
            if (serializedObject == null || serializedObject.targetObject == null) { Close(); return; }
            serializedObject.Update();
            Repaint();
        }

        private void OnGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                Close();
                return;
            }

            serializedObject.Update();

            SerializedProperty patProp = serializedObject.FindProperty(propertyPath);
            if (patProp == null)
            {
                Close();
                return;
            }

            SerializedProperty idProp     = patProp.FindPropertyRelative("id");
            SerializedProperty eventsProp = patProp.FindPropertyRelative("pattern");

            DrawToolbar(idProp, eventsProp, patProp);

            Rect canvasRect = new(0, TOOLBAR_H, position.width, CANVAS_H);
            DrawCanvas(canvasRect, eventsProp);
            DrawDetails(eventsProp);

            serializedObject.ApplyModifiedProperties();
        }

        // ── Toolbar ───────────────────────────────────────────────────────────

        private void DrawToolbar(SerializedProperty idProp, SerializedProperty eventsProp,
            SerializedProperty patProp)
        {
            Rect toolbarRect = new(0, 0, position.width, TOOLBAR_H);
            GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            GUILayout.Label("ID", GUILayout.Width(16f));
            idProp.stringValue = GUILayout.TextField(idProp.stringValue, GUILayout.Width(110f));

            GUILayout.Space(10f);
            GUILayout.Label("Zoom", GUILayout.Width(42f));
            pixelsPerSecond = GUILayout.HorizontalSlider(pixelsPerSecond, 40f, 700f, GUILayout.Width(100f));

            GUILayout.FlexibleSpace();

            // ── Play on device ────────────────────────────────────────────────
            bool hasIP     = !string.IsNullOrEmpty(HapticNetworkSender.DeviceIP);
            int  evCount   = eventsProp.arraySize;
            GUI.enabled = hasIP && evCount > 0;
            if (GUILayout.Button("▶ Device", EditorStyles.toolbarButton, GUILayout.Width(66f)))
                PlayOnDevice(patProp);
            GUI.enabled = true;

            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(26f)))
                PopupWindow.Show(deviceBtnRect, new DeviceSendPopup());
            if (Event.current.type == EventType.Repaint)
                deviceBtnRect = GUILayoutUtility.GetLastRect();

            GUILayout.Space(4f);

            if (GUILayout.Button("From Audio", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                PopupWindow.Show(audioFromBtnRect, new AudioImportPopup(this, eventsProp));
            if (Event.current.type == EventType.Repaint)
                audioFromBtnRect = GUILayoutUtility.GetLastRect();

            if (GUILayout.Button("+ Event", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                AddEvent(eventsProp);

            GUI.enabled = selectedEvent >= 0 && selectedEvent < eventsProp.arraySize;
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(46f)))
                DeleteSelected(eventsProp);
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        internal void PlayOnDevice(SerializedProperty patProp)
        {
            SerializedProperty idProp     = patProp.FindPropertyRelative("id");
            SerializedProperty eventsProp = patProp.FindPropertyRelative("pattern");

            var events = new HapticEvent[eventsProp.arraySize];
            for (int i = 0; i < events.Length; i++)
            {
                SerializedProperty ep = eventsProp.GetArrayElementAtIndex(i);
                events[i] = new HapticEvent
                {
                    StartTime = ep.FindPropertyRelative("StartTime").floatValue,
                    Duration  = ep.FindPropertyRelative("Duration").floatValue,
                    Intensity = ep.FindPropertyRelative("Intensity").floatValue,
                    Sharpness = ep.FindPropertyRelative("Sharpness").floatValue,
                };
            }

            // Unique ID so the device always re-registers the latest version
            string uniqueId = $"{idProp.stringValue}_{System.Guid.NewGuid():N}";
            var    pattern  = new HapticPattern(uniqueId, events);
            byte[] bytes    = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(pattern));

            if (HapticNetworkSender.Send(bytes))
                Debug.Log($"[Haptic] Sent '{uniqueId}' ({events.Length} events) → {HapticNetworkSender.DeviceIP}");
        }

        // ── Device send popup ─────────────────────────────────────────────────

        private sealed class DeviceSendPopup : PopupWindowContent
        {
            private const float ROW_H = 20f;
            private const float PAD   = 4f;

            public override Vector2 GetWindowSize()
            {
                bool     hasDiscovered = !string.IsNullOrEmpty(HapticNetworkSender.DiscoveredIP);
                string[] history       = HapticNetworkSender.IPHistory;

                float h = PAD                   // top padding
                        + ROW_H                 // IP field row
                        + PAD;                  // bottom padding

                if (hasDiscovered) h += ROW_H;

                if (history.Length > 0)
                    h += PAD + 14f             // "History" label
                       + history.Length * ROW_H;

                return new Vector2(270f, h);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Space(PAD);

                // Auto-discovered device
                string discovered = HapticNetworkSender.DiscoveredIP;
                if (!string.IsNullOrEmpty(discovered))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Found: {discovered}", EditorStyles.miniLabel);
                    if (GUILayout.Button("Use", EditorStyles.miniButton, GUILayout.Width(40f)))
                        HapticNetworkSender.DeviceIP = discovered;
                    EditorGUILayout.EndHorizontal();
                }

                // Manual IP field
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("IP", GUILayout.Width(16f));
                HapticNetworkSender.DeviceIP = EditorGUILayout.TextField(HapticNetworkSender.DeviceIP);
                EditorGUILayout.EndHorizontal();

                // History
                string[] history = HapticNetworkSender.IPHistory;
                if (history.Length > 0)
                {
                    GUILayout.Space(PAD);
                    GUILayout.Label("History",
                        new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });

                    foreach (string ip in history)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(ip, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("Use", EditorStyles.miniButton, GUILayout.Width(40f)))
                            HapticNetworkSender.DeviceIP = ip;
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        // ── Canvas ────────────────────────────────────────────────────────────

        private void DrawCanvas(Rect canvas, SerializedProperty eventsProp)
        {
            EditorGUI.DrawRect(canvas, COL_CANVAS);

            float maxH     = canvas.height - PADDING_V * 2f;
            float baseline = canvas.y + canvas.height - PADDING_V;
            float originX  = canvas.x + PADDING_H - scrollX;

            DrawGrid(canvas, maxH, baseline, originX);
            HandleCanvasInput(canvas, eventsProp, maxH, baseline, originX);
            DrawEvents(eventsProp, maxH, baseline, originX);

            // Scroll hint
            if (scrollX > 0f)
            {
                GUI.Label(
                    new Rect(canvas.x + 4f, canvas.yMax - 14f, 120f, 12f),
                    $"scroll: {scrollX:F0}px",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = COL_GRID_LABEL } });
            }
        }

        private void DrawGrid(Rect canvas, float maxH, float baseline, float originX)
        {
            GUIStyle labelStyle = new(EditorStyles.miniLabel) { normal = { textColor = COL_GRID_LABEL } };

            // ── Horizontal intensity lines ────────────────────────────────────
            for (int i = 1; i <= 4; i++)
            {
                float y = baseline - (i / 4f) * maxH;
                EditorGUI.DrawRect(new Rect(canvas.x, y, canvas.width, 1f), COL_GRID);
                GUI.Label(new Rect(canvas.x + 2f, y - 11f, 28f, 11f), $"{i * 25}%", labelStyle);
            }

            // ── Vertical time ticks ───────────────────────────────────────────
            // Pick a tick interval that keeps ~60 px between ticks
            float targetSpacing = 60f;
            float[] niceIntervals = { 0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1f, 2f, 5f, 10f };
            float interval = niceIntervals[niceIntervals.Length - 1];
            foreach (float ni in niceIntervals)
            {
                if (ni * pixelsPerSecond >= targetSpacing) { interval = ni; break; }
            }

            // First tick at or after canvas left edge
            float canvasLeft  = canvas.x;
            float canvasRight = canvas.xMax;
            float topY        = canvas.y;
            float tickBottom  = baseline + 4f;

            float timeAtLeft = (canvasLeft - originX) / pixelsPerSecond;
            int   firstTick  = Mathf.CeilToInt(timeAtLeft / interval);

            GUIStyle tickLabel = new(EditorStyles.miniLabel)
            {
                normal    = { textColor = COL_GRID_LABEL },
                alignment = TextAnchor.UpperCenter,
            };

            for (int t = firstTick; ; t++)
            {
                float time = t * interval;
                float x    = originX + time * pixelsPerSecond;
                if (x > canvasRight) break;
                if (x < canvasLeft)  continue;

                // Vertical grid line
                EditorGUI.DrawRect(new Rect(x, topY, 1f, canvas.height - PADDING_V * 0.5f), COL_GRID);

                // Tick nub below baseline
                EditorGUI.DrawRect(new Rect(x, baseline, 1f, 4f), COL_GRID_LABEL);

                // Label (show ms under 1 s, seconds otherwise)
                string label = time < 1f
                    ? $"{Mathf.RoundToInt(time * 1000)}ms"
                    : $"{time:0.#}s";
                GUI.Label(new Rect(x - 20f, tickBottom, 40f, 12f), label, tickLabel);
            }
        }

        private void DrawEvents(SerializedProperty eventsProp, float maxH, float baseline, float originX)
        {
            int count = eventsProp.arraySize;

            if (count == 0)
            {
                Rect hint = new(0, TOOLBAR_H, position.width, CANVAS_H);
                GUI.Label(hint, "No events — click '+ Event' to begin",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
                return;
            }

            for (int i = 0; i < count; i++)
            {
                GetEventRect(eventsProp, i, maxH, baseline, originX,
                    out float ex, out float ey, out float ew, out float eh);

                float sharpness = eventsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("Sharpness").floatValue;

                DrawEventVisual(ex, ey, ew, eh, baseline, sharpness, i == selectedEvent);

                // Sharpness line — highlight + cursor when hovered or active
                if (eh > BORDER_W * 3f)
                {
                    Rect sharpHitRect = SharpnessLineHitRect(ex, ey, ew, eh, sharpness, baseline);

                    bool sharpHot = hoveredEvent == i && hoveredTarget == HoverTarget.SharpnessLine;
                    if (sharpHot || (dragMode == DragMode.SharpnessLine && dragIndex == i))
                    {
                        float bw   = BORDER_W;
                        float lnY  = SharpnessLineY(ey, eh, sharpness, baseline);
                        Color highlight = new(1f, 1f, 1f, 0.9f);
                        EditorGUI.DrawRect(new Rect(ex + bw + 1f, lnY - 1f, ew - bw * 2f - 2f, 3.5f), highlight);
                    }

                    EditorGUIUtility.AddCursorRect(sharpHitRect, MouseCursor.ResizeVertical);
                }

                // Body cursor — pan when hovering over event body
                EditorGUIUtility.AddCursorRect(new Rect(ex, ey, ew, eh), MouseCursor.Pan);

                // Top-right drag handle (duration + intensity)
                bool handleHot = hoveredEvent == i && hoveredTarget == HoverTarget.EventHandle;
                DrawHandle(EventHandleRect(ex, ey, ew), handleHot ? COL_HANDLE_HOV : COL_HANDLE);

            }
        }

        /// <summary>
        /// Draws a single haptic event block with sharpness encoded three ways:
        /// border color (cyan→white) and inner line position.
        /// Shared by the EditorWindow and the PropertyDrawer preview.
        /// </summary>
        internal static void DrawEventVisual(float ex, float ey, float ew, float eh,
            float baseline, float sharpness, bool isSelected)
        {
            float bw = BORDER_W;

            // 2. Color: lerp soft-cyan → near-white
            Color lineCol = isSelected
                ? COL_SELECTED
                : Color.Lerp(COL_LINE_SOFT, COL_LINE_SHARP, sharpness);
            Color fillCol = Color.Lerp(COL_FILL_SOFT, COL_FILL_SHARP, sharpness);

            // Fill
            EditorGUI.DrawRect(new Rect(ex + bw, ey + bw, ew - bw * 2f, eh - bw), fillCol);

            // П-shape: left | top | right
            EditorGUI.DrawRect(new Rect(ex,            ey, bw, eh), lineCol);
            EditorGUI.DrawRect(new Rect(ex,            ey, ew, bw),       lineCol);
            EditorGUI.DrawRect(new Rect(ex + ew - bw,  ey, bw, eh), lineCol);

            // 3. Inner sharpness line — always rendered (min display position = 0.03)
            if (eh > bw * 3f)
            {
                float lineY = SharpnessLineY(ey, eh, sharpness, baseline);
                Color lineInner = new(lineCol.r, lineCol.g, lineCol.b, 0.65f);
                EditorGUI.DrawRect(new Rect(ex + bw + 1f, lineY, ew - bw * 2f - 2f, 1.5f), lineInner);
            }
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void HandleCanvasInput(Rect canvas, SerializedProperty eventsProp,
            float maxH, float baseline, float originX)
        {
            Event e    = Event.current;
            int   count = eventsProp.arraySize;

            // ── Delete key ────────────────────────────────────────────────────
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) &&
                selectedEvent >= 0 && selectedEvent < count)
            {
                DeleteSelected(eventsProp);
                e.Use();
                return;
            }

            // ── Hover detection ──────────────────────────────────────────────
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                int        prevEvent  = hoveredEvent;
                HoverTarget prevTarget = hoveredTarget;
                hoveredEvent  = -1;
                hoveredTarget = HoverTarget.None;

                for (int i = 0; i < count && hoveredEvent < 0; i++)
                {
                    GetEventRect(eventsProp, i, maxH, baseline, originX,
                        out float ex, out float ey, out float ew, out float eh);

                    float sharpness = eventsProp.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("Sharpness").floatValue;

                    if (EventHandleRect(ex, ey, ew).Contains(e.mousePosition))
                    {
                        hoveredEvent  = i;
                        hoveredTarget = HoverTarget.EventHandle;
                    }
                    else if (eh > BORDER_W * 3f &&
                             SharpnessLineHitRect(ex, ey, ew, eh, sharpness, baseline).Contains(e.mousePosition))
                    {
                        hoveredEvent  = i;
                        hoveredTarget = HoverTarget.SharpnessLine;
                    }
                    if (hoveredEvent < 0 && new Rect(ex, ey, ew, eh).Contains(e.mousePosition))
                    {
                        hoveredEvent  = i;
                        hoveredTarget = HoverTarget.MoveEvent;
                    }
                }

                if (hoveredEvent != prevEvent || hoveredTarget != prevTarget)
                    Repaint();
            }

            // ── Mouse down ───────────────────────────────────────────────────
            if (e.type == EventType.MouseDown && e.button == 0 && canvas.Contains(e.mousePosition))
            {
                bool consumed = false;

                // Pass 1: precise handles take priority
                for (int i = 0; i < count && !consumed; i++)
                {
                    GetEventRect(eventsProp, i, maxH, baseline, originX,
                        out float ex, out float ey, out float ew, out float eh);

                    if (EventHandleRect(ex, ey, ew).Contains(e.mousePosition))
                    {
                        SerializedProperty ev = eventsProp.GetArrayElementAtIndex(i);
                        RecordUndo("Resize Haptic Event");
                        dragMode           = DragMode.EventHandle;
                        dragIndex          = i;
                        dragStartMouse     = e.mousePosition;
                        dragStartDuration  = ev.FindPropertyRelative("Duration").floatValue;
                        dragStartIntensity = ev.FindPropertyRelative("Intensity").floatValue;
                        selectedEvent      = i;
                        e.Use();
                        consumed = true;
                        continue;
                    }

                    float sharp = eventsProp.GetArrayElementAtIndex(i).FindPropertyRelative("Sharpness").floatValue;
                    if (eh > BORDER_W * 3f &&
                        SharpnessLineHitRect(ex, ey, ew, eh, sharp, baseline).Contains(e.mousePosition))
                    {
                        RecordUndo("Change Haptic Sharpness");
                        dragMode           = DragMode.SharpnessLine;
                        dragIndex          = i;
                        dragStartMouse     = e.mousePosition;
                        dragStartSharpness = sharp;
                        dragStartEh        = eh;
                        selectedEvent      = i;
                        e.Use();
                        consumed = true;
                    }
                }

                // Pass 2: body — collect all hits, cycle selection
                if (!consumed)
                {
                    var bodyHits = new List<int>();
                    for (int i = 0; i < count; i++)
                    {
                        GetEventRect(eventsProp, i, maxH, baseline, originX,
                            out float ex, out float ey, out float ew, out float eh);
                        if (new Rect(ex, ey, ew, eh).Contains(e.mousePosition))
                            bodyHits.Add(i);
                    }

                    if (bodyHits.Count > 0)
                    {
                        // Cycle: if selected is already one of the hits, advance to next
                        int currentIdx  = bodyHits.IndexOf(selectedEvent);
                        int nextSel     = currentIdx >= 0
                            ? bodyHits[(currentIdx + 1) % bodyHits.Count]
                            : bodyHits[0];

                        GetEventRect(eventsProp, nextSel, maxH, baseline, originX,
                            out float _, out float _, out float _, out float _);
                        SerializedProperty ev = eventsProp.GetArrayElementAtIndex(nextSel);
                        RecordUndo("Move Haptic Event");
                        dragMode              = DragMode.MoveEvent;
                        dragIndex             = nextSel;
                        dragStartMouse        = e.mousePosition;
                        dragStartTime         = ev.FindPropertyRelative("StartTime").floatValue;
                        selectedEvent         = nextSel;
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                        e.Use();
                    }
                }
            }

            // ── Mouse drag ───────────────────────────────────────────────────
            if (e.type == EventType.MouseDrag && e.button == 0 && dragMode != DragMode.None)
            {
                Vector2 delta = e.mousePosition - dragStartMouse;

                if (dragMode == DragMode.EventHandle && dragIndex < count)
                {
                    SerializedProperty ev = eventsProp.GetArrayElementAtIndex(dragIndex);
                    ev.FindPropertyRelative("Duration").floatValue =
                        Mathf.Max(dragStartDuration + delta.x / pixelsPerSecond, MIN_DUR);
                    ev.FindPropertyRelative("Intensity").floatValue =
                        Mathf.Clamp01(dragStartIntensity - delta.y / maxH);

                    serializedObject.ApplyModifiedProperties();
                    e.Use();
                    Repaint();
                }
                else if (dragMode == DragMode.SharpnessLine && dragIndex < count)
                {
                    // Drag up → sharpness increases (line moves toward top of block)
                    float newSharpness = Mathf.Clamp01(dragStartSharpness - delta.y / dragStartEh);
                    eventsProp.GetArrayElementAtIndex(dragIndex)
                        .FindPropertyRelative("Sharpness").floatValue = newSharpness;

                    serializedObject.ApplyModifiedProperties();
                    e.Use();
                    Repaint();
                }
                else if (dragMode == DragMode.MoveEvent && dragIndex < count)
                {
                    float newStart = Mathf.Max(0f, dragStartTime + delta.x / pixelsPerSecond);
                    eventsProp.GetArrayElementAtIndex(dragIndex)
                        .FindPropertyRelative("StartTime").floatValue = newStart;

                    serializedObject.ApplyModifiedProperties();
                    e.Use();
                    Repaint();
                }
            }

            // ── Mouse up ─────────────────────────────────────────────────────
            if (e.type == EventType.MouseUp && dragMode != DragMode.None)
            {
                GUIUtility.hotControl = 0;
                dragMode = DragMode.None;
                e.Use();
            }

            // ── Scroll (horizontal pan) ───────────────────────────────────────
            if (e.type == EventType.ScrollWheel && canvas.Contains(e.mousePosition))
            {
                scrollX = Mathf.Max(0f, scrollX + e.delta.y * 12f);
                e.Use();
                Repaint();
            }
        }

        // ── Details panel ─────────────────────────────────────────────────────

        private void DrawDetails(SerializedProperty eventsProp)
        {
            float y = TOOLBAR_H + CANVAS_H;
            Rect  r = new(0, y, position.width, position.height - y);

            EditorGUI.DrawRect(r, COL_DETAILS_BG);

            GUILayout.BeginArea(r);

            if (selectedEvent >= 0 && selectedEvent < eventsProp.arraySize)
            {
                SerializedProperty ev = eventsProp.GetArrayElementAtIndex(selectedEvent);

                EditorGUILayout.LabelField($"Event  [{selectedEvent}]", EditorStyles.boldLabel);

                FloatField(ev.FindPropertyRelative("StartTime"), "Start Time");
                FloatField(ev.FindPropertyRelative("Duration"),  "Duration");
                FloatField(ev.FindPropertyRelative("Intensity"), "Intensity");
                FloatField(ev.FindPropertyRelative("Sharpness"), "Sharpness", "(iOS only)");
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select an event to edit its values",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndArea();
        }

        private static void FloatField(SerializedProperty prop, string label, string suffix = null)
        {
            EditorGUILayout.BeginHorizontal();
            string fullLabel = suffix != null ? $"{label} {suffix}" : label;
            EditorGUILayout.LabelField(fullLabel, GUILayout.Width(140f));
            prop.floatValue = EditorGUILayout.FloatField(prop.floatValue);
            EditorGUILayout.EndHorizontal();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void GetEventRect(SerializedProperty eventsProp, int index,
            float maxH, float baseline, float originX,
            out float ex, out float ey, out float ew, out float eh)
        {
            SerializedProperty e = eventsProp.GetArrayElementAtIndex(index);
            float startTime = e.FindPropertyRelative("StartTime").floatValue;
            float duration  = e.FindPropertyRelative("Duration").floatValue;
            float intensity = e.FindPropertyRelative("Intensity").floatValue;

            ex = originX + startTime * pixelsPerSecond;
            ew = Mathf.Max(duration  * pixelsPerSecond, 4f);
            eh = Mathf.Max(intensity * maxH,            4f);
            ey = baseline - eh;
        }

        private void RecordUndo(string name) =>
            Undo.RecordObject(serializedObject.targetObject, name);

        private static Rect EventHandleRect(float ex, float ey, float ew) =>
            new(ex + ew - HANDLE_R, ey - HANDLE_R, HANDLE_R * 2f, HANDLE_R * 2f);

        private static float SharpnessLineY(float ey, float eh, float sharpness, float baseline)
        {
            float bw             = BORDER_W;
            float displaySharp   = Mathf.Max(sharpness, 0.03f); // always render at minimum 0.03 position
            float lineY          = ey + (1f - displaySharp) * eh;
            return Mathf.Clamp(lineY, ey + bw + 1f, baseline - bw - 1f);
        }

        private static Rect SharpnessLineHitRect(float ex, float ey, float ew, float eh,
            float sharpness, float baseline)
        {
            float bw    = BORDER_W;
            float lineY = SharpnessLineY(ey, eh, sharpness, baseline);
            return new Rect(ex + bw + 1f, lineY - 5f, ew - bw * 2f - 2f, 11f);
        }

        private static void DrawHandle(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
        }

        private void GenerateFromAudio(SerializedProperty eventsProp)
        {
            if (audioClip == null) return;

            if (eventsProp.arraySize > 0 && !EditorUtility.DisplayDialog(
                    "Replace Pattern",
                    $"This will replace all {eventsProp.arraySize} existing event(s). Continue?",
                    "Generate", "Cancel"))
                return;

            int   channels     = audioClip.channels;
            int   sampleRate   = audioClip.frequency;
            int   totalSamples = audioClip.samples;

            float[] data = new float[totalSamples * channels];
            if (!audioClip.GetData(data, 0))
            {
                Debug.LogWarning("[Haptic] Could not read AudioClip data. " +
                    "Ensure the clip's Load Type is set to 'Decompress On Load'.");
                return;
            }

            // ── Step 1: RMS per 10 ms window (all channels averaged) ─────────
            int   winSamples  = Mathf.Max(1, sampleRate / 100); // 10 ms
            float winDuration = winSamples / (float)sampleRate;
            int   winCount    = Mathf.CeilToInt((float)totalSamples / winSamples);

            float[] rms = new float[winCount];
            for (int w = 0; w < winCount; w++)
            {
                int   start = w * winSamples;
                int   end   = Mathf.Min(start + winSamples, totalSamples);
                float sum   = 0f;
                for (int s = start; s < end; s++)
                {
                    float avg = 0f;
                    for (int c = 0; c < channels; c++) avg += data[s * channels + c];
                    avg /= channels;
                    sum += avg * avg;
                }
                rms[w] = Mathf.Sqrt(sum / (end - start));
            }

            // ── Step 2: normalize ─────────────────────────────────────────────
            float maxRms = 0f;
            foreach (float v in rms) if (v > maxRms) maxRms = v;
            if (maxRms < 1e-5f) { Debug.LogWarning("[Haptic] AudioClip appears silent."); return; }
            for (int i = 0; i < rms.Length; i++) rms[i] /= maxRms;

            // ── Step 3: onset detection — local peaks above threshold ─────────
            float minGapSec     = audioMinGapMs / 1000f;
            int   minGapWindows = Mathf.Max(1, Mathf.RoundToInt(minGapSec / winDuration));

            var events = new List<(float time, float intensity, float sharpness, float duration)>();
            int lastPeakWin = -minGapWindows;

            for (int i = 1; i < rms.Length - 1; i++)
            {
                if (rms[i] < audioThreshold) continue;
                if (rms[i] <= rms[i - 1] || rms[i] < rms[i + 1]) continue; // must be local max
                if (i - lastPeakWin < minGapWindows) continue;

                float intensity = rms[i];

                // Sharpness: how steeply the amplitude rose into this peak (over ~30 ms)
                float prev      = i >= 3 ? rms[i - 3] : 0f;
                float sharpness = Mathf.Clamp01((intensity - prev) * 1.5f);

                // Duration: how long signal stays above threshold * 0.4 after peak
                float dur = winDuration;
                for (int j = i + 1; j < rms.Length; j++)
                {
                    if (rms[j] < audioThreshold * 0.4f) break;
                    dur += winDuration;
                }
                dur = Mathf.Clamp(dur, 0.02f, 0.5f);

                events.Add((i * winDuration, intensity, sharpness, dur));
                lastPeakWin = i;
            }

            // ── Step 4: write to SerializedProperty ───────────────────────────
            RecordUndo("Generate Haptic Pattern from Audio");
            eventsProp.arraySize = events.Count;
            for (int i = 0; i < events.Count; i++)
            {
                var (time, intensity, sharpness, duration) = events[i];
                SerializedProperty p = eventsProp.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("StartTime").floatValue = time;
                p.FindPropertyRelative("Duration").floatValue  = duration;
                p.FindPropertyRelative("Intensity").floatValue = intensity;
                p.FindPropertyRelative("Sharpness").floatValue = sharpness;
            }

            selectedEvent = events.Count > 0 ? 0 : -1;
            serializedObject.ApplyModifiedProperties();
            Repaint();
            Debug.Log($"[Haptic] Generated {events.Count} event(s) from '{audioClip.name}'.");
        }

        // ── Audio import popup ────────────────────────────────────────────────

        private sealed class AudioImportPopup : PopupWindowContent
        {
            private readonly HapticPatternEditorWindow parent;
            private readonly SerializedProperty        eventsProp;

            public AudioImportPopup(HapticPatternEditorWindow parent, SerializedProperty eventsProp)
            {
                this.parent     = parent;
                this.eventsProp = eventsProp;
            }

            public override Vector2 GetWindowSize() => new(290f, 88f);

            public override void OnGUI(Rect rect)
            {
                GUILayout.Space(4f);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Audio Clip", GUILayout.Width(72f));
                parent.audioClip = (AudioClip)EditorGUILayout.ObjectField(
                    parent.audioClip, typeof(AudioClip), false);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Threshold", GUILayout.Width(72f));
                parent.audioThreshold = EditorGUILayout.Slider(parent.audioThreshold, 0.05f, 0.95f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Min Gap (ms)", GUILayout.Width(72f));
                parent.audioMinGapMs = EditorGUILayout.FloatField(parent.audioMinGapMs);
                GUILayout.EndHorizontal();

                GUILayout.Space(4f);

                GUI.enabled = parent.audioClip != null;
                if (GUILayout.Button("Generate"))
                {
                    parent.GenerateFromAudio(eventsProp);
                    editorWindow.Close();
                }
                GUI.enabled = true;
            }
        }

        private void DeleteSelected(SerializedProperty eventsProp)
        {
            if (selectedEvent < 0 || selectedEvent >= eventsProp.arraySize) return;
            RecordUndo("Delete Haptic Event");
            eventsProp.DeleteArrayElementAtIndex(selectedEvent);
            serializedObject.ApplyModifiedProperties();
            selectedEvent = Mathf.Clamp(selectedEvent - 1, -1, eventsProp.arraySize - 1);
            Repaint();
        }

        private void AddEvent(SerializedProperty eventsProp)
        {
            RecordUndo("Add Haptic Event");
            int idx = eventsProp.arraySize;
            eventsProp.arraySize++;

            SerializedProperty e = eventsProp.GetArrayElementAtIndex(idx);

            float startTime = 0f;
            if (idx > 0)
            {
                SerializedProperty prev = eventsProp.GetArrayElementAtIndex(idx - 1);
                startTime = prev.FindPropertyRelative("StartTime").floatValue
                          + prev.FindPropertyRelative("Duration").floatValue
                          + 0.1f;
            }

            e.FindPropertyRelative("StartTime").floatValue = startTime;
            e.FindPropertyRelative("Duration").floatValue  = 0.3f;
            e.FindPropertyRelative("Intensity").floatValue = 0.5f;
            e.FindPropertyRelative("Sharpness").floatValue = 0f;

            selectedEvent = idx;
        }
    }
}
