using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

namespace DisBot
{
    public interface IMongoDBWorker
    {
        public void Connect(string connectStr);

        public void AddData<T>(T item, string database, string collection) where T : new();
        public void AddData<T>(IEnumerable<T> items, string database, string collection) where T : new();

        public Task DropDBAsync(string databaseName);
        public Task DropCollectionAsync(string databaseName, string collectionName);

        public Task AddDataAsync<T>(T item, string database, string collection) where T : new();
        public Task AddDataAsync<T>(List<T> items, string databaseName, string collectionName) where T : new();
        public Task AddDataAsync<T>(IEnumerable<T> items, string databaseName, string collectionName) where T : new();

        public Task UpdateDataAsync<T>(T item, string database, string collection, string propertyname) where T : new();

        public Task<IEnumerable<T>> ReadDataAsync<T>(string databaseName, string collectionName) where T : new();
        public Task<IEnumerable<T>> ReadDataAsync<T>(Func<T, bool> predicate, string databaseName, string collectionName) where T : new();
    }

    public class MongoDBWorker : IMongoDBWorker
    {
        MongoClient _client;

        public MongoDBWorker()
        {

        }

        public MongoDBWorker(string connectString)
        {
            _client = new MongoClient(connectString);
        }


        public void Connect(string connectString)
        {
            _client = new MongoClient(connectString);
        }

        public async Task UpdateDataAsync<T>(T item, string databaseName, string collectionName, string propertyName) where T : new()
        {
            var property = typeof(T).GetProperty(propertyName);
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq(propertyName, property.GetValue(item));
            await collection.DeleteOneAsync(filter);
            await collection.InsertOneAsync(item);
        }

        public async Task<IEnumerable<T>> ReadDataAsync<T>(Func<T, bool> predicate, string databaseName, string collectionName) where T : new()
        {
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<T>(collectionName);
            using (var documents = await collection.FindAsync(new BsonDocument()))
            {
                while (await documents.MoveNextAsync())
                {
                    return documents.Current.Where(predicate);
                }
            }
            return null;
        }

        public async Task<IEnumerable<T>> ReadDataAsync<T>(string databaseName, string collectionName) where T : new()
        {
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<T>(collectionName);
            using (var documents = await collection.FindAsync(new BsonDocument()))
            {
                while (await documents.MoveNextAsync())
                {
                    return documents.Current;
                }
            }
            return null;
        }

        public async Task AddDataAsync<T>(T item, string databaseName, string collectionName) where T : new()
        {
            var saveDocDef = item.ToBsonDocument<T>();
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            await collection.InsertOneAsync(saveDocDef);
        }

        public async Task AddDataAsync<T>(List<T> items, string databaseName, string collectionName) where T : new()
        {
            List<BsonDocument> documents = new List<BsonDocument>();
            foreach(var item in items)
            {
                documents.Add(item.ToBsonDocument<T>());
            }
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            await collection.InsertManyAsync(documents);
        }

        public async Task AddDataAsync<T>(IEnumerable<T> items, string databaseName, string collectionName) where T : new()
        {
            List<BsonDocument> documents = new List<BsonDocument>();
            foreach (var item in items)
            {
                documents.Add(item.ToBsonDocument<T>());
            }
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            await collection.InsertManyAsync(documents);
        }

        public void AddData<T>(T item, string databaseName, string collectionName) where T : new()
        {
            var saveDocDef = item.ToBsonDocument<T>();
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            collection.InsertOne(saveDocDef);
        }

        public void AddData<T>(IEnumerable<T> items, string databaseName, string collectionName) where T : new()
        {
            List<BsonDocument> documents = new List<BsonDocument>();
            foreach (var item in items)
            {
                documents.Add(item.ToBsonDocument<T>());
            }
            var database = _client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);
            collection.InsertMany(documents);
        }

        public async Task DropDBAsync(string databaseName)
        {
            await _client.DropDatabaseAsync(databaseName);
        }

        public async Task DropCollectionAsync(string databaseName, string collectionName)
        {
            var db = _client.GetDatabase(databaseName);
            await db.DropCollectionAsync(collectionName);
        }

        BsonDocument ToBson<T>(T req)
        {
            BsonDocument bson = new BsonDocument(bsonElements(req));
            return bson;
        }

        IEnumerable<BsonElement> bsonElements<T>(T req)
        {
            var _properties = typeof(T).GetProperties();
            BsonElement[] resp = new BsonElement[_properties.Length];
            for (int i = 0; i < _properties.Length; i++)
            {
                resp[i] = new BsonElement(_properties[i].Name, BsonValue.Create(_properties[i].GetValue(req)));
            }
            return resp;
        }
    }

    [BsonIgnoreExtraElements]
    public class DateTimeClass
    {
        public DateTime dateTime { get; set; }
    }
}
