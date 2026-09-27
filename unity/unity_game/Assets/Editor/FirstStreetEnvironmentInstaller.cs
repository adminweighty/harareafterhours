#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HarareAfterHours.Editor;

namespace HarareAfterHours.EditorTools
{
    /// <summary>
    /// An incremental replacement of the existing (18,18) plot. Source modules
    /// are reusable prefabs; baked per-material meshes keep the draw count low.
    /// This tool updates only its own generated assets and labelled scene root.
    /// </summary>
    public static class FirstStreetEnvironmentInstaller
    {
        const string Root = "Assets/Environment";
        const string Output = "Assets/Resources/HarareEnvironment/FirstStreetCorner.prefab";
        const float W = FirstStreetBlock.Width;
        const float D = FirstStreetBlock.Depth;
        static readonly Dictionary<string, Material> Materials = new();
        static Mesh cube;
        static Font font;

        [MenuItem("Harare After Hours/Environment/Install First Street Reference Block")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before installing environment assets.");
            foreach (string dir in new[] { "Buildings", "Roads", "Props", "Vegetation", "Signs", "Materials", "Textures", "Prefabs/Modules", "ReferenceImages", "Reports" })
                Directory.CreateDirectory(Root + "/" + dir);
            Directory.CreateDirectory("Assets/Resources/HarareEnvironment");
            AssetDatabase.Refresh();
            RuntimeMaterialBuildSupport.EnsureTemplates();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = source.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(source);
            CreateMaterials();
            CreateModules();
            GameObject block = new("First Street - reference corner");
            block.AddComponent<FirstStreetBlock>();
            try
            {
                GameObject architecture = new("Bata frontage - reference 10");
                architecture.transform.SetParent(block.transform, false);
                LODGroup group = architecture.AddComponent<LODGroup>();
                LOD[] lods = new LOD[3];
                for (int level = 0; level < 3; level++)
                {
                    Geometry geometry = new();
                    BuildArchitecture(geometry, level);
                    GameObject lod = geometry.Save(architecture.transform, "FirstStreet_LOD" + level, "Buildings");
                    lods[level] = new LOD(new[] { .24f, .09f, .008f }[level], lod.GetComponentsInChildren<Renderer>());
                }
                group.SetLODs(lods);
                group.RecalculateBounds();
                AddCollision(block.transform);
                AddSigns(block.transform);
                AddStreet(block.transform);
                GameObject modelSocket = new("Future FBX replacement - metres +Y up +Z north");
                modelSocket.transform.SetParent(block.transform, false);
                foreach (Renderer renderer in block.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.allowOcclusionWhenDynamic = true;
                    // Never treat text, glass or thin props as solid occluders.
                    StaticEditorFlags flags = StaticEditorFlags.OccludeeStatic;
                    if (renderer.name == "Concrete" || renderer.name == "Stone") flags |= StaticEditorFlags.OccluderStatic;
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                }
                PrefabUtility.SaveAsPrefabAsset(block, Output);
                WriteAssetBudget(block);
            }
            finally { UnityEngine.Object.DestroyImmediate(block); }
            InstallInScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[FirstStreet] Installed one reference block; existing world layout and gameplay retained.");
        }

        static void CreateMaterials()
        {
            Materials.Clear();
            MakeMaterial("Stone", new Color(.63f, .56f, .45f), .22f);
            MakeMaterial("Concrete", new Color(.72f, .69f, .60f), .18f);
            MakeMaterial("Recess", new Color(.12f, .13f, .13f), .12f);
            MakeMaterial("Glass", new Color(.19f, .29f, .32f), .67f);
            MakeMaterial("GlassAlternate", new Color(.31f, .38f, .38f), .6f);
            MakeMaterial("Metal", new Color(.17f, .19f, .18f), .42f);
            MakeMaterial("Paving", new Color(.53f, .47f, .38f), .15f);
            MakeMaterial("PavingJoint", new Color(.36f, .33f, .28f), .12f);
            MakeMaterial("Kerb", new Color(.67f, .65f, .58f), .14f);
            MakeMaterial("StreetBlue", new Color(.025f, .14f, .28f), .26f);
            MakeMaterial("SignWhite", new Color(.86f, .84f, .76f), .23f);
            MakeMaterial("Wood", new Color(.32f, .19f, .105f), .16f);
            MakeMaterial("Leaf", new Color(.22f, .34f, .11f), .1f);
            MakeMaterial("LeafLight", new Color(.33f, .42f, .16f), .1f);
        }

