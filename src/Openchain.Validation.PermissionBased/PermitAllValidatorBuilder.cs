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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Openchain.Ledger;

namespace Openchain.Validation.PermissionBased
{
    public class PermitAllValidatorBuilder : IComponentBuilder<PermissionBasedValidator>
    {
        private readonly PermissionBasedValidator validator;

        public PermitAllValidatorBuilder()
        {
            P2pkhSubject subject = new P2pkhSubject(new string[0], 0, new KeyEncoder(0));
            List<Acl> permissions = new List<Acl>()
            {
                new Acl(new IPermissionSubject[] { subject }, LedgerPath.Parse("/"), true, StringPattern.MatchAll, PermissionSet.AllowAll)
            };

            StaticPermissionLayout layout = new StaticPermissionLayout(permissions);

            this.validator = new PermissionBasedValidator(new[] { layout });
        }

        public string Name { get; } = "PermitAll";

        public PermissionBasedValidator Build(IServiceProvider serviceProvider)
        {
            return validator;
        }

        public Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration) => Task.FromResult(0);
    }
}
