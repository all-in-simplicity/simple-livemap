using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CitizenFX.Core;
using Octokit;

namespace LiveMap.Server.Utilities
{
    public sealed class UpdateChecker
    {
        private const string RepositoryName = "simple-livemap";

        private const string RepositoryOwner = "all-in-simplicity";

        private static readonly GitHubClient Client = new(new ProductHeaderValue("simple-livemap"));

        private static Version LocalVersion => new(GetAssemblyFileVersion());

        private static string GetAssemblyFileVersion()
        {
            var attribute = (AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).Single();

            return attribute.Version;
        }

        public static async Task CheckForUpdate()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                var latestRelease = await Client.Repository.Release.GetLatest(RepositoryOwner, RepositoryName);

                var latestVersion = new Version(latestRelease.TagName);

                var versionComparison = LocalVersion.CompareTo(latestVersion);

                if (versionComparison < 0)
                {
                    await BaseScript.Delay(0);

                    Debug.WriteLine($"^3An update is available for simple-livemap (current version: {LocalVersion})^7");

                    Debug.WriteLine($"^3{latestRelease.HtmlUrl}^7");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
