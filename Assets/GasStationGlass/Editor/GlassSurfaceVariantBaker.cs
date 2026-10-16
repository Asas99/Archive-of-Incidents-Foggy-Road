using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GlassSurfaceVariantBaker
{
    const string Root="Assets/GasStationGlass/Textures/";
    public const string AssetPath=Root+"GlassSurfaceVariants.asset";
    [MenuItem("Tools/Foggy Road/Gas Station Glass/Bake Distinct Surface Variants")]
    public static void Bake()
    {
        var sourceA=Read("DustSource.png"); var sourceB=Read("WearSource.png");
        const int size=1024; const int count=GlassSurfaceVariation.VariantCount;
        var array=new Texture2DArray(size,size,count,TextureFormat.BC7,true,true)
        {name="Shared Glass Dust and Wear Variants",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=2};
        Directory.CreateDirectory("Library/GlassSurfaceVariantPreviews");
        for(int slice=0;slice<count;slice++)
        {
            var random=new System.Random(104729+slice*7919);
            float Rand(float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
            float left=Rand(0.035f,0.16f),right=Rand(0.025f,0.14f),bottom=Rand(0.09f,0.3f),top=Rand(0.025f,0.13f);
            float a=Rand(0.07f,0.9f),b=Rand(0.07f,0.9f),c=Rand(0.07f,0.9f),d=Rand(0.07f,0.9f),phase=Rand(0,80);
            float patchX=Rand(0.2f,0.8f),patchY=Rand(0.25f,0.85f),patchWidth=Rand(0.14f,0.4f),patchHeight=Rand(0.12f,0.3f);
            float wearScale=Rand(0.55f,0.9f),wearX=Rand(0,0.1f),wearY=Rand(0,0.1f);
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1),v=y/(float)(size-1);
                float waves=0.55f+0.8f*Mathf.PerlinNoise(u*(3+slice%4)+phase,phase*0.3f);
                float bottomHeight=bottom*waves;
                // Independent source strips replace the original full-window silhouette.
                float l=Sample(sourceA,0.025f+u*0.75f,Mathf.PingPong(a+v*(0.6f+slice*0.05f),1))*Falloff(u,left);
                float r=Sample(sourceA,0.975f-(1-u)*0.75f,Mathf.PingPong(b+v*1.1f,1))*Falloff(1-u,right);
                float low=Sample(sourceA,Mathf.PingPong(c+u*1.3f,1),0.025f+v*0.7f)*Falloff(v,bottomHeight)*1.35f;
                float high=Sample(sourceA,Mathf.PingPong(d+u*0.8f,1),0.975f-(1-v)*0.65f)*Falloff(1-v,top)*0.7f;
                float cloudy=Mathf.Pow(Mathf.PerlinNoise(u*5+phase,v*7+phase),5)*0.12f;
                float ellipse=Mathf.Pow((u-patchX)/patchWidth,2)+Mathf.Pow((v-patchY)/patchHeight,2);
                float wiped=Mathf.SmoothStep(0.12f,1,Mathf.Clamp01(ellipse));
                float grit=0.45f+0.8f*Mathf.PerlinNoise(u*21+phase,v*29+phase);
                float dust=Mathf.Clamp01((Mathf.Max(Mathf.Max(l,r),Mathf.Max(low,high))+cloudy)*grit*wiped);
                float wear=Sample(sourceB,wearX+u*wearScale,wearY+v*wearScale);
                wear*=0.4f+0.8f*Mathf.PerlinNoise(u*7+phase,v*9+phase);
                pixels[y*size+x]=new Color32((byte)(dust*255),(byte)(Mathf.Clamp01(wear)*255),0,255);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true);
            texture.SetPixels32(pixels); texture.Apply(true,false);
            // Previews are diagnostic masks, not per-pane runtime textures.
            File.WriteAllBytes("Library/GlassSurfaceVariantPreviews/Variant_"+slice.ToString("D2")+".png",texture.EncodeToPNG());
            EditorUtility.CompressTexture(texture,TextureFormat.BC7,TextureCompressionQuality.Best);
            for(int mip=0;mip<array.mipmapCount;mip++) array.SetPixelData(texture.GetPixelData<byte>(mip),mip,slice);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        array.Apply(false,true);
        var existing=AssetDatabase.LoadAssetAtPath<Texture2DArray>(AssetPath);
        if(existing) { EditorUtility.CopySerialized(array,existing); UnityEngine.Object.DestroyImmediate(array); }
        else { AssetDatabase.CreateAsset(array,AssetPath); existing=array; }
        foreach(string path in new[]{"Assets/GasStationGlass/Materials/Station Dusty Glass.mat","Assets/Kullanıcı içeriği/materyaller/glass mat.mat"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material || material.shader.name!=GlassSurfaceVariation.ShaderName) continue;
            material.SetTexture("_SurfaceVariants",existing); material.SetFloat("_UseSurfaceVariants",1); EditorUtility.SetDirty(material);
        }
        foreach(string name in new[]{"DustSource.png","WearSource.png"})
        { var importer=(TextureImporter)AssetImporter.GetAtPath(Root+name); importer.isReadable=false; importer.SaveAndReimport(); }
        AssetDatabase.SaveAssets(); GlassSurfaceVariationEditor.Refresh();
    }
    struct Source { public Color32[] pixels; public int width,height; }
    static Source Read(string name)
    {
        var path=Root+name; var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.isReadable=true; importer.sRGBTexture=false; importer.npotScale=TextureImporterNPOTScale.None; importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport(); var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return new Source{pixels=texture.GetPixels32(),width=texture.width,height=texture.height};
    }
    static float Falloff(float distance,float width) { return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/width)); }
    static float Sample(Source source,float u,float v)
    {
        float x=Mathf.Clamp01(u)*(source.width-1),y=Mathf.Clamp01(v)*(source.height-1); int ix=(int)x,iy=(int)y;
        int jx=Mathf.Min(ix+1,source.width-1),jy=Mathf.Min(iy+1,source.height-1); float fx=x-ix,fy=y-iy;
        return Mathf.Lerp(Mathf.Lerp(source.pixels[iy*source.width+ix].r,source.pixels[iy*source.width+jx].r,fx),Mathf.Lerp(source.pixels[jy*source.width+ix].r,source.pixels[jy*source.width+jx].r,fx),fy)/255;
    }
}
