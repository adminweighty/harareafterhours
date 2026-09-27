#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    public static class GroundSurfaceInstaller
    {
        const string Atlas="Assets/Environment/Textures/HarareSurfaceAtlas.png";
        const string Root="Assets/Resources/HarareEnvironment/Surfaces/";
        public static void Install()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.textureShape=TextureImporterShape.Texture2DArray;settings.flipbookColumns=2;settings.flipbookRows=2;
            settings.mipmapEnabled=true;settings.readable=true;settings.sRGBTexture=true;
            settings.wrapMode=TextureWrapMode.Mirror;settings.filterMode=FilterMode.Trilinear;settings.aniso=4;
            importer.SetTextureSettings(settings);importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var array=AssetDatabase.LoadAssetAtPath<Texture2DArray>(Atlas);
            if(array==null||array.depth!=4)throw new System.Exception("Expected four independently tiled surface layers");
            // Identify layers by their palette, avoiding assumptions about the
            // importer row order (image top-left versus GPU bottom-left).
            var colours=Enumerable.Range(0,4).Select(i=>{
                var pixels=array.GetPixels(i,array.mipmapCount-1);return pixels[0];}).ToArray();
            int asphalt=Enumerable.Range(0,4).OrderBy(i=>colours[i].grayscale).First();
            int concrete=Enumerable.Range(0,4).OrderByDescending(i=>colours[i].grayscale).First();
            var remaining=Enumerable.Range(0,4).Where(i=>i!=asphalt&&i!=concrete).ToArray();
            int soil=remaining.OrderByDescending(i=>colours[i].r-colours[i].b).First();
            int paving=remaining.Single(i=>i!=soil);
            Debug.Log("[GroundSurface] "+array.width+"px x4; asphalt="+asphalt+" paving="+paving+" soil="+soil+" concrete="+concrete);
            Make("Asphalt",array,asphalt,2,new Color(.64f,.66f,.68f),.12f);
            InstallPhotographicAsphalt();
            Make("Paving",array,paving,1.6f,new Color(.95f,.92f,.86f),.13f);
            Make("Soil",array,soil,2.5f,new Color(.72f,.73f,.68f),.05f);
            Make("Concrete",array,concrete,2,new Color(.83f,.83f,.80f),.16f);
            Make("WallWarm",array,concrete,3,new Color(.95f,.88f,.74f),.16f);
            Make("WallGrey",array,concrete,3,new Color(.79f,.83f,.83f),.16f);
            Make("WallCream",array,concrete,3,new Color(1,.97f,.87f),.16f);
            Make("WallStone",array,concrete,3,new Color(.71f,.69f,.63f),.16f);
            Paint("RoadPaint",new Color(.78f,.67f,.38f));Paint("CrossingPaint",new Color(.78f,.77f,.70f));
            foreach(string name in new[]{"Paving","Kerb","Stone","StoneAlternate","Concrete","Plaster"})
            {
                string path="Assets/Environment/Materials/FirstStreet_"+name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null)continue;
                var source=GroundEnvironment.Surface(name=="Paving"?"Paving":name=="Kerb"?"Concrete":"WallWarm");
                bool alreadyTextured=material.shader.name=="Harare/World Surface";
                Color tint=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.white;
                material.shader=source.shader;material.CopyPropertiesFromMaterial(source);
                if(name!="Paving"&&name!="Kerb")material.SetColor("_BaseColor",alreadyTextured?tint:Color.Lerp(Color.white,tint,.5f));
                EditorUtility.SetDirty(material);
            }
            importer.isReadable=false;importer.textureCompression=TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="iPhone",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
            importer.SaveAndReimport();AssetDatabase.SaveAssets();
            var shader=Shader.Find("Harare/World Surface");
            if(ShaderUtil.ShaderHasError(shader))throw new System.Exception("Ground shader compilation failed");
            Debug.Log("[GroundSurface] Installed reference-led shared materials; atlas retained unmodified.");
        }
        static void Make(string name,Texture2DArray array,int layer,float metres,Color tint,float smooth)
        {
            string path=Root+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Harare/World Surface"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("Harare/World Surface");material.SetTexture("_Surfaces",array);material.SetFloat("_Slice",layer);
            material.SetFloat("_MetresPerTile",metres);material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",smooth);
            material.enableInstancing=true;EditorUtility.SetDirty(material);
        }
        static void Paint(string name,Color tint)
        {
            string path=Root+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",.05f);material.enableInstancing=true;EditorUtility.SetDirty(material);
        }
        static void InstallPhotographicAsphalt()
        {
            const string folder="Assets/Environment/Textures/PhotographicAsphalt/";
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Asphalt.mat");
            string[] maps={"diff","nor_gl","rough"};
            string[] properties={"_PhotoColor","_PhotoNormal","_PhotoRoughness"};
            for(int i=0;i<maps.Length;i++)
            {
                string path=folder+"asphalt_03_"+maps[i]+"_1k.jpg";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new System.Exception("Missing photographic asphalt map: "+path);
                importer.textureType=i==1?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=i==0;importer.mipmapEnabled=true;importer.isReadable=false;
                importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
                importer.anisoLevel=4;importer.maxTextureSize=1024;
                importer.textureCompression=TextureImporterCompression.Compressed;
                foreach(string platform in new[]{"iPhone","Android"})
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name=platform,overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
                importer.SaveAndReimport();
                material.SetTexture(properties[i],AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }
            material.SetFloat("_PhotoSurface",1);material.EnableKeyword("_PHOTO_SURFACE");
            material.SetFloat("_MetresPerTile",2.05f);material.SetFloat("_NormalStrength",.45f);
            material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
        }
    }
}
#endif
