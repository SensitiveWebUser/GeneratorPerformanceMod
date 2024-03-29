using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorldGenerationEngineFinal;

namespace MyTestMod.Harmony.StreetTile
{
    internal class StreetTile
    {

        [HarmonyPatch(typeof(WorldGenerationEngineFinal.StreetTile), "spawnWildernessPrefab")]
        private static class SpawnWildernessPrefab
        {
            private static WorldGenerationEngineFinal.StreetTile streetTile;
            private static Vector2i WildernessPOICenter;
            private static int WildernessPOISize;
            private static int WildernessPOIHeight;
            private static POITags traderTag;
            private static POITags wildernessTag;

            [HarmonyPrefix]
            private static bool Prefix(WorldGenerationEngineFinal.StreetTile __instance, ref bool __result, ref Vector2i ___WildernessPOICenter, ref int ___WildernessPOISize, ref int ___WildernessPOIHeight, ref POITags ___traderTag, ref POITags ___wildernessTag)
            {
                streetTile = __instance;
                WildernessPOICenter = ___WildernessPOICenter;
                WildernessPOISize = ___WildernessPOISize;
                WildernessPOIHeight = ___WildernessPOIHeight;
                traderTag = ___traderTag;
                wildernessTag = ___wildernessTag;

                __result = NewSpawnWildernessPrefabMethod();

                return false;
            }

            private static bool NewSpawnWildernessPrefabMethod()
            {
                GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(WorldGenerationEngineFinal.WorldBuilder.Instance.Seed + 4096953);
                POITags poiTags = (WorldGenerationEngineFinal.WorldBuilder.Instance.Towns == WorldGenerationEngineFinal.WorldBuilder.GenerationSelections.None) ? POITags.none : traderTag;
                Vector2i centerPosition = streetTile.WorldPositionCenter;
                PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(poiTags, POITags.none, default(Vector2i), default(Vector2i), false, centerPosition);
                int attemptCount = -1;
                int rotation;
                int width;
                int height;
                Vector2i position;
                Rect boundingRect;
                int medianHeight;
                int worldSize = WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize; // Cache world size

                do
                {
                    attemptCount++;
                    if (attemptCount >= 6)
                    {
                        break;
                    }
                    rotation = (wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 12)) % 4;
                    width = (rotation == 1 || rotation == 3) ? wildernessPrefab.size.z : wildernessPrefab.size.x;
                    height = (rotation == 1 || rotation == 3) ? wildernessPrefab.size.x : wildernessPrefab.size.z;

                    position = CalculatePosition(width, height, gameRandom);
                    int maxDimension = Math.Max(width, height);
                    boundingRect = new Rect(position.x, position.y, maxDimension, maxDimension);
                    Rect extendedRect = new Rect(boundingRect.min - new Vector2(maxDimension, maxDimension) / 2f, boundingRect.size + new Vector2(maxDimension, maxDimension))
                    {
                        center = new Vector2(position.x + height / 2, position.y + width / 2)
                    };

                    if (extendedRect.max.x < worldSize && extendedRect.min.x >= 0f && extendedRect.max.y < worldSize && extendedRect.min.y >= 0f)
                    {
                        BiomeType biome = WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)boundingRect.center.x, (int)boundingRect.center.y);
                        medianHeight = Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight((int)boundingRect.center.x, (int)boundingRect.center.y));
                        List<int> heights = GetHeights(position, width, height, biome, medianHeight, worldSize);

