namespace ServerSkills.Login;

public class AccountDto
{
    public LoginResult ResultCode { get; set; }
    public int Id { get; set; }
    public string Email  { get; set; }
    public string NickName { get; set; }
}