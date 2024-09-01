using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace ILDiscard.CodeGen
{
    [Flags]
    public enum DiscardMembersOptions
    {
        None,
        DiscardByDefault = 1
    }

    class AssemblyResolver : BaseAssemblyResolver
    {
    }

    public class ILDiscardPostProcessor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly.Name == "ILDiscard.Runtime") return false;
            return compiledAssembly.References.Any(f => Path.GetFileName(f) == "ILDiscard.Runtime.dll");
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
                return new(null);


            var folderList = compiledAssembly.References
                .Select(reference => Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(reference)))
                .Distinct().OrderBy(x => x);

            var loader = new AssemblyResolver();

            foreach (var folder in folderList)
            {
                loader.AddSearchDirectory(folder);
            }

            var readerParameters = new ReaderParameters
            {
                InMemory = true,
                AssemblyResolver = loader,
                ReadSymbols = true,
                ReadingMode = ReadingMode.Deferred
            };

            readerParameters.SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData);

            using var assembly = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);

            var diagnostics = ProcessAssembly(assembly);


            var peStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            var writeParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                WriteSymbols = true,
                SymbolStream = pdbStream
            };

            assembly.Write(peStream, writeParameters);
            peStream.Flush();
            pdbStream.Flush();


            return new ILPostProcessResult(new InMemoryAssembly(peStream.ToArray(), pdbStream.ToArray()), diagnostics);
        }

        List<DiagnosticMessage> ProcessAssembly(AssemblyDefinition assemblyDefinition)
        {
            var mainModule = assemblyDefinition.MainModule;

            List<TypeDefinition> typesToRemove = new List<TypeDefinition>();
            List<DiagnosticMessage> diagnostics = new List<DiagnosticMessage>();
            foreach (var typeDefinition in mainModule.Types)
            {
                ProcessType(typeDefinition, typesToRemove, diagnostics);


                static void ProcessType(TypeDefinition typeDefinition, List<TypeDefinition> typesToRemove, List<DiagnosticMessage> diagnosticMessages)
                {
                    if (typeDefinition.IsSpecialName) return;
                    foreach (var t in typeDefinition.NestedTypes)
                    {
                        ProcessType(t, typesToRemove, diagnosticMessages);
                    }

                    if (!typeDefinition.HasCustomAttributes) return;

                    var hasDiscardAttribute = false;
                    var options = DiscardMembersOptions.None;
                    foreach (var attribute in typeDefinition.CustomAttributes)
                    {
                        var type = attribute.Constructor.DeclaringType;
                        if (type.Namespace != "ILDiscard") continue;
                        if (type.Name == "DiscardAttribute")
                        {
                            typesToRemove.Add(typeDefinition);
                            return;
                        }

                        if (type.Name == "DiscardMembersAttribute")
                        {
                            options = (DiscardMembersOptions)(int)attribute.ConstructorArguments[0].Value;
                            hasDiscardAttribute = true;
                        }
                    }

                    if (!hasDiscardAttribute)
                        return;


                    var optOut = (options & DiscardMembersOptions.DiscardByDefault) != 0;

                    bool ToRemove(Collection<CustomAttribute> customAttributes)
                    {
                        foreach (var attribute in customAttributes)
                        {
                            var type = attribute.AttributeType;
                            if (type.Namespace != "ILDiscard") continue;
                            var name = type.Name;
                            if (name == "DontDiscardAttribute") return false;
                            if (name == "DiscardAttribute") return true;
                        }

                        return optOut;
                    }

                    var toRemove = new List<object>();
                    foreach (var fieldDefinition in typeDefinition.Fields)
                    {
                        if (fieldDefinition.IsSpecialName || fieldDefinition.IsCompilerControlled) continue;
                        if (ToRemove(fieldDefinition.CustomAttributes))
                        {
                            toRemove.Add(fieldDefinition);
                        }
                    }

                    foreach (var f in toRemove)
                    {
                        typeDefinition.Fields.Remove((FieldDefinition)f);
                    }

                    toRemove.Clear();
                    foreach (var propertyDefinition in typeDefinition.Properties)
                    {
                        if (propertyDefinition.IsSpecialName) continue;
                        if (ToRemove(propertyDefinition.CustomAttributes))
                        {
                            toRemove.Add(propertyDefinition);
                        }
                    }

                    foreach (var f in toRemove)
                    {
                        typeDefinition.Properties.Remove((PropertyDefinition)f);
                    }

                    toRemove.Clear();
                    foreach (var methodDefinition in typeDefinition.Methods)
                    {
                        if (methodDefinition.IsSpecialName) continue;
                        if (ToRemove(methodDefinition.CustomAttributes))
                        {
                            toRemove.Add(methodDefinition);
                        }
                    }

                    foreach (var f in toRemove)
                    {
                        typeDefinition.Methods.Remove((MethodDefinition)f);
                    }
                }
            }

            var types = mainModule.Types;
            foreach (var type in typesToRemove)
            {
                types.Remove(type);
            }

            return diagnostics;
        }
    }
}