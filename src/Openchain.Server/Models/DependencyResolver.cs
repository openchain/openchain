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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;

namespace Openchain.Server.Models
{
    public class DependencyResolver<T>
        where T : class
    {
        private readonly Func<T> builder;

        public DependencyResolver(IAssemblyLoadContextAccessor assemblyLoader, string assemblyName, IDictionary<string, string> parameters)
        {
            Assembly assembly = assemblyLoader.Default.Load(assemblyName);

            if (assembly != null)
                this.builder = FindConstructor(assembly, parameters);
            else
                this.builder = null;
        }

        public static DependencyResolver<T> Create(IConfiguration config, IAssemblyLoadContextAccessor assemblyLoader)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (IConfigurationSection section in config.GetSection("settings").GetChildren())
                parameters.Add(section.Key, section.Value);

            return new DependencyResolver<T>(assemblyLoader, config["type"], parameters);
        }

        public T Build()
        {
            if (builder == null)
                return null;
            else
                return builder();
        }

        private static Func<T> FindConstructor(Assembly assembly, IDictionary<string, string> parameters)
        {
            Type type = assembly.GetTypes().FirstOrDefault(item => typeof(T).IsAssignableFrom(item));

            if (type == null)
                return null;

            foreach (ConstructorInfo constructor in type.GetConstructors())
            {
                Expression[] result = Activate(constructor, parameters);
                if (result != null)
                    return Expression.Lambda<Func<T>>(Expression.New(constructor, result)).Compile();
            }

            return null;
        }

        private static Expression[] Activate(ConstructorInfo constructor, IDictionary<string, string> parameters)
        {
            ParameterInfo[] constructorParameters = constructor.GetParameters();

            Expression[] invokeParameters = new Expression[constructorParameters.Length];

            for (int i = 0; i < constructorParameters.Length; i++)
            {
                string value;
                if (!parameters.TryGetValue(constructorParameters[i].Name, out value))
                    return null;

                if (constructorParameters[i].ParameterType == typeof(string))
                {
                    invokeParameters[i] = Expression.Constant(value);
                }
                else
                {
                    return null;
                }
            }

            return invokeParameters;
        }
    }
}
