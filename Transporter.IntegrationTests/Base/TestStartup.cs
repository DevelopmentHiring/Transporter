using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transporter.CouchbaseAdapter.Data.Implementations;
using Transporter.CouchbaseAdapter.Data.Interfaces;
using Transporter.IntegrationTests.Helpers.Couchbase.Implementations;
using Transporter.IntegrationTests.Helpers.Couchbase.Interfaces;

namespace Transporter.IntegrationTests.Base
{
    public class TestStartup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            UpdateAppSetting("test1", "55");
            services.AddTransient<ICouchbaseProviderHelper, CouchbaseProviderHelper>();
            services.AddTransient<ICouchbaseProvider, CouchbaseProvider>();
            services.AddTransient<IBucketProvider, BucketProvider>();

            services.AddSingleton(configuration);
        }
        
        public static void UpdateAppSetting(string key, string value)
        {
            var configJson = File.ReadAllText("appsettings.json");
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
            config[key] = value;
            var updatedConfigJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("appsettings.json", updatedConfigJson);
        }

        public static void Configure(IApplicationBuilder app)
        {
        }
    }
}