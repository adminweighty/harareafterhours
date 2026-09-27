using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarareAfterHours
{
    /// <summary>
    /// Builds the playable vertical slice at runtime. Keeping the first city
    /// procedural makes its draw count and memory footprint predictable while
    /// the art team replaces blocks with licensed, production-ready assets.
    /// </summary>
    public sealed class HarareAfterHoursBootstrap : MonoBehaviour
    {
        private const string VehiclePrefabResourceRoot = "HarareVehicles/Prefabs/";

        public static HarareAfterHoursBootstrap Instance { get; private set; }

        public ThirdPersonController Player { get; private set; }
        public VehicleController FeaturedVehicle { get; private set; }
        public MissionDirector Mission { get; private set; }
        public FlutterGameBridge Bridge { get; private set; }
        public PlayerProgression Progression { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public HarareArtLibrary ArtLibrary { get; private set; }
        public CharacterRoster Characters { get; private set; }

        private readonly List<Material> _runtimeMaterials = new();
        private bool _built;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        private void Start()
        {
            BuildWorld();
        }

        private void OnDestroy()
        {
            foreach (Material material in _runtimeMaterials)
            {
                if (material != null) Destroy(material);
            }
        }

        public void BuildWorld()
        {
            if (_built) return;
            _built = true;

            Random.InitState(20260921);
            ConfigureEveningLighting();

            GameObject world = new("Harare Evening District");
            CreateGroundAndRoads(world.transform);
            FirstStreetBlock.EnsurePresent(world.transform);
            CreateBuildings(world.transform);
            CreateNightMarket(world.transform);
            CreateStreetLights(world.transform);
            var expansion=new GameObject("Connected Harare avenue expansion").AddComponent<ExpandedCity>();
            expansion.transform.SetParent(world.transform);expansion.Build();
            var joina = new GameObject("Joina City opening district").AddComponent<JoinaCityBlock>();
            joina.transform.SetParent(world.transform);joina.Build();
            ArtLibrary = new GameObject("Supplied Art & Music").AddComponent<HarareArtLibrary>();
            ArtLibrary.transform.SetParent(world.transform);
            ArtLibrary.Build();
            Characters = new GameObject("Character Roster").AddComponent<CharacterRoster>();
            Characters.transform.SetParent(world.transform);
            Characters.Initialize();
            CreateTraffic(world.transform);
            CreatePedestrians(world.transform);
            CreateJoinaStreetLife(world.transform);

            Bridge = new GameObject("FlutterBridge").AddComponent<FlutterGameBridge>();
            Bridge.gameObject.transform.SetParent(transform);

            // v3.1 starts in the live CBD beside Rudo. Retire the older Joina
            // tutorial flag so arrest/restart also returns to the M01 district.
            PlayerPrefs.SetInt(JoinaCityBlock.IntroSaveKey, 1);
            Player = CreatePlayer(MissionDirector.OpeningSpawn);
            Progression = Player.gameObject.AddComponent<PlayerProgression>();
            Progression.Configure(Bridge);
            Combat = Player.gameObject.AddComponent<PlayerCombat>();
            Combat.Configure(Progression);
            FeaturedVehicle = CreateFeaturedVehicle(new Vector3(3f, 0.14f, 1.5f));
            CreateCamera(Player.transform);
            // Practice bags are not street characters; keep the live city clear.

            Mission = new GameObject("Mission Director").AddComponent<MissionDirector>();
            Mission.Configure(Player, FeaturedVehicle, Bridge, MissionDirector.ServiceYardPosition);

            Player.Configure(FeaturedVehicle, Mission, Bridge);
            Bridge.Bind(Player, Mission, Progression);

            GameplayHud hud = new GameObject("Gameplay HUD").AddComponent<GameplayHud>();
            hud.Configure(Player, FeaturedVehicle, Mission, Progression, Combat);
            var streetActions=new GameObject("Street action encounters").AddComponent<StreetActionDirector>();
            streetActions.transform.SetParent(world.transform);
            streetActions.Configure(Player,Progression,Bridge);
            var animator=Player.GetComponentInChildren<Animator>();
            if(animator!=null)animator.gameObject.AddComponent<StreetActionPose>();

            var presentation=new GameObject("Evening city presentation").AddComponent<ReferenceCityPresentation>();
            presentation.transform.SetParent(world.transform);presentation.Configure();
            var graphics=new GameObject("City graphics quality").AddComponent<CityGraphics>();
            graphics.transform.SetParent(world.transform);graphics.Configure();
            var trial=new GameObject("Avenue circuit time trial").AddComponent<CityTimeTrial>();
            trial.transform.SetParent(world.transform);trial.Configure(Player,Progression);
            Bridge.Publish("world_ready", "{\"district\":\"Harare CBD\",\"mode\":\"campaign\",\"missionId\":\"M01\",\"definitionVersion\":31,\"time\":\"18:30\"}");
        }

        private void ConfigureEveningLighting()
        {
            var rig = new GameObject("18:30 evening lighting rig").AddComponent<EveningLightingRig>();
            rig.transform.SetParent(transform, false);
            rig.Configure();
        }

        private void CreateGroundAndRoads(Transform parent) => GroundEnvironment.Build(parent);

        private void CreateBuildings(Transform parent)
        {
            bool firstStreetInstalled = FindFirstObjectByType<FirstStreetBlock>() != null;
            Material[] facades =
            {
                GroundEnvironment.Surface("WallGrey"),
                GroundEnvironment.Surface("WallWarm"),
                GroundEnvironment.Surface("WallCream"),
                GroundEnvironment.Surface("WallStone"),
            };
            Material roof = MakeMaterial(new Color(0.055f, 0.07f, 0.085f), 0.25f, 0.25f);
            Material window = MakeMaterial(new Color(0.18f, 0.25f, 0.29f), 0.3f, 0.72f);
            Material orangeWindow = MakeMaterial(new Color(0.28f, 0.30f, 0.28f), 0.2f, 0.62f);

            for (int x = -62; x <= 62; x += 20)
            {
                for (int z = -62; z <= 62; z += 20)
                {
                    if (Mathf.Abs(x) < 17 || Mathf.Abs(z) < 17) continue;

                    float width = Random.Range(9f, 15f);
                    float depth = Random.Range(9f, 15f);
                    float height = Random.Range(7f, 23f);
                    Vector3 position = new(x + Random.Range(-2.5f, 2.5f), height * 0.5f, z + Random.Range(-2.5f, 2.5f));
                    // Consume the exact legacy random sequence so every other
                    // plot and gameplay spawn keeps its original placement.
                    Transform buildingParent = parent;
                    if (firstStreetInstalled && x == FirstStreetBlock.LegacyGridX && z == FirstStreetBlock.LegacyGridZ)
                    {
                        GameObject replaced = new("Replaced First Street prototype");
                        replaced.transform.SetParent(parent);
                        replaced.SetActive(false);
                        buildingParent = replaced.transform;
                        Destroy(replaced);
                    }
                    CreateBox("Harare building", buildingParent, position, new Vector3(width, height, depth), facades[Random.Range(0, facades.Length)]);
                    CreateBox("Rooftop", buildingParent, position + Vector3.up * (height * 0.5f + 0.3f), new Vector3(width * 0.82f, 0.6f, depth * 0.82f), roof);
                    CityVenue.AddToPlot(buildingParent, x, z, position, width, depth);

                    // Keep genuine 3D facades. Full street photographs include
                    // sky, crowds and perspective and are not facade textures.
                    int columns = Mathf.Max(2, Mathf.FloorToInt(width / 3.5f));
                    int rows = Mathf.Clamp(Mathf.FloorToInt(height / 3.2f), 2, 6);
                    for (int row = 0; row < rows; row++)
                    {
                        for (int column = 0; column < columns; column++)
                        {
                            float localX = -width * 0.32f + column * (width * 0.64f / (columns - 1));
                            float localY = -height * 0.28f + row * (height * 0.56f / (rows - 1));
                            CreateBox(
                                "Lit office window",
                                buildingParent,
                                position + new Vector3(localX, localY, depth * 0.505f),
                                new Vector3(1.05f, 1.35f, 0.05f),
                                Random.value > 0.28f ? window : orangeWindow,
                                false
                            );
                        }
                    }
                }
            }
        }

        private void CreateNightMarket(Transform parent)
        {
            Material canopyOrange = MakeMaterial(new Color(0.95f, 0.23f, 0.1f), 0f, 0.35f);
            Material canopyLime = MakeMaterial(new Color(0.55f, 0.9f, 0.25f), 0f, 0.35f);
            Material stall = MakeMaterial(new Color(0.22f, 0.11f, 0.055f), 0f, 0.2f);
            Material sign = MakeMaterial(new Color(1f, 0.65f, 0.12f), 0.2f, 0.8f, true);

            for (int index = 0; index < 7; index++)
            {
                float x = 22f + index * 2.75f;
                float z = -30f + (index % 2) * 4.5f;
                CreateBox("Market stall", parent, new Vector3(x, 1f, z), new Vector3(2.25f, 2f, 1.9f), stall);
                CreateBox("Market canopy", parent, new Vector3(x, 2.25f, z), new Vector3(2.7f, 0.22f, 2.35f), index % 2 == 0 ? canopyOrange : canopyLime, false);
            }

            CreateBox("Mbare night market sign", parent, new Vector3(29.5f, 4.5f, -35.5f), new Vector3(18f, 2.8f, 0.35f), sign, false);
        }

        private void CreateStreetLights(Transform parent)
        {
            Material pole = MakeMaterial(new Color(0.08f, 0.095f, 0.12f), 0.7f, 0.65f);
            Material bulb = MakeMaterial(new Color(1f, 0.58f, 0.17f), 0.1f, 0.75f, true);
            var lightingRoot = new GameObject("Adaptive street lighting");
            lightingRoot.transform.SetParent(parent, false);
            var lighting = lightingRoot.AddComponent<AdaptiveStreetLighting>();

            // Light all three CBD roads, including the service-yard route. Lamp
            // geometry stays visible while the controller activates only the
            // nearest lights inside the mobile additional-light budget.
            for (int index = -60; index <= 60; index += 30)
            {
                CreateStreetLight(lightingRoot.transform, new Vector3(8.7f, 0f, index), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(-8.7f, 0f, index + 8f), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(index, 0f, 8.7f), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(index, 0f, -33.3f), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(index, 0f, 50.7f), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(-33.3f, 0f, index), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(50.7f, 0f, index), pole, bulb, lighting);
            }
            foreach (float distance in new[] { 90f, 150f, 210f })
            {
                CreateStreetLight(lightingRoot.transform, new Vector3(8.7f, 0f, distance), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(8.7f, 0f, -distance), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(distance, 0f, 8.7f), pole, bulb, lighting);
                CreateStreetLight(lightingRoot.transform, new Vector3(-distance, 0f, 8.7f), pole, bulb, lighting);
            }
        }

        private void CreateStreetLight(Transform parent, Vector3 position, Material pole, Material bulb, AdaptiveStreetLighting lighting)
        {
            GameObject lightRoot = new("Street light");
            lightRoot.transform.SetParent(parent);
            lightRoot.transform.position = position;
            // Unity's cylinder is two units tall: a Y scale of three gives
            // a six-metre pole, with the lantern at its top rather than halfway up.
            CreateChildPrimitive(PrimitiveType.Cylinder, "Pole", lightRoot.transform, new Vector3(0f, 3f, 0f), new Vector3(.16f, 3f, .16f), pole, false);
            CreateChildPrimitive(PrimitiveType.Cube, "Lamp arm", lightRoot.transform, new Vector3(.55f, 5.95f, 0f), new Vector3(1.2f, .09f, .09f), pole, false);
            CreateChildPrimitive(PrimitiveType.Cube, "Lamp", lightRoot.transform, new Vector3(1.08f, 5.85f, 0f), new Vector3(.48f, .10f, .24f), bulb, false);

            GameObject lampAnchor = new("Lamp light");
            lampAnchor.transform.SetParent(lightRoot.transform, false);
            lampAnchor.transform.localPosition = new Vector3(1.08f, 5.8f, 0f);
            lampAnchor.transform.localRotation = Quaternion.LookRotation(new Vector3(.16f, -1f, 0f));
            Light lamp = lampAnchor.AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.color = new Color(1f, 0.61f, 0.30f);
            lamp.intensity = 8.5f;
            lamp.range = 24f;
            lamp.spotAngle = 96f;
            lamp.innerSpotAngle = 54f;
            lamp.shadows = LightShadows.None;
            lighting.Register(lamp);
        }

        private ThirdPersonController CreatePlayer(Vector3 position)
        {
            GameObject player = new("Tari - Player");
            player.transform.position = position;

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.34f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.35f;

            AvatarCustomization avatar = player.AddComponent<AvatarCustomization>();
            avatar.BuildDefaultVisual();
            ThirdPersonController controller = player.AddComponent<ThirdPersonController>();
            return controller;
        }

        private VehicleController CreateFeaturedVehicle(Vector3 position)
        {
            GameObject car=InstantiateVehiclePrefab("Delivery");
            if(car==null)throw new System.InvalidOperationException("Install the shared SUV prefab before running.");
            car.name="Delivery SUV";car.transform.position=position;car.transform.rotation=Quaternion.Euler(0,90,0);
            var vehicle=car.GetComponent<VehicleController>();
            vehicle.CreateCameraAnchor();
            car.AddComponent<TrafficVehicle>().ConfigureParked(new[]{
                new Vector3(10,.025f,-3.1f),new Vector3(68,.025f,-3.1f),new Vector3(68,.025f,3.1f),new Vector3(-68,.025f,3.1f),new Vector3(-68,.025f,-3.1f)});
            return vehicle;
        }

        private GameObject CreateFallbackFeaturedVehicle()
        {
            GameObject car = new("Delivery Coupe fallback");

            Material body = MakeMaterial(new Color(0.94f, 0.14f, 0.08f), 0.68f, 0.82f);
            Material glass = MakeMaterial(new Color(0.08f, 0.55f, 0.7f), 0.35f, 0.9f, true);
            Material wheel = MakeMaterial(new Color(0.02f, 0.025f, 0.035f), 0.1f, 0.55f);
            Material headlight = MakeMaterial(new Color(0.98f, 0.92f, 0.67f), 0.1f, 0.85f, true);

            CreateChildPrimitive(PrimitiveType.Cube, "Car body", car.transform, new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 0.62f, 4.05f), body, false);
            CreateChildPrimitive(PrimitiveType.Cube, "Car cabin", car.transform, new Vector3(0f, 1.18f, -0.15f), new Vector3(1.55f, 0.68f, 2.05f), glass, false);
            CreateChildPrimitive(PrimitiveType.Cube, "Headlight left", car.transform, new Vector3(-0.55f, 0.72f, 2.04f), new Vector3(0.45f, 0.22f, 0.08f), headlight, false);
            CreateChildPrimitive(PrimitiveType.Cube, "Headlight right", car.transform, new Vector3(0.55f, 0.72f, 2.04f), new Vector3(0.45f, 0.22f, 0.08f), headlight, false);

            foreach (Vector3 wheelPosition in new[]
                     {
                         new Vector3(-1.02f, 0.36f, 1.32f), new Vector3(1.02f, 0.36f, 1.32f),
                         new Vector3(-1.02f, 0.36f, -1.32f), new Vector3(1.02f, 0.36f, -1.32f),
                     })
            {
                GameObject wheelObject = CreateChildPrimitive(PrimitiveType.Cylinder, "Wheel", car.transform, wheelPosition, new Vector3(0.58f, 0.22f, 0.58f), wheel, false);
                wheelObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            return car;
        }

        private void CreateCamera(Transform target)
        {
            // The URP template already supplies a camera and an AudioListener.
            // Reuse it so the scene always has exactly one listener.
            // Some URP template cameras are not tagged MainCamera, so look for
            // the existing Camera component rather than relying on Camera.main.
            Camera camera = null;
            Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera sceneCamera in sceneCameras)
            {
                if (sceneCamera == null || !sceneCamera.gameObject.scene.IsValid()) continue;
                camera = sceneCamera;
                break;
            }
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            else
            {
                camera.gameObject.tag = "MainCamera";
            }

            // A fresh URP scene can carry a template camera as well as the
            // gameplay camera. Keep just one render path and one listener.
            foreach (Camera sceneCamera in sceneCameras)
            {
                if (sceneCamera != camera) sceneCamera.enabled = false;
            }
            AudioListener desiredListener = camera.GetComponent<AudioListener>();
            if (desiredListener == null) desiredListener = camera.gameObject.AddComponent<AudioListener>();
            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                listener.enabled = listener == desiredListener;
            }

            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 180f;
            camera.allowHDR = true;
            camera.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;

            ThirdPersonCameraRig cameraRig = camera.GetComponent<ThirdPersonCameraRig>();
            if (cameraRig == null) cameraRig = camera.gameObject.AddComponent<ThirdPersonCameraRig>();
            cameraRig.SetTarget(target, false);
        }

        private void CreateTraffic(Transform parent)
        {
            Texture2D[] vehiclePhotos = Resources.LoadAll<Texture2D>("HarareArt/Vehicles");
            Vector3[] route =
            {
                new(-68f, 0.14f, -3.1f), new(68f, 0.14f, -3.1f), new(68f, 0.14f, 3.1f), new(-68f, 0.14f, 3.1f),
            };
            string[] prefabNames = { "Blue", "Ivory", "Burgundy" };
            Material[] colors =
            {
                MakeMaterial(new Color(0.22f, 0.65f, 0.9f), 0.65f, 0.78f),
                MakeMaterial(new Color(0.92f, 0.64f, 0.1f), 0.5f, 0.6f),
                MakeMaterial(new Color(0.2f, 0.78f, 0.48f), 0.55f, 0.72f),
            };

            for (int index = 0; index < 3; index++)
            {
                GameObject traffic = InstantiateVehiclePrefab(prefabNames[index]);
                if (traffic == null) traffic = CreateFallbackTrafficVehicle(index, vehiclePhotos, colors);
                traffic.name = $"Traffic vehicle {index + 1} — {prefabNames[index]}";
                traffic.transform.SetParent(parent);
                traffic.transform.position = route[index];
                TrafficVehicle vehicle = traffic.GetComponent<TrafficVehicle>();
                if (vehicle == null) vehicle = traffic.AddComponent<TrafficVehicle>();
                vehicle.Configure(route, index, 7f + index * 1.2f);
            }
        }

        private GameObject InstantiateVehiclePrefab(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(VehiclePrefabResourceRoot + "SharedSUV");
            if (prefab == null) return null;
            var instance=Instantiate(prefab);
            Color paint=prefabName=="Blue"?new Color(.08f,.22f,.36f):prefabName=="Ivory"?new Color(.73f,.69f,.58f):prefabName=="Burgundy"?new Color(.28f,.055f,.055f):new Color(.19f,.32f,.27f);
            instance.GetComponent<VehicleAppearance>()?.SetAppearance(paint);
            return instance;
        }

        private GameObject CreateFallbackTrafficVehicle(int index, Texture2D[] vehiclePhotos, Material[] colors)
        {
            GameObject traffic = new("Traffic vehicle fallback");
            CreateChildPrimitive(PrimitiveType.Cube, "Traffic body", traffic.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.55f, 0.55f, 3.2f), colors[index], false);
            CreateChildPrimitive(PrimitiveType.Cube, "Traffic cabin", traffic.transform, new Vector3(0f, 0.98f, -0.08f), new Vector3(1.2f, 0.5f, 1.65f), MakeMaterial(new Color(0.1f, 0.45f, 0.6f), 0.25f, 0.8f, true), false);
            if (vehiclePhotos.Length > 0)
            {
                CreateVehicleArtworkDecal(traffic.transform, vehiclePhotos[index % vehiclePhotos.Length]);
            }
            return traffic;
        }

        private void CreatePedestrians(Transform parent)
        {
            if (Characters != null && Characters.IsReady)
            {
                // The 23 NPC entries plus the playable Tari map one-for-one to
                // the supplied cast references. The source prefab owns its LOD
                // thresholds, so the city can carry this full cast on mobile.
                float[] sidewalks = { -8.6f, 8.6f, -34.6f, -49.4f, 34.6f, 49.4f };
                for (int index = 0; index < Characters.NpcCount; index++)
                {
                    float x = sidewalks[index % sidewalks.Length];
                    int row = index / sidewalks.Length;
                    float z = -52f + row * 27f + (index % 2 == 0 ? 0f : 2.4f);
                    Characters.SpawnNpc(parent, index, new Vector3(x, 0.02f, z), 3.1f, 0.62f + (index % 4) * 0.08f);
                }
                return;
            }

            CreateFallbackPedestrians(parent);
        }

        private void CreateJoinaStreetLife(Transform parent)
        {
            // A bounded addition: six sidewalk NPCs, two cars and one usable parked car.
            if (Characters.IsReady)
                for (int i=0;i<6 && i<Characters.NpcCount;i++)
                    Characters.SpawnNpc(parent,i,new Vector3(24+i*8,.15f,225),1.1f,.65f);
            Vector3[] route={new(-3,.15f,163),new(-3,.15f,207),new(83,.15f,207),new(83,.15f,213),new(3,.15f,213),new(3,.15f,163)};
            for(int i=0;i<2;i++)
            {
                var car=InstantiateVehiclePrefab(i==0?"Blue":"Ivory");if(car==null)continue;
                car.name="Joina avenue traffic";car.transform.SetParent(parent);
                car.AddComponent<TrafficVehicle>().Configure(route,i*2,5+i);
            }
            var parked=InstantiateVehiclePrefab("Burgundy");if(parked==null)return;
            parked.name="Joina arrival parked SUV";parked.transform.SetParent(parent);
            parked.transform.SetPositionAndRotation(new Vector3(16,.16f,190),Quaternion.Euler(0,180,0));
            parked.GetComponent<VehicleController>().CreateCameraAnchor();
            parked.AddComponent<TrafficVehicle>().ConfigureParked(route);
        }

        private void CreateFallbackPedestrians(Transform parent)
        {
            Texture2D[] characterPhotos = LoadPortraitImages();
            Material[] clothes =
            {
                MakeMaterial(new Color(0.83f, 0.22f, 0.12f), 0f, 0.35f),
                MakeMaterial(new Color(0.18f, 0.55f, 0.88f), 0f, 0.35f),
                MakeMaterial(new Color(0.45f, 0.78f, 0.25f), 0f, 0.35f),
            };
            Material skin = MakeMaterial(new Color(0.28f, 0.12f, 0.065f), 0f, 0.3f);

            for (int index = 0; index < 12; index++)
            {
                float x = index % 2 == 0 ? -8.6f : 8.6f;
                float z = -55f + index * 9.5f;
                GameObject pedestrian = CreatePrimitive(PrimitiveType.Capsule, "Night pedestrian", parent, new Vector3(x, 1f, z), new Vector3(0.48f, 1f, 0.48f), clothes[index % clothes.Length], false);
                CreateChildPrimitive(PrimitiveType.Sphere, "Head", pedestrian.transform, new Vector3(0f, 0.94f, 0f), Vector3.one * 0.58f, skin, false);
                if (characterPhotos.Length > 0)
                {
                    CreateCharacterPhotoBillboard(pedestrian.transform, characterPhotos[index % characterPhotos.Length]);
                }
                pedestrian.AddComponent<NpcWanderer>().Configure(new Vector3(x, 1f, z), 4.2f, 0.65f + (index % 3) * 0.1f);
            }
        }

        private void CreateCombatChallenges(Transform parent, PlayerProgression progression)
        {
            Material core = MakeMaterial(new Color(0.28f, 0.13f, 0.07f), 0f, 0.16f);
            Material frame = MakeMaterial(new Color(0.065f, 0.07f, 0.075f), 0.15f, 0.22f);
            Material ring = MakeMaterial(new Color(0.62f, 0.55f, 0.39f), 0f, 0.12f);
            Vector3[] positions =
            {
                new(4.5f, 0f, -0.5f),
                new(13f, 0f, -7f),
                new(-5f, 0f, -17f),
                new(-12f, 0f, 8f),
                new(7.5f, 0f, 17f),
                new(25f, 0f, -18f),
            };

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject challenge = new($"Street practice target {index + 1}");
                challenge.transform.SetParent(parent);
                challenge.transform.position = positions[index];

                CapsuleCollider collider = challenge.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 1.15f, 0f);
                collider.height = 2.3f;
                collider.radius = 0.52f;

                CreateChildPrimitive(PrimitiveType.Cylinder, "Weighted rubber base", challenge.transform, new Vector3(0f, .1f, 0f), new Vector3(.92f, .1f, .92f), frame, false);
                CreateChildPrimitive(PrimitiveType.Cylinder, "Target support", challenge.transform, new Vector3(0f, .48f, 0f), new Vector3(.16f, .3f, .16f), frame, false);
                CreateChildPrimitive(PrimitiveType.Capsule, "Padded practice bag", challenge.transform, new Vector3(0f, 1.43f, 0f), new Vector3(.62f, .83f, .62f), core, false);
                foreach(float height in new[]{.96f,1.85f})
                    CreateChildPrimitive(PrimitiveType.Cylinder, "Canvas reinforcement band", challenge.transform, new Vector3(0f,height,0f), new Vector3(.635f,.035f,.635f), ring, false);

                challenge.AddComponent<CombatTarget>().Configure(progression, 3);
            }
        }

        private Texture2D[] LoadPortraitImages()
        {
            List<Texture2D> portraits = new();
            foreach (Texture2D image in Resources.LoadAll<Texture2D>("HarareArt/References"))
            {
                if (image != null && image.height >= image.width * 1.1f) portraits.Add(image);
            }
            return portraits.ToArray();
        }

        private void CreateVehicleArtworkDecal(Transform parent, Texture2D vehiclePhoto)
        {
            float aspect = vehiclePhoto.width / Mathf.Max(1f, vehiclePhoto.height);
            float width = Mathf.Clamp(aspect * 0.55f, 0.72f, 1.35f);

            CreatePhotoSurface(
                "Moving vehicle artwork",
                parent,
                new Vector3(0.79f, 0.66f, 0f),
                new Vector3(width, 0.55f, 1f),
                vehiclePhoto,
                Quaternion.Euler(0f, 90f, 0f),
                true
            );
        }

        private void CreateCharacterPhotoBillboard(Transform parent, Texture2D portrait)
        {
            float aspect = portrait.width / Mathf.Max(1f, portrait.height);
            GameObject billboard = CreatePhotoSurface(
                "Animated character portrait",
                parent,
                new Vector3(0f, 0.95f, 0.32f),
                new Vector3(Mathf.Clamp(1.5f * aspect, 0.58f, 0.94f), 1.5f, 1f),
                portrait,
                Quaternion.identity,
                true
            );
            billboard.AddComponent<CameraFacingBillboard>();
        }

        private GameObject CreatePhotoSurface(string objectName, Transform parent, Vector3 position, Vector3 scale, Texture2D texture, Quaternion rotation, bool local = false)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = objectName;
            surface.transform.SetParent(parent);
            if (local)
            {
                surface.transform.localPosition = position;
                surface.transform.localRotation = rotation;
            }
            else
            {
                surface.transform.position = position;
                surface.transform.rotation = rotation;
            }
            surface.transform.localScale = scale;
            if (surface.TryGetComponent(out Collider collider)) collider.enabled = false;
            if (surface.TryGetComponent(out Renderer renderer)) renderer.sharedMaterial = MakePhotoMaterial(texture);
            return surface;
        }

        private Material MakeMaterial(Color color, float metallic = 0f, float smoothness = 0.35f, bool emissive = false)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Lit", "Standard");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.35f);
            }
            _runtimeMaterials.Add(material);
            return material;
        }

        private Material MakePhotoMaterial(Texture2D texture)
        {
            Material material = RuntimeMaterialFactory.Create("Universal Render Pipeline/Unlit", "Unlit/Texture");
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            _runtimeMaterials.Add(material);
            return material;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = true)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            if (gameObject.TryGetComponent(out Renderer renderer)) renderer.sharedMaterial = material;
            if (!keepCollider && gameObject.TryGetComponent(out Collider collider)) collider.enabled = false;
            return gameObject;
        }

        private static GameObject CreateBox(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = true)
        {
            return CreatePrimitive(PrimitiveType.Cube, objectName, parent, position, scale, material, keepCollider);
        }

        private static GameObject CreateChildPrimitive(PrimitiveType type, string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider = false)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = localScale;
            if (gameObject.TryGetComponent(out Renderer renderer)) renderer.sharedMaterial = material;
            if (!keepCollider && gameObject.TryGetComponent(out Collider collider)) collider.enabled = false;
            return gameObject;
        }
    }
}
