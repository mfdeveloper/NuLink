using System.Collections.Generic;
using System.IO;
using Buildalyzer;
using NuLink.Cli.ProjectStyles;

namespace NuLink.Cli
{
    public class PackageReferenceLoader
    {
        private readonly IUserInterface _ui;
        private readonly NuLinkCommandOptions _commandOptions;

        public PackageReferenceLoader(IUserInterface ui, NuLinkCommandOptions options = null)
        {
            _ui = ui;
            _commandOptions = options;
        }

        public HashSet<PackageReferenceInfo> LoadPackageReferences(IEnumerable<ProjectAnalyzer> projects)
        {
            var results = new HashSet<PackageReferenceInfo>();

            foreach (var project in projects)
            {
                _ui.ReportMedium(() => $"Checking package references: {Path.GetFileName(project.ProjectFile.Path)}");

                var projectStyle = ProjectStyle.Create(_ui, project, _commandOptions);
                var projectPackages = projectStyle.LoadPackageReferences();
                
                results.UnionWith(projectPackages);
            }

            return results;
        }
        
    }
}