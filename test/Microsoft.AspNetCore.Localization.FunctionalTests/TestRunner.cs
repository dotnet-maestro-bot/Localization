// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class TestRunner
    {
        private const string SolutionName = "Localization.sln";
        private string _applicationPath;

        public TestRunner(string applicationPath)
        {
            _applicationPath = Path.Combine(
                ResolveSolutionDirectory(PlatformServices.Default.Application.ApplicationBasePath), 
                applicationPath,
                Path.GetFileName(applicationPath) + ".csproj");
        }

        private static string ResolveSolutionDirectory(string projectFolder)
        {
            var directory = new DirectoryInfo(projectFolder);

            while (directory.Parent != null)
            {
                var solutionPath = Path.Combine(directory.FullName, SolutionName);

                if (File.Exists(solutionPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            // If we don't find any files then make the project folder the root
            throw new Exception($"Unable to find solution {SolutionName} file given the project directory {projectFolder}.");
        }

        private async Task<string> RunTestAndGetResponse(
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string applicationBaseUrl,
            string environmentName,
            string locale)
        {
            var logger = new LoggerFactory()
                .AddConsole(LogLevel.Debug)
                .CreateLogger(string.Format("Localization Test Site:{0}:{1}:{2}", ServerType.Kestrel, runtimeFlavor, runtimeArchitecture));

            using (logger.BeginScope("LocalizationTest"))
            {
                var deploymentParameters = new DeploymentParameters(_applicationPath, ServerType.Kestrel, runtimeFlavor, runtimeArchitecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = environmentName,
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.1",
                    WorkingDirectory = Path.GetDirectoryName(_applicationPath),
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();

                    var cookie = new Cookie(CookieRequestCultureProvider.DefaultCookieName, "c=" + locale + "|uic=" + locale);
                    var cookieContainer = new CookieContainer();
                    cookieContainer.Add(new Uri(deploymentResult.ApplicationBaseUri), cookie);

                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.CookieContainer = cookieContainer;

                    using (var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) })
                    {
                        // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                        var response = await RetryHelper.RetryRequest(() =>
                        {
                            return httpClient.GetAsync(string.Empty);
                        }, logger, deploymentResult.HostShutdownToken);

                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        public async Task RunTestAndVerifyResponse(
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string applicationBaseUrl,
            string environmentName,
            string locale,
            string expectedText)
        {
            var responseText = await RunTestAndGetResponse(runtimeFlavor, runtimeArchitecture, applicationBaseUrl, environmentName, locale);
            Console.WriteLine("Response Text " + responseText);
            Assert.Equal(expectedText, responseText);
        }

        public async Task RunTestAndVerifyResponseHeading(
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string applicationBaseUrl,
            string environmentName,
            string locale,
            string expectedHeadingText)
        {
            var responseText = await RunTestAndGetResponse(runtimeFlavor, runtimeArchitecture, applicationBaseUrl, environmentName, locale);
            var headingIndex = responseText.IndexOf(expectedHeadingText);
            Console.WriteLine("Response Header " + responseText);
            Assert.True(headingIndex >= 0);
        }
    }
}