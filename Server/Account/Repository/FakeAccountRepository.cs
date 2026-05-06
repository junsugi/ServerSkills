using Bogus;

namespace ServerSkills.Login;

public class FakeAccountRepository : IAccountRepository
{
    private Faker _faker = new Faker();
    
    public Account? TryGetById(string id)
    {
        Account account = new Account()
        {
            DbId = _faker.Random.Int(),
            Id = _faker.Internet.Email(),
            DisplayName = _faker.Name.FullName(),
        };

        return account;
    }
}