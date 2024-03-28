using WorldGenerationEngineFinal;

namespace MyTestMod.Harmony.HighwayPlanner
{
    internal class ExitConnection
    {
        public enum CDirs
        {
            Invalid = -1, // 0xFFFFFFFF
            North = 0,
            East = 1,
            South = 2,
            West = 3,
        }

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

            for (var directionIndex = 0; directionIndex < 4; ++directionIndex)
            {
                if (parent.getHighwayExitPosition(directionIndex) == WorldPosition)
                {
                    ExitEdge = (CDirs)directionIndex;
                    break;
                }
            }

            parent.SetExitUsed(WorldPosition);
        }

        public void SetExitUsedManually()
        {
            var isExitUsed = ParentTile.UsedExitList.Contains(ParentTile.getHighwayExitPosition((int)ExitEdge));
            var isConnectedExit = ParentTile.ConnectedExits[(int)ExitEdge];
            var isRoadExit = ParentTile.RoadExits[(int)ExitEdge];

            if (isExitUsed && isConnectedExit && isRoadExit)
            {
                return;
            }

            ParentTile.SetExitUsed(WorldPosition);
        }
    }
}