        static void MakeMaterial(string name, Color color, float smoothness)
        {
            string path = Root + "/Materials/FirstStreet_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Material template = Resources.Load<Material>("HarareRuntime/LitFallback");
            if (template == null) throw new InvalidOperationException("Runtime URP material template missing.");
            if (material == null) { material = new Material(template); AssetDatabase.CreateAsset(material, path); }
            else material.shader = template.shader;
            material.name = "FirstStreet_" + name;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", name == "Metal" ? .3f : 0);
            material.DisableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            Materials[name] = material;
        }

        static void BuildArchitecture(Geometry g, int lod)
        {
            const float ground = 3.9f;
            const float top = 13.65f; // Ground + three upper floors; roof is outside the crop.
            g.Box("Stone", new Vector3(0, (ground + top) / 2, 0), new Vector3(W, top - ground, D));
            // Recess ground floor behind the two visible arcades.
            g.Box("Recess", new Vector3(1.2f, 2.04f, 1.2f), new Vector3(W - 2.4f, 3.72f, D - 2.4f));
            g.Box("Concrete", new Vector3(0, ground, 0), new Vector3(W + .18f, .3f, D + .18f));
            g.Box("Recess", new Vector3(0, top + .025f, 0), new Vector3(W - .45f, .1f, D - .45f));
            // Modest flat parapet: no invented towers, balconies or pitched roof.
            for (int face = 0; face < 4; face++)
            {
                float length = face % 2 == 0 ? W : D;
                float depth = face % 2 == 0 ? D : W;
                Quaternion rotation = Quaternion.Euler(0, face * 90, 0);
                Vector3 normal = rotation * Vector3.back;
                Vector3 origin = normal * depth * .5f;
                g.Box("Concrete", origin + Vector3.up * 13.95f, new Vector3(length, .6f, .25f), rotation);
                for (int floor = 0; floor < 3; floor++)
                {
                    float y = 4.42f + floor * 3.25f;
                    if (lod == 2)
                    {
                        g.Box("Glass", origin + normal * .015f + Vector3.up * (y + 1), new Vector3(length - .6f, 1.9f, .045f), rotation);
                        continue;
                    }
                    int bays = face % 2 == 0 ? 5 : 3;
                    float bayWidth = (length - .6f) / bays;
                    for (int bay = 0; bay < bays; bay++)
                    {
                        Vector3 p = origin + rotation * new Vector3(-length / 2 + .3f + (bay + .5f) * bayWidth, y + 1, -.055f);
                        Window(g, p, rotation, bayWidth - .3f, 1.95f, lod, (bay + floor) % 3 == 0);
                    }
                    g.Box("Concrete", origin + normal * .10f + Vector3.up * (y - .22f), new Vector3(length, .2f, .22f), rotation);
                    if (lod == 0)
                    {
                        for (int joint = 1; joint < Mathf.FloorToInt(length / 1.4f); joint++)
                            g.Box("PavingJoint", origin + rotation * new Vector3(-length / 2 + joint * 1.4f, y + 2.35f, -.008f), new Vector3(.015f, .46f, .02f), rotation);
                    }
                }
            }
            // Only the two street-facing sides receive reference-derived retail detail.
            for (int face = 0; face < 2; face++)
            {
                Quaternion rotation = Quaternion.Euler(0, face * 90, 0);
                float length = face == 0 ? W : D;
                float depth = face == 0 ? D : W;
                int bays = face == 0 ? 3 : 2;
                float bayWidth = length / bays;
                Vector3 origin = rotation * new Vector3(0, 0, -depth * .5f);
                for (int bay = 0; bay < bays; bay++)
                {
                    float retailLength = length - 2.4f;
                    Vector3 shift = face == 0 ? Vector3.right * 1.2f : Vector3.forward * 1.2f;
                    Vector3 p = origin + shift + rotation * new Vector3(-retailLength / 2 + (bay + .5f) * retailLength / bays, 0, 2.36f);
                    Shop(g, p, rotation, retailLength / bays - .26f, lod);
                }
                for (int column = 0; column <= bays; column++)
                    g.Box("Stone", origin + rotation * new Vector3(-length / 2 + .24f + column * (length - .48f) / bays, 2.08f, .28f), new Vector3(.4f, 3.8f, .42f), rotation);
                g.Box("Stone", origin + Vector3.up * 3.4f, new Vector3(length, .82f, .28f), rotation);
                g.Box("Concrete", origin + rotation * new Vector3(0, 3.05f, .9f), new Vector3(length, .13f, 2.1f), rotation);
                if (lod < 2) g.Box("Metal", origin + rotation * new Vector3(0, 3.11f, -.13f), new Vector3(length, .08f, .07f), rotation);
            }
        }

