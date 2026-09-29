// File-based .NET-Programm (dotnet run --file Repair-PeEncoding.cs -- <zielPfad> [extensions]).
//
// Konvertiert Quelltextdateien auf die Repo-Standardkodierung UTF-8 mit BOM.
// Dateien, die bereits valides UTF-8 mit BOM sind, bleiben unangetastet (idempotent, minimale
// Diffs). Dateien ohne BOM bekommen den BOM ergänzt; UTF-16/UTF-32-Dateien (am BOM erkannt,
// z. B. Out-File aus Windows PowerShell 5) werden entsprechend gelesen; alle übrigen Dateien,
// die kein valides UTF-8 sind, werden als Windows-1252 gelesen und nach UTF-8 umkodiert. Die strikte Dekodierung ist
// entscheidend: ein lossy UTF-8-Read würde Windows-1252-Umlaute unwiederbringlich zu U+FFFD
// (�) zerstören.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

if (args.Length < 1) {
    Console.WriteLine("Usage:");
    Console.WriteLine("  Repair-PeEncoding <zielPfad> [extensions]");
    Console.WriteLine();
    Console.WriteLine("Beispiel:");
    Console.WriteLine("  Repair-PeEncoding C:\\ws\\git\\ProjectExplorer \".cs,.csproj,.md\"");
    return 1;
}

var rootPath = Path.GetFullPath(args[0]);

var extensions = args.Length >= 2
    ? args[1]
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(e => e.StartsWith('.') ? e : "." + e)
        .ToHashSet(StringComparer.OrdinalIgnoreCase)
    : new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        ".cs",
        ".csproj",
        ".props",
        ".targets",
        ".slnx",
        ".sln",
        ".md",
        ".ps1"
    };

if (!Directory.Exists(rootPath)) {
    Console.Error.WriteLine($"Pfad existiert nicht: {rootPath}");
    return 2;
}

// Generierte/fremde Verzeichnisse überspringen — dort liegt kein Repo-Quelltext.
var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
    ".git", ".vs", "bin", "obj", "node_modules", "packages", "deploy", "dist"
};

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var utf8Strict   = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
var utf8Bom      = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
var windows1252  = Encoding.GetEncoding(1252);

var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
    .Where(file => extensions.Contains(Path.GetExtension(file)))
    .Where(file => !Path.GetRelativePath(rootPath, file)
        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Any(segment => excludedDirs.Contains(segment)));

var bomAdded  = 0;
var reEncoded = 0;
var fromUtf16 = 0;
var skipped   = 0;
var failed    = 0;

foreach (var file in files) {
    try {
        var bytes   = File.ReadAllBytes(file);
        var hasBom  = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var payload = hasBom ? bytes.AsSpan(3) : bytes.AsSpan();

        string text;
        string action;
        var unicode = DetectUnicodeBom(bytes);
        if (unicode != null) {
            // UTF-16/32 ist nie valides UTF-8 — ohne diese Prüfung würde es als 1252 zerstört.
            text   = unicode.GetString(bytes, unicode.Preamble.Length, bytes.Length - unicode.Preamble.Length);
            action = $"{unicode.WebName} → UTF-8+BOM";
            fromUtf16++;
        } else {
            try {
                text = utf8Strict.GetString(payload);
                if (hasBom) {
                    // Bereits valides UTF-8 mit BOM → nichts zu tun.
                    skipped++;
                    continue;
                }
                action = "BOM ergänzt";
                bomAdded++;
            } catch (DecoderFallbackException) {
                text   = windows1252.GetString(payload);
                action = "Windows-1252 → UTF-8+BOM";
                reEncoded++;
            }
        }

        File.WriteAllText(file, text, utf8Bom);
        Console.WriteLine($"{action,-24} {Path.GetRelativePath(rootPath, file)}");
    } catch (Exception ex) {
        failed++;
        Console.Error.WriteLine($"Fehlgeschlagen: {file}");
        Console.Error.WriteLine($"  {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Fertig. BOM ergänzt: {bomAdded}, umkodiert (Windows-1252): {reEncoded}, umkodiert (UTF-16/32): {fromUtf16}, bereits OK: {skipped}, fehlgeschlagen: {failed}");

return failed == 0 ? 0 : 3;

// UTF-32 vor UTF-16 prüfen: der UTF-32-LE-BOM (FF FE 00 00) beginnt mit dem UTF-16-LE-BOM.
static Encoding? DetectUnicodeBom(byte[] bytes) {
    var candidates = new Encoding[] {
        new UTF32Encoding(bigEndian: false, byteOrderMark: true),
        new UTF32Encoding(bigEndian: true,  byteOrderMark: true),
        new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
        new UnicodeEncoding(bigEndian: true,  byteOrderMark: true),
    };
    return candidates.FirstOrDefault(e => bytes.AsSpan().StartsWith(e.Preamble));
}
