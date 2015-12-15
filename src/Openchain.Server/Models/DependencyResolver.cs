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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Linq;
using Openchain.Ledger;

namespace Openchain.Server.Models
{
    public class DependencyResolver<T>
        where T : class
    {
        private readonly IComponentBuilder<T> builder;
        private readonly Task initialize;

        public DependencyResolver(IAssemblyLoadContextAccessor assemblyLoader, string basePath, IDictionary<string, string> parameters)
        {
            IList<Assembly> assemblies = LoadAllAssemblies(basePath, assemblyLoader);

            this.builder = FindBuilder(assemblies, parameters);

            if (this.builder != null)
                initialize = this.builder.Initialize(parameters);
        }

        private IComponentBuilder<T> FindBuilder(IList<Assembly> assemblies, IDictionary<string, string> parameters)
        {
            return (from assembly in assemblies
                    from type in assembly.GetTypes()
                    where typeof(IComponentBuilder<T>).IsAssignableFrom(type)
                    let instance = (IComponentBuilder<T>)type.GetConstructor(Type.EmptyTypes).Invoke(new object[0])
                    where instance.Name == parameters["provider"]
                    select instance)
                    .FirstOrDefault();
        }

        public static DependencyResolver<T> Create(IConfiguration config, IApplicationEnvironment application, IAssemblyLoadContextAccessor assemblyLoader)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (IConfigurationSection section in config.GetChildren())
                parameters.Add(section.Key, section.Value);

            return new DependencyResolver<T>(assemblyLoader, application.ApplicationBasePath, parameters);
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

        private static IList<Assembly> LoadAllAssemblies(string projectPath, IAssemblyLoadContextAccessor assemblyLoader)
        {
            string projectFilePath = Path.Combine(projectPath, "project.json");
            JObject configurationFile = JObject.Parse(File.ReadAllText(projectFilePath));

            JObject dependencies = (JObject)configurationFile["dependencies"];

            return dependencies.Properties()
                .Select(property => property.Name)
                .Where(name => name.StartsWith("Openchain."))
                .Select(name => assemblyLoader.Default.Load(name))
                .ToList();
        }
    }
}
