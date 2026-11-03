// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_InteropEnablement
{
    [TestMethod]
    [DataRow("true", "", "false", "true")]
    [DataRow("true", "true", "true", "true")]
    [DataRow("true", "false", "false", "false")]
    [DataRow("false", "", "false", "false")]
    public async Task SolutionWidePublishAot_PreservesReferenceProjectionAndInteropDefaults(
        string publishAot, string interopOverride, string expectedProjection, string expectedLibrary)
    {
        string directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "CsWinRT-InteropEnablement-" + Guid.NewGuid().ToString("N"))).FullName;

        try
        {
            string targets = Path.GetFullPath(Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "CsWinRTTargetsPath").Value!);
            string beforeSdkTargets = Path.Combine(Path.GetDirectoryName(targets)!, "Microsoft.Windows.CsWinRT.BeforeMicrosoftNetSdk.targets");
            string solutionPath = Path.Combine(directory, "InteropEnablement.slnx");
            string[] names = ["Projection", "Library", "Application", "NativeComponent"];

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                bool isProjection = i == 0;
                bool isComponent = i == 3;
                XElement properties = new("PropertyGroup",
                    new XElement("OutputType", i == 2 ? "Exe" : "Library"),
                    new XElement("AssemblyName", name),
                    new XElement("IntermediateOutputPath", name + Path.DirectorySeparatorChar),
                    new XElement("_TargetPlatformVersionUsesCsWinRT3", "true"),
                    new XElement("CsWinRTGenerateReferenceProjection", isProjection ? "true" : "false"),
                    new XElement("CsWinRTComponent", isComponent ? "true" : "false"),
                    new XElement("CsWinRTBuildForNativeConsumer", isComponent ? "true" : "false"));

                if (interopOverride.Length != 0)
                {
                    properties.Add(new XElement("CsWinRTGenerateInteropAssembly", interopOverride));
                }

                XDocument project = new(new XElement("Project", new XAttribute("DefaultTargets", "Build"), properties,
                    new XElement("Import", new XAttribute("Project", beforeSdkTargets)),
                    new XElement("Import", new XAttribute("Project", targets)),
                    new XElement("Target", new XAttribute("Name", "Build"),
                        new XElement("Error",
                            new XAttribute("Condition", $"'$(PublishAot)' != '{publishAot}'"),
                            new XAttribute("Text", "The solution-wide PublishAot property must reach every project.")),
                        new XElement("Error",
                            new XAttribute("Condition", "'$(CsWinRTGenerateInteropAssembly)' != 'false'"),
                            new XAttribute("Text", "The duplicate SDK interop targets must remain disabled.")),
                        new XElement("WriteLinesToFile",
                            new XAttribute("File", name + ".result"),
                            new XAttribute("Lines", "$(CsWinRTGenerateInteropAssembly2);$(ProduceOnlyReferenceAssembly)"),
                            new XAttribute("Overwrite", "true")))));
                project.Save(Path.Combine(directory, name + ".csproj"));
            }

            new XDocument(new XElement("Solution", names.Select(name =>
                new XElement("Project", new XAttribute("Path", name + ".csproj"))))).Save(solutionPath);

            ProcessStartInfo startInfo = new("dotnet")
            {
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("msbuild");
            startInfo.ArgumentList.Add(solutionPath);
            startInfo.ArgumentList.Add("-nologo");
            startInfo.ArgumentList.Add("-p:PublishAot=" + publishAot);

            using Process process = Process.Start(startInfo)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            Task completion = process.WaitForExitAsync();

            if (await Task.WhenAny(completion, Task.Delay(TimeSpan.FromMinutes(3))) != completion)
            {
                process.Kill(entireProcessTree: true);
                await completion;
                Assert.Fail($"MSBuild timed out.\n{await output}\n{await error}");
            }

            await completion;
            Assert.AreEqual(0, process.ExitCode, $"{await output}\n{await error}");
            CollectionAssert.AreEqual(new[] { expectedProjection, "true" }, File.ReadAllLines(Path.Combine(directory, "Projection.result")));
            CollectionAssert.AreEqual(new[] { expectedLibrary }, File.ReadAllLines(Path.Combine(directory, "Library.result")));
            string expectedExecutable = interopOverride.Length == 0 ? "true" : interopOverride;
            CollectionAssert.AreEqual(new[] { expectedExecutable }, File.ReadAllLines(Path.Combine(directory, "Application.result")));
            CollectionAssert.AreEqual(new[] { expectedExecutable }, File.ReadAllLines(Path.Combine(directory, "NativeComponent.result")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
