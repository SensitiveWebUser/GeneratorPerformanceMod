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
                POITags withoutTags = (WorldGenerationEngineFinal.WorldBuilder.Instance.Towns == WorldGenerationEngineFinal.WorldBuilder.GenerationSelections.None) ? POITags.none : traderTag;
                POITags none = POITags.none;
                Vector2i worldPositionCenter = streetTile.WorldPositionCenter;
                PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(withoutTags, none, default(Vector2i), default(Vector2i), false, worldPositionCenter);
                int num = -1;
                int num2;
                int num3;
                int num4;
                Vector2i vector2i;
                Rect rect;
                int num6;
                for (; ; )
                {
                    IL_60:
                    num++;
                    if (num >= 6)
                    {
                        break;
                    }
                    num2 = (wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 12)) % 4;
                    num3 = wildernessPrefab.size.x;
                    num4 = wildernessPrefab.size.z;
                    if (num2 == 1 || num2 == 3)
                    {
                        num3 = wildernessPrefab.size.z;
                        num4 = wildernessPrefab.size.x;
                    }
                    if (num3 > 150 || num4 > 150)
                    {
                        vector2i = streetTile.WorldPositionCenter - new Vector2i((num3 - 150) / 2, (num4 - 150) / 2);
                    }
                    else
                    {
                        try
                        {
                            vector2i = new Vector2i(gameRandom.RandomRange(streetTile.WorldPosition.x + 10, streetTile.WorldPosition.x + 150 - num3 - 10), gameRandom.RandomRange(streetTile.WorldPosition.y + 10, streetTile.WorldPosition.y + 150 - num4 - 10));
                        }
                        catch
                        {
                            vector2i = streetTile.WorldPositionCenter - new Vector2i(num3 / 2, num4 / 2);
                        }
                    }
                    int num5 = (num3 > num4) ? num3 : num4;
                    rect = new Rect(vector2i.x, vector2i.y, num5, num5);
                    new Rect(rect.min - new Vector2(num5, num5) / 2f, rect.size + new Vector2(num5, num5));
                    Rect rect2 = new Rect(rect.min - new Vector2(num5, num5) / 2f, rect.size + new Vector2(num5, num5))
                    {
                        center = new Vector2(vector2i.x + num4 / 2, vector2i.y + num3 / 2)
                    };
                    if (rect2.max.x < WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize && rect2.min.x >= 0f && rect2.max.y < WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize && rect2.min.y >= 0f)
                    {
                        BiomeType biome = WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)rect.center.x, (int)rect.center.y);
                        num6 = Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight((int)rect.center.x, (int)rect.center.y));
                        List<int> list = new List<int>();
                        for (int i = vector2i.x; i < vector2i.x + num3; i++)
                        {
                            for (int j = vector2i.y; j < vector2i.y + num4; j++)
                            {
                                if (i >= WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize || i < 0 || j >= WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize || j < 0 || WorldGenerationEngineFinal.WorldBuilder.Instance.GetWater(i, j) > 0 || biome != WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome(i, j) || Mathf.Abs(Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(i, j)) - num6) > 11)
                                {
                                    goto IL_60;
                                }
                                list.Add((int)WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(i, j));
                            }
                        }
                        num6 = GetMedianHeight(list);
                        if (num6 + wildernessPrefab.yOffset >= 2)
                        {
                            goto Block_20;
                        }
                    }
                }
                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return false;
                Block_20:
                Vector3i vector3i = new Vector3i(SubHalfWorld(vector2i.x), GetHeightCeil(rect.center) + wildernessPrefab.yOffset + 1, SubHalfWorld(vector2i.y));
                Vector3i vector3i2 = new Vector3i(SubHalfWorld(vector2i.x), GetHeightCeil(rect.center), SubHalfWorld(vector2i.y));
                int num7 = PrefabManager.PrefabInstanceId++;
                gameRandom.SetSeed(vector2i.x + vector2i.x * vector2i.y + vector2i.y);
                num2 = gameRandom.RandomRange(0, 4);
                num2 = (num2 + wildernessPrefab.RotationsToNorth & 3);
                Vector2 vector = new Vector2(vector2i.x + num3 / 2, vector2i.y + num4 / 2);
                if (num2 == 0)
                {
                    vector = new Vector2(vector2i.x + num3 / 2, vector2i.y);
                }
                else if (num2 == 1)
                {
                    vector = new Vector2(vector2i.x + num3, vector2i.y + num4 / 2);
                }
                else if (num2 == 2)
                {
                    vector = new Vector2(vector2i.x + num3 / 2, vector2i.y + num4);
                }
                else if (num2 == 3)
                {
                    vector = new Vector2(vector2i.x, vector2i.y + num4 / 2);
                }
                float num8 = 0f;
                if (wildernessPrefab.POIMarkers != null)
                {
                    List<Prefab.Marker> list2 = wildernessPrefab.RotatePOIMarkers(true, num2);
                    for (int k = list2.Count - 1; k >= 0; k--)
                    {
                        if (list2[k].MarkerType != Prefab.Marker.MarkerTypes.RoadExit)
                        {
                            list2.RemoveAt(k);
                        }
                    }
                    int index = gameRandom.RandomRange(0, list2.Count);
                    if (list2.Count > 0)
                    {
                        Vector3i start = list2[index].Start;
                        int num9 = (list2[index].Size.x > list2[index].Size.z) ? list2[index].Size.x : list2[index].Size.z;
                        num8 = Mathf.Max(num8, num9 / 2f);
                        string groupName = list2[index].GroupName;
                        Vector2 vector2 = new Vector2(start.x + list2[index].Size.x / 2f, start.z + list2[index].Size.z / 2f);
                        vector = new Vector2(vector2i.x + vector2.x, vector2i.y + vector2.y);
                        Vector2 vector3 = vector;
                        bool isPrefabPath = false;
                        if (list2.Count > 1)
                        {
                            list2 = wildernessPrefab.POIMarkers.FindAll((Prefab.Marker m) => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName);
                            if (list2.Count > 0)
                            {
                                index = gameRandom.RandomRange(0, list2.Count);
                                vector3 = new Vector2(vector2i.x + list2[index].Start.x + list2[index].Size.x / 2f, vector2i.y + list2[index].Start.z + list2[index].Size.z / 2f);
                            }
                            isPrefabPath = true;
                        }
                        Path path = new Path(true, num8, false, false);
                        path.FinalPathPoints.Add(new Vector2(vector.x, vector.y));
                        path.pathPoints3d.Add(new Vector3(vector.x, vector3i2.y, vector.y));
                        path.FinalPathPoints.Add(new Vector2(vector3.x, vector3.y));
                        path.pathPoints3d.Add(new Vector3(vector3.x, vector3i2.y, vector3.y));
                        path.IsPrefabPath = isPrefabPath;
                        path.StartPointID = num7;
                        path.EndPointID = num7;
                        WorldGenerationEngineFinal.WorldBuilder.Instance.wildernessPaths.Add(path);
                    }
                }
                SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(vector2i.x, Mathf.CeilToInt(num6 + wildernessPrefab.yOffset + 1), vector2i.y), (byte)num2);
                PrefabDataInstance pdi = new PrefabDataInstance(num7, new Vector3i(vector3i.x, num6 + wildernessPrefab.yOffset + 1, vector3i.z), (byte)num2, wildernessPrefab);
                AddPrefab(pdi);
                WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount++;
                if (num6 != GetHeightCeil(rect.min.x, rect.min.y) || num6 != GetHeightCeil(rect.max.x, rect.min.y) || num6 != GetHeightCeil(rect.min.x, rect.max.y) || num6 != GetHeightCeil(rect.max.x, rect.max.y))
                {
                    WildernessPOICenter = new Vector2i(rect.center);
                    WildernessPOISize = Mathf.RoundToInt(Mathf.Max(rect.size.x, rect.size.y));
                    WildernessPOIHeight = num6;
                }
                if (num8 != 0f)
                {
                    WildernessPlanner.WildernessPathInfos.Add(new WorldGenerationEngineFinal.WorldBuilder.WildernessPathInfo(new Vector2i(vector), num7, num8, WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)vector.x, (int)vector.y), 0, null));
                }
                int num10 = Mathf.FloorToInt(rect.x / 10f) - 1;
                int num11 = Mathf.CeilToInt(rect.xMax / 10f) + 1;
                int num12 = Mathf.FloorToInt(rect.y / 10f) - 1;
                int num13 = Mathf.CeilToInt(rect.yMax / 10f) + 1;
                for (int l = num10; l < num11; l++)
                {
                    for (int m2 = num12; m2 < num13; m2++)
                    {
                        if (l >= 0 && l < WorldGenerationEngineFinal.WorldBuilder.Instance.PathingGrid.GetLength(0) && m2 >= 0 && m2 < WorldGenerationEngineFinal.WorldBuilder.Instance.PathingGrid.GetLength(1))
                        {
                            if (l == num10 || l == num11 - 1 || m2 == num12 || m2 == num13 - 1)
                            {
                                PathingUtils.SetPathBlocked(l, m2, 2);
                            }
                            else
                            {
                                PathingUtils.SetPathBlocked(l, m2, true);
                            }
                        }
                    }
                }
                num10 = Mathf.FloorToInt(rect.x) - 1;
                num11 = Mathf.CeilToInt(rect.xMax) + 1;
                num12 = Mathf.FloorToInt(rect.y) - 1;
                num13 = Mathf.CeilToInt(rect.yMax) + 1;
                for (int n = num10; n < num11; n += 150)
                {
                    for (int num14 = num12; num14 < num13; num14 += 150)
                    {
                        WorldGenerationEngineFinal.StreetTile streetTileWorld = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(n, num14);
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
