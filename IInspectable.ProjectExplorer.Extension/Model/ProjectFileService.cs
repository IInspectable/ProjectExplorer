#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectFileService {

        static readonly Logger Logger = Logger.Create<ProjectFileService>();

        public Task<List<ProjectFile>> GetProjectFilesAsync(string path, CancellationToken cancellationToken) {

            var task = Task.Run(() => {

                var patterns = new[] {"*.csproj"}; //, "*.vbproj", "*.vcxproj", "*.jsproj", "*.fsproj" };

                var projectFiles = new List<ProjectFile>();

                if (String.IsNullOrWhiteSpace(path)) {
                    Logger.Warn($"{nameof(GetProjectFilesAsync)}: path is null or empty");
                    return projectFiles;
                }

                foreach (var pattern in patterns) {
                    foreach (var file in Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories)) {

                        cancellationToken.ThrowIfCancellationRequested();

                        var projectFile = LoadProjectFile(file);

                        if (projectFile != null) {
                            projectFiles.Add(projectFile);
                        }
                    }
                }

                return projectFiles;
            }, cancellationToken);

            return task;
        }

        [CanBeNull]
        ProjectFile LoadProjectFile(string file) {
            try {
                return ProjectFile.FromFile(file);

            } catch (Exception ex) {
                Logger.Error(ex, $"Fehler beim Laden der Projektdatei '{file}'");
                return null;
            }
        }

    }

}