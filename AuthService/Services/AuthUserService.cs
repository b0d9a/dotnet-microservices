using AuthService.Models;
using MongoDB.Driver;

namespace AuthService.Services;

public class AuthUserService
{
    private readonly IMongoCollection<AuthUser> _users;

    public AuthUserService(IConfiguration config)
    {
        var connStr  = config["MONGODB_CONNECTION"] ?? "mongodb://localhost:27017";
        var client   = new MongoClient(connStr);
        var database = client.GetDatabase("auth_db");
        _users       = database.GetCollection<AuthUser>("auth_users");

        // Unique index on username and email
        var indexOptions = new CreateIndexOptions { Unique = true };
        _users.Indexes.CreateOne(new CreateIndexModel<AuthUser>(
            Builders<AuthUser>.IndexKeys.Ascending(u => u.Username), indexOptions));
        _users.Indexes.CreateOne(new CreateIndexModel<AuthUser>(
            Builders<AuthUser>.IndexKeys.Ascending(u => u.Email), indexOptions));
    }

    public async Task<AuthUser?> GetByUsernameAsync(string username) =>
        await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _users.Find(u => u.Username == username).AnyAsync();

    public async Task<bool> EmailExistsAsync(string email) =>
        await _users.Find(u => u.Email == email).AnyAsync();

    public async Task CreateAsync(AuthUser user) =>
        await _users.InsertOneAsync(user);
}
