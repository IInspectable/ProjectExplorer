#region Using Directives

using System;
using System.IO;

#endregion

namespace IInspectable.Utilities.IO; 

public static class PathHelper {

    /// <summary>
    /// Creates a relative path from one file or folder to another.
    /// </summary>
    /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
    /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
    /// <returns>The relative path from the start directory to the end path.</returns>
    /// <exception cref="System.InvalidOperationException"><paramref name="fromPath"/> or <paramref name="fromPath"/> is <c>null</c>.</exception>
    /// <exception cref="System.UriFormatException"></exception>
    /// <exception cref="System.UriFormatException"></exception>
    public static string GetRelativePath(string fromPath, string toPath) {
        if (string.IsNullOrEmpty(fromPath)) {
            return toPath;
        }

        if (string.IsNullOrEmpty(toPath)) {
            throw new ArgumentNullException(nameof(toPath));
        }

        Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
        Uri toUri   = new Uri(AppendDirectorySeparatorChar(toPath));

        if (fromUri.Scheme != toUri.Scheme) {
            return toPath;
        }

        Uri    relativeUri  = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase)) {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    static string AppendDirectorySeparatorChar(string path) {
        // Append a slash only if the path is a directory and does not have a slash.
        if (!Path.HasExtension(path) &&
            !path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
            return path + Path.DirectorySeparatorChar;
        }

        return path;
    }
}