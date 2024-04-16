using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using WorldGenerationEngineFinal;

namespace GeneratorPerformanceMod.Harmony.StreetTile
{
    internal class StreetTile
    {

        [HarmonyPatch(typeof(WorldGenerationEngineFinal.StreetTile), "spawnWildernessPrefab")]
        private static class SpawnWildernessPrefab
        {
            private static WorldGenerationEngineFinal.StreetTile streetTile;
            private static POITags traderTag;
            private const int MaxAttempts = 6;
            private const int MinSize = 150;
            private const int Offset = 10;
            private const int SeedOffset = 4096953;

            [HarmonyPrefix]
            private static bool Prefix(WorldGenerationEngineFinal.StreetTile __instance, ref bool __result, ref Vector2i ___WildernessPOICenter, ref int ___WildernessPOISize, ref int ___WildernessPOIHeight, ref POITags ___traderTag, ref POITags ___wildernessTag)
            {
                streetTile = __instance;
                traderTag = ___traderTag;

                __result = NewSpawnWildernessPrefabMethod();

                return false;
            }

            private static bool NewSpawnWildernessPrefabMethod()
            {
                var gameRandom = GameRandomManager.Instance.CreateGameRandom(WorldGenerationEngineFinal.WorldBuilder.Instance.Seed + SeedOffset);
                var poiTags = WorldGenerationEngineFinal.WorldBuilder.Instance.Towns == WorldGenerationEngineFinal.WorldBuilder.GenerationSelections.None ? POITags.none : traderTag;
                var worldPositionCenter = streetTile.WorldPositionCenter;
                var wildernessPrefab = PrefabManager.GetWildernessPrefab(poiTags, POITags.none, new Vector2i(), new Vector2i(), center: worldPositionCenter);

                for (int attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    var rotation = (wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 12)) % 4;
                    var (sizeX, sizeZ) = GetRotatedSizes(wildernessPrefab, rotation);
                    var position = GetPosition(gameRandom, sizeX, sizeZ);
                    var rect = GetRect(position, sizeX, sizeZ);

                    if (!IsValidRect(rect))
                    {
                        continue;
                    }

                    var biome = WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)rect.center.x, (int)rect.center.y);
                    var centerHeight = Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight((int)rect.center.x, (int)rect.center.y));
                    var heights = GetHeights(position, sizeX, sizeZ, biome, centerHeight);

                    if (heights == null)
                    {
                        continue;
                    }

                    var medianHeight = GetMedianHeight(heights);

                    if (medianHeight + wildernessPrefab.yOffset < 2)
                    {
                        continue;
                    }

                    // The rest of the code that uses medianHeight and doesn't have any continue or return statements
                    // ...

                    GameRandomManager.Instance.FreeGameRandom(gameRandom);
                    return true;
                }

                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return false;
            }

            private static (int, int) GetRotatedSizes(PrefabData wildernessPrefab, int rotation)
            {
                var sizeX = wildernessPrefab.size.x;
                var sizeZ = wildernessPrefab.size.z;

                if (rotation == 1 || rotation == 3)
                {
                    sizeX = wildernessPrefab.size.z;
                    sizeZ = wildernessPrefab.size.x;
                }

                return (sizeX, sizeZ);
            }

            private static Vector2i GetPosition(GameRandom gameRandom, int sizeX, int sizeZ)
            {
                if (sizeX <= MinSize && sizeZ <= MinSize)
                {
                    try
                    {
                        return new Vector2i(
                            gameRandom.RandomRange(streetTile.WorldPosition.x + Offset, streetTile.WorldPosition.x + MinSize - sizeX - Offset),
                            gameRandom.RandomRange(streetTile.WorldPosition.y + Offset, streetTile.WorldPosition.y + MinSize - sizeZ - Offset)
                        );
                    }
                    catch
                    {
                        return streetTile.WorldPositionCenter - new Vector2i(sizeX / 2, sizeZ / 2);
                    }
                }

                return streetTile.WorldPositionCenter - new Vector2i((sizeX - MinSize) / 2, (sizeZ - MinSize) / 2);
            }

            private static Rect GetRect(Vector2i position, int sizeX, int sizeZ)
            {
                var size = Math.Max(sizeX, sizeZ);
                var rect = new Rect(position.x, position.y, size, size);
                var offset = new Vector2(size, size) / 2f;

                rect.min -= offset;
                rect.size += offset;
                rect.center = new Vector2(position.x + sizeZ / 2, position.y + sizeX / 2);

                return rect;
            }

            private static bool IsValidRect(Rect rect)
            {
                var worldSize = WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize;

                return rect.max.x < worldSize && rect.min.x >= 0 && rect.max.y < worldSize && rect.min.y >= 0;
            }

            private static List<int> GetHeights(Vector2i position, int sizeX, int sizeZ, BiomeType biome, int centerHeight)
            {
                var heights = new List<int>();

                for (int x = position.x; x < position.x + sizeX; x++)
                {
                    for (int y = position.y; y < position.y + sizeZ; y++)
                    {
                        if (!IsValidPosition(x, y, biome, centerHeight))
                        {
                            return null;
                        }

                        heights.Add((int)WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(x, y));
                    }
                }

                return heights;
            }

            private static bool IsValidPosition(int x, int y, BiomeType biome, int centerHeight)
            {
                var worldSize = WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize;

                return x < worldSize && x >= 0 && y < worldSize && y >= 0
                    && WorldGenerationEngineFinal.WorldBuilder.Instance.GetWater(x, y) <= 0
                    && biome == WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome(x, y)
                    && Math.Abs(Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(x, y)) - centerHeight) <= 11;
            }

            private static int GetMedianHeight(List<int> heights)
            {
                var sortedHeights = new List<int>(heights);
                sortedHeights.Sort();

                int count = sortedHeights.Count;
                if (count == 0)
                {
                    throw new InvalidOperationException("Empty list cannot have a median");
                }

                if (count % 2 == 1)
                {
                    return sortedHeights[count / 2];
                }
                else
                {
                    return (sortedHeights[count / 2 - 1] + sortedHeights[count / 2]) / 2;
                }
            }
        }
    }
}
