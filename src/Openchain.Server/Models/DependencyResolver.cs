// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Openchain.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Openchain.Server.Models
{
    public class DependencyResolver<T>
        where T : class
    {
        private readonly IComponentBuilder<T> builder;
        private readonly Task initialize;

        public DependencyResolver(IServiceProvider serviceProvider, string basePath, IConfigurationSection configuration)
        {
            IList<Assembly> assemblies = LoadAllAssemblies(basePath);

            this.builder = FindBuilder(assemblies, configuration);

            if (this.builder != null)
                initialize = this.builder.Initialize(serviceProvider, configuration);
        }

        private IComponentBuilder<T> FindBuilder(IList<Assembly> assemblies, IConfigurationSection configuration)
        {
            return (from assembly in assemblies
                    from type in assembly.GetTypes()
                    where typeof(IComponentBuilder<T>).IsAssignableFrom(type)
                    let instance = (IComponentBuilder<T>)type.GetConstructor(Type.EmptyTypes).Invoke(new object[0])
                    where instance.Name.Equals(configuration["provider"], StringComparison.OrdinalIgnoreCase)
                    select instance)
                    .FirstOrDefault();
        }

        public static async Task<Func<IServiceProvider, T>> Create(IServiceProvider serviceProvider, string configurationPath)
        {
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
            IWebHostEnvironment application = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            IConfigurationSection rootSection = configuration.GetSection(configurationPath);

            try
            {
                DependencyResolver<T> resolver = new DependencyResolver<T>(serviceProvider, new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName, rootSection);

                if (resolver.builder == null)
                    serviceProvider.GetRequiredService<ILogger>().LogWarning($"Unable to find a provider for {typeof(T).FullName} from the '{configurationPath}' configuration section.");

                return await resolver.Build();
            }
            catch (Exception exception)
            {
                serviceProvider.GetRequiredService<ILogger>().LogError($"Error while creating {typeof(T).FullName} from the '{configurationPath}' configuration section:\n {exception}");
                throw;
            }
        }

        public async Task<Func<IServiceProvider, T>> Build()
        {
            if (builder == null)
            {
                return _ => null;
            }
            else
            {
                await initialize;
                return builder.Build;
            }
        }

        private static IList<Assembly> LoadAllAssemblies(string projectPath)
        {
            return DependencyContext.Default.RuntimeLibraries
                .Where(library => library.Name.StartsWith("Openchain.", StringComparison.OrdinalIgnoreCase))
                .Select(library => Assembly.Load(new AssemblyName(library.Name)))
                .ToList();
        }
    }
}
