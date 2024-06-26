﻿using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorldGenerationEngineFinal;

namespace GeneratorPerformanceMod.Harmony.HighwayPlanner
{
    [HarmonyPatch(typeof(WorldGenerationEngineFinal.HighwayPlanner))]
    [HarmonyPatch("Plan")]
    internal static class HighwayPlanner
    {
        private static readonly List<ExitConnection> ExitConnections = new List<ExitConnection>();
        private static Path GetPathToTownshipResult;

        [HarmonyPrefix]
        private static bool Prefix(int worldSeed, ref IEnumerator __result)
        {
            Log.Out("[GeneratorPerformanceMod] Boosting HighwayPlanner Planning");
            __result = NewPlanMethod(worldSeed);
            return false;
        }

        private static IEnumerator NewPlanMethod(int worldSeed)
        {
            yield return WorldGenerationEngineFinal.WorldBuilder.Instance.SetMessage("Planning Highways");
            MicroStopwatch ms = new MicroStopwatch(true);
            ExitConnections.Clear();

            for (int i = 0; i < WorldGenerationEngineFinal.WorldBuilder.Instance.Townships.Count; i++)
            {
                Township township = WorldGenerationEngineFinal.WorldBuilder.Instance.Townships[i];
                for (int j = 0; j < township.Gateways.Count; j++)
                {
                    WorldGenerationEngineFinal.StreetTile gateway = township.Gateways[j];
                    gateway.SetAllExistingNeighborsForGateway();
                }
            }

            List<Township> highwayTownships = WorldGenerationEngineFinal.WorldBuilder.Instance.Townships.FindAll(_township => WorldGenerationEngineFinal.WorldBuilder.townshipDatas[_township.GetTypeName()].SpawnGateway);
            Shuffle(worldSeed + 3943 + 1, ref highwayTownships);

            TownshipNode currentTownshipNode = new TownshipNode(highwayTownships[0]);
            yield return PrimsAlgo(currentTownshipNode, highwayTownships);
            for (; currentTownshipNode != null; currentTownshipNode = currentTownshipNode.next)
            {
                if (currentTownshipNode.Path != null)
                    WorldGenerationEngineFinal.WorldBuilder.Instance.paths.Add(currentTownshipNode.Path);
                if (currentTownshipNode.next == null)
                    break;
            }

            currentTownshipNode = new TownshipNode(currentTownshipNode.Township);
            yield return PrimsAlgo(currentTownshipNode, highwayTownships);
            for (; currentTownshipNode != null; currentTownshipNode = currentTownshipNode.next)
            {
                if (currentTownshipNode.Path != null)
                    WorldGenerationEngineFinal.WorldBuilder.Instance.paths.Add(currentTownshipNode.Path);
                if (currentTownshipNode.next == null)
                    break;
            }

            currentTownshipNode = new TownshipNode(currentTownshipNode.Township);
            yield return PrimsAlgo(currentTownshipNode, highwayTownships);
            for (; currentTownshipNode != null; currentTownshipNode = currentTownshipNode.next)
            {
                if (currentTownshipNode.Path != null)
                    WorldGenerationEngineFinal.WorldBuilder.Instance.paths.Add(currentTownshipNode.Path);
            }

            yield return CleanupHighwayConnections(highwayTownships);
            RunTownshipDirtRoads();

            Log.Out(string.Format("HighwayPlanner.Plan took {0}", (float)(ms.ElapsedMilliseconds * (1.0 / 1000.0))));
        }

        private static IEnumerator CleanupHighwayConnections(List<Township> highwayTownships)
        {
            CheckIfExitConnectionsAreValid(highwayTownships);
            yield return CleanupOrphanedTiles(highwayTownships);
        }

        private static IEnumerator CleanupOrphanedTiles(List<Township> highwayTownships)
        {
            List<Vector2i> tilesToRemove = new List<Vector2i>();
            List<Path> pathsToRemove = new List<Path>();

            foreach (Township highwayTownship in highwayTownships)
            {
                Township township = highwayTownship;
                yield return WorldGenerationEngineFinal.WorldBuilder.Instance.SetMessage("Planning Highways");
                if (WorldGenerationEngineFinal.WorldBuilder.townshipDatas[township.GetTypeName()].SpawnGateway && township.Gateways.Count != 0)
                {
                    ProcessGateways(township, ref tilesToRemove, ref pathsToRemove);
                    RemoveTiles(tilesToRemove);
                    RemovePath(pathsToRemove);
                }
            }
        }

