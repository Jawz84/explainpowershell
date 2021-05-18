using System.Management.Automation.Language;

namespace ExplainPowershell.SyntaxAnalyzer
{
    static partial class Helpers {
        public static string TokenExplainer(TokenKind tokenKind)
        {
            var suffix = "";
            switch (tokenKind)
            {
                case TokenKind.Ampersand:
                    suffix = "The invocation operator '&'.";
                    break;
                case TokenKind.And:
                    suffix = "The logical and operator '-and'.";
                    break;
                case TokenKind.AndAnd:
                    suffix = "The (unimplemented) operator '&&'.";
                    break;
                case TokenKind.As:
                    suffix = "The type conversion operator '-as'.";
                    break;
                case TokenKind.Assembly:
                    suffix = "The 'assembly' keyword";
                    break;
                case TokenKind.AtCurly:
                    suffix = "The opening token of a hash expression '@{'.";
                    break;
                case TokenKind.AtParen:
                    suffix = "The opening token of an array expression '@('.";
                    break;
                case TokenKind.Band:
                    suffix = "The bitwise and operator '-band'.";
                    break;
                case TokenKind.Base:
                    suffix = "The 'base' keyword";
                    break;
                case TokenKind.Begin:
                    suffix = "The 'begin' keyword.";
                    break;
                case TokenKind.Bnot:
                    suffix = "The bitwise not operator '-bnot'.";
                    break;
                case TokenKind.Bor:
                    suffix = "The bitwise or operator '-bor'.";
                    break;
                case TokenKind.Break:
                    suffix = "The 'break' keyword.";
                    break;
                case TokenKind.Bxor:
                    suffix = "The bitwise exclusive or operator '-xor'.";
                    break;
                case TokenKind.Catch:
                    suffix = "The 'catch' keyword.";
                    break;
                case TokenKind.Ccontains:
                    suffix = "The case sensitive contains operator '-ccontains'.";
                    break;
                case TokenKind.Ceq:
                    suffix = "The case sensitive equal operator '-ceq'.";
                    break;
                case TokenKind.Cge:
                    suffix = "The case sensitive greater than or equal operator '-cge'.";
                    break;
                case TokenKind.Cgt:
                    suffix = "The case sensitive greater than operator '-cgt'.";
                    break;
                case TokenKind.Cin:
                    suffix = "The case sensitive in operator '-cin'.";
                    break;
                case TokenKind.Class:
                    suffix = "The 'class' keyword.";
                    break;
                case TokenKind.Cle:
                    suffix = "The case sensitive less than or equal operator '-cle'.";
                    break;
                case TokenKind.Clike:
                    suffix = "The case sensitive like operator '-clike'.";
                    break;
                case TokenKind.Clt:
                    suffix = "The case sensitive less than operator '-clt'.";
                    break;
                case TokenKind.Cmatch:
                    suffix = "The case sensitive match operator '-cmatch'.";
                    break;
                case TokenKind.Cne:
                    suffix = "The case sensitive not equal operator '-cne'.";
                    break;
                case TokenKind.Cnotcontains:
                    suffix = "The case sensitive not contains operator '-cnotcontains'.";
                    break;
                case TokenKind.Cnotin:
                    suffix = "The case sensitive not in operator '-notin'.";
                    break;
                case TokenKind.Cnotlike:
                    suffix = "The case sensitive notlike operator '-cnotlike'.";
                    break;
                case TokenKind.Cnotmatch:
                    suffix = "The case sensitive not match operator '-cnotmatch'.";
                    break;
                case TokenKind.Colon:
                    suffix = "The PS class base class and implemented interfaces operator ':'. Also used in base class ctor calls.";
                    break;
                case TokenKind.ColonColon:
                    suffix = "The static member access operator '::'.";
                    break;
                case TokenKind.Comma:
                    suffix = "The unary or binary array operator ','.";
                    break;
                case TokenKind.Command:
                    suffix = "The 'command' keyword";
                    break;
                case TokenKind.Comment:
                    suffix = "A single line comment, or a delimited comment.";
                    break;
                case TokenKind.Configuration:
                    suffix = "The 'configuration' keyword";
                    break;
                case TokenKind.Continue:
                    suffix = "The 'continue' keyword.";
                    break;
                case TokenKind.Creplace:
                    suffix = "The case sensitive replace operator '-creplace'.";
                    break;
                case TokenKind.Csplit:
                    suffix = "The case sensitive split operator '-csplit'.";
                    break;
                case TokenKind.Data:
                    suffix = "The 'data' keyword.";
                    break;
                case TokenKind.Define:
                    suffix = "The (unimplemented) 'define' keyword.";
                    break;
                case TokenKind.Divide:
                    suffix = "The division operator '/'.";
                    break;
                case TokenKind.DivideEquals:
                    suffix = "The division assignment operator '/='.";
                    break;
                case TokenKind.Do:
                    suffix = "The 'do' keyword.";
                    break;
                case TokenKind.DollarParen:
                    suffix = "The opening token of a sub-expression '$('.";
                    break;
                case TokenKind.Dot:
                    suffix = "The instance member access or dot source invocation operator '.'.";
                    break;
                case TokenKind.DotDot:
                    suffix = "The range operator '..'.";
                    break;
                case TokenKind.DynamicKeyword:
                    suffix = "The token kind for dynamic keywords";
                    break;
                case TokenKind.Dynamicparam:
                    suffix = "The 'dynamicparam' keyword.";
                    break;
                case TokenKind.Else:
                    suffix = "The 'else' keyword.";
                    break;
                case TokenKind.ElseIf:
                    suffix = "The 'elseif' keyword.";
                    break;
                case TokenKind.End:
                    suffix = "The 'end' keyword.";
                    break;
                case TokenKind.EndOfInput:
                    suffix = "Marks the end of the input script or file.";
                    break;
                case TokenKind.Enum:
                    suffix = "The 'enum' keyword";
                    break;
                case TokenKind.Equals:
                    suffix = "The assignment operator '='.";
                    break;
                case TokenKind.Exclaim:
                    suffix = "The logical not operator '!'.";
                    break;
                case TokenKind.Exit:
                    suffix = "The 'exit' keyword.";
                    break;
                case TokenKind.Filter:
                    suffix = "The 'filter' keyword.";
                    break;
                case TokenKind.Finally:
                    suffix = "The 'finally' keyword.";
                    break;
                case TokenKind.For:
                    suffix = "The 'for' keyword.";
                    break;
                case TokenKind.Foreach:
                    suffix = "The 'foreach' keyword.";
                    break;
                case TokenKind.Format:
                    suffix = "The string format operator '-f'.";
                    break;
                case TokenKind.From:
                    suffix = "The (unimplemented) 'from' keyword.";
                    break;
                case TokenKind.Function:
                    suffix = "The 'function' keyword.";
                    break;
                case TokenKind.Generic:
                    suffix = "A token that is only valid as a command name, command argument, function name, or configuration name. It may contain characters not allowed in identifiers. Tokens with this kind are always instances of StringLiteralToken or StringExpandableToken if the token contains variable references or subexpressions.";
                    break;
                case TokenKind.HereStringExpandable:
                    suffix = "A double quoted here string literal. Tokens with this kind are always instances of StringExpandableToken. even if there are no nested tokens to expand.";
                    break;
                case TokenKind.HereStringLiteral:
                    suffix = "A single quoted here string literal. Tokens with this kind are always instances of StringLiteralToken.";
                    break;
                case TokenKind.Hidden:
                    suffix = "The 'hidden' keyword";
                    break;
                case TokenKind.Icontains:
                    suffix = "The case insensitive contains operator '-icontains' or '-contains'.";
                    break;
                case TokenKind.Identifier:
                    suffix = "A simple identifier, always begins with a letter or '', and is followed by letters, numbers, or ''.";
                    break;
                case TokenKind.Ieq:
                    suffix = "The case insensitive equal operator '-ieq' or '-eq'.";
                    break;
                case TokenKind.If:
                    suffix = "The 'if' keyword.";
                    break;
                case TokenKind.Ige:
                    suffix = "The case insensitive greater than or equal operator '-ige' or '-ge'.";
                    break;
                case TokenKind.Igt:
                    suffix = "The case insensitive greater than operator '-igt' or '-gt'.";
                    break;
                case TokenKind.Iin:
                    suffix = "The case insensitive in operator '-iin' or '-in'.";
                    break;
                case TokenKind.Ile:
                    suffix = "The case insensitive less than or equal operator '-ile' or '-le'.";
                    break;
                case TokenKind.Ilike:
                    suffix = "The case insensitive like operator '-ilike' or '-like'.";
                    break;
                case TokenKind.Ilt:
                    suffix = "The case insensitive less than operator '-ilt' or '-lt'.";
                    break;
                case TokenKind.Imatch:
                    suffix = "The case insensitive match operator '-imatch' or '-match'.";
                    break;
                case TokenKind.In:
                    suffix = "The 'in' keyword.";
                    break;
                case TokenKind.Ine:
                    suffix = "The case insensitive not equal operator '-ine' or '-ne'.";
                    break;
                case TokenKind.InlineScript:
                    suffix = "The 'InlineScript' keyword";
                    break;
                case TokenKind.Inotcontains:
                    suffix = "The case insensitive notcontains operator '-inotcontains' or '-notcontains'.";
                    break;
                case TokenKind.Inotin:
                    suffix = "The case insensitive notin operator '-inotin' or '-notin'";
                    break;
                case TokenKind.Inotlike:
                    suffix = "The case insensitive not like operator '-inotlike' or '-notlike'.";
                    break;
                case TokenKind.Inotmatch:
                    suffix = "The case insensitive not match operator '-inotmatch' or '-notmatch'.";
                    break;
                case TokenKind.Interface:
                    suffix = "The 'interface' keyword";
                    break;
                case TokenKind.Ireplace:
                    suffix = "The case insensitive replace operator '-ireplace' or '-replace'.";
                    break;
                case TokenKind.Is:
                    suffix = "The type test operator '-is'.";
                    break;
                case TokenKind.IsNot:
                    suffix = "The type test operator '-isnot'.";
                    break;
                case TokenKind.Isplit:
                    suffix = "The case insensitive split operator '-isplit' or '-split'.";
                    break;
                case TokenKind.Join:
                    suffix = "The join operator '-join'.";
                    break;
                case TokenKind.Label:
                    suffix = "A label token - always begins with ':', followed by the label name. Tokens with this kind are always instances of LabelToken.";
                    break;
                case TokenKind.LBracket:
                    suffix = "The opening square brace token '['.";
                    break;
                case TokenKind.LCurly:
                    suffix = "The opening curly brace token '{'.";
                    break;
                case TokenKind.LineContinuation:
                    suffix = "A line continuation (backtick followed by newline).";
                    break;
                case TokenKind.LParen:
                    suffix = "The opening parenthesis token '('.";
                    break;
                case TokenKind.Minus:
                    suffix = "The substraction operator '-'.";
                    break;
                case TokenKind.MinusEquals:
                    suffix = "The subtraction assignment operator '-='.";
                    break;
                case TokenKind.MinusMinus:
                    suffix = "The pre-decrement operator '--'.";
                    break;
                case TokenKind.Module:
                    suffix = "The 'module' keyword";
                    break;
                case TokenKind.Multiply:
                    suffix = "The multiplication operator '*'.";
                    break;
                case TokenKind.MultiplyEquals:
                    suffix = "The multiplcation assignment operator '*='.";
                    break;
                case TokenKind.Namespace:
                    suffix = "The 'namespace' keyword";
                    break;
                case TokenKind.NewLine:
                    suffix = "A newline (one of '\n', '\r', or '\r\n').";
                    break;
                case TokenKind.Not:
                    suffix = "The logical not operator '-not'.";
                    break;
                case TokenKind.Number:
                    suffix = "Any numerical literal token. Tokens with this kind are always instances of NumberToken.";
                    break;
                case TokenKind.Or:
                    suffix = "The logical or operator '-or'.";
                    break;
                case TokenKind.OrOr:
                    suffix = "The (unimplemented) operator '||'.";
                    break;
                case TokenKind.Parallel:
                    suffix = "The 'parallel' keyword.";
                    break;
                case TokenKind.Param:
                    suffix = "The 'param' keyword.";
                    break;
                case TokenKind.Parameter:
                    suffix = "A parameter to a command, always begins with a dash ('-'), followed by the parameter name. Tokens with this kind are always instances of ParameterToken.";
                    break;
                case TokenKind.Pipe:
                    suffix = "The pipe operator '|'.";
                    break;
                case TokenKind.Plus:
                    suffix = "The addition operator '+'.";
                    break;
                case TokenKind.PlusEquals:
                    suffix = "The addition assignment operator '+='.";
                    break;
                case TokenKind.PlusPlus:
                    suffix = "The pre-increment operator '++'.";
                    break;
                case TokenKind.PostfixMinusMinus:
                    suffix = "The post-decrement operator '--'.";
                    break;
                case TokenKind.PostfixPlusPlus:
                    suffix = "The post-increment operator '++'.";
                    break;
                case TokenKind.Private:
                    suffix = "The 'private' keyword";
                    break;
                case TokenKind.Process:
                    suffix = "The 'process' keyword.";
                    break;
                case TokenKind.Public:
                    suffix = "The 'public' keyword";
                    break;
                case TokenKind.QuestionDot:
                    suffix = "";
                    break;
                case TokenKind.QuestionLBracket:
                    suffix = "";
                    break;
                case TokenKind.QuestionMark:
                    suffix = "";
                    break;
                case TokenKind.QuestionQuestion:
                    suffix = "";
                    break;
                case TokenKind.QuestionQuestionEquals:
                    suffix = "";
                    break;
                case TokenKind.RBracket:
                    suffix = "The closing square brace token ']'.";
                    break;
                case TokenKind.RCurly:
                    suffix = "The closing curly brace token '}'.";
                    break;
                case TokenKind.RedirectInStd:
                    suffix = "The (unimplemented) stdin redirection operator '<'.";
                    break;
                case TokenKind.Redirection:
                    suffix = "A redirection operator such as '2>&1' or '>>'.";
                    break;
                case TokenKind.Rem:
                    suffix = "The modulo division (remainder) operator '%'.";
                    break;
                case TokenKind.RemainderEquals:
                    suffix = "The modulo division (remainder) assignment operator '%='.";
                    break;
                case TokenKind.Return:
                    suffix = "The 'return' keyword.";
                    break;
                case TokenKind.RParen:
                    suffix = "The closing parenthesis token ')'.";
                    break;
                case TokenKind.Semi:
                    suffix = "The statement terminator ';'.";
                    break;
                case TokenKind.Sequence:
                    suffix = "The 'sequence' keyword.";
                    break;
                case TokenKind.Shl:
                    suffix = "The shift left operator.";
                    break;
                case TokenKind.Shr:
                    suffix = "The shift right operator.";
                    break;
                case TokenKind.SplattedVariable:
                    suffix = "A splatted variable token, always begins with '@' and followed by the variable name. Tokens with this kind are always instances of VariableToken.";
                    break;
                case TokenKind.Static:
                    suffix = "The 'static' keyword";
                    break;
                case TokenKind.StringExpandable:
                    suffix = "A double quoted string literal. Tokens with this kind are always instances of StringExpandableToken even if there are no nested tokens to expand.";
                    break;
                case TokenKind.StringLiteral:
                    suffix = "A single quoted string literal. Tokens with this kind are always instances of StringLiteralToken.";
                    break;
                case TokenKind.Switch:
                    suffix = "The 'switch' keyword.";
                    break;
                case TokenKind.Throw:
                    suffix = "The 'throw' keyword.";
                    break;
                case TokenKind.Trap:
                    suffix = "The 'trap' keyword.";
                    break;
                case TokenKind.Try:
                    suffix = "The 'try' keyword.";
                    break;
                case TokenKind.Type:
                    suffix = "The 'type' keyword";
                    break;
                case TokenKind.Unknown:
                    suffix = "An unknown token, signifies an error condition.";
                    break;
                case TokenKind.Until:
                    suffix = "The 'until' keyword.";
                    break;
                case TokenKind.Using:
                    suffix = "The (unimplemented) 'using' keyword.";
                    break;
                case TokenKind.Var:
                    suffix = "The (unimplemented) 'var' keyword.";
                    break;
                case TokenKind.Variable:
                    suffix = "A variable token, always begins with '$' and followed by the variable name, possibly enclose in curly braces. Tokens with this kind are always instances of VariableToken.";
                    break;
                case TokenKind.While:
                    suffix = "The 'while' keyword.";
                    break;
                case TokenKind.Workflow:
                    suffix = "The 'workflow' keyword.";
                    break;
                case TokenKind.Xor:
                    suffix = "The logical exclusive or operator '-xor'.";
                    break;
            }
            return suffix;
        }
    }
}
