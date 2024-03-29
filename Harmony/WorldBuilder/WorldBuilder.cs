using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WorldGenerationEngineFinal;

namespace MyTestMod.Harmony.WorldBuilder
{
    internal class WorldBuilder
    {

        [HarmonyPatch(typeof(WorldGenerationEngineFinal.WorldBuilder), "generateMountains")]
        private class GenerateMountains
        {

            private static WorldGenerationEngineFinal.WorldBuilder worldBuilder;
            private static StampGroup waterLayer;
            private static StampGroup terrainLayer;
            private static StampGroup biomeLayer;
            private static Dictionary<BiomeType, Color32> biomeColors;
            private static DynamicProperties thisWorldProperties;

            [HarmonyPrefix]
            private static bool Prefix(WorldGenerationEngineFinal.WorldBuilder __instance, ref IEnumerator __result, ref StampGroup ___waterLayer, ref StampGroup ___terrainLayer, ref StampGroup ___biomeLayer, ref Dictionary<BiomeType, Color32> ___biomeColors, ref DynamicProperties ___thisWorldProperties)
            {
                Debug.Log("[MyTestMod] WorldBuilder.generateMountains Prefix");

                worldBuilder = __instance;
                waterLayer = ___waterLayer;
                terrainLayer = ___terrainLayer;
                biomeLayer = ___biomeLayer;
                biomeColors = ___biomeColors;
                thisWorldProperties = ___thisWorldProperties;

                __result = NewGenerateMountainsMethod();

                return false;
            }

            private static IEnumerator NewGenerateMountainsMethod()
            {
                int worldWidthInTiles = worldBuilder.WorldSize / worldBuilder.WorldTileSize;
                int progressCounter = 0;
                int halfWorldTileSize = worldBuilder.WorldTileSize / 2;
                StringBuilder comboTypeNameBuilder = new StringBuilder();
                for (int tileX = 0; tileX < worldWidthInTiles; ++tileX)
                {
                    for (int tileY = 0; tileY < worldWidthInTiles; ++tileY)
                    {
                        if (worldBuilder.IsMessageElapsed())
                        {
                            string progressMessage = "Generating Terrain: {0}%";
                            yield return worldBuilder.SetMessage(string.Format(progressMessage, Mathf.FloorToInt(100f * (progressCounter++ / (float)(worldWidthInTiles * worldWidthInTiles)))), false);
                        }
                        TerrainType currentTerrainType = worldBuilder.terrainTypeMap.data[tileX, tileY];
                        if (currentTerrainType == TerrainType.mountains || currentTerrainType == TerrainType.plains)
                        {
                            BiomeType currentBiomeType = worldBuilder.biomeMap.data[tileX, tileY];
                            if (currentBiomeType == BiomeType.none)
                            {
                                currentBiomeType = BiomeType.forest;
                            }
                            string biomeTypeName = currentBiomeType.ToStringCached();
                            string terrainTypeName = currentTerrainType.ToStringCached();
                            comboTypeNameBuilder.Clear();
                            comboTypeNameBuilder.Append(biomeTypeName).Append('_').Append(terrainTypeName);
                            string comboTypeName = comboTypeNameBuilder.ToString();
                            GetTerrainProperties(biomeTypeName, terrainTypeName, comboTypeName, out Vector2 scale, out bool useBiomeMask, out int clusterIterations, out int clusterRange, out float biomeAlphaCutoff);
                            int centerX = tileX * worldBuilder.WorldTileSize + halfWorldTileSize;
                            int centerY = tileY * worldBuilder.WorldTileSize + halfWorldTileSize;
                            for (int i = 0; i < clusterIterations; i++)
                            {
                                if (StampManager.TryGetStamp(terrainTypeName, comboTypeName, out RawStamp rawStamp))
                                {
                                    TranslationData translationData = new TranslationData(centerX + Rand.Instance.Range(-(clusterRange / 2), clusterRange / 2, false), centerY + Rand.Instance.Range(-(clusterRange / 2), clusterRange / 2, false), true, scale.x, scale.y, true, 0);
                                    List<Stamp> stamps = terrainLayer.Stamps;
                                    stamps.Add(new Stamp(rawStamp, translationData, false, default, 0.1f, false, rawStamp.name));
                                    if (rawStamp.hasWater)
                                    {
                                        waterLayer.Stamps.Add(new Stamp(rawStamp, translationData, false, default, 0.1f, true, ""));
                                    }
                                    if (useBiomeMask)
                                    {
                                        biomeLayer.Stamps.Add(new Stamp(rawStamp, translationData, true, biomeColors[currentBiomeType], biomeAlphaCutoff, false, ""));
                                    }
                                }
                            }
                        }
                    }
                }
                yield break;
            }

