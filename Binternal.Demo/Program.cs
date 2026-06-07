using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;

using var services = new ServiceCollection()
    .AddHttpClient("demo")
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(2, _ => TimeSpan.FromMilliseconds(50))
    )
    .Services.BuildServiceProvider();

using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("demo");

Console.WriteLine($"Client created: {client.GetType().Name}");
