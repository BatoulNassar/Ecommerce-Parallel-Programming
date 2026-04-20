// See https://aka.ms/new-console-template for more information
using Ecommerce.StressTests;

Console.WriteLine("Starting the stress test on the procurement system...");

BuyProduct_StressTest.Run();
Console.WriteLine("The test has been completed.");
Console.ReadKey();