using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Buildalyzer;
using ByteDev.Nuget.Nuspec;

namespace NuLink.Cli.ProjectStyles
{
    public class PaketProjectStyle : ProjectStyle
    {
        public const string DEPENDENCIES_FILE = "paket.dependencies";

        protected static string depedenciesFilePath = "";
        protected (string basePath, string idPath, string rootPath) packageTargetFolders;


        public (string basePath, string idPath, string rootPath) PackageTargetFolders {
            
            get 
            {

                if (string.IsNullOrWhiteSpace(packageTargetFolders.basePath))
                {

                    string basePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create),
                        ".nuget",
                        "packages"
                    );

                    if (string.IsNullOrWhiteSpace(packageTargetFolders.idPath) && string.IsNullOrWhiteSpace(packageTargetFolders.rootPath))
                    {
                        string packageIdPath = Path.Combine(basePath, CommandOptions.PackageId.ToLower());

                        CommandOptions.Version = !string.IsNullOrWhiteSpace(CommandOptions.Version) ? CommandOptions.Version : Directory.GetDirectories(packageIdPath).Last();
                        string packageVersionPath = Path.Combine(packageIdPath, CommandOptions.Version);

                        string rootPath = Path.Combine(
                            packageIdPath,
                            packageVersionPath
                        );

                        return (basePath, packageIdPath, rootPath);
                    }
                }

                return packageTargetFolders;
            }
        }


        public PaketProjectStyle(
            IUserInterface ui, ProjectAnalyzer project, XElement projectXml, NuLinkCommandOptions options = null)
            : base(ui, project, projectXml, options)
        {
            packageTargetFolders = PackageTargetFolders;

        }

        public static bool IsStyle(ProjectAnalyzer project, XElement projectXml = null)
        {
            depedenciesFilePath = Path.Combine(
                Path.GetDirectoryName(project.SolutionDirectory),
                DEPENDENCIES_FILE
            );

            return File.Exists(depedenciesFilePath);

            //<None Include="paket.references" />
            //IEnumerable<XElement> paketImport = projectXml.XPathSelectElements("//Import"); //projectXml.DescendantsAndSelf("Import");
            //var navigator = projectXml.CreateNavigator();
            //var paketImport = navigator.XPath2Evaluate("/Project/Import/@Project");


            //if (paketImport == null)
            //{
            //    paketImport = projectXml.XPath2SelectElements("//Import[contains(@Project, 'paket.targets')]");
            //    //var el = xpath.Cast<XElement>();
            //}

            //return paketImport != null;
        }

        public override IEnumerable<PackageReferenceInfo> LoadPackageReferences()
        {
            string data = File.ReadAllText(depedenciesFilePath);

            var dependencies = Regex.Split(data, "nuget\\s|git\\s", RegexOptions.IgnoreCase)
                                    .Where(dep => !Regex.IsMatch(dep, "^(nuget|source|git)"))
                                    .Select(DependencyLine);

            if (CommandOptions != null)
            {
                var package = dependencies.FirstOrDefault(p => p.PackageId == CommandOptions.PackageId);
                if (package == null)
                {

                    if (Directory.Exists(PackageTargetFolders.rootPath))
                    {
                        return dependencies.Union(
                                new[] { new PackageReferenceInfo(
                                    CommandOptions.PackageId,
                                    CommandOptions.Version,
                                    rootFolderPath: PackageTargetFolders.rootPath,
                                    libSubfolderPath: "lib"
                                )
                            }
                        );
                    }
                }
            }

            return dependencies;

        }

        private PackageReferenceInfo DependencyLine(string dep)
        {
            var depInfo = dep.Split(" ");

            var packageId = depInfo[0].Trim('\r', '\n');
            string rootFolder = Path.Combine(Path.GetDirectoryName(Project.SolutionDirectory), "packages", packageId);
            string version;
            
            if (depInfo.Length > 1 && Regex.IsMatch(depInfo[1], "\\d{1,}"))
            {
                version = depInfo[1];
            }
            else
            {
                string nuspecFilePath = Path.Combine(rootFolder, $"{packageId}.nuspec");
                var nuspec = NuspecManifest.Load(nuspecFilePath);
                version = nuspec.MetaData.Version;
            }

            return new PackageReferenceInfo(packageId, version, rootFolder, libSubfolderPath: "lib");
        }

    }
}
