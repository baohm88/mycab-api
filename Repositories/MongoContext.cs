// using Microsoft.Extensions.Configuration;
// using MongoDB.Driver;
// using MyCabs.Api.Models;

// namespace MyCabs.Api.Repositories
// {
//     public class MongoContext
//     {
//         private readonly IMongoDatabase _db;

//         public MongoContext(IConfiguration config)
//         {
//             var connectionString = config["MongoSettings:ConnectionString"];
//             var dbName = config["MongoSettings:DatabaseName"];

//             if (string.IsNullOrEmpty(connectionString))
//                 throw new InvalidOperationException("MongoDB connection string is not configured.");
//             if (string.IsNullOrEmpty(dbName))
//                 throw new InvalidOperationException("MongoDB database name is not configured.");

//             var client = new MongoClient(connectionString);
//             _db = client.GetDatabase(dbName);

//             // Index: unique email
//             var users = _db.GetCollection<User>("users");
//             var emailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
//             var options = new CreateIndexOptions { Unique = true, Name = "uniq_email" };
//             users.Indexes.CreateOne(new CreateIndexModel<User>(emailIndex, options));
//         }

//         public IMongoCollection<User> Users => _db.GetCollection<User>("users");
//     }
// }


using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MyCabs.Api.Models;

namespace MyCabs.Api.Repositories
{
    public class MongoContext
    {
        private readonly IMongoDatabase _db;

        public MongoContext(IConfiguration config)
        {
            var connectionString = config["MongoSettings:ConnectionString"];
            var dbName = config["MongoSettings:DatabaseName"];

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("MongoDB connection string is not configured.");
            if (string.IsNullOrEmpty(dbName))
                throw new InvalidOperationException("MongoDB database name is not configured.");

            var client = new MongoClient(connectionString);
            _db = client.GetDatabase(dbName);

            // Tạo unique index cho Email
            var users = _db.GetCollection<User>("users");
            var emailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var options = new CreateIndexOptions
            {
                Unique = true,
                Name = "uniq_email"
            };
            users.Indexes.CreateOne(new CreateIndexModel<User>(emailIndex, options));
        }

        public IMongoCollection<User> Users => _db.GetCollection<User>("users");
    }
}
