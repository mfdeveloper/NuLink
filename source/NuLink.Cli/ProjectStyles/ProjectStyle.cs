using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Buildalyzer;

namespace NuLink.Cli.ProjectStyles
{
    public abstract class ProjectStyle
    {
        public const string MsbuildNamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";
        
        protected ProjectStyle(IUserInterface ui, ProjectAnalyzer project, XElement projectXml, NuLinkCommandOptions options = null)
        {
            this.UI = ui;
            this.Project = project;
            this.ProjectXml = projectXml;
            this.CommandOptions = options;
        }

        public abstract IEnumerable<PackageReferenceInfo> LoadPackageReferences();

        protected IUserInterface UI { get; }

        protected ProjectAnalyzer Project { get; }

        protected XElement ProjectXml { get; }

        protected NuLinkCommandOptions CommandOptions { get; }

        public static ProjectStyle Create(IUserInterface ui, ProjectAnalyzer project, NuLinkCommandOptions options = null)
        {
            var projectXml = XElement.Load(project.ProjectFile.Path);

            if (PaketProjectStyle.IsStyle(project))
            {
                return new PaketProjectStyle(ui, project, projectXml, options);
            }

            var isSdkStyle = SdkProjectStyle.IsSdkStyleProject(projectXml);
            var isOldStyle = OldProjectStyle.IsOldStyleProject(projectXml);

            if (isSdkStyle && !isOldStyle)
            {
                return new SdkProjectStyle(ui, project, projectXml);
            }

            if (isOldStyle && !isSdkStyle)
            {
                return new OldProjectStyle(ui, project, projectXml);
            }
                
            throw new Exception($"Error: could not recognize project style: {project.ProjectFile.Path}");
        }
    }
}