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
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            Hp = 100,
            Atk = 5,
            Def = 5
        };

        return player;
    }
}