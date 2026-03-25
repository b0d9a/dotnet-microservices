# Microservices Architecture on .NET 8

A microservices-based system for user management and authentication, built with C# / .NET 8, MongoDB, and Docker. Includes Win32 P/Invoke interop and COM Interop demonstrations via a dedicated math library.

## System Architecture

```
Client
  └── API Gateway (port 5000)
        ├── Auth Service   (port 5001)  →  MongoDB (auth_db)
        ├── User Service   (port 5002)  →  MongoDB (users_db)
        ├── Win32 Interop  (/system/*)       kernel32.dll
        └── COM Interop    (/system/com-calc) MathComLib
```

## Components

### 1. API Gateway (port 5000)
- Reverse-proxies `/auth/*` to Auth Service and `/users/*` to User Service
- Validates JWT on all protected routes before forwarding
- Forwards `Authorization` header to downstream services
- Exposes Win32 and COM Interop endpoints directly

### 2. Auth Service (port 5001)
- User registration with BCrypt password hashing
- Username and email uniqueness validation
- JWT token issuance (HMAC-SHA256, 24-hour expiry)
- MongoDB database: `auth_db`, collection: `auth_users`

### 3. User Service (port 5002)
- Full CRUD for user profiles
- Independent JWT validation (defense-in-depth)
- Partial update support (fullName, email, role)
- MongoDB database: `users_db`, collection: `user_profiles`

### 4. Win32 Interop (P/Invoke)
- `GET /system/info` — memory info via `GlobalMemoryStatusEx` (kernel32.dll)
- `GET /system/cpu`  — processor info via `GetSystemInfo` (kernel32.dll)

### 5. COM Interop (MathComLib)
- `GET /system/com-calc` — math operations via `MathOperations` COM object (RCW)
- Supports: add, sub, mul, div, pow, sqrt, sin, cos, log, log10

## Technology Stack

| Component | Technology |
|---|---|
| Language | C# 12, .NET 8 |
| Web framework | ASP.NET Core Web API |
| Database | MongoDB 7.0 |
| Authentication | JWT Bearer (HMAC-SHA256) |
| Password hashing | BCrypt.Net-Next |
| OS Interop | P/Invoke — `kernel32.dll` |
| COM Interop | MathComLib (RCW via `[ComVisible]`) |
| Containerization | Docker, Docker Compose |
| Networking | Docker bridge network |

## Getting Started

### Prerequisites
- Docker Desktop
- Docker Compose

### Run

```bash
docker-compose up --build
```

Services will be available at:

| Service | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| Auth Service | http://localhost:5001 |
| User Service | http://localhost:5002 |
| MongoDB | localhost:27017 |

### Stop

```bash
docker-compose down
```

To also remove MongoDB data volume:

```bash
docker-compose down -v
```

## API Reference

### Authentication (public)

**Register**
```http
POST http://localhost:5000/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "mypassword123"
}
```

**Login**
```http
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "username": "john_doe",
  "password": "mypassword123"
}
```

Response:
```json
{
  "token": "<JWT>",
  "username": "john_doe",
  "email": "john@example.com"
}
```

### User Management (JWT required)

All user endpoints require the header:
```
Authorization: Bearer <token>
```

| Method | Endpoint | Description |
|---|---|---|
| GET | `/users` | Get all users |
| GET | `/users/{id}` | Get user by ID |
| POST | `/users` | Create user profile |
| PUT | `/users/{id}` | Update user profile |
| DELETE | `/users/{id}` | Delete user |

**Create user profile**
```http
POST http://localhost:5000/users
Authorization: Bearer <token>
Content-Type: application/json

{
  "userId": "abc123",
  "username": "john_doe",
  "email": "john@example.com",
  "fullName": "John Doe",
  "role": "user"
}
```

**Update user profile (partial)**
```http
PUT http://localhost:5000/users/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "fullName": "John Smith",
  "email": "john.smith@example.com",
  "role": "admin"
}
```

### Win32 Interop (JWT required)

**Memory information**
```http
GET http://localhost:5000/system/info
Authorization: Bearer <token>
```

Response:
```json
{
  "source": "Win32 API — kernel32.dll GlobalMemoryStatusEx (PInvoke)",
  "memoryLoadPercent": 42,
  "totalPhysicalMB": 16384,
  "availablePhysicalMB": 9437,
  "usedPhysicalMB": 6947,
  "totalPageFileMB": 18944,
  "availablePageFileMB": 11200,
  "totalVirtualMB": 134217727,
  "availableVirtualMB": 134200910,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

**CPU / processor information**
```http
GET http://localhost:5000/system/cpu
Authorization: Bearer <token>
```

Response:
```json
{
  "source": "Win32 API — kernel32.dll GetSystemInfo (PInvoke)",
  "architecture": "x64 (AMD or Intel)",
  "numberOfProcessors": 8,
  "pageSizeBytes": 4096,
  "processorLevel": 6,
  "processorRevision": 45825,
  "allocationGranularity": 65536,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### COM Interop — MathComLib (JWT required)

**All operations**
```http
GET http://localhost:5000/system/com-calc?a=10&b=3
Authorization: Bearer <token>
```

**Specific operation** (`op`: add / sub / mul / div / pow / sqrt / sin / cos / log / log10)
```http
GET http://localhost:5000/system/com-calc?a=10&b=3&op=add
Authorization: Bearer <token>
```

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `JWT_SECRET` | Secret key for signing JWT tokens | required |
| `JWT_ISSUER` | JWT issuer claim | `kursovaya` |
| `JWT_AUDIENCE` | JWT audience claim | `kursovaya` |
| `MONGODB_CONNECTION` | MongoDB connection URI | `mongodb://mongodb:27017` |
| `AUTH_SERVICE_URL` | Auth Service base URL | `http://auth-service:80` |
| `USER_SERVICE_URL` | User Service base URL | `http://user-service:80` |

## Project Structure

```
kursovaya/
├── ApiGateway/
│   ├── Controllers/
│   │   ├── ProxyController.cs     # Reverse proxy routes
│   │   └── SystemController.cs    # Win32 & COM Interop endpoints
│   ├── Interop/
│   │   └── SystemInfo.cs          # P/Invoke declarations (kernel32.dll)
│   ├── Program.cs                 # JWT validation, HttpClient setup
│   └── Dockerfile
├── AuthService/
│   ├── Controllers/
│   │   └── AuthController.cs      # /auth/register, /auth/login
│   ├── Models/
│   │   └── AuthUser.cs            # BSON model + request/response DTOs
│   ├── Services/
│   │   ├── AuthUserService.cs     # MongoDB access (auth_db)
│   │   └── JwtService.cs          # JWT generation
│   ├── Program.cs
│   └── Dockerfile
├── UserService/
│   ├── Controllers/
│   │   └── UsersController.cs     # CRUD endpoints
│   ├── Models/
│   │   └── UserProfile.cs         # BSON model + request DTOs
│   ├── Services/
│   │   └── UserProfileService.cs  # MongoDB access (users_db)
│   ├── Program.cs
│   └── Dockerfile
├── MathComLib/
│   └── MathOperations.cs          # COM-visible math library (RCW)
└── docker-compose.yml
```

## View Logs

```bash
docker logs api-gateway
docker logs auth-service
docker logs user-service
docker logs mongodb
```

## Security Notes

- JWT is validated independently at both the API Gateway and the User Service (defense-in-depth)
- Passwords are never stored in plain text — BCrypt hashing with salt is used
- Unique indexes on `username` and `email` prevent duplicate registrations
- All user endpoints are protected; auth endpoints are explicitly `[AllowAnonymous]`
