using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class ValidateGrassRangeUploads
{
    [StructLayout(LayoutKind.Sequential)] struct Range { public uint start,count; }
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        var grass=UnityEngine.Object.FindObjectsByType<FarGrassRenderer>(FindObjectsSortMode.None).First(g=>g.isActiveAndEnabled);
        var shader=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset").rendererFeatures.OfType<FarGrassFeature>().Single().indirectGrassShader;
        var caches=(IDictionary)typeof(FarGrassRenderer).GetField("gpuCaches",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(grass);
        int Sum(string field){int total=0;foreach(DictionaryEntry e in caches)total+=(int)e.Value.GetType().GetField(field).GetValue(e.Value);return total;}
        int VerifyBuffers()
        {
            int checkedRanges=0;
            foreach(DictionaryEntry entry in caches)
            {
                var g=entry.Value;var type=g.GetType();int count=(int)type.GetField("count").GetValue(g);if(count==0)continue;
                var buffer=(ComputeBuffer)type.GetField("ranges").GetValue(g);var expected=(Array)type.GetField("candidates").GetValue(g);var actual=new Range[count];buffer.GetData(actual,0,0,count);
                for(int i=0;i<count;i++)
                {
                    var value=expected.GetValue(i);var t=value.GetType();
                    if(actual[i].start!=(uint)t.GetField("start").GetValue(value)||actual[i].count!=(uint)t.GetField("count").GetValue(value))throw new Exception("GPU range mismatch at "+i);
                    checkedRanges++;
                }
            }
            return checkedRanges;
        }
        var owner=new GameObject("Temporary grass upload validation camera");var camera=owner.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=60;camera.farClipPlane=800;
        var report=new StringBuilder();int ranges=0;
        try
        {
            camera.transform.SetPositionAndRotation(new Vector3(509.513f,2.235f,1248.762f),Quaternion.identity);grass.PrepareGpu(camera,shader);ranges+=VerifyBuffers();
            int before=Sum("rangeUploads"),skipBefore=Sum("rangeUploadSkips");int[] drawCounts={grass.drawCalls,grass.candidatePatches,grass.testedInstances};
            for(int i=0;i<100;i++)grass.PrepareGpu(camera,shader);
            int avoided=Sum("rangeUploadSkips")-skipBefore;
            if(Sum("rangeUploads")!=before||avoided==0)throw new Exception("Stationary camera did not reuse range uploads");
            if(drawCounts[0]!=grass.drawCalls||drawCounts[1]!=grass.candidatePatches||drawCounts[2]!=grass.testedInstances)throw new Exception("Draw candidate selection changed while reusing buffers");
            ranges+=VerifyBuffers();report.AppendLine("PASS 100 identical camera preparations: additional uploads=0; avoided="+avoided+"; draw candidate selection unchanged");
            foreach(var pose in new[]{new Vector4(509.513f,2.235f,1248.762f,180),new Vector4(500,2.235f,1180,90),new Vector4(440,2.25f,902,0),new Vector4(509.513f,2.235f,1248.762f,0)})
            {
                camera.transform.SetPositionAndRotation(new Vector3(pose.x,pose.y,pose.z),Quaternion.Euler(0,pose.w,0));grass.PrepareGpu(camera,shader);ranges+=VerifyBuffers();
                report.AppendLine("PASS changed camera/return view: GPU range buffer matches live CPU candidate ranges");
            }
            // A second camera must replace the shared range buffer when its patch sequence differs.
            var other=new GameObject("Other validation camera");try{var c=other.AddComponent<Camera>();c.enabled=false;c.transform.SetPositionAndRotation(new Vector3(418,2.25f,902),Quaternion.Euler(0,90,0));grass.PrepareGpu(c,shader);ranges+=VerifyBuffers();grass.PrepareGpu(camera,shader);ranges+=VerifyBuffers();report.AppendLine("PASS alternating cameras: both buffer contents match their own candidates");}finally{UnityEngine.Object.DestroyImmediate(other);}
            report.AppendLine("Verified actual GPU range entries="+ranges+"; total uploads="+Sum("rangeUploads")+"; total reuses="+Sum("rangeUploadSkips"));
        }
        finally{UnityEngine.Object.DestroyImmediate(owner);Directory.CreateDirectory("Recovery/DecalVisibilityPermanent_20261114");File.WriteAllText("Recovery/DecalVisibilityPermanent_20261114/GrassRangeValidation.txt",report.ToString());}
        return report.ToString();
    }
}
