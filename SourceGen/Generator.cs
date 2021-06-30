using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SourceGen
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("GenerateListAccessAttribute", GenerateListAccessAttribute.SourceText));

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif 
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Compare symbols correctly", Justification = "This works")]
        public void Execute(GeneratorExecutionContext context)
        {
            //context.CancellationToken.ThrowIfCancellationRequested();

            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                return;

            // get the added attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName(GenerateListAccessAttribute.AttributeName)!;

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IPropertySymbol> group in receiver.Properties.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, context);
                if(!string.IsNullOrEmpty(classSource))
                    context.AddSource($"{group.Key.Name}_ListAccess.g.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, ISymbol attributeSymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return ""; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();


            StringBuilder methodDefinitions = new StringBuilder();
            List<string> usingNamespaces = new();
            // create methods for each property 
            foreach (IPropertySymbol propertySymbol in properties)
            {
                string usingNamespace = ProcessProperty(methodDefinitions, propertySymbol, attributeSymbol, context);
                if (!string.IsNullOrEmpty(usingNamespace) && usingNamespace != namespaceName)
                    usingNamespaces.Add($"using {usingNamespace};");
            }

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"using System;
using System.Collections.Generic;
");
            foreach (string usingNamespace in usingNamespaces.Distinct())
                source.AppendLine(usingNamespace);

            source.Append($@"
namespace {namespaceName}
{{
    partial class {classSymbol.Name}
    {{
        {methodDefinitions}
    }}
}}
");
            var x = source.ToString();
            return source.ToString();
        }

        private string ProcessProperty(StringBuilder source, IPropertySymbol propertySymbol, ISymbol attributeSymbol, GeneratorExecutionContext context)
        {
            INamedTypeSymbol readOnlyListType = context.Compilation.GetTypeByMetadataName(typeof(IReadOnlyList<>).FullName)!;

            string propertyName = propertySymbol.Name;
            INamedTypeSymbol propertyType = (INamedTypeSymbol)propertySymbol.Type;
            
            bool isReadOnlyList = readOnlyListType.Equals(propertyType.OriginalDefinition, SymbolEqualityComparer.Default);
            bool hasOneGenericArgument = propertyType.TypeArguments.Length == 1;

            if (!isReadOnlyList || !hasOneGenericArgument)
            {
                //TODO: issue a diagnostic
                return "";
            }

            ITypeSymbol entityType = propertyType.TypeArguments[0];
            string entityNamespace = entityType.ContainingNamespace.ToDisplayString();
            string entityFullName = entityType.ToDisplayString();
            string entityTypeNameWithoutNamespace = entityFullName.Split('.').Last();
            string friendlyMoniker = entityTypeNameWithoutNamespace.FirstCharToLower();

            // get the ListAccess attribute from the property, and any associated data
            AttributeData attributeData = propertySymbol.GetAttributes().Single(ad => ad.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant toProtected = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == nameof(GenerateListAccessAttribute.Protected)).Value;
            bool isProtected = !toProtected.IsNull && toProtected.Value!.Equals(true);

            source.Append($@"
        {(isProtected ? "protected" : "private")} void AddTo{propertyName}({entityTypeNameWithoutNamespace} {friendlyMoniker})
        {{
           ((List<{entityTypeNameWithoutNamespace}>){propertyName}).Add({friendlyMoniker});
        }}

        {(isProtected ? "protected" : "private")} void AddRangeTo{propertyName}(IEnumerable<{entityTypeNameWithoutNamespace}> {friendlyMoniker}Enumerable)
        {{
           ((List<{entityTypeNameWithoutNamespace}>){propertyName}).AddRange({friendlyMoniker}Enumerable);
        }}

        {(isProtected ? "protected" : "private")} void RemoveFrom{propertyName}({entityTypeNameWithoutNamespace} {friendlyMoniker})
        {{
           ((List<{entityTypeNameWithoutNamespace}>){propertyName}).Remove({friendlyMoniker});
        }}

        {(isProtected ? "protected" : "private")} void Clear{propertyName}()
        {{
           ((List<{entityTypeNameWithoutNamespace}>){propertyName}).Clear();
        }}
");

            return entityNamespace;
        }

        internal class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IPropertySymbol> Properties { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                SyntaxNode syntaxNode = context.Node;
                SemanticModel semanticModel = context.SemanticModel;

                if (syntaxNode is not PropertyDeclarationSyntax { AttributeLists: { Count: > 0 } } propSyntax) 
                    return;

                var symbol = semanticModel.GetDeclaredSymbol(syntaxNode) as IPropertySymbol;
                if(symbol!.GetAttributes().Any(ad => ad.AttributeClass!.ToDisplayString() == GenerateListAccessAttribute.AttributeName))
                {
                    Properties.Add(symbol);
                }
            }
        }
    }
}