            private static void GetTerrainProperties(
              string biomeTypeName,
              string terrainTypeName,
              string comboTypeName,
              out Vector2 sizeMinMax,
              out bool useBiomeMask,
              out int iterCount,
              out int range,
              out float biomeCutoff)
            {
                sizeMinMax = Vector2.one;
                string _input1 = thisWorldProperties.GetString(comboTypeName + ".scale");
                if (_input1 != string.Empty)
                {
                    sizeMinMax = StringParsers.ParseVector2(_input1);
                }
                else
                {
                    string _input2 = thisWorldProperties.GetString(terrainTypeName + ".scale");
                    if (_input2 != string.Empty)
                        sizeMinMax = StringParsers.ParseVector2(_input2);
                }
                useBiomeMask = false;
                string _input3 = thisWorldProperties.GetString(comboTypeName + ".use_biome_mask");
                if (_input3 != string.Empty)
                {
                    useBiomeMask = StringParsers.ParseBool(_input3);
                }
                else
                {
                    string _input4 = thisWorldProperties.GetString(terrainTypeName + ".use_biome_mask");
                    if (_input4 != string.Empty)
                        useBiomeMask = StringParsers.ParseBool(_input4);
                }
                iterCount = 5;
                string _input5 = thisWorldProperties.GetString(comboTypeName + ".cluster_iterations");
                if (_input5 != string.Empty)
                {
                    iterCount = Mathf.Abs(StringParsers.ParseSInt32(_input5));
                }
                else
                {
                    string _input6 = thisWorldProperties.GetString(terrainTypeName + ".cluster_iterations");
                    if (_input6 != string.Empty)
                        iterCount = Mathf.Abs(StringParsers.ParseSInt32(_input6));
                }
                range = 800;
                string _input7 = thisWorldProperties.GetString(comboTypeName + ".cluster_range");
                if (_input7 != string.Empty)
                {
                    range = Mathf.Abs(StringParsers.ParseSInt32(_input7));
                }
                else
                {
                    string _input8 = thisWorldProperties.GetString(terrainTypeName + ".cluster_range");
                    if (_input8 != string.Empty)
                        range = Mathf.Abs(StringParsers.ParseSInt32(_input8));
                }
                range = Mathf.FloorToInt(range * (worldBuilder.WorldTileSize / 1024f));
                biomeCutoff = 0.1f;
                string _input9 = thisWorldProperties.GetString(comboTypeName + ".biome_mask_min");
                if (_input9 != string.Empty)
                {
                    biomeCutoff = StringParsers.ParseFloat(_input9);
                }
                else
                {
                    string _input10 = thisWorldProperties.GetString(terrainTypeName + ".biome_mask_min");
                    if (!(_input10 != string.Empty))
                        return;
                    biomeCutoff = StringParsers.ParseFloat(_input10);
                }
            }

        }

        [HarmonyPatch(typeof(WorldGenerationEngineFinal.WorldBuilder), "generateBaseStamps")]
        private class GenerateBaseStamps
        {

            private static WorldGenerationEngineFinal.WorldBuilder worldBuilder;
            private static StampGroup waterLayer;
            private static Dictionary<BiomeType, Color32> biomeColors;
            private static DynamicProperties thisWorldProperties;
            private static Color32[] biomeDest;
            private static Color32[] radDest;
            private static Color[] terrainDest;
            private static float[] waterDest;
            private static bool GenWaterBorderN;
            private static bool GenWaterBorderE;
            private static bool GenWaterBorderS;
            private static bool GenWaterBorderW;
            private static readonly Vector2i[] directions8way = new Vector2i[]
            {
                Vector2i.up,
                Vector2i.up + Vector2i.right,
                Vector2i.right,
                Vector2i.right + Vector2i.down,
                Vector2i.down,
                Vector2i.down + Vector2i.left,
                Vector2i.left,
                Vector2i.left + Vector2i.up
            };

