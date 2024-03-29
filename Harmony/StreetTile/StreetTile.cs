using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
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
                Log.Out("[MyTestMod] StreetTile.spawnWildernessPrefab Prefix");

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
                POITags noTags = POITags.none;
                Vector2i centerPosition = streetTile.WorldPositionCenter;
                PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(poiTags, noTags, default(Vector2i), default(Vector2i), false, centerPosition);
                int attemptCount = -1;
                int rotation;
                int width;
                int height;
                Vector2i position;
                Rect boundingRect;
                int medianHeight;
                for (; ; )
                {
                IL_60:
                    attemptCount++;
                    if (attemptCount >= 6)
                    {
                        break;
                    }
                    rotation = (wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 12)) % 4;
                    width = wildernessPrefab.size.x;
                    height = wildernessPrefab.size.z;
                    if (rotation == 1 || rotation == 3)
                    {
                        width = wildernessPrefab.size.z;
                        height = wildernessPrefab.size.x;
                    }
                    if (width > 150 || height > 150)
                    {
                        position = streetTile.WorldPositionCenter - new Vector2i((width - 150) / 2, (height - 150) / 2);
                    }
                    else
                    {
                        try
                        {
                            position = new Vector2i(gameRandom.RandomRange(streetTile.WorldPosition.x + 10, streetTile.WorldPosition.x + 150 - width - 10), gameRandom.RandomRange(streetTile.WorldPosition.y + 10, streetTile.WorldPosition.y + 150 - height - 10));
                        }
                        catch
                        {
                            position = streetTile.WorldPositionCenter - new Vector2i(width / 2, height / 2);
                        }
                    }
                    int maxDimension = (width > height) ? width : height;
                    boundingRect = new Rect(position.x, position.y, maxDimension, maxDimension);
                    new Rect(boundingRect.min - new Vector2(maxDimension, maxDimension) / 2f, boundingRect.size + new Vector2(maxDimension, maxDimension));
                    Rect extendedRect = new Rect(boundingRect.min - new Vector2(maxDimension, maxDimension) / 2f, boundingRect.size + new Vector2(maxDimension, maxDimension))
                    {
                        center = new Vector2(position.x + height / 2, position.y + width / 2)
                    };
                    if (extendedRect.max.x < WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize && extendedRect.min.x >= 0f && extendedRect.max.y < WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize && extendedRect.min.y >= 0f)
                    {
                        BiomeType biome = WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)boundingRect.center.x, (int)boundingRect.center.y);
                        medianHeight = Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight((int)boundingRect.center.x, (int)boundingRect.center.y));
                        List<int> heights = new List<int>();
                        for (int i = position.x; i < position.x + width; i++)
                        {
                            for (int j = position.y; j < position.y + height; j++)
                            {
                                if (i >= WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize || i < 0 || j >= WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize || j < 0 || WorldGenerationEngineFinal.WorldBuilder.Instance.GetWater(i, j) > 0 || biome != WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome(i, j) || Mathf.Abs(Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(i, j)) - medianHeight) > 11)
                                {
                                    goto IL_60;
                                }
                                heights.Add((int)WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(i, j));
                            }
                        }
                        medianHeight = GetMedianHeight(heights);
                        if (medianHeight + wildernessPrefab.yOffset >= 2)
                        {
                            return Block_20(ref rotation, ref width, ref height, ref position, ref boundingRect, ref medianHeight, ref wildernessPrefab, ref gameRandom);
                        }
                    }
                }
                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return false;
            }

            private static bool PlacePrefab(ref int rotation, ref int width, ref int height, ref Vector2i position, ref Rect boundingRect, ref int medianHeight, ref PrefabData wildernessPrefab, ref GameRandom gameRandom)
            {
                Vector3i position3D = new Vector3i(SubHalfWorld(position.x), GetHeightCeil(boundingRect.center) + wildernessPrefab.yOffset + 1, SubHalfWorld(position.y));
                Vector3i position3D2 = new Vector3i(SubHalfWorld(position.x), GetHeightCeil(boundingRect.center), SubHalfWorld(position.y));
                int prefabInstanceId = PrefabManager.PrefabInstanceId++;
                gameRandom.SetSeed(position.x + position.x * position.y + position.y);
                rotation = gameRandom.RandomRange(0, 4);
                rotation = (rotation + wildernessPrefab.RotationsToNorth & 3);
                Vector2 vector = new Vector2(position.x + width / 2, position.y + height / 2);
                if (rotation == 0)
                {
                    vector = new Vector2(position.x + width / 2, position.y);
                }
                else if (rotation == 1)
                {
                    vector = new Vector2(position.x + width, position.y + height / 2);
                }
                else if (rotation == 2)
                {
                    vector = new Vector2(position.x + width / 2, position.y + height);
                }
                else if (rotation == 3)
                {
                    vector = new Vector2(position.x, position.y + height / 2);
                }
                float maxDimension = 0f;
                if (wildernessPrefab.POIMarkers != null)
                {
                    List<Prefab.Marker> poiMarkers = wildernessPrefab.RotatePOIMarkers(true, rotation);
                    for (int i = poiMarkers.Count - 1; i >= 0; i--)
                    {
                        if (poiMarkers[i].MarkerType != Prefab.Marker.MarkerTypes.RoadExit)
                        {
                            poiMarkers.RemoveAt(i);
                        }
                    }
                    int index = gameRandom.RandomRange(0, poiMarkers.Count);
                    if (poiMarkers.Count > 0)
                    {
                        Vector3i start = poiMarkers[index].Start;
                        int markerSize = (poiMarkers[index].Size.x > poiMarkers[index].Size.z) ? poiMarkers[index].Size.x : poiMarkers[index].Size.z;
                        maxDimension = Mathf.Max(maxDimension, markerSize / 2f);
                        string groupName = poiMarkers[index].GroupName;
                        Vector2 markerCenter = new Vector2(start.x + poiMarkers[index].Size.x / 2f, start.z + poiMarkers[index].Size.z / 2f);
                        vector = new Vector2(position.x + markerCenter.x, position.y + markerCenter.y);
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
                }
                SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(position.x, Mathf.CeilToInt(medianHeight + wildernessPrefab.yOffset + 1), position.y), (byte)rotation);
                PrefabDataInstance prefabInstance = new PrefabDataInstance(prefabInstanceId, new Vector3i(position3D.x, medianHeight + wildernessPrefab.yOffset + 1, position3D.z), (byte)rotation, wildernessPrefab);
                AddPrefab(prefabInstance);
                WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount++;
                if (medianHeight != GetHeightCeil(boundingRect.min.x, boundingRect.min.y) || medianHeight != GetHeightCeil(boundingRect.max.x, boundingRect.min.y) || medianHeight != GetHeightCeil(boundingRect.min.x, boundingRect.max.y) || medianHeight != GetHeightCeil(boundingRect.max.x, boundingRect.max.y))
                {
                    WildernessPOICenter = new Vector2i(boundingRect.center);
                    WildernessPOISize = Mathf.RoundToInt(Mathf.Max(boundingRect.size.x, boundingRect.size.y));
                    WildernessPOIHeight = medianHeight;
                }
                if (maxDimension != 0f)
                {
                    WildernessPlanner.WildernessPathInfos.Add(new WorldGenerationEngineFinal.WorldBuilder.WildernessPathInfo(new Vector2i(vector), prefabInstanceId, maxDimension, WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)vector.x, (int)vector.y), 0, null));
                }
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
                startX = Mathf.FloorToInt(boundingRect.x) - 1;
                endX = Mathf.CeilToInt(boundingRect.xMax) + 1;
                startY = Mathf.FloorToInt(boundingRect.y) - 1;
                endY = Mathf.CeilToInt(boundingRect.yMax) + 1;
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
                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return true;
            }

            private static void SpawnMarkerPartsAndPrefabsWilderness(
              PrefabData _parentPrefab,
              Vector3i _parentPosition,
              int _parentRotations)
            {
                GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(_parentPosition.ToString().GetHashCode());
                List<Prefab.Marker> markerList = _parentPrefab.RotatePOIMarkers(true, _parentRotations);
                List<Prefab.Marker> all1 = markerList.FindAll(m => m.MarkerType == Prefab.Marker.MarkerTypes.POISpawn);
                List<Prefab.Marker> all2 = markerList.FindAll(m => m.MarkerType == Prefab.Marker.MarkerTypes.PartSpawn);
                if (all1.Count > 0)
                {
                    for (int index = 0; index < all1.Count; ++index)
                    {
                        Prefab.Marker marker = all1[index];
                        int num1 = _parentPosition.x + marker.Start.x;
                        int num2 = _parentPosition.z + marker.Start.z;
                        Vector2i maxSize = new Vector2i(marker.Size.x, marker.Size.z);
                        Vector2i vector2i1 = new Vector2i(marker.Start.x, marker.Start.z);
                        Vector2i vector2i2 = vector2i1 + maxSize;
                        Vector2i vector2i3 = vector2i1 + maxSize / 2;
                        Vector2i minSize = maxSize;
                        int rotations = marker.Rotations;
                        PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(traderTag, marker.Tags, maxSize, minSize, center: new Vector2i(_parentPosition.x + vector2i3.x, _parentPosition.z + vector2i3.y));
                        if (wildernessPrefab != null)
                        {
                            byte num3 = (byte)((_parentRotations + wildernessPrefab.RotationsToNorth + rotations) % 4);
                            int width = wildernessPrefab.size.x;
                            int height = wildernessPrefab.size.z;
                            if (num3 == 1 || num3 == 3)
                            {
                                int num4 = width;
                                width = height;
                                height = num4;
                            }
                            switch (rotations)
                            {
                                case 0:
                                    num1 += maxSize.x / 2 - width / 2;
                                    num2 = num2 + maxSize.y - height;
                                    break;
                                case 1:
                                    num2 += maxSize.y / 2 - height / 2;
                                    break;
                                case 2:
                                    num1 += maxSize.x / 2 - width / 2;
                                    break;
                                case 3:
                                    num2 += maxSize.y / 2 - height / 2;
                                    num1 = num1 + maxSize.x - width;
                                    break;
                            }
                            Rect rect = new Rect(num1, num2, width, height);
                            Vector3i _position = new Vector3i(num1 - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2, _parentPosition.y + marker.Start.y + wildernessPrefab.yOffset, num2 - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2);
                            AddPrefab(new PrefabDataInstance(PrefabManager.PrefabInstanceId++, _position, num3, wildernessPrefab));
                            ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                            wildernessPrefab.RotatePOIMarkers(true, num3);
                            SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(num1, _parentPosition.y + marker.Start.y + wildernessPrefab.yOffset, num2), num3);
                        }
                    }
                }
                if (all2.Count > 0)
                {
                    List<string> stringList = new List<string>();
                    for (int index = 0; index < all2.Count; ++index)
                    {
                        if (!(all2[index].GroupName.ToLower() == "highwayN") && !(all2[index].GroupName.ToLower() == "highwayS") && !(all2[index].GroupName.ToLower() == "highwayE") && !(all2[index].GroupName.ToLower() == "highwayW") && !stringList.Contains(all2[index].GroupName))
                            stringList.Add(all2[index].GroupName);
                    }
                    foreach (string str in stringList)
                    {
                        string groupName = str;
                        List<Prefab.Marker> all3 = all2.FindAll(m => m.GroupName == groupName);
                        float num5 = 1f;
                        if (all3.Count > 1)
                        {
                            num5 = 0.0f;
                            foreach (Prefab.Marker marker in all3)
                                num5 += marker.PartChanceToSpawn;
                        }
                        float num6 = 0.0f;
                        foreach (Prefab.Marker marker in all3)
                        {
                            num6 += marker.PartChanceToSpawn / num5;
                            if ((double)gameRandom.RandomRange(0.0f, 1f) <= (double)num6 && (marker.Tags.IsEmpty || wildernessTag.Test_AnySet(marker.Tags)))
                            {
                                PrefabData prefabByName = PrefabManager.GetPrefabByName(marker.PartToSpawn);
                                if (prefabByName == null)
                                {
                                    Log.Error("Part to spawn {0} not found!", marker.PartToSpawn);
                                }
                                else
                                {
                                    Vector3i _position = new Vector3i(_parentPosition.x + marker.Start.x - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2, _parentPosition.y + marker.Start.y, _parentPosition.z + marker.Start.z - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2);
                                    byte num7 = marker.Rotations;
                                    switch (num7)
                                    {
                                        case 1:
                                            num7 = 3;
                                            break;
                                        case 3:
                                            num7 = 1;
                                            break;
                                    }
                                    byte num8 = (byte)((_parentRotations + prefabByName.RotationsToNorth + num7) % 4);
                                    AddPrefab(new PrefabDataInstance(PrefabManager.PrefabInstanceId++, _position, num8, prefabByName));
                                    ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                                    SpawnMarkerPartsAndPrefabsWilderness(prefabByName, _parentPosition + marker.Start, num8);
                                    break;
                                }
                            }
                        }
                    }
                }
                GameRandomManager.Instance.FreeGameRandom(gameRandom);
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
