using ServerSkills.Login;

namespace ServerSkills;

public class AccountService(IAccountRepository accountRepository) : IAccountService
{
    public LoginResult Login(string id, out AccountDto? accountDto)
    {
        Account? account = accountRepository.TryGetById(id);
        accountDto = AccountMapper.ToDto(account);
        
        if (accountDto == null)
            return LoginResult.NotFound;
        
        return LoginResult.Success;
    }
}