#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Imaging.Interop;

using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

class ProjectFileService {

    private readonly ProjectExplorerPackage _package;

    static readonly Logger Logger = Logger.Create<ProjectFileService>();

    public ProjectFileService(ProjectExplorerPackage package) {
        _package = package;

    }

    public Task<List<ProjectFile>> GetProjectFilesAsync(string path, CancellationToken cancellationToken) {

        var task = Task.Run(async () => {

            var files = SearchProjectFiles(path, cancellationToken);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var imageService = await _package.GetServiceAsync(typeof(SVsImageService)) as IVsImageService2;
            var fileMonikers = new Dictionary<string, ImageMoniker>();

            var projectFiles = new List<ProjectFile>();
            foreach (var file in files) {

                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(file);
                if (!fileMonikers.TryGetValue(extension, out var moniker)) {
                    moniker                 = imageService?.GetImageMonikerForFile(file) ?? KnownMonikers.AddDocument;
                    fileMonikers[extension] = moniker;
                }

                var project = ProjectFile.FromFile(file, moniker);
                projectFiles.Add(project);
            }

            return projectFiles;
        }, cancellationToken);

        return task;
    }

    List<string> SearchProjectFiles(string path, CancellationToken cancellationToken) {

        const string projSuffix = "proj";
        var extensions = SupportedProjectExtensions().Where(ext=> ext.EndsWith(projSuffix))
                                                     .ToArray();

        var projectFiles = new List<string>();

        if (String.IsNullOrWhiteSpace(path)) {
            Logger.Warn($"{nameof(GetProjectFilesAsync)}: path is null or empty");
            return projectFiles;
        }

        // Kniff - alle candidaten enden mit "proj"...
        foreach (var file in Directory.EnumerateFiles(path, $"*.*{projSuffix}", SearchOption.AllDirectories)) {

            cancellationToken.ThrowIfCancellationRequested();

            if (!extensions.Any(p => file.EndsWith(p))) {
                continue;
            }

            projectFiles.Add(file);
        }

        return projectFiles;
    }

    public ImmutableList<string> SupportedProjectExtensions() {

        return EnumeratePossibleProjectExtensions().Distinct()
                                                   .ToImmutableList();

        IEnumerable<string> EnumeratePossibleProjectExtensions() {

            var projects = _package.ApplicationRegistryRoot.OpenSubKey("Projects");
            if (projects == null) {
                yield break;
            }

            foreach (var projectKeyName in projects.GetSubKeyNames()) {
                var project = projects.OpenSubKey(projectKeyName);
                if (project == null) {
                    continue;
                }

                var possibleProjectExtensions = project.GetValue("PossibleProjectExtensions") as string;
                if (possibleProjectExtensions.IsNullOrEmpty()) {
                    continue;
                }

                foreach (var ext in possibleProjectExtensions.Split(';')) {
                    yield return ext;
                }

            }
        }
    }

}