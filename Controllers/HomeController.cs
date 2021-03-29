using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using box_dotnet_sdk_oauth_sample.Models;
using Box.V2;
using Box.V2.Config;

namespace box_dotnet_sdk_oauth_sample.Controllers
{
    public class HomeController : Controller
    {
        
        // マイアプリ(標準OAuth）→ 構成 → クライアントIDとクライアントシークレット
        private const string clientId = "asly78y1y07oux91k1ay2g3i8whpjq61";
        private const string  clientSecret = "Jti29KH4L1tAJKa7rd0kX3PEI8DGHuHJ";
        private const string  callBackUrl = "https://localhost:5001/Home/UIElements";
        
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            
            var config = new BoxConfig(clientId, clientSecret, new System.Uri(callBackUrl));
            ViewBag.authorizationUrl = config.AuthCodeUri.ToString();
                
            // ViewBag.authorizationUrl = $"https://account.box.com/api/oauth2/authorize?client_id={clientId}&response_type=code";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> UIElements(String code)
        {
            Console.WriteLine("BoxRedirect {0}", code);
            
            var config = new BoxConfig(clientId, clientSecret, new System.Uri(callBackUrl));
            var client = new BoxClient(config);
            
            await client.Auth.AuthenticateAsync(code);
            var accessToken = client.Auth.Session.AccessToken;
            //var refreshToken = client.Auth.Session.RefreshToken;

            var user = await client.UsersManager.GetCurrentUserInformationAsync();
            Console.WriteLine($"Login User: Id={user.Id}, Name={user.Name}, Login={user.Login}");

            ViewBag.accessToken = accessToken;
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
