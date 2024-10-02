using Integration.Service;

namespace Integration;

public abstract class Program
{
    public static void Main(string[] args)
    {
        //var service = new ItemIntegrationService();

        var service = new ItemIntegrationService(
            new Backend.ItemOperationBackend(),
            "redisConnString");


        ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemAsync("a"));
        ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemAsync("b"));
        ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemAsync("c"));

        Thread.Sleep(500);

        ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemAsync("a"));
        ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemAsync("b"));
        ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemAsync("c"));

        Thread.Sleep(5000);

        Console.WriteLine("Everything recorded:");

        service.GetAllItems().ForEach(Console.WriteLine);

        Console.ReadLine();
    }
}