        private static void ProcessGateways(Township township, ref List<Vector2i> tilesToRemove, ref List<Path> pathsToRemove)
        {
            foreach (WorldGenerationEngineFinal.StreetTile gateway in township.Gateways)
            {
                SetGatewayExitsAsUnused(gateway);
                if (gateway.UsedExitList.Count < 2)
                {
                    DisconnectGatewayFromNeighbors(gateway);
                    DisconnectGatewayFromHighways(gateway, tilesToRemove, pathsToRemove);
                    tilesToRemove.Add(gateway.GridPosition);
                }
            }
        }

        private static void SetGatewayExitsAsUnused(WorldGenerationEngineFinal.StreetTile gateway)
        {
            const int NumberOfExits = 4;

            for (int index = 0; index < NumberOfExits; ++index)
            {
                Vector2i highwayExitPosition = gateway.getHighwayExitPosition(index);
                if (!gateway.UsedExitList.Contains(highwayExitPosition))
                {
                    gateway.SetExitUnUsed(highwayExitPosition);
                }
            }
        }

        private static void DisconnectGatewayFromNeighbors(WorldGenerationEngineFinal.StreetTile gateway)
        {
            const int NumberOfNeighbors = 4;

            for (int index = 0; index < NumberOfNeighbors; ++index)
            {
                WorldGenerationEngineFinal.StreetTile neighbor = gateway.GetNeighborByIndex(index);
                Vector2i gatewayExitPosition = gateway.getHighwayExitPosition(index);
                gateway.SetExitUnUsed(gatewayExitPosition);

                if (neighbor.Township == gateway.Township)
                {
                    Vector2i neighborExitPosition = neighbor.getHighwayExitPosition(neighbor.GetNeighborIndex(gateway));
                    neighbor.SetExitUnUsed(neighborExitPosition);
                }
            }
        }

        private static void DisconnectGatewayFromHighways(WorldGenerationEngineFinal.StreetTile gateway, List<Vector2i> tilesToRemove, List<Path> pathsToRemove)
        {
            foreach (Path connectedHighway in gateway.ConnectedHighways)
            {
                WorldGenerationEngineFinal.StreetTile streetTileWorld;
                if (WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(connectedHighway.StartPosition) == gateway)
                {
                    streetTileWorld = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(connectedHighway.EndPosition);
                    streetTileWorld.SetExitUnUsed(connectedHighway.EndPosition);
                }
                else
                {
                    streetTileWorld = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(connectedHighway.StartPosition);
                    streetTileWorld.SetExitUnUsed(connectedHighway.StartPosition);
                }
                if (streetTileWorld.UsedExitList.Count < 2)
                {
                    tilesToRemove.Add(streetTileWorld.GridPosition);
                }
                pathsToRemove.Add(connectedHighway);
            }
        }

        private static void RemovePath(List<Path> paths)
        {
            foreach (Path path in paths)
            {
                path.Dispose();
                WorldGenerationEngineFinal.WorldBuilder.Instance.paths.Remove(path);
            }
            paths.Clear();
        }

        private static void RemoveTiles(List<Vector2i> tilesToRemove)
        {
            foreach (Vector2i vector2i in tilesToRemove)
            {
                WorldGenerationEngineFinal.StreetTile streetTileGrid = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileGrid(vector2i);
                if (streetTileGrid.Township != null)
                {
                    streetTileGrid.Township.Gateways.Remove(streetTileGrid);
                    streetTileGrid.Township.Streets.Remove(vector2i);
                }
                streetTileGrid.StreetTilePrefabDatas.Clear();
                streetTileGrid.District = null;
                streetTileGrid.Township = null;
            }
            tilesToRemove.Clear();
        }

        private static void CheckIfExitConnectionsAreValid(List<Township> highwayTownships)
        {
            foreach (Township highwayTownship in highwayTownships)
            {
                if (highwayTownship.Gateways.Count == 0)
                {
                    Debug.LogError("Township has zero gateways! This should not happen!");
                    return;
                }

                foreach (WorldGenerationEngineFinal.StreetTile gateway in highwayTownship.Gateways)
                {
                    HashSet<Vector2i> usedExitSet = new HashSet<Vector2i>(gateway.UsedExitList);
                    for (int index = 0; index < 4; ++index)
                    {
                        WorldGenerationEngineFinal.StreetTile neighborByIndex = gateway.GetNeighborByIndex(index);
                        Vector2i highwayExitPosition = gateway.getHighwayExitPosition(index);
                        if (!usedExitSet.Contains(highwayExitPosition) && (neighborByIndex.Township != gateway.Township || !neighborByIndex.HasExitTo(gateway)))
                            gateway.SetExitUnUsed(highwayExitPosition);
                    }
                }
            }

            foreach (ExitConnection exitConnection in ExitConnections)
            {
                exitConnection.SetExitUsedManually();
            }
        }

