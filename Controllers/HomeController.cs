using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using box_dotnet_sdk_oauth_sample.Models;
using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using Microsoft.AspNetCore.Http;

namespace box_dotnet_sdk_oauth_sample.Controllers
{
    public class HomeController : Controller
    {
        // マイアプリ(標準OAuth）→ 構成 → クライアントIDとクライアントシークレット
        private const string clientId = "asly78y1y07oux91k1ay2g3i8whpjq61";
        private const string clientSecret = "Jti29KH4L1tAJKa7rd0kX3PEI8DGHuHJ";
        private const string callBackUrl = "https://localhost:5001/Home/UIElements";

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

            OAuthSession session = client.Auth.Session;

            HttpContext.Session.SetString("AccessToken", session.AccessToken);
            HttpContext.Session.SetString("RefreshToken", session.RefreshToken);
            HttpContext.Session.SetInt32("ExpiresIn", session.ExpiresIn);
            HttpContext.Session.SetString("TokenType", session.TokenType);


            var user = await client.UsersManager.GetCurrentUserInformationAsync();
            Console.WriteLine($"Login User: Id={user.Id}, Name={user.Name}, Login={user.Login}");

            ViewBag.accessToken = accessToken;
            return View();
        }

        public async Task<IActionResult> Upload(String code)
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");
            var refreshToken = HttpContext.Session.GetString("RefreshToken");
            var expiresIn = HttpContext.Session.GetInt32("ExpiresIn") ?? default(int);
            var tokenType = HttpContext.Session.GetString("TokenType");

            Console.WriteLine($"session accessToken {accessToken}");
            Console.WriteLine($"session refreshToken {refreshToken}");
            Console.WriteLine($"session expiresIn {expiresIn}");
            Console.WriteLine($"session tokenType {tokenType}");

            var session = new OAuthSession(accessToken, refreshToken, expiresIn, tokenType);


            var config = new BoxConfig(clientId, clientSecret, new System.Uri(callBackUrl));
            var client = new BoxClient(config, session);

            var filePath = @"appsettings.json";


            var count = HttpContext.Session.GetInt32("count") ?? 0;
            HttpContext.Session.SetInt32("count", ++count);

            await using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BoxFileRequest requestParams = new BoxFileRequest()
                {
                    Name = $"test-upload-{count}.json",
                    Parent = new BoxRequestEntity() {Id = "0"}
                };

                BoxFile file = await client.FilesManager.UploadAsync(requestParams, fileStream);
                Console.WriteLine($"uploaded {file.Id} / {file.Name}");
            }

            // return RedirectToAction("UIElements");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}