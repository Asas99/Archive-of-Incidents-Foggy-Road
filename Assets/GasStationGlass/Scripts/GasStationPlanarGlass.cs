using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>One reflection per coplanar window group, owned by the player camera.</summary>
[ExecuteAlways, DefaultExecutionOrder(10000)]
public sealed partial class GasStationPlanarGlass : MonoBehaviour
{
    [Serializable]
    public sealed class WindowGroup
    {
        public string name;
        [Tooltip("Brief live reflection on actor arrival, then baked until a new arrival.")] public bool arrivalBurstOnly;
        [Min(.1f)] public float arrivalBurstSeconds=2;
        [NonSerialized] internal GlassArrivalBurst arrivalBurst=new GlassArrivalBurst();
        public Transform plane;
        public Vector3 localPoint;
        public Vector3 localNormal = Vector3.forward;
        public Renderer[] windows = Array.Empty<Renderer>();
        public ReflectionProbe bakedPositive, bakedNegative;
        [NonSerialized] internal Camera camera;
        [NonSerialized] internal RenderTexture texture;
        [NonSerialized] internal Matrix4x4 vp;
        [NonSerialized] internal double lastUpdate = -100;
        [NonSerialized] internal bool ready;
        [NonSerialized] internal int lastCaptureFrame=-1;
        [NonSerialized] internal int side;
        [NonSerialized] internal Vector3 lastPoint, lastNormal;
        [NonSerialized] internal float liveWeight;
        [NonSerialized] internal double lastActivity=-100, nextActivityCheck, lastVisible=-100;
        [NonSerialized] internal bool activity;
        [NonSerialized] internal string reason="Baked";
        [NonSerialized] internal int sourceId;
        [NonSerialized] internal readonly HashSet<ReflectionActor> nearbyActors = new HashSet<ReflectionActor>();
    }

    public Camera playerCamera;
    public WindowGroup[] groups = Array.Empty<WindowGroup>();
    [Tooltip("Index of the dedicated, feature-free forward renderer in the active URP asset.")]
    public int reflectionRendererIndex = -1;
    public UniversalRenderPipelineAsset configuredPipeline;
    public LayerMask reflectionLayers = ~0;
    [Range(0.125f, 1)] public float resolutionScale = 0.5f;
    [Range(1, 2)] public int maxActiveGroups = 2;
    public float fullRateDistance = 15, reducedRateDistance = 25, fadeEndDistance = 30;
    [Range(1, 60)] public float distantUpdatesPerSecond = 30;
    [Range(0, 0.25f)] public float minimumScreenArea = 0.01f;
    [Range(0.001f, 0.1f)] public float clipOffset = 0.02f;
    public bool previewInSceneView = false;
    public bool reflectShadows = true;
    public int ActiveGroups { get; private set; }
    public Camera LastSourceCamera { get; private set; }

    static readonly int TextureId = Shader.PropertyToID("_PlanarTexture");
    static readonly int MatrixId = Shader.PropertyToID("_PlanarVP");
    static readonly int WeightId = Shader.PropertyToID("_PlanarWeight");
    static readonly int MipId = Shader.PropertyToID("_PlanarMaxMip");
    static readonly int RenderingId = Shader.PropertyToID("_GasGlassRenderingReflection");
    MaterialPropertyBlock block;
    readonly Plane[] frustum = new Plane[6];
    readonly List<Candidate> candidates = new List<Candidate>();
    readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
    struct Candidate { public WindowGroup group; public float distance, area; }
    bool warned;
    bool transmissionReady;
    UniversalAdditionalCameraData transmissionCamera;
    CameraOverrideOption previousColourOption;
    Camera[] cameraBuffer = Array.Empty<Camera>();

