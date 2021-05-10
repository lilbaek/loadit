using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Loadit.Analyzer
{
    [Generator]
    public class HttpGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new HttpFinder());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            HttpFinder syntaxReceiver = (HttpFinder) context.SyntaxReceiver!;
            var httpClass = syntaxReceiver.ClassToAugment;
            if (httpClass is null)
            {
                // if we didn't find the user class, there is nothing to do
                return;
            }

            var source = new StringBuilder();
            source.AppendLine(@"using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Exceptions;
using Loadit.Generators;
using loadit.shared.Result;
#nullable enable
namespace Loadit
{
public partial class Http {");

            foreach (var entry in HttpGeneratorDefinitions.Generate)
            {
                foreach (var variation in entry.Variations)
                {
                    var paramSignature = new StringBuilder();
                    var callSignature = new StringBuilder();
                    int count = 0;
                    foreach (var entryParameter in variation)
                    {
                        paramSignature.Append(entryParameter.Value + " " + entryParameter.Key);
                        callSignature.Append(entryParameter.Key);
                        count++;
                        if (count != variation.Count)
                        {
                            paramSignature.Append(", ");
                            callSignature.Append(", ");
                        }
                    }
                    source.AppendLine(@"public async " + entry.ResponseType + @" " + entry.MethodName + @"(" + paramSignature + @")
                    {
                        var localWatch = Stopwatch.StartNew();
                        try
                        {
                            var response = await _httpClient." + entry.MethodName + @"(" + callSignature + @");
                            ProcessResponseResult(localWatch, response);
                            return response;
                        }
                        catch (TaskCanceledException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            HandleException(e);
                            try
                            {
                                _reportingProvider.GetReporter().LogError(e.Message);
                            } 
                            catch
                            {
                                //Ignore
                            }
                            throw new GeneratorCallException(e.Message, e);
                        }
                    }
                ");
                }
            }

            source.AppendLine(@"}}");
            SourceText sourceText = SourceText.From(source.ToString(), Encoding.UTF8);
            context.AddSource("Http.Generated.cs", sourceText);
        }

        class HttpFinder : ISyntaxReceiver
        {
            public ClassDeclarationSyntax? ClassToAugment { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds && cds.Identifier.ValueText == "Http")
                {
                    ClassToAugment = cds;
                }
            }
        }
    }
}