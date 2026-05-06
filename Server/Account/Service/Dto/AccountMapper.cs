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
            DbId = account.DbId,
            Id = account.Id,
            DisplayName = account.DisplayName,
        };

        return dto;
    }

    public static Account ToEntity(AccountDto dto)
    {
        Account account = new Account
        {
            DbId = dto.DbId,
            Id = dto.Id
        };

        return account;
    }
}