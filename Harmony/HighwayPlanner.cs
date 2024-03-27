using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WorldGenerationEngineFinal;

namespace MyTestMod.Harmony
{
    internal class HighwayPlanner
    {
        [HarmonyPatch(typeof(WorldGenerationEngineFinal.HighwayPlanner))]
        [HarmonyPatch("Plan")]
        public class Plan
        {
            private static bool Prefix(int worldSeed, ref IEnumerator __result)
            {
                Log.Out("[MOD]======HighwayPlanner.Plan Prefix======");
                Log.Out("[MOD]worldSeed: " + worldSeed);

                IEnumerator plan = NewPlanMethod(worldSeed);
                __result = plan;

                return false;
            }

            public static IEnumerator NewPlanMethod(int worldSeed)
            {
                yield return WorldBuilder.Instance.SetMessage("Planning Highways");
                MicroStopwatch ms = new MicroStopwatch(true);
                ExitConnections.Clear();
                foreach (Township township in WorldBuilder.Instance.Townships)
                {
                    foreach (StreetTile gateway in township.Gateways)
                        gateway.SetAllExistingNeighborsForGateway();
                }
                List<Township> highwayTownships = WorldBuilder.Instance.Townships.FindAll(_township => WorldBuilder.townshipDatas[_township.GetTypeName()].SpawnGateway);
                Shuffle(worldSeed + 3943 + 1, ref highwayTownships);
                TownshipNode cur = new TownshipNode(highwayTownships[0]);
                yield return PrimsAlgo(cur, highwayTownships);
                for (; cur != null; cur = cur.next)
                {
                    if (cur.Path != null)
                        WorldBuilder.Instance.paths.Add(cur.Path);
                    if (cur.next == null)
                        break;
                }
                cur = new TownshipNode(cur.Township);
                yield return PrimsAlgo(cur, highwayTownships);
                for (; cur != null; cur = cur.next)
                {
                    if (cur.Path != null)
                        WorldBuilder.Instance.paths.Add(cur.Path);
                    if (cur.next == null)
                        break;
                }
                cur = new TownshipNode(cur.Township);
                yield return PrimsAlgo(cur, highwayTownships);
                for (; cur != null; cur = cur.next)
                {
                    if (cur.Path != null)
                        WorldBuilder.Instance.paths.Add(cur.Path);
                }
                yield return CleanupHighwayConnections(highwayTownships);
                yield return RunTownshipDirtRoads();
                Log.Out(string.Format("HighwayPlanner.Plan took {0}", (float)(ms.ElapsedMilliseconds * (1.0 / 1000.0))));
            }
        }

        private static readonly List<ExitConnection> ExitConnections = new List<ExitConnection>();
        private static Path GetPathToTownshipResult;