            [HarmonyPrefix]
            private static bool Prefix(WorldGenerationEngineFinal.WorldBuilder __instance, ref IEnumerator __result, MicroStopwatch ms, ref StampGroup ___waterLayer, ref Dictionary<BiomeType, Color32> ___biomeColors, ref DynamicProperties ___thisWorldProperties, ref Color32[] ___biomeDest, ref Color32[] ___radDest, ref Color[] ___terrainDest, ref float[] ___waterDest, ref bool ___GenWaterBorderN, ref bool ___GenWaterBorderE, ref bool ___GenWaterBorderS, ref bool ___GenWaterBorderW)
            {
                Debug.Log("[MyTestMod] WorldBuilder.generateBaseStamps Prefix");

                worldBuilder = __instance;
                waterLayer = ___waterLayer;
                biomeColors = ___biomeColors;
                thisWorldProperties = ___thisWorldProperties;
                biomeDest = ___biomeDest;
                radDest = ___radDest;
                terrainDest = ___terrainDest;
                waterDest = ___waterDest;
                GenWaterBorderN = ___GenWaterBorderN;
                GenWaterBorderE = ___GenWaterBorderE;
                GenWaterBorderS = ___GenWaterBorderS;
                GenWaterBorderW = ___GenWaterBorderW;

                __result = NewGenerateBaseStampsMethod(ms);

                return false;
            }

            private static IEnumerator NewGenerateBaseStampsMethod(MicroStopwatch ms)
            {
                StampManager.GetStamp("base").blendAmount = 0.25f;

                SettingColours();

                Task terrainBorderTask = StartTerrainBorderTask();
                Task biomeTask = StartBiomeTask();

                while (true)
                {
                    bool isBiomeTaskRunning = biomeTask.Status == TaskStatus.Running;
                    bool isTerrainBorderTaskRunning = terrainBorderTask.Status == TaskStatus.Running;

                    if (!isBiomeTaskRunning && !isTerrainBorderTaskRunning)
                    {
                        break;
                    }

                    string taskName = isTerrainBorderTaskRunning ? (isBiomeTaskRunning ? "Creating terrain and biome stamps" : "Creating terrain stamps") : "Creating biome stamps";
                    string message = $"{taskName}: {ms.Elapsed.Minutes}m {ms.Elapsed.Seconds}s";

                    yield return worldBuilder.SetMessage(message, false);
                }
                yield break;
            }

            private static void DrawStamp(int l, int worldSize, Vector2 vector2, int num3, string stampName, Color color)
            {
                TranslationData translationData = null;
                switch (num3)
                {
                    case 0:
                        translationData = new TranslationData(l, 0, true, vector2.x, vector2.y, false, 0);
                        break;
                    case 1:
                        translationData = new TranslationData(l, worldSize, true, vector2.x, vector2.y, false, 0);
                        break;
                    case 2:
                        translationData = new TranslationData(0, l, true, vector2.x, vector2.y, false, 0);
                        break;
                    case 3:
                        translationData = new TranslationData(worldSize, l, true, vector2.x, vector2.y, false, 0);
                        break;
                }
                if (translationData != null && StampManager.TryGetStamp(stampName, out RawStamp stamp2))
                {
                    StampManager.DrawStamp(radDest, new Stamp(stamp2, translationData, true, color, 0.1f, false, ""), true);
                }
            }

            private static void SettingColours()
            {
                RawStamp stamp = StampManager.GetStamp("ground");
                float terrainRed = stamp.terrainPixels[0].r;
                Color32 colorBiome = biomeColors[BiomeType.forest];
                Parallel.For(0, biomeDest.Length, delegate (int index)
                {
                    biomeDest[index] = colorBiome;
                });
                Color colorTerrain = new Color(terrainRed, terrainRed, terrainRed);
                Parallel.For(0, terrainDest.Length, delegate (int index)
                {
                    terrainDest[index] = colorTerrain;
                });
            }

