using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Infrastructure.Auth
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public double ExpiryMinutes { get; set; }
    }
}
