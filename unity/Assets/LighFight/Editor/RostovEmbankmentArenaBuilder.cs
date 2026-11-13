using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LighFight.Runtime;
using LighFight.Core;

namespace LighFight.Editor
{
    public static class RostovEmbankmentArenaBuilder
    {
        private const string Folder = "Assets/LighFight/Art/Arenas/RostovEmbankment";
        private const string PrefabPath = Folder + "/RostovEmbankmentArena.prefab";
        private const string PreviewDir = Folder + "/Previews";

        private static Texture2D LoadTexture(string filename, bool isNormal = false)
        {
            string path = Folder + "/Textures/" + filename;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                bool changed = false;
                if (isNormal && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    changed = true;
                }
                if (importer.wrapMode != TextureWrapMode.Repeat)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreatePbrMaterial(string name, string albedo, string normal, string puddle, float smoothness, float metallic, float wetness, float tiling)
        {
            string path = Folder + "/" + name + ".mat";
            var shader = Shader.Find("LighFight/RostovEmbankmentPBR") ?? Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (!string.IsNullOrEmpty(albedo))
            {
                mat.SetTexture("_MainTex", LoadTexture(albedo, false));
                mat.SetTextureScale("_MainTex", new Vector2(tiling, tiling));
            }
            if (!string.IsNullOrEmpty(normal))
            {
                mat.SetTexture("_BumpMap", LoadTexture(normal, true));
                mat.SetTextureScale("_BumpMap", new Vector2(tiling, tiling));
            }
            if (!string.IsNullOrEmpty(puddle))
            {
                mat.SetTexture("_PuddleMap", LoadTexture(puddle, false));
                mat.SetTextureScale("_PuddleMap", new Vector2(tiling, tiling));
            }
            mat.SetFloat("_Glossiness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Wetness", wetness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateWaterMaterial()
        {
            string path = Folder + "/Mat_DonRiverWater.mat";
            var shader = Shader.Find("LighFight/RostovWater") ?? Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BumpMap", LoadTexture("water_normal.png", true));
            mat.SetColor("_DeepColor", new Color(0.12f, 0.28f, 0.38f)); // Natural deep daytime Don River green-blue
            mat.SetColor("_ShallowColor", new Color(0.28f, 0.48f, 0.54f)); // Sunlit shallow river water
            mat.SetFloat("_Glossiness", 0.96f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateGarlandMaterial()
        {
            string path = Folder + "/Mat_BridgeFestiveGarlands.mat";
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_Color", new Color(1.8f, 1.5f, 1.0f)); // Warm golden-white glowing LED
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateBackdropMaterial()
        {
            string path = Folder + "/SunsetBackdrop.mat";
            var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_MainTex", LoadTexture("sunset_sky.png", false));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh LoadMesh(string filename)
        {
            string path = Folder + "/Meshes/" + filename;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go)
            {
                var filter = go.GetComponentInChildren<MeshFilter>();
                if (filter) return filter.sharedMesh;
            }
            return null;
        }

        public static void Validate(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (r.name == "CombatFloor" || r.name.StartsWith("FloorTile") || r.name.StartsWith("Walkable")) continue;
                var b = r.bounds;
                if (b.max.x > -5.5f && b.min.x < 5.5f && b.max.z > -2.0f && b.min.z < 2.0f && b.max.y > 0.05f && b.min.y < 2.85f)
                {
                    throw new InvalidOperationException("Decoration enters 11x4m fight volume: " + r.name + " bounds=" + b);
                }
            }
        }

        public static GameObject Build()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(PreviewDir);

            // 1. Physically-Accurate PBR Materials with Wetness / Puddle Mask
            var graniteMat = CreatePbrMaterial("Mat_GranitePromenade", "granite_albedo.png", "granite_normal.png", "granite_puddle_mask.png", 0.40f, 0.0f, 0.85f, 1.4f);
            var castIronMat = CreatePbrMaterial("Mat_CastIronAntique", "cast_iron_albedo.png", "cast_iron_normal.png", "", 0.65f, 0.85f, 0.0f, 1.10f);
            var waterMat = CreateWaterMaterial();
            var backdropMat = CreateBackdropMaterial();
            var garlandMat = CreateGarlandMaterial();

            var steelMat = CreatePbrMaterial("Mat_BridgeSteelTruss", "cast_iron_albedo.png", "cast_iron_normal.png", "", 0.70f, 0.90f, 0.0f, 0.8f);
            steelMat.SetColor("_Color", new Color(0.35f, 0.40f, 0.48f));

            var concreteMat = CreatePbrMaterial("Mat_BridgePylonConcrete", "granite_albedo.png", "granite_normal.png", "", 0.35f, 0.05f, 0.0f, 0.6f);
            concreteMat.SetColor("_Color", new Color(0.85f, 0.86f, 0.88f));

            var glassMat = CreatePbrMaterial("Mat_ElevatorGlass", "", "", "", 0.96f, 0.25f, 0.0f, 1.0f);
            glassMat.SetColor("_Color", new Color(0.18f, 0.46f, 0.54f, 0.75f));

            var arenaMat = CreatePbrMaterial("Mat_RostovArenaMembrane", "stadium_facade.png", "", "", 0.85f, 0.15f, 0.0f, 1.0f);
            arenaMat.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f)); // Clean daylight architectural white/silver
            arenaMat.DisableKeyword("_EMISSION");
            arenaMat.SetColor("_EmissionColor", Color.black);

            var fiveSeasMat = CreatePbrMaterial("Mat_FiveSeasFacade", "", "", "", 0.92f, 0.40f, 0.0f, 1.0f);
            fiveSeasMat.SetColor("_Color", new Color(0.12f, 0.28f, 0.46f));

            var trafficMat = CreatePbrMaterial("Mat_BridgeTraffic", "", "", "", 0.80f, 0.65f, 0.0f, 1.0f);
            trafficMat.SetColor("_Color", new Color(0.25f, 0.28f, 0.35f));

            var navSignMat = CreatePbrMaterial("Mat_NavSign", "navigation_sign.png", "", "", 0.60f, 0.1f, 0.0f, 1.0f);
            var lifebuoyMat = CreatePbrMaterial("Mat_Lifebuoy", "", "", "", 0.45f, 0.0f, 0.0f, 1.0f);
            lifebuoyMat.SetColor("_Color", new Color(0.95f, 0.35f, 0.15f));

            var shorelineMat = CreatePbrMaterial("Mat_LevberdonShoreline", "granite_albedo.png", "granite_normal.png", "", 0.25f, 0.05f, 0.0f, 0.6f);
            shorelineMat.SetColor("_Color", new Color(0.76f, 0.68f, 0.52f)); // Sunlit river sand & promenade stone

            var treesMat = CreatePbrMaterial("Mat_LevberdonTrees", "", "", "", 0.20f, 0.0f, 0.0f, 1.0f);
            treesMat.SetColor("_Color", new Color(0.24f, 0.45f, 0.18f)); // Natural lush Russian green, zero pink!

            var root = new GameObject("Rostov Embankment Arena");
            root.AddComponent<OfficeArena>().UseSharedMaterials();

            // 2. High-Poly Combat Stage Floor (18x8m) with wet granite PBR & puddles
            var floorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorGo.name = "CombatFloor";
            floorGo.transform.SetParent(root.transform, false);
            floorGo.transform.localPosition = new Vector3(0, -0.06f, 0);
            floorGo.transform.localScale = new Vector3(20, 0.12f, 8);
            var floorR = floorGo.GetComponent<Renderer>();
            floorR.sharedMaterial = graniteMat;
            floorR.receiveShadows = true;
            floorR.shadowCastingMode = ShadowCastingMode.On;
            UnityEngine.Object.DestroyImmediate(floorGo.GetComponent<Collider>());

            // 2b. Expansive 150m Navigable Promenade and Upper Terrace
            var promenadeMesh = LoadMesh("WalkableEmbankmentPromenade.obj");
            if (promenadeMesh)
            {
                var promGo = new GameObject("WalkableEmbankmentPromenade");
                promGo.transform.SetParent(root.transform, false);
                promGo.transform.localPosition = Vector3.zero;
                var mf = promGo.AddComponent<MeshFilter>();
                mf.sharedMesh = promenadeMesh;
                var mr = promGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = graniteMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;

                // Physical Colliders for Free-Roam Character Exploration:
                // Lower Promenade floor collider: 150m x 0.2m x 16.4m
                var floorCol = promGo.AddComponent<BoxCollider>();
                floorCol.center = new Vector3(0, -0.10f, -5.8f);
                floorCol.size = new Vector3(150.0f, 0.20f, 16.4f);

                // Upper Terrace floor collider along Beregovaya: 150m x 0.2m x 14.0m at Y = 2.7m
                var upperCol = promGo.AddComponent<BoxCollider>();
                upperCol.center = new Vector3(0, 2.70f, -21.0f);
                upperCol.size = new Vector3(150.0f, 0.20f, 14.0f);

                // Retaining wall barrier collider along Z = -14.0m
                var wallColLeft = promGo.AddComponent<BoxCollider>();
                wallColLeft.center = new Vector3(-54.0f, 1.40f, -14.0f);
                wallColLeft.size = new Vector3(42.0f, 2.80f, 0.8f);

                var wallColCenter = promGo.AddComponent<BoxCollider>();
                wallColCenter.center = new Vector3(0.0f, 1.40f, -14.0f);
                wallColCenter.size = new Vector3(48.0f, 2.80f, 0.8f);

                var wallColRight = promGo.AddComponent<BoxCollider>();
                wallColRight.center = new Vector3(54.0f, 1.40f, -14.0f);
                wallColRight.size = new Vector3(42.0f, 2.80f, 0.8f);

                // Grand staircases ramp colliders at X = ±32m
                foreach (float sx in new[] { -32.0f, 32.0f })
                {
                    var stairRamp = new GameObject("StairRamp_" + sx);
                    stairRamp.transform.SetParent(promGo.transform, false);
                    stairRamp.transform.localPosition = new Vector3(sx, 1.45f, -11.5f);
                    stairRamp.transform.localRotation = Quaternion.Euler(-26.5f, 0, 0);
                    var col = stairRamp.AddComponent<BoxCollider>();
                    col.size = new Vector3(8.0f, 0.25f, 6.2f);
                }
            }

            var railingsMesh = LoadMesh("WalkableParapetRailings.obj");
            if (railingsMesh)
            {
                var railGo = new GameObject("WalkableParapetRailings");
                railGo.transform.SetParent(root.transform, false);
                railGo.transform.localPosition = Vector3.zero;
                var mf = railGo.AddComponent<MeshFilter>();
                mf.sharedMesh = railingsMesh;
                var mr = railGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = castIronMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;

                // River barrier collider along the 150m promenade balustrade (Z = 2.4m)
                var riverCol = railGo.AddComponent<BoxCollider>();
                riverCol.center = new Vector3(0, 0.65f, 2.4f);
                riverCol.size = new Vector3(150.0f, 1.30f, 0.4f);
            }

            // 3. Modular Sculpted Parapet Sections along river promenade (Z = 2.32m)
            var parapetMesh = LoadMesh("RostovParapetSection.obj");
            if (parapetMesh)
            {
                for (int i = -6; i <= 6; i++)
                {
                    var pSection = new GameObject("ParapetSection_" + i);
                    pSection.transform.SetParent(root.transform, false);
                    pSection.transform.localPosition = new Vector3(i * 2.0f, 0, 2.32f);
                    var mf = pSection.AddComponent<MeshFilter>();
                    mf.sharedMesh = parapetMesh;
                    var mr = pSection.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = castIronMat;
                    mr.receiveShadows = true;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                }
            }

            // 4. Historical Cast-Iron Mooring Bollards (outside fight volume at X = ±6.2m, Z = 2.32m)
            foreach (float bx in new[] { -6.2f, 6.2f })
            {
                var bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bollard.name = "MooringBollard_" + (bx < 0 ? "Left" : "Right");
                bollard.transform.SetParent(root.transform, false);
                bollard.transform.localPosition = new Vector3(bx, 0.35f, 2.32f);
                bollard.transform.localScale = new Vector3(0.40f, 0.35f, 0.40f);
                var bR = bollard.GetComponent<Renderer>();
                bR.sharedMaterial = castIronMat;
                bR.receiveShadows = true;
                bR.shadowCastingMode = ShadowCastingMode.On;
                UnityEngine.Object.DestroyImmediate(bollard.GetComponent<Collider>());
            }

            // 5. Vintage Rostov 3-Globe Fluted Streetlamps spaced across the 150m Promenade
            var lampMesh = LoadMesh("RostovStreetlamp.obj");
            if (lampMesh)
            {
                foreach (float lx in new[] { -54.0f, -36.0f, -18.0f, -7.2f, 7.2f, 18.0f, 36.0f, 54.0f })
                {
                    string pfx = "Streetlamp_" + lx.ToString("F0");
                    var lampGo = new GameObject(pfx);
                    lampGo.transform.SetParent(root.transform, false);
                    lampGo.transform.localPosition = new Vector3(lx, 0, 2.28f);
                    var mf = lampGo.AddComponent<MeshFilter>();
                    mf.sharedMesh = lampMesh;
                    var mr = lampGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = castIronMat;
                    mr.receiveShadows = true;
                    mr.shadowCastingMode = ShadowCastingMode.On;

                    var plGo = new GameObject(pfx + "_PointLight");
                    plGo.transform.SetParent(lampGo.transform, false);
                    plGo.transform.localPosition = new Vector3(0, 4.2f, 0);
                    var pl = plGo.AddComponent<Light>();
                    pl.type = LightType.Point;
                    pl.color = new Color(1.0f, 0.86f, 0.60f);
                    pl.intensity = 1.8f;
                    pl.range = 8.5f;
                }
            }

            // 6. River Don Water Plane (Animated dual-wave normal shader spanning southwards)
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterGo.name = "DonRiverWater";
            waterGo.transform.SetParent(root.transform, false);
            waterGo.transform.localPosition = new Vector3(0, -0.42f, 105.0f);
            waterGo.transform.localScale = new Vector3(24, 1, 21.0f);
            var waterR = waterGo.GetComponent<Renderer>();
            waterR.sharedMaterial = waterMat;
            waterR.receiveShadows = true;
            UnityEngine.Object.DestroyImmediate(waterGo.GetComponent<Collider>());

            // 7. VOROSHILOVSKY BRIDGE OVERHEAD (Twin roadway spans crossing directly above at Y = 21m)
            var bridgeMesh = LoadMesh("VoroshilovskyBridgeOverhead.obj");
            if (bridgeMesh)
            {
                var brGo = new GameObject("VoroshilovskyBridgeOverhead");
                brGo.transform.SetParent(root.transform, false);
                brGo.transform.localPosition = Vector3.zero;
                var mf = brGo.AddComponent<MeshFilter>();
                mf.sharedMesh = bridgeMesh;
                var mr = brGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = steelMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;

                // Under-bridge LED Soffit Spotlights (Dramatically illuminating wet puddles from overhead)
                foreach (float sx in new[] { -5.0f, 0.0f, 5.0f })
                {
                    var spotGo = new GameObject("BridgeSoffitLight_" + sx);
                    spotGo.transform.SetParent(brGo.transform, false);
                    spotGo.transform.localPosition = new Vector3(sx, 19.5f, 0);
                    spotGo.transform.rotation = Quaternion.Euler(90, 0, 0);
                    var spot = spotGo.AddComponent<Light>();
                    spot.type = LightType.Spot;
                    spot.color = new Color(0.92f, 0.96f, 1.0f);
                    spot.intensity = 2.2f;
                    spot.range = 28.0f;
                    spot.spotAngle = 65.0f;
                    spot.shadows = LightShadows.Soft;
                }
            }

            // 8. Overhead Bridge Traffic Vehicles (Bus & Sedans on bridge deck Y=22m)
            var trafficMesh = LoadMesh("BridgeTrafficVehicles.obj");
            if (trafficMesh)
            {
                var trGo = new GameObject("BridgeTrafficVehicles");
                trGo.transform.SetParent(root.transform, false);
                trGo.transform.localPosition = Vector3.zero;
                var mf = trGo.AddComponent<MeshFilter>();
                mf.sharedMesh = trafficMesh;
                var mr = trGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = trafficMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            // 9. Shipping Navigation Rhombus Signs on Bridge Pier (Y=16.5m)
            var navMesh = LoadMesh("NavigationSignsOverhead.obj");
            if (navMesh)
            {
                var navGo = new GameObject("NavigationSignsOverhead");
                navGo.transform.SetParent(root.transform, false);
                navGo.transform.localPosition = Vector3.zero;
                var mf = navGo.AddComponent<MeshFilter>();
                mf.sharedMesh = navMesh;
                var mr = navGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = navSignMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            // 10. Lifebuoy on Parapet Railing (Z=2.32m)
            var buoyMesh = LoadMesh("ParapetLifebuoy.obj");
            if (buoyMesh)
            {
                var buoyGo = new GameObject("ParapetLifebuoy");
                buoyGo.transform.SetParent(root.transform, false);
                buoyGo.transform.localPosition = Vector3.zero;
                var mf = buoyGo.AddComponent<MeshFilter>();
                mf.sharedMesh = buoyMesh;
                var mr = buoyGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = lifebuoyMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            // 11. BRIDGE PIER PYLONS & 4 PANORAMIC GLASS ELEVATOR SHAFTS
            var pylonMesh = LoadMesh("BridgePierPylon.obj");
            if (pylonMesh)
            {
                foreach (float px in new[] { -10.0f, 10.0f })
                {
                    var pylonGo = new GameObject("BridgePierPylon_" + (px < 0 ? "West" : "East"));
                    pylonGo.transform.SetParent(root.transform, false);
                    pylonGo.transform.localPosition = new Vector3(px, 0, 5.2f);
                    var mf = pylonGo.AddComponent<MeshFilter>();
                    mf.sharedMesh = pylonMesh;
                    var mr = pylonGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = concreteMat;
                    mr.receiveShadows = true;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                }
            }

            // 11. THE FIRST PIER OF THE BRIDGE STANDING DIRECTLY ON THE EMBANKMENT PROMENADE (from Photo 1)
            var westPierMesh = LoadMesh("BridgePromenadePiersWest.obj");
            if (westPierMesh)
            {
                var wpGo = new GameObject("BridgePromenadePiersWest");
                wpGo.transform.SetParent(root.transform, false);
                wpGo.transform.localPosition = Vector3.zero;
                var mf = wpGo.AddComponent<MeshFilter>();
                mf.sharedMesh = westPierMesh;
                var mr = wpGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = concreteMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            var eastPierMesh = LoadMesh("BridgePromenadePiersEast.obj");
            if (eastPierMesh)
            {
                var epGo = new GameObject("BridgePromenadePiersEast");
                epGo.transform.SetParent(root.transform, false);
                epGo.transform.localPosition = Vector3.zero;
                var mf = epGo.AddComponent<MeshFilter>();
                mf.sharedMesh = eastPierMesh;
                var mr = epGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = concreteMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            var wallMesh = LoadMesh("PromenadeRetainingWall.obj");
            if (wallMesh)
            {
                var rwGo = new GameObject("PromenadeRetainingWall");
                rwGo.transform.SetParent(root.transform, false);
                rwGo.transform.localPosition = Vector3.zero;
                var mf = rwGo.AddComponent<MeshFilter>();
                mf.sharedMesh = wallMesh;
                var mr = rwGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = concreteMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            var elevatorMesh = LoadMesh("ElevatorTower.obj");
            if (elevatorMesh)
            {
                var elevatorCoords = new[]
                {
                    new Vector3(-18.0f, 0, -1.5f),
                    new Vector3(-18.0f, 0, 3.5f),
                    new Vector3(18.0f, 0, -1.5f),
                    new Vector3(18.0f, 0, 3.5f)
                };
                for (int i = 0; i < elevatorCoords.Length; i++)
                {
                    var elevGo = new GameObject("ElevatorTower_" + i);
                    elevGo.transform.SetParent(root.transform, false);
                    elevGo.transform.localPosition = elevatorCoords[i];
                    var mf = elevGo.AddComponent<MeshFilter>();
                    mf.sharedMesh = elevatorMesh;
                    var mr = elevGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = glassMat;
                    mr.receiveShadows = true;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                }
            }

            // 11b. MAIN RIVER PIER IN DON RIVER (Z = 48m) with 4 concrete columns from Photo 1 & 2
            var riverPierMesh = LoadMesh("BridgeRiverPier.obj");
            if (riverPierMesh)
            {
                var rpGo = new GameObject("BridgeRiverPier");
                rpGo.transform.SetParent(root.transform, false);
                rpGo.transform.localPosition = Vector3.zero;
                var mf = rpGo.AddComponent<MeshFilter>();
                mf.sharedMesh = riverPierMesh;
                var mr = rpGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = concreteMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            // 11c. FESTIVE LIGHT GARLANDS ALONG BRIDGE (Catenary swags from Photo 2)
            var garlandMesh = LoadMesh("BridgeFestiveGarlands.obj");
            if (garlandMesh)
            {
                var garlandGo = new GameObject("BridgeFestiveGarlands");
                garlandGo.transform.SetParent(root.transform, false);
                garlandGo.transform.localPosition = Vector3.zero;
                var mf = garlandGo.AddComponent<MeshFilter>();
                mf.sharedMesh = garlandMesh;
                var mr = garlandGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = garlandMat;
                mr.receiveShadows = false;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // 12. LEVBERDON (LEFT BANK) SHORELINE TERRAIN & PARK TREES
            var shoreMesh = LoadMesh("LevberdonShoreline.obj");
            if (shoreMesh)
            {
                var shoreGo = new GameObject("LevberdonShoreline");
                shoreGo.transform.SetParent(root.transform, false);
                shoreGo.transform.localPosition = Vector3.zero;
                var mf = shoreGo.AddComponent<MeshFilter>();
                mf.sharedMesh = shoreMesh;
                var mr = shoreGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = shorelineMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            var treesMesh = LoadMesh("LevberdonTrees.obj");
            if (treesMesh)
            {
                var treesGo = new GameObject("LevberdonTrees");
                treesGo.transform.SetParent(root.transform, false);
                treesGo.transform.localPosition = Vector3.zero;
                var mf = treesGo.AddComponent<MeshFilter>();
                mf.sharedMesh = treesMesh;
                var mr = treesGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = treesMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // 13. ROSTOV ARENA ON LEFT BANK (Levberdon across Don River on the LEFT at X = -85m, Z = 175m)
            var arenaMesh = LoadMesh("RostovArenaDistance.obj");
            if (arenaMesh)
            {
                var arenaGo = new GameObject("RostovArenaDistance");
                arenaGo.transform.SetParent(root.transform, false);
                arenaGo.transform.localPosition = new Vector3(-85.0f, 1.0f, 175.0f);
                var mf = arenaGo.AddComponent<MeshFilter>();
                mf.sharedMesh = arenaMesh;
                var mr = arenaGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = arenaMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // 14. FIVE SEAS BUSINESS CENTER (БЦ «Пять Морей» at X = 32m, Z = -18m)
            var fiveSeasMesh = LoadMesh("FiveSeasBusinessCenter.obj");
            if (fiveSeasMesh)
            {
                var fsGo = new GameObject("FiveSeasBusinessCenter");
                fsGo.transform.SetParent(root.transform, false);
                fsGo.transform.localPosition = Vector3.zero;
                var mf = fsGo.AddComponent<MeshFilter>();
                mf.sharedMesh = fiveSeasMesh;
                var mr = fsGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = fiveSeasMat;
                mr.receiveShadows = true;
                mr.shadowCastingMode = ShadowCastingMode.On;
            }

            // 15. Sunset Sky Panorama Backdrop (Behind Don river and Levberdon at Z = 235m)
            var skyGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyGo.name = "SunsetSkyBackdrop";
            skyGo.transform.SetParent(root.transform, false);
            skyGo.transform.localPosition = new Vector3(-20.0f, 52.0f, 235.0f);
            skyGo.transform.localScale = new Vector3(560, 135, 1);
            var skyR = skyGo.GetComponent<Renderer>();
            skyR.sharedMaterial = backdropMat;
            skyR.shadowCastingMode = ShadowCastingMode.Off;
            skyR.receiveShadows = false;
            UnityEngine.Object.DestroyImmediate(skyGo.GetComponent<Collider>());

            Validate(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("ROSTOV_PBR_ARENA_BUILT at " + PrefabPath);
            return root;
        }

        [MenuItem("LighFight/Build and Review Rostov Embankment PBR")]
        public static void Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            var root = Build();
            if (root.scene != scene) EditorSceneManager.MoveGameObjectToScene(root, scene);

            // Lighting: Crisp Daytime Sunlight over Don River (matching User Photo 1)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.72f, 0.95f); // Daylight blue sky
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.84f, 0.92f); // Bright horizon
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.40f, 0.38f); // Granite stone bounce

            var dirLightGo = new GameObject("Daylight Sun");
            EditorSceneManager.MoveGameObjectToScene(dirLightGo, scene);
            var sun = dirLightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.98f, 0.94f); // Warm crisp southern Rostov sun
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            dirLightGo.transform.rotation = Quaternion.Euler(40f, -42f, 0f);

            // Realtime Reflection Probe over the combat arena for mirror wet puddle reflections
            var probeGo = new GameObject("CombatReflectionProbe");
            EditorSceneManager.MoveGameObjectToScene(probeGo, scene);
            probeGo.transform.position = new Vector3(0, 1.2f, 0);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            probe.size = new Vector3(32, 28, 24);
            probe.intensity = 1.25f;

            // Spawn Tripo fighters for 3D exploration and presentation
            var actors = new List<GameObject>();
            foreach (string actorPath in new[] {
                "Assets/LighFight/Art/Drafts/TripoMasterPlayable/MasterHumanoidCandidate.prefab",
                "Assets/LighFight/Art/Drafts/TripoPlayable/RoseHumanoidCandidate.prefab"
            })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(actorPath);
                if (prefab)
                {
                    var a = UnityEngine.Object.Instantiate(prefab);
                    EditorSceneManager.MoveGameObjectToScene(a, scene);
                    actors.Add(a);
                }
            }

            var camGo = new GameObject("Review Camera");
            EditorSceneManager.MoveGameObjectToScene(camGo, scene);
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.72f, 0.95f);
            camera.fieldOfView = 65;
            camera.allowHDR = true;

            // Attach 3D World Explorer controller to the primary character
            if (actors.Count > 0)
            {
                var explorer = actors[0].AddComponent<Rostov3DWorldExplorer>();
                explorer.explorationCamera = camera;
            }

            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf);
            rt.Create();
            camera.targetTexture = rt;

            try
            {
                // Capture 1: Riverfront Viewing Lookout overlooking Rostov Arena (STRICTLY ON THE LEFT!)
                // Character stands at the granite promenade balcony looking across Don River towards the Left Bank.
                // Rostov Arena with white undulating roof and vertical crown spires is situated prominently on the LEFT.
                // Voroshilovsky Bridge spans across on the RIGHT.
                if (actors.Count > 0)
                {
                    actors[0].transform.position = new Vector3(-12.0f, 0.0f, 0.8f);
                    actors[0].transform.rotation = Quaternion.Euler(0, 18f, 0);
                }
                if (actors.Count > 1)
                {
                    actors[1].transform.position = new Vector3(-10.2f, 0.0f, 0.5f);
                    actors[1].transform.rotation = Quaternion.Euler(0, -145f, 0);
                }

                camera.fieldOfView = 65;
                camera.transform.position = new Vector3(-14.2f, 1.8f, -2.4f);
                camera.transform.LookAt(new Vector3(-10.5f, 2.1f, 18.0f));
                camera.Render();
                SaveRenderTexture(rt, PreviewDir + "/game-combat-view.png");

                // Capture 2: Walking Under the Voroshilovsky Bridge
                // Character walking under the bridge between the massive concrete columns of the first pier
                // standing firmly on the paved granite promenade (matching User Photo 1 & 2).
                if (actors.Count > 0)
                {
                    actors[0].transform.position = new Vector3(0.0f, 0.0f, -0.6f);
                    actors[0].transform.rotation = Quaternion.Euler(0, 5f, 0);
                }
                camera.fieldOfView = 68;
                camera.transform.position = new Vector3(-2.2f, 1.85f, -3.8f);
                camera.transform.LookAt(new Vector3(0.0f, 2.2f, 24.0f));
                camera.Render();
                SaveRenderTexture(rt, PreviewDir + "/panoramic-sunset.png");

                // Capture 3: Wide 3D Navigable World & Grand Staircases Overview
                // High-angle view showcasing the expansive 150m promenade, 14-step grand staircase
                // connecting the lower promenade to Beregovaya street, upper terrace, streetlamps, benches, and Don River.
                if (actors.Count > 0)
                {
                    actors[0].transform.position = new Vector3(-28.0f, 0.0f, -3.5f);
                    actors[0].transform.rotation = Quaternion.Euler(0, 45f, 0);
                }
                camera.fieldOfView = 65;
                camera.transform.position = new Vector3(-38.0f, 5.2f, -18.0f);
                camera.transform.LookAt(new Vector3(-24.0f, 1.5f, -2.0f));
                camera.Render();
                SaveRenderTexture(rt, PreviewDir + "/water-level-view.png");

                Debug.Log("ROSTOV_PBR_SCREENSHOTS_CAPTURED in " + PreviewDir);
            }
            finally
            {
                if (rt) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                if (root) UnityEngine.Object.DestroyImmediate(root);
                foreach (var a in actors) if (a) UnityEngine.Object.DestroyImmediate(a);
                if (dirLightGo) UnityEngine.Object.DestroyImmediate(dirLightGo);
                if (probeGo) UnityEngine.Object.DestroyImmediate(probeGo);
                if (camGo) UnityEngine.Object.DestroyImmediate(camGo);
                if (scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("LighFight/Save Rostov Embankment Exploration Scene")]
        public static void SaveExplorationScene()
        {
            const string scenePath = Folder + "/RostovEmbankmentExplorationScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

            // 1. Build the complete 150m Rostov Embankment Arena
            var root = Build();
            if (root.scene != scene) EditorSceneManager.MoveGameObjectToScene(root, scene);

            // 2. Lighting: Crisp Southern Russian Daytime Sunlight
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.72f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.84f, 0.92f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.40f, 0.38f);

            var dirLightGo = new GameObject("Daylight Sun");
            EditorSceneManager.MoveGameObjectToScene(dirLightGo, scene);
            var sun = dirLightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.98f, 0.94f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            dirLightGo.transform.rotation = Quaternion.Euler(40f, -42f, 0f);

            // 3. Realtime Reflection Probe for wet granite puddle reflections
            var probeGo = new GameObject("CombatReflectionProbe");
            EditorSceneManager.MoveGameObjectToScene(probeGo, scene);
            probeGo.transform.position = new Vector3(0, 1.2f, 0);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            probe.size = new Vector3(32, 28, 24);
            probe.intensity = 1.25f;

            // 4. Spawn Player Character with Rostov3DWorldExplorer
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Drafts/TripoMasterPlayable/MasterHumanoidCandidate.prefab");
            GameObject playerGo = null;
            if (playerPrefab)
            {
                playerGo = UnityEngine.Object.Instantiate(playerPrefab);
                playerGo.name = "Player_Explorer";
                EditorSceneManager.MoveGameObjectToScene(playerGo, scene);
                playerGo.transform.position = new Vector3(-12.0f, 0.0f, 0.8f);
                playerGo.transform.rotation = Quaternion.Euler(0, 18f, 0);
            }

            // 5. Main Camera with Third-Person Orbit Follow
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            EditorSceneManager.MoveGameObjectToScene(camGo, scene);
            var camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.72f, 0.95f);
            camera.fieldOfView = 65;
            camera.allowHDR = true;
            camGo.AddComponent<AudioListener>();

            if (playerGo)
            {
                var explorer = playerGo.AddComponent<Rostov3DWorldExplorer>();
                explorer.explorationCamera = camera;
                camGo.transform.position = new Vector3(-14.2f, 1.8f, -2.4f);
                camGo.transform.LookAt(new Vector3(-10.5f, 2.1f, 18.0f));
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("ROSTOV_EXPLORATION_SCENE_SAVED at: " + scenePath);
        }

        private static void SaveRenderTexture(RenderTexture rt, string filename)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(filename, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("Saved preview: " + filename);
        }
    }
}
