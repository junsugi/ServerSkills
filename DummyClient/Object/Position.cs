namespace DummyClient.Object;

public class Position
{
    public float X { get; set; }
    public float Y { get; set; }

    public Position(float x, float y)
    {
        X = x;
        Y = y;   
    }    
    
    public Google.Protobuf.Protocol.Position ToDto()
    {
        Google.Protobuf.Protocol.Position positionInfo = new Google.Protobuf.Protocol.Position();
        positionInfo.X = X;
        positionInfo.Y = Y;
        return positionInfo;
    }
}