using UnityEngine;

namespace HarareAfterHours
{
    /// <summary>Reusable mobile-sized shot effects; no camera shake or full-screen flash.</summary>
    [DefaultExecutionOrder(220)]
    public sealed class WeaponFeedback : MonoBehaviour
    {
        private AudioSource _audio;
        private AudioClip _pistol, _rifle;
        private Material _material;
        private LineRenderer _tracer;
        private GameObject _flash, _impact;
        private Transform _muzzle;
        private float _flashUntil, _traceUntil, _hitUntil;
        private bool _muted, _reduced, _isRifle;
        private bool _characterHit;
        private Color _color;
        private Vector3 _shotStart, _shotEnd;
        private float _shotDistance;
        private bool _surfaceHit;
        private readonly Transform[] _cases = new Transform[8];
        private readonly Vector3[] _caseVelocity = new Vector3[8];
        private readonly float[] _caseUntil = new float[8];
        private Material _brass;
        private int _caseCursor;
        public float LastShotTime { get; private set; } = -100;
        public bool HitConfirmed => Time.time < _hitUntil;
        public bool FlashVisible => _flash != null && _flash.activeSelf;
        public Vector3 TracerOrigin => _shotStart;
        public float VisibleTracerLength => _tracer != null && _tracer.enabled ? Vector3.Distance(_tracer.GetPosition(0),_tracer.GetPosition(1)) : 0;
        public Color ShotColor => _color;

