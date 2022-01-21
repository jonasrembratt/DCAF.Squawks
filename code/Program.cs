using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        const string ArgValuesPath1 = "-v";
        const string ArgValuesPath2 = "--values";

        static FileInfo? InputFile { get; set; }

        static FileInfo OutputFile { get; set; } = null!;
        
        static string? Message { get; set; }
        
        static Exception? Error { get; set; }
        
        static bool IsHelpRequested { get; set; }

        static bool IsInteractive { get; set; }

        static bool IsOverwriteExistingOutputFile { get; set; }

        static FileInfo? ValuesFile { get; set; }
        
        static async Task Main(string[] args)
        {
            initializeFromArgs(args);
            if (IsHelpRequested || Message is {} || Error is {})
            {
                exitWithMessage(1);
                return;
            }

            try
            {
                var parser = new LotAtcSquawksParser(new Variables(await readValuesAsync()));
                var content = await parser.ParseAsync(InputFile!);
                await writeOutputFileAsync(content);
                Message = "DONE!";
                exitWithMessage(0);
            }
            catch (Exception ex)
            {
                Error = ex;
                exitWithMessage(1);
            }
        }

        static async Task writeOutputFileAsync(string content)
        {
            if (OutputFile.Exists && !IsOverwriteExistingOutputFile)
                throw new Exception(
                    $"File already exists: {OutputFile.FullName} (set {ArgOverwrite1} or {ArgOverwrite2} parameter to overwrite)");

            writeToConsole($"Writes LotATC JSON file: {OutputFile.FullName}");
            var fileMode = OutputFile.Exists ? FileMode.Truncate : FileMode.Create;
            await using var stream = new FileStream(OutputFile.FullName, fileMode);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
        }

        static void writeToConsole(string text, ConsoleColor color = ConsoleColor.Yellow)
        {
            if (!IsInteractive) 
                return;
            
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        static void writeToConsole(Exception exception) => writeToConsole($"ERROR: {exception.Message}", ConsoleColor.Red);

        static async Task<Dictionary<string,string>?> readValuesAsync()
        {
            if (!ValuesFile?.Exists ?? true)
                return null;

            await using var stream = ValuesFile.OpenRead();
            try
            {
                return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream,
                    new JsonSerializerOptions
                    {
                        AllowTrailingCommas = true
                    });
            }
            catch (JsonException ex)
            {
                throw Error = new Exception($"In values JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw Error = ex;
            }
        }

        static bool tryGetArgsValue(IReadOnlyList<string> args, [NotNullWhen(true)] out string? value, params string[] keys)
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

        static bool tryGetInputFile(IReadOnlyList<string> args, [NotNullWhen(true)] out FileInfo? inputFile)
        {
            inputFile = null;
            if (!args.Any())
                return false;

            inputFile = new FileInfo(args[0]);
            return true;
        }

        static string getOutputFilePath(FileInfo? inputFile, IReadOnlyList<string> args)
        {
            if (tryGetArgsValue(args, out var path, ArgWritePath1, ArgWritePath2))
                return path;
            
            var f = inputFile!; 
            if (string.IsNullOrWhiteSpace(f.Extension))
                return Path.Combine(f.Directory?.FullName ?? ".", $"{f.Name}.out");

            var extension = f.Extension;
            var nameWithoutExtension = f.Name.Remove(f.Name.IndexOf(extension, StringComparison.Ordinal));
            var name = $"{nameWithoutExtension}.out{extension}";
            return Path.Combine(f.Directory?.FullName ?? ".", name);
        }

        static void initializeFromArgs(IReadOnlyList<string> args)
        {
            IsInteractive = args.Any(i => i is ArgInteractive1 or ArgInteractive2);
            IsHelpRequested = args.Any(i => i is ArgHelp1 or ArgHelp2 or ArgHelp3);
            IsOverwriteExistingOutputFile = args.Any(i => i is ArgOverwrite1 or ArgOverwrite2);

            if (!tryGetInputFile(args, out var inputFile))
            {
                if (IsHelpRequested)
                    return;
                
                IsHelpRequested = true;
                Error = new Exception("Expected path for input file (LotATC JSON file template)!");
                return;
            }

            if (!inputFile.Exists)
            {
                Error = new FileNotFoundException($"Template file does not exist: {inputFile.FullName}");
                return;
            }
            
            InputFile = inputFile;
            OutputFile = new FileInfo(getOutputFilePath(InputFile, args));
            if (tryGetArgsValue(args, out var path, ArgValuesPath1, ArgValuesPath2))
            {
                ValuesFile = new FileInfo(path);
                if (!ValuesFile.Exists)
                {
                    Error = new FileNotFoundException($"Values file does not exist: {ValuesFile.FullName}");
                    return;
                }
                
                writeToConsole($"Resolves dynamic values from: {path}");
            }
            else
            {
                writeToConsole($"WARNING: No values file specifies; Dynamic values will not be resolved!");
            }
        }

        static void exitWithMessage(int exitCode)
        {
            if (!IsInteractive)
            {
                Environment.Exit(exitCode);
                return;
            }

            var isMessageDone = false;
            if (Error is { })
            {
                writeToConsole(Error);
                writeToConsole($"(press any key to exit)");
                isMessageDone = true;
            }

            if (IsHelpRequested)
            {
                showHelp();
            }

            if (Message is { } && !isMessageDone)
            {
                writeToConsole(Message);
                writeToConsole($"(press any key to exit)");
            }
            Console.ReadKey();
            Environment.ExitCode = exitCode;
        }

        static void showHelp()
        {
            var color = ConsoleColor.Cyan;
            writeToConsole(
                $"{Executable} <input path>"+
                $" [{ArgValuesPath1} | {ArgValuesPath2} <values path>]"+
                $" [{ArgWritePath1} | {ArgWritePath2} <output path>]"+
                $" [{ArgOverwrite1} | {ArgOverwrite2}]"+
                $" [{ArgInteractive1} | {ArgInteractive2}]"+
                $" [{ArgHelp1} | {ArgHelp2} | {ArgHelp3}]", 
                color);

            writeToConsole("<input path> = Specifies path to LotATC JSON template file (to be processed)", color);

            writeToConsole(
                $" [{ArgValuesPath1}|{ArgValuesPath2} <values path>]" + 
                " = Specifies path to a JSON file containing values for variables in input file", 
                color);

            writeToConsole(
                $" [{ArgWritePath1}|{ArgWritePath2} <output path>]"+
                " = Specifies path to the output file (the result of the processed template, to be used with LotATC)",
                color);

            writeToConsole(
                $" [{ArgOverwrite1}|{ArgOverwrite2}]"+
                " = Allows replacing/overwriting the LotATC output file",
                color);
            
            writeToConsole(
                $" [{ArgInteractive1}|{ArgInteractive2}]"+
                " = Outputs information about the outcome and waits for user to confirm (avoid with automation)",
                color);

            writeToConsole(
                $" [{ArgHelp1}|{ArgHelp2}|{ArgHelp3}]"+
                " = Presents this help",
                color);
        }
    }
}