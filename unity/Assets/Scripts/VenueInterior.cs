using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    // Small instanced rooms: preserve the exterior layout and building collision.
    // Only the occupied interior exists. Main mission state is never changed.
    public sealed class VenueInterior : MonoBehaviour
    {
        public const string RescueAward = "city_grocer_rescue_v1";
        public static VenueInterior Active { get; private set; }
        public static readonly Vector3 Origin = new(1200, 20, 1200);
        public Vector3 ExitPoint => Origin + new Vector3(0,.1f,-5);
        public bool Escorting { get; private set; }
        public bool RobbersStopped => _robbers.Count > 0 && _robbers.TrueForAll(a=>a!=null&&a.Down);
        public string Objective => _rescue ? !RobbersStopped ? "ROBBERY · Stop robbers "+_robbers.FindAll(a=>a!=null&&a.Down).Count+"/2"
            : !Escorting ? "RESCUE · Reach the shopkeeper" : "ESCORT · Lead shopkeeper to EXIT" : _hostSpoken ? "Return to EXIT · continue outside" : "Meet the host · tap TALK";
        public string Hint => _rescue ? !RobbersStopped ? "PUNCH / KICK / FIRE · watch your LIFE"
            : !Escorting ? "Approach shopkeeper · tap RESCUE" : "Stay close · return to the marked exit" : _hostSpoken ? "Menu shows your next story step" : "Host by the welcome desk · no combat required";
        public string ActionLabel => NearExit ? "EXIT" : NearHost ? "TALK" : _rescue&&RobbersStopped&&!Escorting&&NearCivilian ? "RESCUE" : "USE";
        public bool NearExit => Vector3.Distance(_player.transform.position,ExitPoint)<2.3f;
        public bool NearCivilian => _civilian!=null&&Vector3.Distance(_player.transform.position,_civilian.position)<2.5f;
        ThirdPersonController _player; CityVenue _venue; Vector3 _return; Quaternion _returnRotation;
        readonly List<StreetActor> _robbers=new(); readonly List<Material> _materials=new();
        Transform _civilian, _host; CharacterController _civilianBody; bool _rescue, _hostSpoken;
        public bool NearHost => _host != null && Vector3.Distance(_player.transform.position,_host.position)<2.5f;
        public Vector3 HostPoint => _host != null ? _host.position : ExitPoint;
        public string GuideText => _rescue ? Hint + ". This is an optional prototype rescue, separate from the story."
            : _venue.VenueId=="big_apple" ? "This is The Velvet Room lounge preview. Its M16–M17 story, invitations and conversations are not playable yet. Talk to the host, then use the marked EXIT to return to your current objective."
            : "This venue is a social preview, not an active story mission. Talk to the host, then walk to EXIT and tap EXIT to continue outside.";

        public static void Enter(CityVenue venue,ThirdPersonController player)
        {
            if(Active!=null||player.IsInVehicle||player.InputLocked)return;
            var room=new GameObject(venue.DisplayName+" playable interior").AddComponent<VenueInterior>();
            Active=room;room._venue=venue;room._player=player;
            room._return=player.transform.position;room._returnRotation=player.transform.rotation;
            room._rescue=venue.VenueId=="city_grocer"&&!player.GetComponent<PlayerProgression>().HasMissionAward(RescueAward);
            room.Build();room.MovePlayer(room.ExitPoint,Quaternion.identity);
            FlutterGameBridge.Instance?.PublishGuidance();
            StreetActionDirector.Instance?.Notify(room._rescue?"ROBBERY IN PROGRESS · Save the shopkeeper":"Entered "+venue.DisplayName);
        }
        Material Mat(string name,Color color)
        {
            var m=RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit","Standard");m.name=name;m.color=color;m.SetColor("_BaseColor",color);m.enableInstancing=true;
            m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.20f);_materials.Add(m);return m;
        }
        void Box(string name,Vector3 at,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(transform);
            obj.transform.position=Origin+at;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;
        }
        void Sign(string value,Vector3 at)
        {
            var obj=new GameObject(value);obj.transform.SetParent(transform);obj.transform.position=Origin+at;
            var text=obj.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.065f;text.color=new Color(1,.8f,.3f);
            var m=new Material(Resources.Load<Shader>("HarareWorldText"));m.mainTexture=text.font.material.mainTexture;
            obj.GetComponent<MeshRenderer>().sharedMaterial=m;_materials.Add(m);
        }
        void Build()
        {
            var wall=Mat("Interior cream plaster",new Color(.65f,.59f,.44f));
            var floor=Mat("Interior stone tiles",new Color(.28f,.30f,.28f));
            var wood=Mat("Interior counter walnut",new Color(.27f,.12f,.05f));
            var fixture=Mat("Warm evening ceiling fixture",new Color(1f,.62f,.25f));
            fixture.SetColor("_EmissionColor",new Color(1f,.48f,.16f)*2.2f);
            Box("Floor",new Vector3(0,-.2f,0),new Vector3(14,.4f,14),floor);
            Box("Ceiling",new Vector3(0,5.2f,0),new Vector3(14,.3f,14),wall);
            Box("Back wall",new Vector3(0,2.5f,7),new Vector3(14,5,.3f),wall);
            Box("Entrance wall",new Vector3(0,2.5f,-7),new Vector3(14,5,.3f),wall);
            foreach(float x in new[]{-7f,7f})Box("Side wall",new Vector3(x,2.5f,0),new Vector3(.3f,5,14),wall);
            Box("Exit mat",new Vector3(0,.015f,-5),new Vector3(2,.03f,1.5f),wood);
            Box("Ceiling fixture A",new Vector3(-3,4.95f,0),new Vector3(2.2f,.08f,.32f),fixture);
            Box("Ceiling fixture B",new Vector3(3,4.95f,0),new Vector3(2.2f,.08f,.32f),fixture);
            var lightObject=new GameObject("Warm interior light");lightObject.transform.SetParent(transform);
            lightObject.transform.position=Origin+new Vector3(0,4.25f,0);
            var roomLight=lightObject.AddComponent<Light>();roomLight.type=LightType.Point;
            roomLight.color=new Color(1f,.66f,.40f);roomLight.intensity=4.2f;roomLight.range=11f;roomLight.shadows=LightShadows.None;
            Sign("EXIT · USE",new Vector3(0,2,-6.7f));
            Sign(_venue.DisplayName,new Vector3(0,3,6.7f));
            if(_rescue) foreach(float x in new[]{-5f,5f})
            {
                Box("Shop shelf / lounge counter",new Vector3(x,.8f,1),new Vector3(1,1.6f,5),wood);
                for(int i=0;i<5;i++)Box("Display goods",new Vector3(x,1.8f,-1+i),new Vector3(.65f,.35f,.55f),wall);
            }
            if(!_rescue)
            {
                BuildSocialRoom(wood,wall,fixture);
                return;
            }
            var director=StreetActionDirector.Instance;
            _robbers.Add(director.SpawnActor(false,Origin+new Vector3(-2,.1f,0),"Robber · City Grocer"));
            _robbers.Add(director.SpawnActor(false,Origin+new Vector3(2,.1f,2),"Robber · City Grocer"));
            var root=new GameObject("Shopkeeper · RESCUE");root.transform.SetParent(transform);root.transform.position=Origin+new Vector3(0,.1f,5);
            _civilian=root.transform;_civilianBody=root.AddComponent<CharacterController>();_civilianBody.height=1.78f;_civilianBody.radius=.3f;_civilianBody.center=Vector3.up*.91f;
            var model=Instantiate(Resources.Load<GameObject>("HarareCharacters/Character01"),root.transform);
            model.AddComponent<RuntimeCharacterVisual>().Configure(new CharacterCastEntry{DisplayName="Shopkeeper",SkinTone="dark",Outfit="navy",Accent="gold"},false);
            Sign("SHOPKEEPER · RESCUE",new Vector3(0,2.3f,5));
        }
        void BuildSocialRoom(Material wood,Material wall,Material fixture)
        {
            bool music = _venue.VenueId=="big_apple" || _venue.VenueId=="private_lounge";
            Box("Welcome desk",new Vector3(-3,.55f,-1.2f),new Vector3(1.8f,1.1f,.7f),wood);
            _host=Guest("Venue host",new Vector3(-2,.05f,-.3f),"navy","gold");
            Sign("HOST · TALK",new Vector3(-2,2.2f,-.3f));
            Box("Service bar",new Vector3(-5,.6f,3.5f),new Vector3(1.1f,1.2f,4),wood);
            Guest("Venue staff",new Vector3(-6,.05f,3.5f),"teal","gold");
            foreach(float z in new[]{0f,3.5f})
            {
                Box("Social table",new Vector3(3,.72f,z),new Vector3(1.2f,.12f,1.2f),wood);
                Box("Table base",new Vector3(3,.35f,z),new Vector3(.25f,.7f,.25f),wood);
                Box("Upholstered bench",new Vector3(5,.35f,z),new Vector3(.7f,.7f,1.6f),wall);
            }
            Guest("Guest in conversation",new Vector3(2,.05f,1.2f),"plum","gold");
            Guest("Guest by the tables",new Vector3(4,.05f,2.2f),"olive","silver");
            if(music)
            {
                Box("Performance stage",new Vector3(0,.16f,5.2f),new Vector3(3.8f,.32f,2),wood);
                Box("Stage light strip",new Vector3(0,.34f,4.2f),new Vector3(3.8f,.04f,.08f),fixture);
                Sign("STORY PREVIEW · MENU FOR NEXT STEPS",new Vector3(0,2.5f,6.65f));
            }
        }
        Transform Guest(string label,Vector3 at,string outfit,string accent)
        {
            var prefab=Resources.Load<GameObject>("HarareCharacters/Character01");
            var npc=Instantiate(prefab,Origin+at,Quaternion.Euler(0,180,0),transform);npc.name=label;
            npc.AddComponent<RuntimeCharacterVisual>().Configure(new CharacterCastEntry{
                Id="venue_"+label,DisplayName=label,SkinTone="dark",HairStyle="natural",
                Outfit=outfit,Accent=accent,Accessory="none",Wardrobe="street",Height=1,Build=1},false);
            npc.AddComponent<MissionNpcPresence>().Configure(_player.transform);
            return npc.transform;
        }

        public static bool TryInteract()
        {
            if(Active==null)return false;
            Active.Interact();return true; // Do not board exterior cars or advance unrelated missions.
        }
        public void Interact()
        {
            if(_player.InputLocked||Time.timeScale<=0)return;
            if(NearExit)
            {
                bool rescued=Escorting&&NearCivilian&&RobbersStopped;
                if(Escorting&&!NearCivilian){StreetActionDirector.Instance?.Notify("Wait for the shopkeeper at the exit");return;}
                if(rescued)_player.GetComponent<PlayerProgression>().AwardMissionStep(RescueAward,200);
                MovePlayer(_return,_returnRotation);
                StreetActionDirector.Instance?.Notify(rescued?"SHOPKEEPER SAFE · +200 rescue points":_rescue?"Left rescue unfinished · return to try again":"Back on the street");
                Cleanup();FlutterGameBridge.Instance?.PublishGuidance();Destroy(gameObject);return;
            }
            if(NearHost)
            {
                _hostSpoken=true;
                StreetActionDirector.Instance?.Notify("HOST · This venue's story is not available yet. Menu shows what to do next.");
                FlutterGameBridge.Instance?.PublishGuidance();
                FlutterGameBridge.Instance?.Publish("open_guidance","{}");
                return;
            }
            if(_rescue&&RobbersStopped&&NearCivilian){Escorting=true;StreetActionDirector.Instance?.Notify("Shopkeeper following · lead them to EXIT");}
            else StreetActionDirector.Instance?.Notify(Hint);
        }
        void MovePlayer(Vector3 position,Quaternion rotation)
        {
            TouchGameplayControls.Instance?.Cancel();_player.GetComponent<PlayerCombat>()?.CancelPendingMelee();
            var cc=_player.GetComponent<CharacterController>();cc.enabled=false;_player.transform.SetPositionAndRotation(position,rotation);cc.enabled=true;
            Camera.main?.GetComponent<ThirdPersonCameraRig>()?.SetTarget(_player.transform,false);
        }
        void Update()
        {
            if(_player==null)return;
            // Restart/arrest can relocate the player externally. Never leave an invisible active room.
            if(Vector3.Distance(_player.transform.position,Origin)>40){Cleanup();Destroy(gameObject);return;}
            if(Time.timeScale<=0||_player.InputLocked||!Escorting||_civilianBody==null)return;
            var delta=_player.transform.position-_civilian.position;delta.y=0;
            if(delta.magnitude>1.4f&&delta.magnitude<8)
            {
                _civilian.rotation=Quaternion.LookRotation(delta);
                _civilianBody.Move((delta.normalized*2.6f+Vector3.down*4)*Time.deltaTime);
            }
        }
        void Cleanup()
        {
            if(Active==this)Active=null;
            foreach(var actor in _robbers)if(actor!=null){StreetActionDirector.Instance?.Actors.Remove(actor);actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
            _robbers.Clear();
        }
        void OnDestroy(){Cleanup();foreach(var m in _materials)if(m!=null)Destroy(m);}
    }
}