        static void Window(Geometry g, Vector3 p, Quaternion r, float width, float height, int lod, bool alternate)
        {
            g.Box("Recess", p, new Vector3(width + .13f, height + .14f, .12f), r);
            g.Box(alternate ? "GlassAlternate" : "Glass", p + r * new Vector3(0, 0, -.075f), new Vector3(width, height, .025f), r);
            if (lod > 0) return;
            g.Box("Metal", p + r * new Vector3(0, 0, -.095f), new Vector3(.055f, height, .045f), r);
            g.Box("Metal", p + r * new Vector3(0, .40f, -.095f), new Vector3(width, .045f, .045f), r);
            g.Box("Concrete", p + r * new Vector3(0, -height * .5f - .05f, -.06f), new Vector3(width + .18f, .12f, .26f), r);
        }

        static void Shop(Geometry g, Vector3 p, Quaternion r, float width, int lod)
        {
            g.Box("Metal", p + Vector3.up * 1.55f, new Vector3(width, 2.7f, .13f), r);
            g.Box("Glass", p + r * new Vector3(0, 1.60f, -.085f), new Vector3(width - .16f, 2.45f, .035f), r);
            if (lod == 2) return;
            foreach (float x in new[] { -.58f, .58f })
                g.Box("Metal", p + r * new Vector3(x, 1.5f, -.12f), new Vector3(.065f, 2.55f, .07f), r);
            g.Box("Metal", p + r * new Vector3(0, 2.48f, -.12f), new Vector3(width, .055f, .06f), r);
            g.Box("Kerb", p + r * new Vector3(0, .24f, -.10f), new Vector3(width, .12f, .4f), r);
            if (lod == 0)
                foreach (float x in new[] { -.43f, .43f })
                    g.Box("Kerb", p + r * new Vector3(x, 1.2f, -.18f), new Vector3(.035f, .32f, .05f), r);
        }

        static void AddCollision(Transform root)
        {
            ColliderBox(root, "Upper floors - simplified solid", new Vector3(0, 9.08f, 0), new Vector3(W, 10.16f, D));
            ColliderBox(root, "Closed shop interiors", new Vector3(1.2f, 2.04f, 1.2f), new Vector3(W - 2.4f, 3.72f, D - 2.4f));
            for (int face = 0; face < 2; face++)
            {
                Quaternion r = Quaternion.Euler(0, face * 90, 0);
                float length = face == 0 ? W : D;
                float depth = face == 0 ? D : W;
                int bays = face == 0 ? 3 : 2;
                for (int col = 0; col <= bays; col++)
                    ColliderBox(root, "Arcade pillar", r * new Vector3(-length / 2 + .24f + col * (length - .48f) / bays, 2.08f, -depth * .5f + .28f), new Vector3(.42f, 3.8f, .42f));
            }
        }

