// RoadGuardrailAutoFitter.cs
// Unity Editor tool for automatically fitting modular guardrails to a curved road mesh.
// Place this file in: Assets/Editor/RoadGuardrailAutoFitter.cs

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadGuardrailAutoFitter : EditorWindow
{
    [Header("Targets")]
    [SerializeField] private GameObject roadRoot;
    [SerializeField] private Transform guardrailRoot;

    [Header("Automatic Root Alignment")]
    [SerializeField] private bool centerRootXZ = true;
    [SerializeField] private bool autoYaw = false;
    [SerializeField] private bool autoHorizontalScale = false;

    [Header("Per Module Edge Fitting")]
    [SerializeField] private bool fitModulesToRoadEdge = true;
    [SerializeField] private bool snapModuleHeight = true;
    [SerializeField] private bool processDirectChildrenOnly = true;
    [SerializeField] private bool includeInactive = true;

    [Tooltip("Distance between the road mesh edge and the centre of each guardrail module.")]
    [SerializeField] private float roadEdgeOffset = 0.30f;

    [Tooltip("How far the bottom of the posts is allowed to sink below the road surface.")]
    [SerializeField] private float postEmbedDepth = 0.10f;

    [Tooltip("Safety limit. Modules farther than this from the road are ignored.")]
    [SerializeField] private float maxEdgeSearchDistance = 8.0f;

    [Header("Optional Fine Tuning")]
    [SerializeField] private Vector3 finalWorldOffset = Vector3.zero;

    [Tooltip("Use with care. Keeps the module's forward direction but tilts it to the nearby road normal.")]
    [SerializeField] private bool conformToRoadNormal = false;

    [SerializeField, Range(0f, 1f)] private float normalConformStrength = 0.65f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private Vector2 scroll;

    [MenuItem("Tools/Forest Road/Guardrail Auto Fitter")]
    public static void OpenWindow()
    {
        // Force a visible floating utility window instead of relying on docking.
        RoadGuardrailAutoFitter w = CreateInstance<RoadGuardrailAutoFitter>();
        w.titleContent = new GUIContent("Guardrail Auto Fitter");
        w.minSize = new Vector2(430, 620);
        w.position = new Rect(120, 90, 470, 700);
        w.ShowUtility();
        w.Focus();
        w.Repaint();

        Debug.Log("[Guardrail Auto Fitter] Window opened.");
    }

    [MenuItem("Tools/Forest Road/Guardrail Auto Fitter - Force Open")]
    public static void ForceOpenWindow()
    {
        OpenWindow();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ROAD GUARDRAIL AUTO FITTER", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Fits an imported modular guardrail FBX to a curved road. " +
            "It can first align the whole root, then fit every module to the nearest real road edge and height.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("1) Targets", EditorStyles.boldLabel);
        roadRoot = (GameObject)EditorGUILayout.ObjectField("Road Root", roadRoot, typeof(GameObject), true);
        guardrailRoot = (Transform)EditorGUILayout.ObjectField("Guardrail Root", guardrailRoot, typeof(Transform), true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2) Root Auto Alignment", EditorStyles.boldLabel);
        centerRootXZ = EditorGUILayout.ToggleLeft("Match road centre in X/Z", centerRootXZ);
        autoYaw = EditorGUILayout.ToggleLeft("Auto-match horizontal road direction (Yaw)", autoYaw);
        autoHorizontalScale = EditorGUILayout.ToggleLeft("Auto-match horizontal length/scale", autoHorizontalScale);

        if (autoHorizontalScale)
        {
            EditorGUILayout.HelpBox(
                "Scale matching is useful when Blender/Unity import scale is wrong. " +
                "Leave it OFF when both road and guardrails already have the same scale.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3) Fit Every Guardrail Module", EditorStyles.boldLabel);
        fitModulesToRoadEdge = EditorGUILayout.ToggleLeft("Snap modules sideways to nearest road edge", fitModulesToRoadEdge);
        snapModuleHeight = EditorGUILayout.ToggleLeft("Snap modules vertically to road surface", snapModuleHeight);
        processDirectChildrenOnly = EditorGUILayout.ToggleLeft("Each direct child = one module", processDirectChildrenOnly);
        includeInactive = EditorGUILayout.ToggleLeft("Include inactive modules", includeInactive);

        using (new EditorGUI.DisabledScope(!fitModulesToRoadEdge))
        {
            roadEdgeOffset = EditorGUILayout.FloatField("Road Edge Offset (m)", roadEdgeOffset);
            maxEdgeSearchDistance = EditorGUILayout.FloatField("Max Search Distance (m)", maxEdgeSearchDistance);
        }

        using (new EditorGUI.DisabledScope(!snapModuleHeight))
        {
            postEmbedDepth = EditorGUILayout.FloatField("Post Embed Depth (m)", postEmbedDepth);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("4) Fine Tuning", EditorStyles.boldLabel);
        finalWorldOffset = EditorGUILayout.Vector3Field("Final World Offset", finalWorldOffset);
        conformToRoadNormal = EditorGUILayout.ToggleLeft("Tilt modules to road slope", conformToRoadNormal);

        using (new EditorGUI.DisabledScope(!conformToRoadNormal))
        {
            normalConformStrength = EditorGUILayout.Slider("Slope Follow", normalConformStrength, 0f, 1f);
        }

        EditorGUILayout.Space(12);

        using (new EditorGUI.DisabledScope(roadRoot == null || guardrailRoot == null))
        {
            GUI.backgroundColor = new Color(0.55f, 1.0f, 0.55f);
            if (GUILayout.Button("FULL AUTO FIT TO ROAD", GUILayout.Height(44)))
            {
                FullAutoFit();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Only Align Guardrail Root", GUILayout.Height(28)))
            {
                AlignRoot();
            }

            if (GUILayout.Button("Only Fit Modules To Road Edge", GUILayout.Height(28)))
            {
                FitModules();
            }
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Use Current Selection", GUILayout.Height(26)))
        {
            TryUseSelection();
        }

        verboseLog = EditorGUILayout.ToggleLeft("Detailed Console Log", verboseLog);

        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "Recommended for your scene:\n" +
            "• Match road centre X/Z: ON\n" +
            "• Auto Yaw: OFF at first\n" +
            "• Auto Scale: OFF\n" +
            "• Road Edge Offset: 0.20–0.45 m\n" +
            "• Post Embed: 0.05–0.15 m\n\n" +
            "If the rail is still too close/far from asphalt, only change Road Edge Offset and press FULL AUTO FIT again.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void TryUseSelection()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            return;

        if (Selection.gameObjects.Length >= 1 && roadRoot == null)
            roadRoot = Selection.gameObjects[0];

        if (Selection.gameObjects.Length >= 2 && guardrailRoot == null)
            guardrailRoot = Selection.gameObjects[1].transform;

        Repaint();
    }

    private void FullAutoFit()
    {
        if (!ValidateTargets())
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Full Auto Fit Guardrails To Road");

        try
        {
            AlignRootInternal();
            FitModulesInternal();
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        SceneView.RepaintAll();
    }

    private void AlignRoot()
    {
        if (!ValidateTargets())
            return;

        Undo.SetCurrentGroupName("Align Guardrail Root");
        AlignRootInternal();
        SceneView.RepaintAll();
    }

    private void FitModules()
    {
        if (!ValidateTargets())
            return;

        Undo.SetCurrentGroupName("Fit Guardrail Modules");
        FitModulesInternal();
        SceneView.RepaintAll();
    }

    private bool ValidateTargets()
    {
        if (roadRoot == null || guardrailRoot == null)
        {
            EditorUtility.DisplayDialog("Guardrail Auto Fitter", "Assign both Road Root and Guardrail Root.", "OK");
            return false;
        }

        if (EditorUtility.IsPersistent(guardrailRoot.gameObject))
        {
            EditorUtility.DisplayDialog(
                "Guardrail Auto Fitter",
                "Guardrail Root must be a scene instance, not the FBX asset in the Project window.\n\nDrag the FBX into the scene first.",
                "OK");
            return false;
        }

        if (guardrailRoot.IsChildOf(roadRoot.transform))
        {
            EditorUtility.DisplayDialog(
                "Guardrail Auto Fitter",
                "Guardrail Root should not be a child of Road Root while fitting.",
                "OK");
            return false;
        }

        if (!TryGetCombinedBounds(roadRoot.transform, out _))
        {
            EditorUtility.DisplayDialog("Guardrail Auto Fitter", "No Renderer was found under Road Root.", "OK");
            return false;
        }

        if (!TryGetCombinedBounds(guardrailRoot, out _))
        {
            EditorUtility.DisplayDialog("Guardrail Auto Fitter", "No Renderer was found under Guardrail Root.", "OK");
            return false;
        }

        return true;
    }

    // ---------------------------------------------------------
    // ROOT ALIGNMENT
    // ---------------------------------------------------------

    private void AlignRootInternal()
    {
        if (!TryGetCombinedBounds(roadRoot.transform, out Bounds roadBounds) ||
            !TryGetCombinedBounds(guardrailRoot, out Bounds railBounds))
            return;

        Undo.RecordObject(guardrailRoot, "Align Guardrail Root");

        if (autoYaw)
        {
            float roadYaw = EstimatePrincipalYaw(roadRoot.transform);
            float railYaw = EstimatePrincipalYaw(guardrailRoot);

            float deltaYaw = Mathf.DeltaAngle(railYaw, roadYaw);

            // PCA axis is directionless: +axis and -axis are identical.
            if (deltaYaw > 90f) deltaYaw -= 180f;
            if (deltaYaw < -90f) deltaYaw += 180f;

            guardrailRoot.rotation =
                Quaternion.AngleAxis(deltaYaw, Vector3.up) * guardrailRoot.rotation;

            TryGetCombinedBounds(guardrailRoot, out railBounds);
        }

        if (autoHorizontalScale)
        {
            float roadLength = EstimateHorizontalLength(roadRoot.transform);
            float railLength = EstimateHorizontalLength(guardrailRoot);

            if (roadLength > 0.001f && railLength > 0.001f)
            {
                float ratio = roadLength / railLength;
                ratio = Mathf.Clamp(ratio, 0.1f, 10f);
                guardrailRoot.localScale *= ratio;
                TryGetCombinedBounds(guardrailRoot, out railBounds);
            }
        }

        if (centerRootXZ)
        {
            Vector3 delta = roadBounds.center - railBounds.center;
            delta.y = 0f;
            guardrailRoot.position += delta;
        }

        EditorUtility.SetDirty(guardrailRoot);

        if (verboseLog)
            Debug.Log("[Guardrail Auto Fitter] Root alignment complete.");
    }

    // ---------------------------------------------------------
    // MODULE EDGE FIT
    // ---------------------------------------------------------

    private void FitModulesInternal()
    {
        List<Collider> temporaryColliders = new List<Collider>();

        try
        {
            List<Collider> roadColliders = PrepareRoadColliders(temporaryColliders);

            if (roadColliders.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Auto Fitter",
                    "Could not create/find a collider for the road mesh.",
                    "OK");
                return;
            }

            List<Transform> modules = GetModules();

            if (modules.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Guardrail Auto Fitter",
                    "No guardrail modules were found under the selected Guardrail Root.",
                    "OK");
                return;
            }

            Undo.RecordObjects(modules.ToArray(), "Fit Guardrail Modules To Road");

            int fitted = 0;
            int skipped = 0;

            for (int i = 0; i < modules.Count; i++)
            {
                Transform module = modules[i];

                if (!TryGetCombinedBounds(module, out Bounds moduleBounds))
                {
                    skipped++;
                    continue;
                }

                Vector3 moduleCenter = moduleBounds.center;

                if (!FindClosestRoadPoint(
                        moduleCenter,
                        roadColliders,
                        out Vector3 closestPoint,
                        out Vector3 closestNormal,
                        out float distance))
                {
                    skipped++;
                    continue;
                }

                if (distance > maxEdgeSearchDistance)
                {
                    skipped++;
                    continue;
                }

                Vector3 movement = Vector3.zero;

                if (fitModulesToRoadEdge)
                {
                    Vector3 outward = moduleCenter - closestPoint;
                    outward.y = 0f;

                    // If ClosestPoint is almost the same point, fall back to the
                    // direction away from the road's overall centre.
                    if (outward.sqrMagnitude < 0.0001f)
                    {
                        if (TryGetCombinedBounds(roadRoot.transform, out Bounds rb))
                        {
                            outward = moduleCenter - rb.center;
                            outward.y = 0f;
                        }
                    }

                    if (outward.sqrMagnitude > 0.0001f)
                    {
                        outward.Normalize();

                        Vector3 desiredCenterXZ =
                            new Vector3(closestPoint.x, moduleCenter.y, closestPoint.z) +
                            outward * Mathf.Max(0f, roadEdgeOffset);

                        movement.x += desiredCenterXZ.x - moduleCenter.x;
                        movement.z += desiredCenterXZ.z - moduleCenter.z;
                    }
                }

                if (snapModuleHeight)
                {
                    float desiredBottomY = closestPoint.y - Mathf.Max(0f, postEmbedDepth);
                    movement.y += desiredBottomY - moduleBounds.min.y;
                }

                movement += finalWorldOffset;

                module.position += movement;

                if (conformToRoadNormal && closestNormal.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRotation = RotationKeepingForwardOnSurface(module, closestNormal.normalized);
                    module.rotation = Quaternion.Slerp(module.rotation, targetRotation, normalConformStrength);
                }

                EditorUtility.SetDirty(module);
                fitted++;
            }

            if (verboseLog)
            {
                Debug.Log(
                    $"[Guardrail Auto Fitter] Finished. Fitted {fitted} modules. Skipped {skipped}. " +
                    $"Road colliders used: {roadColliders.Count}.");
            }

            EditorUtility.DisplayDialog(
                "Guardrail Auto Fitter",
                $"Finished.\n\nFitted modules: {fitted}\nSkipped: {skipped}\n\n" +
                "If the rail is too close/far from the asphalt, adjust Road Edge Offset and run it again.",
                "OK");
        }
        finally
        {
            // Temporary colliders were created only for fitting.
            for (int i = temporaryColliders.Count - 1; i >= 0; i--)
            {
                if (temporaryColliders[i] != null)
                    DestroyImmediate(temporaryColliders[i]);
            }
        }
    }

    private List<Transform> GetModules()
    {
        List<Transform> result = new List<Transform>();

        if (processDirectChildrenOnly)
        {
            for (int i = 0; i < guardrailRoot.childCount; i++)
            {
                Transform child = guardrailRoot.GetChild(i);

                if (!includeInactive && !child.gameObject.activeInHierarchy)
                    continue;

                if (child.GetComponentInChildren<Renderer>(includeInactive) != null)
                    result.Add(child);
            }
        }
        else
        {
            Renderer[] renderers = guardrailRoot.GetComponentsInChildren<Renderer>(includeInactive);
            HashSet<Transform> unique = new HashSet<Transform>();

            foreach (Renderer r in renderers)
            {
                if (r == null || r.transform == guardrailRoot)
                    continue;

                Transform t = r.transform;

                // Prefer the top-most object directly under root as one module.
                while (t.parent != null && t.parent != guardrailRoot)
                    t = t.parent;

                if (t != guardrailRoot)
                    unique.Add(t);
            }

            result.AddRange(unique);
        }

        return result;
    }

    // ---------------------------------------------------------
    // ROAD COLLIDERS
    // ---------------------------------------------------------

    private List<Collider> PrepareRoadColliders(List<Collider> temporary)
    {
        List<Collider> result = new List<Collider>();

        Collider[] existing = roadRoot.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in existing)
        {
            if (c != null && c.enabled)
                result.Add(c);
        }

        MeshFilter[] meshFilters = roadRoot.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            Collider existingOnObject = mf.GetComponent<Collider>();
            if (existingOnObject != null)
                continue;

            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            mc.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            result.Add(mc);
            temporary.Add(mc);
        }

        Terrain[] terrains = roadRoot.GetComponentsInChildren<Terrain>(true);
        foreach (Terrain t in terrains)
        {
            TerrainCollider tc = t.GetComponent<TerrainCollider>();
            if (tc != null && tc.enabled && !result.Contains(tc))
                result.Add(tc);
        }

        Physics.SyncTransforms();
        return result;
    }

    private bool FindClosestRoadPoint(
        Vector3 worldPoint,
        List<Collider> colliders,
        out Vector3 closestPoint,
        out Vector3 closestNormal,
        out float distance)
    {
        closestPoint = Vector3.zero;
        closestNormal = Vector3.up;
        distance = float.MaxValue;

        Collider best = null;

        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled)
                continue;

            Vector3 p = c.ClosestPoint(worldPoint);
            float d = Vector3.Distance(worldPoint, p);

            if (d < distance)
            {
                distance = d;
                closestPoint = p;
                best = c;
            }
        }

        if (best == null)
            return false;

        // Try to obtain a real surface normal close to the nearest point.
        // Cast from above first.
        float rayHeight = 5f + maxEdgeSearchDistance;
        Vector3 origin = closestPoint + Vector3.up * rayHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            rayHeight * 2f,
            ~0,
            QueryTriggerInteraction.Ignore);

        float bestRayDist = float.MaxValue;
        bool foundNormal = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (!hit.collider.transform.IsChildOf(roadRoot.transform) &&
                hit.collider.gameObject != roadRoot)
                continue;

            float horizontalDist = Vector2.Distance(
                new Vector2(hit.point.x, hit.point.z),
                new Vector2(closestPoint.x, closestPoint.z));

            if (horizontalDist < bestRayDist)
            {
                bestRayDist = horizontalDist;
                closestNormal = hit.normal;
                foundNormal = true;

                // Prefer the vertical ray's Y when it lands very close to the
                // nearest edge position.
                if (horizontalDist < 0.50f)
                    closestPoint.y = hit.point.y;
            }
        }

        if (!foundNormal)
            closestNormal = Vector3.up;

        return true;
    }

    // ---------------------------------------------------------
    // UTILITY
    // ---------------------------------------------------------

    private static bool TryGetCombinedBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        bool hasBounds = false;
        bounds = new Bounds(root.position, Vector3.zero);

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return hasBounds;
    }

    private static Quaternion RotationKeepingForwardOnSurface(Transform module, Vector3 normal)
    {
        Vector3 forward = module.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, normal);

        if (projectedForward.sqrMagnitude < 0.0001f)
        {
            Vector3 right = Vector3.ProjectOnPlane(module.right, normal);

            if (right.sqrMagnitude < 0.0001f)
                return module.rotation;

            projectedForward = Vector3.Cross(right.normalized, normal).normalized;
        }

        return Quaternion.LookRotation(projectedForward.normalized, normal);
    }

    private static float EstimatePrincipalYaw(Transform root)
    {
        List<Vector3> points = CollectWorldSamplePoints(root, 12000);

        if (points.Count < 2)
            return root.eulerAngles.y;

        double meanX = 0.0;
        double meanZ = 0.0;

        foreach (Vector3 p in points)
        {
            meanX += p.x;
            meanZ += p.z;
        }

        meanX /= points.Count;
        meanZ /= points.Count;

        double xx = 0.0;
        double zz = 0.0;
        double xz = 0.0;

        foreach (Vector3 p in points)
        {
            double dx = p.x - meanX;
            double dz = p.z - meanZ;

            xx += dx * dx;
            zz += dz * dz;
            xz += dx * dz;
        }

        double theta = 0.5 * Math.Atan2(2.0 * xz, xx - zz);

        Vector3 principal = new Vector3(
            (float)Math.Cos(theta),
            0f,
            (float)Math.Sin(theta));

        return Mathf.Atan2(principal.x, principal.z) * Mathf.Rad2Deg;
    }

    private static float EstimateHorizontalLength(Transform root)
    {
        List<Vector3> points = CollectWorldSamplePoints(root, 12000);

        if (points.Count < 2)
            return 0f;

        float yaw = EstimatePrincipalYaw(root) * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)).normalized;

        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (Vector3 p in points)
        {
            float d = Vector2.Dot(new Vector2(p.x, p.z), axis);
            min = Mathf.Min(min, d);
            max = Mathf.Max(max, d);
        }

        return Mathf.Max(0f, max - min);
    }

    private static List<Vector3> CollectWorldSamplePoints(Transform root, int maxPoints)
    {
        List<Vector3> points = new List<Vector3>();
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);

        int totalVertexCount = 0;

        foreach (MeshFilter mf in filters)
        {
            if (mf != null && mf.sharedMesh != null)
                totalVertexCount += mf.sharedMesh.vertexCount;
        }

        int stride = Mathf.Max(1, totalVertexCount / Mathf.Max(1, maxPoints));

        foreach (MeshFilter mf in filters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            Vector3[] verts = mf.sharedMesh.vertices;

            for (int i = 0; i < verts.Length; i += stride)
            {
                points.Add(mf.transform.TransformPoint(verts[i]));

                if (points.Count >= maxPoints)
                    return points;
            }
        }

        // Fallback for objects without MeshFilter.
        if (points.Count == 0)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in renderers)
            {
                points.Add(r.bounds.center);

                if (points.Count >= maxPoints)
                    break;
            }
        }

        return points;
    }
}
#endif
