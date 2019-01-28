using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FunctionGenerator {
    //Reflectionを利用して名前だけでExpressionBuilderの関数を呼び出す。
    //Reflectionを利用して名前だけでExpressionBuilderの関数を呼び出す。
    internal class ExpressionCaller {
        internal static Dictionary<string, LambdaExpression> UserDefinedFunctions = new Dictionary<string, LambdaExpression>();
        internal static Expression MethodExpression(string FuncName, params Expression[] operands) {
            try {
                //ここにはそれ以前にコンパイルしたUser定義関数のlambda式が入っている。
                if (UserDefinedFunctions.ContainsKey(FuncName)) {
                    return Expression.Invoke(UserDefinedFunctions[FuncName], operands);
                }
                //曖昧な一致を防ぐために引数の数を指定して正しいOverloadを取ってくる。
                var Ts = new Type[operands.Length];
                for (int i = 0; i < Ts.Length; i++) {
                    Ts[i] = typeof(Expression);
                }
                var method = typeof(ExpressionBuilder).GetMethod(FuncName + "Expression", Ts.ToArray());
                if (method == null) {
                    throw new Exception("未定義の関数が使用されました。関数名や引数の数を確認して下さい。: " + FuncName);
                }
                return (Expression)method.Invoke(null, operands);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }

    /// <summary>
    /// 式木を組み上げるためのパーツを提供するメソッド群
    /// 大半はMathクラスのコードからの自動生成でできている。
    /// CodeGenerator.slnを参照のこと。
    /// </summary>
    internal class ExpressionBuilder {
        //デバッグ用。
        internal static Expression Debug<T>(Expression expression) {
            var method = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(T) });
            return Expression.Call(method, expression);
        }
        //式の値を取り出す。expressionは引数とか取らないでね。
        internal static T getValue<T>(Expression expression) {
            var func = Expression.Lambda<Func<T>>(
                expression
                ).Compile();
            return func();
        }
        //ちょっと無理やりな実装ですが、変数の値を取り出すためのモノ?
        internal static T getValue<T>(ParameterExpression parameter, T value) {
            var body = Expression.Block(typeof(T), new[] { parameter },
                parameter
                );
            var func = Expression.Lambda<Func<T, T>>(
                body, parameter
                ).Compile();
            return func(value);
        }
        /// <summary>
        /// Σ(x+ix)等の式を表すExpression。コンパイル前のlambda式を受け取る。
        /// </summary>
        /// <param name="from">Σ内部のカウンタ変数の始値(Inclusive)</param>
        /// <param name="to">Σ内部のカウンタ変数の終値(Inclusive)</param>
        /// <param name="lambda">Σ式のBodyを表すlambda式。第一引数としてカウンタ変数を取るような式を作っておくこと。</param>
        /// <param name="param">lambda式で使用したParameterの実際の値を表すExpression[但しカウンタを表すExpressionParameterは含まない。]</param>
        /// <returns></returns>
        public static Expression SumExpression(Expression from, Expression to, LambdaExpression lambda, params Expression[] paramsForLambda) {
            //Σ(x*i)
            var i = Expression.Parameter(typeof(double), "i");
            var label = Expression.Label("Loop");
            var sum = Expression.Parameter(typeof(double), "result");
            //受け取ったlambda式のための準備。(InvocationExpressionを作っておく。)
            var Ps = new List<Expression>();
            Ps.Add(i);
            Ps.AddRange(paramsForLambda);
            var Invoke = Expression.Invoke(lambda, Ps.ToArray());
            //作った内部式を使ってSumを計算するようなBody。
            var body = Expression.Block(typeof(double), new[] { i, sum },
                Expression.Assign(i, from),
                Expression.Assign(sum, Expression.Constant(0d)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.GreaterThan(i, to),
                        Expression.Break(label),
                        Expression.Block(
                           Expression.AddAssign(sum, Invoke),
                           Expression.AddAssign(i, Expression.Constant(1d))
                        ))
                    , label),
                sum);
            return body;
        }
        /// <summary>
        /// Π(x+ix)等の式を表すExpression。積算を繰り返す。コンパイル前のlambda式を受け取る。
        /// </summary>
        /// <param name="from">Σ内部のカウンタ変数の始値(Inclusive)</param>
        /// <param name="to">Σ内部のカウンタ変数の終値(Inclusive)</param>
        /// <param name="lambda">Σ式のBodyを表すlambda式。第一引数としてカウンタ変数を取るような式を作っておくこと。</param>
        /// <param name="param">lambda式で使用したParameterの実際の値を表すExpression[但しカウンタを表すExpressionParameterは含まない。]</param>
        /// <returns></returns>
        public static Expression PiExpression(Expression from, Expression to, LambdaExpression lambda, params Expression[] paramsForLambda) {
            //Π(x*i)
            var i = Expression.Parameter(typeof(double), "i");
            var label = Expression.Label("Loop");
            var sum = Expression.Parameter(typeof(double), "result");
            //受け取ったlambda式のための準備。(InvocationExpressionを作っておく。)
            var Ps = new List<Expression>();
            Ps.Add(i);
            Ps.AddRange(paramsForLambda);
            var Invoke = Expression.Invoke(lambda, Ps.ToArray());
            //作った内部式を使ってSumを計算するようなBody。
            var body = Expression.Block(typeof(double), new[] { i, sum },
                Expression.Assign(i, from),
                Expression.Assign(sum, Expression.Constant(1d)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.GreaterThan(i, to),
                        Expression.Break(label),
                        Expression.Block(
                           Expression.MultiplyAssign(sum, Invoke),
                           Expression.AddAssign(i, Expression.Constant(1d))
                        ))
                    , label),
                sum);
            return body;
        }
        public static Expression FactExpression(Expression expression) {
            var i = Expression.Parameter(typeof(double));
            var result = Expression.Parameter(typeof(double));
            var label = Expression.Label("out");
            var body = Expression.Block(
                typeof(double),
                new[] { i, result },
                Expression.Assign(i, expression),
                Expression.Assign(result, Expression.Constant(1d)),
                Expression.Loop(
                    Expression.Block(
                        Expression.IfThenElse(
                            Expression.GreaterThan(i, Expression.Constant(0d)),
                            Expression.Block(
                                Expression.MultiplyAssign(result, i),
                                Expression.SubtractAssign(i, Expression.Constant(1d))
                            ),
                            Expression.Break(label)
                        )
                   )
                , label)
            , result);
            return body;
        }
        //「nCr」を表現する式を生成する。expr0はn、expr1にr対応する。
        public static Expression CombinationExpression(Expression expr_n, Expression expr_r) {
            var i = Expression.Parameter(typeof(double));
            var denominator = Expression.Parameter(typeof(double));
            var numerator = Expression.Parameter(typeof(double));
            var result = Expression.Parameter(typeof(double));
            var label = Expression.Label("OUT");
            var body = Expression.Block(
                typeof(double),//Type
                new[] { numerator, denominator, i, result },//Variables(abd expressions Follow)
                Expression.Assign(numerator, Expression.Constant(1d)),
                Expression.Assign(denominator, Expression.Constant(1d)),
                Expression.Assign(i, Expression.Constant(0d)),
                Expression.Loop(
                    Expression.IfThenElse(//if-elseステートメント開始
                        Expression.LessThan(i, Expression.Convert(expr_r, typeof(double))),
                        Expression.Block(//ifスコープ
                            Expression.MultiplyAssign(numerator, Expression.Subtract(expr_n, i)),
                            Expression.MultiplyAssign(denominator, Expression.Add(i, Expression.Constant(1d))),
                            Expression.AddAssign(i, Expression.Constant(1d))
                        ),
                        Expression.Block(//elseスコープ
                            Expression.Assign(result, Expression.Divide(numerator, denominator)),
                            Expression.Break(label)
                        )
                    )//if-else終了
                , label)//Loopブロック末尾。抜けるためのラベル。
            , result);//return文に相当
            return body;
        }

        //微積分は数値的に計算してしまうか？（ややコストが高い）

        #region Mathクラスより自動生成
        public  static Expression AbsExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Abs", new Type[] { typeof(double) });
            MethodCallExpression AbsExpression = Expression.Call(Method, expression);
            return AbsExpression;
        }
        public static Expression AcosExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Acos", new Type[] { typeof(double) });
            MethodCallExpression AcosExpression = Expression.Call(Method, expression);
            return AcosExpression;
        }
        public static Expression AcoshExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Acosh", new Type[] { typeof(double) });
            MethodCallExpression AcoshExpression = Expression.Call(Method, expression);
            return AcoshExpression;
        }
        public static Expression AsinExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Asin", new Type[] { typeof(double) });
            MethodCallExpression AsinExpression = Expression.Call(Method, expression);
            return AsinExpression;
        }
        public static Expression AsinhExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Asinh", new Type[] { typeof(double) });
            MethodCallExpression AsinhExpression = Expression.Call(Method, expression);
            return AsinhExpression;
        }
        public static Expression AtanExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Atan", new Type[] { typeof(double) });
            MethodCallExpression AtanExpression = Expression.Call(Method, expression);
            return AtanExpression;
        }
        public static Expression Atan2Expression(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("Atan2", new Type[] { typeof(double), typeof(double) });
            MethodCallExpression Atan2Expression = Expression.Call(Method, expression0, expression1);
            return Atan2Expression;
        }
        public static Expression AtanhExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Atanh", new Type[] { typeof(double) });
            MethodCallExpression AtanhExpression = Expression.Call(Method, expression);
            return AtanhExpression;
        }
        public static Expression CbrtExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Cbrt", new Type[] { typeof(double) });
            MethodCallExpression CbrtExpression = Expression.Call(Method, expression);
            return CbrtExpression;
        }
        public static Expression CeilingExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Ceiling", new Type[] { typeof(double) });
            MethodCallExpression CeilingExpression = Expression.Call(Method, expression);
            return CeilingExpression;
        }
        public static Expression CosExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Cos", new Type[] { typeof(double) });
            MethodCallExpression CosExpression = Expression.Call(Method, expression);
            return CosExpression;
        }
        public static Expression CoshExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Cosh", new Type[] { typeof(double) });
            MethodCallExpression CoshExpression = Expression.Call(Method, expression);
            return CoshExpression;
        }
        public static Expression ExpExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Exp", new Type[] { typeof(double) });
            MethodCallExpression ExpExpression = Expression.Call(Method, expression);
            return ExpExpression;
        }
        public static Expression FloorExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Floor", new Type[] { typeof(double) });
            MethodCallExpression FloorExpression = Expression.Call(Method, expression);
            return FloorExpression;
        }
        public static Expression LogExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Log", new Type[] { typeof(double) });
            MethodCallExpression LogExpression = Expression.Call(Method, expression);
            return LogExpression;
        }
        public static Expression Log10Expression(Expression expression) {
            var Method = typeof(Math).GetMethod("Log10", new Type[] { typeof(double) });
            MethodCallExpression Log10Expression = Expression.Call(Method, expression);
            return Log10Expression;
        }
        public static Expression PowExpression(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) });
            MethodCallExpression PowExpression = Expression.Call(Method, expression0, expression1);
            return PowExpression;
        }
        public static Expression SinExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Sin", new Type[] { typeof(double) });
            MethodCallExpression SinExpression = Expression.Call(Method, expression);
            return SinExpression;
        }
        public static Expression SinhExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Sinh", new Type[] { typeof(double) });
            MethodCallExpression SinhExpression = Expression.Call(Method, expression);
            return SinhExpression;
        }
        public static Expression SqrtExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Sqrt", new Type[] { typeof(double) });
            MethodCallExpression SqrtExpression = Expression.Call(Method, expression);
            return SqrtExpression;
        }
        public static Expression TanExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Tan", new Type[] { typeof(double) });
            MethodCallExpression TanExpression = Expression.Call(Method, expression);
            return TanExpression;
        }
        public static Expression TanhExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Tanh", new Type[] { typeof(double) });
            MethodCallExpression TanhExpression = Expression.Call(Method, expression);
            return TanhExpression;
        }
        public static Expression IEEERemainderExpression(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("IEEERemainder", new Type[] { typeof(double), typeof(double) });
            MethodCallExpression IEEERemainderExpression = Expression.Call(Method, expression0, expression1);
            return IEEERemainderExpression;
        }
        public static Expression LogExpression(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("Log", new Type[] { typeof(double), typeof(double) });
            MethodCallExpression LogExpression = Expression.Call(Method, expression0, expression1);
            return LogExpression;
        }
        public static Expression MaxExpression(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("Max", new Type[] { typeof(double), typeof(double) });
            MethodCallExpression MaxExpression = Expression.Call(Method, expression0, expression1);
            return MaxExpression;
        }
        public static Expression MinExpression(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("Min", new Type[] { typeof(double), typeof(double) });
            MethodCallExpression MinExpression = Expression.Call(Method, expression0, expression1);
            return MinExpression;
        }
        public static Expression RoundExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Round", new Type[] { typeof(double) });
            MethodCallExpression RoundExpression = Expression.Call(Method, expression);
            return RoundExpression;
        }
        public static Expression RoundExpressionWithDigits(Expression expression0, Expression expression1) {
            var Method = typeof(Math).GetMethod("Round", new Type[] { typeof(double), typeof(int) });
            MethodCallExpression RoundExpression = Expression.Call(Method, expression0, expression1);
            return RoundExpression;
        }
        public static Expression TruncateExpression(Expression expression) {
            var Method = typeof(Math).GetMethod("Truncate", new Type[] { typeof(double) });
            MethodCallExpression TruncateExpression = Expression.Call(Method, expression);
            return TruncateExpression;
        }
        #endregion
    }
}