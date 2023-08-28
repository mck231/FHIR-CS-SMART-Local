// See https://aka.ms/new-console-template for more information

using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq;

/// <summary>
/// Main Program
/// </summary>
public static class Program
{
    private const string _defaultFhirServerUrl = "https://launch.smarthealthit.org/v/r4/sim/WzIsIiIsIjg1YWVjYTU5LTYyNjEtNDcwNC1hMmU0LTE4NDZlYzU4MzFhNiIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMV0/fhir/";


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

        Task.Run(() => CreateHostBuilder().Build().Run());

        int listenPort = GetListenPort().Result;

        System.Console.WriteLine($"Listening on: {listenPort}");

        for(int loops = 0; loops< 10; loops++)
        {
            System.Threading.Thread.Sleep(1000);
        }
        

        return 0;
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
