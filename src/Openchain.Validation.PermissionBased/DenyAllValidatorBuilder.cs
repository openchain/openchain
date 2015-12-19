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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Openchain.Infrastructure;

namespace Openchain.Validation.PermissionBased
{
    public class DenyAllValidatorBuilder : IComponentBuilder<PermissionBasedValidator>
    {
        private readonly PermissionBasedValidator validator;

        public DenyAllValidatorBuilder()
        {
            StaticPermissionLayout layout = new StaticPermissionLayout(new Acl[0]);
            this.validator = new PermissionBasedValidator(new[] { layout });
        }

        public string Name { get; } = "DenyAll";

        public PermissionBasedValidator Build(IServiceProvider serviceProvider)
        {
            return validator;
        }

        public Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration) => Task.FromResult(0);
    }
}
