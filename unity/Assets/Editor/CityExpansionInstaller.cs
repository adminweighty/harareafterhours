#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace HarareAfterHours.EditorTools
{
    public static class CityExpansionInstaller
    {
        const string AssetsRoot="Assets/Environment/CityExpansion/",ResourcesRoot="Assets/Resources/HarareEnvironment/CityExpansion/";
        static Material wall,glass,frame,roof,trim,textMaterial;
        public static void Install()
        {
            Directory.CreateDirectory(AssetsRoot);Directory.CreateDirectory(ResourcesRoot);AssetDatabase.Refresh();
            wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Materials/FirstStreet_Concrete.mat");
            if(wall==null)throw new System.InvalidOperationException("Missing existing photographic concrete material");
            glass=Mat("Avenue blue grey glass",new Color(.12f,.20f,.24f),.72f,.15f);
            frame=Mat("Ivory facade frames",new Color(.72f,.70f,.64f),.22f);
            roof=Mat("Weathered roof charcoal",new Color(.15f,.17f,.18f),.2f);
            trim=Mat("Warm sandstone trim",new Color(.59f,.47f,.30f),.25f);
            var text=new GameObject("font source").AddComponent<TextMesh>();
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"Depth tested lettering.mat");
            if(textMaterial==null){textMaterial=new Material(Resources.Load<Shader>("HarareWorldText"));AssetDatabase.CreateAsset(textMaterial,AssetsRoot+"Depth tested lettering.mat");}
            textMaterial.mainTexture=text.font.material.mainTexture;EditorUtility.SetDirty(textMaterial);Object.DestroyImmediate(text.gameObject);
            Office("AvenueOffice",8,false);Office("CBDShopBlock",4,false);Office("GardenOffice",2,true);Avondale();Palm();
            var tree=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/ReferenceRealism/JacarandaMobile.prefab");
            var copy=(GameObject)PrefabUtility.InstantiatePrefab(tree);PrefabUtility.SaveAsPrefabAsset(copy,ResourcesRoot+"AvenueJacaranda.prefab");Object.DestroyImmediate(copy);
            Sky();Graphics();AssetDatabase.SaveAssets();
            Debug.Log("[CityExpansion] Three reference-led building modules, Avondale entrance, shared tree and two graphics presets installed.");
        }
        static Material Mat(string name,Color c,float smooth,float metallic=0)
        {
            string path=AssetsRoot+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;
            Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=material;return o;
        }
        static void Office(string name,int floors,bool garden)
        {
            var root=new GameObject(name);float w=26,d=18,h=3.3f*floors+3.8f;
            var lods=new LOD[3];
            for(int tier=0;tier<3;tier++)
            {
                var level=new GameObject("LOD"+tier);level.transform.SetParent(root.transform,false);
                Box(level.transform,"Stone building envelope",new Vector3(0,h/2,0),new Vector3(w,h,d),wall);
                Box(level.transform,"Roof parapet",new Vector3(0,h+.25f,0),new Vector3(w+.3f,.5f,d+.3f),roof);
                foreach(int side in new[]{-1,1})
                {
                    Box(level.transform,"Ground-floor glazing",new Vector3(0,1.6f,side*(d/2+.06f)),new Vector3(w-.7f,3.1f,.08f),glass);
                    Box(level.transform,"Shop canopy",new Vector3(0,3.5f,side*(d/2+.45f)),new Vector3(w+.4f,.3f,1.3f),frame);
                    for(int floor=0;floor<floors;floor++)
                    {
                        float y=4.6f+floor*3.3f;
                        Box(level.transform,"Ribbon windows",new Vector3(0,y,side*(d/2+.035f)),new Vector3(w-1,2.3f,.06f),glass);
                        if(tier<2)Box(level.transform,"Concrete spandrel",new Vector3(0,y+1.35f,side*(d/2+.12f)),new Vector3(w+.1f,.28f,.3f),frame);
                    }
                    if(tier==0)for(int bay=-3;bay<=3;bay++)
                    {Box(level.transform,"Window mullion",new Vector3(bay*3.6f,h/2,side*(d/2+.13f)),new Vector3(.12f,h-.8f,.14f),frame);}
                    Box(level.transform,"Side glazing",new Vector3(side*(w/2+.03f),h/2,0),new Vector3(.05f,h-5,d-1.2f),glass);
                    if(tier<2)for(int floor=1;floor<=floors;floor++)Box(level.transform,"Side floor band",new Vector3(side*(w/2+.10f),floor*3.3f+.5f,0),new Vector3(.2f,.35f,d),frame);
                }
                if(garden)foreach(float x in new[]{-11f,-5.5f,5.5f,11f})Box(level.transform,"Garden arcade column",new Vector3(x,1.8f,10.5f),new Vector3(.35f,3.6f,.35f),trim);
                if(tier==0)foreach(float x in new[]{-9f,0,9f})Box(level.transform,"Rooftop service housing",new Vector3(x,h+.8f,0),new Vector3(3,1.1f,4),roof);
                Combine(level,name+"_LOD"+tier);lods[tier]=new LOD(new[]{.10f,.035f,.008f}[tier],level.GetComponentsInChildren<Renderer>());
            }
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,h/2,0);collider.size=new Vector3(w,h,d);
            var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,ResourcesRoot+name+".prefab");Object.DestroyImmediate(root);
        }
        static void Avondale()
        {
            var root=new GameObject("Avondale reference entrance");var visual=new GameObject("Architecture");visual.transform.SetParent(root.transform,false);
            foreach(int side in new[]{-1,1})
            {
                Box(visual.transform,"Shopping wing",new Vector3(side*27,3.4f,0),new Vector3(36,6.8f,18),wall);
                var roofPart=Box(visual.transform,"Pitched metal roof",new Vector3(side*27,7.4f,0),new Vector3(37,.7f,19),roof);roofPart.transform.localRotation=Quaternion.Euler(side*7,0,0);
                Box(visual.transform,"Shop display glazing",new Vector3(side*27,2.3f,9.06f),new Vector3(35,4.4f,.08f),glass);
                Box(visual.transform,"Arcade canopy",new Vector3(side*27,4.8f,10),new Vector3(36,.25f,3),frame);
                for(int bay=0;bay<6;bay++)Box(visual.transform,"Arcade column",new Vector3(side*(11+bay*6),2.4f,11.2f),new Vector3(.35f,4.8f,.35f),frame);
                Box(visual.transform,"Entrance tower",new Vector3(side*10,5.5f,2),new Vector3(3.8f,11,16),wall);
                Box(visual.transform,"Tower cap",new Vector3(side*10,11.2f,2),new Vector3(4.4f,.45f,16.6f),roof);
                Box(visual.transform,"Arch pier",new Vector3(side*7.1f,3,10),new Vector3(1.3f,6,1.4f),frame);
            }
            Box(visual.transform,"Central entrance glazing",new Vector3(0,3,8.7f),new Vector3(13,6,.1f),glass);
            for(int door=-2;door<=2;door++)Box(visual.transform,"Entrance door frame",new Vector3(door*2.5f,3,8.85f),new Vector3(.10f,6,.15f),trim);
            var fan=new GameObject("Cream arch infill");fan.transform.SetParent(visual.transform,false);
            var fanMesh=new Mesh();var vertices=new Vector3[26];var triangles=new int[72];vertices[0]=new Vector3(0,6,9.3f);
            for(int i=0;i<=24;i++){float a=i*Mathf.PI/24;vertices[i+1]=new Vector3(Mathf.Cos(a)*6.6f,6+Mathf.Sin(a)*6.6f,9.3f);if(i<24){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}}
            fanMesh.vertices=vertices;fanMesh.triangles=triangles;fanMesh.RecalculateNormals();fanMesh.RecalculateBounds();fan.AddComponent<MeshFilter>().sharedMesh=fanMesh;fan.AddComponent<MeshRenderer>().sharedMaterial=frame;
            // A segmented stone arch: visible opening and silhouette follow photo 17.
            for(int segment=0;segment<24;segment++)
            {
                float angle=(segment+.5f)*Mathf.PI/24;
                var stone=Box(visual.transform,"Arch voussoir",new Vector3(Mathf.Cos(angle)*7.1f,6+Mathf.Sin(angle)*7.1f,10),new Vector3(.96f,1.3f,1.4f),frame);
                stone.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
            }
            Combine(visual,"Avondale");
            Object.DestroyImmediate(fanMesh);
            Label(root.transform,"AVONDALE",new Vector3(0,8.7f,10.8f),.20f);
            Label(root.transform,"SHOPPING CENTRE",new Vector3(0,7.65f,10.8f),.1f);
            Label(root.transform,"CAFÉ",new Vector3(-26,5.6f,9.15f),.13f);
            Label(root.transform,"GRILL & TAKEAWAY",new Vector3(26,5.6f,9.15f),.09f);
            foreach(int side in new[]{-1,1}){var c=root.AddComponent<BoxCollider>();c.center=new Vector3(side*27,3.4f,0);c.size=new Vector3(36,6.8f,18);}
            var centre=root.AddComponent<BoxCollider>();centre.center=new Vector3(0,4.5f,0);centre.size=new Vector3(18,9,18);
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.015f,root.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,ResourcesRoot+"AvondaleEntrance.prefab");Object.DestroyImmediate(root);
        }
        static void Palm()
        {
            var root=new GameObject("Avondale palm module");var leaves=Mat("Palm fronds",new Color(.16f,.26f,.09f),.15f);leaves.SetFloat("_Cull",0);
            var bark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/ReferenceRealism/Jacaranda bark.mat");
            var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.transform.SetParent(root.transform,false);trunk.transform.localPosition=new Vector3(0,3.7f,0);trunk.transform.localScale=new Vector3(.25f,3.7f,.25f);Object.DestroyImmediate(trunk.GetComponent<Collider>());trunk.GetComponent<Renderer>().sharedMaterial=bark;
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            for(int frond=0;frond<10;frond++)
            {
                float a=frond*Mathf.PI*2/10;Vector3 direction=new(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 side=Vector3.Cross(Vector3.up,direction);
                for(int leaflet=0;leaflet<15;leaflet++)
                {
                    float t=leaflet/15f;Vector3 centre=Vector3.up*(7.4f+Mathf.Sin(t*Mathf.PI)*1.1f-t*1.3f)+direction*(t*3.3f);
                    float width=.65f*(1-t)+.08f;
                    foreach(int sign in new[]{-1,1})
                    {
                        int start=vertices.Count;vertices.Add(centre);vertices.Add(centre+side*sign*width-direction*.28f-Vector3.up*.16f);vertices.Add(centre+direction*.18f);
                        triangles.AddRange(new[]{start,start+1,start+2});
                    }
                }
            }
            string path=AssetsRoot+"Palm frond mesh.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var canopy=new GameObject("Palm canopy");canopy.transform.SetParent(root.transform,false);canopy.AddComponent<MeshFilter>().sharedMesh=mesh;canopy.AddComponent<MeshRenderer>().sharedMaterial=leaves;
            var collider=root.AddComponent<CapsuleCollider>();collider.radius=.14f;collider.height=7.4f;collider.center=Vector3.up*3.7f;
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.018f,root.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root,ResourcesRoot+"AvondalePalm.prefab");Object.DestroyImmediate(root);EditorUtility.SetDirty(leaves);
        }
        static void Label(Transform parent,string value,Vector3 p,float size)
        {
            var o=new GameObject(value);o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localRotation=Quaternion.Euler(0,180,0);
            var t=o.AddComponent<TextMesh>();t.text=value;t.anchor=TextAnchor.MiddleCenter;t.fontSize=64;t.characterSize=size;t.color=new Color(.16f,.19f,.19f);o.GetComponent<Renderer>().sharedMaterial=textMaterial;
        }
        static void Combine(GameObject root,string prefix)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>();
            foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
            {
                string path=AssetsRoot+prefix+"_"+group.Key.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
                var o=new GameObject(group.Key.name);o.transform.SetParent(root.transform,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=group.Key;EditorUtility.SetDirty(mesh);
            }
            foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
        }
        static void Graphics()
        {
            foreach(string name in new[]{"Balanced","High"})
            {
                string path=ResourcesRoot+name+".asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if(pipeline==null){pipeline=Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset"));AssetDatabase.CreateAsset(pipeline,path);}
                // Flutter's embedded native target is single-sampled; use camera FXAA instead.
                pipeline.renderScale=name=="High"?1:.85f;pipeline.msaaSampleCount=1;
                pipeline.shadowDistance=name=="High"?90:55;pipeline.shadowCascadeCount=2;pipeline.mainLightShadowmapResolution=2048;
                var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline.supportsHDR=true;EditorUtility.SetDirty(pipeline);
            }
            string profilePath=ResourcesRoot+"CityGrade.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var colour)){colour=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(colour,profile);}colour.postExposure.Override(.25f);colour.contrast.Override(5);colour.saturation.Override(-3);
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.12f);bloom.threshold.Override(1.3f);bloom.highQualityFiltering.Override(false);
            foreach(var c in profile.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(profile);
        }
        static void Sky()
        {
            const string path="Assets/Environment/Textures/PhotographicSky/kloofendal_48d_partly_cloudy_puresky_2k.hdr";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureShape=TextureImporterShape.TextureCube;importer.generateCubemap=TextureImporterGenerateCubemap.AutoCubemap;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.cubemapConvolution=TextureImporterCubemapConvolution.Specular;settings.seamlessCubemap=true;importer.SetTextureSettings(settings);
            importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.isReadable=false;importer.maxTextureSize=512;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
            string materialPath=ResourcesRoot+"PhotographicSky.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Skybox/Cubemap"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_Tex",AssetDatabase.LoadAssetAtPath<Cubemap>(path));material.SetFloat("_Exposure",.8f);material.SetFloat("_Rotation",0);EditorUtility.SetDirty(material);
        }
    }
}
#endif
