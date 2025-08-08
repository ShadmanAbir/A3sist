using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

var services = new ServiceCollection();

services.AddLogging(configure => configure.AddConsole());
services.AddHttpClient&lt;ILLMClient, CodestralLLMClient&gt;();
services.AddTransient&lt;FixerAgent&gt;();

var serviceProvider = services.BuildServiceProvider();

var fixerAgent = serviceProvider.GetRequiredService&lt;FixerAgent&gt;();
var fixedCode = await fixerAgent.FixCodeAsync("public class Broken { public void Method() { } }");

Console.WriteLine(fixedCode);