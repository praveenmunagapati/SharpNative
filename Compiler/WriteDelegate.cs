// /*
//   SharpNative - C# to D Transpiler
//   (C) 2014 Irio Systems 
// */

#region Imports

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#endregion

namespace SharpNative.Compiler
{
    internal static class WriteDelegate
    {
        /*
template Action(T) {
	alias void delegate(T obj) Action;
}

		*/


        public static void Go(OutputWriter outputWriter = null)
        {
            var partials = Context.Instance.DelegatePartials;
            var first = partials.First();
            Context.Instance.Namespace = first.Symbol.ContainingNamespace.FullName();
            Context.Instance.Type = first.Symbol;
            TypeProcessor.ClearUsedTypes();
            var mynamespace = Context.Instance.Type.ContainingNamespace.FullName().RemoveFromEndOfString(".Namespace");
                // + "." + TypeState.Instance.TypeName;

            var myUsingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(mynamespace));
            var SystemUsingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System"));
                // Required as certain functions like boxing are in this namespace
            Context.Instance.UsingDeclarations =
                first.Syntax.Parent.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray()
                    .Union(new[]
                    {
                        myUsingDirective, SystemUsingDirective
                    }).ToArray();
            OutputWriter writer = null;

            using (writer = outputWriter == null ? new OutputWriter(Context.Instance.Namespace, Context.Instance.TypeName) : new TempWriter())
            {
                if (outputWriter != null)
                {
                    writer.WriteLine();
                    writer.Indent = outputWriter.Indent + 2;
                    writer.WriteIndent();
                }

                var objectType =
                    TypeProcessor.GetSemanticModel(partials.First().Syntax)
                        .Compilation.GetTypeByMetadataName("System.Object");

                WriteBcl.Go(writer);

                var @namespace = first.Symbol.ContainingNamespace.FullName();

                WriteStandardIncludes.Go(writer);

                //Look for generic arguments 

                //Look for generic arguments 
                var genericArgs = partials.Select(o => o.Syntax)
                    .Where(o => o.TypeParameterList != null)
                    .SelectMany(o => o.TypeParameterList.Parameters)
                    .ToList();

                var name = Context.Instance.TypeName;

                if (genericArgs.Count > 0)
                {
                    name = "template " + name;
                    name += ("( ");
                    name += (string.Join(" , ", genericArgs.Select(o => o)));
                    name += (" )");

                    writer.Write(name);

                    writer.Write("\r\n");

                    writer.OpenBrace();
                    writer.Indent++;

                    writer.Write("alias Delegate!(" + TypeProcessor.ConvertType(first.Syntax.ReturnType) + " delegate" +
                                 WriteMethod.GetParameterListAsString(first.Syntax.ParameterList));

                    writer.Write(") " + WriteType.TypeName(Context.Instance.Type, false) + ";");

                    writer.Indent--;
                    writer.Write("\r\n");
                    writer.CloseBrace();
                }
                else
                {
                    //Non-generic
                    writer.Write("alias Delegate!(" + TypeProcessor.ConvertType(first.Syntax.ReturnType) + " delegate" +
                                 WriteMethod.GetParameterListAsString(first.Syntax.ParameterList));

                    writer.Write(") " + WriteType.TypeName(Context.Instance.Type, false) + ";");
                }

                if (outputWriter != null)
                {
                    outputWriter.WriteLine(writer.ToString());
                }
            }
        }
    }
}