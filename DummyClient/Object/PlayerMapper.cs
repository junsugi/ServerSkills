using Google.Protobuf.Protocol;

namespace DummyClient.Object;

public static class PlayerMapper
{
    public static Player ToDomain(PlayerInfo playerInfo)
    {
        Player player = new Player()
        {
            Id = playerInfo.Id,
            DisplayName = playerInfo.DisplayName,
            Hp = playerInfo.Hp,
            Atk = playerInfo.Atk,
            Def = playerInfo.Def,
        };  
        
        return player;
    }
}