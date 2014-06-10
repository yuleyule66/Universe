using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace TCDependencyManager
{
    class Program
    {
        private static readonly string[] _excludedRepos = new[] { "xunit", "kruntime", "coreclr", "universe", "rolsyn" };

        static int Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Usage TCDependencyManager.exe ServerUrl UserName Password CI_DGML_PATH");
            }

            var teamCityUrl = args[0];
            var teamCityUser = args[1];
            var teamCityPass = args[2];

            var teamCity = new TeamCityAPI(teamCityUrl,
                                           new NetworkCredential(teamCityUser, teamCityPass));
            string ns = "http://schemas.microsoft.com/vs/2009/dgml";

            var projects = teamCity.GetProjects().Select(p => p.Id);//.Except(_excludedRepos, StringComparer.OrdinalIgnoreCase);

            var nodes = new XElement(XName.Get("Nodes", ns));
            var links = new XElement(XName.Get("Links", ns));
            
            var root = new XElement(XName.Get("DirectedGraph", ns), nodes, links);
            var doc = new XDocument(root);

            
            foreach (var project in projects)
            {
                nodes.Add(new XElement(XName.Get("Node", ns), new XAttribute("Id", project)));
                var dependencies = teamCity.GetTriggers(project)
                                           .FirstOrDefault(t => t.Type.Equals("complexFinishBuildTrigger", StringComparison.OrdinalIgnoreCase));

                if (dependencies != null)
                {
                    foreach (var dependency in dependencies.Properties.Property[0].Value.Split(';'))
                    {
                        links.Add(new XElement(XName.Get("Link", ns), new XAttribute("Target", project), new XAttribute("Source", dependency.Replace(".", ""))));
                    }

                }
            }
            doc.Save(args[3]);

            return 0;
        }

        private static void MapRepoDependencies(List<Project> projects)
        {
            var projectLookup = projects.ToDictionary(project => project.ProjectName, StringComparer.OrdinalIgnoreCase);

            foreach (var project in projects)
            {
                foreach (var dependency in project.Dependencies)
                {
                    Project dependencyProject;
                    if (projectLookup.TryGetValue(dependency, out dependencyProject) &&
                        project.Repo != dependencyProject.Repo)
                    {
                        project.Repo.Dependencies.Add(dependencyProject.Repo);
                    }
                }

            }
        }
    }
}
