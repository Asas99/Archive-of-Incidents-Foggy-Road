using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class VegetationLabTests
{
 static Type Settings => Type.GetType("VegetationOptimizationSettings, Assembly-CSharp-Editor",true);
 static Type Profile => Type.GetType("SceneOptimizationProfile, Assembly-CSharp",true);
 static Type Density => Type.GetType("VegetationDensityRuntime, Assembly-CSharp",true);
 static void Set(object o,string name,object value) => o.GetType().GetField(name).SetValue(o,value);
 static T Get<T>(object o,string name) => (T)o.GetType().GetField(name).GetValue(o);
 static void Normalize(object o) => Settings.GetMethod("Normalize").Invoke(o,null);
 [Test] public void DensityRanksAreDeterministicPermutationWithNestedSubsets()
 {
  var ranks=(int[])Density.GetMethod("BuildRanks").Invoke(null,new object[]{11204});
  var repeat=(int[])Density.GetMethod("BuildRanks").Invoke(null,new object[]{11204});
  CollectionAssert.AreEqual(ranks,repeat);
  CollectionAssert.AreEquivalent(Enumerable.Range(0,11204),ranks);
  var half=Enumerable.Range(0,ranks.Length).Where(i=>ranks[i]<5602).ToArray();
  var quarter=Enumerable.Range(0,ranks.Length).Where(i=>ranks[i]<2801).ToArray();
  Assert.That(half.Length,Is.EqualTo(5602));Assert.That(quarter.All(i=>half.Contains(i)),Is.True);
  Assert.That(ranks.Take(5602).Count(r=>r<5602),Is.InRange(2000,3600),"Subset must not just keep one sequential painted region.");
 }
 [Test] public void EmptyDensityRanksAreSupported() => Assert.That((int[])Density.GetMethod("BuildRanks").Invoke(null,new object[]{0}),Is.Empty);
 [Test] public void LegacyVegetationBackupKeepsNewFieldsAtCompatibleDefaults()
 {
  var settings=JsonUtility.FromJson("{\"treeEnd\":300,\"farGrassEnd\":90}",Settings);
  Assert.That(Get<float>(settings,"treeDensity"),Is.EqualTo(1));
  Assert.That(Get<float>(settings,"nativeGrassDensity"),Is.EqualTo(1));
  Assert.That(Get<float>(settings,"treeLodBias"),Is.EqualTo(1));
  Assert.That(Get<int>(settings,"fullTrees"),Is.EqualTo(400));
 }
 [Test] public void InvalidDensityAndLodValuesAreNormalized()
 {
  var settings=Activator.CreateInstance(Settings);
  Set(settings,"treeDensity",float.NaN);Set(settings,"nativeGrassDensity",-2f);Set(settings,"treeLodBias",float.PositiveInfinity);Set(settings,"fullTrees",-5);
  Normalize(settings);
  Assert.That(Get<float>(settings,"treeDensity"),Is.EqualTo(1));Assert.That(Get<float>(settings,"nativeGrassDensity"),Is.Zero);
  Assert.That(Get<float>(settings,"treeLodBias"),Is.EqualTo(1));Assert.That(Get<int>(settings,"fullTrees"),Is.Zero);
 }
 [Test] public void WritingVegetationSettingsPreservesGlobalGraphicsAndOtherObjectRanges()
 {
  var p=ScriptableObject.CreateInstance(Profile);
  try
  {
   Set(p,"shadowDistance",67f);Set(p,"lodBias",1.37f);Set(p,"mediumEnd",113f);
   var settings=Settings.GetMethod("Read").Invoke(null,new object[]{p});
   Set(settings,"treeDensity",.5f);Set(settings,"nativeGrassDensity",.6f);Set(settings,"treeLodBias",.7f);
   Settings.GetMethod("Write").Invoke(settings,new object[]{p});
   Assert.That(Get<float>(p,"treeDensity"),Is.EqualTo(.5f));Assert.That(Get<float>(p,"nativeGrassDensity"),Is.EqualTo(.6f));
   Assert.That(Get<float>(p,"shadowDistance"),Is.EqualTo(67));Assert.That(Get<float>(p,"lodBias"),Is.EqualTo(1.37f));Assert.That(Get<float>(p,"mediumEnd"),Is.EqualTo(113));
  }
  finally{UnityEngine.Object.DestroyImmediate(p);}
 }
 [Test] public void RepeatedGrassResizeMaintainsOrderedFadeAndDensityBands()
 {
  var p=ScriptableObject.CreateInstance(Profile);
  try
  {
   var settings=Settings.GetMethod("Read").Invoke(null,new object[]{p});
   foreach(float distance in new[]{5f,250f,65f,90f})
   {
    Settings.GetMethod("ResizeGrass").Invoke(settings,new object[]{distance});
    Assert.That(Get<float>(settings,"grassEnd"),Is.LessThanOrEqualTo(Get<float>(settings,"farGrassEnd")));
    Assert.That(Get<float>(settings,"farDensityFullDistance"),Is.LessThan(Get<float>(settings,"farDensityMidDistance")));
    Assert.That(Get<float>(settings,"farDensityMidDistance"),Is.LessThan(Get<float>(settings,"farDensityLowDistance")));
    Assert.That(Get<float>(settings,"farDensityLowDistance"),Is.LessThan(Get<float>(settings,"farGrassEnd")));
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(p);}
 }
}
