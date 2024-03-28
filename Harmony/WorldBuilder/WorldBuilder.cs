using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
                int widthInTiles = worldBuilder.WorldSize / worldBuilder.WorldTileSize;
                int t = 0;
                int num2;
                for (int tileX = 0; tileX < widthInTiles; tileX = num2 + 1)
                {
                    for (int tileY = 0; tileY < widthInTiles; tileY = num2 + 1)
                    {
                        if (worldBuilder.IsMessageElapsed())
                        {
                            string format = "Generating Terrain: {0}%";
                            float num = 100f;
                            num2 = t;
                            t = num2 + 1;
                            yield return worldBuilder.SetMessage(string.Format(format, Mathf.FloorToInt(num * (num2 / (float)(widthInTiles * widthInTiles)))), false);
                        }
                        TerrainType terrainType = worldBuilder.terrainTypeMap.data[tileX, tileY];
                        if (terrainType == TerrainType.mountains || terrainType == TerrainType.plains)
                        {
                            BiomeType biomeType = worldBuilder.biomeMap.data[tileX, tileY];
                            if (biomeType == BiomeType.none)
                            {
                                biomeType = BiomeType.forest;
                            }
                            int num3 = tileX * worldBuilder.WorldTileSize + worldBuilder.WorldTileSize / 2;
                            int num4 = tileY * worldBuilder.WorldTileSize + worldBuilder.WorldTileSize / 2;
                            string text = biomeType.ToStringCached();
                            string text2 = terrainType.ToStringCached();
                            string comboTypeName = string.Format("{0}_{1}", text, text2);
                            GetTerrainProperties(text, text2, comboTypeName, out Vector2 vector, out bool flag, out int num5, out int num6, out float biomeAlphaCutoff);
                            for (int i = 0; i < num5; i++)
                            {
                                if (StampManager.TryGetStamp(text2, comboTypeName, out RawStamp rawStamp))
                                {
                                    TranslationData translationData = new TranslationData(num3 + Rand.Instance.Range(-(num6 / 2), num6 / 2, false), num4 + Rand.Instance.Range(-(num6 / 2), num6 / 2, false), true, vector.x, vector.y, true, 0);
                                    List<Stamp> stamps = terrainLayer.Stamps;
                                    RawStamp stamp = rawStamp;
                                    TranslationData transData = translationData;
                                    bool isCustomColor = false;
                                    string name = rawStamp.name;
                                    stamps.Add(new Stamp(stamp, transData, isCustomColor, default, 0.1f, false, name));
                                    if (rawStamp.hasWater)
                                    {
                                        waterLayer.Stamps.Add(new Stamp(rawStamp, translationData, false, default, 0.1f, true, ""));
                                    }
                                    if (flag)
                                    {
                                        biomeLayer.Stamps.Add(new Stamp(rawStamp, translationData, true, biomeColors[biomeType], biomeAlphaCutoff, false, ""));
                                    }
                                }
                            }
                        }
                        num2 = tileY;
                    }
                    num2 = tileX;
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
                RawStamp fillerBiome = StampManager.GetStamp("filler_biome");
                RawStamp stamp = StampManager.GetStamp("ground");
                StampManager.GetStamp("base").blendAmount = 0.25f;
                float r = stamp.terrainPixels[0].r;
                int worldTileCountWide = worldBuilder.WorldSize / worldBuilder.WorldTileSize;
                Color32 color = biomeColors[BiomeType.forest];
                for (int i = 0; i < biomeDest.Length; i++)
                {
                    biomeDest[i] = color;
                }
                Color color2 = new Color(r, r, r);
                for (int j = 0; j < terrainDest.Length; j++)
                {
                    terrainDest[j] = color2;
                }
                Thread terrainBorderThread = new Thread(delegate ()
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
                                RawStamp stamp2;
                                if (translationData != null && (StampManager.TryGetStamp(worldBuilder.biomeMap.data[Mathf.Clamp(translationData.x / worldBuilder.WorldTileSize, 0, worldBuilder.WorldSize / worldBuilder.WorldTileSize - 1), Mathf.Clamp(translationData.y / worldBuilder.WorldTileSize, 0, worldBuilder.WorldSize / worldBuilder.WorldTileSize - 1)].ToString() + "_land_border", out stamp2) || StampManager.TryGetStamp("land_border", out stamp2)))
                                {
                                    StampManager.DrawStamp(terrainDest, new Stamp(stamp2, translationData, false, default(Color), 0.1f, false, ""), true);
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
                                RawStamp stamp2;
                                if (translationData2 != null && (StampManager.TryGetStamp(worldBuilder.biomeMap.data[Mathf.Clamp(translationData2.x / worldBuilder.WorldTileSize, 0, worldBuilder.WorldSize / worldBuilder.WorldTileSize - 1), Mathf.Clamp(translationData2.y / worldBuilder.WorldTileSize, 0, worldBuilder.WorldSize / worldBuilder.WorldTileSize - 1)].ToString() + "_water_border", out stamp2) || StampManager.TryGetStamp("water_border", out stamp2)))
                                {
                                    StampManager.DrawStamp(terrainDest, new Stamp(stamp2, translationData2, false, default(Color), 0.1f, false, ""), true);
                                    Stamp stamp3 = new Stamp(stamp2, translationData2, true, new Color32(0, 0, (byte)worldBuilder.WaterHeight, 0), 0.1f, true, "");
                                    waterLayer.Stamps.Add(stamp3);
                                    StampManager.DrawWaterStamp(waterDest, stamp3, true);
                                }
                            }
                        }
                        for (int num3 = 0; num3 < 4; num3++)
                        {
                            Vector2 vector2 = Vector2.one * ((float)num / 1024f);
                            TranslationData translationData3 = null;
                            if (num3 == 0)
                            {
                                translationData3 = new TranslationData(l, 0, true, vector2.x, vector2.y, false, 0);
                            }
                            else if (num3 == 1)
                            {
                                translationData3 = new TranslationData(l, worldBuilder.WorldSize, true, vector2.x, vector2.y, false, 0);
                            }
                            else if (num3 == 2)
                            {
                                translationData3 = new TranslationData(0, l, true, vector2.x, vector2.y, false, 0);
                            }
                            else if (num3 == 3)
                            {
                                translationData3 = new TranslationData(worldBuilder.WorldSize, l, true, vector2.x, vector2.y, false, 0);
                            }
                            RawStamp stamp2;
                            if (translationData3 != null && StampManager.TryGetStamp("ground", out stamp2))
                            {
                                StampManager.DrawStamp(radDest, new Stamp(stamp2, translationData3, true, new Color(1f, 0f, 0f, 0f), 0.1f, false, ""), true);
                            }
                        }
                    }
                });
                terrainBorderThread.Start();
                Thread[] biomeThreads = new Thread[]
                {
                new Thread(delegate()
                {
                    if (fillerBiome != null)
                    {
                        GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(worldBuilder.Seed + 20);
                        int num = 0;
                        int num2 = 0;
                        int worldTileCountWide2 = worldTileCountWide;
                        for (int l = num; l < worldTileCountWide; l++)
                        {
                            for (int m = num2; m < worldTileCountWide2; m++)
                            {
                                int num3 = l * worldBuilder.WorldTileSize;
                                int num4 = m * worldBuilder.WorldTileSize;
                                BiomeType biomeType = worldBuilder.biomeMap.data[l, m];
                                if (biomeType == BiomeType.none)
                                {
                                    biomeType = getBiomeViaNeighbors(l, m, biomeType);
                                }
                                if (biomeType != BiomeType.none)
                                {
                                    StampManager.DrawStamp(biomeDest, fillerBiome.terrainPixels, 256 + num3 + worldBuilder.WorldTileSize / 2, 256 + num4 + worldBuilder.WorldTileSize / 2, worldBuilder.WorldSize, worldBuilder.WorldSize, fillerBiome.width, fillerBiome.height, 1f, (float)worldBuilder.WorldTileSize / 1024f * 4f, true, true, biomeColors[biomeType], 0.1f, (float)(gameRandom.RandomRange(0, 4) * 90), false, false);
                                }
                            }
                        }
                        GameRandomManager.Instance.FreeGameRandom(gameRandom);
                    }
                })
                {
                    Priority = System.Threading.ThreadPriority.AboveNormal
                }
                };
                Thread[] array = biomeThreads;
                for (int k = 0; k < array.Length; k++)
                {
                    array[k].Start();
                }
                bool isAnyAlive = true;
                while (isAnyAlive || terrainBorderThread.IsAlive)
                {
                    isAnyAlive = false;
                    foreach (Thread thread in biomeThreads)
                    {
                        isAnyAlive |= thread.IsAlive;
                    }
                    if (terrainBorderThread.IsAlive && isAnyAlive)
                    {
                        yield return worldBuilder.SetMessage(string.Format("Creating terrain and biome stamps: {0}m {1}s", ms.Elapsed.Minutes, ms.Elapsed.Seconds), false);
                    }
                    else if (terrainBorderThread.IsAlive && !isAnyAlive)
                    {
                        yield return worldBuilder.SetMessage(string.Format("Creating terrain stamps: {0}m {1}s", ms.Elapsed.Minutes, ms.Elapsed.Seconds), false);
                    }
                    else
                    {
                        yield return worldBuilder.SetMessage(string.Format("Creating biome stamps: {0}m {1}s", ms.Elapsed.Minutes, ms.Elapsed.Seconds), false);
                    }
                }
                yield break;
            }

            private static BiomeType getBiomeViaNeighbors(int x1, int y1, BiomeType thisBiome)
            {
                int num = x1 + y1 * (worldBuilder.WorldSize / worldBuilder.WorldTileSize) + worldBuilder.Seed;
                for (int i = 0; i < GenerateBaseStamps.directions8way.Length; i++)
                {
                    int num2 = Mathf.Abs(i + Mathf.Abs(num % GenerateBaseStamps.directions8way.Length)) % GenerateBaseStamps.directions8way.Length;
                    if (num2 < 0)
                    {
                        Log.Error("Index less than 0: {0}", new object[]
                        {
                        num2
                        });
                    }
                    if (num2 >= GenerateBaseStamps.directions8way.Length)
                    {
                        Log.Error("Index greater than {1}: {0}", new object[]
                        {
                        num2,
                        GenerateBaseStamps.directions8way.Length - 1
                        });
                    }
                    Vector2i vector2i = GenerateBaseStamps.directions8way[num2];
                    if (x1 + vector2i.x >= 0 && x1 + vector2i.x < worldBuilder.biomeMap.data.GetLength(0) && y1 + vector2i.y >= 0 && y1 + vector2i.y < worldBuilder.biomeMap.data.GetLength(1))
                    {
                        BiomeType biomeType = worldBuilder.biomeMap.data[x1 + vector2i.x, y1 + vector2i.y];
                        if (biomeType != BiomeType.none)
                        {
                            thisBiome = biomeType;
                            worldBuilder.biomeMap.data[x1, y1] = biomeType;
                            break;
                        }
                    }
                }
                return thisBiome;
            }
        }
    }
}
