using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class GasStationPlanarGlass
{
    public enum DynamicBackend { LegacyCamera, SharedAtlas }
    public DynamicBackend dynamicBackend=DynamicBackend.LegacyCamera;
    public GlassAtlasFeature atlasFeature;
    public Shader atlasActorShader,atlasClearShader;
    public bool UsesAtlas=>Lean(this)&&dynamicBackend==DynamicBackend.SharedAtlas;
    public int UpdatedAtlasRegions{get;private set;}
    public long CameraRenderRequests{get;private set;}
    public int AtlasModels{get;private set;}
    public long AtlasTriangles{get;private set;}
    public static GasStationPlanarGlass AtlasOwner{get;private set;}
    public RTHandle AtlasHandle=>atlasHandle;
    public RTHandle AtlasDepthHandle=>atlasDepthHandle;
    public bool HasAtlasWork=>atlasBatches.Count>0&&atlas;
    RenderTexture atlas,atlasDepth;RTHandle atlasHandle,atlasDepthHandle;
    int atlasColumns,tileWidth,tileHeight,atlasCount;
    bool atlasNeedsClear;
    Material tileClear;
    readonly Dictionary<WindowGroup,int> atlasSlots=new Dictionary<WindowGroup,int>();
    readonly Dictionary<Material,Material> atlasMaterials=new Dictionary<Material,Material>();
    readonly List<AtlasBatch> atlasBatches=new List<AtlasBatch>();
    readonly Stack<AtlasBatch> freeBatches=new Stack<AtlasBatch>();
    readonly Plane[] atlasPlanes=new Plane[6];
    Light[] atlasLights=Array.Empty<Light>();double nextLightScan;
    struct AtlasDraw {public Mesh mesh;public Renderer skin;public Matrix4x4 matrix;public Material[] materials;public Bounds bounds;public int layer;public float visibility;}
    sealed class AtlasBatch
    {
        public WindowGroup group;public Rect viewport;public Matrix4x4 vp;public Vector3 eye;
        public readonly List<AtlasDraw> draws=new List<AtlasDraw>();
        public readonly List<ContactDraw> contacts=new List<ContactDraw>();
    }
    void BeginAtlas(Camera source)
    {
        UpdatedAtlasRegions=0;AtlasModels=0;AtlasTriangles=0;ContactShadowDraws=0;
        foreach(var batch in atlasBatches){batch.draws.Clear();batch.contacts.Clear();freeBatches.Push(batch);}atlasBatches.Clear();
        if(!UsesAtlas)return;
        if(Time.realtimeSinceStartupAsDouble>=nextLightScan){atlasLights=FindObjectsByType<Light>(FindObjectsSortMode.None);nextLightScan=Time.realtimeSinceStartupAsDouble+.5;}
        if(!source)return;
        float scale=Mathf.Clamp(resolutionScale,.125f,1);
        int w=Mathf.Max(64,Mathf.RoundToInt(source.pixelWidth*scale)),h=Mathf.Max(64,Mathf.RoundToInt(source.pixelHeight*scale));
        float cap=Mathf.Min(1,Mathf.Min(Mathf.Max(64,atlasResolutionLimit.x)/(float)w,Mathf.Max(64,atlasResolutionLimit.y)/(float)h));w=Mathf.Max(64,Mathf.RoundToInt(w*cap));h=Mathf.Max(64,Mathf.RoundToInt(h*cap));
        bool slotsChanged=atlasCount!=groups.Length;foreach(var g in groups)slotsChanged|=!atlasSlots.ContainsKey(g);
        if(atlas&&tileWidth==w&&tileHeight==h&&!slotsChanged)return;
        ReleaseAtlasTexture();atlasSlots.Clear();atlasCount=groups.Length;atlasColumns=Mathf.Max(1,Mathf.CeilToInt(Mathf.Sqrt(atlasCount)));
        tileWidth=w;tileHeight=h;int rows=Mathf.Max(1,Mathf.CeilToInt((float)atlasCount/atlasColumns));
        atlas=new RenderTexture(atlasColumns*(w+16),rows*(h+16),0,RenderTextureFormat.ARGBHalf){name="Glass shared dynamic atlas",hideFlags=HideFlags.HideAndDontSave,useMipMap=true,autoGenerateMips=false,antiAliasing=1,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
        atlas.Create();atlasHandle=RTHandles.Alloc(atlas);atlasDepth=new RenderTexture(atlas.width,atlas.height,24,RenderTextureFormat.Depth){name="Glass atlas depth",hideFlags=HideFlags.HideAndDontSave};atlasDepth.Create();atlasDepthHandle=RTHandles.Alloc(atlasDepth);atlasNeedsClear=true;
        for(int i=0;i<groups.Length;i++){atlasSlots.Add(groups[i],i);groups[i].ready=false;groups[i].liveWeight=0;}
        if(!tileClear&&atlasClearShader)tileClear=new Material(atlasClearShader){hideFlags=HideFlags.HideAndDontSave};
    }
    Rect AtlasViewport(WindowGroup group)
    {
        int slot=atlasSlots[group];return new Rect((slot%atlasColumns)*(tileWidth+16)+8,(slot/atlasColumns)*(tileHeight+16)+8,tileWidth,tileHeight);
    }
    Vector4 AtlasRect(WindowGroup group)
    {
        if(!atlas||!atlasSlots.ContainsKey(group))return new Vector4(1,1,0,0);
        var r=AtlasViewport(group);return new Vector4(r.width/atlas.width,r.height/atlas.height,r.x/atlas.width,r.y/atlas.height);
    }
    void QueueAtlas(WindowGroup group,Camera source,Vector3 point,Vector3 normal)
    {
        if(!atlas||!tileClear||!atlasActorShader)return;
        var view=source.worldToCameraMatrix*ReflectionMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point)));
        var vp=GL.GetGPUProjectionMatrix(CaptureProjection(source,point,normal,view),true)*view;
        var batch=freeBatches.Count>0?freeBatches.Pop():new AtlasBatch();batch.group=group;batch.viewport=AtlasViewport(group);batch.vp=vp;batch.eye=view.inverse.GetColumn(3);
        GatherDynamicModels(group,source);
        GeometryUtility.CalculateFrustumPlanes(ReflectionProjection(source,point)*view,atlasPlanes);
        foreach(var actor in captureActors)
        {
            if(actor.TryGetBounds(reflectionLayers.value,nearClock,out var actorBounds,out _,false)&&TryContact(actor,actorBounds,out var contactMatrix))batch.contacts.Add(new ContactDraw{matrix=contactMatrix,opacity=contactShadowOpacity*CaptureVisibility(group,actor,actorBounds,source)});
            if(actor.LowDrawParts.Count==0)continue;
            float visibility=actor.TryGetBounds(reflectionLayers.value,nearClock,out actorBounds,out _,false)?CaptureVisibility(group,actor,actorBounds,source):0;
            foreach(var part in actor.LowDrawParts)
            {
                if(!part.anchor||!part.anchor.gameObject.activeInHierarchy||!part.mesh)continue;
                var matrix=part.Matrix;var bounds=TransformBounds(part.mesh.bounds,matrix);if(!GeometryUtility.TestPlanesAABB(atlasPlanes,bounds))continue;
                batch.draws.Add(new AtlasDraw{mesh=part.mesh,matrix=matrix,materials=part.materials,bounds=bounds,layer=actor.gameObject.layer,visibility=visibility});
            }
        }
        foreach(var renderer in dynamicModels)
        {
            var actor=renderer.GetComponentInParent<ReflectionActor>();if(actor&&actor.LowDrawParts.Count>0)continue;
            var mesh=dynamicMeshes.TryGetValue(renderer,out var low)?low:renderer is SkinnedMeshRenderer sk?sk.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if(!mesh)continue;
            batch.draws.Add(new AtlasDraw{mesh=mesh,skin=renderer is SkinnedMeshRenderer?renderer:null,matrix=renderer.localToWorldMatrix,materials=renderer.sharedMaterials,bounds=renderer.bounds,layer=renderer.gameObject.layer,visibility=actor&&actor.TryGetBounds(reflectionLayers.value,nearClock,out var actorBounds,out _,false)?CaptureVisibility(group,actor,actorBounds,source):1});
        }
        // Resolve material wrappers now, outside render graph execution.
        foreach(var draw in batch.draws)foreach(var material in draw.materials)AtlasMaterial(material);
        atlasBatches.Add(batch);AtlasOwner=this;group.vp=vp;group.ready=true;
        TotalCaptures++;CapturesThisTick++;UpdatedAtlasRegions++;RememberDynamic(group,source);
    }
    static Bounds TransformBounds(Bounds b,Matrix4x4 m)
    {
        var x=m.MultiplyVector(new Vector3(b.extents.x,0,0));var y=m.MultiplyVector(new Vector3(0,b.extents.y,0));var z=m.MultiplyVector(new Vector3(0,0,b.extents.z));
        return new Bounds(m.MultiplyPoint3x4(b.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
    }
    Material AtlasMaterial(Material original)
    {
        if(!original)return null;if(atlasMaterials.TryGetValue(original,out var found)&&found){SetNearKeyword(found);return found;}
        var m=new Material(atlasActorShader){hideFlags=HideFlags.HideAndDontSave};
        foreach(string property in new[]{"_BaseMap","_BumpMap","_EmissionMap","_MetallicGlossMap"})if(original.HasProperty(property)&&original.GetTexture(property))m.SetTexture(property,original.GetTexture(property));
        if(original.HasProperty("_BaseMap")){m.SetTextureScale("_BaseMap",original.GetTextureScale("_BaseMap"));m.SetTextureOffset("_BaseMap",original.GetTextureOffset("_BaseMap"));}
        foreach(string property in new[]{"_BaseColor","_EmissionColor"})if(original.HasProperty(property))m.SetColor(property,original.GetColor(property));
        foreach(string property in new[]{"_Metallic","_Smoothness","_BumpScale","_Cutoff"})if(original.HasProperty(property))m.SetFloat(property,original.GetFloat(property));
        m.SetFloat("_UseNormal",original.IsKeywordEnabled("_NORMALMAP")?1:0);m.SetFloat("_UseMetallic",original.IsKeywordEnabled("_METALLICSPECGLOSSMAP")?1:0);
        m.SetFloat("_AlphaClip",original.IsKeywordEnabled("_ALPHATEST_ON")?1:0);m.SetFloat("_EmissionOn",original.IsKeywordEnabled("_EMISSION")?1:0);
        m.SetFloat("_AtlasUnlit",original.shader.name.Contains("Unlit")?1:0);
        SetNearKeyword(m);atlasMaterials[original]=m;return m;
    }
    void SetNearKeyword(Material material){if(material.IsKeywordEnabled("_GLASS_NEAR_SIMPLE")==nearProfileEnabled)return;if(nearProfileEnabled)material.EnableKeyword("_GLASS_NEAR_SIMPLE");else material.DisableKeyword("_GLASS_NEAR_SIMPLE");}
    readonly Vector4[] lightPositions=new Vector4[4],lightColours=new Vector4[4],lightDirections=new Vector4[4];
    readonly float[] lightDistances=new float[4];
    void SetAtlasLights(CommandBuffer cmd,AtlasDraw draw)
    {
        Light sun=AtlasSun;
        var sunColour=sun&&sun.isActiveAndEnabled?sun.color.linear*sun.intensity:Color.black;
        cmd.SetGlobalFloat("_AtlasLocalLights",!nearProfileEnabled||localReflectionLights?1:0);
        cmd.SetGlobalVector("_AtlasSunColour",sunColour);cmd.SetGlobalVector("_AtlasSunDirection",sun?-sun.transform.forward:Vector3.up);
        if(nearProfileEnabled&&!localReflectionLights)return;
        for(int i=0;i<4;i++){lightDistances[i]=float.PositiveInfinity;lightPositions[i]=lightColours[i]=lightDirections[i]=Vector4.zero;}
        foreach(var light in atlasLights)
        {
            if(!light||!light.isActiveAndEnabled||light.intensity<=0||(light.cullingMask&(1<<draw.layer))==0)continue;
            if(light.type==LightType.Directional)continue;
            if(nearProfileEnabled&&!localReflectionLights)continue;
            if(light.type!=LightType.Point&&light.type!=LightType.Spot)continue;
            float distance=draw.bounds.SqrDistance(light.transform.position);if(distance>light.range*light.range)continue;
            int at=3;while(at>=0&&distance<lightDistances[at])at--;at++;if(at>=4)continue;
            for(int j=3;j>at;j--){lightDistances[j]=lightDistances[j-1];lightPositions[j]=lightPositions[j-1];lightColours[j]=lightColours[j-1];lightDirections[j]=lightDirections[j-1];}
            lightDistances[at]=distance;var position=light.transform.position;var colour=light.color.linear*light.intensity;var direction=light.transform.forward;
            lightPositions[at]=new Vector4(position.x,position.y,position.z,Mathf.Max(.01f,light.range));
            lightColours[at]=new Vector4(colour.r,colour.g,colour.b,light.type==LightType.Spot?Mathf.Cos(light.spotAngle*.5f*Mathf.Deg2Rad):-1);
            lightDirections[at]=new Vector4(direction.x,direction.y,direction.z,light.type==LightType.Spot?Mathf.Cos(light.innerSpotAngle*.5f*Mathf.Deg2Rad):1);
        }

        cmd.SetGlobalVectorArray("_AtlasLightPositions",lightPositions);cmd.SetGlobalVectorArray("_AtlasLightColours",lightColours);cmd.SetGlobalVectorArray("_AtlasLightDirections",lightDirections);
    }
    public void DrawAtlas(CommandBuffer cmd,bool shadowAvailable=false,int cascadeCount=1)
    {
        if(!HasAtlasWork)return;
        MainShadowAvailable=nearProfileEnabled&&useExistingSunShadows&&shadowAvailable;
        cmd.SetGlobalFloat("_AtlasShadowReady",MainShadowAvailable?1:0);cmd.SetGlobalInt("_AtlasCascadeCount",cascadeCount);
        cmd.SetRenderTarget(atlas,atlasDepth);
        if(atlasNeedsClear){cmd.ClearRenderTarget(true,true,Color.clear,SystemInfo.usesReversedZBuffer?0:1);atlasNeedsClear=false;}
        foreach(var batch in atlasBatches)
        {
            // Draw an explicit viewport clear; ClearRenderTarget would erase cached tiles too.
            var padded=new Rect(batch.viewport.x-8,batch.viewport.y-8,batch.viewport.width+16,batch.viewport.height+16);
            cmd.SetViewport(padded);cmd.DrawProcedural(Matrix4x4.identity,tileClear,0,MeshTopology.Triangles,3);
            cmd.SetViewport(batch.viewport);cmd.SetGlobalVector("_AtlasEye",batch.eye);cmd.SetGlobalMatrix("_AtlasVP",batch.vp);
            if((batch.draws.Count>0||batch.contacts.Count>0)&&reflectionOcclusion&&occlusionMaterial)foreach(var part in reflectionOcclusion.parts)if(part.source&&part.mesh)
            {cmd.SetGlobalMatrix("_GlassOcclusionMVP",batch.vp*part.source.localToWorldMatrix);cmd.DrawMesh(part.mesh,Matrix4x4.identity,occlusionMaterial,0,dynamicDebug==DynamicDebug.OcclusionDepth?1:0);AtlasTriangles+=(long)part.mesh.GetIndexCount(0)/3;}
            if(dynamicDebug==DynamicDebug.OcclusionDepth)continue;
            if(batch.contacts.Count>0&&contactShadowShader)
            {
                if(!contactMaterial)contactMaterial=new Material(contactShadowShader){hideFlags=HideFlags.HideAndDontSave};
                if(!contactQuad)contactQuad=Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                foreach(var contact in batch.contacts){cmd.SetGlobalFloat("_AtlasContactOpacity",contact.opacity);cmd.SetGlobalMatrix("_GlassOcclusionMVP",batch.vp*contact.matrix);cmd.DrawMesh(contactQuad,Matrix4x4.identity,contactMaterial);ContactShadowDraws++;AtlasTriangles+=2;}
            }
            foreach(var draw in batch.draws)
            {
                SetAtlasLights(cmd,draw);cmd.SetGlobalFloat("_AtlasActorFade",draw.visibility);cmd.SetGlobalMatrix("_AtlasModel",draw.matrix);cmd.SetGlobalMatrix("_AtlasNormal",draw.matrix.inverse.transpose);AtlasModels++;
                for(int sub=0;sub<draw.mesh.subMeshCount;sub++)
                {
                    if(draw.materials.Length==0)continue;var material=AtlasMaterial(draw.materials[Mathf.Min(sub,draw.materials.Length-1)]);if(!material)continue;
                    if(draw.skin)cmd.DrawRenderer(draw.skin,material,sub,0);else cmd.DrawMesh(draw.mesh,draw.matrix,material,sub,0);
                    AtlasTriangles+=(long)draw.mesh.GetIndexCount(sub)/3;
                }
            }
        }
        cmd.GenerateMips(atlas);DynamicModelCount=AtlasModels;DynamicTriangles=AtlasTriangles;
        // The feature restores the main camera targets and viewport after this call.
    }
    void ReleaseAtlasTexture(){if(atlasDepthHandle!=null){atlasDepthHandle.Release();atlasDepthHandle=null;}if(atlasDepth){atlasDepth.Release();DestroyResource(atlasDepth);atlasDepth=null;}if(atlasHandle!=null){atlasHandle.Release();atlasHandle=null;}if(atlas){atlas.Release();DestroyResource(atlas);atlas=null;}}
    void ReleaseAtlas()
    {
        ReleaseNearProfile();ReleaseAtlasTexture();foreach(var m in atlasMaterials.Values)if(m)DestroyResource(m);atlasMaterials.Clear();
        if(tileClear)DestroyResource(tileClear);tileClear=null;atlasBatches.Clear();freeBatches.Clear();atlasSlots.Clear();if(AtlasOwner==this)AtlasOwner=null;
    }
}
