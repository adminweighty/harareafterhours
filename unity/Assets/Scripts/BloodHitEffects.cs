using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Bounded, reusable stylized hit and takedown effects; no textures or decals required.</summary>
    [DefaultExecutionOrder(250)]
    public sealed class BloodHitEffects : MonoBehaviour
    {
        public static BloodHitEffects Instance { get; private set; }
        private sealed class Effect
        {
            public Transform View;
            public Vector3 Velocity, Scale;
            public float Until, Lifetime;
            public bool Droplet;
            public Transform Attachment;
            public Vector3 LocalPoint;
            public Quaternion LocalRotation;
            public bool Attached;
        }
        private readonly Effect[] _pool = new Effect[72];
        private Transform _root;
        private Material _material;
        private Mesh _splat, _sphere;
        private int _cursor;
        private bool _hidden, _reduced;
        public int ActiveCount { get { int count=0; foreach(var item in _pool) if(item?.View != null && item.View.gameObject.activeSelf) count++; return count; } }
        public int EmissionCount { get; private set; }
        public int FatalityCount { get; private set; }
        void Awake() { Instance=this; }
        public void Configure(bool hidden, bool reduced)
        {
            _hidden=hidden; _reduced=reduced;
            if(hidden) Clear();
        }
        public void Clear()
        {
            foreach(var item in _pool)
                if(item?.View != null) { item.View.SetParent(_root,true); item.View.gameObject.SetActive(false); }
        }
        private void EnsurePool()
        {
            if(_root!=null)return;
            _root=new GameObject("Blood effects pool (72 max)").transform;
            _material=RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit","Standard");
            _material.SetColor("_BaseColor",new Color(.38f,.012f,.018f));
            _material.SetFloat("_Metallic",0);_material.SetFloat("_Smoothness",.32f);
            _material.SetFloat("_Cull",0); // Thin splats must render from either side.
            // Irregular flat silhouette, shared across all surface marks.
            const int segments=48;
            var vertices=new Vector3[segments+1];var triangles=new int[segments*3];
            for(int i=0;i<segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                float radius=.5f*(.82f+.08f*Mathf.Sin(angle*3+.4f)+.06f*Mathf.Sin(angle*7)+.04f*Mathf.Cos(angle*11+1.4f));
                vertices[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%segments+1;
            }
            _splat=new Mesh {name="Shared irregular blood splat",vertices=vertices,triangles=triangles};
            _splat.RecalculateNormals();_splat.RecalculateBounds();
            for(int i=0;i<_pool.Length;i++)
            {
                var view=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                view.name="Blood droplet / stain"; view.transform.SetParent(_root,false);
                _sphere=view.GetComponent<MeshFilter>().sharedMesh;
                var collider=view.GetComponent<Collider>(); collider.enabled=false; Destroy(collider);
                var renderer=view.GetComponent<Renderer>(); renderer.sharedMaterial=_material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                view.SetActive(false); _pool[i]=new Effect {View=view.transform};
            }
        }
        private Effect Spawn(Vector3 point, Quaternion rotation, Vector3 scale, float lifetime, Transform parent=null)
        {
            var item=_pool[_cursor++%_pool.Length];
            item.View.SetParent(_root,false);
            item.View.GetComponent<MeshFilter>().sharedMesh=_splat;
            item.View.SetPositionAndRotation(point,rotation); item.View.localScale=scale;
            item.Attached=parent!=null; item.Attachment=parent;
            if(parent!=null){item.LocalPoint=parent.InverseTransformPoint(point);item.LocalRotation=Quaternion.Inverse(parent.rotation)*rotation;}
            item.Scale=item.View.localScale; item.Until=Time.time+lifetime; item.Lifetime=lifetime;
            item.Velocity=Vector3.zero; item.Droplet=false; item.View.gameObject.SetActive(true);
            return item;
        }
        public void Emit(Transform character, Vector3 point, Vector3 direction)
        {
            if(_hidden || character==null || Time.timeScale<=0)return;
            EnsurePool(); EmissionCount++;
            Vector3 normal=direction.sqrMagnitude>.001f?-direction.normalized:-character.forward;
            var animator=character.GetComponentInChildren<Animator>();
            Transform attachment=character;
            float nearest=float.PositiveInfinity;
            if(animator!=null && animator.isHuman)
                foreach(var bone in new[]{HumanBodyBones.Chest,HumanBodyBones.Hips,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})
                {
                    var joint=animator.GetBoneTransform(bone);
                    if(joint==null)continue;
                    float distance=(joint.position-point).sqrMagnitude;
                    if(distance<nearest){nearest=distance;attachment=joint;}
                }
            Spawn(point+normal*.012f,Quaternion.LookRotation(normal),new Vector3(.22f,.29f,.024f),12,attachment);
            for(int i=0;i<(_reduced?3:12);i++)
            {
                var drop=Spawn(point+normal*.03f,Quaternion.identity,Vector3.one*Random.Range(.018f,.04f),Random.Range(.65f,.95f));
                drop.Droplet=true;
                drop.View.GetComponent<MeshFilter>().sharedMesh=_sphere;
                drop.Velocity=normal*Random.Range(.7f,2.1f)+Random.insideUnitSphere*.9f+Vector3.up*.6f;
            }
            // Ignore character colliders; only spill onto an actual upward-facing surface.
            if(TryFindGround(character,out RaycastHit ground))
                for(int i=0;i<3;i++)
                {
                    var offset=Vector3.ProjectOnPlane(Random.insideUnitSphere*.13f,ground.normal);
                    Spawn(ground.point+ground.normal*.008f+offset,Quaternion.FromToRotation(Vector3.forward,ground.normal),new Vector3(.19f+i*.06f,.16f+i*.05f,.01f),18);
                }
        }
        /// <summary>Final-shot flourish: a small, persistent pool and a larger but still capped blood burst.</summary>
        public void EmitFatality(Transform character, Vector3 direction)
        {
            if(_hidden || character==null || Time.timeScale<=0)return;
            EnsurePool(); FatalityCount++;
            Vector3 normal=direction.sqrMagnitude>.001f?-direction.normalized:-character.forward;
            if(TryFindGround(character,out RaycastHit ground))
            {
                int pools=_reduced?2:6;
                for(int i=0;i<pools;i++)
                {
                    Vector3 spread=Vector3.ProjectOnPlane(Random.insideUnitSphere*.38f,ground.normal);
                    float size=.30f+i*.045f;
                    Spawn(ground.point+ground.normal*.010f+spread,
                        Quaternion.FromToRotation(Vector3.forward,ground.normal),
                        new Vector3(size,size*Random.Range(.65f,.92f),.012f),22);
                }
            }
            int droplets=_reduced?3:8;
            for(int i=0;i<droplets;i++)
            {
                Vector3 start=character.position+Vector3.up*Random.Range(.75f,1.3f)+normal*.07f;
                var drop=Spawn(start,Quaternion.identity,Vector3.one*Random.Range(.025f,.052f),Random.Range(.7f,1.05f));
                drop.Droplet=true;
                drop.View.GetComponent<MeshFilter>().sharedMesh=_sphere;
                drop.Velocity=normal*Random.Range(1.2f,2.7f)+Random.insideUnitSphere*1.1f+Vector3.up*Random.Range(.7f,1.25f);
            }
        }
        private static bool TryFindGround(Transform character,out RaycastHit ground)
        {
            ground=default; float closest=float.PositiveInfinity;
            foreach(var hit in Physics.RaycastAll(character.position+Vector3.up*.5f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))
                if(hit.normal.y>.7f && !(hit.collider is CharacterController) && hit.collider.GetComponentInParent<Animator>()==null && !hit.transform.IsChildOf(character) && hit.distance<closest)
                { ground=hit; closest=hit.distance; }
            return ground.collider!=null;
        }
        void LateUpdate()
        {
            foreach(var item in _pool)
            {
                if(item?.View==null || !item.View.gameObject.activeSelf)continue;
                float remaining=item.Until-Time.time;
                if(remaining<=0 || (item.Attached && item.Attachment==null)){item.View.gameObject.SetActive(false);continue;}
                if(item.Attached)item.View.SetPositionAndRotation(item.Attachment.TransformPoint(item.LocalPoint),item.Attachment.rotation*item.LocalRotation);
                if(item.Droplet)
                {
                    item.Velocity+=Vector3.down*9.8f*Time.deltaTime;
                    Vector3 step=item.Velocity*Time.deltaTime;
                    if(step.sqrMagnitude>.000001f && Physics.Raycast(item.View.position,step.normalized,out var hit,step.magnitude,~0,QueryTriggerInteraction.Ignore)
                        && !(hit.collider is CharacterController) && hit.collider.GetComponentInParent<Animator>()==null)
                    {
                        item.Droplet=false; item.View.GetComponent<MeshFilter>().sharedMesh=_splat;
                        item.View.SetPositionAndRotation(hit.point+hit.normal*.008f,Quaternion.FromToRotation(Vector3.forward,hit.normal));
                        item.Scale=new Vector3(.07f,.10f,.01f);item.Until=Time.time+12;item.Lifetime=12;
                    }
                    else item.View.position+=step;
                }
                item.View.localScale=item.Scale*Mathf.Clamp01(remaining/Mathf.Min(.8f,item.Lifetime));
            }
        }
        void OnDestroy()
        {
            if(Instance==this)Instance=null;
            foreach(var item in _pool)if(item?.View!=null)Destroy(item.View.gameObject);
            if(_root!=null)Destroy(_root.gameObject);
            if(_material!=null)Destroy(_material);
            if(_splat!=null)Destroy(_splat);
        }
    }
}
