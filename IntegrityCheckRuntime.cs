using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

internal static class IntegrityCheckRuntime
{
    internal static void Initialize()
    {
        var assemblyPath = typeof(IntegrityCheckRuntime).Assembly.Location;

        using (var binaryReader = new BinaryReader(new FileStream(assemblyPath, FileMode.Open, FileAccess.Read)))
        {
            byte[] fileContent = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

            // Extract hash and file data
            int hashLength = 32; // Length of the hash in ASCII
            byte[] mainData = fileContent.Take(fileContent.Length - hashLength).ToArray();
            byte[] embeddedHash = fileContent.Skip(fileContent.Length - hashLength).ToArray();

            string calculatedHash = ComputeMD5(mainData);
            string storedHash = Encoding.ASCII.GetString(embeddedHash);

            if (calculatedHash != storedHash)
            {
                ProtectCorruptHex();
            }
        }
    }

    internal static string ComputeMD5(byte[] data)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(data);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }

    internal static void ProtectCorruptHex()
    {
        // Generate code to corrupt the hex of the main application
        string sourceCode = "using System;\n" +
                            "using System.IO;\n" +
                            "\n" +
                            "public class CorruptHex\n" +
                            "{\n" +
                            "    public static void Main()\n" +
                            "    {\n" +
                            "        string path = \"" +
                            typeof(IntegrityCheckRuntime).Assembly.Location.Replace("\\", "\\\\") +
                            "\";\n" +
                            "        try\n" +
                            "        {\n" +
                            "            byte[] randomData = new byte[new FileInfo(path).Length];\n" +
                            "            Random rnd = new Random();\n" +
                            "            for (int i = 0; i < randomData.Length; i++)\n" +
                            "            {\n" +
                            "                randomData[i] = (byte)rnd.Next(48, 122); // Random ASCII characters\n" +
                            "            }\n" +
                            "            File.WriteAllBytes(path, randomData);\n" +
                            "        }\n" +
                            "        catch { }\n" +
                            "        Environment.Exit(0);\n" +
                            "    }\n" +
                            "}\n";

        string outputPath = Path.Combine(Path.GetTempPath(), "CorruptHex.exe");
        CompileAndRun(sourceCode, outputPath);

        // Exit current application immediately
        Environment.Exit(0);
    }

    private static void CompileAndRun(string sourceCode, string outputPath)
    {
        // Create a syntax tree for the provided source code
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Set up compilation options
        var compilation = CSharpCompilation.Create(
            "CorruptHex",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(File).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Environment).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        // Emit the compiled code to a file
        using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        {
            EmitResult result = compilation.Emit(stream);

            if (!result.Success)
            {
                Console.WriteLine("Compilation failed:");
                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.WriteLine(diagnostic.ToString());
                }
                Environment.Exit(1);
            }
        }

        // Execute the generated file
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                }
            };
            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to execute compiled program: " + ex.Message);
        }
    }
}
