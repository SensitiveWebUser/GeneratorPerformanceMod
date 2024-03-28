using WorldGenerationEngineFinal;

namespace MyTestMod.Harmony.HighwayPlanner
{
    internal class TownshipNode
    {
        public TownshipNode next;
        public Township Township;
        public Path Path;
        public float Distance;

        public TownshipNode(Township t) => Township = t;

        public void SetPath(Path p)
        {
            Path?.Dispose();
            Path = p;
        }
    }
}
