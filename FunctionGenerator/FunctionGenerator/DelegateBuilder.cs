using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace FunctionGenerator {
    internal class DelegateBuilder {
        //入力になる変数
        static Dictionary<string, ParameterExpression> Parameters;
        //Σなど大型演算子の中身のみに登場するカウンタ変数。
        static Dictionary<string, ParameterExpression> Counters;
        internal static TDelegate BuildDelegate<TDelegate>(Function function, RootNode root) where TDelegate : Delegate {
            //function内で定義した変数名とParameterExpressionを対応させる。
            Parameters = new Dictionary<string, ParameterExpression>();
            Counters = new Dictionary<string, ParameterExpression>();
            foreach (var name in function.Parameters) {
                Parameters.Add(name, Expression.Parameter(typeof(double), name));
            }
            foreach (var name in function.Counters) {
                Counters.Add(name, Expression.Parameter(typeof(double), name));
            }
            try {
                var expr = Convert(root);
                var Params = new List<ParameterExpression>();
                Params.AddRange(Parameters.Values);
                var lambda = Expression.Lambda<TDelegate>(expr, Params);
                //再利用可能にするために登録する。
                ExpressionCaller.UserDefinedFunctions.Add(function.Name, lambda);
                return lambda.Compile();
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return default;
            }
        }

        /// <summary>
        /// Nodeを対応する式木に変換する。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        static Expression Convert(Node node) {            
            switch (node) {
                case RootNode rootNode:
                    return ConvertRoot(rootNode);
                case NumberNode number:
                    return ConvertNumber(number);
                case ConstantNode constant:
                    return ConverConstant(constant);
                case ParameterNode parameter:
                    return ConverParameter(parameter);
                case CounterNode counterNode:
                    return ConverCounter(counterNode);
                case BinaryNode binary:
                    return ConvertBinary(binary);
                case FunctionNode function://User定義の関数も対応可能。
                    switch (function.Name) {
                        case "Sum"://Σ式への対応をする。
                            return ConvertSumFunction(function);
                        case "Pi"://Π式への対応をする。
                            return ConvertPiFunction(function);
                        default:
                            return ConvertFunction(function);
                    }
                case ExpressionNode expression:
                    return ConvertExpression(expression);
                default:
                    throw new Exception("Unknown Node!");
            }
        }

        static Expression ConvertRoot(RootNode rootNode) {
            //Rootの直下は1つだけ。
            return Convert(rootNode.Children[0]);
        }
        static Expression ConvertNumber(NumberNode numberNode) {
            return Expression.Constant(numberNode.Value);
        }
        static Expression ConverConstant(ConstantNode constantNode) {
            return Expression.Constant(constantNode.Value);
        }
        static Expression ConverParameter(ParameterNode parameterNode) {
            return Parameters[parameterNode.Name];
        }
        static Expression ConverCounter(CounterNode counterNode) {
            return Counters[counterNode.Name];
        }
        static Expression ConvertBinary(BinaryNode binaryNode) {
            switch (binaryNode.OpType) {//子ノードは左辺と右辺の2つだけ。
                //再帰的に潜っていく。
                case BinaryNode.OperatorType.Add:
                    return Expression.Add(Convert(binaryNode.Children[0]), Convert(binaryNode.Children[1]));
                case BinaryNode.OperatorType.Subtract:
                    return Expression.Subtract(Convert(binaryNode.Children[0]), Convert(binaryNode.Children[1]));
                case BinaryNode.OperatorType.Multiply:
                    return Expression.Multiply(Convert(binaryNode.Children[0]), Convert(binaryNode.Children[1]));
                case BinaryNode.OperatorType.Divide:
                    return Expression.Divide(Convert(binaryNode.Children[0]), Convert(binaryNode.Children[1]));
                case BinaryNode.OperatorType.Pow:
                    return Expression.Power(Convert(binaryNode.Children[0]), Convert(binaryNode.Children[1]));
                case BinaryNode.OperatorType.Undefined:
                    throw new Exception("Undefined Binary Operator was found :" + binaryNode.Operator);
                default:
                    throw new Exception("Binary.OpType is not setted" + binaryNode.Operator);
            }
        }
        static Expression ConvertFunction(FunctionNode function) {
            List<Expression> operands = new List<Expression>();
            //関数の子ノードは引数に対応する。
            foreach (var item in function.Children) {
                operands.Add(Convert(item));
            }
            return ExpressionCaller.MethodExpression(function.Name, operands.ToArray());
        }
        static Expression ConvertExpression(ExpressionNode expressionNode) {
            //括弧式はRootのような振る舞いをする。直下は1つだけ。
            return Convert(expressionNode.Children[0]);
        }
        static Expression ConvertSumFunction(FunctionNode function) {
            //Σ式のために特別な処理をする。
            //式内に現れる変数をすべてリストアップしてlambda式を作るための準備をする。
            List<ParameterExpression> Paras = new List<ParameterExpression>();
            //カウンタ変数はコンパイルをするところで別途用意するので、ExpressionBuilderには渡さない。
            List<ParameterExpression> ParasWithoutCounter = new List<ParameterExpression>();
            //再帰的に変数を拾ってくる。
            getParametersInFunc(function, Paras, ParasWithoutCounter);
            LambdaExpression lambda;
            switch (Paras.Count) {//入力変数の数で分けてジェネリックを指定。
                case 1:
                    //子ノードは「カウンタ変数、始値、終値、定義式」になっている。
                    lambda = Expression.Lambda<Func<double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 2:
                    lambda = Expression.Lambda<Func<double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 3:
                    lambda = Expression.Lambda<Func<double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 4:
                    lambda = Expression.Lambda<Func<double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 5:
                    lambda = Expression.Lambda<Func<double, double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 6:
                    lambda = Expression.Lambda<Func<double, double, double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 7:
                    lambda = Expression.Lambda<Func<double, double, double, double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.SumExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                default:
                    break;
            }
            throw new Exception("Σのコンパイルに失敗しました。");
        }
        static Expression ConvertPiFunction(FunctionNode function) {
            //Σとほとんど同じです。
            List<ParameterExpression> Paras = new List<ParameterExpression>();
            List<ParameterExpression> ParasWithoutCounter = new List<ParameterExpression>();
            getParametersInFunc(function, Paras, ParasWithoutCounter);
            LambdaExpression lambda;
            switch (Paras.Count) {
                case 1:
                    lambda = Expression.Lambda<Func<double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 2:
                    lambda = Expression.Lambda<Func<double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 3:
                    lambda = Expression.Lambda<Func<double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 4:
                    lambda = Expression.Lambda<Func<double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 5:
                    lambda = Expression.Lambda<Func<double, double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 6:
                    lambda = Expression.Lambda<Func<double, double, double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                case 7:
                    lambda = Expression.Lambda<Func<double, double, double, double, double, double, double, double>>(Convert(function.Children[3]), Paras);
                    return ExpressionBuilder.PiExpression(Convert(function.Children[1]), Convert(function.Children[2]), lambda, ParasWithoutCounter.ToArray());
                default:
                    break;
            }
            throw new Exception("Πのコンパイルに失敗しました。");
        }
        //関数に含まれる変数をすべてリストアップする。
        static void getParametersInFunc(FunctionNode function, List<ParameterExpression> Paras, List<ParameterExpression> ParasWithoutCounter) {
            //最後の子ノード[Body]は別途数える
            for (int i = 0; i < function.Children.Count - 1; i++) {
                var node = function.Children[i];
                switch (node) {
                    case CounterNode counter:
                        Paras.Add(Counters[counter.Name]);
                        break;
                    case ParameterNode parameter:
                        Paras.Add(Parameters[parameter.Name]);
                        ParasWithoutCounter.Add(Parameters[parameter.Name]);
                        break;
                    default:
                        break;
                }
            }
            getParameters(function.Children.Last(), Paras, ParasWithoutCounter);
        }
        //再帰的に潜りながら変数を数え上げる。
        static void getParameters(Node node, List<ParameterExpression> Paras, List<ParameterExpression> ParasWithoutCounter) {
            switch (node.Type) {
                case Node.NodeType.Number:
                    return;
                case Node.NodeType.Constant:
                    return;
                case Node.NodeType.Parameter:
                    var P = (ParameterExpression)ConverParameter(node as ParameterNode);
                    if (!Paras.Contains(P)) {
                        Paras.Add(P);
                    }
                    if (!ParasWithoutCounter.Contains(P)) {
                        ParasWithoutCounter.Add(P);
                    }
                    return;
                case Node.NodeType.Counter:
                    var C = (ParameterExpression)ConverCounter(node as CounterNode);
                    if (!Paras.Contains(C)) {
                        Paras.Add(C);
                    }
                    return;
                case Node.NodeType.BinaryOperator:
                case Node.NodeType.Function:
                    foreach (var item in node.Children) {
                        getParameters(item, Paras, ParasWithoutCounter);
                    }
                    return;
                default:
                    break;
            }
        }
    }
}
