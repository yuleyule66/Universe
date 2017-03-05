using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace AddCommitInfo
{
    class Program
    {
        private const string HeadContentStart = "ref: refs/heads/";
        private const int CommitShaLength = 40;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            var repositoriesRootOption = app.Option("-r|--repositories-root",
                "Directory containing repositories to create the file for.",
                CommandOptionType.SingleValue);

            var repositoryFileOption = app.Argument("Repository file.", "Repository file to update.");
            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(repositoryFileOption.Value))
                {
                    Console.Error.WriteLine("Repository file argument not specified.");
                    return 1;
                }

                if (!repositoriesRootOption.HasValue())
                {
                    Console.Error.WriteLine($"Option {repositoriesRootOption.Template} must have a value.");
                    return 1;
                }

                Execute(repositoriesRootOption.Value().Trim(), repositoryFileOption.Value);

                return 0;
            });

            return app.Execute(args);
        }

        private static void Execute(string repositoriesRoot, string repositoryFile)
        {
            var document = XDocument.Load(repositoryFile);
            foreach (var repositoryElement in document.Root.Element("ItemGroup").Elements("Repository"))
            {
                var repositoryName = repositoryElement.Attribute("Include").Value;
                var repositoryDirectory = Path.Combine(repositoriesRoot, repositoryName);
                if (!Directory.Exists(repositoryDirectory))
                {
                    // Repository does not exist. Possibly due to not being cloned.
                    continue;
                }

                var commitHash = ReadCommitHash(repositoryDirectory);
                repositoryElement.Add(new XAttribute("Commit", commitHash));
            }

            File.WriteAllText(repositoryFile, document.Root.ToString());
        }

        private static string ReadCommitHash(string repositoryPath)
        {
            // Based on https://github.com/aspnet/BuildTools/blob/dev/src/Internal.AspNetCore.BuildTools.Tasks/GetGitCommitInfo.cs
            var headFile = Path.Combine(repositoryPath, ".git", "HEAD");
            if (!File.Exists(headFile))
            {
                throw new Exception($"Unable to determine active git branch for {repositoryPath}.");
            }

            var content = File.ReadAllText(headFile).Trim();
            if (content.StartsWith(HeadContentStart))
            {
                return ResolveFromBranch(repositoryPath, content);
            }
            else if (content.Length == CommitShaLength)
            {
                return content;
            }

            throw new Exception($"Unable to determine active git branch. '.git/HEAD' file in unexpected format: '{content}'.");
        }

        private static string ResolveFromBranch(string repositoryPath, string head)
        {
            var branch = head.Substring(HeadContentStart.Length);

            if (string.IsNullOrEmpty(branch))
            {
                throw new Exception("Current branch appears to be empty. Failed to retrieve current branch.");
            }

            var branchFile = Path.Combine(repositoryPath, ".git", "refs", "heads", branch);
            if (!File.Exists(branchFile))
            {
                throw new Exception("Unable to determine current git commit hash");
            }

            return File.ReadAllText(branchFile).Trim();
        }
    }
}