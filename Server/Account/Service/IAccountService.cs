namespace ServerSkills.Login;

public interface IAccountService
{
    public LoginResult Login(string id, out AccountDto? accountDto);
}