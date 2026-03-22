// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_ComImportInterfaceAnalyzer
{
    [TestMethod]
    public async Task ValidCast_DoesNotWarn()
    {
        const string source = """
            class Test
            {
                void M(object obj)
                {
                    IC c1 = (IC)obj;
                    IC c2 = obj as IC;

                    if (obj is IC)
                    {
                    }

                    if (obj is IC c3)
                    {
                    }

                    if ((object[])obj is [IC c4])
                    {
                    }

                    if ((object[])obj is [IC])
                    {
                    }
                }
            }

            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task InvalidCast_Warns()
    {
        const string source = """
            using System.Runtime.InteropServices;

            class Test
            {
                void M(object obj)
                {
                    IC c1 = {|CSWINRT2009:(IC)obj|};
                    IC c2 = {|CSWINRT2009:obj as IC|};

                    if ({|CSWINRT2009:obj is IC|})
                    {
                    }

                    if ({|CSWINRT2009:obj is IC c3|})
                    {
                    }

                    if ((object[])obj is [{|CSWINRT2009:IC c4|}])
                    {
                    }

                    if ((object[])obj is [{|CSWINRT2009:IC|}])
                    {
                    }
                }
            }

            [Guid("8FA8A526-F93B-4891-97D2-E1CC83D1C463")]
            [ComImport]
            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
