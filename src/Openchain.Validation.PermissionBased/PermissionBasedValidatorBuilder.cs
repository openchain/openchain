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
using Microsoft.Extensions.DependencyInjection;
using Openchain.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.Validation.PermissionBased
{
    public class PermissionBasedValidatorBuilder : IComponentBuilder<PermissionBasedValidator>
    {
        private List<IPermissionsProvider> staticPermissionProviders = new List<IPermissionsProvider>();
        private KeyEncoder keyEncoder;

        public string Name { get; } = "PermissionBased";

        public PermissionBasedValidator Build(IServiceProvider serviceProvider)
        {
            List<IPermissionsProvider> providers = new List<IPermissionsProvider>(staticPermissionProviders);
            providers.Add(new DynamicPermissionLayout(serviceProvider.GetRequiredService<IStorageEngine>(), keyEncoder));

            return new PermissionBasedValidator(providers);
        }

        public Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            byte versionByte = byte.Parse(configuration["version_byte"]);
            this.keyEncoder = new KeyEncoder(versionByte);

            P2pkhSubject[] adminAddresses = configuration
                .GetSection("admin_addresses")
                .GetChildren()
                .Select(key => key.Value)
                .Select(address => new P2pkhSubject(new[] { address }, 1, keyEncoder))
                .ToArray();

            List<Acl> pathPermissions = new List<Acl>()
            {
                // Admins have full rights
                new Acl(adminAddresses, LedgerPath.Parse("/"), true, StringPattern.MatchAll, PermissionSet.AllowAll)
            };

            if (bool.Parse(configuration["allow_third_party_assets"]))
                this.staticPermissionProviders.Add(new P2pkhIssuanceImplicitLayout(keyEncoder));

            if (bool.Parse(configuration["allow_p2pkh_accounts"]))
                this.staticPermissionProviders.Add(new P2pkhImplicitLayout(keyEncoder));

            this.staticPermissionProviders.Add(new StaticPermissionLayout(pathPermissions));

            return Task.FromResult(0);
        }
    }
}