            private static BiomeType GetBiomeViaNeighbors(int currentX, int currentY, BiomeType currentBiome)
            {
                int seedOffset = currentX + currentY * (worldBuilder.WorldSize / worldBuilder.WorldTileSize) + worldBuilder.Seed;
                int mapWidth = worldBuilder.biomeMap.data.GetLength(0);
                int mapHeight = worldBuilder.biomeMap.data.GetLength(1);
                int initialDirectionIndex = seedOffset % directions8way.Length;
                for (int i = 0; i < directions8way.Length; i++)
                {
                    int directionIndex = Mathf.Abs(i + initialDirectionIndex) % directions8way.Length;
                    Vector2i directionVector = directions8way[directionIndex];
                    int neighborX = currentX + directionVector.x;
                    int neighborY = currentY + directionVector.y;
                    if (neighborX >= 0 && neighborX < mapWidth && neighborY >= 0 && neighborY < mapHeight)
                    {
                        BiomeType neighborBiome = worldBuilder.biomeMap.data[neighborX, neighborY];
                        if (neighborBiome != BiomeType.none)
                        {
                            currentBiome = neighborBiome;
                            worldBuilder.biomeMap.data[currentX, currentY] = neighborBiome;
                            break;
                        }
                    }
                }
                return currentBiome;
            }

            private static Task StartBiomeTask()
            {
                RawStamp fillerBiome = StampManager.GetStamp("filler_biome");
                int worldTileCountWide = worldBuilder.WorldSize / worldBuilder.WorldTileSize;

                if (fillerBiome == null)
                {
                    return Task.CompletedTask;
                }

                return Task.Run(delegate ()
                {
                    GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(worldBuilder.Seed + 20);
                    for (int l = 0; l < worldTileCountWide; l++)
                    {
                        for (int m = 0; m < worldTileCountWide; m++)
                        {
                            BiomeType biomeType = worldBuilder.biomeMap.data[l, m];
                            if (biomeType == BiomeType.none)
                            {
                                biomeType = GetBiomeViaNeighbors(l, m, biomeType);
                            }
                            else
                            {
                                StampManager.DrawStamp(biomeDest, fillerBiome.terrainPixels, 256 + l * worldBuilder.WorldTileSize + worldBuilder.WorldTileSize / 2, 256 + m * worldBuilder.WorldTileSize + worldBuilder.WorldTileSize / 2, worldBuilder.WorldSize, worldBuilder.WorldSize, fillerBiome.width, fillerBiome.height, 1f, worldBuilder.WorldTileSize / 1024f * 4f, true, true, biomeColors[biomeType], 0.1f, gameRandom.RandomRange(0, 4) * 90, false, false);
                            }
                        }
                    }
                    GameRandomManager.Instance.FreeGameRandom(gameRandom);
                });
            }

