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

using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Openchain.Infrastructure
{
    /// <summary>
    /// Represents a builder class capable of instanciating an object of a given type.
    /// </summary>
    /// <typeparam name="T">The type of object that can be instanciated by the builder.</typeparam>
    public interface IComponentBuilder<out T>
    {
        /// <summary>
        /// Gets the name of the builder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes the builder.
        /// </summary>
        /// <param name="serviceProvider">The service provider for the current context.</param>
        /// <param name="configuration">The builder configuration.</param>
        /// <returns></returns>
        Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration);

        /// <summary>
        /// Builds the target object.
        /// </summary>
        /// <param name="serviceProvider">The service provider for the current context.</param>
        /// <returns>The built object.</returns>
        T Build(IServiceProvider serviceProvider);
    }
}
