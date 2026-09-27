#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarareAfterHours.EditorTools
{
    public static class SharedVehicleInstaller
    {
        const string Root="Assets/Resources/HarareVehicles";
        const string Models=Root+"/Models/SharedSUV";
        public static void Install()
        {
            Directory.CreateDirectory(Root+"/Materials/SharedSUV");
            AssetDatabase.Refresh();
            for(int i=0;i<3;i++)
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Models+"/SharedSUV_LOD"+i+".fbx");
                importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
                importer.globalScale=1;importer.useFileUnits=true;importer.meshCompression=ModelImporterMeshCompression.Off;
                importer.preserveHierarchy=true;importer.SaveAndReimport();
            }
            var root=new GameObject("SharedSUV");
            try
            {
                var lods=new LOD[3];
                string[] names={"Wheel_FL","Wheel_FR","Wheel_RL","Wheel_RR"};
                var pivots=new Transform[4];
                var colliders=new WheelCollider[4];
                for(int lod=0;lod<3;lod++)
                {
                    var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Models+"/SharedSUV_LOD"+lod+".fbx"),root.transform);
                    model.name="LOD"+lod;
                    var renderers=model.GetComponentsInChildren<MeshRenderer>();
                    foreach(var renderer in renderers)
                    {
                        renderer.sharedMaterials=renderer.sharedMaterials.Select(MaterialFor).ToArray();
                        if(renderer.name.StartsWith("Glass"))renderer.shadowCastingMode=ShadowCastingMode.Off;
                    }
                    for(int i=0;i<4;i++)
                    {
                        var mesh=renderers.Single(r=>r.name.StartsWith(names[i]));
                        if(lod==0)
                        {
                            pivots[i]=new GameObject(names[i]+"_Pivot").transform;
                            pivots[i].SetParent(root.transform,false);
                            // Exported origins are the measured tyre centres, not
                            // asymmetric mesh bounds (which include brake detail).
                            pivots[i].position=mesh.transform.position;
                        }
                        mesh.transform.SetParent(pivots[i],true);
                    }
                    lods[lod]=new LOD(lod==0?.26f:lod==1?.105f:.018f,renderers);
                }
                for(int i=0;i<4;i++)
                {
                    var child=new GameObject("Suspension_"+names[i]);child.transform.SetParent(root.transform,false);
                    child.transform.localPosition=pivots[i].localPosition+Vector3.up*.12f;
                    var wheel=child.AddComponent<WheelCollider>();wheel.radius=.387f;wheel.mass=28;wheel.suspensionDistance=.24f;
                    var spring=wheel.suspensionSpring;spring.spring=42000;spring.damper=5500;spring.targetPosition=.5f;wheel.suspensionSpring=spring;
                    var forward=wheel.forwardFriction;forward.stiffness=1.35f;wheel.forwardFriction=forward;
                    var sideways=wheel.sidewaysFriction;sideways.stiffness=1.65f;wheel.sidewaysFriction=sideways;
                    wheel.ConfigureVehicleSubsteps(5,12,15);
                    colliders[i]=wheel;
                }
                var bodyCollider=root.AddComponent<BoxCollider>();bodyCollider.center=new Vector3(0,1.04f,0);bodyCollider.size=new Vector3(1.97f,1.27f,5.06f);
                var body=root.AddComponent<Rigidbody>();body.mass=1950;body.interpolation=RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearDamping=.08f;body.angularDamping=.7f;
                // Match the supplied source cockpit's steering-wheel side.
                var driver=Anchor(root.transform,"Driver seat",new Vector3(-.47f,.72f,.30f));
                var passenger=Anchor(root.transform,"Passenger seat",new Vector3(.47f,.72f,.30f));
                Anchor(root.transform,"Steering grip left",new Vector3(-.62f,1.07f,.83f));
                Anchor(root.transform,"Steering grip right",new Vector3(-.32f,1.07f,.83f));
                var controller=root.AddComponent<VehicleController>();controller.ConfigureRig(colliders,pivots,driver,passenger);controller.CreateCameraAnchor();
                root.AddComponent<VehicleAppearance>();
                var lodGroup=root.AddComponent<LODGroup>();lodGroup.SetLODs(lods);lodGroup.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/SharedSUV.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            InstallSeatedAnimations();
            AssetDatabase.SaveAssets();
            Review();
            Debug.Log("[SharedVehicles] Shared SUV, separate material regions, four wheel rig and seated animations installed.");
        }
        static Transform Anchor(Transform root,string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root,false);t.localPosition=p;return t;}
        static Material MaterialFor(Material source)
        {
            string name=source.name;string path=Root+"/Materials/SharedSUV/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.name=name;material.enableInstancing=true;
            Color color=new Color(.08f,.09f,.10f);float metallic=0,smooth=.35f;
            if(name.Contains("Paint")){color=Color.white;metallic=.05f;smooth=.48f;}
            else if(name.Contains("WheelMetal")){color=new Color(.4f,.43f,.46f);metallic=.8f;smooth=.55f;}
            else if(name.Contains("Metal")){color=new Color(.18f,.20f,.22f);metallic=.7f;smooth=.5f;}
            else if(name.Contains("Tyre")){color=new Color(.022f,.025f,.029f);smooth=.15f;}
            else if(name.Contains("Interior")){color=new Color(.075f,.065f,.05f);smooth=.22f;}
            else if(name.Contains("HeadLight")){color=new Color(.7f,.78f,.84f);smooth=.8f;}
            else if(name.Contains("TailLight")){color=new Color(.5f,.008f,.012f);smooth=.65f;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.2f,.002f,.002f));}
            else if(name.Contains("Glass"))
            {
                color=new Color(.09f,.12f,.13f,.13f);smooth=.60f;
                material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");material.SetFloat("_EnvironmentReflections",0);
            }
            material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smooth);
            EditorUtility.SetDirty(material);return material;
        }
        static void InstallSeatedAnimations()
        {
            const string source="Assets/Characters/AnimationSource/UAL1_Standard.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(source);
            var clips=importer.clipAnimations.ToList();
            foreach(string name in new[]{"Driving_Loop","Sitting_Idle_Loop"})
            {
                if(clips.Any(c=>c.name==name))continue;
                var clip=importer.defaultClipAnimations.Single(c=>c.name.EndsWith("|"+name,StringComparison.Ordinal));
                clip.name=name;clip.loopTime=true;clip.loopPose=true;clip.lockRootRotation=true;clip.rotationOffset=180;
                clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;
                clips.Add(clip);
            }
            importer.clipAnimations=clips.ToArray();importer.SaveAndReimport();
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>c.name=="Driving_Loop"||c.name=="Sitting_Idle_Loop"))
            {
                string path="Assets/Characters/Animations/"+clip.name+".anim";
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(existing==null)AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(clip),path);else EditorUtility.CopySerialized(clip,existing);
            }
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/Animations/SharedLocomotion.controller");
            if(!controller.parameters.Any(p=>p.name=="Seated"))controller.AddParameter("Seated",AnimatorControllerParameterType.Bool);
            if(!controller.parameters.Any(p=>p.name=="DrivingSeat"))controller.AddParameter("DrivingSeat",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;
            var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
            var locomotion=machine.states.Single(s=>s.state.name=="Locomotion").state;
            foreach(string name in new[]{"Driving","Passenger"})
            {
                if(machine.states.Any(s=>s.state.name==name))continue;
                var state=machine.AddState(name);
                state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Characters/Animations/"+(name=="Driving"?"Driving_Loop":"Sitting_Idle_Loop")+".anim");
                var enter=machine.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=.12f;enter.canTransitionToSelf=false;
                enter.AddCondition(AnimatorConditionMode.If,0,"Seated");enter.AddCondition(name=="Driving"?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"DrivingSeat");
                var exit=state.AddTransition(locomotion);exit.hasExitTime=false;exit.duration=.12f;exit.AddCondition(AnimatorConditionMode.IfNot,0,"Seated");
            }
            EditorUtility.SetDirty(controller);
        }
        public static void Review()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
            var light=new GameObject("Studio key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,-35,0);
            var car=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("HarareVehicles/Prefabs/SharedSUV"));
            car.GetComponent<LODGroup>().ForceLOD(0);
            car.GetComponent<VehicleAppearance>().SetAppearance(new Color(.2f,.36f,.31f));
            var camera=new GameObject("Vehicle review").AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.18f);
            Render(camera,new Vector3(6,3.3f,7),new Vector3(0,.9f,0),3.3f,"suv-exterior");
            for(int lod=1;lod<3;lod++)
            {
                car.GetComponent<LODGroup>().ForceLOD(lod);
                Render(camera,new Vector3(6,3.3f,7),new Vector3(0,.9f,0),3.3f,"suv-lod"+lod);
            }
            car.GetComponent<LODGroup>().ForceLOD(0);
            var actor=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("HarareCharacters/Character01"));
            actor.transform.SetParent(car.GetComponent<VehicleController>().DriverSeat,false);
            var visual=actor.AddComponent<RuntimeCharacterVisual>();
            visual.Configure(new CharacterCastEntry{DisplayName="Driver",SkinTone="dark",Outfit="charcoal",Accent="gold"},true);
            visual.SetSeated(true,true);
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
            actor.GetComponent<LODGroup>().ForceLOD(0);
            Render(camera,new Vector3(3,1.8f,3),new Vector3(.3f,1.05f,.3f),1.4f,"suv-driver");
            Debug.Log("[VehicleReview] Seats "+car.GetComponent<VehicleController>().DriverSeat.position+" wheels "+string.Join(",",car.GetComponent<VehicleController>().Wheels.Select(w=>w.transform.localPosition.ToString())));
        }
        static void Render(Camera camera,Vector3 p,Vector3 target,float size,string name)
        {
            var skins=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            var objects=new System.Collections.Generic.List<GameObject>();
            var meshes=new System.Collections.Generic.List<Mesh>();
            foreach(var skin in skins)
            {
                var group=skin.GetComponentInParent<LODGroup>();
                if(group==null || group.GetLODs()[0].renderers.Contains(skin))
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
                    var obj=new GameObject("Review posed driver");objects.Add(obj);
                    obj.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);obj.transform.localScale=skin.transform.lossyScale;
                    obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                }
                skin.forceRenderingOff=true;
            }
            camera.transform.position=p;camera.transform.LookAt(target);camera.orthographicSize=size;
            var rt=new RenderTexture(1200,800,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1200,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1200,800),0,0);texture.Apply();
            File.WriteAllBytes("/private/tmp/harare-vehicle-repair/"+name+".png",texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            foreach(var obj in objects)UnityEngine.Object.DestroyImmediate(obj);
            foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
            foreach(var skin in skins)skin.forceRenderingOff=false;
        }
    }
}
#endif
