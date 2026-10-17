using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DoorwayColliderTool
{
    public const string Output="Recovery/DoorwayColliders";
    public static readonly string[][] Targets={
        new[]{"kap\u0131i\u00e71 (1)","kap\u0131i\u00e71 (2)"},
        new[]{"kap\u0131i\u00e71","kap\u0131i\u00e71.1"},
        new[]{"kap\u0131i\u00e71 (3)","Meshy_AI_Threshold_of_Silence_1010191343_texture"},
        new[]{"kap\u0131i\u00e71 (4)","kap\u0131i\u00e71 (5)"},
        new[]{"kap\u0131i\u00e71 (6)","Meshy_AI_Threshold_of_Silence_1008053606_texture (4)"}};
    public sealed class Draft
    {
        public MeshFilter a,b;public Vector3 origin;public Quaternion rotation;public Bounds bounds;
        public Vector2 min,max;public Transform hinge;public string method;
    }
    static DoorwayColliderTool() {EditorApplication.update+=Requests;}
    static void Requests()
    {
        const string file="Library/DoorwayColliders.request";
        if(!File.Exists(file) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string command=File.ReadAllText(file).Trim();
        if(command=="playtest")
        {
            if(!EditorApplication.isPlaying) {if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=true;return;}
            File.Delete(file);
            try {DoorwayColliderValidation.Validate(true);} catch(Exception e){Error(e);}
            finally {if(File.Exists(Output+"/PlayMode.txt") && File.ReadAllText(Output+"/PlayMode.txt").EndsWith("PASS\n")) File.WriteAllText(file,"save"); EditorApplication.isPlaying=false;} return;
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(file);
        try {if(command=="build") BuildAll();else if(command=="validate") DoorwayColliderValidation.Validate(false);else if(command=="save"){DoorwayColliderValidation.Validate(false);if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Final scene save failed");File.WriteAllText(Output+"/Saved.txt","PASS validated scene saved");}else Audit();}
        catch(Exception e){Error(e);}
    }
    static void Error(Exception e) {Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Error.txt",e.ToString());Debug.LogException(e);}
    public static string PathOf(Transform t) {string s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
    public static Draft[] Drafts()
    {
        var scene=SceneManager.GetActiveScene();var all=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m.gameObject.scene==scene).ToArray();
        return Targets.Select(pair=>
        {
            MeshFilter Find(string name) {var found=all.Where(m=>m.name==name).ToArray();if(found.Length!=1||!found[0].sharedMesh)throw new InvalidOperationException(name+": expected exactly one valid scene mesh, found "+found.Length);return found[0];}
            return Measure(Find(pair[0]),Find(pair[1]));
        }).ToArray();
    }
    static Vector3[] Vertices(Mesh mesh)
    {
        using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
        using(var vertices=new NativeArray<Vector3>(mesh.vertexCount,Allocator.Temp)) {data[0].GetVertices(vertices);return vertices.ToArray();}
    }
    public static Draft Measure(MeshFilter a,MeshFilter b)
    {
        var size=a.sharedMesh.bounds.size;int axis=size.x<size.y?(size.x<size.z?0:2):(size.y<size.z?1:2);
        Vector3 local=Vector3.zero;local[axis]=1;
        Vector3 normal=a.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(local).normalized;
        normal=Vector3.ProjectOnPlane(normal,Vector3.up).normalized;
        if(normal.sqrMagnitude<0.99f) throw new InvalidOperationException("Wall is not upright: "+a.name);
        var draft=new Draft{a=a,b=b,origin=(a.GetComponent<Renderer>().bounds.center+b.GetComponent<Renderer>().bounds.center)*0.5f,rotation=Quaternion.LookRotation(normal,Vector3.up)};
        var inv=Matrix4x4.TRS(draft.origin,draft.rotation,Vector3.one).inverse;
        var vertices=new List<Vector3[]>(); bool first=true;Bounds bounds=default;
        foreach(var source in new[]{a,b})
        {
            var points=Vertices(source.sharedMesh);var matrix=inv*source.transform.localToWorldMatrix;
            for(int i=0;i<points.Length;i++) {points[i]=matrix.MultiplyPoint3x4(points[i]);if(first){bounds=new Bounds(points[i],Vector3.zero);first=false;}else bounds.Encapsulate(points[i]);}
            vertices.Add(points);
        }
        if(bounds.size.z>Mathf.Min(bounds.size.x,bounds.size.y)*0.6f) throw new InvalidOperationException("Pair does not form one thin doorway wall: "+a.name);
        draft.bounds=bounds;
        const int n=192;bool[,] occupied=new bool[n,n];int sourceIndex=0;
        foreach(var source in new[]{a,b})
        {
            var points=vertices[sourceIndex++];
            using(var data=MeshUtility.AcquireReadOnlyMeshData(source.sharedMesh))
            {
                var d=data[0];bool wide=d.indexFormat==IndexFormat.UInt32;
                var small=wide?default:d.GetIndexData<ushort>();var large=wide?d.GetIndexData<uint>():default;
                Vector2 Project(int i) => new Vector2((points[i].x-bounds.min.x)/bounds.size.x*n,(points[i].y-bounds.min.y)/bounds.size.y*n);
                for(int part=0;part<d.subMeshCount;part++)
                {
                    var sub=d.GetSubMesh(part);if(sub.topology!=MeshTopology.Triangles)continue;
                    for(int j=sub.indexStart;j<sub.indexStart+sub.indexCount;j+=3)
                    {
                        Vector2 p=Project((wide?(int)large[j]:small[j])+sub.baseVertex),q=Project((wide?(int)large[j+1]:small[j+1])+sub.baseVertex),r=Project((wide?(int)large[j+2]:small[j+2])+sub.baseVertex);
                        float area=Cross(q-p,r-p);if(Mathf.Abs(area)<0.000001f)continue;
                        int xmin=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p.x,Mathf.Min(q.x,r.x))),0,n-1),xmax=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p.x,Mathf.Max(q.x,r.x))),0,n-1);
                        int ymin=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p.y,Mathf.Min(q.y,r.y))),0,n-1),ymax=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p.y,Mathf.Max(q.y,r.y))),0,n-1);
                        for(int y=ymin;y<=ymax;y++) for(int x=xmin;x<=xmax;x++)
                        {
                            var v=new Vector2(x+0.5f,y+0.5f);float u=Cross(q-p,v-p)/area,w=Cross(v-p,r-p)/area;
                            if(u>=0&&w>=0&&u+w<=1)occupied[x,y]=true;
                        }
                    }
                }
            }
        }
        // Find the widest clear run that remains open over human-height samples.
        int y0=Mathf.Clamp(Mathf.CeilToInt(n*0.08f),1,n-2),y1=Mathf.Clamp(Mathf.FloorToInt(n*0.60f),y0+1,n-2);
        int bestL=-1,bestR=-1,start=-1;
        for(int x=1;x<n-1;x++)
        {
            bool clear=true;for(int y=y0;y<=y1;y++)if(occupied[x,y]){clear=false;break;}
            if(clear&&start<0)start=x;
            if((!clear||x==n-2)&&start>=0){int end=clear?x:x-1;if(end-start>bestR-bestL){bestL=start;bestR=end;}start=-1;}
        }
        draft.method="Projected triangles: shared front/back empty opening";
        if(bestL<0 || (bestR-bestL)*bounds.size.x/n<0.7f) throw new InvalidOperationException("No reliable mesh opening: "+a.name+". Use manual preview handles; no collider generated.");
        // Contract one grid cell away from uncertain silhouette pixels.
        bestL++;bestR--;int top=y1;
        while(top<n-1)
        {
            bool clear=true;for(int x=bestL;x<=bestR;x++)if(occupied[x,top]){clear=false;break;}
            if(!clear)break;top++;
        }
        draft.min=new Vector2(bounds.min.x+(bestL+0.5f)*bounds.size.x/n,bounds.min.y);
        draft.max=new Vector2(bounds.min.x+(bestR+0.5f)*bounds.size.x/n,bounds.min.y+(top-1)*bounds.size.y/n);
        var hinges=UnityEngine.Object.FindObjectsByType<TwoSidedDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(h=>h.gameObject.scene==a.gameObject.scene)
            .Select(h=>new{h,p=inv.MultiplyPoint3x4(h.transform.position)}).Where(v=>Mathf.Abs(v.p.z)<bounds.size.z+1 && v.p.x>bounds.min.x && v.p.x<bounds.max.x)
            .OrderBy(v=>Mathf.Abs(v.p.x-(draft.min.x+draft.max.x)*0.5f)).ToArray();
        if(hinges.Length>0)draft.hinge=hinges[0].h.transform;
        Directory.CreateDirectory(Output);
        var image=new Texture2D(n,n,TextureFormat.RGB24,false);var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)pixels[y*n+x]=occupied[x,y]?Color.white:Color.black;
        image.SetPixels(pixels);image.Apply();File.WriteAllBytes(Output+"/Opening_"+Array.FindIndex(Targets,p=>p[0]==a.name)+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        return draft;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    public static void Audit()
    {
        var report=new StringBuilder();foreach(var d in Drafts())report.AppendLine(d.a.name+" + "+d.b.name+" | wall="+d.bounds+" | opening="+d.min+".."+d.max+" | hinge="+(d.hinge?d.hinge.name:"NOT FOUND"));
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Audit.txt",report.ToString());
    }
    [MenuItem("Tools/Foggy Road/Doorway Colliders/Open Tool")]
    public static void Open()=>EditorWindow.GetWindow<DoorwayColliderWindow>("Doorway Colliders");
    public static DoorwayColliderAuthoring[] Owners()=>UnityEngine.Object.FindObjectsByType<DoorwayColliderAuthoring>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(o=>o.gameObject.scene==SceneManager.GetActiveScene()).OrderBy(o=>o.name).ToArray();
    public static void BuildAll()
    {
        var drafts=Drafts();var scene=SceneManager.GetActiveScene();Directory.CreateDirectory("Assets/GasStationGlass/Backups");
        string backup="Assets/GasStationGlass/Backups/BeforeDoorwayColliders_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Scene backup failed");
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Build five doorway collider sets");
        try
        {
            for(int i=0;i<drafts.Length;i++)
            {
                var d=drafts[i];var owner=Owners().FirstOrDefault(o=>o.first==d.a&&o.second==d.b);
                if(!owner)
                {
                    var go=new GameObject("Doorway Colliders "+(i+1));Undo.RegisterCreatedObjectUndo(go,"Create doorway collider root");go.transform.SetParent(d.a.transform.parent,true);go.transform.SetPositionAndRotation(d.origin,d.rotation);
                    if((go.transform.lossyScale-Vector3.one).sqrMagnitude>0.00001f)throw new InvalidOperationException("Doorway root must have unit world scale");
                    owner=Undo.AddComponent<DoorwayColliderAuthoring>(go);owner.first=d.a;owner.second=d.b;owner.doorHinge=d.hinge;owner.wall=d.bounds;owner.openingMin=d.min;owner.openingMax=d.max;owner.measurement=d.method;
                }
                Rebuild(owner);
            }
            DoorwayColliderValidation.Validate(false);
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(scene);
            File.WriteAllText(Output+"/Built.txt","PASS five pairs/ten source meshes, fifteen boxes. Backup="+backup+"\nRuntime validation and final save pending.");
        }
        catch {Undo.RevertAllDownToGroup(group);throw;}
    }
    public static void Rebuild(DoorwayColliderAuthoring owner)
    {
        Undo.RecordObject(owner,"Update doorway measurements");var shapes=owner.Shapes();
        if(owner.generated.Length!=3)owner.generated=new BoxCollider[3];
        for(int i=0;i<3;i++)
        {
            var box=owner.generated[i];if(!box){var go=new GameObject(new[]{"Left","Right","Header"}[i]);Undo.RegisterCreatedObjectUndo(go,"Create doorway box");go.transform.SetParent(owner.transform,false);go.layer=owner.first.gameObject.layer;box=Undo.AddComponent<BoxCollider>(go);owner.generated[i]=box;}
            Undo.RecordObject(box,"Fit doorway collider");box.center=shapes[i].center;box.size=shapes[i].size;box.enabled=true;box.isTrigger=false;EditorUtility.SetDirty(box);
        }
        if(owner.replaced.Length==0)
        {
            owner.replaced=new[]{owner.first,owner.second}.SelectMany(m=>m.GetComponents<Collider>()).Where(c=>!c.isTrigger).Distinct().ToArray();owner.previousEnabled=owner.replaced.Select(c=>c.enabled).ToArray();
        }
        foreach(var c in owner.replaced)if(c){Undo.RecordObject(c,"Replace source collider");c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        EditorUtility.SetDirty(owner);EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);Physics.SyncTransforms();
    }
    public static void Restore(DoorwayColliderAuthoring owner)
    {
        for(int i=0;i<owner.replaced.Length;i++)if(owner.replaced[i]){Undo.RecordObject(owner.replaced[i],"Restore source collider");owner.replaced[i].enabled=owner.previousEnabled[i];}
        foreach(var c in owner.generated)if(c){Undo.RecordObject(c,"Disable doorway proxy");c.enabled=false;}
    }
}