        static void AddSigns(Transform root)
        {
            Geometry signs = new();
            signs.Box("SignWhite", new Vector3(-1.25f, 3.45f, -D / 2 - .17f), new Vector3(5.2f, .72f, .08f));
            signs.Box("StreetBlue", World(6.65f, 2.9f, 12.8f), new Vector3(.10f, .44f, 1.65f));
            signs.Box("Metal", World(6.65f, 1.5f, 12.8f), new Vector3(.075f, 3f, .075f));
            signs.Save(root, "FirstStreet_SignBoards", "Signs");
            Text(root, "Bata", new Vector3(-1.25f, 3.45f, -D / 2 - .23f), Quaternion.identity, .075f, new Color(.66f, .08f, .045f), FontStyle.BoldAndItalic);
            Text(root, "First Street", World(6.58f, 2.9f, 12.8f), Quaternion.Euler(0, 90, 0), .022f, Color.white, FontStyle.Normal);
        }

        static Vector3 World(float x, float y, float z) => new Vector3(x, y, z) - FirstStreetBlock.PlotCentre;

        static void AddStreet(Transform root)
        {
            Geometry paving = new();
            // 180 mm pavement; original 12 m carriageways and their routes stay in place.
            paving.Box("Paving", World(16.15f, .105f, 17.10f), new Vector3(19.9f, .15f, 21.8f));
            paving.Box("Kerb", World(6.28f, .11f, 17.1f), new Vector3(.16f, .18f, 21.8f));
            paving.Box("Kerb", World(16.15f, .11f, 6.28f), new Vector3(19.9f, .18f, .16f));
            paving.Save(root, "FirstStreet_Pavement", "Roads");
            ColliderBox(root, "Pavement 180mm", World(16.15f, .105f, 17.1f), new Vector3(19.9f, .15f, 21.8f));

            Geometry joints = new();
            // Flat narrow strips are combined into two meshes, with no individual colliders.
            for (float z = 6.65f; z < 28; z += .55f)
                joints.Box("PavingJoint", World(8.35f, .187f, z), new Vector3(4.1f, .006f, .02f));
            for (float x = 6.65f; x < 10.5f; x += .65f)
                joints.Box("PavingJoint", World(x, .187f, 17.1f), new Vector3(.02f, .006f, 21.8f));
            for (float x = 10.7f; x < 26.1f; x += .65f)
                joints.Box("PavingJoint", World(x, .187f, 10.3f), new Vector3(.02f, .006f, 8f));
            for (float z = 6.65f; z < 14.3f; z += .55f)
                joints.Box("PavingJoint", World(18.3f, .187f, z), new Vector3(15.6f, .006f, .02f));
            GameObject details = joints.Save(root, "FirstStreet_PavingJoints", "Roads");
            DistanceLOD(details, .065f);

            Geometry furniture = new();
            Vector3 table = World(23.8f, 0, 12.3f);
            Table(furniture, table);
            GameObject stall = furniture.Save(root, "FirstStreet_VendorTable", "Props");
            DistanceLOD(stall, .035f);
            ColliderBox(root, "Vendor table collision", table + Vector3.up * .65f, new Vector3(1.6f, .94f, .68f));

            GameObject tree = new("FirstStreet_ShadeTree");
            tree.transform.SetParent(root, false);
            tree.transform.localPosition = World(25.0f, 0, 8.5f);
            LODGroup lodGroup = tree.AddComponent<LODGroup>();
            LOD[] levels = new LOD[3];
            for (int level = 0; level < 3; level++)
            {
                Geometry g = new();
                g.Box("Wood", new Vector3(0, 2.3f, 0), new Vector3(.22f, 4.3f, .23f));
                int clusters = level == 0 ? 5 : level == 1 ? 3 : 1;
                for (int i = 0; i < clusters; i++)
                {
                    float angle = i * Mathf.PI * 2 / clusters;
                    Vector3 offset = clusters == 1 ? Vector3.zero : new Vector3(Mathf.Cos(angle), (i % 2) * .35f, Mathf.Sin(angle)) * 1.1f;
                    g.Ellipsoid(i % 2 == 0 ? "Leaf" : "LeafLight", new Vector3(0, 5.3f, 0) + offset, new Vector3(clusters == 1 ? 2.7f : 1.9f, 1.5f, clusters == 1 ? 2.7f : 1.9f), level == 0 ? 10 : 6, 5);
                }
                var mesh = g.Save(tree.transform, "FirstStreet_Tree_LOD" + level, "Vegetation");
                levels[level] = new LOD(new[] { .16f, .065f, .015f }[level], mesh.GetComponentsInChildren<Renderer>());
            }
            lodGroup.SetLODs(levels);
            lodGroup.RecalculateBounds();
            ColliderBox(tree.transform, "Tree trunk", new Vector3(0, 2.3f, 0), new Vector3(.3f, 4.3f, .3f));
        }

