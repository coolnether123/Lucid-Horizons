using RimWorld.Planet;
using RimWorld;

public class BiomeWorker_Dreamworld : BiomeWorker
{
    public override float GetScore(Tile tile, int tileID)
    {
        if (tile.WaterCovered)
        {
            return -100f;
        }
        if (tile.temperature < -10f)
        {
            return 0f;
        }
        if (tile.rainfall < 300f)
        {
            return 0f;
        }
        return 5f;
    }
}