public sealed class DoorwayColliderWindow : EditorWindow
{
    Vector2 scroll;DoorwayColliderTool.Draft[] drafts;string error;
    void OnEnable(){SceneView.duringSceneGui+=DrawDrafts;}
    void OnDisable(){SceneView.duringSceneGui-=DrawDrafts;}
    void DrawDrafts(SceneView view)
    {
        if(drafts==null)return;
        foreach(var d in drafts)using(new Handles.DrawingScope(Matrix4x4.TRS(d.origin,d.rotation,Vector3.one)))
        {
            Handles.color=Color.green;Handles.DrawWireCube(new Vector3((d.min.x+d.max.x)*0.5f,(d.min.y+d.max.y)*0.5f,d.bounds.center.z),new Vector3(d.max.x-d.min.x,d.max.y-d.min.y,d.bounds.size.z));
            var lo=d.bounds.min;var hi=d.bounds.max;
            var shapes=new[]{new Bounds(new Vector3((lo.x+d.min.x)*0.5f,d.bounds.center.y,d.bounds.center.z),new Vector3(d.min.x-lo.x,d.bounds.size.y,d.bounds.size.z)),new Bounds(new Vector3((d.max.x+hi.x)*0.5f,d.bounds.center.y,d.bounds.center.z),new Vector3(hi.x-d.max.x,d.bounds.size.y,d.bounds.size.z)),new Bounds(new Vector3((d.min.x+d.max.x)*0.5f,(d.max.y+hi.y)*0.5f,d.bounds.center.z),new Vector3(d.max.x-d.min.x,hi.y-d.max.y,d.bounds.size.z))};
            for(int i=0;i<3;i++){Handles.color=new[]{Color.cyan,Color.yellow,Color.magenta}[i];Handles.DrawWireCube(shapes[i].center,shapes[i].size);}
        }
    }
    void OnGUI()
    {
        EditorGUILayout.LabelField("Five doorways / ten source models",EditorStyles.boldLabel);
        if(GUILayout.Button("Preview / measure source geometry")) {try{drafts=DoorwayColliderTool.Drafts();error=null;SceneView.RepaintAll();}catch(Exception e){error=e.Message;}}
        if(GUILayout.Button("Create / update five collider sets")){try{DoorwayColliderTool.BuildAll();error=null;}catch(Exception e){error=e.Message;}}
        if(GUILayout.Button("Validate saved collider sets")){try{DoorwayColliderValidation.Validate(false);error=null;}catch(Exception e){error=e.Message;}}
        if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
        scroll=EditorGUILayout.BeginScrollView(scroll);
        foreach(var owner in DoorwayColliderTool.Owners())
        {
            EditorGUILayout.LabelField(owner.name,EditorStyles.boldLabel);EditorGUILayout.ObjectField("First",owner.first,typeof(MeshFilter),true);EditorGUILayout.ObjectField("Second",owner.second,typeof(MeshFilter),true);
            if(GUILayout.Button("Select / adjust opening")){Selection.activeGameObject=owner.gameObject;SceneView.FrameLastActiveSceneView();}
            if(GUILayout.Button("Restore original colliders"))DoorwayColliderTool.Restore(owner);
        }
        if(drafts!=null)foreach(var d in drafts)EditorGUILayout.LabelField(d.a.name+" + "+d.b.name+" width="+(d.max.x-d.min.x).ToString("F2")+"m");
        EditorGUILayout.EndScrollView();
    }
}

