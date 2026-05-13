using Bogus;
using ServerSkills.Login;

namespace ServerSkills;

public class PlayerFactory
{
    private static Faker _faker = new Faker();
    
    public static Player Create(AccountDto dto)
    {
        Player player = new Player()
        {
            ObjectId = ObjectManager.Instance.GenerateId(GameObjectType.PLAYER, dto.Id),
            NickName = dto.NickName,
            Hp = 100,
            Atk = 5,
            Def = 5,
            Inventory =  new Inventory(),
            Position = new Position()
        };

        return player;
    }
}