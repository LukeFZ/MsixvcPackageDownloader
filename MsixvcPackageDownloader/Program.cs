using MsixvcPackageDownloader.Models;
using XboxWebApi.Authentication;
using XboxWebApi.Authentication.Model;
using XboxWebApi.Common;

namespace MsixvcPackageDownloader
{
    internal class Program
    {
        private const string TokenFilename = "token.json";
        private static string TokenPath => Path.Join(Directory.GetCurrentDirectory(), TokenFilename);

        private const string PackageUrl = "https://packagespc.xboxlive.com/GetBasePackage/";

        private static XToken _updateToken;

        /* This is just a POC for this endpoint */
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing...");

            AuthenticationService? authService = null;

            if (File.Exists(TokenPath))
            {
                authService = await AuthenticationService.LoadFromJsonFileAsync(TokenPath);
                if (!authService.XToken.Valid)
                {
                    Console.WriteLine("Token expired, please reauthenticate!");
                    authService = null;
                }
            }

            if (authService == null)
            {
                var requestUrl =
                    AuthenticationService.GetWindowsLiveAuthenticationUrl(
                        new WindowsLiveAuthenticationQuery(clientId: "00000000402b5328"));

                Console.WriteLine(
                    "Please sign-in at this url in your browser, then paste the resulting URL back into this window and press enter.");
                Console.WriteLine($"Url: {requestUrl}");

                var resultingUrl = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(resultingUrl))
                    return;

                // Attempt to fix URL errors
                if (!resultingUrl.Contains('#') && resultingUrl.Contains("access_token="))
                {
                    Console.WriteLine("Detected reference to access_token without '#', attempting to fix URL format...");
                    if (resultingUrl.Contains("&access_token="))
                    {
                        resultingUrl = resultingUrl.Replace("&access_token=", "#access_token=");
                    }
                    else if (resultingUrl.Contains("?access_token="))
                    {
                        resultingUrl = resultingUrl.Replace("?access_token=", "#access_token=");
                    }
                }

                if (!resultingUrl.Contains("refresh_token"))
                    resultingUrl += "&refresh_token=thisisunused";

                try
                {
                    var response = AuthenticationService.ParseWindowsLiveResponse(resultingUrl);

                    authService = new AuthenticationService(response);
                    authService.UserToken = await AuthenticationService.AuthenticateXASUAsync(authService.AccessToken);
                    authService.XToken = await AuthenticationService.AuthenticateXSTSAsync(authService.UserToken, authService.DeviceToken, authService.TitleToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine("CRITICAL AUTHENTICATION ERROR");
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine($"Error Message: {ex.Message}");
                    Console.WriteLine("Full Stack Trace:");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine();
                    Console.WriteLine("Press any key to retry or exit...");
                    Console.ReadLine();
                    return;
                }
            }

            await authService.DumpToJsonFileAsync(TokenPath);

            await GetUpdateXSTSToken(authService.UserToken, authService.DeviceToken);

            Console.WriteLine("Initialization finished!");

            while (true)
            {
                var updateHttpClient = new HttpClient();

                Console.WriteLine("Please enter the ContentId of the package you want to fetch download links for:");
                var contentId = Console.ReadLine();

                if (!_updateToken.Valid)
                {
                    if (!authService.UserToken.Valid || !authService.DeviceToken.Valid)
                    {
                        if (!await authService.AuthenticateAsync())
                        {
                            Console.WriteLine("Could not regenerate update token. Please restart the app and reauthenticate!");
                            return;
                        }
                    }

                    await GetUpdateXSTSToken(authService.UserToken, authService.DeviceToken);
                }

                var isValidId = Guid.TryParse(contentId, out var contentGuid);
                if (!isValidId)
                {
                    Console.WriteLine("Error: You entered an invalid content id.");
                }
                else
                {
                    var updateUrl = PackageUrl + contentId;
                    var updateRequest = new HttpRequestMessage(HttpMethod.Get, updateUrl);
                    updateRequest.Headers.Add("Authorization", $"XBL3.0 x={_updateToken.UserInformation.Userhash};{_updateToken.Jwt}");

                    var updateResult = await updateHttpClient.SendAsync(updateRequest);
                    if (!updateResult.IsSuccessStatusCode)
                        Console.WriteLine($"Failed to fetch package information. Status Code: {updateResult.StatusCode}");
                    else
                    {
                        try
                        {
                            var updateData = await updateResult.Content.ReadAsJsonAsync<GetBasePackageResponse>();
                            Console.WriteLine("Got response!");

                            if (updateData.PackageFound)
                            {
                                foreach (var file in updateData.PackageFiles.Where(pred =>  // PC files have the .msixvc extension, Xbox files don't have any
                                             !pred.FileName.EndsWith(".phf") &&
                                             !pred.FileName.EndsWith(".xsp")))
                                    Console.WriteLine($"{file.FileName} | Size: {file.FileSize} | Link: {file.CdnRootPaths[0] + file.RelativeUrl}");
                            }
                            else
                            {
                                Console.WriteLine("Error: Server did not find requested package.");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error while parsing server response. {e}");
                        }
                    }
                }
            }
        }

        public static async Task<bool> GetUpdateXSTSToken(UserToken userToken, DeviceToken deviceToken)
        {
            var httpClient = AuthenticationService.ClientFactory("https://xsts.auth.xboxlive.com");
            var request = new HttpRequestMessage(HttpMethod.Post, "xsts/authorize");
            var xstsTokenRequest = new XSTSRequest(userToken, "http://update.xboxlive.com", deviceToken: deviceToken);

            request.Headers.Add("x-xbl-contract-version", "1");
            request.Content = new JsonContent(xstsTokenRequest);

            var response = await httpClient.SendAsync(request);
            var responseData = await response.Content.ReadAsJsonAsync<XASResponse>();
            _updateToken = new XToken(responseData);
            return true;
        }
    }
}