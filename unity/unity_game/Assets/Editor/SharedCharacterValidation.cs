#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    public static class SharedCharacterValidation
    {
        static readonly List<string> Report = new();
        public static void Validate()
        {
            Report.Clear();
            CharacterRepairReview.After();
            var visual = UnityEngine.Object.FindFirstObjectByType<RuntimeCharacterVisual>();
            var animator = visual.GetComponentInChildren<Animator>();
            var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            Require(visual.GetComponentsInChildren<Animator>().Length == 1, "Exactly one Animator per character");
            Require(skins.Length == 3, "Three shared-rig LOD meshes");
            Require(animator.avatar.isValid && animator.avatar.isHuman, "Valid calibrated Humanoid avatar");
            Require(!animator.applyRootMotion, "Controller owns world movement; animation has no root-motion drift");
            foreach (var skin in skins)
            {
                Require(skin.bones.All(b=>b != null && b.IsChildOf(animator.transform)), "All LOD bones belong to shared skeleton");
                Require(skin.sharedMaterials.All(m=>m != null && m.shader.name == "Universal Render Pipeline/Lit"), "All character materials use mobile URP shader");
                Require(skin.sharedMaterials.Where(m=>m.name.Contains("Skin") || m.name.Contains("Eyes") || m.name.Contains("Coils")).All(m=>m.mainTexture != null), "Skin, eyes and hair have diffuse textures");
            }
            Require(animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal) != null &&
                animator.GetBoneTransform(HumanBodyBones.RightThumbDistal) != null, "Finger rig mapped on both hands");
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach(var skin in skins) skin.updateWhenOffscreen = true;
            Sample(animator, camera, "idle", "Locomotion", 0, true, 0);
            float headY=animator.GetBoneTransform(HumanBodyBones.Head).position.y;
            Require(animator.GetBoneTransform(HumanBodyBones.LeftHand).position.y < headY-.35f, "Idle arms relaxed below shoulders");
            Require(animator.GetBoneTransform(HumanBodyBones.Head).forward.z > .5f, "Face points in controller's +Z direction");
            Sample(animator, camera, "walk", "Locomotion", 1.8f, true, 0);
            Sample(animator, camera, "run", "Locomotion", 5.5f, true, 0);
            Sample(animator, camera, "jump-rise", "Jump rise", 1.8f, false, 4);
            Sample(animator, camera, "jump-fall", "Jump fall", 1.8f, false, -2);
            Sample(animator, camera, "land", "Land", 0, true, 0);
            ValidateCast(skins[0].sharedMesh);
            File.WriteAllLines("/private/tmp/harare-character-repair/asset-validation.txt", Report);
            Debug.Log("[SharedCharacterValidation] PASS\n" + string.Join("\n", Report));
        }
        static void Sample(Animator animator, Camera camera, string name, string state, float speed, bool grounded, float vertical)
        {
            animator.SetFloat("Speed", speed); animator.SetBool("Grounded",grounded); animator.SetFloat("VerticalSpeed",vertical);
            animator.Play(state,0,0); animator.Update(0);
            float min=100, max=-100;
            for(int i=0;i<45;i++)
            {
                animator.Update(1f/60);
                var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                min=Mathf.Min(min,foot.z); max=Mathf.Max(max,foot.z);
                if (i==14 || i==30)
                    CharacterRepairReview.Render(camera,new Vector3(2.5f,1.3f,4),new Vector3(0,1,0),1.15f,name+"-"+i);
            }
            if (name=="walk" || name=="run") Require(max-min > .15f, name+" has authored forward/back foot motion");
            Require(animator.transform.localPosition.sqrMagnitude < .001f, name+" retains controller-owned origin");
            Report.Add("POSE: "+name+"; left-foot stride range "+(max-min).ToString("F3")+"m.");
        }
        static void ValidateCast(Mesh sharedMesh)
        {
            var root = new GameObject("Shared cast verification");
            try
            {
                var roster=root.AddComponent<CharacterRoster>(); roster.Initialize();
                var player=new GameObject("Player"); player.transform.SetParent(root.transform);
                var avatar=player.AddComponent<AvatarCustomization>(); roster.AttachPlayer(avatar);
                for(int i=0;i<roster.NpcCount;i++) roster.SpawnNpc(root.transform,i,new Vector3(i*2,0,0),1,.6f);
                var visuals=root.GetComponentsInChildren<RuntimeCharacterVisual>();
                Require(visuals.Length==24,"All 24 cast roles use the repaired base");
                foreach(var v in visuals)
                {
                    Require(v.transform.localScale==Vector3.one,"Cast has no distorted non-uniform body scaling");
                    Require(v.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh==sharedMesh,"Cast shares the exact same base mesh asset");
                    Require(v.GetComponentsInChildren<Animator>().Length==1,"Cast shares one Animator per instance");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void Require(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException(message);
            Report.Add("PASS: "+message);
        }
    }
}
#endif
