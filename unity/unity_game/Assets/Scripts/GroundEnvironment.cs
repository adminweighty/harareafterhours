using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Reference-led surfaces; original street centres and mission layout stay unchanged.</summary>
    public static class GroundEnvironment
    {
        public const float WorldSize=1200f;
        public const float BoundaryHalf=598f;
        public static Material Surface(string name)=>Resources.Load<Material>("HarareEnvironment/Surfaces/"+name);
        public static void Build(Transform parent)
        {
            Material soil=Surface("Soil"),asphalt=Surface("Asphalt"),paving=Surface("Paving"),concrete=Surface("Concrete");
            if(soil==null||asphalt==null||paving==null||concrete==null)
                throw new System.InvalidOperationException("Install GroundSurfaceInstaller before building the city.");
            // Continuous support beyond the old 150m plane and 156m roads.
            Box("City ground",parent,new Vector3(0,-.25f,0),new Vector3(WorldSize,.5f,WorldSize),soil);
            Box("City hardstand",parent,new Vector3(0,.006f,0),new Vector3(160,.012f,160),concrete,false);
            float[] streets={-42,0,42};
            Vector2[] segments={new(-78,-48),new(-36,-6),new(6,36),new(48,78)};
            foreach(float street in streets)
            {
                Box("Road north-south",parent,new Vector3(street,.055f,0),new Vector3(12,.11f,156),asphalt);
                Box("Road east-west",parent,new Vector3(0,.06f,street),new Vector3(156,.12f,12),asphalt);
                foreach(var segment in segments)
                {
                    float centre=(segment.x+segment.y)*.5f,length=segment.y-segment.x;
                    foreach(int side in new[]{-1,1})
                    {
                        Box(side<0?"Sidewalk west":"Sidewalk east",parent,new Vector3(street+side*7.3f,.075f,centre),new Vector3(2.2f,.15f,length),paving);
                        Box(side<0?"Sidewalk south":"Sidewalk north",parent,new Vector3(centre,.08f,street+side*7.3f),new Vector3(length,.16f,2.2f),paving);
                        Box("Stone kerb",parent,new Vector3(street+side*6.13f,.07f,centre),new Vector3(.14f,.14f,length),concrete);
                        Box("Stone kerb",parent,new Vector3(centre,.075f,street+side*6.13f),new Vector3(length,.15f,.14f),concrete);
                    }
                }
                for(int d=-70;d<=70;d+=10)
                {
                    bool junction=false;foreach(float cross in streets)if(Mathf.Abs(d-cross)<13)junction=true;
                    if(junction)continue;
                    Box("Lane dash",parent,new Vector3(street,.125f,d),new Vector3(.18f,.006f,3),Surface("RoadPaint"),false);
                    Box("Lane dash",parent,new Vector3(d,.13f,street),new Vector3(3,.006f,.18f),Surface("RoadPaint"),false);
                }
            }
            // Pedestrian crossings beside the starting junction, on top of
            // asphalt only. No collider steps or road-centre relocation.
            for(int side=-1;side<=1;side+=2)
                for(int stripe=-4;stripe<=4;stripe++)
                    Box("First Street crossing",parent,new Vector3(stripe, .126f, side*9),new Vector3(.5f,.004f,2.4f),Surface("CrossingPaint"),false);
            // Keep the playable area supported; the boundary is well beyond
            // every existing traffic route, plot and mission checkpoint.
            for(int side=-1;side<=1;side+=2)
            {
                Boundary(parent,new Vector3(side*BoundaryHalf,2,0),new Vector3(1,4,WorldSize));
                Boundary(parent,new Vector3(0,2,side*BoundaryHalf),new Vector3(WorldSize,4,1));
            }
        }
        static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material material,bool collision=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.position=p;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision){obj.GetComponent<Collider>().enabled=false;Object.Destroy(obj.GetComponent<Collider>());}
            return obj;
        }
        static void Boundary(Transform parent,Vector3 p,Vector3 size)
        {
            var obj=new GameObject("Distant ground boundary");obj.transform.SetParent(parent,false);obj.transform.position=p;
            obj.AddComponent<BoxCollider>().size=size;
        }
    }
}