        private static void RunTownshipDirtRoads()
        {
            var roadExitMapping = new Dictionary<int, int> { { 0, 2 }, { 1, 3 }, { 2, 0 }, { 3, 1 } };

            var townships = WorldGenerationEngineFinal.WorldBuilder.Instance.Townships.FindAll(_township => !WorldGenerationEngineFinal.WorldBuilder.townshipDatas[_township.GetTypeName()].SpawnGateway);

            System.Threading.Tasks.Parallel.ForEach(townships, town =>
            {
                foreach (WorldGenerationEngineFinal.StreetTile streetTile in town.Streets.Values)
                {
                    if (streetTile.GetNumTownshipNeighbors() == 1)
                    {
                        int num = -1;
                        for (int idx = 0; idx < 4; ++idx)
                        {
                            WorldGenerationEngineFinal.StreetTile neighborByIndex = streetTile.GetNeighborByIndex(idx);
                            if (neighborByIndex != null && neighborByIndex.Township == streetTile.Township)
                            {
                                num = idx;
                                break;
                            }
                        }

                        if (roadExitMapping.ContainsKey(num))
                        {
                            streetTile.SetRoadExit(roadExitMapping[num], true);
                            CreateDirtRoad(town);
                        }
                    }
                }
            });
        }

        private static IEnumerator CreateDirtRoad(Township township)
        {
            foreach (Vector2i exit in township.GetUnusedTownExits())
            {
                Vector2i closestPoint = Vector2i.zero;
                float closestDist = float.MaxValue;
                foreach (Path path in WorldGenerationEngineFinal.WorldBuilder.Instance.paths)
                {
                    MicroStopwatch microStopwatch = new MicroStopwatch(true);
                    if (!path.isCountryRoad)
                    {
                        foreach (Vector2 finalPathPoint in path.FinalPathPoints)
                        {
                            Vector2 pp = finalPathPoint;
                            float dist = Vector2i.DistanceSqr(exit, new Vector2i(pp));
                            if ((double)dist < (double)closestDist)
                            {
                                if (WorldGenerationEngineFinal.WorldBuilder.Instance.IsMessageElapsed())
                                    yield return WorldGenerationEngineFinal.WorldBuilder.Instance.SetMessage("Planning Highways");
                                if (PathingUtils.HasValidPath(exit, new Vector2i(pp), true))
                                {
                                    closestDist = dist;
                                    closestPoint = new Vector2i((int)pp.x, (int)pp.y);
                                }
                            }
                            pp = new Vector2();
                        }
                    }
                }

                if (WorldGenerationEngineFinal.WorldBuilder.Instance.IsMessageElapsed())
                {
                    yield return WorldGenerationEngineFinal.WorldBuilder.Instance.SetMessage("Planning Highways");
                }

                Path path1 = new Path(exit, closestPoint, 2, true, true, false, false);
                if (path1.IsValid)
                {
                    foreach (WorldGenerationEngineFinal.StreetTile streetTile in township.Streets.Values)
                    {
                        for (int index = 0; index < 4; ++index)
                        {
                            if ((double)Vector2i.Distance(streetTile.getHighwayExitPosition(index), exit) < 10.0)
                                streetTile.SetExitUsed(exit);
                        }
                    }
                    WorldGenerationEngineFinal.WorldBuilder.Instance.paths.Add(path1);
                }
            }
        }

        private static void Shuffle<T>(int seed, ref List<T> list)
        {
            GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(seed);
            int count = list.Count;
            while (count > 1)
            {
                --count;
                int index = gameRandom.RandomRange(0, count) % count;
                (list[count], list[index]) = (list[index], list[count]);
            }
            GameRandomManager.Instance.FreeGameRandom(gameRandom);
        }

