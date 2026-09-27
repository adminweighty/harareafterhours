using System.Collections.Generic;
using UnityEngine;
namespace HarareAfterHours
{
    /// <summary>Additive avenue network. The original CBD and mission plots stay intact.</summary>
    public sealed class ExpandedCity : MonoBehaviour
    {
        public const float RoadHalf=420;
        public int BuildingCount { get; private set; }
        public static readonly Vector3 AvondalePosition=new(300,0,260);
        readonly List<Vector3> _paint=new();readonly List<int> _indices=new();
        Mesh _paintMesh;
        const string Resource="HarareEnvironment/CityExpansion/";
        public void Build()
        {
            float[] streets={-420,-210,0,210,420};
            foreach(float street in streets)
            {
                foreach(bool horizontal in new[]{false,true})
                {
                    if(street==0){Road(street,-428,-78,horizontal,12);Road(street,78,428,horizontal,12);}
                    else Road(street,-428,428,horizontal,16);
                    for(int i=0;i<streets.Length-1;i++)
                    {
                        float start=streets[i]+9,end=streets[i+1]-9;
                        if(street==0&&start<78&&end>0)start=78;
                        if(street==0&&start<0&&end>-78)end=-78;
                        Pavements(street,start,end,horizontal,street==0?6:8);
                    }
                    for(float d=-402;d<=402;d+=12)
                    {
                        bool junction=false;foreach(float cross in streets)if(Mathf.Abs(cross-d)<14)junction=true;
                        if(junction||(street==0&&Mathf.Abs(d)<84))continue;
                        Paint(horizontal?new Vector3(d,.132f,street):new Vector3(street,.127f,d),horizontal?new Vector2(4,.16f):new Vector2(.16f,4));
                    }
                }
            }
            var avenue=Resources.Load<GameObject>(Resource+"AvenueOffice");var shop=Resources.Load<GameObject>(Resource+"CBDShopBlock");var garden=Resources.Load<GameObject>(Resource+"GardenOffice");
            if(avenue==null||shop==null||garden==null)throw new System.InvalidOperationException("Run CityExpansionInstaller.Install first.");
            var tree=Resources.Load<GameObject>(Resource+"AvenueJacaranda");
            int index=0;
            foreach(float street in streets)foreach(bool horizontal in new[]{false,true})
                foreach(float d in new[]{-366f,-318,-270,-144,-96,96,144,270,318,366})foreach(int side in new[]{-1,1})
                {
                    Vector3 p=horizontal?new Vector3(d,0,street+side*26):new Vector3(street+side*26,0,d);
                    // Preserve the landmark forecourt and the original CBD bounding box.
                    if(Mathf.Abs(p.x)<90&&Mathf.Abs(p.z)<90)continue;
                    if(Mathf.Abs(p.x-300)<65&&p.z>218&&p.z<300)continue;
                    if(JoinaCityBlock.ReservesPlot(p))continue;
                    var prefab=Mathf.Abs(p.x)+Mathf.Abs(p.z)>610?garden:index%3==0?avenue:shop;
                    var block=Instantiate(prefab,transform);block.name=prefab.name+" plot "+index++;
                    block.transform.SetPositionAndRotation(p,Quaternion.Euler(0,horizontal?(side>0?180:0):(side>0?-90:90),0));BuildingCount++;
                    if(index%2==0&&tree!=null)
                    {
                        Vector3 treePosition=horizontal?new Vector3(d+17,0,street+side*12):new Vector3(street+side*12,0,d+17);
                        Instantiate(tree,treePosition,Quaternion.identity,transform);
                    }
                }
            var landmark=Instantiate(Resources.Load<GameObject>(Resource+"AvondaleEntrance"),transform);
            landmark.name="Avondale reference shopping entrance";landmark.transform.SetPositionAndRotation(AvondalePosition,Quaternion.Euler(0,180,0));
            Box("Avondale forecourt",new Vector3(300,.035f,233),new Vector3(98,.07f,29),GroundEnvironment.Surface("Paving"));
            var car=Resources.Load<GameObject>("HarareVehicles/Prefabs/SharedSUV");
            if(car!=null)for(int i=0;i<5;i++)
            {
                var parked=Instantiate(car,new Vector3(264+i*17,.15f,235),Quaternion.Euler(0,90,0),transform);parked.name="Avondale parked vehicle";
                var controller=parked.GetComponent<VehicleController>();if(controller!=null){controller.enabled=false;Destroy(controller);}
                var body=parked.GetComponent<Rigidbody>();if(body!=null){body.isKinematic=true;Destroy(body);}
                foreach(var wheel in parked.GetComponentsInChildren<WheelCollider>()){wheel.enabled=false;Destroy(wheel);}
                if(parked.GetComponent<Collider>()==null){var box=parked.AddComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(1.9f,1.8f,4.5f);}
            }
            if(tree!=null)foreach(float x in new[]{251f,349f})Instantiate(tree,new Vector3(x,0,230),Quaternion.identity,transform);
            var palm=Resources.Load<GameObject>(Resource+"AvondalePalm");
            if(palm!=null)foreach(float x in new[]{276f,289f,311f,324f})Instantiate(palm,new Vector3(x,0,245),Quaternion.identity,transform);
            _paintMesh=new Mesh{name="Avenue shared road markings"};_paintMesh.SetVertices(_paint);_paintMesh.SetTriangles(_indices,0);_paintMesh.RecalculateNormals();_paintMesh.RecalculateBounds();
            var paint=new GameObject("Avenue road markings");paint.transform.SetParent(transform,false);paint.AddComponent<MeshFilter>().sharedMesh=_paintMesh;paint.AddComponent<MeshRenderer>().sharedMaterial=GroundEnvironment.Surface("RoadPaint");
        }
        void Road(float street,float a,float b,bool horizontal,float width)
        {Box("Expanded avenue asphalt",horizontal?new Vector3((a+b)/2,.06f,street):new Vector3(street,.055f,(a+b)/2),horizontal?new Vector3(b-a,.12f,width):new Vector3(width,.11f,b-a),GroundEnvironment.Surface("Asphalt"));}
        void Pavements(float street,float a,float b,bool horizontal,float half)
        {
            if(b<=a)return;
            foreach(int side in new[]{-1,1})
            {
                // A proper lowered entrance to the Avondale parking forecourt.
                if(horizontal&&street==210&&side==1&&a<300&&b>300){PavementSide(street,a,292,horizontal,half,side);PavementSide(street,308,b,horizontal,half,side);continue;}
                PavementSide(street,a,b,horizontal,half,side);
            }
        }
        void PavementSide(float street,float a,float b,bool horizontal,float half,int side)
        {
            if(b<=a)return;
            Vector3 centre=horizontal?new Vector3((a+b)/2,.075f,street+side*(half+1.25f)):new Vector3(street+side*(half+1.25f),.075f,(a+b)/2);
            Box("Avenue pavement",centre,horizontal?new Vector3(b-a,.15f,2.2f):new Vector3(2.2f,.15f,b-a),GroundEnvironment.Surface("Paving"));
            centre=horizontal?new Vector3((a+b)/2,.07f,street+side*(half+.1f)):new Vector3(street+side*(half+.1f),.07f,(a+b)/2);
            Box("Avenue kerb",centre,horizontal?new Vector3(b-a,.14f,.15f):new Vector3(.15f,.14f,b-a),GroundEnvironment.Surface("Concrete"));
        }
        void Box(string name,Vector3 p,Vector3 scale,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);go.transform.position=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;}
        void Paint(Vector3 p,Vector2 size)
        {
            int first=_paint.Count;float x=size.x/2,z=size.y/2;
            _paint.AddRange(new[]{p+new Vector3(-x,0,-z),p+new Vector3(-x,0,z),p+new Vector3(x,0,z),p+new Vector3(x,0,-z)});
            _indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
        }
        void OnDestroy(){if(_paintMesh!=null)Destroy(_paintMesh);}
    }
}