    // Camera.main may be null: this project uses an untagged first-person camera.
    public Camera ResolvePlayerCamera()
    {
        if (IsScreenCamera(playerCamera)) return playerCamera;
        var tagged=Camera.main;
        if (IsScreenCamera(tagged)) return tagged;
        int count=Camera.allCamerasCount;
        if (cameraBuffer.Length<count) cameraBuffer=new Camera[count];
        int found=Camera.GetAllCameras(cameraBuffer);
        Camera best=null;
        for(int i=0;i<found;i++)
        {
            var candidate=cameraBuffer[i];
            if(IsScreenCamera(candidate) && (!best || candidate.depth>best.depth)) best=candidate;
        }
        return best;
    }
    static bool IsScreenCamera(Camera camera)
    {
        if(!camera || !camera.enabled || !camera.gameObject.activeInHierarchy || camera.cameraType!=CameraType.Game || camera.targetTexture || camera.targetDisplay!=0) return false;
        if(camera.GetComponent<DistantMountainBackdrop>()) return false;
        return camera.GetComponent<PlayerInteraction>() || !camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) || data.renderType==CameraRenderType.Base;
    }

    void OnEnable()
    {
        block = new MaterialPropertyBlock(); warned = false;
        RefreshBakedBindings();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorPreview;
        #endif
    }
    #if UNITY_EDITOR
    double nextPreview;
    void EditorPreview()
    {
        if (Application.isPlaying || !previewInSceneView || !UnityEditor.SceneView.lastActiveSceneView || UnityEditor.EditorApplication.isCompiling) return;
        if (UnityEditor.EditorApplication.timeSinceStartup < nextPreview) return;
        nextPreview=UnityEditor.EditorApplication.timeSinceStartup+1.0/30;
        LateUpdate();
        if (ActiveGroups>0) UnityEditor.SceneView.lastActiveSceneView.Repaint();
    }
    #endif

    void LateUpdate()
    {
        Camera source = ResolvePlayerCamera();
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            source = UnityEditor.SceneView.lastActiveSceneView ? UnityEditor.SceneView.lastActiveSceneView.camera : source;
        }
        #endif
        TickReflections(source,Application.isPlaying || previewInSceneView);
    }

    /// <summary>Refresh reflections for a specific source camera (also used for in-scene verification).</summary>
    public void RenderForCamera(Camera source)
    {
        TickReflections(source,true);
    }

    static float ScreenArea(Camera camera, Bounds bounds)
    {
        float minX=1, minY=1, maxX=0, maxY=0;
        for (int i=0;i<8;i++)
        {
            var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            var v=camera.WorldToViewportPoint(p);
            // A bounds crossing the near plane must not accidentally disappear.
            if (v.z <= camera.nearClipPlane) return 1;
            minX=Mathf.Min(minX,v.x); minY=Mathf.Min(minY,v.y); maxX=Mathf.Max(maxX,v.x); maxY=Mathf.Max(maxY,v.y);
        }
        return Mathf.Max(0,Mathf.Clamp01(maxX)-Mathf.Clamp01(minX))*Mathf.Max(0,Mathf.Clamp01(maxY)-Mathf.Clamp01(minY));
    }

    void Render(WindowGroup group, Camera source, Vector3 point, Vector3 normal)
    {
        if(UsesAtlas){QueueAtlas(group,source,point,normal);return;}
        if (!group.camera)
        {
            var go = new GameObject("Glass Reflection - " + group.name) { hideFlags = HideFlags.HideAndDontSave };
            group.camera = go.AddComponent<Camera>();
            group.camera.enabled = false;
            go.AddComponent<UniversalAdditionalCameraData>();
        }
        var camera = group.camera;
        int width=Mathf.Max(64,Mathf.RoundToInt(source.pixelWidth*resolutionScale));
        int height=Mathf.Max(64,Mathf.RoundToInt(source.pixelHeight*resolutionScale));
        if(Lean(this)){float cap=Mathf.Min(1,Mathf.Min(960f/width,540f/height));width=Mathf.Max(64,Mathf.RoundToInt(width*cap));height=Mathf.Max(64,Mathf.RoundToInt(height*cap));}
        if (!group.texture || group.texture.width != width || group.texture.height != height)
        {
            if (group.texture) { group.texture.Release(); DestroyResource(group.texture); }
            var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : (Lean(this)?RenderTextureFormat.ARGB32:RenderTextureFormat.DefaultHDR);
            group.texture=new RenderTexture(width,height,24,format) { name="Glass HDR - " + group.name, hideFlags=HideFlags.HideAndDontSave, useMipMap=true, autoGenerateMips=false, filterMode=FilterMode.Trilinear, wrapMode=TextureWrapMode.Clamp, antiAliasing=1 };
            group.texture.Create(); group.ready=false;
        }
        camera.CopyFrom(source);
        camera.enabled=false; camera.targetTexture=group.texture;
        camera.cameraType=CameraType.Reflection;
        // A body hidden from the first-person camera can still exist in reflections.
        camera.cullingMask=reflectionLayers.value;
        camera.allowMSAA=false; camera.allowHDR=true;
        if(Lean(this)){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;}
        var data=camera.GetUniversalAdditionalCameraData();
        data.renderType=CameraRenderType.Base; data.renderPostProcessing=false;
        data.requiresColorTexture=false; data.requiresDepthTexture=false;
        data.renderShadows=!Lean(this)&&reflectShadows; data.SetRenderer(Lean(this)?dynamicRendererIndex:reflectionRendererIndex);
        data.antialiasing=AntialiasingMode.None;
        float d=-Vector3.Dot(normal,point);
        Matrix4x4 mirror=ReflectionMatrix(new Vector4(normal.x,normal.y,normal.z,d));
        Vector3 reflectedPosition=mirror.MultiplyPoint(source.transform.position);
        camera.transform.SetPositionAndRotation(reflectedPosition,Quaternion.LookRotation(mirror.MultiplyVector(source.transform.forward),mirror.MultiplyVector(source.transform.up)));
        camera.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
        Vector3 clipPosition=camera.worldToCameraMatrix.MultiplyPoint(point+normal*clipOffset);
        Vector3 clipNormal=camera.worldToCameraMatrix.MultiplyVector(normal).normalized;
        camera.farClipPlane=Mathf.Max(source.nearClipPlane+.01f,Lean(this)?Mathf.Min(source.farClipPlane,Vector3.Distance(source.transform.position,point)+Mathf.Max(1,reflectionSceneDepth)):source.farClipPlane);
        camera.projectionMatrix=CaptureProjection(source,point,normal,camera.worldToCameraMatrix);
        camera.cullingMatrix=camera.projectionMatrix*camera.worldToCameraMatrix;
        group.vp=GL.GetGPUProjectionMatrix(camera.projectionMatrix,true)*camera.worldToCameraMatrix;
        bool oldInvert=GL.invertCulling;
        float oldRendering=Shader.GetGlobalFloat(RenderingId);
        Profiler.BeginSample("Gas Station Glass / Planar Reflection");
        try
        {
            GL.invertCulling=!oldInvert;
            Shader.SetGlobalFloat(RenderingId,1);
            if(Lean(this)){GatherDynamicModels(group,source);DynamicCapture=this;CaptureCamera=camera;}
            request.destination=group.texture;
            CameraRenderRequests++;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
            TotalCaptures++; CapturesThisTick++;
            group.texture.GenerateMips();
            group.ready=true;
            if(Lean(this))RememberDynamic(group,source);
        }
        catch (Exception exception)
        {
            group.ready=false;
            if (!warned) { Debug.LogException(exception,this); warned=true; }
        }
        finally
        {
            if(DynamicCapture==this){DynamicCapture=null;CaptureCamera=null;}
            GL.invertCulling=oldInvert;
            Shader.SetGlobalFloat(RenderingId,oldRendering);
            Profiler.EndSample();
        }
    }

    public static Matrix4x4 ReflectionMatrix(Vector4 plane)
    {
        var n=new Vector3(plane.x,plane.y,plane.z);
        var matrix=Matrix4x4.identity;
        for(int row=0;row<3;row++)
        {
            for(int col=0;col<3;col++) matrix[row,col]-=2*n[row]*n[col];
            matrix[row,3]=-2*plane.w*n[row];
        }
        return matrix;
    }

    void Apply(WindowGroup group,float weight,WindowGroup captureSource=null)
    {
        if (block == null) block = new MaterialPropertyBlock();
        var captured=captureSource??group;
        foreach (var window in group.windows)
        {
            if (!window) continue;
            window.GetPropertyBlock(block);
            block.SetFloat(WeightId,weight);
            block.SetFloat("_DynamicOverlay",Lean(this)?1:0);
            block.SetFloat("_DynamicStrength",nearProfileEnabled&&UsesAtlas?dynamicReflectionStrength:1);
            block.SetFloat("_DynamicDebug",(float)dynamicDebug);
            block.SetFloat("_TransmissionReady",transmissionReady ? 1 : 0);
            block.SetFloat("_TransmissionSamples",transmissionQuality==TransmissionQuality.High?4:transmissionQuality==TransmissionQuality.Balanced?2:0);
            SetBakedProperties(block,group);
            block.SetTexture(TextureId,UsesAtlas&&atlas ? atlas : captured.texture ? captured.texture : Texture2D.blackTexture);
            block.SetVector("_PlanarAtlasRect",UsesAtlas?AtlasRect(captured):new Vector4(1,1,0,0));
            block.SetMatrix(MatrixId,captured.vp);
            block.SetFloat(MipId,UsesAtlas?dynamicMipLimit:captured.texture ? Mathf.Log(Mathf.Max(captured.texture.width,captured.texture.height),2) : 0);
            window.SetPropertyBlock(block);
        }
    }
    public void RegisterWindow(Renderer window,Vector3 localPoint,Vector3 localNormal)
    {
        if(!window || !GlassSurfaceVariation.UsesGlass(window)) return;
        foreach(var existing in groups) foreach(var member in existing.windows) if(member==window) return;
        var point=window.transform.TransformPoint(localPoint);
        var normal=window.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(localNormal).normalized;
        bool moving=window.GetComponentInParent<Animator>() || window.GetComponentInParent<Rigidbody>();
        for(var t=window.transform;t;t=t.parent)
        {
            string name=t.name.ToLowerInvariant(); moving|=name.Contains("door") || name.Contains("kapı") || name.Contains("kapi");
        }
        if(!moving)
        {
            foreach(var group in groups)
            {
                if(!group.plane || group.name.StartsWith("Moving")) continue;
                var binding=window.GetComponent<GlassReflectionBinding>();
                var existingBinding=group.plane.GetComponent<GlassReflectionBinding>();
                if(!binding || !existingBinding || !binding.Verified || !existingBinding.Verified) continue;
                var n=group.plane.localToWorldMatrix.inverse.transpose.MultiplyVector(group.localNormal).normalized;
                if(Mathf.Abs(Vector3.Dot(n,normal))<0.9999f || Mathf.Abs(Vector3.Dot(point-group.plane.TransformPoint(group.localPoint),normal))>0.01f) continue;
                bool sameDirection=Vector3.Dot(n,normal)>=0;
                if(binding.positive!=(sameDirection?existingBinding.positive:existingBinding.negative) || binding.negative!=(sameDirection?existingBinding.negative:existingBinding.positive)) continue;
                var members=new List<Renderer>(group.windows); members.Add(window); group.windows=members.ToArray(); return;
            }
        }
        var all=new List<WindowGroup>(groups);
        all.Add(new WindowGroup { name=(moving ? "Moving " : "Facade ")+groups.Length,plane=window.transform,localPoint=localPoint,localNormal=localNormal,windows=new[]{window} });
        groups=all.ToArray();
        RefreshBakedBindings();
    }
    public void UnregisterWindow(Renderer window)
    {
        for(int i=groups.Length-1;i>=0;i--)
        {
            var group=groups[i]; var members=new List<Renderer>();
            foreach(var member in group.windows) if(member && member!=window) members.Add(member);
            if(members.Count==group.windows.Length) continue;
            group.windows=members.ToArray();
            if(members.Count==0)
            {
                if(group.camera) { group.camera.targetTexture=null; DestroyResource(group.camera.gameObject); }
                if(group.texture) { group.texture.Release(); DestroyResource(group.texture); }
                var all=new List<WindowGroup>(groups); all.RemoveAt(i); groups=all.ToArray();
            }
            else if(!group.plane || (window && group.plane==window.transform))
            {
                var variation=members[0].GetComponent<GlassSurfaceVariation>();
                group.plane=members[0].transform;
                if(variation) { group.localPoint=variation.LocalPoint; group.localNormal=variation.LocalNormal; }
                group.ready=false;
            }
        }
    }
    void ConfigureTransmission(Camera source,bool visible)
    {
        if(transmissionCamera && (!visible || transmissionCamera.GetComponent<Camera>()!=source))
        { transmissionCamera.requiresColorOption=previousColourOption; transmissionCamera=null; }
        transmissionReady=false;
        if(!visible || !source || transmissionQuality==TransmissionQuality.Off) return;
        if(source.cameraType==CameraType.SceneView)
        { transmissionReady=((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsCameraOpaqueTexture; return; }
        var data=source.GetUniversalAdditionalCameraData();
        if(!transmissionCamera) { transmissionCamera=data; previousColourOption=data.requiresColorOption; }
        data.requiresColorTexture=true; transmissionReady=true;
    }
    void ResetWeights() { ConfigureTransmission(null,false); ActiveGroups=0; foreach(var group in groups) Apply(group,0); }
    void OnDisable()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorPreview;
        #endif
        ResetWeights(); lastCaptureFrame=-1;ReleaseDynamicMaterials();ReleaseAtlas();
        foreach(var group in groups)
        {
            if(group.camera) { group.camera.targetTexture=null; DestroyResource(group.camera.gameObject); }
            if(group.texture) { group.texture.Release(); DestroyResource(group.texture); }
            group.camera=null; group.texture=null; group.ready=false;
            Apply(group,0);
        }
    }
    void OnValidate()
    {
        fullRateDistance=Mathf.Max(1,fullRateDistance);
        reducedRateDistance=Mathf.Max(fullRateDistance,reducedRateDistance);
        fadeEndDistance=Mathf.Max(reducedRateDistance+1,fadeEndDistance);
        foreach(var group in groups) group.ready=false;
    }
    static void DestroyResource(UnityEngine.Object resource)
    {
        if(Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
    }
}


