using Google.Protobuf.Protocol;

namespace ServerSkills;

public static class PlayerMapper
{
    public static PlayerInfo ToDto(Player player)
    {
        ObjectInfo objectInfo = new ObjectInfo()
        {
            ObjectId = player.ObjectId,
            NickName = player.NickName,
        };
        PlayerInfo playerInfo = new PlayerInfo()
        {
            ObjectInfo = objectInfo,
            Hp = 100,
            Atk = 5,
            Def = 5
        };
        
        return playerInfo;
    }
}