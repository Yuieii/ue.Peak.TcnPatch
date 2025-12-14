// Copyright (c) 2025 Yuieii.

using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ue.Bundler;

var basePath = "../../../..";
var sourceFilePath = Path.Combine(basePath, "ue.Peak.TcnPatch/Plugin.cs");
var docsBasePath = Path.Combine(basePath, "Docs");
var dllBasePath = Path.Combine(basePath, "ue.Peak.TcnPatch/bin/Debug/netstandard2.1");

if (!File.Exists(Path.Combine(dllBasePath, "ue.Peak.TcnPatch.dll")))
{
    Console.Error.WriteLine("Compile the plugin DLL first!");
    Environment.Exit(-1);
}

using var sourceFile = File.OpenRead(sourceFilePath);
using var textReader = new StreamReader(sourceFile);

var syntaxTree = CSharpSyntaxTree.ParseText(textReader.ReadToEnd());

var targets = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>()
    .Where(x => x.Modifiers.Any(t => t.IsKind(SyntaxKind.ConstKeyword)));

var version = "";
foreach (var target in targets)
{
    var variable = target.Declaration.Variables.First();
    var name = variable.Identifier.Text;
    if (name != "ModVersion") continue;
    
    var value = variable.Initializer!.Value;
    version = value.ToString().Trim('"');
    Console.WriteLine("Version is " + version);
    break;
}

if (string.IsNullOrEmpty(version))
{
    Console.Error.WriteLine("Cannot detect version from source code!");
    Environment.Exit(-1);
}

var manifest = new Manifest
{
    Name = "PeakTcnPatch",
    Description = "PEAK 繁體中文化模組 by悠依",
    VersionNumber = version,
    WebsiteUrl = new Uri("https://github.com/Yuieii/ue.Peak.TcnPatch"),
    Dependencies = [
        new Dependency("BepInEx", "BepInExPack_PEAK", "5.4.75301")
    ]
};

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

options.Converters.Add(new DependencyConverter());

using var modZip = File.Open("Yuieii-PeakTcnPatch.zip", FileMode.Create, FileAccess.Write);
using var archive = new ZipArchive(modZip, ZipArchiveMode.Create);

{
    var manifestEntry = archive.CreateEntry("manifest.json");
    using var manifestJson = manifestEntry.Open();
    JsonSerializer.Serialize(manifestJson, manifest, options);
}

archive.CreateEntryFromFile(Path.Combine(dllBasePath, "ue.Peak.TcnPatch.dll"), "ue.Peak.TcnPatch.dll");
archive.CreateEntryFromFile(Path.Combine(dllBasePath, "ue.Core.dll"), "ue.Core.dll");
archive.CreateEntryFromFile(Path.Combine(docsBasePath, "Icon.png"), "icon.png");

{
    using var fileStream = File.OpenRead(Path.Combine(basePath, "Readme.md"));
    using var reader = new StreamReader(fileStream);
    
    var content = reader.ReadToEnd()
        .Replace("\r", "")
        .Replace("> [!TIP]\n", "")
        .Replace("> [!IMPORTANT]\n", "");

    var readmeEntry = archive.CreateEntry("README.md");
    using var entryStream = readmeEntry.Open();
    using var writer = new StreamWriter(entryStream);
    writer.Write(content);
}

archive.CreateEntryFromFile(Path.Combine(docsBasePath, "Changelog.md"), "CHANGELOG.md");

Console.WriteLine("Created an archive file for Thunderstore.");
