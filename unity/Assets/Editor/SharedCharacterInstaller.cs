#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    public static class SharedCharacterInstaller
    {
        const string Root = "Assets/Resources/HarareCharacters";
        const string Art = "Assets/Characters";
        const string AnimationSource = Art + "/AnimationSource/UAL1_Standard.fbx";
        static readonly string[] ClipNames = { "Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Sprint_Loop", "Jump_Start", "Jump_Loop", "Jump_Land" };

        [MenuItem("Harare After Hours/Install Repaired Shared Characters")]
        public static void Install()
        {
            Directory.CreateDirectory(Art + "/Materials");
            Directory.CreateDirectory(Art + "/Animations");
            AssetDatabase.Refresh();
            ConfigureAnimationSource();
            for (int i = 0; i < 3; i++) ConfigureBaseAvatar(Root + "/Character01_LOD" + i + ".fbx");
            var controller = CreateController();
            var prefab = new GameObject("Character01");
            try
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Character01_LOD0.fbx");
                var baseRig = UnityEngine.Object.Instantiate(model, prefab.transform);
                baseRig.name = "Shared humanoid rig";
                baseRig.transform.localPosition = Vector3.zero;
                baseRig.transform.localRotation = Quaternion.identity;
                baseRig.transform.localScale = Vector3.one;
                var animator = baseRig.GetComponent<Animator>();
                if (animator == null) throw new InvalidOperationException("Base model has no Animator.");
                if (animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
                    throw new InvalidOperationException("Base Humanoid Avatar invalid.");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                var bones = baseRig.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                var skin = baseRig.GetComponentInChildren<SkinnedMeshRenderer>();
                skin.sharedMaterials = skin.sharedMaterials.Select(MaterialFor).ToArray();
                skin.quality = SkinQuality.Bone4;
                // FBX renderer coordinates are not necessarily metres (armature scale).
                // Expand the imported bounds in their own space to include airborne limbs.
                var bounds = skin.localBounds;
                bounds.Expand(bounds.size.magnitude * .35f);
                Debug.Log("[SharedCharacters] Skin bounds " + bounds + " rootScale " + skin.transform.lossyScale);
                skin.localBounds = bounds;
                var lods = new LOD[3];
                lods[0] = new LOD(.28f, new Renderer[] { skin });
                for (int i = 1; i < 3; i++)
                {
                    var other = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Character01_LOD" + i + ".fbx"));
                    try
                    {
                        var source = other.GetComponentInChildren<SkinnedMeshRenderer>();
                        var child = new GameObject("Shared mesh LOD" + i);
                        child.transform.SetParent(skin.transform.parent, false);
                        child.transform.localPosition = skin.transform.localPosition;
                        child.transform.localRotation = skin.transform.localRotation;
                        child.transform.localScale = skin.transform.localScale;
                        var renderer = child.AddComponent<SkinnedMeshRenderer>();
                        renderer.sharedMesh = source.sharedMesh;
                        renderer.sharedMaterials = skin.sharedMaterials;
                        renderer.bones = source.bones.Select(b => bones[b.name]).ToArray();
                        renderer.rootBone = bones[source.rootBone.name];
                        renderer.localBounds = bounds;
                        renderer.quality = SkinQuality.Bone4;
                        lods[i] = new LOD(i == 1 ? .12f : .018f, new Renderer[] { renderer });
                    }
                    finally { UnityEngine.Object.DestroyImmediate(other); }
                }
                var group = prefab.AddComponent<LODGroup>();
                group.SetLODs(lods);
                group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(prefab, Root + "/Character01.prefab");
                CalibrateStride(prefab, controller);
            }
            finally { UnityEngine.Object.DestroyImmediate(prefab); }
            AssetDatabase.SaveAssets();
            HarareAfterHoursCharacterValidation.Validate();
            CharacterRepairReview.After();
            Debug.Log("[SharedCharacters] Installed fitted base, one skeleton, three LODs and seven authored locomotion clips.");
        }

        static void ConfigureAnimationSource()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(AnimationSource);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AnimationSource);
            var mapping = new Dictionary<string, string> {
                {"Hips","pelvis"}, {"Spine","spine_01"}, {"Chest","spine_02"}, {"UpperChest","spine_03"},
                {"Neck","neck_01"}, {"Head","Head"},
                {"LeftShoulder","clavicle_l"},{"RightShoulder","clavicle_r"},
                {"LeftUpperArm","upperarm_l"},{"RightUpperArm","upperarm_r"},
                {"LeftLowerArm","lowerarm_l"},{"RightLowerArm","lowerarm_r"},
                {"LeftHand","hand_l"},{"RightHand","hand_r"},
                {"LeftUpperLeg","thigh_l"},{"RightUpperLeg","thigh_r"},
                {"LeftLowerLeg","calf_l"},{"RightLowerLeg","calf_r"},
                {"LeftFoot","foot_l"},{"RightFoot","foot_r"},
                {"LeftToes","ball_l"},{"RightToes","ball_r"}
            };
            string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] names = { "thumb", "index", "middle", "ring", "pinky" };
            string[] segments = { "Proximal", "Intermediate", "Distal" };
            for (int side = 0; side < 2; side++)
                for (int f = 0; f < fingers.Length; f++)
                    for (int s = 0; s < 3; s++)
                        mapping[(side == 0 ? "Left" : "Right") + fingers[f] + segments[s]] =
                            names[f] + "_0" + (s + 1) + (side == 0 ? "_l" : "_r");
            var transforms = model.GetComponentsInChildren<Transform>();
            var desc = new HumanDescription {
                human = mapping.Where(kv => transforms.Any(t => t.name == kv.Value)).Select(kv =>
                    new HumanBone { humanName=HumanTrait.BoneName[(int)Enum.Parse(typeof(HumanBodyBones), kv.Key)], boneName=kv.Value, limit=new HumanLimit {useDefaultValues=true} }).ToArray(),
                skeleton = transforms.Select(t => new SkeletonBone { name=t.name, position=t.localPosition,
                    rotation=t.localRotation, scale=t.localScale }).ToArray(),
                upperArmTwist=.5f, lowerArmTwist=.5f, upperLegTwist=.5f, lowerLegTwist=.5f,
                armStretch=.05f, legStretch=.05f, feetSpacing=0, hasTranslationDoF=false
            };
            desc.skeleton = CalibratedTPose(model, desc.human);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription = desc;
            importer.importAnimation = true;
            var clips = importer.defaultClipAnimations.Where(c => ClipNames.Any(n => c.name == n || c.name.EndsWith("|" + n, StringComparison.Ordinal))).ToArray();
            if (clips.Length != ClipNames.Length) throw new InvalidOperationException("Expected seven source clips, got " + clips.Length + ": " + string.Join(",", importer.defaultClipAnimations.Select(c=>c.name)));
            foreach (var clip in clips)
            {
                clip.name = ClipNames.Single(n => clip.name == n || clip.name.EndsWith("|" + n, StringComparison.Ordinal));
                clip.loopTime = clip.name.EndsWith("_Loop", StringComparison.Ordinal);
                clip.loopPose = clip.loopTime;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                // The library's baked forward is -Z; game controllers face +Z.
                clip.rotationOffset = 180;
                clip.keepOriginalPositionY = false;
                clip.keepOriginalPositionXZ = true;
                clip.heightFromFeet = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(AnimationSource).OfType<Avatar>().First();
            if (!sourceAvatar.isValid || !sourceAvatar.isHuman) throw new InvalidOperationException("Animation source avatar invalid.");
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(AnimationSource).OfType<AnimationClip>())
            {
                if (!ClipNames.Contains(clip.name)) continue;
                string path = Art + "/Animations/" + clip.name + ".anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (existing != null) EditorUtility.CopySerialized(clip, existing);
                else AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(clip), path);
            }
        }

        static void ConfigureBaseAvatar(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var desc = importer.humanDescription;
            var mapping = desc.human.Where(b => HumanTrait.BoneName.Contains(b.humanName)).ToList();
            string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] names = { "thumb", "index", "middle", "ring", "pinky" };
            string[] segments = { "Proximal", "Intermediate", "Distal" };
            for (int side=0; side<2; side++) for(int f=0; f<5; f++) for(int s=0; s<3; s++)
            {
                string human = (side == 0 ? "Left" : "Right") + fingers[f] + segments[s];
                human = HumanTrait.BoneName[(int)Enum.Parse(typeof(HumanBodyBones), human)];
                if (mapping.Any(b=>b.humanName == human)) continue;
                mapping.Add(new HumanBone { humanName=human, boneName=names[f]+"_0"+(s+1)+(side==0?"_l":"_r"), limit=new HumanLimit {useDefaultValues=true} });
            }
            desc.human = mapping.ToArray();
            desc.skeleton = CalibratedTPose(model, desc.human);
            importer.humanDescription = desc;
            importer.SaveAndReimport();
        }

        static SkeletonBone[] CalibratedTPose(GameObject model, HumanBone[] mapping)
        {
            // Unity 6000.3's own Avatar Configure > Enforce T-Pose operation.
            // This is editor-only: the resulting calibrated Avatar is serialized into the FBX importer.
            var tool = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AvatarSetupTool", true);
            var wrapper = tool.GetNestedType("BoneWrapper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = model.name;
            try
            {
                tool.GetMethod("SampleBindPose").Invoke(null, new object[] {instance});
                var transforms = instance.GetComponentsInChildren<Transform>();
                var bones = Array.CreateInstance(wrapper, HumanTrait.BoneCount);
                for (int i=0; i<HumanTrait.BoneCount; i++)
                {
                    string human = HumanTrait.BoneName[i];
                    string name = mapping.FirstOrDefault(b=>b.humanName == human).boneName;
                    bones.SetValue(Activator.CreateInstance(wrapper, human, transforms.FirstOrDefault(t=>t.name == name)), i);
                }
                tool.GetMethod("MakePoseValid").Invoke(null, new object[] {bones});
                return transforms.Select(t=>new SkeletonBone {name=t.name, position=t.localPosition, rotation=t.localRotation, scale=t.localScale}).ToArray();
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        static void CalibrateStride(GameObject prefab, AnimatorController controller)
        {
            var tree = (BlendTree)controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Locomotion").state.motion;
            var children = tree.children;
            // The pack's sprint take has inconsistent in-place contact timing.
            // Use its forward run cycle at both jogging and running speeds so
            // phase-synchronised blending retains stable planted-foot motion.
            children[3].motion = Clip("Jog_Fwd_Loop");
            for(int i=0;i<children.Length;i++) children[i].timeScale=1;
            tree.children=children;
            var animator=prefab.GetComponentInChildren<Animator>();
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.SetBool("Grounded",true);
            for(int child=1;child<children.Length;child++)
            {
                float target=children[child].threshold;
                animator.SetFloat("Speed",target); animator.Play("Locomotion",0,0); animator.Update(0);
                var left=new List<Vector3>(); var right=new List<Vector3>();
                const float dt=1f/120;
                int count=Mathf.CeilToInt(((AnimationClip)children[child].motion).length*2/dt);
                for(int i=0;i<count;i++)
                {
                    animator.Update(dt);
                    left.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
                    right.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                }
                var speeds=new List<float>();
                foreach(var samples in new[]{left,right})
                {
                    float floor=samples.Min(p=>p.y);
                    for(int i=1;i<samples.Count;i++)
                    {
                        Vector3 velocity=(samples[i]-samples[i-1])/dt;
                        if(samples[i].y<floor+.055f && Mathf.Abs(velocity.y)<.6f && velocity.z<-.15f)
                            speeds.Add(-velocity.z);
                    }
                }
                if(speeds.Count<8) throw new InvalidOperationException("Insufficient planted-foot samples: "+children[child].motion.name);
                speeds.Sort(); float measured=speeds[speeds.Count/2];
                children[child].timeScale=Mathf.Clamp(target/measured,.5f,2.5f);
                Debug.Log($"[StrideCalibration] {children[child].motion.name}: planted-foot {measured:F3}m/s; target {target:F2}m/s; playback {children[child].timeScale:F3}");
            }
            tree.children=children;
            EditorUtility.SetDirty(tree);
        }

        static AnimatorController CreateController()
        {
            string path = Art + "/Animations/SharedLocomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var motion = machine.AddState("Locomotion");
            machine.defaultState = motion;
            var tree = new BlendTree { name="Idle Walk Jog Sprint", blendType=BlendTreeType.Simple1D,
                blendParameter="Speed", useAutomaticThresholds=false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Clip("Idle_Loop"), 0);
            tree.AddChild(Clip("Walk_Loop"), 1.8f);
            tree.AddChild(Clip("Jog_Fwd_Loop"), 3.3f);
            tree.AddChild(Clip("Sprint_Loop"), 5.5f);
            motion.motion = tree;
            motion.iKOnFeet = true;
            var rise = machine.AddState("Jump rise");
            rise.motion = Clip("Jump_Start"); rise.speed=2.2f;
            var fall = machine.AddState("Jump fall"); fall.motion=Clip("Jump_Loop");
            var land = machine.AddState("Land"); land.motion=Clip("Jump_Land"); land.speed=2;
            var takeoff = Transition(motion, rise, .07f);
            takeoff.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            takeoff.AddCondition(AnimatorConditionMode.Greater, .1f, "VerticalSpeed");
            var drop = Transition(motion, fall, .1f);
            drop.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            drop.AddCondition(AnimatorConditionMode.Less, .1f, "VerticalSpeed");
            Transition(rise, fall, .1f).AddCondition(AnimatorConditionMode.Less, 0, "VerticalSpeed");
            Transition(rise, land, .08f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            Transition(fall, land, .08f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            var recover = Transition(land, motion, .12f); recover.hasExitTime=true; recover.exitTime=.3f;
            var again = Transition(land, rise, .06f);
            again.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            again.AddCondition(AnimatorConditionMode.Greater, .1f, "VerticalSpeed");
            return controller;
        }
        static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "/Animations/" + name + ".anim");
        static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration)
        {
            var t = from.AddTransition(to); t.hasExitTime=false; t.hasFixedDuration=true; t.duration=duration;
            return t;
        }

        static Material MaterialFor(Material source)
        {
            string name = source.name;
            string path = Art + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = Color.white;
            string lower = name.ToLowerInvariant();
            string texture = lower.Contains("skin") ? "young_darkskinned_male_diffuse"
                : lower.Contains("casualsuit") ? "outfit_generated"
                : lower.Contains("shoes") ? "shoes05_diffuse"
                : lower.Contains("eyes") ? "brown_eye"
                : lower.Contains("coils") ? "cropped_coils"
                : lower.Contains("eyebrow") ? "eyebrow001" : null;
            if (texture != null)
            {
                string texturePath=Root + "/Textures/" + texture + ".png";
                var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                ti.mipmapEnabled=true; ti.maxTextureSize=texture.Contains("diffuse") || texture.Contains("outfit") ? 2048 : 1024;
                ti.textureCompression=TextureImporterCompression.Compressed; ti.SaveAndReimport();
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            mat.SetFloat("_Smoothness", lower.Contains("eyes") ? .4f : .22f);
            if (lower.Contains("coils")) mat.color = new Color(.28f,.28f,.28f,1);
            if (lower.Contains("cargo")) mat.color = new Color(.065f,.07f,.08f,1);
            if (lower.Contains("gold")) { mat.color=new Color(.65f,.43f,.16f); mat.SetFloat("_Metallic", .65f); }
            if (lower.Contains("eyebrow"))
            {
                mat.SetFloat("_AlphaClip",1); mat.SetFloat("_Cutoff",.35f); mat.SetFloat("_Cull",0);
                mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue=2450;
            }
            if (lower.Contains("casualsuit"))
            {
                string normalPath=Root + "/Textures/male_casualsuit05_normal.png";
                var ti=(TextureImporter)AssetImporter.GetAtPath(normalPath);
                ti.textureType=TextureImporterType.NormalMap; ti.mipmapEnabled=true; ti.maxTextureSize=1024; ti.SaveAndReimport();
                mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                mat.SetFloat("_BumpScale",.55f); mat.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
