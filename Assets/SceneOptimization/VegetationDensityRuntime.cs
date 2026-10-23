using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reversible visual density. The original TerrainData and TerrainCollider are never edited.</summary>
public sealed class VegetationDensityRuntime : IDisposable
{
 sealed class Entry
 {
  public Terrain terrain;
  public TerrainData source, copy;
  public TreeInstance[] trees;
  public int[] ranks;
  public float detailDensity;
  public int lastCount = -1;
 }
 readonly List<Entry> entries = new List<Entry>();

 // Nested subsets: increasing the percentage adds trees without moving existing trees.
 public static int[] BuildRanks(int count)
 {
  var order = new int[count]; var ranks = new int[count];
  for (int i=0;i<count;i++) order[i]=i;
  Array.Sort(order, (a,b) => { int c=Hash(a).CompareTo(Hash(b)); return c!=0?c:a.CompareTo(b); });
  for (int i=0;i<count;i++) ranks[order[i]]=i;
  return ranks;
 }
 static uint Hash(int index)
 {
  unchecked { uint v=(uint)index+0x9e3779b9u; v=(v^(v>>16))*0x7feb352du; v=(v^(v>>15))*0x846ca68bu; return v^(v>>16); }
 }
 public void Apply(Terrain[] terrains, float trees, float grass)
 {
  if (!Application.isPlaying) return;
  trees = Mathf.Clamp01(trees); grass = Mathf.Clamp01(grass);
  foreach (var terrain in terrains)
  {
   if (!terrain || !terrain.terrainData) continue;
   var entry=entries.Find(e=>e.terrain==terrain);
   if (entry==null)
   {
    entry=new Entry { terrain=terrain,source=terrain.terrainData,detailDensity=terrain.detailObjectDensity };
    entries.Add(entry);
   }
   // A different tool replaced this TerrainData: never overwrite its reference.
   if (terrain.terrainData!=entry.source && terrain.terrainData!=entry.copy) continue;
   terrain.detailObjectDensity=entry.detailDensity*grass;
   if (trees>=1)
   {
    if (terrain.terrainData==entry.copy) terrain.terrainData=entry.source;
    continue;
   }
   if (entry.trees==null) { entry.trees=entry.source.treeInstances; entry.ranks=BuildRanks(entry.trees.Length); }
   if (entry.trees.Length==0) continue;
   if (!entry.copy)
   {
    entry.copy=UnityEngine.Object.Instantiate(entry.source);
    entry.copy.name=entry.source.name+" (temporary vegetation density)";
    entry.copy.hideFlags=HideFlags.HideAndDontSave;
   }
   int count=Mathf.FloorToInt(entry.trees.Length*trees);
   if (entry.lastCount!=count)
   {
    var subset=new TreeInstance[count]; int next=0;
    for(int i=0;i<entry.trees.Length;i++) if(entry.ranks[i]<count) subset[next++]=entry.trees[i];
    entry.copy.SetTreeInstances(subset,false); entry.lastCount=count;
   }
   terrain.terrainData=entry.copy;
  }
 }
 public void Dispose()
 {
  foreach(var e in entries)
  {
   if(e.terrain)
   {
    if(e.copy && e.terrain.terrainData==e.copy) e.terrain.terrainData=e.source;
    e.terrain.detailObjectDensity=e.detailDensity;
   }
   if(e.copy) UnityEngine.Object.Destroy(e.copy);
  }
  entries.Clear();
 }
}