        private static IEnumerator CleanupHighwayConnections(List<Township> highwayTownships)
        {
            List<Vector2i> tilesToRemove = new List<Vector2i>();
            List<Path> pathsToRemove = new List<Path>();
            foreach (Township highwayTownship in highwayTownships)
            {
                if (highwayTownship.Gateways.Count == 0)
                {
                    Debug.LogError("Township has zero gateways! This should not happen!");
                }
                else
                {
                    foreach (StreetTile gateway in highwayTownship.Gateways)
                    {
                        for (int index = 0; index < 4; ++index)
                        {
                            StreetTile neighborByIndex = gateway.GetNeighborByIndex(index);
                            Vector2i highwayExitPosition = gateway.getHighwayExitPosition(index);
                            if (!gateway.UsedExitList.Contains(highwayExitPosition) && (neighborByIndex.Township != gateway.Township || !neighborByIndex.HasExitTo(gateway)))
                                gateway.SetExitUnUsed(highwayExitPosition);
                        }
                    }
                }
            }
            foreach (ExitConnection exitConnection in ExitConnections)
                exitConnection.SetExitUsedManually();
            foreach (Township highwayTownship in highwayTownships)
            {
                Township t = highwayTownship;
                yield return WorldBuilder.Instance.SetMessage("Planning Highways");
                if (WorldBuilder.townshipDatas[t.GetTypeName()].SpawnGateway && t.Gateways.Count != 0)
                {
                    foreach (StreetTile gateway in t.Gateways)
                    {
                        for (int index = 0; index < 4; ++index)
                        {
                            gateway.GetNeighborByIndex(index);
                            Vector2i highwayExitPosition = gateway.getHighwayExitPosition(index);
                            if (!gateway.UsedExitList.Contains(highwayExitPosition))
                                gateway.SetExitUnUsed(highwayExitPosition);
                        }
                        if (gateway.UsedExitList.Count < 2)
                        {
                            for (int index = 0; index < 4; ++index)
                            {
                                StreetTile neighborByIndex = gateway.GetNeighborByIndex(index);
                                gateway.SetExitUnUsed(gateway.getHighwayExitPosition(index));
                                if (neighborByIndex.Township == gateway.Township)
                                    neighborByIndex.SetExitUnUsed(neighborByIndex.getHighwayExitPosition(neighborByIndex.GetNeighborIndex(gateway)));
                            }
                            foreach (Path connectedHighway in gateway.ConnectedHighways)
                            {
                                StreetTile streetTileWorld;
                                if (WorldBuilder.Instance.GetStreetTileWorld(connectedHighway.StartPosition) == gateway)
                                {
                                    streetTileWorld = WorldBuilder.Instance.GetStreetTileWorld(connectedHighway.EndPosition);
                                    streetTileWorld.SetExitUnUsed(connectedHighway.EndPosition);
                                }
                                else
                                {
                                    streetTileWorld = WorldBuilder.Instance.GetStreetTileWorld(connectedHighway.StartPosition);
                                    streetTileWorld.SetExitUnUsed(connectedHighway.StartPosition);
                                }
                                if (streetTileWorld.UsedExitList.Count < 2)
                                    tilesToRemove.Add(streetTileWorld.GridPosition);
                                pathsToRemove.Add(connectedHighway);
                            }
                            tilesToRemove.Add(gateway.GridPosition);
                        }
                    }
                    foreach (Vector2i vector2i in tilesToRemove)
                    {
                        StreetTile streetTileGrid = WorldBuilder.Instance.GetStreetTileGrid(vector2i);
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
                    foreach (Path path in pathsToRemove)
                    {
                        path.Dispose();
                        WorldBuilder.Instance.paths.Remove(path);
                    }
                    pathsToRemove.Clear();
                }
            }
        }

        private static IEnumerator RunTownshipDirtRoads()
        {
            MicroStopwatch ms = new MicroStopwatch(true);
            foreach (Township town in WorldBuilder.Instance.Townships.FindAll(_township => !WorldBuilder.townshipDatas[_township.GetTypeName()].SpawnGateway))
            {
                yield return WorldBuilder.Instance.SetMessage("Planning Highways");
                foreach (StreetTile streetTile in town.Streets.Values)
                {
                    if (streetTile.GetNumTownshipNeighbors() == 1)
                    {
                        int num = -1;
                        for (int idx = 0; idx < 4; ++idx)
                        {
                            StreetTile neighborByIndex = streetTile.GetNeighborByIndex(idx);
                            if (neighborByIndex != null && neighborByIndex.Township == streetTile.Township)
                            {
                                num = idx;
                                break;
                            }
                        }
                        switch (num)
                        {
                            case 0:
                                streetTile.SetRoadExit(2, true);
                                goto label_18;
                            case 1:
                                streetTile.SetRoadExit(3, true);
                                goto label_18;
                            case 2:
                                streetTile.SetRoadExit(0, true);
                                goto label_18;
                            case 3:
                                streetTile.SetRoadExit(1, true);
                                goto label_18;
                            default:
                                goto label_18;
                        }
                    }
                }
                label_18:
                foreach (Vector2i exit in town.GetUnusedTownExits())
                {
                    Vector2i closestPoint = Vector2i.zero;
                    float closestDist = float.MaxValue;
                    foreach (Path path in WorldBuilder.Instance.paths)
                    {
                        if (!path.isCountryRoad)
                        {
                            foreach (Vector2 finalPathPoint in path.FinalPathPoints)
                            {
                                Vector2 pp = finalPathPoint;
                                float dist = Vector2i.DistanceSqr(exit, new Vector2i(pp));
                                if ((double)dist < (double)closestDist)
                                {
                                    if (WorldBuilder.Instance.IsMessageElapsed())
                                        yield return WorldBuilder.Instance.SetMessage("Planning Highways");
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
                    if (WorldBuilder.Instance.IsMessageElapsed())
                        yield return WorldBuilder.Instance.SetMessage("Planning Highways");
                    Path path1 = new Path(exit, closestPoint, 2, true, true, false, false);
                    if (path1.IsValid)
                    {
                        foreach (StreetTile streetTile in town.Streets.Values)
                        {
                            for (int index = 0; index < 4; ++index)
                            {
                                if ((double)Vector2i.Distance(streetTile.getHighwayExitPosition(index), exit) < 10.0)
                                    streetTile.SetExitUsed(exit);
                            }
                        }
                        WorldBuilder.Instance.paths.Add(path1);
                    }
                }
            }
            Log.Out(string.Format("HighwayPlanner.runTownshipDirtRoads took {0}", (float)(ms.ElapsedMilliseconds * (1.0 / 1000.0))));
        }

        private static void Shuffle<T>(int seed, ref List<T> list)
        {
            int count = list.Count;
            GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(seed);
            while (count > 1)
            {
                --count;
                int index = gameRandom.RandomRange(0, count) % count;
                (list[count], list[index]) = (list[index], list[count]);
            }
            GameRandomManager.Instance.FreeGameRandom(gameRandom);
        }

        private static IEnumerator PrimsAlgo(
          TownshipNode head,
          List<Township> highwayTownships)
        {
            List<Township> closedList = new List<Township>();
            TownshipNode current = head;
            List<Township> townshipList = new List<Township>();
            MicroStopwatch msReset = new MicroStopwatch(true);
            while (current != null)
            {
                int closestDist = int.MaxValue;
                Township closestTownship = null;
                List<Township> all = highwayTownships.FindAll(t => !current.Township.ConnectedTownships.Contains(t.ID) && !closedList.Contains(t) && t.ID != current.Township.ID && WorldBuilder.townshipDatas[t.GetTypeName()].SpawnGateway);
                all.Sort((_t1, _t2) => Vector2i.DistanceSqr(current.Township.GridCenter, _t1.GridCenter).CompareTo(Vector2i.DistanceSqr(current.Township.GridCenter, _t2.GridCenter)));
                msReset.ResetAndRestart();
                foreach (Township township in all)
                {
                    Township t = township;
                    yield return GetPathToTownship(current.Township, t, msReset);
                    Path path = GetPathToTownshipResult;
                    GetPathToTownshipResult = null;
                    if (msReset.ElapsedMilliseconds > 500L)
                    {
                        yield return WorldBuilder.Instance.SetMessage("Planning Highways");
                        msReset.ResetAndRestart();
                    }
                    if (path != null)
                    {
                        current.SetPath(path);
                        closestTownship = t;
                        break;
                    }
                    path = null;
                    t = null;
                }
                if (closestTownship == null)
                {
                    current = current.next;
                }
                else
                {
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
                    closestTownship = null;
                }
            }
        }

        private static void SetTileExits(Path path)
        {
            SetTileExit(path, path.StartPosition);
            SetTileExit(path, path.EndPosition);
        }

        private static void SetTileExit(Path currentPath, Vector2i exit)
        {
            StreetTile parent = WorldBuilder.Instance.GetStreetTileWorld(exit);
            if (parent != null)
            {
                if (parent.District != null && parent.District.name == "gateway")
                {
                    ExitConnections.Add(new ExitConnection(parent, exit, currentPath));
                    return;
                }
                foreach (StreetTile neighbor in parent.GetNeighbors())
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
            foreach (Township township2 in WorldBuilder.Instance.Townships)
            {
                if (township2.Area.Contains(exit.AsVector2()))
                {
                    township1 = township2;
                    break;
                }
            }
            if (township1 == null)
                return;
            foreach (StreetTile gateway in township1.Gateways)
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
            Path path = null;
            foreach (Vector2i unusedTownExit1 in thisTownship.GetUnusedTownExits())
            {
                foreach (Vector2i unusedTownExit2 in otherTownship.GetUnusedTownExits())
                {
                    Path path1 = new Path(unusedTownExit1, unusedTownExit2, 4, false, true, false, false);
                    int num = path1.Cost * path1.Cost;
                    if (path1.IsValid && num < closestDist)
                    {
                        closestDist = num;
                        path = path1;
                    }
                    path1.Dispose();
                }
                if (msReset.ElapsedMilliseconds > 500L)
                {
                    yield return null;
                    msReset.ResetAndRestart();
                }
            }
            GetPathToTownshipResult = path;
        }

        public enum CDirs
        {
            Invalid = -1, // 0xFFFFFFFF
            North = 0,
            East = 1,
            South = 2,
            West = 3,
        }

        public class ExitConnection
        {
            public static int NextID;
            public int ID;
            public StreetTile ParentTile;
            public Vector2i WorldPosition;
            public CDirs ExitEdge = CDirs.Invalid;
            public Path ConnectedPath;
            public StreetTile ConnectedTile;

            public bool IsPathConnection => ConnectedPath != null;

            public bool IsTileConnection => ConnectedTile != null;

            public ExitConnection(
              StreetTile parent,
              Vector2i worldPos,
              Path connectedPath = null,
              StreetTile connectedTile = null)
            {
                ID = NextID++;
                ParentTile = parent;
                WorldPosition = worldPos;
                ConnectedPath = connectedPath;
                ConnectedTile = connectedTile;
                for (int index = 0; index < 4; ++index)
                {
                    if (parent.getHighwayExitPosition(index) == WorldPosition)
                    {
                        ExitEdge = (CDirs)index;
                        break;
                    }
                }
                parent.SetExitUsed(WorldPosition);
            }

            public bool SetExitUsedManually()
            {
                return ParentTile.UsedExitList.Contains(ParentTile.getHighwayExitPosition((int)ExitEdge)) && ParentTile.ConnectedExits[(int)ExitEdge] && ParentTile.RoadExits[(int)ExitEdge] || ParentTile.SetExitUsed(WorldPosition);
            }
        }

        private class TownshipNode
        {
            public TownshipNode next;
            public Township Township;
            public Path Path;
            public float Distance;

            public TownshipNode(Township t) => Township = t;

            public void SetPath(Path p)
            {
                if (Path != null)
                {
                    Path.Dispose();
                    Path = null;
                }
                Path = p;
            }
        }
    }

}
