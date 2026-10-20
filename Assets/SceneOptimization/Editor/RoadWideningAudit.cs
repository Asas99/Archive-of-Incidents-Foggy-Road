using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
[InitializeOnLoad] public static class RoadWideningAudit
{
 static RoadWideningAudit(){EditorApplication.update+=Poll;}
 [Serializable] class Data{public Vector3[] vertices;public int[] triangles;public Vector2[] uv;public string meshPath;public TerrainEntry[] terrains;public RailEntry[] rails;public Bounds station;}
 [Serializable] class TerrainEntry{public string name,path;public Vector3 origin,size;public int heightResolution,detailResolution,trees,detailLayers;}
 [Serializable] class RailEntry{public string name,parent;public Vector3 position;public Bounds bounds;public int children;}
 static void Poll(){const string request="Library/RoadWideningAudit.request";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(request);var backup=File.ReadAllText("Library/RoadWideningBackup.path").Trim();try{
 var scene=EditorSceneManager.GetActiveScene();var road=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.name=="NurbsPath_ROAD"&&x.gameObject.scene==scene);var mesh=road.sharedMesh;
 if(!EditorSceneManager.SaveScene(scene,backup+"/OpenBefore.unity",true))throw new Exception("Backup failed");
 var data=new Data{vertices=mesh.vertices.Select(v=>road.transform.TransformPoint(v)).ToArray(),triangles=mesh.triangles,uv=mesh.uv,meshPath=AssetDatabase.GetAssetPath(mesh),terrains=Terrain.activeTerrains.Select(t=>new TerrainEntry{name=t.name,path=AssetDatabase.GetAssetPath(t.terrainData),origin=t.transform.position,size=t.terrainData.size,heightResolution=t.terrainData.heightmapResolution,detailResolution=t.terrainData.detailResolution,trees=t.terrainData.treeInstanceCount,detailLayers=t.terrainData.detailPrototypes.Length}).ToArray(),rails=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene&&System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^GRPRO_[LR]_\d+_")).Select(t=>{var r=t.GetComponentsInChildren<Renderer>(true);var b=r.Length>0?r[0].bounds:new Bounds(t.position,Vector3.zero);foreach(var x in r.Skip(1))b.Encapsulate(x.bounds);return new RailEntry{name=t.name,parent=t.parent?t.parent.name:"",position=t.position,bounds=b,children=t.childCount};}).ToArray()};
 var station=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.gameObject.scene==scene&&t.name=="benzinlik");if(station){var rr=station.GetComponentsInChildren<Renderer>(true);if(rr.Length>0){data.station=rr[0].bounds;foreach(var r in rr.Skip(1))data.station.Encapsulate(r.bounds);}}
 foreach(var t in data.terrains){var dest=backup+"/"+Path.GetFileName(t.path);if(!File.Exists(dest))File.Copy(t.path,dest);}
 File.WriteAllText(backup+"/Audit.json",JsonUtility.ToJson(data));Debug.Log("ROAD_AUDIT_PASS");
 }catch(Exception e){File.WriteAllText(backup+"/AuditError.txt",e.ToString());Debug.LogException(e);}}
}
