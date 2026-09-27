using UnityEngine;
namespace HarareAfterHours
{
    public sealed class VehicleAppearance : MonoBehaviour
    {
        [SerializeField] private Color paint=new Color(.18f,.29f,.36f);
        [SerializeField] private Texture2D bodyTexture;
        public void SetAppearance(Color color,Texture2D texture=null){paint=color;bodyTexture=texture;Apply();}
        private void Awake()=>Apply();
        private void Apply()
        {
            var block=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(materials[i]!=null && materials[i].name.Contains("Vehicle_Paint"))
                {
                    renderer.GetPropertyBlock(block,i);block.SetColor("_BaseColor",paint);
                    if(bodyTexture!=null)block.SetTexture("_BaseMap",bodyTexture);
                    renderer.SetPropertyBlock(block,i);
                }
            }
        }
    }
}
