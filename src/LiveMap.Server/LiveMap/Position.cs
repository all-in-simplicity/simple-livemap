namespace LiveMap.Server.LiveMap;

public struct Position
{
    public float X { get; set; }

    public float Y { get; set; }

    public Position()
    {
    }

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}