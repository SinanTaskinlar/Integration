using Integration.Backend;
using Integration.Common;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Integration.Service
{
    public sealed class ItemIntegrationService
    {
        private readonly ItemOperationBackend _itemIntegrationBackend;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly RedLockFactory _redLockFactory;

        //public ItemIntegrationService()
        //{
        //}

        public ItemIntegrationService(ItemOperationBackend itemIntegrationBackend, string redisConnectionString)
        {
            _itemIntegrationBackend = itemIntegrationBackend ?? throw new ArgumentNullException(nameof(itemIntegrationBackend));
            _redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
            _redLockFactory = RedLockFactory.Create(new List<RedLockNet.SERedis.Configuration.RedLockMultiplexer> { new RedLockMultiplexer(_redisConnection) });
        }

        public async Task<Result> SaveItemAsync(string itemContent)
        {
            if (string.IsNullOrEmpty(itemContent))
            {
                return new Result(false, "Item content cannot be empty.");
            }

            var lockKey = $"lock:item:{itemContent}";
            var expiryTime = TimeSpan.FromSeconds(30);
            var waitTime = TimeSpan.FromSeconds(10);
            var retryTime = TimeSpan.FromMilliseconds(500);

            using (var redLock = await _redLockFactory.CreateLockAsync(lockKey, expiryTime, waitTime, retryTime))
            {
                if (redLock.IsAcquired)
                {
                    var db = _redisConnection.GetDatabase();
                    var cacheKey = $"item:{itemContent}";

                    if (await db.KeyExistsAsync(cacheKey))
                    {
                        return new Result(false, $"Duplicate item received with content {itemContent}.");
                    }

                    if (_itemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
                    {
                        await db.StringSetAsync(cacheKey, "exists", TimeSpan.FromMinutes(10));
                        return new Result(false, $"Duplicate item received with content {itemContent}.");
                    }

                    var item = _itemIntegrationBackend.SaveItem(itemContent);
                    await db.StringSetAsync(cacheKey, item.Id.ToString(), TimeSpan.FromMinutes(10));
                    return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
                }
                else
                {
                    return new Result(false, "Unable to acquire lock. Please try again later.");
                }
            }
        }

        public List<Item> GetAllItems()
        {
            return _itemIntegrationBackend.GetAllItems();
        }
    }
}