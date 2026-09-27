using System.Collections.Generic;
using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Small, walk-up discovery circuit; does not replace the main mission.</summary>
    public sealed class CityVenue : MonoBehaviour
    {
        private static readonly List<CityVenue> Venues = new();
        public string VenueId { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3 Approach => transform.TransformPoint(new Vector3(0, .2f, 1.4f));
        private string _notice;
        private float _noticeUntil;
        private readonly List<Material> _owned = new();

        public static void AddToPlot(Transform parent, int x, int z, Vector3 centre, float width, float depth)
        {
            string id = null, title = null, subtitle = null;
            bool lounge = false;
            if (x == 18 && z == -22) { id="big_apple"; title="The Velvet Room"; subtitle="LOUNGE PREVIEW"; lounge=true; }
            if (x == 18 && z == 58) { id="private_lounge"; title="Private Lounge"; subtitle="MUSIC • SOCIAL • VIP"; lounge=true; }
            if (x == -22 && z == 18) { id="city_grocer"; title="CITY GROCER"; subtitle="FRUIT • VEG • DAILY ESSENTIALS"; }
            if (x == -22 && z == -22) { id="sadza_kitchen"; title="SADZA KITCHEN"; subtitle="SADZA • GRILL • TAKEAWAY"; }
            if (id == null) return;
            var root = new GameObject(title + " storefront");
            root.transform.SetParent(parent);
            float side = x > 0 ? -1 : 1;
            root.transform.SetPositionAndRotation(new Vector3(centre.x + side*(width/2+.09f),0,centre.z), Quaternion.Euler(0,side*90,0));
            var venue = root.AddComponent<CityVenue>();
            venue.VenueId=id; venue.DisplayName=title;
            venue.Build(Mathf.Min(depth-.3f,10),subtitle,lounge,x>0);
            Venues.Add(venue);
        }

        private Material Material(string name, Color color, bool glow=false)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=name, enableInstancing=true };
            material.SetColor("_BaseColor",color);
            if(glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*.8f); }
            _owned.Add(material); return material;
        }

        private void Build(float width,string subtitle,bool lounge,bool club)
        {
            var dark=Material("Shared storefront charcoal",new Color(.055f,.06f,.065f));
            var trim=Material("Storefront accent",lounge?new Color(.64f,.42f,.13f):club?new Color(.6f,.08f,.075f):new Color(.15f,.32f,.18f));
            var glass=Material("Tinted shop glazing",new Color(.1f,.17f,.18f));
            glass.SetFloat("_Smoothness",.78f);
            var light=Material("Low cost sign glow",lounge?new Color(1,.68f,.26f):new Color(.95f,.75f,.53f),true);
            var renderers = new List<Renderer>();
            void Box(string name,Vector3 pos,Vector3 size,Material mat)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
                obj.transform.SetParent(transform,false);obj.transform.localPosition=pos;obj.transform.localScale=size;
                Destroy(obj.GetComponent<Collider>());
                var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=mat;renderers.Add(renderer);
            }
            Box("Recessed glazing",new Vector3(0,1.55f,0),new Vector3(width,3.1f,.1f),glass);
            Box("Shop fascia",new Vector3(0,3.55f,.05f),new Vector3(width,1,.18f),dark);
            Box("Entrance canopy",new Vector3(0,3.02f,.45f),new Vector3(width,.15f,.95f),dark);
            Box("Warm entrance strip",new Vector3(0,2.9f,.35f),new Vector3(width-.2f,.045f,.05f),light);
            for(int i=-2;i<=2;i++)
                Box("Door and window mullion",new Vector3(i*width/4,1.5f,.12f),new Vector3(.08f,3,.12f),trim);
            Box("Door handle",new Vector3(.28f,1.2f,.22f),new Vector3(.04f,.55f,.06f),light);
            if(lounge)
                for(int i=0;i<13;i++)
                    Box("Gold horizontal screen",new Vector3(-width*.37f,.3f+i*.19f,.17f),new Vector3(width*.23f,.065f,.1f),trim);
            else if(!club)
                for(int i=0;i<4;i++)
                    Box("Window display",new Vector3(-width*.37f+i*.35f,.75f,.16f),new Vector3(.26f,.25f,.16f),trim);
            Text(DisplayName,new Vector3(0,3.72f,.16f),lounge?new Color(1,.78f,.4f):club?new Color(1,.25f,.2f):Color.white,Mathf.Min(.135f,width/(DisplayName.Length*1.3f)));
            Text(subtitle,new Vector3(0,3.3f,.16f),Color.white,.055f);
            Text("USE • ENTER",new Vector3(0,1.75f,.19f),Color.white,.045f);
            // One coarse culling LOD, shared primitive meshes and no new real-time lights.
            var group=gameObject.AddComponent<LODGroup>();
            group.SetLODs(new[]{new LOD(.018f,GetComponentsInChildren<Renderer>())});group.RecalculateBounds();
        }

        private void Text(string value,Vector3 position,Color color,float size)
        {
            var obj=new GameObject(value);obj.transform.SetParent(transform,false);obj.transform.localPosition=position;
            obj.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=obj.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;
            text.fontSize=48;text.characterSize=size;text.color=color;
            var material=new Material(Resources.Load<Shader>("HarareWorldText"));
            material.mainTexture=text.font.material.mainTexture;_owned.Add(material);
            obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        public static CityVenue Nearest(ThirdPersonController player)
        {
            if(player==null||player.IsInVehicle||player.InputLocked)return null;
            CityVenue nearest=null;float distance=2.8f;
            foreach(var venue in Venues)
            {
                if(venue==null)continue;
                float d=Vector3.Distance(player.transform.position,venue.Approach);
                if(d>=distance)continue;
                // Require the player outside the facade, never through the building.
                if(venue.transform.InverseTransformPoint(player.transform.position).z<.15f)continue;
                nearest=venue;distance=d;
            }
            return nearest;
        }
        public static string Hint(ThirdPersonController player)
        {
            var venue=Nearest(player);
            return venue==null?null:Time.time<venue._noticeUntil?venue._notice:$"ENTER / E · {venue.DisplayName}";
        }
        public static bool TryInteract(ThirdPersonController player)
        {
            var venue=Nearest(player);if(venue==null)return false;
            if(StreetActionDirector.Instance!=null&&StreetActionDirector.Instance.Wanted>0)
            {venue._notice="Lose the police before checking in.";venue._noticeUntil=Time.time+4;return true;}
            var score=player.GetComponent<PlayerProgression>();
            if(score==null)return true;
            int before=score.Points;score.AwardMissionStep("venue_discovery_"+venue.VenueId,50);
            venue._notice=score.Points>before?$"{venue.DisplayName} discovered · +50 PTS":$"Welcome back to {venue.DisplayName}";
            venue._noticeUntil=Time.time+5;
            VenueInterior.Enter(venue,player);return true;
        }
        private void OnDestroy()
        {
            Venues.Remove(this);foreach(var material in _owned)if(material!=null)Destroy(material);
        }
    }
}