            private static Task StartTerrainBorderTask()
            {
                return Task.Run(delegate ()
                {
                    Vector2 vector = new Vector2(1.5f, 3.5f);
                    string @string = thisWorldProperties.GetString("border.scale");
                    if (@string != string.Empty)
                    {
                        vector = StringParsers.ParseVector2(@string);
                    }
                    int num = 512;
                    if ((@string = thisWorldProperties.GetString("border_step_distance")) != string.Empty)
                    {
                        num = StringParsers.ParseSInt32(@string, 0, -1, NumberStyles.Integer);
                    }
                    int num2 = num / 2;
                    int worldSizeTile = worldBuilder.WorldSize / worldBuilder.WorldTileSize - 1;
                    for (int l = 0; l < worldBuilder.WorldSize + num; l += num)
                    {
                        if (!GenWaterBorderE || !GenWaterBorderW || !GenWaterBorderN || !GenWaterBorderS)
                        {
                            for (int m = 0; m < 4; m++)
                            {
                                TranslationData translationData = null;
                                if (m == 0 && !GenWaterBorderS)
                                {
                                    translationData = new TranslationData(l + Rand.Instance.Range(0, num2, false), Rand.Instance.Range(0, num2, false), true, vector.x, vector.y, true, 0);
                                }
                                else if (m == 1 && !GenWaterBorderN)
                                {
                                    translationData = new TranslationData(l + Rand.Instance.Range(0, num2, false), worldBuilder.WorldSize - Rand.Instance.Range(0, num2, false), true, vector.x, vector.y, true, 0);
                                }
                                else if (m == 2 && !GenWaterBorderW)
                                {
                                    translationData = new TranslationData(Rand.Instance.Range(0, num2, false), l + Rand.Instance.Range(0, num2, false), true, vector.x, vector.y, true, 0);
                                }
                                else if (m == 3 && !GenWaterBorderE)
                                {
                                    translationData = new TranslationData(worldBuilder.WorldSize - Rand.Instance.Range(0, num2, false), l + Rand.Instance.Range(0, num2, false), true, vector.x, vector.y, true, 0);
                                }
                                if (translationData != null && (StampManager.TryGetStamp(worldBuilder.biomeMap.data[Mathf.Clamp(translationData.x / worldBuilder.WorldTileSize, 0, worldSizeTile), Mathf.Clamp(translationData.y / worldBuilder.WorldTileSize, 0, worldSizeTile)].ToString() + "_land_border", out RawStamp stamp2) || StampManager.TryGetStamp("land_border", out stamp2)))
                                {
                                    StampManager.DrawStamp(terrainDest, new Stamp(stamp2, translationData, false, default, 0.1f, false, ""), true);
                                }
                            }
                        }
                        if (GenWaterBorderE || GenWaterBorderW || GenWaterBorderN || GenWaterBorderS)
                        {
                            for (int n = 0; n < 4; n++)
                            {
                                TranslationData translationData2 = null;
                                if (n == 0 && GenWaterBorderS)
                                {
                                    translationData2 = new TranslationData(l, Rand.Instance.Range(0, num2 / 2, false), true, vector.x, vector.y, true, 0);
                                }
                                else if (n == 1 && GenWaterBorderN)
                                {
                                    translationData2 = new TranslationData(l, worldBuilder.WorldSize - Rand.Instance.Range(0, num2 / 2, false), true, vector.x, vector.y, true, 0);
                                }
                                else if (n == 2 && GenWaterBorderW)
                                {
                                    translationData2 = new TranslationData(Rand.Instance.Range(0, num2 / 2, false), l, true, vector.x, vector.y, true, 0);
                                }
                                else if (n == 3 && GenWaterBorderE)
                                {
                                    translationData2 = new TranslationData(worldBuilder.WorldSize - Rand.Instance.Range(0, num2 / 2, false), l, true, vector.x, vector.y, true, 0);
                                }
                                if (translationData2 != null && (StampManager.TryGetStamp(worldBuilder.biomeMap.data[Mathf.Clamp(translationData2.x / worldBuilder.WorldTileSize, 0, worldSizeTile), Mathf.Clamp(translationData2.y / worldBuilder.WorldTileSize, 0, worldSizeTile)].ToString() + "_water_border", out RawStamp stamp2) || StampManager.TryGetStamp("water_border", out stamp2)))
                                {
                                    StampManager.DrawStamp(terrainDest, new Stamp(stamp2, translationData2, false, default, 0.1f, false, ""), true);
                                    Stamp stamp3 = new Stamp(stamp2, translationData2, true, new Color32(0, 0, (byte)worldBuilder.WaterHeight, 0), 0.1f, true, "");
                                    waterLayer.Stamps.Add(stamp3);
                                    StampManager.DrawWaterStamp(waterDest, stamp3, true);
                                }
                            }
                        }
                        Vector2 vector2 = Vector2.one * (num / 1024f);
                        for (int num3 = 0; num3 < 4; num3++)
                        {
                            DrawStamp(l, worldBuilder.WorldSize, vector2, num3, "ground", new Color(1f, 0f, 0f, 0f));
                        }
                    }
                });
            }
        }
    }
}
