using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Models;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
}
