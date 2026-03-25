using MongoDB.Driver;
using UserService.Models;

namespace UserService.Services;

public class UserProfileService
{
    private readonly IMongoCollection<UserProfile> _users;

    public UserProfileService(IConfiguration config)
    {
        var connStr  = config["MONGODB_CONNECTION"] ?? "mongodb://localhost:27017";
        var client   = new MongoClient(connStr);
        var database = client.GetDatabase("users_db");
        _users       = database.GetCollection<UserProfile>("user_profiles");

        _users.Indexes.CreateOne(new CreateIndexModel<UserProfile>(
            Builders<UserProfile>.IndexKeys.Ascending(u => u.UserId),
            new CreateIndexOptions { Unique = true }));
    }

    public async Task<List<UserProfile>> GetAllAsync() =>
        await _users.Find(_ => true).ToListAsync();

    public async Task<UserProfile?> GetByIdAsync(string id) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<UserProfile?> GetByUserIdAsync(string userId) =>
        await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();

    public async Task<UserProfile> CreateAsync(UserProfile profile)
    {
        await _users.InsertOneAsync(profile);
        return profile;
    }

    public async Task<bool> UpdateAsync(string id, UpdateDefinition<UserProfile> update)
    {
        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }
}