                        if (heights.Count > 0)
                        {
                            medianHeight = GetMedianHeight(heights);
                            if (medianHeight + wildernessPrefab.yOffset >= 2)
                            {
                                return PlacePrefab(ref rotation, ref width, ref height, ref position, ref boundingRect, ref medianHeight, ref wildernessPrefab, ref gameRandom);
                            }
                        }
                    }
                } while (attemptCount < 6);

                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return false;
            }

            private static Vector2i CalculatePosition(int width, int height, GameRandom gameRandom)
            {
                Vector2i position;

                if (width > 150 || height > 150)
                {
                    position = streetTile.WorldPositionCenter - new Vector2i((width - 150) / 2, (height - 150) / 2);
                }
                else
                {
                    int xRange = streetTile.WorldPosition.x + 150 - width - 10;
                    int yRange = streetTile.WorldPosition.y + 150 - height - 10;

                    if (xRange > 10 && yRange > 10)
                    {
                        position = new Vector2i(gameRandom.RandomRange(streetTile.WorldPosition.x + 10, xRange), gameRandom.RandomRange(streetTile.WorldPosition.y + 10, yRange));
                    }
                    else
                    {
                        position = streetTile.WorldPositionCenter - new Vector2i(width / 2, height / 2);
                    }
                }

                return position;
            }

            private static List<int> GetHeights(Vector2i position, int width, int height, BiomeType biome, int medianHeight, int worldSize)
            {
                List<int> heights = new List<int>();
                WorldGenerationEngineFinal.WorldBuilder worldBuilder = WorldGenerationEngineFinal.WorldBuilder.Instance;

                int xEnd = position.x + width;
                int yEnd = position.y + height;

                for (int i = position.x; i < xEnd; i++)
                {
                    for (int j = position.y; j < yEnd; j++)
                    {
                        if (i >= 0 && i < worldSize && j >= 0 && j < worldSize)
                        {
                            if (worldBuilder.GetWater(i, j) <= 0 && biome == worldBuilder.GetBiome(i, j))
                            {
                                int heightAtPoint = Mathf.CeilToInt(worldBuilder.GetHeight(i, j));

                                if (Mathf.Abs(heightAtPoint - medianHeight) <= 11)
                                {
                                    heights.Add(heightAtPoint);
                                }
                            }
                        }
                    }
                }

                return heights;
            }

            private static bool PlacePrefab(ref int rotation, ref int width, ref int height, ref Vector2i position, ref Rect boundingRect, ref int medianHeight, ref PrefabData wildernessPrefab, ref GameRandom gameRandom)
            {
                Vector3i position3D = new Vector3i(SubHalfWorld(position.x), GetHeightCeil(boundingRect.center) + wildernessPrefab.yOffset + 1, SubHalfWorld(position.y));
                Vector3i position3D2 = new Vector3i(SubHalfWorld(position.x), GetHeightCeil(boundingRect.center), SubHalfWorld(position.y));
                int prefabInstanceId = PrefabManager.PrefabInstanceId++;
                gameRandom.SetSeed(position.x + position.x * position.y + position.y);
                rotation = gameRandom.RandomRange(0, 4);
                rotation = (rotation + wildernessPrefab.RotationsToNorth & 3);
                Vector2 vector = CalculateVector(position, width, height, rotation);
                float maxDimension = 0f;

                if (wildernessPrefab.POIMarkers != null)
                {
                    maxDimension = ProcessPOIMarkers(wildernessPrefab, gameRandom, rotation, position, position3D2, prefabInstanceId, maxDimension);
                }

                SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(position.x, Mathf.CeilToInt(medianHeight + wildernessPrefab.yOffset + 1), position.y), (byte)rotation);
                PrefabDataInstance prefabInstance = new PrefabDataInstance(prefabInstanceId, new Vector3i(position3D.x, medianHeight + wildernessPrefab.yOffset + 1, position3D.z), (byte)rotation, wildernessPrefab);
                AddPrefab(prefabInstance);
                WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount++;

                UpdateWildernessPOI(boundingRect, medianHeight);

                if (maxDimension != 0f)
                {
                    WildernessPlanner.WildernessPathInfos.Add(new WorldGenerationEngineFinal.WorldBuilder.WildernessPathInfo(new Vector2i(vector), prefabInstanceId, maxDimension, WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)vector.x, (int)vector.y), 0, null));
                }

                UpdatePathingGrid(boundingRect);
                UpdateStreetTileWorld(boundingRect);

                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return true;
            }

            private static Vector2 CalculateVector(Vector2i position, int width, int height, int rotation)
            {
                switch (rotation)
                {
                    case 0:
                        return new Vector2(position.x + width / 2, position.y);
                    case 1:
                        return new Vector2(position.x + width, position.y + height / 2);
                    case 2:
                        return new Vector2(position.x + width / 2, position.y + height);
                    case 3:
                        return new Vector2(position.x, position.y + height / 2);
                    default:
                        return Vector2.zero;
                }
            }

            private static float ProcessPOIMarkers(PrefabData wildernessPrefab, GameRandom gameRandom, int rotation, Vector2i position, Vector3i position3D2, int prefabInstanceId, float maxDimension)
            {
                List<Prefab.Marker> poiMarkers = wildernessPrefab.RotatePOIMarkers(true, rotation);
                poiMarkers.RemoveAll(marker => marker.MarkerType != Prefab.Marker.MarkerTypes.RoadExit);
                int index = gameRandom.RandomRange(0, poiMarkers.Count);

                if (poiMarkers.Count > 0)
                {
                    Vector3i start = poiMarkers[index].Start;
                    int markerSize = Math.Max(poiMarkers[index].Size.x, poiMarkers[index].Size.z);
                    maxDimension = Mathf.Max(maxDimension, markerSize / 2f);
                    string groupName = poiMarkers[index].GroupName;
                    Vector2 markerCenter = new Vector2(start.x + poiMarkers[index].Size.x / 2f, start.z + poiMarkers[index].Size.z / 2f);
                    Vector2 vector = new Vector2(position.x + markerCenter.x, position.y + markerCenter.y);
                    Vector2 vector3 = vector;
                    bool isPrefabPath = false;

                    if (poiMarkers.Count > 1)
                    {
                        poiMarkers = wildernessPrefab.POIMarkers.FindAll((Prefab.Marker m) => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName);
                        if (poiMarkers.Count > 0)
                        {
                            index = gameRandom.RandomRange(0, poiMarkers.Count);
                            vector3 = new Vector2(position.x + poiMarkers[index].Start.x + poiMarkers[index].Size.x / 2f, position.y + poiMarkers[index].Start.z + poiMarkers[index].Size.z / 2f);
                        }
                        isPrefabPath = true;
                    }

                    Path path = new Path(true, maxDimension, false, false);
                    path.FinalPathPoints.Add(new Vector2(vector.x, vector.y));
                    path.pathPoints3d.Add(new Vector3(vector.x, position3D2.y, vector.y));
                    path.FinalPathPoints.Add(new Vector2(vector3.x, vector3.y));
                    path.pathPoints3d.Add(new Vector3(vector3.x, position3D2.y, vector3.y));
                    path.IsPrefabPath = isPrefabPath;
                    path.StartPointID = prefabInstanceId;
                    path.EndPointID = prefabInstanceId;
                    WorldGenerationEngineFinal.WorldBuilder.Instance.wildernessPaths.Add(path);
                }

                return maxDimension;
            }

            private static void UpdateWildernessPOI(Rect boundingRect, int medianHeight)
            {
                if (medianHeight != GetHeightCeil(boundingRect.min.x, boundingRect.min.y) || medianHeight != GetHeightCeil(boundingRect.max.x, boundingRect.min.y) || medianHeight != GetHeightCeil(boundingRect.min.x, boundingRect.max.y) || medianHeight != GetHeightCeil(boundingRect.max.x, boundingRect.max.y))
                {
                    WildernessPOICenter = new Vector2i(boundingRect.center);
                    WildernessPOISize = Mathf.RoundToInt(Mathf.Max(boundingRect.size.x, boundingRect.size.y));
                    WildernessPOIHeight = medianHeight;
                }
            }

            private static void UpdatePathingGrid(Rect boundingRect)
            {
                int startX = Mathf.FloorToInt(boundingRect.x / 10f) - 1;
                int endX = Mathf.CeilToInt(boundingRect.xMax / 10f) + 1;
                int startY = Mathf.FloorToInt(boundingRect.y / 10f) - 1;
                int endY = Mathf.CeilToInt(boundingRect.yMax / 10f) + 1;

                for (int i = startX; i < endX; i++)
                {
                    for (int j = startY; j < endY; j++)
                    {
                        if (i >= 0 && i < WorldGenerationEngineFinal.WorldBuilder.Instance.PathingGrid.GetLength(0) && j >= 0 && j < WorldGenerationEngineFinal.WorldBuilder.Instance.PathingGrid.GetLength(1))
                        {
                            if (i == startX || i == endX - 1 || j == startY || j == endY - 1)
                            {
                                PathingUtils.SetPathBlocked(i, j, 2);
                            }
                            else
                            {
                                PathingUtils.SetPathBlocked(i, j, true);
                            }
                        }
                    }
                }
            }

            private static void UpdateStreetTileWorld(Rect boundingRect)
            {
                int startX = Mathf.FloorToInt(boundingRect.x) - 1;
                int endX = Mathf.CeilToInt(boundingRect.xMax) + 1;
                int startY = Mathf.FloorToInt(boundingRect.y) - 1;
                int endY = Mathf.CeilToInt(boundingRect.yMax) + 1;

                for (int i = startX; i < endX; i += 150)
                {
                    for (int j = startY; j < endY; j += 150)
                    {
                        WorldGenerationEngineFinal.StreetTile streetTileWorld = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(i, j);
                        if (streetTileWorld != null)
                        {
                            streetTile.Used = true;
                        }
                    }
                }
            }

            private static void SpawnMarkerPartsAndPrefabsWilderness(PrefabData _parentPrefab, Vector3i _parentPosition, int _parentRotations)
            {
                GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(_parentPosition.ToString().GetHashCode());
                List<Prefab.Marker> markerList = _parentPrefab.RotatePOIMarkers(true, _parentRotations);
                List<Prefab.Marker> poiSpawnMarkers = markerList.FindAll(m => m.MarkerType == Prefab.Marker.MarkerTypes.POISpawn);
                List<Prefab.Marker> partSpawnMarkers = markerList.FindAll(m => m.MarkerType == Prefab.Marker.MarkerTypes.PartSpawn);

                ProcessPOISpawnMarkers(poiSpawnMarkers, _parentPosition, _parentRotations, gameRandom);
                ProcessPartSpawnMarkers(partSpawnMarkers, _parentPosition, _parentRotations, gameRandom);

                GameRandomManager.Instance.FreeGameRandom(gameRandom);
            }

            private static void ProcessPOISpawnMarkers(List<Prefab.Marker> poiSpawnMarkers, Vector3i _parentPosition, int _parentRotations, GameRandom gameRandom)
            {
                foreach (Prefab.Marker marker in poiSpawnMarkers)
                {
                    Vector2i position = CalculatePosition(marker, _parentPosition);
                    Vector2i maxSize = new Vector2i(marker.Size.x, marker.Size.z);
                    Vector2i center = new Vector2i(_parentPosition.x + maxSize.x / 2, _parentPosition.z + maxSize.y / 2);
                    PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(traderTag, marker.Tags, maxSize, maxSize, center: center);

                    if (wildernessPrefab != null)
                    {
                        byte rotation = CalculateRotation(_parentRotations, wildernessPrefab, marker);
                        Vector2i size = CalculateSize(wildernessPrefab, rotation);
                        Rect rect = new Rect(position.x, position.y, size.x, size.y);
                        Vector3i _position = new Vector3i(position.x - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2, _parentPosition.y + marker.Start.y + wildernessPrefab.yOffset, position.y - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2);
                        AddPrefab(new PrefabDataInstance(PrefabManager.PrefabInstanceId++, _position, rotation, wildernessPrefab));
                        ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                        wildernessPrefab.RotatePOIMarkers(true, rotation);
                        SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(position.x, _parentPosition.y + marker.Start.y + wildernessPrefab.yOffset, position.y), rotation);
                    }
                }
            }

            private static void ProcessPartSpawnMarkers(List<Prefab.Marker> partSpawnMarkers, Vector3i _parentPosition, int _parentRotations, GameRandom gameRandom)
            {
                List<string> groupNames = partSpawnMarkers.Select(m => m.GroupName.ToLower()).Distinct().ToList();
                groupNames.RemoveAll(name => name == "highwayN" || name == "highwayS" || name == "highwayE" || name == "highwayW");

                foreach (string groupName in groupNames)
                {
                    List<Prefab.Marker> groupMarkers = partSpawnMarkers.FindAll(m => m.GroupName == groupName);
                    float totalChance = groupMarkers.Sum(m => m.PartChanceToSpawn);
                    float cumulativeChance = 0f;

                    foreach (Prefab.Marker marker in groupMarkers)
                    {
                        cumulativeChance += marker.PartChanceToSpawn / totalChance;
                        if (gameRandom.RandomRange(0.0f, 1f) <= cumulativeChance && (marker.Tags.IsEmpty || wildernessTag.Test_AnySet(marker.Tags)))
                        {
                            PrefabData prefabByName = PrefabManager.GetPrefabByName(marker.PartToSpawn);
                            if (prefabByName == null)
                            {
                                Log.Error("Part to spawn {0} not found!", marker.PartToSpawn);
                            }
                            else
                            {
                                Vector3i _position = new Vector3i(_parentPosition.x + marker.Start.x - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2, _parentPosition.y + marker.Start.y, _parentPosition.z + marker.Start.z - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2);
                                byte rotation = CalculateRotation(_parentRotations, prefabByName, marker);
                                AddPrefab(new PrefabDataInstance(PrefabManager.PrefabInstanceId++, _position, rotation, prefabByName));
                                ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                                SpawnMarkerPartsAndPrefabsWilderness(prefabByName, _parentPosition + marker.Start, rotation);
                                break;
                            }
                        }
                    }
                }
            }

            private static Vector2i CalculatePosition(Prefab.Marker marker, Vector3i _parentPosition)
            {
                int x = _parentPosition.x + marker.Start.x;
                int y = _parentPosition.z + marker.Start.z;
                return new Vector2i(x, y);
            }

            private static byte CalculateRotation(int _parentRotations, PrefabData prefab, Prefab.Marker marker)
            {
                byte rotation = (byte)((_parentRotations + prefab.RotationsToNorth + marker.Rotations) % 4);
                return rotation;
            }

            private static Vector2i CalculateSize(PrefabData prefab, byte rotation)
            {
                int width = prefab.size.x;
                int height = prefab.size.z;
                if (rotation == 1 || rotation == 3)
                {
                    (height, width) = (width, height);
                }
                return new Vector2i(width, height);
            }

            private static void AddPrefab(PrefabDataInstance pdi)
            {
                streetTile.StreetTilePrefabDatas.Add(pdi);
                if (streetTile.Township != null)
                    streetTile.Township.AddPrefab(pdi);
                else
                    PrefabManager.AddUsedPrefabWorld(-1, pdi);
            }

            private static int GetHeightCeil(Vector2 r) => Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(r));

            private static int GetHeightCeil(float x, float y)
            {
                return Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(x, y));
            }

            private static int SubHalfWorld(int pos) => pos - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2;

            private static int GetMedianHeight(List<int> heights)
            {
                heights.Sort();
                int count = heights.Count;
                int index = count / 2;
                return count % 2 == 0 ? (heights[index] + heights[index - 1]) / 2 : heights[index];
            }
        }
    }
}
