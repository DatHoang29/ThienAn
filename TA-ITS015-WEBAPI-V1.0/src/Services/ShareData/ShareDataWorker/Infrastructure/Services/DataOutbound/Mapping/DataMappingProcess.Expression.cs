using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping
{
    /// <summary>
    /// Bộ phân tích và đánh giá biểu thức $extend.expression trong TargetShapeJson
    /// Created date: 16/09/2026
    /// </summary>
    public static partial class DataMappingProcess
    {
        private const int MaxExpressionLength = 512;
        private const int MaxRecursionDepth = 32;

        /// <summary>
        /// Description: Danh sách hàm được hỗ trợ trong biểu thức tính toán $extend.expression.
        /// Created date: 16/09/2026
        /// </summary>
        private static readonly HashSet<string> SupportedFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "CONCAT", "ISNULL", "COALESCE", "NULLIF", "UPPER", "LOWER", "LEN", "LTRIM", "RTRIM", "ROUND", "ABS"
        };

        /// <summary>
        /// Description: Kiểm tra phát hiện các từ khóa SQL độc hại trong biểu thức $extend.expression.
        /// Created date: 16/09/2026
        /// </summary>
        public static void ValidateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return;
            string upper = expression.ToUpperInvariant();
            if (upper.Contains("DROP ") || upper.Contains("DELETE ") || upper.Contains("UPDATE ") || upper.Contains("INSERT ") || upper.Contains("EXEC ") || upper.Contains("TRUNCATE ") ||
                upper.Contains("EXEC(") || upper.Contains("SELECT ") || upper.Contains(";") || upper.Contains("/*") || upper.Contains("--") || upper.Contains("SYS."))
            {
                throw new InvalidOperationException("Detected potential SQL injection in expression: " + expression);
            }
        }

        /// <summary>
        /// Kiểm tra an toàn tĩnh và cú pháp của biểu thức mà không cần dòng dữ liệu.
        /// Trả về false nếu biểu thức rỗng, quá dài, vi phạm whitelist an toàn hoặc cú pháp không hợp lệ.
        /// </summary>
        public static bool IsStaticallyValid(string? expression, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "Biểu thức không được để trống.";
                return false;
            }

            if (expression.Length > MaxExpressionLength)
            {
                error = $"Biểu thức vượt quá độ dài tối đa cho phép ({MaxExpressionLength} ký tự).";
                return false;
            }

            try
            {
                ValidateExpression(expression);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            try
            {
                var tokens = Tokenize(expression);
                var parser = new Parser(tokens);
                var ast = parser.Parse();
                if (ast == null)
                {
                    error = "Không thể phân tích cú pháp biểu thức.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Đánh giá biểu thức trên một dòng dữ liệu thô.
        /// Trả về false kèm thông điệp lỗi nếu biểu thức không hợp lệ hoặc phát sinh lỗi khi tính toán (lỗi chia 0, kiểu dữ liệu...).
        /// </summary>
        public static bool TryEvaluate(
            string? expression,
            IDictionary<string, object?> row,
            out object? result,
            out string? error)
        {
            result = null;
            error = null;

            if (!IsStaticallyValid(expression, out error))
                return false;

            try
            {
                var tokens = Tokenize(expression!);
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                return ast.TryEvaluate(row, 0, out result, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        #region Tokenizer

        private enum TokenType
        {
            Number,
            String,
            Identifier,
            Plus,
            Minus,
            Multiply,
            Divide,
            Modulo,
            LParen,
            RParen,
            Comma,
            End
        }

        private readonly record struct Token(TokenType Type, string Text, object? Value, int Position);

        /// <summary>
        /// Description: Phân tích cú pháp chuỗi biểu thức thành danh sách token từ vựng.
        /// Created date: 16/09/2026
        /// </summary>
        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            var i = 0;
            var len = text.Length;

            while (i < len)
            {
                var c = text[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(c))
                {
                    var start = i;
                    var hasDot = false;
                    while (i < len && (char.IsDigit(text[i]) || (text[i] == '.' && !hasDot && i + 1 < len && char.IsDigit(text[i + 1]))))
                    {
                        if (text[i] == '.')
                            hasDot = true;
                        i++;
                    }

                    var numStr = text[start..i];
                    if (!decimal.TryParse(numStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var numVal))
                        throw new InvalidOperationException($"Số '{numStr}' tại vị trí {start} không hợp lệ.");

                    tokens.Add(new Token(TokenType.Number, numStr, numVal, start));
                    continue;
                }

                if (c == '\'')
                {
                    var start = i;
                    i++;
                    var sb = new StringBuilder();
                    while (i < len)
                    {
                        if (text[i] == '\'')
                        {
                            if (i + 1 < len && text[i + 1] == '\'')
                            {
                                sb.Append('\'');
                                i += 2;
                            }
                            else
                            {
                                i++;
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(text[i]);
                            i++;
                        }
                    }

                    tokens.Add(new Token(TokenType.String, sb.ToString(), sb.ToString(), start));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    var start = i;
                    while (i < len && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                    }

                    var ident = text[start..i];
                    tokens.Add(new Token(TokenType.Identifier, ident, ident, start));
                    continue;
                }

                switch (c)
                {
                    case '+': tokens.Add(new Token(TokenType.Plus, "+", null, i++)); break;
                    case '-': tokens.Add(new Token(TokenType.Minus, "-", null, i++)); break;
                    case '*': tokens.Add(new Token(TokenType.Multiply, "*", null, i++)); break;
                    case '/': tokens.Add(new Token(TokenType.Divide, "/", null, i++)); break;
                    case '%': tokens.Add(new Token(TokenType.Modulo, "%", null, i++)); break;
                    case '(': tokens.Add(new Token(TokenType.LParen, "(", null, i++)); break;
                    case ')': tokens.Add(new Token(TokenType.RParen, ")", null, i++)); break;
                    case ',': tokens.Add(new Token(TokenType.Comma, ",", null, i++)); break;
                    default:
                        throw new InvalidOperationException($"Ký tự không hợp lệ '{c}' tại vị trí {i}.");
                }
            }

            tokens.Add(new Token(TokenType.End, string.Empty, null, len));
            return tokens;
        }

        #endregion

        #region Parser & AST

        private abstract class ExprNode
        {
            /// <summary>
            /// Author: DatHQ
            /// Description: Tính giá trị của nút biểu thức trên một dòng dữ liệu; lỗi thì trả false kèm câu lỗi.
            /// Created date: 18/09/2026
            /// </summary>
            public abstract bool TryEvaluate(IDictionary<string, object?> row, int depth, out object? result, out string? error);
        }

        private sealed class LiteralNode(object? value) : ExprNode
        {
            /// <summary>
            /// Description: Trả về giá trị hằng số trực tiếp của node Literal.
            /// Created date: 16/09/2026
            /// </summary>
            public override bool TryEvaluate(IDictionary<string, object?> row, int depth, out object? result, out string? error)
            {
                result = value;
                error = null;
                return true;
            }
        }

        private sealed class VariableNode(string name) : ExprNode
        {
            /// <summary>
            /// Description: Đọc giá trị của trường biến từ dòng dữ liệu nguồn.
            /// Created date: 16/09/2026
            /// </summary>
            public override bool TryEvaluate(IDictionary<string, object?> row, int depth, out object? result, out string? error)
            {
                result = null;
                error = null;

                var matchedKey = row.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
                if (matchedKey == null)
                {
                    error = $"Không tìm thấy trường '{name}' trong dòng dữ liệu nguồn.";
                    return false;
                }

                result = UnwrapValue(row[matchedKey]);
                return true;
            }
        }

        private sealed class UnaryOpNode(TokenType op, ExprNode operand) : ExprNode
        {
            /// <summary>
            /// Description: Đánh giá toán tử một ngôi (+ hoặc -) trên toán hạng.
            /// Created date: 16/09/2026
            /// </summary>
            public override bool TryEvaluate(IDictionary<string, object?> row, int depth, out object? result, out string? error)
            {
                result = null;
                if (depth > MaxRecursionDepth)
                {
                    error = "Vượt quá độ sâu đệ quy tối đa của biểu thức.";
                    return false;
                }

                if (!operand.TryEvaluate(row, depth + 1, out var val, out error))
                    return false;

                if (val == null)
                {
                    result = null;
                    return true;
                }

                if (!TryConvertToDecimal(val, out var decVal))
                {
                    error = $"Toán hạng '{val}' không thể chuyển đổi sang số cho phép toán dấu.";
                    return false;
                }

                result = op == TokenType.Minus ? -decVal : decVal;
                return true;
            }
        }

        private sealed class BinaryOpNode(TokenType op, ExprNode left, ExprNode right) : ExprNode
        {
            /// <summary>
            /// Description: Đánh giá toán tử nhị phân (+, -, *, /, %) giữa hai biểu thức vế trái và vế phải.
            /// Created date: 16/09/2026
            /// </summary>
            public override bool TryEvaluate(IDictionary<string, object?> row, int depth, out object? result, out string? error)
            {
                result = null;
                if (depth > MaxRecursionDepth)
                {
                    error = "Vượt quá độ sâu đệ quy tối đa của biểu thức.";
                    return false;
                }

                if (!left.TryEvaluate(row, depth + 1, out var leftVal, out error))
                    return false;
                if (!right.TryEvaluate(row, depth + 1, out var rightVal, out error))
                    return false;

                if (leftVal == null || rightVal == null)
                {
                    result = null;
                    return true;
                }

                if (!TryConvertToDecimal(leftVal, out var leftDec))
                {
                    error = $"Toán hạng bên trái '{leftVal}' không thể chuyển đổi sang số.";
                    return false;
                }

                if (!TryConvertToDecimal(rightVal, out var rightDec))
                {
                    error = $"Toán hạng bên phải '{rightVal}' không thể chuyển đổi sang số.";
                    return false;
                }

                switch (op)
                {
                    case TokenType.Plus:
                        result = leftDec + rightDec;
                        return true;
                    case TokenType.Minus:
                        result = leftDec - rightDec;
                        return true;
                    case TokenType.Multiply:
                        result = leftDec * rightDec;
                        return true;
                    case TokenType.Divide:
                        if (rightDec == 0m)
                        {
                            error = "Phép chia cho 0.";
                            return false;
                        }
                        result = leftDec / rightDec;
                        return true;
                    case TokenType.Modulo:
                        if (rightDec == 0m)
                        {
                            error = "Phép chia lấy dư cho 0.";
                            return false;
                        }
                        result = leftDec % rightDec;
                        return true;
                    default:
                        error = $"Toán tử nhị phân '{op}' không được hỗ trợ.";
                        return false;
                }
            }
        }

        private sealed class FunctionCallNode(string name, List<ExprNode> arguments) : ExprNode
        {
            /// <summary>
            /// Description: Đánh giá kết quả gọi hàm tích hợp (CONCAT, ROUND, UPPER, LEN...) với các tham số đầu vào.
            /// Created date: 16/09/2026
            /// </summary>
            public override bool TryEvaluate(IDictionary<string, object?> row, int depth, out object? result, out string? error)
            {
                result = null;
                error = null;

                if (depth > MaxRecursionDepth)
                {
                    error = "Vượt quá độ sâu đệ quy tối đa.";
                    return false;
                }

                var upperName = name.ToUpperInvariant();
                if (!SupportedFunctions.Contains(upperName))
                {
                    error = $"Hàm '{name}' không được hỗ trợ trong shape expression.";
                    return false;
                }

                var evalArgs = new List<object?>();
                foreach (var arg in arguments)
                {
                    if (!arg.TryEvaluate(row, depth + 1, out var argVal, out error))
                        return false;
                    evalArgs.Add(argVal);
                }

                switch (upperName)
                {
                    case "CONCAT":
                        if (evalArgs.Count == 0)
                        {
                            error = "Hàm CONCAT yêu cầu ít nhất 1 tham số.";
                            return false;
                        }
                        var sb = new StringBuilder();
                        foreach (var a in evalArgs)
                        {
                            if (a != null)
                                sb.Append(FormatScalarForString(a));
                        }
                        result = sb.ToString();
                        return true;

                    case "ISNULL":
                        if (evalArgs.Count != 2)
                        {
                            error = "Hàm ISNULL yêu cầu đúng 2 tham số.";
                            return false;
                        }
                        result = evalArgs[0] ?? evalArgs[1];
                        return true;

                    case "COALESCE":
                        if (evalArgs.Count == 0)
                        {
                            error = "Hàm COALESCE yêu cầu ít nhất 1 tham số.";
                            return false;
                        }
                        result = evalArgs.FirstOrDefault(a => a != null);
                        return true;

                    case "NULLIF":
                        if (evalArgs.Count != 2)
                        {
                            error = "Hàm NULLIF yêu cầu đúng 2 tham số.";
                            return false;
                        }
                        if (AreEqual(evalArgs[0], evalArgs[1]))
                            result = null;
                        else
                            result = evalArgs[0];
                        return true;

                    case "UPPER":
                        if (evalArgs.Count != 1)
                        {
                            error = "Hàm UPPER yêu cầu đúng 1 tham số.";
                            return false;
                        }
                        result = evalArgs[0]?.ToString()?.ToUpperInvariant();
                        return true;

                    case "LOWER":
                        if (evalArgs.Count != 1)
                        {
                            error = "Hàm LOWER yêu cầu đúng 1 tham số.";
                            return false;
                        }
                        result = evalArgs[0]?.ToString()?.ToLowerInvariant();
                        return true;

                    case "LEN":
                        if (evalArgs.Count != 1)
                        {
                            error = "Hàm LEN yêu cầu đúng 1 tham số.";
                            return false;
                        }
                        result = evalArgs[0] == null ? null : (decimal)evalArgs[0]!.ToString()!.Length;
                        return true;

                    case "LTRIM":
                        if (evalArgs.Count != 1)
                        {
                            error = "Hàm LTRIM yêu cầu đúng 1 tham số.";
                            return false;
                        }
                        result = evalArgs[0]?.ToString()?.TrimStart();
                        return true;

                    case "RTRIM":
                        if (evalArgs.Count != 1)
                        {
                            error = "Hàm RTRIM yêu cầu đúng 1 tham số.";
                            return false;
                        }
                        result = evalArgs[0]?.ToString()?.TrimEnd();
                        return true;

                    case "ROUND":
                        if (evalArgs.Count is < 1 or > 2)
                        {
                            error = "Hàm ROUND yêu cầu 1 hoặc 2 tham số.";
                            return false;
                        }
                        if (evalArgs[0] == null)
                        {
                            result = null;
                            return true;
                        }
                        if (!TryConvertToDecimal(evalArgs[0], out var rDec))
                        {
                            error = $"Tham số đầu vào của ROUND '{evalArgs[0]}' không phải là số hợp lệ.";
                            return false;
                        }
                        var decimals = 0;
                        if (evalArgs.Count == 2 && evalArgs[1] != null)
                        {
                            if (!int.TryParse(evalArgs[1]!.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out decimals))
                            {
                                error = "Tham số độ chính xác của ROUND phải là số nguyên.";
                                return false;
                            }
                        }
                        result = Math.Round(rDec, Math.Clamp(decimals, 0, 10), MidpointRounding.AwayFromZero);
                        return true;

                    case "ABS":
                        if (evalArgs.Count != 1)
                        {
                            error = "Hàm ABS yêu cầu đúng 1 tham số.";
                            return false;
                        }
                        if (evalArgs[0] == null)
                        {
                            result = null;
                            return true;
                        }
                        if (!TryConvertToDecimal(evalArgs[0], out var aDec))
                        {
                            error = $"Tham số của ABS '{evalArgs[0]}' không phải là số hợp lệ.";
                            return false;
                        }
                        result = Math.Abs(aDec);
                        return true;

                    default:
                        error = $"Hàm '{name}' không được hỗ trợ.";
                        return false;
                }
            }
        }

        /// <summary>
        /// Description: Bộ phân tích cú pháp theo kỹ thuật đệ quy giảm dần (Recursive Descent Parser).
        /// Created date: 16/09/2026
        /// </summary>
        private class Parser(List<Token> tokens)
        {
            private int _pos;
            private int _depth;

            private Token Current => _pos < tokens.Count ? tokens[_pos] : tokens[^1];

            /// <summary>
            /// Description: Tiêu thụ token hiện tại và dịch chuyển vị trí con trỏ sang token kế tiếp.
            /// Created date: 16/09/2026
            /// </summary>
            private Token Advance()
            {
                var tok = Current;
                if (_pos < tokens.Count - 1)
                    _pos++;
                return tok;
            }

            /// <summary>
            /// Description: Kiểm tra nếu token hiện tại khớp kiểu chỉ định thì dịch chuyển và trả về true.
            /// Created date: 16/09/2026
            /// </summary>
            private bool Match(TokenType type)
            {
                if (Current.Type == type)
                {
                    Advance();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Description: Phân tích toàn bộ chuỗi token thành cây cú pháp trừu tượng AST (ExprNode).
            /// Created date: 16/09/2026
            /// </summary>
            public ExprNode Parse()
            {
                var expr = ParseExpr();
                if (Current.Type != TokenType.End)
                    throw new InvalidOperationException($"Ký tự không mong đợi '{Current.Text}' tại vị trí {Current.Position}.");
                return expr;
            }

            /// <summary>
            /// Description: Phân tích biểu thức cộng trừ (ưu tiên mức thấp nhất).
            /// Created date: 16/09/2026
            /// </summary>
            private ExprNode ParseExpr()
            {
                if (_depth > MaxRecursionDepth)
                    throw new InvalidOperationException("Biểu thức lồng quá sâu.");

                _depth++;
                try
                {
                    var left = ParseTerm();

                    while (Current.Type is TokenType.Plus or TokenType.Minus)
                    {
                        var op = Advance().Type;
                        var right = ParseTerm();
                        left = new BinaryOpNode(op, left, right);
                    }

                    return left;
                }
                finally
                {
                    _depth--;
                }
            }

            /// <summary>
            /// Description: Phân tích số hạng nhân chia và chia lấy dư (ưu tiên mức trung bình).
            /// Created date: 16/09/2026
            /// </summary>
            private ExprNode ParseTerm()
            {
                var left = ParseUnary();

                while (Current.Type is TokenType.Multiply or TokenType.Divide or TokenType.Modulo)
                {
                    var op = Advance().Type;
                    var right = ParseUnary();
                    left = new BinaryOpNode(op, left, right);
                }

                return left;
            }

            /// <summary>
            /// Description: Phân tích toán tử đơn một ngôi (dấu cộng/trừ đứng trước).
            /// Created date: 16/09/2026
            /// </summary>
            private ExprNode ParseUnary()
            {
                if (Current.Type is TokenType.Plus or TokenType.Minus)
                {
                    var op = Advance().Type;
                    var operand = ParseUnary();
                    return new UnaryOpNode(op, operand);
                }

                return ParsePrimary();
            }

            /// <summary>
            /// Description: Phân tích biểu thức nguyên tố (số, chuỗi, tên biến, hàm gọi hoặc ngoặc tròn).
            /// Created date: 16/09/2026
            /// </summary>
            private ExprNode ParsePrimary()
            {
                var tok = Current;

                if (tok.Type == TokenType.Number)
                {
                    Advance();
                    return new LiteralNode(tok.Value);
                }

                if (tok.Type == TokenType.String)
                {
                    Advance();
                    return new LiteralNode(tok.Value);
                }

                if (tok.Type == TokenType.Identifier)
                {
                    Advance();
                    if (Match(TokenType.LParen))
                    {
                        var args = new List<ExprNode>();
                        if (!Match(TokenType.RParen))
                        {
                            do
                            {
                                args.Add(ParseExpr());
                            } while (Match(TokenType.Comma));

                            if (!Match(TokenType.RParen))
                                throw new InvalidOperationException($"Thiếu dấu đóng ngoặc ')' cho hàm '{tok.Text}'.");
                        }

                        return new FunctionCallNode(tok.Text, args);
                    }

                    return new VariableNode(tok.Text);
                }

                if (Match(TokenType.LParen))
                {
                    var inner = ParseExpr();
                    if (!Match(TokenType.RParen))
                        throw new InvalidOperationException("Thiếu dấu đóng ngoặc ')' trong biểu thức.");
                    return inner;
                }

                throw new InvalidOperationException($"Token không mong đợi '{tok.Text}' tại vị trí {tok.Position}.");
            }
        }

        #endregion

        #region Value Conversion Helpers

        /// <summary>
        /// Description: Mở bọc đối tượng thô (hoặc JsonElement) để lấy kiểu dữ liệu nguyên thuỷ C#.
        /// Created date: 16/09/2026
        /// </summary>
        private static object? UnwrapValue(object? val)
        {
            if (val is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return null;
                    case JsonValueKind.String:
                        return je.GetString();
                    case JsonValueKind.Number:
                        if (je.TryGetDecimal(out var d))
                            return d;
                        if (je.TryGetInt64(out var l))
                            return (decimal)l;
                        return je.GetRawText();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    default:
                        return je.GetRawText();
                }
            }

            return val;
        }

        /// <summary>
        /// Description: Cố gắng chuyển đổi an toàn một giá trị số bất kỳ sang kiểu decimal.
        /// Created date: 16/09/2026
        /// </summary>
        private static bool TryConvertToDecimal(object? val, out decimal result)
        {
            result = 0m;
            if (val == null)
                return false;

            if (val is decimal d)
            {
                result = d;
                return true;
            }

            if (val is int i)
            {
                result = i;
                return true;
            }

            if (val is long l)
            {
                result = l;
                return true;
            }

            if (val is double dbl)
            {
                result = (decimal)dbl;
                return true;
            }

            if (val is float f)
            {
                result = (decimal)f;
                return true;
            }

            return decimal.TryParse(val.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Description: Định dạng giá trị vô hướng sang chuỗi theo CultureInfo.InvariantCulture.
        /// Created date: 16/09/2026
        /// </summary>
        private static string FormatScalarForString(object? val)
        {
            if (val == null)
                return string.Empty;

            if (val is decimal d)
                return d.ToString(CultureInfo.InvariantCulture);

            if (val is double dbl)
                return dbl.ToString(CultureInfo.InvariantCulture);

            if (val is float f)
                return f.ToString(CultureInfo.InvariantCulture);

            return val.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Description: So sánh bằng giữa hai giá trị, ưu tiên so sánh bằng kiểu số (decimal) trước khi so sánh chuỗi.
        /// Created date: 16/09/2026
        /// </summary>
        private static bool AreEqual(object? a, object? b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;

            if (TryConvertToDecimal(a, out var aDec) && TryConvertToDecimal(b, out var bDec))
                return aDec == bDec;

            return string.Equals(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }

        #endregion
    }
}

