using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Task = System.Threading.Tasks.Task;

namespace Nitra.VisualStudio
{
  class RoslynSymbolReader
  {
    VisualStudioWorkspace _workspace;

    public RoslynSymbolReader(IComponentModel componentModel)
    {
      _workspace = componentModel.GetService<VisualStudioWorkspace>();
      _workspace.WorkspaceChanged += _workspace_WorkspaceChanged;
    }
    
    public async void InitRoslyn()
    {
      try
      {
        await Task.Run(InitRoslynAsync);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Exception: " + ex);
      }
    }

    private async Task InitRoslynAsync()
    {
      try
      {
        foreach (var project in _workspace.CurrentSolution.Projects)
        {
          var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

          var solution = _workspace.CurrentSolution;
          var docs = solution.Projects.SelectMany(p => p.Documents);
          var voidTypeSymbol = compilation.GetTypeByMetadataName("void");

          foreach (var doc in docs)
          {
            bool isTestAttribute(string name)
            {
              switch (name)
              {
                case "TestClassKisExtendedAttribute":
                case "TestClassKisExtended":
                case "TestClassExtensionAttribute":
                case "TestClassExtension":
                  return true;
                default:
                  return false;
              }
            }
            var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
            var testClassDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
              .Where(c => c.AttributeLists.Any(s => s.Attributes.Any(a => isTestAttribute(a.Name.ToString()))))
              .ToArray();

            var methodDecls = testClassDecls.SelectMany(c => c.Members.OfType<MethodDeclarationSyntax>()).ToArray();

            foreach (var methodDecl in methodDecls)
            {
              if (methodDecl.ParameterList.Parameters.Any())
                continue;
              var semanticModel = compilation.GetSemanticModel(methodDecl.SyntaxTree);
              if (semanticModel is IMethodSymbol methodSymbol)
              {
                if (!methodSymbol.ReturnsVoid || methodSymbol.IsGenericMethod || methodSymbol.IsExtensionMethod)
                  continue;

                string makeParentQualifiedName(ISymbol symbol)
                {
                  string loop(ISymbol symbol2, List<string> builder)
                  {
                    builder.Add(symbol2.Name);
                    if (symbol2.ContainingType != null)
                      return loop(symbol2.ContainingType, builder);
                    else if (symbol2.ContainingNamespace != null)
                      return loop(symbol2.ContainingNamespace, builder);

                    builder.Reverse();
                    return string.Join(",", builder);
                  }

                  return loop(symbol, new List<string>());
                }
                Debug.WriteLine($"method: void {makeParentQualifiedName(methodSymbol.ContainingType)}{methodSymbol.Name}();");
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Exception: " + ex);
      }
    }

    private void _workspace_WorkspaceChanged(object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
    {
      Debug.WriteLine($"tr: _workspace_WorkspaceChanged(DocumentId={e.DocumentId} Kind={e.Kind} ProjectId={e.ProjectId} NewSolution={e.NewSolution} OldSolution={e.OldSolution})");
    }

  }
}
