#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarareAfterHours.EditorTools
{
    public static class CharacterRepairReview
    {
        public static void Before() => Capture("before");
        public static void After() => Capture("after");
        static void Capture(string stage)
        {
            Directory.CreateDirectory("/private/tmp/harare-character-repair");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .65f, .65f);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.1f;
            key.transform.rotation = Quaternion.Euler(30, 150, 0);
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .5f;
            fill.transform.rotation = Quaternion.Euler(20, -60, 0);
            var prefab = Resources.Load<GameObject>("HarareCharacters/Character01");
            var instance = UnityEngine.Object.Instantiate(prefab);
            var visual = instance.AddComponent<RuntimeCharacterVisual>();
            visual.Configure(new CharacterCastEntry { Id="review", DisplayName="Review", SkinTone="dark", HairStyle="fade", Outfit="charcoal", Accent="gold", Accessory="none", Wardrobe="street", Height=1, Build=1 }, true);
            var animator = instance.GetComponentInChildren<Animator>();
            if (stage == "after" && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.SetBool("Grounded", true); animator.Play("Locomotion", 0, .25f); animator.Update(.02f);
            }
            var bones = instance.GetComponentsInChildren<Transform>().Where(t => t.name == "head" || t.name == "foot_l" || t.name == "ball_l" || t.name == "pelvis");
            foreach (var bone in bones) Debug.Log("[CharacterReview] " + bone.name + " " + bone.position.ToString("F3") + " forward " + bone.forward.ToString("F3"));
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach(var material in renderer.sharedMaterials) Debug.Log("[CharacterReviewMaterial] " + material.name + " shader=" + material.shader.name + " texture=" + (material.mainTexture == null ? "NONE" : material.mainTexture.name));
            foreach (LODGroup group in instance.GetComponentsInChildren<LODGroup>()) group.ForceLOD(0);
            Camera camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.15f,.18f,.22f);
            camera.orthographic = true;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 20;
            Render(camera, new Vector3(0,1,4), new Vector3(0,.95f,0), 1.05f, stage + "-front");
            Render(camera, new Vector3(.35f,1.66f,2), new Vector3(0,1.64f,0), .28f, stage + "-face");
            Debug.Log("[CharacterReview] Captured " + stage);
        }
        public static void Render(Camera camera, Vector3 position, Vector3 target, float size, string name)
        {
            // Manual editor Animator.Update does not upload GPU skinning every time.
            // Bake the current bone pose for deterministic review captures, without changing assets.
            var skins = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            var bakedObjects = new System.Collections.Generic.List<GameObject>();
            var bakedMeshes = new System.Collections.Generic.List<Mesh>();
            var originalVisibility = skins.Select(s => s.forceRenderingOff).ToArray();
            foreach (var skin in skins)
            {
                var group = skin.GetComponentInParent<LODGroup>();
                bool isHigh = group == null || group.GetLODs()[0].renderers.Contains(skin);
                if (isHigh && !skin.forceRenderingOff)
                {
                    var mesh = new Mesh(); skin.BakeMesh(mesh);
                    var baked = new GameObject("Temporary posed review mesh");
                    baked.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);
                    baked.transform.localScale=skin.transform.lossyScale;
                    baked.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=baked.AddComponent<MeshRenderer>(); renderer.sharedMaterials=skin.sharedMaterials;
                    for(int i=0;i<skin.sharedMaterials.Length;i++)
                    {
                        var block=new MaterialPropertyBlock(); skin.GetPropertyBlock(block,i); renderer.SetPropertyBlock(block,i);
                    }
                    bakedObjects.Add(baked); bakedMeshes.Add(mesh);
                }
                skin.forceRenderingOff=true;
            }
            camera.transform.position = position; camera.transform.LookAt(target); camera.orthographicSize=size;
            var rt = new RenderTexture(800,1000,24);
            camera.targetTexture=rt; camera.Render(); camera.Render();
            RenderTexture.active=rt;
            var texture=new Texture2D(800,1000,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,800,1000),0,0); texture.Apply();
            File.WriteAllBytes("/private/tmp/harare-character-repair/"+name+".png",texture.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(texture); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            foreach(var baked in bakedObjects) UnityEngine.Object.DestroyImmediate(baked);
            foreach(var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
            for(int i=0;i<skins.Length;i++) skins[i].forceRenderingOff=originalVisibility[i];
        }
    }
}
#endif
