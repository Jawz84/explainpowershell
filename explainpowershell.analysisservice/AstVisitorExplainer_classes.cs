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
            var args = "";
            var argsText = " without arguments";
            var objectOrClass = "object";
            var stat = "";

            if (methodCallAst.Arguments != null)
                args = string.Join(", ", methodCallAst.Arguments.Select(args => args.Extent.Text));

            if (methodCallAst.Static)
            {
                objectOrClass = "class";
                stat = "static ";
            }

            if (args != "")
                argsText = $", with arguments '{args}'";

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

            var attributes = functionMemberAst.Attributes.Count > 0 ?
                $", with attributes '{string.Join(", ", functionMemberAst.Attributes.Select(m => m.TypeName.Name))}'." :
                ".";

            if (functionMemberAst.IsConstructor)
            {
                StringBuilder parameterSignature = new();
                foreach (var par in functionMemberAst.Parameters)
                {
                    parameterSignature
                        .Append(par.StaticType.Name)
                        .Append(' ')
                        .Append(par.Name.VariablePath.UserPath)
                        .Append(", ");
                }
                parameterSignature.Remove(parameterSignature.Length - 2, 2);

                var howManyParameters = functionMemberAst.Parameters.Count == 0 ? string.Empty : $"has {functionMemberAst.Parameters.Count} parameters and ";

                description = $"A constructor, a special method, used to set things up within the object. Constructors have the same name as the class. This constructor {howManyParameters}is called when [{(functionMemberAst.Parent as TypeDefinitionAst).Name}]::new({parameterSignature}) is used.";
                helpResult.DocumentationLink += "#constructor";
            }
            else
            {
                helpResult.DocumentationLink += "#class-methods";
                var modifier = "M";
                modifier = functionMemberAst.IsHidden ? "A hidden m" : modifier;
                modifier = functionMemberAst.IsStatic ? "A static m" : modifier;
                description = $"{modifier}ethod '{functionMemberAst.Name}' that returns type '{functionMemberAst.ReturnType.TypeName.FullName}'{attributes}";
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
            HelpEntity helpResult = null;
            var description = "";

            if ((propertyMemberAst.Parent as TypeDefinitionAst).IsClass)
            {
                var attributes = propertyMemberAst.Attributes.Count >= 0 ?
                    $", with attributes '{string.Join(", ", propertyMemberAst.Attributes.Select(p => p.TypeName.Name))}'." :
                    ".";
                description = $"Property '{propertyMemberAst.Name}' of type '{propertyMemberAst.PropertyType.TypeName.FullName}'{attributes}";
                helpResult = HelpTableQuery("about_classes");
                helpResult.DocumentationLink += "#class-properties";
            }

            if ((propertyMemberAst.Parent as TypeDefinitionAst).IsEnum)
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
            var highlight = "";
            var about = "";
            var attributes = ".";
            var synopsis = "";

            if (typeDefinitionAst.Attributes.Count > 0)
            {
                attributes = $", with the attributes '{string.Join(", ", typeDefinitionAst.Attributes.Select(a => a.TypeName.Name))}'.";
            }

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
