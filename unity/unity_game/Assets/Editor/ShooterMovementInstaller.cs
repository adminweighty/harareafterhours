#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using UnityEditor.Animations;
namespace HarareAfterHours.EditorTools
{
    public static class ShooterMovementInstaller
    {
        public static void Inspect()
        {
            File.WriteAllLines("/private/tmp/harare-animation-clips.txt",AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/AnimationSource/UAL1_Standard.fbx").OfType<AnimationClip>().Select(c=>c.name));
        }
        public static void Install()
        {
            const string source="Assets/Characters/AnimationSource/UAL1_Standard.fbx",folder="Assets/Characters/Animations/";
            var importer=(ModelImporter)AssetImporter.GetAtPath(source);
            var clips=importer.clipAnimations.ToList();
            foreach(string name in new[]{"Crouch_Idle_Loop","Crouch_Fwd_Loop"})
            {
                if(clips.Any(c=>c.name==name))continue;
                var clip=importer.defaultClipAnimations.First(c=>c.name==name||c.name.EndsWith("|"+name));
                clip.name=name;clip.loopTime=true;clip.loopPose=true;clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
                clip.keepOriginalOrientation=true;clip.rotationOffset=180;clip.keepOriginalPositionY=false;clip.keepOriginalPositionXZ=true;clip.heightFromFeet=true;clips.Add(clip);
            }
            importer.clipAnimations=clips.ToArray();importer.SaveAndReimport();
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>c.name.StartsWith("Crouch_")))
            {
                var path=folder+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(existing==null)AssetDatabase.CreateAsset(Object.Instantiate(clip),path);else EditorUtility.CopySerialized(clip,existing);
            }
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"SharedLocomotion.controller");
            if(!controller.parameters.Any(p=>p.name=="Crouched"))controller.AddParameter("Crouched",AnimatorControllerParameterType.Bool);
            if(!controller.parameters.Any(p=>p.name=="GaitDirection")){controller.AddParameter("GaitDirection",AnimatorControllerParameterType.Float);var parameters=controller.parameters;parameters.First(p=>p.name=="GaitDirection").defaultFloat=1;controller.parameters=parameters;}
            var machine=controller.layers[0].stateMachine;
            if(!machine.states.Any(s=>s.state.name=="Crouch locomotion"))
            {
                var state=machine.AddState("Crouch locomotion");var tree=new BlendTree{name="Crouch idle and walk",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
                AssetDatabase.AddObjectToAsset(tree,controller);
                tree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Crouch_Idle_Loop.anim"),0);
                tree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Crouch_Fwd_Loop.anim"),1.25f);state.motion=tree;
                var locomotion=machine.states.First(s=>s.state.name=="Locomotion").state;
                var enter=locomotion.AddTransition(state);enter.hasExitTime=false;enter.duration=.14f;enter.AddCondition(AnimatorConditionMode.If,0,"Crouched");
                var leave=state.AddTransition(locomotion);leave.hasExitTime=false;leave.duration=.12f;leave.AddCondition(AnimatorConditionMode.IfNot,0,"Crouched");
            }
            foreach(var entry in machine.states.Where(s=>s.state.name is "Locomotion" or "Crouch locomotion")){entry.state.speedParameter="GaitDirection";entry.state.speedParameterActive=true;}
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();Debug.Log("SHOOTER ANIMATIONS INSTALLED");
        }
    }
}
#endif
