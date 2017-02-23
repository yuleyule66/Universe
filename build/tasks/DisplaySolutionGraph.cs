// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.UniverseTasks
{
    public class DisplaySolutionGraph : Task
    {
        [Required]
        public ITaskItem[] RestoreGraphEntries { get; set; }

        [Required]
        public ITaskItem[] Repositories { get; set; }

        public override bool Execute()
        {
            var repositoryLookup = new List<RepositoryInfo>();

            foreach(var repo in Repositories)
            {
                repositoryLookup.Add(new RepositoryInfo
                {
                    Name = repo.ItemSpec
                });
            }

            foreach (var entry in RestoreGraphEntries)
            {
                var type = entry.GetMetadata("Type");
                if (type == "ProjectReference")
                {
                    var count = 1;
                }
                else if (type == "ProjectSpec")
                {
                    var packageId = entry.GetMetadata("ProjectName");
                    var packageVersion = entry.GetMetadata("Version");
                }
                else if (type == "Dependency")
                {
                    var count = 3;
                }
                else if (type == "ProjectReference")
                {
                    var count = 3;
                }
            }

            // foreach (var info in repositoryLookup)
            // {
            //     foreach (var item in info.DependencyNames)
            //     {
            //         var dependency = repositoryLookup.Find(r => r.PackageNames.Contains(item));
            //         if (dependency != null)
            //         {
            //             info.Dependencies.Add(dependency);
            //         }
            //     }
            // }

            // var batches = repositoryLookup.GroupBy(r => r.Order, r => r.Name).OrderBy(r => r.Key).ToArray();
            // foreach ( var batch in batches)
            // {
            //     Log.LogMessage(MessageImportance.High, "{0} - {1}", batch.Key, string.Join(", ", batch));
            // }

            return false;
        }
    }

    public class RepositoryInfo
    {
        public string Name { get ; set; }

        public HashSet<string> PackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> DependencyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HashSet<RepositoryInfo> Dependencies = new HashSet<RepositoryInfo>();

        public int Order
        {
            get
            {
                return GetOrder(new List<RepositoryInfo>(), this);
            }
        }

        private static int GetOrder(List<RepositoryInfo> visited, RepositoryInfo info)
        {
            if (visited.Contains(info))
            {
                throw new Exception("A cyclic dependency between the following repositories has been detected: " +
                    string.Join(" -> ", visited));
            }

            visited.Add(info);

            var order = 0;
            foreach (var dependency in info.Dependencies)
            {
                order = Math.Max(order, GetOrder(visited, dependency));
            }

            visited.RemoveAt(visited.Count - 1);

            return order + 1;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
