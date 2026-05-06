using Google.Protobuf.Protocol;

namespace ServerSkills;

public static class PlayerMapper
{
    public static PlayerInfo ToDto(Player player)
    {
        PlayerInfo playerInfo = new PlayerInfo()
        {
            Id = player.Id,
            DisplayName = player.DisplayName,
            Hp = 100,
            Atk = 5,
            Def = 5
        };
        
        return playerInfo;
    }
}