        private static IEnumerator PrimsAlgo(TownshipNode head, List<Township> highwayTownships)
        {
            HashSet<Township> closedList = new HashSet<Township>();
            TownshipNode current = head;
            MicroStopwatch msReset = new MicroStopwatch(true);

            while (current != null)
            {
                int closestDist = int.MaxValue;
                Township closestTownship = null;

                var all = highwayTownships
                .Where(_township => !current.Township.ConnectedTownships.Contains(_township.ID) && !closedList.Contains(_township) && _township.ID != current.Township.ID && WorldGenerationEngineFinal.WorldBuilder.townshipDatas[_township.GetTypeName()].SpawnGateway)
                .OrderBy(_township => Vector2i.DistanceSqr(current.Township.GridCenter, _township.GridCenter))
                .ToList();

                msReset.ResetAndRestart();

                foreach (Township township in all)
                {
                    yield return GetPathToTownship(current.Township, township, msReset);
                    Path path = GetPathToTownshipResult;
                    GetPathToTownshipResult = null;

                    if (msReset.ElapsedMilliseconds > 500L)
                    {
                        yield return WorldGenerationEngineFinal.WorldBuilder.Instance.SetMessage("Planning Highways");
                        msReset.ResetAndRestart();
                    }

                    if (path != null)
                    {
                        current.SetPath(path);
                        closestTownship = township;
                        break;
                    }
                }

                if (closestTownship == null)
                {
                    current = current.next;
                    continue;
                }

                current.Township.ConnectedTownships.Add(closestTownship.ID);
                closestTownship.ConnectedTownships.Add(current.Township.ID);
                SetTileExits(current.Path);
                current.Path.commitPathingMapData();
                closedList.Add(current.Township);

                current.next = new TownshipNode(closestTownship)
                {
                    Distance = closestDist
                };

                current = current.next;
            }
        }

        private static void SetTileExits(Path path)
        {
            SetTileExit(path, path.StartPosition);
            SetTileExit(path, path.EndPosition);
        }

        private static void SetTileExit(Path currentPath, Vector2i exit)
        {
            WorldGenerationEngineFinal.StreetTile parent = WorldGenerationEngineFinal.WorldBuilder.Instance.GetStreetTileWorld(exit);
            if (parent != null)
            {
                if (parent.District != null && parent.District.name == "gateway")
                {
                    ExitConnections.Add(new ExitConnection(parent, exit, currentPath));
                    return;
                }
                foreach (WorldGenerationEngineFinal.StreetTile neighbor in parent.GetNeighbors())
                {
                    if (neighbor != null && neighbor.District != null && neighbor.District.name == "gateway")
                    {
                        ExitConnections.Add(new ExitConnection(neighbor, exit, currentPath));
                        return;
                    }
                }
                parent = null;
            }
            if (parent != null)
                return;
            Township township1 = null;
            foreach (Township township2 in WorldGenerationEngineFinal.WorldBuilder.Instance.Townships)
            {
                if (township2.Area.Contains(exit.AsVector2()))
                {
                    township1 = township2;
                    break;
                }
            }
            if (township1 == null)
                return;
            foreach (WorldGenerationEngineFinal.StreetTile gateway in township1.Gateways)
            {
                for (int index = 0; index < 4; ++index)
                {
                    if (gateway.getHighwayExitPosition(index) == exit || (double)Vector2i.DistanceSqr(gateway.getHighwayExitPosition(index), exit) < 100.0)
                    {
                        ExitConnections.Add(new ExitConnection(gateway, exit, currentPath));
                        return;
                    }
                }
            }
        }

        private static IEnumerator GetPathToTownship(
          Township thisTownship,
          Township otherTownship,
          MicroStopwatch msReset)
        {
            GetPathToTownshipResult = null;
            int closestDist = int.MaxValue;
            Path shortestPath = null;

            foreach (Vector2i unusedTownExit1 in thisTownship.GetUnusedTownExits())
            {
                foreach (Vector2i unusedTownExit2 in otherTownship.GetUnusedTownExits())
                {
                    Path path = new Path(unusedTownExit1, unusedTownExit2, 4, false, true, false, false);

                    if (path.IsValid && path.Cost < closestDist)
                    {
                        shortestPath?.Dispose();
                        closestDist = path.Cost;
                        shortestPath = path;
                    }
                    else
                    {
                        path.Dispose();
                    }
                }

                if (msReset.ElapsedMilliseconds > 500L)
                {
                    yield return null;
                    msReset.ResetAndRestart();
                }
            }

            GetPathToTownshipResult = shortestPath;
        }
    }
}
