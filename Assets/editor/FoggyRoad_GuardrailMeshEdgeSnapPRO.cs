// FoggyRoad_GuardrailMeshEdgeSnapPRO.cs
// Unity 6+ Editor tool
// Put in: Assets/Editor/FoggyRoad_GuardrailMeshEdgeSnapPRO.cs
//
// Purpose:
// Snap modular Blender guardrails to the ACTUAL left/right boundary edges
// of a curved road mesh. Unlike Collider.ClosestPoint(), this reads the
// road's triangle topology and extracts the real outer boundary edges.
//
// Expected Blender module names are supported automatically:
//   GRPRO_L_000_0000m
//   GRPRO_R_000_0000m
//
// Menu:
//   Tools > Foggy Road > Guardrail Mesh Edge Snap PRO

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FoggyRoadTools.MeshEdgeSnap
{
    public class GuardrailMeshEdgeSnapPRO : EditorWindow
    {
        [Serializable]
        private struct BoundarySegment
        {
            public Vector3 a;
            public Vector3 b;

            public BoundarySegment(Vector3 a, Vector3 b)
            {
                this.a = a;
                this.b = b;
            }
        }

        private class RailModule
        {
            public Transform transform;
            public Bounds bounds;
            public int station;
            public bool isLeft;
            public Vector3 originalCenter;
        }

        private class RailPair
        {
            public int station;
            public RailModule left;
            public RailModule right;
            public Vector3 center;
        }

        [Header("Targets")]
        [SerializeField] private GameObject roadRoot;
        [SerializeField] private Transform guardrailRoot;

        [Header("Snap")]
        [SerializeField] private float outsideOffset = 0.22f;
        [SerializeField] private float postEmbedDepth = 0.08f;
        [SerializeField] private bool snapHeight = true;
        [SerializeField] private bool alignYawToRoad = true;

        [Header("Detection")]
        [Tooltip("Top-facing triangles must have normal.y >= this value.")]
        [SerializeField, Range(0.0f, 0.95f)] private float topNormalThreshold = 0.25f;

        [Tooltip("Boundary edge direction should roughly follow the local road direction. This rejects road start/end cap edges.")]
        [SerializeField, Range(0.0f, 0.95f)] private float edgeDirectionThreshold = 0.30f;

        [Tooltip("Safety radius around each rail station.")]
        [SerializeField] private float maxBoundarySearchDistance = 25.0f;

        [Header("Names")]
        [SerializeField] private bool useModuleNamesForLeftRight = true;

        [Header("Debug")]
        [SerializeField] private bool drawDetectedRoadEdges = true;
        [SerializeField] private bool verboseLog = true;

        private readonly List<BoundarySegment> cachedBoundary = new List<BoundarySegment>();
        private Vector2 scroll;

        private static readonly Regex LeftRegex =
            new Regex(@"(?:^|_)L_(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RightRegex =
            new Regex(@"(?:^|_)R_(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [MenuItem("Tools/Foggy Road/Guardrail Mesh Edge Snap PRO")]
        public static void OpenWindow()
        {
            GuardrailMeshEdgeSnapPRO w = CreateInstance<GuardrailMeshEdgeSnapPRO>();
            w.titleContent = new GUIContent("Guardrail Edge Snap PRO");
            w.minSize = new Vector2(470, 650);
            w.position = new Rect(100, 70, 510, 720);
            w.ShowUtility();
            w.Focus();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneDebug;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneDebug;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("GUARDRAIL MESH EDGE SNAP PRO", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This version reads the actual OUTER EDGES of the road mesh. " +
                "It does not use Collider.ClosestPoint, so a curved road can be snapped much more accurately.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("1. TARGETS", EditorStyles.boldLabel);

            roadRoot = (GameObject)EditorGUILayout.ObjectField(
                "Road Root", roadRoot, typeof(GameObject), true);

            guardrailRoot = (Transform)EditorGUILayout.ObjectField(
                "Guardrail Root", guardrailRoot, typeof(Transform), true);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. FIT SETTINGS", EditorStyles.boldLabel);

            outsideOffset = EditorGUILayout.FloatField("Outside Edge Offset (m)", outsideOffset);
            snapHeight = EditorGUILayout.ToggleLeft("Snap post bottom to road edge height", snapHeight);

            using (new EditorGUI.DisabledScope(!snapHeight))
            {
                postEmbedDepth = EditorGUILayout.FloatField("Post Embed Depth (m)", postEmbedDepth);
            }

            alignYawToRoad = EditorGUILayout.ToggleLeft("Align each module to local road direction", alignYawToRoad);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("3. ROAD EDGE DETECTION", EditorStyles.boldLabel);

            topNormalThreshold = EditorGUILayout.Slider(
                "Top Surface Threshold", topNormalThreshold, 0.0f, 0.95f);

            edgeDirectionThreshold = EditorGUILayout.Slider(
                "Side Edge Direction Match", edgeDirectionThreshold, 0.0f, 0.95f);

            maxBoundarySearchDistance = EditorGUILayout.FloatField(
                "Max Edge Search Distance", maxBoundarySearchDistance);

            useModuleNamesForLeftRight = EditorGUILayout.ToggleLeft(
                "Read L / R from module names", useModuleNamesForLeftRight);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("4. DEBUG", EditorStyles.boldLabel);

            drawDetectedRoadEdges = EditorGUILayout.ToggleLeft(
                "Show detected road boundary in Scene view", drawDetectedRoadEdges);

            verboseLog = EditorGUILayout.ToggleLeft("Detailed Console log", verboseLog);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(roadRoot == null))
            {
                if (GUILayout.Button("DETECT ROAD OUTER EDGES", GUILayout.Height(32)))
                {
                    DetectRoadEdges();
                }
            }

            using (new EditorGUI.DisabledScope(roadRoot == null || guardrailRoot == null))
            {
                GUI.backgroundColor = new Color(0.55f, 1f, 0.55f);

                if (GUILayout.Button("AUTO SNAP LEFT + RIGHT TO ROAD", GUILayout.Height(46)))
                {
                    AutoSnap();
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Frame Road + Guardrails", GUILayout.Height(26)))
            {
                FrameTargets();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Recommended first test:\n" +
                "Outside Edge Offset = 0.15–0.30 m\n" +
                "Post Embed = 0.05–0.10 m\n" +
                "Top Surface Threshold = 0.25\n" +
                "Side Edge Direction Match = 0.30\n\n" +
                "Your Blender names GRPRO_L_... and GRPRO_R_... are recognized automatically.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ============================================================
        // MAIN
        // ============================================================

        private void AutoSnap()
        {
            if (!ValidateTargets())
                return;

            if (!DetectRoadEdges())
                return;

            List<RailModule> modules = CollectRailModules();

            if (modules.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Edge Snap PRO",
                    "No guardrail modules were found directly under Guardrail Root.",
                    "OK");
                return;
            }

            List<RailPair> pairs = BuildPairs(modules);

            if (pairs.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Edge Snap PRO",
                    "Could not pair L/R modules.\n\n" +
                    "Expected names similar to:\nGRPRO_L_000_0000m\nGRPRO_R_000_0000m",
                    "OK");
                return;
            }

            // Rail pair midpoint is an excellent estimate of the Blender road
            // centerline because both rails were generated from the same path.
            // It remains centered even when the original half-width is wrong.
            pairs.Sort((x, y) => x.station.CompareTo(y.station));

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Snap Guardrails To Actual Road Edges");

            List<UnityEngine.Object> undoObjects = new List<UnityEngine.Object>();
            foreach (RailPair pair in pairs)
            {
                if (pair.left != null) undoObjects.Add(pair.left.transform);
                if (pair.right != null) undoObjects.Add(pair.right.transform);
            }

            Undo.RecordObjects(undoObjects.ToArray(), "Snap Guardrails To Road Edges");

            int leftDone = 0;
            int rightDone = 0;
            int failed = 0;

            for (int i = 0; i < pairs.Count; i++)
            {
                RailPair pair = pairs[i];

                Vector3 center = pair.center;
                Vector3 tangent = GetPairTangent(pairs, i);

                tangent.y = 0f;

                if (tangent.sqrMagnitude < 0.0001f)
                {
                    failed++;
                    continue;
                }

                tangent.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

                // LEFT
                if (pair.left != null)
                {
                    if (FindBestSideBoundary(
                        center,
                        tangent,
                        right,
                        -1f,
                        out Vector3 edgePoint,
                        out Vector3 edgeTangent))
                    {
                        FitModule(
                            pair.left,
                            center,
                            tangent,
                            edgePoint,
                            edgeTangent,
                            -right);

                        leftDone++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                // RIGHT
                if (pair.right != null)
                {
                    if (FindBestSideBoundary(
                        center,
                        tangent,
                        right,
                        +1f,
                        out Vector3 edgePoint,
                        out Vector3 edgeTangent))
                    {
                        FitModule(
                            pair.right,
                            center,
                            tangent,
                            edgePoint,
                            edgeTangent,
                            right);

                        rightDone++;
                    }
                    else
                    {
                        failed++;
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();

            if (verboseLog)
            {
                Debug.Log(
                    $"[Guardrail Edge Snap PRO] Boundary segments={cachedBoundary.Count}, " +
                    $"Pairs={pairs.Count}, Left fitted={leftDone}, Right fitted={rightDone}, Failed={failed}");
            }

            EditorUtility.DisplayDialog(
                "Guardrail Edge Snap PRO",
                $"Finished.\n\nLeft fitted: {leftDone}\nRight fitted: {rightDone}\nFailed: {failed}\n\n" +
                "If rails are slightly too far from asphalt, reduce Outside Edge Offset.",
                "OK");
        }

        private void FitModule(
            RailModule module,
            Vector3 sourceCenterlinePoint,
            Vector3 sourcePathTangent,
            Vector3 roadEdgePoint,
            Vector3 roadEdgeTangent,
            Vector3 outwardDirection)
        {
            Transform t = module.transform;

            // Re-read current bounds because prior paired operations may have moved objects.
            if (!TryGetBounds(t, out Bounds b))
                return;

            Vector3 moduleCenter = b.center;

            Vector3 outward = outwardDirection;
            outward.y = 0f;

            if (outward.sqrMagnitude < 0.0001f)
                return;

            outward.Normalize();

            Vector3 targetCenter =
                roadEdgePoint +
                outward * Mathf.Max(0f, outsideOffset);

            Vector3 move = new Vector3(
                targetCenter.x - moduleCenter.x,
                0f,
                targetCenter.z - moduleCenter.z);

            if (snapHeight)
            {
                float desiredBottomY =
                    roadEdgePoint.y - Mathf.Max(0f, postEmbedDepth);

                move.y =
                    desiredBottomY - b.min.y;
            }

            t.position += move;

            if (alignYawToRoad)
            {
                Vector3 srcTan = sourcePathTangent;
                Vector3 dstTan = roadEdgeTangent;

                srcTan.y = 0f;
                dstTan.y = 0f;

                if (srcTan.sqrMagnitude > 0.0001f &&
                    dstTan.sqrMagnitude > 0.0001f)
                {
                    srcTan.Normalize();
                    dstTan.Normalize();

                    // Boundary direction is unsigned. Pick whichever direction
                    // is closest to the current rail direction.
                    if (Vector3.Dot(srcTan, dstTan) < 0f)
                        dstTan = -dstTan;

                    float yawDelta = Vector3.SignedAngle(
                        srcTan,
                        dstTan,
                        Vector3.up);

                    t.rotation =
                        Quaternion.AngleAxis(yawDelta, Vector3.up) *
                        t.rotation;
                }
            }

            EditorUtility.SetDirty(t);
        }

        // ============================================================
        // ROAD BOUNDARY EXTRACTION
        // ============================================================

        private bool DetectRoadEdges()
        {
            cachedBoundary.Clear();

            if (roadRoot == null)
                return false;

            MeshFilter[] meshFilters =
                roadRoot.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null)
                    continue;

                ExtractBoundaryFromMeshFilter(mf, topNormalThreshold, cachedBoundary);
            }

            // Fallback: some imported meshes can have reversed triangle winding.
            if (cachedBoundary.Count == 0)
            {
                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf == null || mf.sharedMesh == null)
                        continue;

                    ExtractBoundaryFromMeshFilter(
                        mf,
                        -1f, // special fallback mode: abs(normal.y)
                        cachedBoundary);
                }
            }

            SceneView.RepaintAll();

            if (cachedBoundary.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Edge Snap PRO",
                    "No road boundary edges were detected.\n\n" +
                    "Make sure Road Root contains the actual road MeshFilter.",
                    "OK");

                return false;
            }

            if (verboseLog)
                Debug.Log($"[Guardrail Edge Snap PRO] Detected {cachedBoundary.Count} boundary segments.");

            return true;
        }

        private static void ExtractBoundaryFromMeshFilter(
            MeshFilter mf,
            float normalThreshold,
            List<BoundarySegment> output)
        {
            Mesh mesh = mf.sharedMesh;
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            Dictionary<ulong, int> edgeUseCount =
                new Dictionary<ulong, int>();

            Dictionary<ulong, Vector2Int> edgeVerts =
                new Dictionary<ulong, Vector2Int>();

            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int ia = tris[i];
                int ib = tris[i + 1];
                int ic = tris[i + 2];

                Vector3 wa = mf.transform.TransformPoint(verts[ia]);
                Vector3 wb = mf.transform.TransformPoint(verts[ib]);
                Vector3 wc = mf.transform.TransformPoint(verts[ic]);

                Vector3 n = Vector3.Cross(wb - wa, wc - wa).normalized;
                float upDot = Vector3.Dot(n, Vector3.up);

                bool isTop =
                    normalThreshold >= 0f
                    ? upDot >= normalThreshold
                    : Mathf.Abs(upDot) >= 0.25f;

                if (!isTop)
                    continue;

                CountEdge(ia, ib, edgeUseCount, edgeVerts);
                CountEdge(ib, ic, edgeUseCount, edgeVerts);
                CountEdge(ic, ia, edgeUseCount, edgeVerts);
            }

            foreach (KeyValuePair<ulong, int> kv in edgeUseCount)
            {
                if (kv.Value != 1)
                    continue;

                Vector2Int e = edgeVerts[kv.Key];

                if (e.x < 0 || e.x >= verts.Length ||
                    e.y < 0 || e.y >= verts.Length)
                    continue;

                Vector3 a = mf.transform.TransformPoint(verts[e.x]);
                Vector3 b = mf.transform.TransformPoint(verts[e.y]);

                Vector3 horizontal = b - a;
                horizontal.y = 0f;

                if (horizontal.sqrMagnitude < 0.000001f)
                    continue;

                output.Add(new BoundarySegment(a, b));
            }
        }

        private static void CountEdge(
            int a,
            int b,
            Dictionary<ulong, int> counts,
            Dictionary<ulong, Vector2Int> verts)
        {
            int lo = Mathf.Min(a, b);
            int hi = Mathf.Max(a, b);

            ulong key =
                ((ulong)(uint)lo << 32) |
                (uint)hi;

            if (counts.TryGetValue(key, out int c))
                counts[key] = c + 1;
            else
            {
                counts[key] = 1;
                verts[key] = new Vector2Int(lo, hi);
            }
        }

        // ============================================================
        // SIDE MATCHING
        // ============================================================

        private bool FindBestSideBoundary(
            Vector3 center,
            Vector3 roadTangent,
            Vector3 right,
            float desiredSideSign,
            out Vector3 bestPoint,
            out Vector3 bestTangent)
        {
            bestPoint = Vector3.zero;
            bestTangent = Vector3.forward;

            float bestScore = float.MaxValue;
            bool found = false;

            Vector2 c2 = new Vector2(center.x, center.z);
            Vector2 roadTan2 = new Vector2(roadTangent.x, roadTangent.z).normalized;
            Vector2 right2 = new Vector2(right.x, right.z).normalized;

            for (int i = 0; i < cachedBoundary.Count; i++)
            {
                BoundarySegment seg = cachedBoundary[i];

                Vector2 a = new Vector2(seg.a.x, seg.a.z);
                Vector2 b = new Vector2(seg.b.x, seg.b.z);

                Vector2 segDir = b - a;
                float segLen = segDir.magnitude;

                if (segLen < 0.0001f)
                    continue;

                segDir /= segLen;

                // Reject start/end perimeter edges that cross the road.
                float directionMatch =
                    Mathf.Abs(Vector2.Dot(segDir, roadTan2));

                if (directionMatch < edgeDirectionThreshold)
                    continue;

                Vector2 q = ClosestPointOnSegment2D(c2, a, b);
                Vector2 delta = q - c2;

                float distance = delta.magnitude;

                if (distance > maxBoundarySearchDistance)
                    continue;

                float side =
                    Vector2.Dot(delta, right2);

                if (desiredSideSign < 0f && side >= -0.001f)
                    continue;

                if (desiredSideSign > 0f && side <= 0.001f)
                    continue;

                float along =
                    Mathf.Abs(Vector2.Dot(delta, roadTan2));

                // Prefer the locally nearest edge and edges parallel to the route.
                float score =
                    distance +
                    along * 0.35f +
                    (1f - directionMatch) * 2.0f;

                if (score >= bestScore)
                    continue;

                float t = SegmentT2D(c2, a, b);

                bestPoint = Vector3.Lerp(seg.a, seg.b, t);

                Vector3 dir3 = seg.b - seg.a;
                dir3.y = 0f;

                if (dir3.sqrMagnitude > 0.0001f)
                    bestTangent = dir3.normalized;

                bestScore = score;
                found = true;
            }

            // Relax side-direction filter if the road has coarse topology.
            if (!found && edgeDirectionThreshold > 0.01f)
            {
                float oldThreshold = edgeDirectionThreshold;
                edgeDirectionThreshold = 0f;

                bool retry = FindBestSideBoundary(
                    center,
                    roadTangent,
                    right,
                    desiredSideSign,
                    out bestPoint,
                    out bestTangent);

                edgeDirectionThreshold = oldThreshold;
                return retry;
            }

            return found;
        }

        private static Vector2 ClosestPointOnSegment2D(
            Vector2 p,
            Vector2 a,
            Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = Vector2.Dot(ab, ab);

            if (denom <= 0.0000001f)
                return a;

            float t =
                Mathf.Clamp01(
                    Vector2.Dot(p - a, ab) / denom);

            return a + ab * t;
        }

        private static float SegmentT2D(
            Vector2 p,
            Vector2 a,
            Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = Vector2.Dot(ab, ab);

            if (denom <= 0.0000001f)
                return 0f;

            return Mathf.Clamp01(
                Vector2.Dot(p - a, ab) / denom);
        }

        // ============================================================
        // MODULES / PAIRS
        // ============================================================

        private List<RailModule> CollectRailModules()
        {
            List<RailModule> result = new List<RailModule>();

            for (int i = 0; i < guardrailRoot.childCount; i++)
            {
                Transform child = guardrailRoot.GetChild(i);

                if (!TryGetBounds(child, out Bounds b))
                    continue;

                string name = child.name;

                bool isLeft = false;
                bool isRight = false;
                int station = -1;

                if (useModuleNamesForLeftRight)
                {
                    Match ml = LeftRegex.Match(name);
                    Match mr = RightRegex.Match(name);

                    if (ml.Success)
                    {
                        isLeft = true;
                        int.TryParse(ml.Groups[1].Value, out station);
                    }
                    else if (mr.Success)
                    {
                        isRight = true;
                        int.TryParse(mr.Groups[1].Value, out station);
                    }
                }

                if (!isLeft && !isRight)
                    continue;

                result.Add(new RailModule
                {
                    transform = child,
                    bounds = b,
                    station = station,
                    isLeft = isLeft,
                    originalCenter = b.center
                });
            }

            return result;
        }

        private static List<RailPair> BuildPairs(List<RailModule> modules)
        {
            Dictionary<int, RailPair> map =
                new Dictionary<int, RailPair>();

            foreach (RailModule m in modules)
            {
                if (m.station < 0)
                    continue;

                if (!map.TryGetValue(m.station, out RailPair pair))
                {
                    pair = new RailPair
                    {
                        station = m.station
                    };

                    map.Add(m.station, pair);
                }

                if (m.isLeft)
                    pair.left = m;
                else
                    pair.right = m;
            }

            List<RailPair> pairs = new List<RailPair>();

            foreach (RailPair pair in map.Values)
            {
                if (pair.left != null && pair.right != null)
                {
                    pair.center =
                        (pair.left.originalCenter +
                         pair.right.originalCenter) * 0.5f;
                }
                else if (pair.left != null)
                {
                    pair.center = pair.left.originalCenter;
                }
                else if (pair.right != null)
                {
                    pair.center = pair.right.originalCenter;
                }
                else
                {
                    continue;
                }

                pairs.Add(pair);
            }

            return pairs;
        }

        private static Vector3 GetPairTangent(
            List<RailPair> pairs,
            int index)
        {
            if (pairs.Count == 1)
                return Vector3.forward;

            if (index <= 0)
                return pairs[1].center - pairs[0].center;

            if (index >= pairs.Count - 1)
                return pairs[pairs.Count - 1].center -
                       pairs[pairs.Count - 2].center;

            return pairs[index + 1].center -
                   pairs[index - 1].center;
        }

        // ============================================================
        // UTILS
        // ============================================================

        private bool ValidateTargets()
        {
            if (roadRoot == null || guardrailRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Edge Snap PRO",
                    "Assign both Road Root and Guardrail Root.",
                    "OK");
                return false;
            }

            if (EditorUtility.IsPersistent(guardrailRoot.gameObject))
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Edge Snap PRO",
                    "Guardrail Root must be the object in Hierarchy, not the FBX file in Project.",
                    "OK");
                return false;
            }

            MeshFilter[] mfs =
                roadRoot.GetComponentsInChildren<MeshFilter>(true);

            if (mfs == null || mfs.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Edge Snap PRO",
                    "Road Root does not contain a MeshFilter.",
                    "OK");
                return false;
            }

            return true;
        }

        private static bool TryGetBounds(
            Transform root,
            out Bounds bounds)
        {
            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);

            bounds = new Bounds(root.position, Vector3.zero);
            bool found = false;

            foreach (Renderer r in renderers)
            {
                if (r == null)
                    continue;

                if (!found)
                {
                    bounds = r.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return found;
        }

        private void FrameTargets()
        {
            if (roadRoot != null)
            {
                Selection.activeGameObject = roadRoot;
                SceneView.FrameLastActiveSceneView();
            }
            else if (guardrailRoot != null)
            {
                Selection.activeTransform = guardrailRoot;
                SceneView.FrameLastActiveSceneView();
            }
        }

        private void DrawSceneDebug(SceneView sceneView)
        {
            if (!drawDetectedRoadEdges || cachedBoundary.Count == 0)
                return;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = new Color(1f, 0.75f, 0.1f, 0.95f);

            for (int i = 0; i < cachedBoundary.Count; i++)
            {
                BoundarySegment seg = cachedBoundary[i];
                Handles.DrawAAPolyLine(3f, seg.a, seg.b);
            }

            Handles.color = Color.white;
        }
    }
}
#endif
