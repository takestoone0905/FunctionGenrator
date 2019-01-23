using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionGenerator {
    internal class ExpressionLexer {
        enum LexerState { Constant, Number, Parameter, Counter, Function, BinaryExpression, Expression, SigmaExpression, PiExpression, Undefined, Root };
        //定義済み関数名リスト
        internal static List<string> Functions;

        /// <summary>
        /// 組み込み関数をファイルから読み込む。
        /// </summary>
        static void getPrelude() {
            try {
                Functions = new List<string>();
                using (StreamReader SR = new StreamReader("functions.txt")) {
                    string name = SR.ReadLine();
                    while (!string.IsNullOrEmpty(name)) {
                        Functions.Add(name);
                        name = SR.ReadLine();
                    }
                }
            } catch {
                Console.WriteLine("functions.txtが見つかりませんでした。組み込み関数を読み込んでいます。");
                var Methods = typeof(Math).GetMethods();
                using (StreamWriter SW = new StreamWriter("functions.txt")) {
                    foreach (var item in Methods.Where(x => x.IsStatic)) {
                        SW.WriteLine(item.Name);
                    }
                }
                Functions = new List<string>();
                using (StreamReader SR = new StreamReader("functions.txt")) {
                    string name = SR.ReadLine();
                    while (!string.IsNullOrEmpty(name)) {
                        Functions.Add(name);
                        name = SR.ReadLine();
                    }
                }
            }
        }

        /// <summary>
        /// Nodeツリーを作り、そのRootNodeを返します。
        /// 再帰的に子をたどれば木の全体を再構成可能です。
        /// </summary>
        /// <returns></returns>
        internal static RootNode BuildTree(Function f) {
            try {
                if (Functions == null) {//初回のみ
                    getPrelude();
                }
                RootNode root = new RootNode();
                BuildTree(f, root);
                //SpyTree(root);
                return root;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        static void BuildTree(Function f, Node Parent) {
            LexerState state;
            //今の部分木のRootになる要素を見つける。何が見つかったかはstateに格納されている。
            var Key = SearchRoot(f, out state);
            if (Key == null) {
                throw new Exception("式の解析を中断します。");
            }
            switch (state) {//stateに対応するNodeを生成し、parentと関連付ける。
                case LexerState.Constant:
                    ConstantNode constantNode = new ConstantNode(double.Parse(Key[0]), Parent);
                    if (Parent != null) Parent.Children.Add(constantNode);
                    //                    Console.WriteLine("CONSTATNT:" + Key[0]);
                    break;
                case LexerState.Number:
                    NumberNode numberNode = new NumberNode(double.Parse(Key[0]), Parent);
                    if (Parent != null) Parent.Children.Add(numberNode);
                    //                    Console.WriteLine("NUMBER:" + Key[0]);
                    break;
                case LexerState.Parameter:
                    ParameterNode parameterNode = new ParameterNode(Key[0], Parent);
                    if (Parent != null) Parent.Children.Add(parameterNode);
                    //                    Console.WriteLine("PARAMTER:" + Key[0]);
                    break;
                case LexerState.Counter:
                    CounterNode CounterNode = new CounterNode(Key[0], Parent);
                    if (Parent != null) Parent.Children.Add(CounterNode);
                    //                    Console.WriteLine("COUNTER:" + Key[0]);
                    break;
                case LexerState.BinaryExpression:
                    //左辺　演算子　右辺の順で格納されているはず。
                    BinaryNode binaryNode = new BinaryNode(Key[1], Parent);
                    if (Parent != null) Parent.Children.Add(binaryNode);
                    //Key[i]をBodyとするFunctionオブジェクトを生成（コピーコンストラクタ）                        
                    BuildTree(new Function(f, Key[0]), binaryNode);
                    BuildTree(new Function(f, Key[2]), binaryNode);
                    //                                            Console.WriteLine("BINARY:" + Key[0] + " " + Key[1] + " " + Key[2]);
                    break;
                case LexerState.Expression:
                    ExpressionNode expressionNode = new ExpressionNode(Parent);
                    if (Parent != null) Parent.Children.Add(expressionNode);
                    BuildTree(new Function(f, Key[0]), expressionNode);
                    //                    Console.WriteLine("Expression:" + Key[0]);
                    break;
                case LexerState.Function:
                    FunctionNode functionNode = new FunctionNode(Key[0], Parent);
                    if (Parent != null) Parent.Children.Add(functionNode);
                    foreach (var item in Key.Skip(1)) {
                        //子ノードになる引数についてParseを進める。(Key[0]は関数名なのでSkip)
                        //Key[i]をBodyとするFunctionオブジェクトを生成（コピーコンストラクタ）
                        BuildTree(new Function(f, item), functionNode);
                    }
                    #region for DEBUG
                    /*
                    string message = "FUNCTION:";
                    foreach (var item in Key) {
                        message += item;
                        if (item != Key.Last()) message += ",";
                    }
                    Console.WriteLine(message);
                    */
                    #endregion
                    break;
                case LexerState.SigmaExpression:
                    FunctionNode SumfunctionNode = new FunctionNode(Key[0], Parent);
                    if (Parent != null) Parent.Children.Add(SumfunctionNode);
                    foreach (var item in Key.Skip(1)) {
                        //子ノードになる引数についてParseを進める。(Key[0]は関数名なのでSkip)
                        //Key[i]をBodyとするFunctionオブジェクトを生成（コピーコンストラクタ）
                        BuildTree(new Function(f, item), SumfunctionNode);
                    }
                    break;
                case LexerState.PiExpression:
                    FunctionNode PifunctionNode = new FunctionNode(Key[0], Parent);
                    if (Parent != null) Parent.Children.Add(PifunctionNode);
                    foreach (var item in Key.Skip(1)) {
                        //子ノードになる引数についてParseを進める。(Key[0]は関数名なのでSkip)
                        //Key[i]をBodyとするFunctionオブジェクトを生成（コピーコンストラクタ）
                        BuildTree(new Function(f, item), PifunctionNode);
                    }
                    break;
                case LexerState.Undefined:
                    throw new Exception("解釈できない要素が見つかりました。");

                case LexerState.Root:
                    break;
            }
        }
        /// <summary>
        ///  作ったツリーを世代ごとに表示。
        ///  確認用に。
        /// </summary>
        static void SpyTree(Node root) {//指定のNodeをルートとする部分木を表示
            Queue<Node> nodes = new Queue<Node>();
            nodes.Enqueue(root);
            while (nodes.Count > 0) {
                Queue<Node> next = new Queue<Node>();
                foreach (var item in nodes) {
                    Console.Write(item + " ");
                    foreach (var child in item.Children) {
                        next.Enqueue(child);
                    }
                }
                Console.WriteLine();
                nodes = next;
            }
        }

        /// <summary>
        /// 式を要素に分割する。
        /// 例：左辺、演算子、右辺
        /// 例；関数名前、引数、引数、引数・・・・
        /// 再帰的に潜っていく。
        /// どんな式を発見したのかをLexerStateで知らせる。
        /// /// </summary>
        /// <example> (10*5+x)+(45/5)  =>  <10*5+x> <+> <45/5> </example>
        /// <param name="input"></param>
        /// <returns></returns>
        static string[] SearchRoot(Function function, out LexerState state) {
            string input = function.Body;
            string[] Elements;
            try {
                //括弧の外に入っている+と-を探す。
                Elements = SearchPlusMinus(input);
                if (Elements != null) {
                    state = LexerState.BinaryExpression;
                    return Elements;
                }
                //全部が掛け算割り算でできている。
                Elements = SearchMulDiv(input);
                if (Elements != null) {
                    state = LexerState.BinaryExpression;
                    return Elements;
                }
                //全部が冪算だけである。
                Elements = SearchPow(input);
                if (Elements != null) {
                    state = LexerState.BinaryExpression;
                    return Elements;
                }
                //ノードが1つだけあるような状態。
                //考えられる状態としては、「実数が1つ」「定数が1つ」「変数が1つ」「関数呼び出しが1つ」「括弧式が1つ」
                Elements = SearchNumber(input);
                if (Elements != null) {
                    state = LexerState.Number;
                    return Elements;
                }
                Elements = SearchConstant(input, function.Constants);
                if (Elements != null) {
                    state = LexerState.Constant;
                    return Elements;
                }
                Elements = SearchParameter(input, function.Parameters.ToArray());
                if (Elements != null) {
                    state = LexerState.Parameter;
                    return Elements;
                }
                Elements = SearchCounter(input, function.Counters.ToArray());
                if (Elements != null) {
                    state = LexerState.Counter;
                    return Elements;
                }
                Elements = SearchFunctionCall(input, function.Constants, function.Parameters.ToArray(), function.Counters.ToArray());
                if (Elements != null) {
                    switch (Elements[0]) {//特別な処理を要する関数があるので。
                        case "Sum":
                            state = LexerState.SigmaExpression;
                            return Elements;
                        case "Pi":
                            state = LexerState.PiExpression;
                            return Elements;
                        default:
                            state = LexerState.Function;
                            return Elements;
                    }
                }
                Elements = SearchParenthesis(input);
                if (Elements != null) {
                    state = LexerState.Expression;
                    return Elements;
                }
                state = LexerState.Undefined;
                throw new ExpressionLexerException("解釈不能な式です。" + input);
            } catch (Exception e) {
                state = LexerState.Undefined;
                Console.WriteLine(e.Message);
                return null;
            }
        }
        static string[] SearchPlusMinus(string input) {
            //括弧カウント。
            int pare = 0;
            //括弧の外に入っている+と-を探す。
            for (int i = 0; i < input.Length; i++) {
                switch (input[i]) {
                    case '+':
                    case '-':
                        if (pare == 0) {
                            string left = input.Substring(0, i);
                            string right = input.Substring(i + 1);
                            string op = input[i].ToString();
                            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right) || string.IsNullOrWhiteSpace(left)) {
                                throw new Exception("「" + left + op + right + "」" + "式が不完全です");
                            }
                            return new string[] { left, op, right };
                        } else {
                            break;
                        }
                    case '(':
                    case '[':
                        pare++;
                        break;
                    case ')':
                    case ']':
                        pare--;
                        break;
                    default:
                        break;
                }
            }
            return null;
        }
        static string[] SearchMulDiv(string input) {
            //括弧カウント。
            int pare = 0;
            //括弧の外に入っている*と/を探す。
            for (int i = 0; i < input.Length; i++) {
                switch (input[i]) {
                    case '*':
                    case '/':
                        if (pare == 0) {
                            string left = input.Substring(0, i);
                            string right = input.Substring(i + 1);
                            string op = input[i].ToString();
                            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right) || string.IsNullOrWhiteSpace(left)) {
                                throw new Exception("「" + left + op + right + "」" + "式が不完全です");
                            }
                            return new string[] { left, op, right };
                        } else {
                            break;
                        }
                    case '(':
                    case '[':
                        pare++;
                        break;
                    case ')':
                    case ']':
                        pare--;
                        break;
                    default:
                        break;
                }
            }
            return null;
        }
        static string[] SearchPow(string input) {
            //括弧カウント。
            int pare = 0;
            //括弧の外に入っている^を探す。
            for (int i = 0; i < input.Length; i++) {
                switch (input[i]) {
                    case '^':
                        if (pare == 0) {
                            string left = input.Substring(0, i);
                            string right = input.Substring(i + 1);
                            string op = input[i].ToString();
                            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right) || string.IsNullOrWhiteSpace(left)) {
                                throw new Exception("「" + left + op + right + "」" + "式が不完全です");
                            }
                            return new string[] { left, op, right };
                        } else {
                            break;
                        }
                    case '(':
                    case '[':
                        pare++;
                        break;
                    case ')':
                    case ']':
                        pare--;
                        break;
                    default:
                        break;
                }
            }
            return null;
        }
        /// <summary>
        /// Parse可能ならそれを、だめならnull
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string[] SearchNumber(string input) {
            double val = 0d;
            if (double.TryParse(input, out val)) {
                return new[] { input };
            }
            return null;
        }
        /// <summary>
        /// Functionで定義されている定数であればその値に変換して数字を返す。
        /// </summary>
        /// <param name="input">数式</param>
        /// <param name="constantList">Functionクラスの辞書</param>
        /// <returns></returns>
        static string[] SearchConstant(string input, Dictionary<string, double> constantList) {
            if (constantList.ContainsKey(input)) {
                return new string[] { constantList[input].ToString() };
            }
            return null;
        }
        /// <summary>
        /// 変数を探す。辞書になければnull
        /// </summary>
        /// <param name="input"></param>
        /// <param name="constantList"></param>
        /// <returns></returns>
        static string[] SearchParameter(string input, string[] parameterList) {
            if (parameterList.Contains(input)) {
                return new string[] { input };
            }
            return null;
        }
        /// <summary>
        /// カウンタ変数を探す。辞書になければnull
        /// </summary>
        /// <param name="input"></param>
        /// <param name="constantList"></param>
        /// <returns></returns>
        static string[] SearchCounter(string input, string[] counterList) {
            if (counterList.Contains(input)) {
                return new string[] { input };
            }
            return null;
        }
        /// <summary>
        /// 関数呼び出しを見つけ出す。未定義の定数・変数・関数を発見したなどの場合にはnullを返す。
        /// </summary>
        /// <param name="input"></param>
        /// <param name="constantList"></param>
        /// <returns></returns>
        static string[] SearchFunctionCall(string input, Dictionary<string, double> constantList, string[] parameterList, string[] counterList) {
            //関数が入れ子になって複数の[]が存在するかもしれないので。
            try {
                var from = input.IndexOf('[');
                if (from == -1) {//[]が見つからない
                    return null;
                }
                var name = input.Substring(0, from);
                switch (name) {
                    case "Sum":
                        return SearchSigma(input, constantList, parameterList, counterList);
                    case "Pi":
                        return SearchPi(input, constantList, parameterList, counterList);
                    default:
                        break;
                }
                if (input.Last() != ']') {
                    throw new ExpressionLexerException("関数の括弧が対応していません : " + name);
                }
                var operands = input.Substring(from + 1, input.Length - name.Length - 2);
                if (string.IsNullOrEmpty(operands)) {
                    throw new ExpressionLexerException("関数の引数がありません : " + name);
                }
                var keys = operands.Split(',');
                List<string> elements = new List<string>();
                //組み込み関数またはユーザー定義の関数である。
                if (Functions.Contains(name) || ExpressionCaller.UserDefinedFunctions.ContainsKey(name)) {
                    elements.Add(name);
                } else {
                    throw new Exception("未定義の関数です :" + name);
                }
                for (int i = 0; i < keys.Length; i++) {
                    if (isNumber(keys[i])) {
                        elements.Add(keys[i]);
                    } else if (constantList.ContainsKey(keys[i])) {
                        elements.Add(constantList[keys[i]].ToString());
                    } else if (parameterList.Contains(keys[i])) {
                        elements.Add(keys[i]);
                    } else {//その他(式や関数呼び出し)の場合は次の世代で解析されるので、そのままにしておく。
                        elements.Add(keys[i]);
                    }
                }
                return elements.ToArray();
            } catch (ExpressionLexerException e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        static string[] SearchSigma(string input, Dictionary<string, double> constantList, string[] parameterList, string[] counterList) {
            //Σ[i,0,3i*x]等のΣ式を認識する
            //組み込みの関数SumExpressionを呼び出すための措置。
            var name = "Sum";
            var from = input.IndexOf('[');
            if (from != name.Length) {// Σ[カウンタ、始値、終値、定義]
                throw new ExpressionLexerException("Σ式は \"Sum[カウンタ、始値、終値、定義]\"の形をとる必要があります。input:" + input);
            }
            if (input.Last() != ']') {
                throw new ExpressionLexerException("Σ式の括弧が対応していません :" + input);
            }
            //大括弧内を取り出す。
            var operands = input.Substring(from + 1, input.Length - name.Length - 2);
            if (string.IsNullOrEmpty(operands)) {
                throw new ExpressionLexerException("関数の引数がありません : " + input);
            }
            var keys = operands.Split(',');
            if (keys.Length != 4) {// Σ[カウンタ、始値、終値、定義]
                throw new ExpressionLexerException("引数の数が一致しません。Σ式は \"Sum[カウンタ、始値、終値、定義]\"の形をとる必要があります。 : " + operands);
            }
            var elements = new List<string>();
            elements.Add(name);
            for (int i = 0; i < keys.Length; i++) {
                if (i == 0) {//カウンタ変数。
                    if (counterList.Contains(keys[i])) {
                        elements.Add(keys[i]);
                    } else {
                        throw new Exception("Σにて未定義のカウンタ変数を発見しました。Σ式は \"Sum[カウンタ、始値、終値、定義]\"の形をとる必要があります。 : " + keys[i]);
                    }
                } else {
                    if (isNumber(keys[i])) {
                        elements.Add(keys[i]);
                    } else if (constantList.ContainsKey(keys[i])) {
                        elements.Add(constantList[keys[i]].ToString());
                    } else if (parameterList.Contains(keys[i])) {
                        elements.Add(keys[i]);
                    } else {//その他(式や関数呼び出し)の場合は次の世代で解析されるので、そのままにしておく。
                        elements.Add(keys[i]);
                    }
                }
            }
            return elements.ToArray();
        }
        static string[] SearchPi(string input, Dictionary<string, double> constantList, string[] parameterList, string[] counterList) {
            //Π[i,0,3i*x]等のΠ式を認識する
            //組み込みの関数PiExpressionを呼び出すための措置。
            var name = "Pi";
            var from = input.IndexOf('[');
            if (from != name.Length) {// Π[カウンタ、始値、終値、定義]
                throw new ExpressionLexerException("Π式は \"Pi[カウンタ、始値、終値、定義]\"の形をとる必要があります。input:" + input);
            }
            if (input.Last() != ']') {
                throw new ExpressionLexerException("Π式の括弧が対応していません :" + input);
            }
            var operands = input.Substring(from + 1, input.Length - name.Length - 2);
            if (string.IsNullOrEmpty(operands)) {
                throw new ExpressionLexerException("関数の引数がありません : " + input);
            }
            var keys = operands.Split(',');
            if (keys.Length != 4) {// Π[カウンタ、始値、終値、定義]
                throw new ExpressionLexerException("引数の数が一致しません。Π式は \"Pi[カウンタ、始値、終値、定義]\"の形をとる必要があります。 : " + operands);
            }
            var elements = new List<string>();
            elements.Add(name);
            for (int i = 0; i < keys.Length; i++) {
                if (i == 0) {//カウンタ変数。
                    if (counterList.Contains(keys[i])) {
                        elements.Add(keys[i]);
                    } else {
                        throw new Exception("Πにて未定義のカウンタ変数を発見しました。Π式は \"Pi[カウンタ、始値、終値、定義]\"の形をとる必要があります。 : " + keys[i]);
                    }
                } else {
                    if (isNumber(keys[i])) {
                        elements.Add(keys[i]);
                    } else if (constantList.ContainsKey(keys[i])) {
                        elements.Add(constantList[keys[i]].ToString());
                    } else if (parameterList.Contains(keys[i])) {
                        elements.Add(keys[i]);
                    } else {//その他(式や関数呼び出し)の場合は次の世代で解析されるので、そのままにしておく。
                        elements.Add(keys[i]);
                    }
                }
            }
            return elements.ToArray();
        }
        static string[] SearchParenthesis(string input) {
            try {
                if (input[0] != '(') {
                    return null;
                }
                int paren = 0;
                for (int i = 0; i < input.Length; i++) {
                    if (paren == '(') paren++;
                    if (paren == ')') paren--;
                }
                if (paren != 0) {
                    throw new Exception("括弧式の括弧が対応していません。 : " + input);
                }
                input = input.Substring(1, input.Length - 2);
                return new string[] { input };
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        static bool isNumber(string input) {
            double val = 0d;
            return double.TryParse(input, out val);
        }

    }
    [Serializable]
    internal class ExpressionLexerException : Exception {
        internal ExpressionLexerException() { }
        internal ExpressionLexerException(string message) : base(message) { }
        internal ExpressionLexerException(string message, Exception inner) : base(message, inner) { }
        protected ExpressionLexerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
