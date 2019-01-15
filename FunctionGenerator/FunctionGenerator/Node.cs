using System.Collections.Generic;

namespace FunctionGenerator {
    /// <summary>
    /// Functionから式木のコンバートに挟まれる中間表現。
    /// 様々な具象クラスを持つ。
    /// </summary>
    internal abstract class Node {
        internal enum NodeType { Number, Constant, Parameter, Counter, Function, BinaryOperator, Expression, Root };
        internal List<Node> Children { get; set; }
        internal NodeType Type { get; set; }
        internal Node Parent { get; set; }
    }

    internal class RootNode : Node {
        internal RootNode() {
            Type = NodeType.Root;
            Children = new List<Node>();
        }
    }
    internal class BinaryNode : Node {
        internal string Operator { get; set; }
        internal enum OperatorType { Add, Subtract, Multiply, Divide, Pow, Undefined };
        internal OperatorType OpType { get; }
        internal BinaryNode(string op) {
            Type = NodeType.BinaryOperator;
            Operator = op;
            switch (op) {
                case "+":
                    OpType = OperatorType.Add;
                    break;
                case "-":
                    OpType = OperatorType.Subtract;
                    break;
                case "*":
                    OpType = OperatorType.Multiply;
                    break;
                case "/":
                    OpType = OperatorType.Divide;
                    break;
                case "^":
                    OpType = OperatorType.Pow;
                    break;
                default:
                    OpType = OperatorType.Undefined;
                    break;
            }
            Children = new List<Node>();
        }
        internal BinaryNode(string op, Node parent) {
            Type = NodeType.BinaryOperator;
            Operator = op;
            switch (op) {
                case "+":
                    OpType = OperatorType.Add;
                    break;
                case "-":
                    OpType = OperatorType.Subtract;
                    break;
                case "*":
                    OpType = OperatorType.Multiply;
                    break;
                case "/":
                    OpType = OperatorType.Divide;
                    break;
                case "^":
                    OpType = OperatorType.Pow;
                    break;
                default:
                    OpType = OperatorType.Undefined;
                    break;
            }
            Parent = parent;
            Children = new List<Node>();
        }
    }
    internal class NumberNode : Node {
        internal double Value { get; set; }
        internal NumberNode(double value) {
            Value = value;
            Type = NodeType.Number;
            Children = new List<Node>();
        }
        internal NumberNode(double value, Node parent) {
            Value = value;
            Parent = parent;
            Type = NodeType.Number;
            Children = new List<Node>();
        }
    }
    internal class ConstantNode : Node {
        internal double Value { get; set; }
        internal ConstantNode(double value) {
            Value = value;
            Type = NodeType.Constant;
            Children = new List<Node>();
        }
        internal ConstantNode(double value, Node parent) {
            Value = value;
            Parent = parent;
            Type = NodeType.Constant;
            Children = new List<Node>();
        }
    }
    internal class ParameterNode : Node {
        internal string Name { get; set; }
        internal ParameterNode(string name) {
            Name = name;
            Type = NodeType.Parameter;
            Children = new List<Node>();
        }
        internal ParameterNode(string name, Node parent) {
            Name = name;
            Parent = parent;
            Type = NodeType.Parameter;
            Children = new List<Node>();
        }
    }
    internal class CounterNode : Node {//ΣやΠといったカウンタ変数を表すNode
        internal string Name { get; set; }
        internal CounterNode(string name) {
            Name = name;
            Type = NodeType.Counter;
            Children = new List<Node>();
        }
        internal CounterNode(string name, Node parent) {
            Name = name;
            Parent = parent;
            Type = NodeType.Counter;
            Children = new List<Node>();
        }
    }
    internal class FunctionNode : Node {//関数呼び出しを表すNode
        internal string Name { get; set; }
        internal FunctionNode(string name, Node parent) {
            Type = NodeType.Function;
            Name = name;
            Children = new List<Node>();
            Parent = parent;
        }
        internal FunctionNode(string name) {
            Type = NodeType.Function;
            Name = name;
            Children = new List<Node>();
        }
    }
    internal class ExpressionNode : Node {//括弧式を表すNode
        internal ExpressionNode(Node parent) {
            Type = NodeType.Expression;
            Children = new List<Node>();
            Parent = parent;
        }
        internal ExpressionNode() {
            Type = NodeType.Expression;
            Children = new List<Node>();
        }
    }
}
