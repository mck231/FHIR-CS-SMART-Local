// See https://aka.ms/new-console-template for more information

using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Invocation;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Web;

/// <summary>
/// Main Program
/// </summary>
public static class Program
{
    private const string _defaultFhirServerUrl = "https://launch.smarthealthit.org/v/r4/sim/WzIsIiIsIiIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMV0/fhir";

    private static string _authCode = string.Empty;
    private static string _clientState = string.Empty;
    private const string _clientId = "fhir_demo_id";
    private static string _redirectUrl = string.Empty;
    private static string _tokenUrl = string.Empty;

    /// <summary>
    /// Program to access a SMART FHIR Server with a local webserver for redirection
    /// </summary>
    /// <param name="fhirServerUrl"> FHIR R4 endpoint</param>
    /// <returns></returns>
    static int Main(string fhirServerUrl)
    {
        if(string.IsNullOrEmpty(fhirServerUrl))
        {
            fhirServerUrl = _defaultFhirServerUrl;
        }

        System.Console.WriteLine($"FHIR Server: {fhirServerUrl}");
        
        FhirClient fhirClient = new FhirClient(fhirServerUrl);

        if(!FhirUtils.TryGetSmartUrls(fhirClient, out string authorizeUrl, out string tokenUrl))
        {
            System.Console.WriteLine($"Failed to discover SMART URLs");
            return -1;
        }

        System.Console.WriteLine($"Authorize URL: {authorizeUrl}");
        System.Console.WriteLine($"     Token URL: {tokenUrl}");
        _tokenUrl = tokenUrl;

        Task.Run(() => CreateHostBuilder().Build().Run());

        int listenPort = GetListenPort().Result;

        System.Console.WriteLine($"Listening on: {listenPort}");

        _redirectUrl = $"http://127.0.0.1:{listenPort}";

        string url = $"{authorizeUrl}"+
        $"?response_type=code"+
        $"&client_id={_clientId}"+
        $"&redirect_uri={HttpUtility.UrlEncode(_redirectUrl)}"+
        $"&scope={HttpUtility.UrlEncode("openid fhirUser profile launch/patient+patient/*.read")}"+
        $"&state=local_state" +
        $"&aud={fhirServerUrl}";

        LaunchUrl(url);

        for(int loops = 0; loops< 30; loops++)
        {
            System.Threading.Thread.Sleep(1000);
        }      

        return 0;
    }

    /// <summary>
    /// Set the authorization code and state
    /// </summary>
    /// <param name="code"></param>
    /// <param name="state"></param>
    public static async void SetAuthCode(string code, string state)
    {
        _authCode = code;
        _clientState = state;

        System.Console.WriteLine($"Codee received: {code}");

        Dictionary<string, string> requestValues = new Dictionary<string, string>()
        {
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", _redirectUrl},
            {"client_id", _clientId},
        };

        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_tokenUrl),
            Content = new FormUrlEncodedContent(requestValues),
            
        };

        HttpClient client = new HttpClient();

        HttpResponseMessage response = await client.SendAsync(request);

        if(!response.IsSuccessStatusCode)
        {
            System.Console.WriteLine($"failed to exchange code for token");
            throw new Exception($"Unaunthorized: {response.StatusCode}");
        }

        string json = await response.Content.ReadAsStringAsync();

        System.Console.WriteLine($"------ Token ----------");
        System.Console.WriteLine(json);
        System.Console.WriteLine($"------ Token ----------");
    }

    /// <summary>
    /// Launch a URL in the user's default web browser
    /// </summary>
    /// <param name="url"></param>
    /// <returns>true if successful, false otherwise</returns>
    public static bool LaunchUrl(string url)
    {
        try
        {
             ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = url,
                UseShellExecute = true,
            };

            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (System.Exception)
        {
            //System.Console.WriteLine($"Failed to launch URL");
        }

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                url = url.Replace("&", "^&");
                System.Diagnostics.Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true});    
                return true;
            }
            catch (System.Exception)
            {
                System.Console.WriteLine($"Failed to launch URL");
                return false;
                //throw;
            }
            
        }
      
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string[] allowedProgramsToRun = { "xdg-open", "gnome-open", "kfmclient"};

            foreach (string helper in allowedProgramsToRun)
            {
                try
                {
                    System.Diagnostics.Process.Start(helper,url);
                    return true;
                }
                catch (System.Exception)
                {
                                        
                }
                
            }

            System.Diagnostics.Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                System.Diagnostics.Process.Start("open", url);    
                return true;
            }
            catch (System.Exception)
            {
                
                //throw;
            }
            
        }
        System.Console.WriteLine($"Failed to launch URL");
        return false;       
    }
 
    /// <summary>
    /// Start a web server in the background and wait for it to initialize
    /// </summary>
    public static async void StartWebServerInBackground()
    {
        //Task.Run(() => CreateHostBuilder().Build().Run());
        await Task.Delay(500);
    }


    /// <summary>
    /// Determine to the listening port of the web server
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<int> GetListenPort()
    {
        await Task.Delay(500);
        for(int loops = 0; loops < 100; loops++)
        {
            await Task.Delay(500);
            if(Startup.Addressess == null)
            {
                continue;
            }

            string address = Startup.Addressess.Addresses.FirstOrDefault();

            if(string.IsNullOrEmpty(address))
            {
                continue;
            }

            if(address.Length < 18)
            {
                continue;
            }

            if(int.TryParse(address.Substring(17), out int port) && (port != 0))
            {
                return port;
            }
        }

        throw new Exception($"Failed to get listen port!");
    }

     public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {                   
                    webBuilder.UseUrls("http://127.0.0.1:0");
                    webBuilder.UseKestrel();
                    webBuilder.UseStartup<Startup>();
                });
    
}
