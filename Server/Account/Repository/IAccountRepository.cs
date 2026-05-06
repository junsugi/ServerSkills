namespace ServerSkills.Login;

public interface IAccountRepository
{
    public Account? TryGetById(string id);
}