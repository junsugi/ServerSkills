namespace ServerSkills.Login;

public class AccountDto
{
    public LoginResult ResultCode { get; set; }
    public int DbId { get; set; }
    public string Id  { get; set; }
    public string DisplayName { get; set; }
}