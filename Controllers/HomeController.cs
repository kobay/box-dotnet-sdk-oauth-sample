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
        private const string ClientId = "asly78y1y07oux91k1ay2g3i8whpjq61";
        private const string ClientSecret = "Jti29KH4L1tAJKa7rd0kX3PEI8DGHuHJ";
        private const string CallBackUrl = "https://localhost:5001/Home/BoxRedirect";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var config = new BoxConfig(ClientId, ClientSecret, new System.Uri(CallBackUrl));
            
            // 認証用URLは、文字列を連結してもいいですが、このコードで作れます。
            ViewBag.authorizationUrl = config.AuthCodeUri.ToString();
            // ViewBag.authorizationUrl = $"https://account.box.com/api/oauth2/authorize?client_id={clientId}&response_type=code";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> BoxRedirect(String code)
        {
            // AUTHコードが受け取れたか確認
            Console.WriteLine("BoxRedirect {0}", code);

            
            var config = new BoxConfig(ClientId, ClientSecret, new System.Uri(CallBackUrl));
            var client = new BoxClient(config);

            // AUTHコード → アクセストークンへ交換
            await client.Auth.AuthenticateAsync(code);
            
            var accessToken = client.Auth.Session.AccessToken;
            //var refreshToken = client.Auth.Session.RefreshToken;

            OAuthSession session = client.Auth.Session;

            HttpContext.Session.SetString("AccessToken", session.AccessToken);
            HttpContext.Session.SetString("RefreshToken", session.RefreshToken);
            HttpContext.Session.SetInt32("ExpiresIn", session.ExpiresIn);
            HttpContext.Session.SetString("TokenType", session.TokenType);
            
            // clientが動くかテスト
            var user = await client.UsersManager.GetCurrentUserInformationAsync();
            Console.WriteLine($"Login User: Id={user.Id}, Name={user.Name}, Login={user.Login}");
            
            ViewBag.accessToken = accessToken;
            return RedirectToAction("UIElements");
        }
        
        public async Task<IActionResult> UIElements()
        {
            // セッションからアクセストークン等を取り出す
            // ここはおそらくよりよい方法があるはず
            var accessToken = HttpContext.Session.GetString("AccessToken");
            var refreshToken = HttpContext.Session.GetString("RefreshToken");
            // expiresInはそのまま利用すべきではないと思われるが・・
            var expiresIn = HttpContext.Session.GetInt32("ExpiresIn") ?? default(int);
            var tokenType = HttpContext.Session.GetString("TokenType");

            Console.WriteLine($"session accessToken {accessToken}");
            Console.WriteLine($"session refreshToken {refreshToken}");
            Console.WriteLine($"session expiresIn {expiresIn}");
            Console.WriteLine($"session tokenType {tokenType}");
            
            // sessionを組み立て直し
            var session = new OAuthSession(accessToken, refreshToken, expiresIn, tokenType);
            // clientをSessionを元に作成
            var config = new BoxConfig(ClientId, ClientSecret, new System.Uri(CallBackUrl));
            var client = new BoxClient(config, session);
            
            // 動作確認として認証したユーザーの情報を表示
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


            var config = new BoxConfig(ClientId, ClientSecret, new System.Uri(CallBackUrl));
            var client = new BoxClient(config, session);

            // appsettins.jsonをルートフォルダ配下にアップロードしてみる
            var filePath = @"appsettings.json";

            // 同名のファイルでエラーにならないようにランダムな数字を付与する
            Random rnd = new Random();
            var rndNum = rnd.Next(Int32.MaxValue);

            await using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BoxFileRequest requestParams = new BoxFileRequest()
                {
                    Name = $"test-upload-{rndNum}.json",
                    Parent = new BoxRequestEntity() {Id = "0"}
                };

                BoxFile file = await client.FilesManager.UploadAsync(requestParams, fileStream);
                Console.WriteLine($"uploaded {file.Id} / {file.Name}");
            }

            // アップロードしたファイルをもう一度UI ELEMENTSで確認
            return RedirectToAction("UIElements");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}