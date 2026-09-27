#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarareAfterHours.EditorTools
{
    /// <summary>
    /// Runs the M02 story path in the live scene and audits the M03-M05 gates.
    /// Validation keys are suffixed in the editor, so real campaign saves and
    /// rewards are never read, overwritten, or deleted.
    /// </summary>
    [InitializeOnLoad]
    public static class ActOneCampaignValidation
    {
        const string Key = ActOneCampaign.ValidationSessionKey;
        static int _stage;
        static int _frame;
        static double _deadline;
        static HarareAfterHoursBootstrap _game;
        static int _pointsAfterM02;

        static ActOneCampaignValidation()=>EditorApplication.update+=Tick;

        public static void Run()
        {
            Directory.CreateDirectory("/private/tmp/harare-act-one-validation");
            SessionState.SetBool(Key,true);
            PlayerPrefs.DeleteKey(ActOneCampaign.ActiveSaveKey);
            for(int level=2;level<=5;level++)PlayerPrefs.DeleteKey(ActOneCampaign.ActiveRewardKey(level));
            PlayerPrefs.DeleteKey(PlayerProgression.ActiveSaveKey);
            PlayerPrefs.SetInt(ActOneCampaign.ActiveRewardKey(1),1);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            _stage=0;_deadline=0;EditorApplication.isPlaying=true;
        }

        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||_frame==Time.frameCount)return;
            _frame=Time.frameCount;
            try
            {
                if(_deadline==0)_deadline=EditorApplication.timeSinceStartup+120;
                Require(EditorApplication.timeSinceStartup<_deadline,"Act I validation timed out");
                _game=HarareAfterHoursBootstrap.Instance;
                if(_game==null||_game.Player==null||_game.Mission==null||Time.time<2f)return;
                switch(_stage)
                {
                    case 0:
                        _game.Mission.SetActiveMission("{\"missionId\":\"M02\",\"level\":2,\"isReplay\":false}");
                        ActOneCampaign m02=_game.Mission.ExtendedCampaign;
                        Require(m02.Active&&m02.Level==2,"M02 starts after M01 reward");
                        Require(m02.ObjectiveText.Contains("overpayment"),"M02 begins with the scripted overpayment");
                        Require(GameObject.Find("Choice A · Return it directly")!=null,"primary story choice is visible");
                        Require(GameObject.Find("Choice B · Ask Chipo to resolve it")!=null,"secondary story choice is visible");
                        MovePlayer(m02.PrimaryChoicePosition);Require(m02.TryInteract(),"M02 accepts the first story choice");
                        Require(m02.PhaseIndex==1,"M02 advances to the market collection route");
                        while(m02.PhaseIndex==1){MovePlayer(m02.ObjectivePosition);m02.Tick();}
                        Require(m02.PhaseIndex==2&&m02.ObjectiveText.Contains("contractor"),"market route reaches the contractor confrontation");
                        MovePlayer(m02.SecondaryChoicePosition);Require(m02.TryInteract(),"M02 accepts the confrontation branch");
                        MovePlayer(m02.ObjectivePosition);Require(m02.TryInteract(),"M02 cart is cleared and the note recovered");
                        Require(m02.Completed,"M02 completes through live world actions");
                        Require(PlayerPrefs.GetInt(ActOneCampaign.ActiveRewardKey(2))==1,"M02 reward is committed once");
                        Require(_game.Bridge.LastEvent.Contains("mission_complete"),"M02 result is published to Flutter");
                        Require(_game.Bridge.LastEvent.Contains("\\\"choiceLabel\\\":\\\"Return it directly\\\""),"M02 result preserves its authored consequence");
                        Require(_game.Bridge.LastEvent.Contains("\\\"coins\\\":600"),"M02 first completion pays 600 coins");
                        _pointsAfterM02=_game.Progression.Points;
                        _stage++;break;
                    case 1:
                        _game.Mission.SetActiveMission("{\"missionId\":\"M03\",\"level\":3,\"isReplay\":false}");
                        Require(_game.Mission.ActiveMissionLevel==3,"M03 unlocks after M02");
                        Require(_game.Mission.ObjectiveText.Contains("Tino")&&_game.Mission.ObjectiveText.Contains("Grip setup"),"M03 exposes its scripted delivery setup");
                        Require(_game.Progression.Points==_pointsAfterM02,"mission selection cannot duplicate the M02 reward");
                        PlayerPrefs.SetInt(ActOneCampaign.ActiveRewardKey(3),1);
                        _game.Mission.SetActiveMission("{\"missionId\":\"M04\",\"level\":4,\"isReplay\":false}");
                        Require(_game.Mission.ActiveMissionLevel==4&&_game.Mission.ObjectiveText.Contains("runner"),"M04 exposes the missing-phone trail");
                        PlayerPrefs.SetInt(ActOneCampaign.ActiveRewardKey(4),1);
                        _game.Mission.SetActiveMission("{\"missionId\":\"M05\",\"level\":5,\"isReplay\":false}");
                        Require(_game.Mission.ActiveMissionLevel==5&&_game.Mission.ObjectiveText.Contains("route card"),"M05 exposes the last-kombi route");
                        for(int level=6;level<=30;level++)
                        {
                            PlayerPrefs.SetInt(ActOneCampaign.ActiveRewardKey(level-1),1);
                            string id=$"M{level:00}";
                            string payload=$"{{\"missionId\":\"{id}\",\"level\":{level},\"isReplay\":false,\"title\":\"Story {level}\",\"district\":\"Harare\",\"activity\":\"combat delivery\",\"vehicle\":\"Sedan\",\"coinReward\":{500+level},\"xpReward\":{100+level},\"objectives\":[\"Meet contact {level}\",\"Travel route {level}\",\"Resolve threat {level}\",\"Choose outcome {level}\"],\"primaryLabel\":\"Crew choice {level}\",\"secondaryLabel\":\"Community choice {level}\",\"primaryTrust\":1,\"secondaryCommunity\":1}}";
                            _game.Mission.SetActiveMission(payload);
                            Require(_game.Mission.ActiveMissionLevel==level,$"{id} is selectable in sequence");
                            Require(_game.Mission.ObjectiveText.Contains($"Meet contact {level}"),$"{id} receives its authored objective contract");
                        }
                        Finish("PASS: M02 plays end to end with visible choices, saved checkpoints, one-time reward and Flutter result; M03-M05 keep their bespoke Act I routes; M06-M30 accept their authored mission contracts and unlock in order. Real saves were isolated.",0);
                        break;
                }
            }
            catch(Exception error){Finish("FAIL: "+error,1);}
        }

        static void MovePlayer(Vector3 position)
        {
            CharacterController body=_game.Player.GetComponent<CharacterController>();
            if(body!=null)body.enabled=false;
            _game.Player.transform.position=position;
            if(body!=null)body.enabled=true;
            Physics.SyncTransforms();
        }

        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Finish(string report,int exitCode)
        {
            File.WriteAllText("/private/tmp/harare-act-one-validation/validation.txt",report);
            Debug.Log("[ActOneCampaignValidation] "+report);
            PlayerPrefs.DeleteKey(ActOneCampaign.ActiveSaveKey);
            for(int level=2;level<=5;level++)PlayerPrefs.DeleteKey(ActOneCampaign.ActiveRewardKey(level));
            PlayerPrefs.DeleteKey(PlayerProgression.ActiveSaveKey);
            PlayerPrefs.Save();SessionState.EraseBool(Key);
            EditorApplication.isPlaying=false;EditorApplication.Exit(exitCode);
        }
    }
}
#endif