        static void Table(Geometry g, Vector3 p)
        {
            g.Box("Wood", p + Vector3.up * 1.05f, new Vector3(1.6f, .10f, .68f));
            foreach (float x in new[] { -.67f, .67f })
                foreach (float z in new[] { -.24f, .24f })
                    g.Box("Wood", p + new Vector3(x, .62f, z), new Vector3(.06f, .85f, .06f));
            for (int i = 0; i < 6; i++)
                g.Box(i % 2 == 0 ? "StreetBlue" : "SignWhite", p + new Vector3(-.61f + i * .24f, 1.16f, 0), new Vector3(.20f, .13f, .31f));
        }

        static void Text(Transform parent, string value, Vector3 p, Quaternion r, float scale, Color color, FontStyle style)
        {
            GameObject go = new(value + " lettering");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.transform.localRotation = r;
            TextMesh text = go.AddComponent<TextMesh>();
            text.text = value;
            text.font = font;
            text.fontSize = 64;
            text.fontStyle = style;
            text.characterSize = scale;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            DistanceLOD(go, .025f);
        }

        static void ColliderBox(Transform parent, string name, Vector3 p, Vector3 size)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.AddComponent<BoxCollider>().size = size;
        }

        static void DistanceLOD(GameObject go, float threshold)
        {
            var group = go.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(threshold, go.GetComponentsInChildren<Renderer>()) });
            group.RecalculateBounds();
        }

        static void CreateModules()
        {
            SaveModule("OfficeWindow", g => Window(g, Vector3.zero, Quaternion.identity, 2.45f, 1.95f, 0, false));
            SaveModule("Shopfront", g => Shop(g, Vector3.zero, Quaternion.identity, 4.6f, 0));
            SaveModule("ArcadePillar", g => g.Box("Stone", new Vector3(0, 1.9f, 0), new Vector3(.4f, 3.8f, .42f)));
            SaveModule("Parapet", g => g.Box("Concrete", new Vector3(0, .3f, 0), new Vector3(3, .6f, .25f)));
            SaveModule("Kerb", g => g.Box("Kerb", new Vector3(0, .09f, 0), new Vector3(1, .18f, .16f)));
            SaveModule("VendorTable", g => Table(g, Vector3.zero));
        }

        static void SaveModule(string name, Action<Geometry> build)
        {
            Geometry g = new();
            build(g);
            GameObject go = g.Save(null, "Module_" + name, "Prefabs/Modules");
            PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/Modules/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void InstallInScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var existing = UnityEngine.Object.FindFirstObjectByType<FirstStreetBlock>();
            if (existing != null)
            {
                // Keep prefab overrides; normal prefab reimport updates unchanged properties.
                if (!PrefabUtility.IsPartOfPrefabInstance(existing.gameObject))
                    throw new InvalidOperationException("FirstStreetBlock is unpacked. Preserve manual edits before reinstalling.");
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.position = FirstStreetBlock.PlotCentre;
                var area = instance.AddComponent<OcclusionArea>();
                area.center = new Vector3(0, 8, 0);
                area.size = new Vector3(65, 25, 65);
            }
            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.useOcclusionCulling = true;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            StaticOcclusionCulling.smallestOccluder = 3;
            StaticOcclusionCulling.smallestHole = .5f;
            if (!StaticOcclusionCulling.Compute()) throw new InvalidOperationException("First Street occlusion bake failed.");
            EditorSceneManager.SaveScene(scene);
        }

        static void WriteAssetBudget(GameObject block)
        {
            var lines = new List<string> { "First Street asset budget (not a device frame-time benchmark)" };
            foreach (LODGroup group in block.GetComponentsInChildren<LODGroup>())
            {
                int index = 0;
                foreach (LOD lod in group.GetLODs())
                {
                    int triangles = lod.renderers.Where(r => r is MeshRenderer && r.GetComponent<MeshFilter>() != null)
                        .Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);
                    lines.Add($"{group.name} LOD{index++}: {triangles} triangles, {lod.renderers.Length} renderers, threshold {lod.screenRelativeTransitionHeight}");
                }
            }
            lines.Add($"Colliders: {block.GetComponentsInChildren<Collider>().Length} (boxes only)");
            lines.Add($"Added real-time lights: {block.GetComponentsInChildren<Light>().Length}");
            lines.Add($"Shared surface materials: {Materials.Count}; text uses one shared font atlas");
            File.WriteAllLines(Root + "/Reports/FirstStreet-asset-budget.txt", lines);
        }

        sealed class Geometry
        {
            readonly Dictionary<string, List<CombineInstance>> parts = new();
            readonly List<Mesh> temporary = new();
            public void Box(string material, Vector3 p, Vector3 size, Quaternion? rotation = null)
                => Add(material, cube, Matrix4x4.TRS(p, rotation ?? Quaternion.identity, size));
            void Add(string material, Mesh mesh, Matrix4x4 matrix)
            {
                if (!parts.TryGetValue(material, out var list)) parts[material] = list = new();
                list.Add(new CombineInstance { mesh = mesh, transform = matrix });
            }
            public void Ellipsoid(string material, Vector3 p, Vector3 radii, int segments, int rings)
            {
                List<Vector3> vertices = new();
                List<int> indices = new();
                for (int y = 0; y <= rings; y++)
                {
                    float phi = Mathf.PI * y / rings;
                    for (int x = 0; x <= segments; x++)
                    {
                        float theta = Mathf.PI * 2 * x / segments;
                        vertices.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)));
                    }
                }
                for (int y = 0; y < rings; y++)
                    for (int x = 0; x < segments; x++)
                    {
                        int a = y * (segments + 1) + x, b = a + segments + 1;
                        indices.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                    }
                Mesh mesh = new();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                temporary.Add(mesh);
                Add(material, mesh, Matrix4x4.TRS(p, Quaternion.identity, radii));
            }
            public GameObject Save(Transform parent, string name, string folder)
            {
                GameObject root = new(name);
                root.transform.SetParent(parent, false);
                foreach (var entry in parts)
                {
                    Mesh mesh = new() { name = name + "_" + entry.Key };
                    mesh.CombineMeshes(entry.Value.ToArray(), true, true);
                    mesh.RecalculateBounds();
                    string path = Root + "/" + folder + "/" + mesh.name + ".asset";
                    Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (asset == null) { AssetDatabase.CreateAsset(mesh, path); asset = mesh; }
                    else { EditorUtility.CopySerialized(mesh, asset); UnityEngine.Object.DestroyImmediate(mesh); }
                    GameObject go = new(entry.Key);
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = asset;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = Materials[entry.Key];
                    renderer.shadowCastingMode = entry.Key == "Glass" || entry.Key == "GlassAlternate" ? ShadowCastingMode.Off : ShadowCastingMode.On;
                }
                foreach (Mesh mesh in temporary) UnityEngine.Object.DestroyImmediate(mesh);
                return root;
            }
        }
    }
}
#endif
