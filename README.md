# Azure Active Directory Integration

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET%20Core-5.0-purple)](https://dotnet.microsoft.com/)

A comprehensive .NET web API solution for integrating with Microsoft Azure Active Directory (Azure AD) / Microsoft Entra ID, providing user and group management capabilities using Microsoft Graph API.

## 🚀 Features

- ✨ **User Management**
  - Retrieve all users with filtering, searching, and pagination
  - Get user details by ID
  - Search users across the directory
  - Get user group memberships
  - Authentication with ROPC flow (development/testing)

- 🔐 **Group Management**
  - List all groups with filtering capabilities
  - Get group details and members
  - Check group membership status
  - Retrieve group owners and sub-groups
  - Get parent groups and group hierarchy

- 🔧 **Authentication Methods**
  - Client Credentials Flow (recommended for production)
  - Resource Owner Password Credentials (ROPC) flow
  - Device Code Flow support
  - Authorization Code Flow with PKCE

- 📊 **Additional Features**
  - Comprehensive logging with Serilog
  - Health checks for Microsoft Graph connectivity
  - Swagger/OpenAPI documentation
  - CORS support
  - Dependency injection with scoped services
  - Background token renewal service
## 🛠️ Prerequisites

- [.NET 5.0 SDK](https://dotnet.microsoft.com/download)
- Azure AD tenant with appropriate permissions
- Azure AD application registration

## ⚙️ Setup

### 1. Azure AD Configuration

Create an Azure AD application in the [Azure Portal](https://portal.azure.com):

1. Navigate to **Azure Active Directory** > **App registrations**
2. Click **New registration**
3. Configure the application:
   - Name: Your application name
   - Supported account types: Choose appropriate option
   - Redirect URI: `http://localhost:5000/auth/callback` (for development)

### 2. API Permissions

Add the following Microsoft Graph permissions:

#### Application Permissions (for client credentials flow):
- `User.Read.All` - Read all users
- `Group.Read.All` - Read all groups
- `Directory.Read.All` - Read directory data

#### Delegated Permissions (for user authentication):
- `User.Read` - Read user profile
- `User.ReadBasic.All` - Read basic profiles of all users
- `Group.Read.All` - Read groups

### 3. Authentication Configuration

#### For Production (Client Credentials):
- Generate a client secret in **Certificates & secrets**
- Note down: Tenant ID, Client ID, Client Secret

#### For Development (ROPC):
- Enable **Allow public client flows** in Authentication settings
- Configure username/password authentication

### 4. Application Configuration

Update `appsettings.json`:

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "http://localhost:5000/auth/callback",
    "Instance": "https://login.microsoftonline.com/"
  }
}
```

## 🚁 Quick Start

### Clone and Run

```bash
# Clone the repository
git clone https://github.com/FathVarli/AzureActiveDirectory.git
cd AzureActiveDirectory

# Restore dependencies
dotnet restore

# Update user secrets (recommended for development)
dotnet user-secrets init
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"

# Run the application
dotnet run
```

## 📖 API Endpoints

### User Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | Get all users |
| GET | `/api/users/{id}` | Get user by ID |
| GET | `/api/users/search?q={term}` | Search users |
| GET | `/api/users/{id}/groups` | Get user's groups |
| GET | `/api/users/with-groups` | Get users with their groups |
| POST | `/api/users/by-ids` | Get multiple users by IDs |

### Group Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/groups` | Get all groups |
| GET | `/api/groups/{id}` | Get group by ID |
| GET | `/api/groups/search?q={term}` | Search groups |
| GET | `/api/groups/{id}/members` | Get group members |
| GET | `/api/groups/{id}/owners` | Get group owners |
| GET | `/api/groups/{id}/member-count` | Get member count |
| GET | `/api/groups/{id}/is-member/{userId}` | Check group membership |
| POST | `/api/groups/members` | Get multiple groups' members |

### Authentication (ROPC - Development Only)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate user |
| POST | `/api/auth/login-with-info` | Authenticate and get user info |
| POST | `/api/auth/validate` | Validate credentials |
| GET | `/api/auth/security-info` | Get security information |

## 🔧 Usage Examples

### Get All Users with Filtering

```bash
# Get first 10 users
curl -X GET "https://localhost:5001/api/users?top=10"

# Search users
curl -X GET "https://localhost:5001/api/users?search=john"

# Filter users
curl -X GET "https://localhost:5001/api/users?filter=startswith(displayName,'A')"
```

### Authenticate User (ROPC)

```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "user@tenant.onmicrosoft.com",
    "password": "userpassword"
  }'
```

### Check Group Membership

```bash
curl -X GET "https://localhost:5001/api/groups/{groupId}/is-member/{userId}"
```

## 🔒 Security Considerations

### ROPC Security Warning

⚠️ **Warning**: Resource Owner Password Credentials (ROPC) flow is included for development/testing purposes only. **DO NOT USE IN PRODUCTION**:

- User passwords are sent to the application
- No MFA support
- Doesn't support modern auth features
- Consider security risks carefully

## 📝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow C# coding conventions
- Add XML documentation for public APIs
- Include unit tests for new features
- Ensure all tests pass before submitting PR

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💬 Support

- 🐛 **Issues**: [GitHub Issues](https://github.com/FathVarli/AzureActiveDirectory/issues)

## 🤝 Acknowledgments

- [Microsoft Graph SDK for .NET](https://github.com/microsoftgraph/msgraph-sdk-dotnet)
- [Azure Identity library](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/identity)
- [Serilog](https://serilog.net/)

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/FathVarli">FathVarli</a>
</p>
