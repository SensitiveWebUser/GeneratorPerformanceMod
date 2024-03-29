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
                POITags _withoutTags = WorldGenerationEngineFinal.WorldBuilder.Instance.Towns == WorldGenerationEngineFinal.WorldBuilder.GenerationSelections.None ? POITags.none : traderTag;
                POITags none = POITags.none;
                Vector2i worldPositionCenter = streetTile.WorldPositionCenter;
                Vector2i maxSize = new Vector2i();
                Vector2i minSize = new Vector2i();
                Vector2i center = worldPositionCenter;
                PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(_withoutTags, none, maxSize, minSize, center: center);
                int num1 = -1;
                label_1:
                int num2;
                int num3;
                Vector2i vector2i;
                Rect rect1;
                int medianHeight;
                do
                {
                    Rect rect2;
                    do
                    {
                        ++num1;
                        if (num1 >= 6)
                        {
                            GameRandomManager.Instance.FreeGameRandom(gameRandom);
                            return false;
                        }
                        int num4 = ((int)wildernessPrefab.RotationsToNorth + gameRandom.RandomRange(0, 12)) % 4;
                        num2 = wildernessPrefab.size.x;
                        num3 = wildernessPrefab.size.z;
                        if (num4 == 1 || num4 == 3)
                        {
                            num2 = wildernessPrefab.size.z;
                            num3 = wildernessPrefab.size.x;
                        }
                        if (num2 <= 150)
                        {
                            if (num3 <= 150)
                            {
                                try
                                {
                                    vector2i = new Vector2i(gameRandom.RandomRange(streetTile.WorldPosition.x + 10, streetTile.WorldPosition.x + 150 - num2 - 10), gameRandom.RandomRange(streetTile.WorldPosition.y + 10, streetTile.WorldPosition.y + 150 - num3 - 10));
                                    goto label_10;
                                }
                                catch
                                {
                                    vector2i = streetTile.WorldPositionCenter - new Vector2i(num2 / 2, num3 / 2);
                                    goto label_10;
                                }
                            }
                        }
                        vector2i = streetTile.WorldPositionCenter - new Vector2i((num2 - 150) / 2, (num3 - 150) / 2);
                        label_10:
                        int num5 = num2 > num3 ? num2 : num3;
                        rect1 = new Rect((float)vector2i.x, (float)vector2i.y, (float)num5, (float)num5);
                        Rect rect3 = new Rect(rect1.min - new Vector2((float)num5, (float)num5) / 2f, rect1.size + new Vector2((float)num5, (float)num5));
                        rect2 = new Rect(rect1.min - new Vector2((float)num5, (float)num5) / 2f, rect1.size + new Vector2((float)num5, (float)num5));
                        rect2.center = new Vector2((float)(vector2i.x + num3 / 2), (float)(vector2i.y + num2 / 2));
                    }
                    while ((double)rect2.max.x >= (double)WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize || (double)rect2.min.x < 0.0 || (double)rect2.max.y >= (double)WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize || (double)rect2.min.y < 0.0);
                    BiomeType biome = WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)rect1.center.x, (int)rect1.center.y);
                    int num6 = Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight((int)rect1.center.x, (int)rect1.center.y));
                    List<int> heights = new List<int>();
                    for (int x = vector2i.x; x < vector2i.x + num2; ++x)
                    {
                        for (int y = vector2i.y; y < vector2i.y + num3; ++y)
                        {
                            if (x < WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize && x >= 0 && y < WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize && y >= 0 && WorldGenerationEngineFinal.WorldBuilder.Instance.GetWater(x, y) <= (byte)0 && biome == WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome(x, y) && Mathf.Abs(Mathf.CeilToInt(WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(x, y)) - num6) <= 11)
                                heights.Add((int)WorldGenerationEngineFinal.WorldBuilder.Instance.GetHeight(x, y));
                            else
                                goto label_1;
                        }
                    }
                    medianHeight = GetMedianHeight(heights);
                }
                while (medianHeight + wildernessPrefab.yOffset < 2);
                Vector3i vector3i1 = new Vector3i(SubHalfWorld(vector2i.x), GetHeightCeil(rect1.center) + wildernessPrefab.yOffset + 1, SubHalfWorld(vector2i.y));
                Vector3i vector3i2 = new Vector3i(SubHalfWorld(vector2i.x), GetHeightCeil(rect1.center), SubHalfWorld(vector2i.y));
                int _id = PrefabManager.PrefabInstanceId++;
                gameRandom.SetSeed(vector2i.x + vector2i.x * vector2i.y + vector2i.y);
                int num7 = gameRandom.RandomRange(0, 4) + (int)wildernessPrefab.RotationsToNorth & 3;
                Vector2 vector2_1 = new Vector2((float)(vector2i.x + num2 / 2), (float)(vector2i.y + num3 / 2));
                switch (num7)
                {
                    case 0:
                        vector2_1 = new Vector2((float)(vector2i.x + num2 / 2), (float)vector2i.y);
                        break;
                    case 1:
                        vector2_1 = new Vector2((float)(vector2i.x + num2), (float)(vector2i.y + num3 / 2));
                        break;
                    case 2:
                        vector2_1 = new Vector2((float)(vector2i.x + num2 / 2), (float)(vector2i.y + num3));
                        break;
                    case 3:
                        vector2_1 = new Vector2((float)vector2i.x, (float)(vector2i.y + num3 / 2));
                        break;
                }
                float num8 = 0.0f;
                if (wildernessPrefab.POIMarkers != null)
                {
                    List<Prefab.Marker> markerList = wildernessPrefab.RotatePOIMarkers(true, num7);
                    for (int index = markerList.Count - 1; index >= 0; --index)
                    {
                        if (markerList[index].MarkerType != Prefab.Marker.MarkerTypes.RoadExit)
                            markerList.RemoveAt(index);
                    }
                    int index1 = gameRandom.RandomRange(0, markerList.Count);
                    if (markerList.Count > 0)
                    {
                        Vector3i start = markerList[index1].Start;
                        int num9 = markerList[index1].Size.x > markerList[index1].Size.z ? markerList[index1].Size.x : markerList[index1].Size.z;
                        num8 = Mathf.Max(num8, (float)num9 / 2f);
                        string groupName = markerList[index1].GroupName;
                        Vector2 vector2_2 = new Vector2((float)start.x + (float)markerList[index1].Size.x / 2f, (float)start.z + (float)markerList[index1].Size.z / 2f);
                        vector2_1 = new Vector2((float)vector2i.x + vector2_2.x, (float)vector2i.y + vector2_2.y);
                        Vector2 vector2_3 = vector2_1;
                        bool flag = false;
                        if (markerList.Count > 1)
                        {
                            List<Prefab.Marker> all = wildernessPrefab.POIMarkers.FindAll((Predicate<Prefab.Marker>)(m => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName));
                            if (all.Count > 0)
                            {
                                int index2 = gameRandom.RandomRange(0, all.Count);
                                vector2_3 = new Vector2((float)(vector2i.x + all[index2].Start.x) + (float)all[index2].Size.x / 2f, (float)(vector2i.y + all[index2].Start.z) + (float)all[index2].Size.z / 2f);
                            }
                            flag = true;
                        }
                        Path path = new Path(true, num8)
                        {
                            FinalPathPoints = {
              new Vector2(vector2_1.x, vector2_1.y)
            },
                            pathPoints3d = {
              new Vector3(vector2_1.x, (float) vector3i2.y, vector2_1.y)
            }
                        };
                        path.FinalPathPoints.Add(new Vector2(vector2_3.x, vector2_3.y));
                        path.pathPoints3d.Add(new Vector3(vector2_3.x, (float)vector3i2.y, vector2_3.y));
                        path.IsPrefabPath = flag;
                        path.StartPointID = _id;
                        path.EndPointID = _id;
                        WorldGenerationEngineFinal.WorldBuilder.Instance.wildernessPaths.Add(path);
                    }
                }
                SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(vector2i.x, Mathf.CeilToInt((float)(medianHeight + wildernessPrefab.yOffset + 1)), vector2i.y), (int)(byte)num7);
                AddPrefab(new PrefabDataInstance(_id, new Vector3i(vector3i1.x, medianHeight + wildernessPrefab.yOffset + 1, vector3i1.z), (byte)num7, wildernessPrefab));
                ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                if ((medianHeight != GetHeightCeil(rect1.min.x, rect1.min.y) || medianHeight != GetHeightCeil(rect1.max.x, rect1.min.y) || medianHeight != GetHeightCeil(rect1.min.x, rect1.max.y) ? 1 : (medianHeight != GetHeightCeil(rect1.max.x, rect1.max.y) ? 1 : 0)) != 0)
                {
                    WildernessPOICenter = new Vector2i(rect1.center);
                    WildernessPOISize = Mathf.RoundToInt(Mathf.Max(rect1.size.x, rect1.size.y));
                    WildernessPOIHeight = medianHeight;
                }
                if ((double)num8 != 0.0)
                    WildernessPlanner.WildernessPathInfos.Add(new WorldGenerationEngineFinal.WorldBuilder.WildernessPathInfo(new Vector2i(vector2_1), _id, num8, WorldGenerationEngineFinal.WorldBuilder.Instance.GetBiome((int)vector2_1.x, (int)vector2_1.y)));
                int num10 = Mathf.FloorToInt(rect1.x / 10f) - 1;
                int num11 = Mathf.CeilToInt(rect1.xMax / 10f) + 1;
                int num12 = Mathf.FloorToInt(rect1.y / 10f) - 1;
                int num13 = Mathf.CeilToInt(rect1.yMax / 10f) + 1;
                for (int x = num10; x < num11; ++x)
                {
                    for (int y = num12; y < num13; ++y)
                    {
                        if (x >= 0 && x < WorldGenerationEngineFinal.WorldBuilder.Instance.PathingGrid.GetLength(0) && y >= 0 && y < WorldGenerationEngineFinal.WorldBuilder.Instance.PathingGrid.GetLength(1))
                        {
                            if (x == num10 || x == num11 - 1 || y == num12 || y == num13 - 1)
                                PathingUtils.SetPathBlocked(x, y, (sbyte)2);
                            else
                                PathingUtils.SetPathBlocked(x, y, true);
                        }
                    }
                }
                int num14 = Mathf.FloorToInt(rect1.x) - 1;
                int num15 = Mathf.CeilToInt(rect1.xMax) + 1;
                int num16 = Mathf.FloorToInt(rect1.y) - 1;
                int num17 = Mathf.CeilToInt(rect1.yMax) + 1;
                for (int x = num14; x < num15; x += 150)
                {
                    for (int y = num16; y < num17; y += 150)
                    {
                        WorldGenerationEngineFinal.StreetTile streetTileWorld = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(x, y);
                        if (streetTileWorld != null)
                            streetTileWorld.Used = true;
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
                List<Prefab.Marker> all1 = markerList.FindAll((Predicate<Prefab.Marker>)(m => m.MarkerType == Prefab.Marker.MarkerTypes.POISpawn));
                List<Prefab.Marker> all2 = markerList.FindAll((Predicate<Prefab.Marker>)(m => m.MarkerType == Prefab.Marker.MarkerTypes.PartSpawn));
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
                        int rotations = (int)marker.Rotations;
                        PrefabData wildernessPrefab = PrefabManager.GetWildernessPrefab(traderTag, marker.Tags, maxSize, minSize, center: new Vector2i(_parentPosition.x + vector2i3.x, _parentPosition.z + vector2i3.y));
                        if (wildernessPrefab != null)
                        {
                            byte num3 = (byte)((_parentRotations + (int)wildernessPrefab.RotationsToNorth + rotations) % 4);
                            int width = wildernessPrefab.size.x;
                            int height = wildernessPrefab.size.z;
                            if (num3 == (byte)1 || num3 == (byte)3)
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
                            Rect rect = new Rect((float)num1, (float)num2, (float)width, (float)height);
                            Vector3i _position = new Vector3i(num1 - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2, _parentPosition.y + marker.Start.y + wildernessPrefab.yOffset, num2 - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2);
                            AddPrefab(new PrefabDataInstance(PrefabManager.PrefabInstanceId++, _position, num3, wildernessPrefab));
                            ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                            wildernessPrefab.RotatePOIMarkers(true, (int)num3);
                            SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(num1, _parentPosition.y + marker.Start.y + wildernessPrefab.yOffset, num2), (int)num3);
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
                        List<Prefab.Marker> all3 = all2.FindAll((Predicate<Prefab.Marker>)(m => m.GroupName == groupName));
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
                                    Log.Error("Part to spawn {0} not found!", (object)marker.PartToSpawn);
                                }
                                else
                                {
                                    Vector3i _position = new Vector3i(_parentPosition.x + marker.Start.x - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2, _parentPosition.y + marker.Start.y, _parentPosition.z + marker.Start.z - WorldGenerationEngineFinal.WorldBuilder.Instance.WorldSize / 2);
                                    byte num7 = marker.Rotations;
                                    switch (num7)
                                    {
                                        case 1:
                                            num7 = (byte)3;
                                            break;
                                        case 3:
                                            num7 = (byte)1;
                                            break;
                                    }
                                    byte num8 = (byte)((_parentRotations + (int)prefabByName.RotationsToNorth + (int)num7) % 4);
                                    AddPrefab(new PrefabDataInstance(PrefabManager.PrefabInstanceId++, _position, num8, prefabByName));
                                    ++WorldGenerationEngineFinal.WorldBuilder.Instance.WildernessPrefabCount;
                                    SpawnMarkerPartsAndPrefabsWilderness(prefabByName, _parentPosition + marker.Start, (int)num8);
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
