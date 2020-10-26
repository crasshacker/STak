using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.Loader;
using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using NLog;
using STak.TakEngine.AI;

namespace STak.TakEngine
{
    public static class PluginLoader<T>
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();


        public static IList<T> LoadPlugins(string pathName)
        {
            List<T>      plugins     = new List<T>();
            List<string> binaryPaths = new List<string>();
            List<string> sourcePaths = new List<string>();
            List<string> ignorePaths = new List<string>();

            if (File.Exists(pathName))
            {
                if (Path.GetExtension(pathName) == ".dll")
                {
                    binaryPaths.Add(pathName);
                }
                else if (Path.GetExtension(pathName) == ".cs")
                {
                    sourcePaths.Add(pathName);
                }
            }
            else if (Directory.Exists(pathName))
            {
                binaryPaths.AddRange(Directory.GetFiles(pathName, "*.dll"));
                sourcePaths.AddRange(Directory.GetFiles(pathName, "*.cs"));
            }

            foreach (var sourcePath in sourcePaths)
            {
                // See if we already have an assembly for this source file.
                var binaryPath = binaryPaths.Where(p => Path.ChangeExtension(p, ".cs") == sourcePath)
                                                                                   .SingleOrDefault();

                // If the assembly exists and it's older than the source file, we'll recompile the source.
                if (binaryPath != null)
                {
                    if (File.GetLastWriteTime(binaryPath) < File.GetLastWriteTime(sourcePath))
                    {
                        s_logger.Debug($"Ignoring plugin {Path.GetFileName(binaryPath)}; source will be recompiled.");
                        binaryPaths.Remove(binaryPath);
                    }
                    else
                    {
                        s_logger.Debug($"Ignoring plugin source {Path.GetFileName(binaryPath)}; DLL is up to date.");
                        ignorePaths.Add(sourcePath);
                    }
                }
            }

            sourcePaths = sourcePaths.Where(p => ! ignorePaths.Contains(p)).ToList();

            foreach (var pathToProcess in binaryPaths)
            {
                plugins.AddRange(LoadPluginsFromAssembly(pathToProcess));
            }
            foreach (var pathToProcess in sourcePaths)
            {
                plugins.AddRange(LoadPluginsFromSourceCode(pathToProcess));
            }

            return plugins;
        }


        private static IList<T> LoadPluginsFromAssembly(string pathName)
        {
            List<string> dllFileNames = new List<string>();

            if (File.Exists(pathName))
            {
                dllFileNames.Add(pathName);
            }
            else if (Directory.Exists(pathName))
            {
                dllFileNames.AddRange(Directory.GetFiles(pathName, "*.dll"));
            }

            List<Assembly> assemblies = new List<Assembly>();

            foreach(string dllFileName in dllFileNames)
            {
                try
                {
                    assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(dllFileName));
                }
                catch (Exception ex)
                {
                    s_logger.Error($"Failed to load plugin {Path.GetFileName(dllFileName)}: {ex.Message}");
                }
            }

            return LoadPluginsFromAssembly(assemblies);
        }


        private static IList<T> LoadPluginsFromSourceCode(string pathName)
        {
            Assembly assembly = Compile(pathName);
            return (assembly != null) ? LoadPluginsFromAssembly(new List<Assembly> { assembly })
                                      : new List<T>();
        }


        private static Assembly Compile(string pathName)
        {
            Assembly assembly = null;

            string codeText = File.ReadAllText(pathName);
            string baseName = Path.GetFileNameWithoutExtension(pathName);

            try
            {
                var compilerOptions    = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var syntaxTrees        = CSharpSyntaxTree.ParseText(codeText);
                var metadataReferences = GetMetadataReferences();

                var compilation = CSharpCompilation.Create(baseName).WithOptions(compilerOptions)
                                                                    .AddReferences(metadataReferences)
                                                                    .AddSyntaxTrees(syntaxTrees);

                s_logger.Info($"Compiling plugin source code \"{pathName}\".");
                using var memoryStream = new MemoryStream();
                var emitResults = compilation.Emit(memoryStream);

                foreach (var diagnostic in emitResults.Diagnostics)
                {
                    var logLevel = diagnostic.Severity == DiagnosticSeverity.Error
                                || diagnostic.IsWarningAsError                        ? LogLevel.Error
                                 : diagnostic.Severity == DiagnosticSeverity.Warning  ? LogLevel.Warn
                                                                                      : LogLevel.Info;
                    s_logger.Log(logLevel, $"Compilation of {baseName} plugin produced: {diagnostic}");
                }

                if (emitResults.Success)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    assembly = AssemblyLoadContext.Default.LoadFromStream(memoryStream);

                    // Cache the assembly on disk if possible, so we won't need to recompile until source is modified.
                    var assemblyName = Path.ChangeExtension(pathName, "dll");
                    try
                    {
                        s_logger.Info("Compilation successful; writing assembly to disk to avoid future compiles.");
                        var fileStream = new FileStream(assemblyName, FileMode.Create, FileAccess.Write);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.WriteTo(fileStream);
                    }
                    catch (Exception)
                    {
                        s_logger.Warn($"Failed to write assembly {assemblyName} to disk.");
                    }
                }
            }
            catch (Exception ex)
            {
                s_logger.Error($"Caught exception while compiling plugin: {ex.Message}");
            }

            return assembly;
        }


        private static MetadataReference[] GetMetadataReferences()
        {
            List<MetadataReference> refs = new List<MetadataReference>();

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));

            refs.Add(MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(List<>).GetTypeInfo().Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(Logger).GetTypeInfo().Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(typeof(PluginLoader<>).GetTypeInfo().Assembly.Location));

            return refs.ToArray();
        }


        private static IList<T> LoadPluginsFromAssembly(IEnumerable<Assembly> assemblies)
        {
            List<T> plugins = new List<T>();

            try
            {
                foreach (var assembly in assemblies)
                {
                    foreach (Type type in assembly.GetExportedTypes().Where(t => t.IsClass && ! t.IsAbstract))
                    {
                        if (type.GetInterface(typeof(T).FullName) != null)
                        {
                            s_logger.Debug($"Activating plugin of type {type.Name}.");
                            plugins.Add((T)Activator.CreateInstance(type));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                s_logger.Error($"Failed to load plugin: {ex.Message}");
            }

            return plugins;
        }
    }
}