[CustomEditor(typeof(DoorwayColliderAuthoring))]
public sealed class DoorwayColliderAuthoringEditor : Editor
{
    public override void OnInspectorGUI(){DrawDefaultInspector();var o=(DoorwayColliderAuthoring)target;if(GUILayout.Button("Update boxes from opening"))DoorwayColliderTool.Rebuild(o);if(GUILayout.Button("Restore original colliders"))DoorwayColliderTool.Restore(o);}
    void OnSceneGUI()
    {
        var o=(DoorwayColliderAuthoring)target;using(new Handles.DrawingScope(o.transform.localToWorldMatrix))
        {
            var min=o.openingMin;var max=o.openingMax;Vector3 center=new Vector3((min.x+max.x)*0.5f,(min.y+max.y)*0.5f,0);
            Handles.color=Color.green;Handles.DrawWireCube(center,new Vector3(max.x-min.x,max.y-min.y,o.wall.size.z));
            EditorGUI.BeginChangeCheck();float size=HandleUtility.GetHandleSize(o.transform.TransformPoint(center))*0.1f;
            var left=Handles.Slider(new Vector3(min.x,center.y,0),Vector3.right,size,Handles.CubeHandleCap,0);
            var right=Handles.Slider(new Vector3(max.x,center.y,0),Vector3.right,size,Handles.CubeHandleCap,0);
            var bottom=Handles.Slider(new Vector3(center.x,min.y,0),Vector3.up,size,Handles.CubeHandleCap,0);
            var top=Handles.Slider(new Vector3(center.x,max.y,0),Vector3.up,size,Handles.CubeHandleCap,0);
            if(EditorGUI.EndChangeCheck()){Undo.RecordObject(o,"Adjust doorway opening");o.openingMin=new Vector2(left.x,bottom.y);o.openingMax=new Vector2(right.x,top.y);EditorUtility.SetDirty(o);}
            try{var shapes=o.Shapes();for(int i=0;i<shapes.Length;i++){Handles.color=new[]{Color.cyan,Color.yellow,Color.magenta}[i];Handles.DrawWireCube(shapes[i].center,shapes[i].size);}}catch(InvalidOperationException){}
        }
    }
}



