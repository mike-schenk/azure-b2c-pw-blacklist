using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;
using System.Security.Claims;
using WebApp_OpenIDConnect_DotNet.Models;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Web;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Newtonsoft.Json.Linq;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    public class HomeController : Controller
    {
        readonly AzureAdB2COptions _azureAdB2COptions;
        private readonly IHttpClientFactory _clientFactory;

        public HomeController(IOptions<AzureAdB2COptions> azureAdB2COptions, IHttpClientFactory clientFactory)
        {
            _azureAdB2COptions = azureAdB2COptions.Value;
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult About()
        {
            ViewData["Message"] = String.Format("Claims available for the user {0}", (User.FindFirst("name")?.Value));
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Api()
        {
            string responseString = "";
            try
            {
                // Retrieve the token with the specified scopes
                var scope = _azureAdB2COptions.ApiScopes.Split(' ');
                string signedInUserID = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                IConfidentialClientApplication cca =
                ConfidentialClientApplicationBuilder.Create(_azureAdB2COptions.ClientId)
                    .WithRedirectUri(_azureAdB2COptions.RedirectUri)
                    .WithClientSecret(_azureAdB2COptions.ClientSecret)
                    .WithB2CAuthority(_azureAdB2COptions.Authority)
                    .Build();
                new MSALStaticCache(signedInUserID, this.HttpContext).EnablePersistence(cca.UserTokenCache);

                var accounts = await cca.GetAccountsAsync();
                AuthenticationResult result = await cca.AcquireTokenSilent(scope, accounts.FirstOrDefault())
                    .ExecuteAsync();

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _azureAdB2COptions.ApiUrl);

                // Add token to the Authorization header and make the request
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = await client.SendAsync(request);

                // Handle the response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        responseString = await response.Content.ReadAsStringAsync();
                        break;
                    case HttpStatusCode.Unauthorized:
                        responseString = $"Please sign in again. {response.ReasonPhrase}";
                        break;
                    default:
                        responseString = $"Error calling API. StatusCode=${response.StatusCode}";
                        break;
                }
            }
            catch (MsalUiRequiredException ex)
            {
                responseString = $"Session has expired. Please sign in again. {ex.Message}";
            }
            catch (Exception ex)
            {
                responseString = $"Error calling API: {ex.Message}";
            }

            ViewData["Payload"] = $"{responseString}";            
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Delete()
        {
            // use the graph API to delete this user's account from the directory

            // NOTE: in order to delete users, the application itself
            // needs to be part of the global administrators (or maybe
            // some other admin) group.

            // tenantid comes out of the http://schemas.microsoft.com/identity/claims/tenantid claim
            // user's objectid comes from http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier

            // POST https://login.microsoftonline.com/{tenantid}/oauth2/token

            var tenantId = User.FindFirst(c => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid").Value;

            var tokenRequestParams = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", _azureAdB2COptions.GraphApiClientId},
                {"client_secret", _azureAdB2COptions.GraphApiClientSecret},
                {"Resource", "https://graph.windows.net"}
            };

            var req = new HttpRequestMessage(HttpMethod.Post,
                $"https://login.microsoftonline.com/{tenantId}/oauth2/token");
            req.Content = new FormUrlEncodedContent(tokenRequestParams);

            var client = _clientFactory.CreateClient();

            var res = await client.SendAsync(req);
            // retrieve the access_token from the JSON body of the response
            var resBody = await res.Content.ReadAsStringAsync();
            dynamic jsonResponse = JObject.Parse(resBody);
            string tkn = jsonResponse.access_token;


            // DELETE https://graph.windows.net/{tenantid}/users/{userobjectid}?api-version=1.6
            var userObjectId = User.FindFirst(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            req = new HttpRequestMessage(HttpMethod.Delete,
                $"https://graph.windows.net/{tenantId}/users/{userObjectId}?api-version=1.6");
            req.Headers.Add("Authorization", "Bearer " + tkn);
            res = await client.SendAsync(req);
            if (res.StatusCode == HttpStatusCode.NoContent)
                return RedirectToAction("signout", "Session");
            return View("About");
        }

        public IActionResult Error(string message)
        {
            ViewBag.Message = message;
            return View();
        }
    }
}
