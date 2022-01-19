using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCAF.Squawks
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class Program
    {
        const string Executable = "dcafsquawks";
        const string ArgHelp1 = "-?";
        const string ArgHelp2 = "-h";
        const string ArgHelp3 = "--help";
        const string ArgOverwrite1 = "-o";
        const string ArgOverwrite2 = "--overwite";
        const string ArgInteractive1 = "-i";
        const string ArgInteractive2 = "--interactive";
        const string ArgWritePath1 = "-w";
        const string ArgWritePath2 = "--write";

        static bool IsHelpRequested { get; set; }

        static bool IsInteractive { get; set; }

        static bool OverwriteExistingOutputFile { get; set; }
        
        static async Task Main(string[] args)
        {
            initializeFromArgs(args);
            if (IsHelpRequested)
            {
                IsInteractive = true;
                endWithMessage("", 1);
                return;
            }
            
            if (!tryGetInputFile(args, out var inputFile))
            {
                IsHelpRequested = true;
                endWithMessage("Expected argument: Path for JSON file to process", 1);
                return;
            }

            var parser = new LotAtcSquawksParser(new Variables(await readVariablesAsync(args)));
            try
            {
                var outputString = await parser.ParseAsync(inputFile!);
                var outputFilePath = getOutputFilePath(inputFile, args);
                await writeOutputFileAsync(outputFilePath, outputString);
            }
            catch (Exception ex)
            {
                endWithMessage(ex.Message, 1);
            }
        }

        static async Task writeOutputFileAsync(string path, string content)
        {
            var file = new FileInfo(path);
            if (file.Exists && !OverwriteExistingOutputFile)
                throw new Exception(
                    $"File already exists: {file.FullName} (set {ArgOverwrite1} or {ArgOverwrite2} parameter to overwrite)");


            await using var stream = new FileStream(file.FullName, FileMode.Truncate);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
        }

        static async Task<Dictionary<string,string>?> readVariablesAsync(IReadOnlyList<string> args)
        {
            if (!tryGetArgsValue(args, out var path, "-v", "--variables"))
                return null;

            var file = new FileInfo(path!);
            if (!file.Exists)
                return null;

            using var stream = file.OpenRead(); 
            try
            {
                return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        static bool tryGetArgsValue(IReadOnlyList<string> args, out string? value, params string[] keys)
        {
            for (var i = 0; i < args.Count-1; i++)
            {
                if (!keys.Any(key => key.Equals(args[i], StringComparison.Ordinal))) 
                    continue;
                
                value = args[i + 1];
                return true;
            }

            value = null;
            return false;
        }

        static bool tryGetInputFile(IReadOnlyList<string> args, out FileInfo? inputFile)
        {
            inputFile = null;
            if (!args.Any())
                return false;

            inputFile = new FileInfo(args[0]);
            return inputFile.Exists;
        }

        static string getOutputFilePath(FileInfo? inputFile, string[] args) // todo consider supporting specifying output file name in args
        {
            if (tryGetArgsValue(args, out var path, ArgWritePath1, ArgWritePath2))
                return path!;
            
            var f = inputFile!; 
            if (string.IsNullOrWhiteSpace(f.Extension))
                return Path.Combine(f.Directory?.FullName ?? ".", $"{f.Name}.out");

            var extension = f.Extension;
            var nameWithoutExtension = f.Name.Remove(f.Name.IndexOf(extension, StringComparison.Ordinal));
            var name = $"{nameWithoutExtension}.out{extension}";
            return Path.Combine(f.Directory?.FullName ?? ".", name);
        }

        static void initializeFromArgs(string[] args)
        {
            IsInteractive = args.Any(i => i is ArgInteractive1 or ArgInteractive2);
            OverwriteExistingOutputFile = args.Any(i => i is ArgOverwrite1 or ArgOverwrite2);
            IsHelpRequested = args.Any(i => i is ArgHelp1 or ArgHelp2 or ArgHelp3);
        }

        static void endWithMessage(string message, int exitCode)
        {
            if (!IsInteractive)
            {
                Environment.Exit(exitCode);
                return;
            }

            if (IsHelpRequested)
            {
                showHelp();
            }
            Console.WriteLine($"{message} (press any key to exit)");
            Console.ReadKey();
            Environment.ExitCode = exitCode;
        }

        static void showHelp()
        {
            Console.WriteLine(
                $"{Executable} <json-template-path>"+
                $" [{ArgWritePath1}|{ArgWritePath2} <LotATC-json-path>]"+
                $" [{ArgOverwrite1}|{ArgOverwrite2}]"+
                $" [{ArgInteractive1}|{ArgInteractive2}]"+
                $" [{ArgHelp1}|{ArgHelp2}|{ArgHelp3}]");

            Console.Write($" [{ArgWritePath1}|{ArgWritePath2} <LotATC-json-path>]");
            Console.WriteLine( " = Specifies the path to the output file (to be used with LotATC)");
            
            Console.Write($" [{ArgOverwrite1}|{ArgOverwrite2}]");
            Console.WriteLine( " = Allows replacing/overwriting the LotATC output file");
            
            Console.Write($" [{ArgInteractive1}|{ArgInteractive2}]");
            Console.WriteLine( " = Outputs information about the outcome and waits for user to confirm");
            
            Console.Write($" [{ArgHelp1}|{ArgHelp2}|{ArgHelp3}]");
            Console.WriteLine( " = Presents this help");
        }
    }
}