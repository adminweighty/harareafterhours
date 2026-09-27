#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace HarareAfterHours.EditorTools
{
    public static class ReferenceCityRealismInstaller
    {
        const string Root="Assets/Environment/ReferenceRealism/";
        static Material stone,metal,red,navy,wood;
        public static void Install()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/HarareEnvironment/HarareAfternoonSky.mat");
            if(sky==null){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,"Assets/Resources/HarareEnvironment/HarareAfternoonSky.mat");}
            sky.SetFloat("_SunDisk",1);sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",1);
            sky.SetColor("_SkyTint",new Color(.5f,.5f,.5f));sky.SetColor("_GroundColor",new Color(.36f,.32f,.26f));sky.SetFloat("_Exposure",1.05f);EditorUtility.SetDirty(sky);
            string[] maps={"diff","nor_gl","rough"};Texture2D[] textures=new Texture2D[3];
            for(int i=0;i<3;i++)
            {
                string path="Assets/Environment/Textures/PhotographicWall/concrete_wall_004_"+maps[i]+"_1k.jpg";
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=i==1?TextureImporterType.NormalMap:TextureImporterType.Default;
                imp.sRGBTexture=i==0;imp.maxTextureSize=1024;imp.mipmapEnabled=true;imp.isReadable=false;imp.wrapMode=TextureWrapMode.Repeat;imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=4;
                foreach(string platform in new[]{"iPhone","Android"})imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name=platform,overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});
                imp.SaveAndReimport();textures[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            foreach(string name in new[]{"Stone","StoneAlternate","Concrete","Plaster"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Materials/FirstStreet_"+name+".mat");if(material==null)continue;
                material.shader=Shader.Find("Harare/World Surface");material.EnableKeyword("_PHOTO_SURFACE");material.SetFloat("_PhotoSurface",1);
                material.SetTexture("_PhotoColor",textures[0]);material.SetTexture("_PhotoNormal",textures[1]);material.SetTexture("_PhotoRoughness",textures[2]);
                material.SetFloat("_NormalStrength",.22f);material.SetFloat("_MetresPerTile",2);
                material.SetColor("_BaseColor",name=="Stone"?new Color(1,.97f,.91f):Color.white);EditorUtility.SetDirty(material);
            }
            foreach(string name in new[]{"Glass","GlassAlternate"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Materials/FirstStreet_"+name+".mat");
                material.SetColor("_BaseColor",new Color(.12f,.18f,.20f));material.SetFloat("_Smoothness",.78f);material.SetFloat("_Metallic",.15f);EditorUtility.SetDirty(material);
            }
            var asphalt=GroundEnvironment.Surface("Asphalt");asphalt.SetColor("_BaseColor",new Color(.68f,.74f,.80f));EditorUtility.SetDirty(asphalt);
            stone=Mat("Warm stone",new Color(.61f,.58f,.51f));metal=Mat("Painted metal",new Color(.12f,.14f,.14f));
            red=Mat("Vendor cloth terracotta",new Color(.52f,.12f,.09f));navy=Mat("Vendor cloth navy",new Color(.07f,.14f,.23f));wood=Mat("Vendor wood",new Color(.27f,.17f,.10f));
            var stall=new GameObject("First Street vendor module");
            Box(stall.transform,new Vector3(0,1,0),new Vector3(1.7f,.09f,.85f),wood);
            foreach(float x in new[]{-.75f,.75f})foreach(float z in new[]{-.34f,.34f})Box(stall.transform,new Vector3(x,.52f,z),new Vector3(.06f,.96f,.06f),metal);
            foreach(float x in new[]{-.85f,.85f})Box(stall.transform,new Vector3(x,1.18f,.37f),new Vector3(.04f,2.35f,.04f),metal);
            var canopy=Box(stall.transform,new Vector3(0,2.28f,0),new Vector3(1.9f,.04f,1.15f),navy);canopy.transform.localRotation=Quaternion.Euler(7,0,0);
            Box(stall.transform,new Vector3(0,2.19f,-.57f),new Vector3(1.9f,.18f,.025f),navy);
            for(int i=0;i<6;i++)Box(stall.transform,new Vector3(-.66f+i*.26f,1.10f,-.05f),new Vector3(.22f,.12f,.45f),i%2==0?red:stone);
            Combine(stall,"Vendor");var lod=stall.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.035f,stall.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            var collision=stall.AddComponent<BoxCollider>();collision.center=new Vector3(0,.55f,0);collision.size=new Vector3(1.7f,.9f,.85f);
            var module=PrefabUtility.SaveAsPrefabAsset(stall,Root+"FirstStreetVendor.prefab");Object.DestroyImmediate(stall);
            var treeModule=JacarandaMobileInstaller.Install();
            string prefab="Assets/Resources/HarareEnvironment/FirstStreetCorner.prefab";var block=PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var previous=block.transform.Find("Reference street detail pass");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                var details=new GameObject("Reference street detail pass");details.transform.SetParent(block.transform,false);
                foreach(float z in new[]{9.5f,12.2f})
                {var vendor=(GameObject)PrefabUtility.InstantiatePrefab(module);vendor.transform.SetParent(details.transform,false);vendor.transform.localPosition=new Vector3(9.35f,.18f,z)-FirstStreetBlock.PlotCentre;}
                // Narrow drain slots follow the kerb, away from crossings and navigation.
                foreach(float z in new[]{10f,23f})
                {Vector3 p=new Vector3(6.48f,.195f,z)-FirstStreetBlock.PlotCentre;for(int i=0;i<6;i++)Box(details.transform,p+Vector3.forward*(i*.09f),new Vector3(.28f,.01f,.035f),metal);}
                var source=block.transform.Find("FirstStreet_ShadeTree");
                if(source!=null)source.gameObject.SetActive(false); // Preserve prototype asset for rollback.
                foreach(Vector3 p in new[]{new Vector3(-8.65f,0,10),new Vector3(-8.65f,0,26),new Vector3(25,0,8.5f)})
                {var tree=(GameObject)PrefabUtility.InstantiatePrefab(treeModule);tree.transform.SetParent(details.transform,false);tree.transform.localPosition=p-FirstStreetBlock.PlotCentre;}
                // Preserve the original named reference building and all collisions.
                PrefabUtility.SaveAsPrefabAsset(block,prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(block);}
            AssetDatabase.SaveAssets();Debug.Log("[ReferenceRealism] First Street materials, vendor module, drains, shade trees and sky installed.");
        }
        static Material Mat(string name,Color color)
        {
            string path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.18f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Box(Transform parent,Vector3 p,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;}
        static void Combine(GameObject root,string prefix)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>();
            foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
            {
                string path=Root+prefix+"_"+group.Key.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
                var go=new GameObject(group.Key.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;EditorUtility.SetDirty(mesh);
            }
            foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
        }
    }
}
#endif
