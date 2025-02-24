using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public partial class AstVisitorExplainer : AstVisitor2
    {
        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst)
        {
            var args = string.Empty;
            var argsText = " without arguments";
            var objectOrClass = "object";
            var stat = string.Empty;

            if (methodCallAst.Arguments?.Any() == true)
            {
                args = string.Join(", ", methodCallAst.Arguments.Select(args => args.Extent.Text));
            }

            if (methodCallAst.Static)
            {
                objectOrClass = "class";
                stat = "static ";
            }

            if (!string.IsNullOrEmpty(args))
            {
                argsText = $", with arguments '{args}'";
            }

            explanations.Add(
                new Explanation
                {
                    Description = $"Invoke the {stat}method '{methodCallAst.Member}' on {objectOrClass} '{methodCallAst.Expression}'{argsText}.",
                    CommandName = "Method",
                    HelpResult = HelpTableQuery("about_Methods")
                }.AddDefaults(methodCallAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            var objectOrClass = "object";
            var stat = "";

            if (memberExpressionAst.Static)
            {
                objectOrClass = "class";
                stat = "static ";
            }

            explanations.Add(
                new Explanation
                {
                    Description = $"Access the {stat}property '{memberExpressionAst.Member}' on {objectOrClass} '{memberExpressionAst.Expression}'",
                    CommandName = "Property",
                    HelpResult = HelpTableQuery("about_Properties")
                }.AddDefaults(memberExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst)
        {
            // SKIP
            // Throws exception in DevContainer when for example trying:
            // PowerShell code sent: class Person {[int]$age ; Person($a) {$this.age = $a}}; class Child : Person {[string]$School   ;   Child([int]$a, [string]$s ) : base($a) {         $this.School = $s     } }
            // #34
            AstExplainer(baseCtorInvokeMemberExpressionAst);
            return base.VisitBaseCtorInvokeMemberExpression(baseCtorInvokeMemberExpressionAst);
        }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst)
        {
            string description;
            var helpResult = HelpTableQuery("about_classes");
            var attributes = functionMemberAst.Attributes?.Count > 0 ?
                $", with attributes '{string.Join(", ", functionMemberAst.Attributes.Select(m => m.TypeName.Name))}'." :
                ".";

            if (functionMemberAst.IsConstructor)
            {
                var parameterSignature = new StringBuilder();
                foreach (var par in functionMemberAst.Parameters)
                {
                    if (par?.StaticType?.Name != null && par.Name?.VariablePath?.UserPath != null)
                    {
                        parameterSignature
                            .Append(par.StaticType.Name)
                            .Append(' ')
                            .Append(par.Name.VariablePath.UserPath)
                            .Append(", ");
                    }
                }
                if (parameterSignature.Length > 2)
                {
                    parameterSignature.Length -= 2; // Remove last ", "
                }

                var parentType = functionMemberAst.Parent as TypeDefinitionAst;
                var className = parentType?.Name ?? "Unknown";
                var howManyParameters = functionMemberAst.Parameters.Count == 0 ? string.Empty : $"has {functionMemberAst.Parameters.Count} parameters and ";
                description = $"A constructor, a special method, used to set things up within the object. Constructors have the same name as the class. This constructor {howManyParameters}is called when [{className}]::new({parameterSignature}) is used.";
                
                if (helpResult?.DocumentationLink != null)
                {
                    helpResult.DocumentationLink += "#constructor";
                }
            }
            else
            {
                if (helpResult?.DocumentationLink != null)
                {
                    helpResult.DocumentationLink += "#class-methods";
                }

                var modifier = "M";
                modifier = functionMemberAst.IsHidden ? "A hidden m" : modifier;
                modifier = functionMemberAst.IsStatic ? "A static m" : modifier;
                description = $"{modifier}ethod '{functionMemberAst.Name}' that returns type '{functionMemberAst.ReturnType?.TypeName?.FullName ?? "void"}'{attributes}";
            }

            explanations.Add(new Explanation()
            {
                Description = description,
                CommandName = "Method member",
                HelpResult = helpResult,
                TextToHighlight = functionMemberAst.Name
            }.AddDefaults(functionMemberAst, explanations));

            return base.VisitFunctionMember(functionMemberAst);
        }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst)
        {
            HelpEntity? helpResult = null;
            var description = string.Empty;
            var parentType = propertyMemberAst.Parent as TypeDefinitionAst;

            if (parentType?.IsClass == true)
            {
                var attributes = propertyMemberAst.Attributes?.Count > 0 ?
                    $", with attributes '{string.Join(", ", propertyMemberAst.Attributes.Select(p => p.TypeName.Name))}'." :
                    ".";
                description = $"Property '{propertyMemberAst.Name}' of type '{propertyMemberAst.PropertyType?.TypeName?.FullName ?? "unknown"}'{attributes}";
                helpResult = HelpTableQuery("about_classes");
                if (helpResult != null)
                {
                    helpResult.DocumentationLink += "#class-properties";
                }
            }
            else if (parentType?.IsEnum == true)
            {
                description = $"Enum label '{propertyMemberAst.Name}', with value '{propertyMemberAst.InitialValue}'.";
                helpResult = HelpTableQuery("about_enum");
            }

            explanations.Add(new Explanation()
            {
                Description = description,
                CommandName = "Property member",
                HelpResult = helpResult,
                TextToHighlight = propertyMemberAst.Name
            }.AddDefaults(propertyMemberAst, explanations));

            return base.VisitPropertyMember(propertyMemberAst);
        }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            var highlight = string.Empty;
            var about = string.Empty;
            var synopsis = string.Empty;
            var attributes = typeDefinitionAst.Attributes?.Count > 0 ?
                $", with the attributes '{string.Join(", ", typeDefinitionAst.Attributes.Select(a => a.TypeName.Name))}'." :
                ".";

            if (typeDefinitionAst.IsClass)
            {
                highlight = "class";
                about = "about_classes";
                synopsis = $"A class is a blueprint for a type. Create a new instance of this type with [{typeDefinitionAst.Name}]::new().";
            }
            else if (typeDefinitionAst.IsEnum)
            {
                highlight = "enum";
                about = "about_enum";
                synopsis = "Enum is short for enumeration. An enumeration is a distinct type that consists of a set of named labels called the enumerator list.";
            }

            explanations.Add(new Explanation()
            {
                Description = $"Defines a '{highlight}', with the name '{typeDefinitionAst.Name}'{attributes} {synopsis}",
                CommandName = "Type definition",
                HelpResult = HelpTableQuery(about),
                TextToHighlight = highlight
            }.AddDefaults(typeDefinitionAst, explanations));

            return base.VisitTypeDefinition(typeDefinitionAst);
        }
    }
}