        private void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false; _audio.spatialBlend = 0; _audio.volume = .65f;
            _audio.priority = 64;
            _pistol = MakeSound("Pistol report", 115, .17f);
            _rifle = MakeSound("Rifle report", 85, .12f);
            _material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Sprites/Default");
            var tracer = new GameObject("Weapon tracer");
            tracer.transform.SetParent(transform, false);
            _tracer = tracer.AddComponent<LineRenderer>();
            _tracer.sharedMaterial = _material;
            _tracer.positionCount = 2;
            _tracer.useWorldSpace = true;
            _tracer.enabled = false;
            _flash = Effect("Muzzle flash");
            _impact = Effect("Impact spark");
            _brass = RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit", "Standard");
            _brass.SetColor("_BaseColor",new Color(.63f,.40f,.12f));
            _brass.SetFloat("_Metallic",.7f); _brass.SetFloat("_Smoothness",.45f);
            for(int i=0;i<_cases.Length;i++)
            {
                var shell=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shell.name="Pooled spent cartridge";
                var c=shell.GetComponent<Collider>();c.enabled=false;Destroy(c);
                var r=shell.GetComponent<Renderer>();r.sharedMaterial=_brass;
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                shell.transform.localScale=new Vector3(.018f,.025f,.018f);
                shell.SetActive(false);_cases[i]=shell.transform;
            }
        }
        public void Configure(bool muted, bool reduced, bool rifle)
        {
            _muted = muted; _reduced = reduced; _isRifle = rifle;
            _color = new Color(1f,.74f,.34f);
            _material.SetColor("_BaseColor", _color);
            _tracer.startWidth = .012f; _tracer.endWidth = .004f;
            _audio.Stop(); _flash.SetActive(false); _impact.SetActive(false); _tracer.enabled = false;
            _flashUntil = _traceUntil = _hitUntil = 0;
            foreach(var shell in _cases)if(shell!=null)shell.gameObject.SetActive(false);
        }
        public void Fire(Transform muzzle, Vector3 fallbackOrigin, Vector3 end, bool hit, bool characterHit = false, bool surfaceHit = false)
        {
            _characterHit = characterHit;
            _muzzle = muzzle;
            _surfaceHit = surfaceHit || hit;
            LastShotTime = Time.time;
            _flashUntil = Time.time + .035f;
            _shotStart = muzzle != null ? muzzle.position : fallbackOrigin;
            _shotEnd = end; _shotDistance = Vector3.Distance(_shotStart,end);
            _traceUntil = Time.time + Mathf.Max(.055f, _shotDistance / 300f + .045f);
            _hitUntil = hit ? Time.time + .22f : 0;
            _tracer.SetPosition(0, _shotStart);
            _tracer.SetPosition(1, _shotStart);
            _impact.transform.position = end;
            _impact.transform.localScale = Vector3.one * .12f;
            if (!_muted) _audio.PlayOneShot(_isRifle ? _rifle : _pistol);
            if(!_reduced && muzzle!=null)
            {
                int i=_caseCursor++%_cases.Length;
                _cases[i].SetPositionAndRotation(muzzle.position-muzzle.forward*.18f,muzzle.rotation);
                _caseVelocity[i]=muzzle.right*Random.Range(1.2f,1.8f)+Vector3.up*Random.Range(1f,1.6f);
                _caseUntil[i]=Time.time+.7f;_cases[i].gameObject.SetActive(true);
            }
            LateUpdate();
        }
        private void LateUpdate()
        {
            var equipment = GetComponent<PlayerEquipmentVisual>();
            bool visible = equipment != null && equipment.IsHoldingWeapon && Time.timeScale > 0;
            _flash.SetActive(visible && !_reduced && Time.time < _flashUntil && _muzzle != null);
            _impact.SetActive(visible && !_reduced && !_characterHit && _surfaceHit && Time.time < LastShotTime+.065f);
            _tracer.enabled = visible && !_reduced && _shotDistance>.05f && Time.time < _traceUntil;
            if(_tracer.enabled)
            {
                float head=Mathf.Min(_shotDistance,Mathf.Max(.12f,(Time.time-LastShotTime)*300f));
                Vector3 direction=(_shotEnd-_shotStart).normalized;
                _tracer.SetPosition(0,_shotStart+direction*Mathf.Max(0,head-.65f));
                _tracer.SetPosition(1,_shotStart+direction*head);
            }
            if (_muzzle != null)
            {
                _flash.transform.SetPositionAndRotation(_muzzle.position, _muzzle.rotation);
                _flash.transform.localScale = new Vector3(.045f,.045f,Random.Range(.10f,.17f));
            }
            if (_impact.activeSelf) _impact.transform.localScale = Vector3.one * .035f;
            for(int i=0;i<_cases.Length;i++)
            {
                if(!_cases[i].gameObject.activeSelf)continue;
                if(Time.time>=_caseUntil[i]){_cases[i].gameObject.SetActive(false);continue;}
                if(Time.timeScale<=0)continue;
                _caseVelocity[i]+=Vector3.down*9.8f*Time.deltaTime;
                Vector3 step=_caseVelocity[i]*Time.deltaTime;
                if(Physics.Raycast(_cases[i].position,step.normalized,step.magnitude,~0,QueryTriggerInteraction.Ignore))
                {_cases[i].gameObject.SetActive(false);continue;}
                _cases[i].position+=step;_cases[i].Rotate(720*Time.deltaTime,360*Time.deltaTime,0,Space.Self);
            }
        }
        private GameObject Effect(string name)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.name = name;
            item.transform.SetParent(transform, false);
            var collider = item.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            item.GetComponent<Renderer>().sharedMaterial = _material;
            item.SetActive(false);
            return item;
        }
        private static AudioClip MakeSound(string name, float pitch, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate*duration)];
            var noise = new System.Random(420);
            for (int i=0; i<samples.Length; i++)
            {
                float t=(float)i/rate, u=t/duration;
                float envelope = Mathf.Min(1,t/.004f)*Mathf.Pow(1-u,2);
                float crack = (float)(noise.NextDouble()*2-1)*Mathf.Exp(-t*45);
                float body = Mathf.Sin(2*Mathf.PI*pitch*t)*Mathf.Exp(-t*30);
                samples[i] = Mathf.Clamp((crack*.78f + body*.3f)*envelope,-.9f,.9f);
            }
            var clip = AudioClip.Create(name,samples.Length,1,rate,false);
            clip.SetData(samples,0); return clip;
        }
        private void OnDestroy()
        {
            foreach(var shell in _cases)if(shell!=null)Destroy(shell.gameObject);
            Destroy(_material); Destroy(_brass); Destroy(_pistol); Destroy(_rifle);
        }
    }
}
