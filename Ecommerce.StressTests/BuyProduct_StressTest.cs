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

            var protectedScenario = Scenario.Create("Protected Scenario", async context =>
            {
                var url = "https://localhost:7036/api/Products/buy-with-thread?ProductId=1&UserId=1&quantity=1";
                var request = Http.CreateRequest("POST", url);
                return await Http.Send(httpClient, request);
            })
            .WithLoadSimulations(Simulation.KeepConstant(copies: 1000, during: TimeSpan.FromSeconds(30)));

           
            var unprotectedScenario = Scenario.Create("Unprotected Scenario", async context =>
            {
                var url = "https://localhost:7036/api/Products/buy-without-thread?ProductId=1&UserId=1&quantity=1";
                var request = Http.CreateRequest("POST", url);
                return await Http.Send(httpClient, request);
            })
            .WithLoadSimulations(Simulation.KeepConstant(copies: 1000, during: TimeSpan.FromSeconds(30)));

           
            NBomberRunner
                .RegisterScenarios(unprotectedScenario) 
                .Run();
        }
    }
}
