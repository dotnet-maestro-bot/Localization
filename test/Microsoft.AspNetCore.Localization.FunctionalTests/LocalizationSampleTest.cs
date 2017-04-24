// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationSampleTest : LoggedTest
    {
        private static readonly string _applicationPath = Path.Combine("samples", "LocalizationSample");

        public LocalizationSampleTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task RunSite_WindowsOnlyAsync()
        {
            using (StartLog(out var loggerFactory))
            {
                var testRunner = new TestRunner(_applicationPath);

                await testRunner.RunTestAndVerifyResponseHeading(
                    loggerFactory,
                    RuntimeFlavor.Clr,
                    RuntimeArchitecture.x64,
                    "http://localhost:5080",
                    "My/Resources",
                    "fr-FR",
                    "<h1>Bonjour</h1>");
            }
        }

        [Fact]
        public async Task RunSite_AnyOSAsync()
        {
            using (StartLog(out var loggerFactory))
            {
                var testRunner = new TestRunner(_applicationPath);

                await testRunner.RunTestAndVerifyResponseHeading(
                    loggerFactory,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64,
                    "http://localhost:5081/",
                    "My/Resources",
                    "fr-FR",
                    "<h1>Bonjour</h1>");
            }
        }
    }
}
