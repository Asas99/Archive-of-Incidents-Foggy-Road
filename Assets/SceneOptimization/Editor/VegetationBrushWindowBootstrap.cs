using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class VegetationBrushWindowBootstrap
{
 static VegetationBrushWindowBootstrap(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  const string request="Library/VegetationBrush.open";
  if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  File.Delete(request);FoggyRoad_BitkiSilgi.AcEkle();
  File.WriteAllText(File.ReadAllText("Library/VegetationBrushBackup.path").Trim()+"/Loaded.txt","PASS Editor tool compiled and opened. Brush is not activated; no vegetation or heights edited.\n");
 }
}
