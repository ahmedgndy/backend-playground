# 🔐 OTP Authentication Demo

A complete, educational ASP.NET Core Web API project demonstrating secure Email OTP (One-Time Password) authentication flow.

## ✨ Features

### Security Features

- ✅ **Cryptographically Secure OTP Generation** - Uses `RandomNumberGenerator` (not `Random`)
- ✅ **Salted Hashing** - OTPs are hashed with SHA256 + unique salt before storage
- ✅ **Constant-Time Comparison** - Prevents timing attacks during verification
- ✅ **Single-Use OTPs** - Each OTP can only be used once
- ✅ **Expiration** - OTPs expire after 10 minutes
- ✅ **Max Attempts** - OTPs are invalidated after 3 failed attempts
- ✅ **Rate Limiting** - Per-IP and per-email rate limiting

### Backend Features

- ASP.NET Core Web API (Controller-based)
- Entity Framework Core with SQLite
- Optional Redis store for ephemeral OTPs
- Pluggable email services (SendGrid, SMTP, Console)
- Comprehensive logging (OTP values are NEVER logged)
- Swagger/OpenAPI documentation

### Frontend Features

- Modern, responsive UI with beautiful animations
- Step-by-step OTP flow
- Auto-focus and paste support for OTP inputs
- Timer countdown and expiration handling
- Toast notifications
- No external frameworks (pure HTML/CSS/JS)

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Run the Application

```bash
# Navigate to project directory
cd OTP

# Restore packages
dotnet restore

# Run the application
dotnet run
```

The application will start at:

- **Frontend**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

### Development Mode

By default, OTPs are logged to the console (not sent via email). Look for the boxed OTP in the terminal output:

```
╔══════════════════════════════════════════════════════════╗
║           DEVELOPMENT MODE - OTP NOTIFICATION            ║
╠══════════════════════════════════════════════════════════╣
║  Email: user@example.com                                 ║
║  OTP:   123456                                           ║
╚══════════════════════════════════════════════════════════╝
```

## 📁 Project Structure

```
OTP/
├── Controllers/
│   └── AuthController.cs          # API endpoints
├── Data/
│   └── OtpDbContext.cs            # EF Core database context
├── Models/
│   ├── OtpRecord.cs               # Database entity
│   └── DTOs/
│       ├── OtpRequestDto.cs       # Request OTP input
│       ├── OtpVerifyDto.cs        # Verify OTP input
│       └── ApiResponse.cs         # Standard API response
├── Services/
│   ├── Interfaces/
│   │   ├── IOtpService.cs         # OTP operations contract
│   │   ├── IOtpStore.cs           # Storage abstraction
│   │   └── IEmailService.cs       # Email sending contract
│   └── Implementations/
│       ├── OtpService.cs          # Core OTP logic
│       ├── DatabaseOtpStore.cs    # SQLite storage
│       ├── RedisOtpStore.cs       # Redis storage (optional)
│       ├── SendGridEmailService.cs # SendGrid implementation
│       ├── SmtpEmailService.cs    # SMTP implementation
│       └── ConsoleEmailService.cs # Development logging
├── wwwroot/
│   ├── index.html                 # Frontend UI
│   ├── css/styles.css             # Styling
│   └── js/app.js                  # Frontend logic
├── Program.cs                     # Application configuration
├── appsettings.json              # Production settings
└── appsettings.Development.json  # Development settings
```

## 🔌 API Endpoints

### POST /api/auth/request-otp

Request an OTP to be sent to an email address.

**Request:**

```json
{
  "email": "user@example.com"
}
```

**Response:**

```json
{
  "success": true,
  "message": "If this email is registered, you will receive a verification code shortly."
}
```

### POST /api/auth/verify-otp

Verify an OTP code.

**Request:**

```json
{
  "email": "user@example.com",
  "otp": "123456"
}
```

**Success Response:**

```json
{
  "success": true,
  "message": "Email verified successfully!",
  "data": {
    "verified": true,
    "message": "You would typically receive a JWT token here"
  }
}
```

**Error Response:**

```json
{
  "success": false,
  "message": "Invalid OTP. 2 attempt(s) remaining."
}
```

## ⚙️ Configuration

### Email Providers

Configure the email provider in `appsettings.json`:

#### Console (Development)

```json
{
  "Email": {
    "Provider": "Console"
  }
}
```

#### SendGrid

```json
{
  "Email": {
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "SG.your-api-key-here",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "Your App"
    }
  }
}
```

#### SMTP (Gmail example)

```json
{
  "Email": {
    "Provider": "Smtp",
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "FromEmail": "your-email@gmail.com",
      "FromName": "Your App",
      "UseSsl": true
    }
  }
}
```

> ⚠️ **Gmail Note**: You need to create an [App Password](https://myaccount.google.com/apppasswords) (requires 2FA enabled).

### Redis Store (Optional)

To use Redis instead of SQLite for OTP storage, uncomment the Redis configuration in `Program.cs`:

```csharp
// Uncomment these lines in Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
builder.Services.AddScoped<IOtpStore, RedisOtpStore>();
```

## 🔒 Security Best Practices Demonstrated

1. **Never Store Plaintext OTPs**

   - OTPs are hashed with SHA256 + salt before storage
   - Even if database is compromised, OTPs cannot be retrieved

2. **Cryptographically Secure Random Numbers**

   - Uses `RandomNumberGenerator.Fill()` not `Random`
   - Suitable for security-sensitive applications

3. **Constant-Time Comparison**

   - Uses `CryptographicOperations.FixedTimeEquals()`
   - Prevents timing attacks

4. **Rate Limiting**

   - Limits OTP requests per IP and email
   - Prevents brute-force and enumeration attacks

5. **Generic Error Messages**

   - Same response whether email exists or not
   - Prevents user enumeration

6. **OTP Constraints**

   - Single-use (marked as used after verification)
   - Short expiration (10 minutes)
   - Limited attempts (3 max)

7. **Secure Logging**
   - Never log actual OTP values
   - Email addresses are masked in logs

## 📚 Learning Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [OWASP OTP Guidelines](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html)
- [RandomNumberGenerator Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.randomnumbergenerator)

## 📝 License

This project is for educational purposes. Feel free to use and modify as needed.
