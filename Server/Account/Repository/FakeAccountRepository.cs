using Bogus;

namespace ServerSkills.Login;

public class FakeAccountRepository : IAccountRepository
{
    private int _nextDbId = 0;
    private Faker _faker = new();
    
    public Account? TryGetById(string id)
    {
        Account account = new Account()
        {
            Id = Interlocked.Increment(ref _nextDbId),
            Email = _faker.Internet.Email(),
            NickName = _faker.Internet.UserName()
        };

        return account;
    }
}