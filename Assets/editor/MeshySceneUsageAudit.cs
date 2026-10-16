using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MeshySceneUsageAudit
{
    [Serializable] public class Entry { public string folder; public long bytes; public bool usedInScene; public string[] otherReferences; }
    [Serializable] public class Report { public string scene; public bool unsavedChanges; public string time; public Entry[] entries; }
    static MeshySceneUsageAudit() { EditorApplication.update+=Poll; }
    static void Poll()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Library/MeshySceneUsageAudit.request")) return;
        File.Delete("Library/MeshySceneUsageAudit.request");
        try { Audit(); } catch(Exception e) { File.WriteAllText("Library/MeshySceneUsageAudit.error.txt",e.ToString()); }
    }
    [MenuItem("Tools/Foggy Road/Audit Meshy Imports in Active Scene")]
    public static void Audit()
    {
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("No loaded active scene.");
        var roots=scene.GetRootGameObjects();
        var objects=new List<UnityEngine.Object>();
        foreach(var root in roots)
        {
            objects.Add(root);
            objects.AddRange(root.GetComponentsInChildren<Component>(true).Where(c=>c).Cast<UnityEngine.Object>());
        }
        var paths=new HashSet<string>(EditorUtility.CollectDependencies(objects.ToArray()).Select(AssetDatabase.GetAssetPath).Where(p=>!string.IsNullOrEmpty(p)),StringComparer.OrdinalIgnoreCase);
        // Keep on-disk references too: conservative for unsaved removals and scene lighting data.
        if(!string.IsNullOrEmpty(scene.path)) paths.UnionWith(AssetDatabase.GetDependencies(scene.path,true));
        paths.UnionWith(AssetDatabase.GetDependencies(paths.ToArray(),true));
        var entries=AssetDatabase.GetSubFolders("Assets/MeshyImports").OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Select(p=>new Entry{folder=p,usedInScene=paths.Any(a=>a.StartsWith(p+"/",StringComparison.OrdinalIgnoreCase)),bytes=Directory.EnumerateFiles(p,"*",SearchOption.AllDirectories).Sum(f=>new FileInfo(f).Length)}).ToArray();
        var other=new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach(var e in entries.Where(e=>!e.usedInScene)) other[e.folder]=new HashSet<string>();
        foreach(var path in AssetDatabase.GetAllAssetPaths())
        {
            if(!path.StartsWith("Assets/") || path==scene.path || path.StartsWith("Assets/GasStationGlass/Backups/")) continue;
            string ext=Path.GetExtension(path).ToLowerInvariant();
            if(ext!=".unity" && ext!=".prefab" && ext!=".asset" && ext!=".mat") continue;
            foreach(var dependency in AssetDatabase.GetDependencies(path,false))
            {
                if(!dependency.StartsWith("Assets/MeshyImports/")) continue;
                int slash=dependency.IndexOf('/',"Assets/MeshyImports/".Length);
                if(slash<0) continue;
                string folder=dependency.Substring(0,slash);
                if(!path.StartsWith(folder+"/",StringComparison.OrdinalIgnoreCase) && other.TryGetValue(folder,out var users)) users.Add(path);
            }
        }
        foreach(var e in entries) e.otherReferences=other.TryGetValue(e.folder,out var users) ? users.OrderBy(p=>p).ToArray() : Array.Empty<string>();
        var report=new Report {scene=scene.path,unsavedChanges=scene.isDirty,time=DateTime.Now.ToString("s"),entries=entries};
        Directory.CreateDirectory("Recovery/MeshyUsageAudit");
        File.WriteAllText("Recovery/MeshyUsageAudit/ActiveSceneUsage.json",JsonUtility.ToJson(report,true));
        var md=new StringBuilder("# MeshyImports active scene usage\n\nScene: "+scene.path+"\n\nLive scene + saved-scene dependency union. Includes inactive objects, prefabs, materials, textures and indirect references. No assets deleted.\n\n");
        foreach(var e in entries.Where(e=>!e.usedInScene))
        {
            md.AppendLine("- `"+e.folder+"` — "+(e.bytes/1048576.0).ToString("F1")+" MiB");
            foreach(var user in e.otherReferences) md.AppendLine("  - Other asset reference: `"+user+"`");
        }
        md.AppendLine("\nScope: unused by this scene does not guarantee unused by runtime string loading, editor import scripts or other assets. Old gas station backup scenes excluded from other-asset list. Unsaved removals remain conservatively treated as used until saved.");
        File.WriteAllText("Recovery/MeshyUsageAudit/UnusedInActiveScene.md",md.ToString());
        File.WriteAllText("Library/MeshySceneUsageAudit.done.txt","PASS "+report.time+" Scene="+scene.path+" folders="+entries.Length+" unused="+entries.Count(e=>!e.usedInScene));
    }
}
