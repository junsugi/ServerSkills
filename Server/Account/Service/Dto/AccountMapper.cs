using ServerSkills.Login;

namespace ServerSkills;

public static class AccountMapper
{
    public static AccountDto? ToDto(Account? account)
    {
        if (account == null)
            return null;
        
        AccountDto dto = new AccountDto()
        {
            Id = account.Id,
            Email = account.Email,
            NickName = account.NickName,
        };

        return dto;
    }

    public static Account ToEntity(AccountDto dto)
    {
        Account account = new Account
        {
            Id = dto.Id,
            Email = dto.Email
        };

        return account;
    }
}