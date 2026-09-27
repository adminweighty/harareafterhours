using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace HarareAfterHours
{
    public sealed class ReferenceCityPresentation : MonoBehaviour
    {
        readonly List<Material> _signMaterials=new();
        public void Configure()
        {
            // Lighting belongs to EveningLightingRig. This presentation layer
            // adds only signs and a single local reflection capture.
            if(Camera.main!=null){Camera.main.clearFlags=CameraClearFlags.Skybox;Camera.main.backgroundColor=RenderSettings.fogColor;}
            var block=FindFirstObjectByType<FirstStreetBlock>();
            if(block!=null)foreach(var text in block.GetComponentsInChildren<TextMesh>())
            {
                // The default font shader draws through geometry, exposing
                // mirrored backfaces. Use depth-tested text on each board face.
                var material=new Material(Resources.Load<Shader>("HarareWorldText"));
                material.mainTexture=text.font.material.mainTexture;_signMaterials.Add(material);
                text.GetComponent<Renderer>().sharedMaterial=material;
                if(text.text=="First Street")
                {
                    var reverse=Instantiate(text.gameObject,text.transform.parent);reverse.name="First Street reverse lettering";
                    reverse.transform.localPosition=text.transform.localPosition+Vector3.right*.14f;
                    reverse.transform.localRotation=Quaternion.Euler(0,-90,0);
                }
            }
            StartCoroutine(CaptureReflection());
        }
        void OnDestroy(){foreach(var material in _signMaterials)if(material!=null)Destroy(material);}
        IEnumerator CaptureReflection()
        {
            // Capture once after world construction. No six-face render every frame.
            yield return null;yield return null;
            var obj=new GameObject("First Street 128px reflection");obj.transform.SetParent(transform,false);obj.transform.position=new Vector3(7.7f,3.2f,12);
            var probe=obj.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution=128;probe.size=new Vector3(65,30,65);probe.boxProjection=true;
            probe.nearClipPlane=.3f;probe.farClipPlane=90;probe.shadowDistance=0;probe.intensity=.8f;
            probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.RenderProbe();
        }
    }
}
