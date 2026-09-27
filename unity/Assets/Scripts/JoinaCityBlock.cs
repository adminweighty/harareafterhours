using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarareAfterHours
{
    /// <summary>Reference-led Joina landmark, added without moving the original mission district.</summary>
    public sealed class JoinaCityBlock : MonoBehaviour
    {
        public const string IntroSaveKey = "Harare.Joina.Welcome.v1";
        public static readonly Vector3 Spawn = new(7.5f, .2f, 173f);
        public static readonly Vector3 Contact = new(18f, .2f, 225f);
        public static bool WelcomePending => PlayerPrefs.GetInt(IntroSaveKey, 0) == 0;
        readonly List<Material> materials = new();
        readonly List<Mesh> meshes = new();
        readonly Dictionary<Material, List<CombineInstance>> solid = new(), detail = new();
        Mesh cube, cylinder, palmCrown;
        Material stone, glass, dark, gold, white, leaves, bark;

        public static bool ReservesPlot(Vector3 p) => p.x > 8 && p.x < 96 && p.z > 218 && p.z < 295;

        public void Build()
        {
            cube = PrimitiveMesh(PrimitiveType.Cube); cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            stone = new Material(GroundEnvironment.Surface("WallCream")){name="Joina textured limestone",enableInstancing=true};
            stone.SetColor("_BaseColor",new Color(.93f,.9f,.82f));stone.SetFloat("_MetresPerTile",4);materials.Add(stone);
            glass = Material("Joina blue reflective glazing", new Color(.12f,.34f,.47f), .78f, .65f);
            dark = Material("Joina charcoal metal", new Color(.055f,.075f,.085f), .3f, .5f);
            gold = Material("Joina warm shop lighting", new Color(1,.65f,.23f), .4f, .25f, true);
            white = Material("Joina pale facade bands", new Color(.84f,.82f,.73f), .22f, .15f);
            leaves = Material("Joina palm foliage",new Color(.16f,.25f,.065f),.18f,0);
            leaves.SetFloat("_Cull",0);
            bark = Material("Joina palm bark",new Color(.24f,.17f,.09f),.12f,0);
            // South-facing podium leaves a continuous public forecourt beside the avenue.
            Box("Retail podium", new(47,7.5f,254), new(60,15,48), stone, true);
            Box("Forecourt", new(44,.07f,224.5f), new(74,.14f,10), GroundEnvironment.Surface("Paving"), true);
            Box("West pedestrian arcade", new(12,.07f,253), new(9,.14f,62), GroundEnvironment.Surface("Paving"), true);
            Box("South arrival plaza",new(33,.065f,184),new(50,.13f,32),GroundEnvironment.Surface("Paving"),true);
            Box("Podium cornice", new(47,15.25f,254), new(61,.5f,49), white);
            Box("Arcade canopy", new(47,4.8f,228.2f), new(60,.28f,4.2f), stone, true);
            Box("Canopy warm soffit", new(47,4.62f,226.7f), new(59,.08f,.16f), gold);
            for(int i=0;i<10;i++)
            {
                float x=20+i*5.8f;
                Box("Shop glass",new(x,2.4f,229.92f),new(5.3f,4.3f,.12f),glass);
                Box("Shop pier",new(x-2.8f,2.4f,227.9f),new(.35f,4.8f,.4f),stone,true);
                Box("Shop transom",new(x,3.8f,229.8f),new(5.3f,.09f,.1f),dark, false, true);
                Box("Shop mullion",new(x,2.3f,229.78f),new(.09f,4.2f,.1f),dark,false,true);
            }
            Sign("JOINA CITY",new(37,10.7f,229.85f),.7f,Color.black);
            Sign("SHOP  /  DINE  /  WORK",new(62,7.1f,229.85f),.22f,new Color(.12f,.16f,.18f));
            string[] shops={"CITY CAFE","FASHION","TECH","FOOD COURT","JOINA • LOBBY"};
            for(int i=0;i<shops.Length;i++)Sign(shops[i],new(23+i*11.6f,4.12f,227.55f),.12f,Color.white);
            // Panel joints, upper retail glazing and west-facing arcade avoid a blank box podium.
            for(float x=17;x<=77;x+=3)
                Box("Stone panel vertical joint",new(x,10.1f,229.98f),new(.035f,9.3f,.04f),dark,false,true);
            foreach(float y in new[]{5.4f,8.4f,11.4f,14.4f})
            {
                Box("Stone panel horizontal joint",new(47,y,229.98f),new(60,.035f,.04f),dark,false,true);
                Box("West panel joint",new(16.98f,y,254),new(.04f,.035f,48),dark,false,true);
            }
            for(float z=233;z<277;z+=5.8f)
            {
                Box("West retail glazing",new(16.92f,2.3f,z),new(.14f,4.3f,5.2f),glass);
                Box("West upper glazing",new(16.92f,11.9f,z),new(.14f,2.4f,4.3f),glass);
                Box("West arcade column",new(14.8f,2.4f,z-2.8f),new(.4f,4.8f,.35f),stone,true);
            }
            Box("West arcade canopy",new(15.3f,4.8f,254),new(3.5f,.28f,48),stone,true);
            Box("Front roof rail",new(47,16.1f,229.9f),new(60,.1f,.1f),dark,false,true);
            for(float x=18;x<78;x+=2)Box("Roof rail post",new(x,15.65f,229.9f),new(.07f,1,.07f),dark,false,true);
            // Cream rectangular tower, staggered curved glass wings, drum and disc crown.
            Box("Office stone tower",new(39,52,256),new(22,74,23),stone,true);
            Wing(new(56,15,256),12,49,glass);
            Wing(new(53,64,258),9,27,glass);
            Box("Central crown spine",new(45,85,260),new(8,15,12),stone);
            Cylinder(new(46,95,260),new(13,4,13),stone);
            Cylinder(new(46,99.4f,260),new(21,.55f,21),white);
            Cylinder(new(46,98.65f,260),new(19,.13f,19),gold);
            Cylinder(new(46,100.05f,260),new(12,.2f,12),dark);
            for(int i=0;i<32;i++)
            {
                float angle=i*Mathf.PI*2/32;
                Box("Crown drum flute",new(46+6.55f*Mathf.Cos(angle),95,260+6.55f*Mathf.Sin(angle)),new(.14f,7.4f,.14f),white,false,true);
            }
            for(int i=0;i<3;i++)Box("Crown aerial",new(41+i*5,103,260),new(.1f,6,.1f),dark,false,true);
            for(int floor=0;floor<21;floor++)
            {
                float y=17.2f+floor*3.4f;
                for(int col=0;col<6;col++)
                    Box("Recessed office window",new(29.8f+col*3.3f,y,244.43f),new(1.75f,1.8f,.16f),glass,false,true);
                for(int col=0;col<6;col++)
                    Box("West office window",new(27.94f,y,246.3f+col*3.5f),new(.16f,1.8f,1.8f),glass,false,true);
            }
            for(float y=18;y<64;y+=3.4f)Wing(new(56,y,256),12.08f,.65f,white);
            for(float y=66;y<91;y+=3.4f)Wing(new(53,y,258),9.08f,.65f,white);
            // Window mullions stay a separate culling group; no per-window materials/lights.
            for(int i=0;i<=16;i++)
            {
                float a=Mathf.PI+i*Mathf.PI/16;
                Box("Curved glazing mullion",new(56+12.1f*Mathf.Cos(a),39.5f,256+12.1f*Mathf.Sin(a)),new(.1f,49,.1f),dark,false,true);
            }
            foreach(float z in new[]{183f,199,233,252,272})Palm(new Vector3(12.4f,.15f,z));
            foreach(float x in new[]{29f,45,61,76})Palm(new Vector3(x,.15f,222));
            foreach(float x in new[]{23f,39f,54f})
            {
                Box("Arrival planter",new(x,.35f,197),new(5,.7f,2.2f),stone,true);
                Box("Planter foliage",new(x,.8f,197),new(4.6f,.4f,1.8f),leaves);
                Box("Bench seat",new(x,.55f,194.8f),new(3.2f,.2f,.65f),bark,true);
                Box("Bench back",new(x,.94f,195.1f),new(3.2f,.65f,.12f),bark,true);
            }
            for(int i=0;i<5;i++)
            {
                float z=179+i*23;
                Box("Street lamp",new(-8,4,z),new(.16f,8,.16f),dark,true);
                Box("Lamp arm",new(-6.8f,7.8f,z),new(2.5f,.12f,.14f),dark);
                Box("Lamp head",new(-5.7f,7.7f,z),new(.7f,.1f,.4f),gold);
                AddStreetLampLight(new Vector3(-5.7f,7.55f,z));
                Box("City banner",new(-7.2f,5.5f,z),new(1.1f,2.3f,.07f),dark);
                Sign("HARARE\nOUR CITY\nOUR HOME",new(-7.2f,5.5f,z-.045f),.07f,Color.white);
            }
            for(int i=0;i<9;i++)Box("Zebra crossing",new(-4.8f+i*1.2f,.135f,202),new(.6f,.012f,3.8f),GroundEnvironment.Surface("RoadPaint"));
            Box("Wayfinding pole",new(8.8f,1.8f,219),new(.12f,3.6f,.12f),dark,true);
            Box("Wayfinding board",new(8.8f,3.4f,219),new(5,1.2f,.14f),dark);
            Sign("JOINA CITY  >\nJASON MOYO AVE",new(8.8f,3.4f,218.9f),.13f,Color.white);
            Bake(solid,"Joina silhouette and street surfaces",.003f);
            Bake(detail,"Joina close facade detail",.09f);
            var prefab=Resources.Load<GameObject>("HarareCharacters/Character01");
            if(prefab!=null)
            {
                var contact=Instantiate(prefab,Contact,Quaternion.Euler(0,205,0),transform);contact.name="Joina welcome contact";
                contact.AddComponent<RuntimeCharacterVisual>().Configure(new CharacterCastEntry{Id="joina_contact",DisplayName="City contact",SkinTone="dark",HairStyle="close cut",Outfit="olive",Accessory="none",Wardrobe="field",Accent="silver",Height=1,Build=1},false);
            }
        }

        void AddStreetLampLight(Vector3 position)
        {
            var source = new GameObject("Joina warm street light");
            source.transform.SetParent(transform, false);
            source.transform.localPosition = position;
            source.transform.localRotation = Quaternion.LookRotation(new Vector3(.12f,-1f,0f));
            var light = source.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f,.61f,.30f);
            light.intensity = 8.5f;
            light.range = 25f;
            light.spotAngle = 96f;
            light.innerSpotAngle = 54f;
            light.shadows = LightShadows.None;
            if(AdaptiveStreetLighting.Instance!=null)AdaptiveStreetLighting.Instance.Register(light);
        }

        Material Material(string name,Color color,float smooth,float metallic,bool emissive=false)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);
            if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.7f);}materials.Add(m);return m;
        }
        Mesh PrimitiveMesh(PrimitiveType type)
        {
            var p=GameObject.CreatePrimitive(type);var mesh=p.GetComponent<MeshFilter>().sharedMesh;
            p.SetActive(false);Destroy(p);return mesh;
        }
        void Palm(Vector3 p)
        {
            Cylinder(p+Vector3.up*4.5f,new Vector3(.38f,4.5f,.38f),bark);
            var collider=new GameObject("Palm trunk collision");collider.transform.SetParent(transform,false);collider.transform.localPosition=p+Vector3.up*4.5f;
            var capsule=collider.AddComponent<CapsuleCollider>();capsule.radius=.2f;capsule.height=9;
            if(palmCrown!=null){Add(palmCrown,p,Vector3.one,leaves);return;}
            var v=new List<Vector3>();var triangles=new List<int>();
            for(int frond=0;frond<11;frond++)
            {
                float a=frond*Mathf.PI*2/11;var forward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,forward);
                for(int j=1;j<=10;j++)
                {
                    float t=j/11f;var centre=Vector3.up*(9+Mathf.Sin(t*Mathf.PI)*1.15f-t*t*1.6f)+forward*t*3.8f;
                    float width=Mathf.Sin(t*Mathf.PI)*.95f;
                    foreach(int sign in new[]{-1,1})
                    {
                        int n=v.Count;v.Add(centre-forward*.25f);v.Add(centre+side*width*sign+forward*.6f-Vector3.up*.24f);v.Add(centre+forward*.19f);
                        triangles.AddRange(new[]{n,n+1,n+2});
                    }
                }
            }
            var mesh=new Mesh{name="Reusable fan palm crown"};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);palmCrown=mesh;Add(mesh,p,Vector3.one,leaves);
        }
        void Add(Mesh mesh,Vector3 p,Vector3 scale,Material m,bool fine=false)
        {
            var buckets=fine?detail:solid;if(!buckets.TryGetValue(m,out var list))buckets[m]=list=new();
            list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,Quaternion.identity,scale)});
        }
        void Box(string name,Vector3 p,Vector3 scale,Material m,bool collision=false,bool fine=false)
        {
            Add(cube,p,scale,m,fine);
            if(collision){var go=new GameObject(name+" collision");go.transform.SetParent(transform,false);go.transform.localPosition=p;go.AddComponent<BoxCollider>().size=scale;}
        }
        void Cylinder(Vector3 p,Vector3 scale,Material m)=>Add(cylinder,p,scale,m);
        void Wing(Vector3 p,float radius,float height,Material m)
        {
            // Half-round street face and flat rear. Sixteen segments are enough at city scale.
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<16;i++)
            {
                float a=Mathf.PI+i*Mathf.PI/16,b=Mathf.PI+(i+1)*Mathf.PI/16;
                var u=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                var v=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                int n=vertices.Count;vertices.AddRange(new[]{u,u+Vector3.up*height,v+Vector3.up*height,v});
                indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                n=vertices.Count;vertices.AddRange(new[]{Vector3.up*height,v+Vector3.up*height,u+Vector3.up*height});indices.AddRange(new[]{n,n+1,n+2});
            }
            var mesh=new Mesh{name="Joina half-round wing"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);Add(mesh,p,Vector3.one,m);
        }
        void Bake(Dictionary<Material,List<CombineInstance>> buckets,string name,float cutoff)
        {
            var root=new GameObject(name);root.transform.SetParent(transform,false);var renderers=new List<Renderer>();
            foreach(var entry in buckets)
            {
                var mesh=new Mesh{name=name+" / "+entry.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray());meshes.Add(mesh);
                var go=new GameObject(entry.Key.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=entry.Key;renderers.Add(r);
            }
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(cutoff,renderers.ToArray())});lod.RecalculateBounds();
        }
        void Sign(string value,Vector3 p,float size,Color color)
        {
            var go=new GameObject(value);go.transform.SetParent(transform,false);go.transform.localPosition=p;
            var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
            var m=new Material(Resources.Load<Shader>("HarareWorldText"));m.mainTexture=text.font.material.mainTexture;materials.Add(m);go.GetComponent<Renderer>().sharedMaterial=m;
            var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.014f,new[]{go.GetComponent<Renderer>()})});lod.RecalculateBounds();
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var m in materials)if(m!=null)Destroy(m);}
    }
}
