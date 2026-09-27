#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace HarareAfterHours.EditorTools
{
 public static class JacarandaMobileInstaller
 {
  const string Root="Assets/Environment/ReferenceRealism/";
  public static GameObject Install()
  {
   var leaf=Material("Jacaranda leaves","Harare/Photographic Foliage");
   leaf.SetTexture("_BaseMap",Texture("leaves_diff",false));leaf.SetTexture("_AlphaMap",Texture("leaves_alpha",true));leaf.SetFloat("_Cutoff",.4f);
   var bark=Material("Jacaranda bark","Universal Render Pipeline/Lit");bark.SetTexture("_BaseMap",Texture("trunk_diff",false));bark.SetColor("_BaseColor",Color.white);bark.SetFloat("_Smoothness",.12f);
   var root=new GameObject("Jacaranda mobile shade tree");var group=root.AddComponent<LODGroup>();var lods=new LOD[3];
   var oldRandom=Random.state;
   for(int level=0;level<3;level++)
   {
    Random.InitState(731);var tier=new GameObject("Jacaranda LOD"+level);tier.transform.SetParent(root.transform,false);
    var parts=new List<CombineInstance>();var temp=new List<GameObject>();
    Branch(Vector3.zero,new Vector3(.08f,3.5f,0),.15f,parts,temp);
    for(int b=0;b<5;b++){float angle=b*Mathf.PI*2/5;Branch(new Vector3(0,2.3f,0),new Vector3(Mathf.Cos(angle)*1.8f,4.7f,Mathf.Sin(angle)*1.8f),.065f,parts,temp);}
    var trunk=SaveMesh("Jacaranda bark LOD"+level);trunk.CombineMeshes(parts.ToArray());foreach(var go in temp)Object.DestroyImmediate(go);Add(tier.transform,trunk,bark);
    int count=new[]{420,200,85}[level];var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
    for(int i=0;i<count;i++)
    {
     Vector3 p=Random.insideUnitSphere;p=new Vector3(p.x*2.7f,4.85f+p.y*1.25f,p.z*2.7f);
     Quaternion r=Quaternion.Euler(Random.Range(25,155),Random.Range(0,360),Random.Range(0,360));float size=Random.Range(.85f,1.35f)*(level==2?1.4f:1);
     int first=vertices.Count;foreach(var corner in new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0)}){vertices.Add(p+r*corner*size*.5f);normals.Add(r*Vector3.forward);}
     uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
    }
    var mesh=SaveMesh("Jacaranda leaves LOD"+level);mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);Add(tier.transform,mesh,leaf);
    lods[level]=new LOD(new[]{.18f,.08f,.018f}[level],tier.GetComponentsInChildren<Renderer>());
   }
   Random.state=oldRandom;group.SetLODs(lods);group.RecalculateBounds();var collision=root.AddComponent<CapsuleCollider>();collision.radius=.18f;collision.height=3.5f;collision.center=Vector3.up*1.75f;
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"JacarandaMobile.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
   if(ShaderUtil.ShaderHasError(leaf.shader))throw new System.Exception("Foliage shader compilation failed");return prefab;
  }
  static Material Material(string name,string shader){string path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
  static Texture2D Texture(string name,bool mask){string path="Assets/Environment/Textures/Jacaranda/jacaranda_tree_"+name+"_1k."+(mask?"png":"jpg");var i=(TextureImporter)AssetImporter.GetAtPath(path);i.sRGBTexture=!mask;i.mipmapEnabled=true;i.maxTextureSize=1024;i.isReadable=false;i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Trilinear;foreach(string p in new[]{"iPhone","Android"})i.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name=p,overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6});i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  static Mesh SaveMesh(string name){string path=Root+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m==null){m=new Mesh();AssetDatabase.CreateAsset(m,path);}else m.Clear();EditorUtility.SetDirty(m);return m;}
  static void Add(Transform parent,Mesh mesh,Material material){var go=new GameObject(material.name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;}
  static void Branch(Vector3 a,Vector3 b,float radius,List<CombineInstance> parts,List<GameObject> temp){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.transform.position=(a+b)*.5f;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);go.transform.localScale=new Vector3(radius*2,(b-a).magnitude*.5f,radius*2);parts.Add(new CombineInstance{mesh=go.GetComponent<MeshFilter>().sharedMesh,transform=go.transform.localToWorldMatrix});temp.Add(go);}
 }
}
#endif
