using NBomber.CSharp;
using NBomber.Http.CSharp;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Ecommerce.StressTests
{
    public class BuyProduct_StressTest
    {
        public static void Run()
        {
            using var httpClient = new HttpClient();

            var body = JsonSerializer.Serialize(new
            {
                productId = 1,
                quantity = 1
            });

            var scenario = Scenario.Create("E-Commerce Purchase Scenario", async context =>
            {
                var url = "https://localhost:7036/api/Products?ProductId=1&UserId=1&quantity=1";
                var request = Http.CreateRequest("POST", url);
                return await Http.Send(httpClient, request);
            })
            .WithLoadSimulations(

                Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(1))
            );

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }
    }
}