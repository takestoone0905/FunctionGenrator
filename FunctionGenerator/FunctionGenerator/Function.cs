using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FunctionGenerator {
    /// <summary>
    /// 入力ファイルから生成されるオブジェクト。
    /// </summary>
    internal class Function {
        /// <summary>
        /// Xmlより取り出される関数のプロパティ
        /// </summary>
        internal string Name { get; set; }
        internal string Body { get; set; }
        internal Dictionary<string, double> Constants { get; private set; }
        internal List<string> Parameters { get; private set; }
        internal List<string> Counters { get; private set; }

        internal Function() {
            Constants = new Dictionary<string, double>();
            Parameters = new List<string>();
            Counters = new List<string>();
        }
        internal Function(string name) {
            Name = name;
            Constants = new Dictionary<string, double>();
            Parameters = new List<string>();
            Counters = new List<string>();
        }
        internal Function(Function original, string body) {
            //プロパティを引き継ぎつつ関数定義を置き換える。
            //再帰的に定義式を解析する際に利用する。
            Constants = original.Constants;
            Parameters = original.Parameters;
            Counters = original.Counters;
            Body = body;
        }

        internal void AddConstant(string name, double value) {
            Constants.Add(name, value);
        }
        internal void AddParameter(string name) {
            Parameters.Add(name);
        }
        internal void AddCounter(string name) {
            Counters.Add(name);
        }


        internal static Function BuildFrom(string name, string body, IEnumerable<string> parameters, Dictionary<string, double> constatns) {
            Function function = new Function(name);
            function.Body = body;
            function.Parameters = parameters.ToList();
            function.Constants = constatns;
            return function;
        }
        internal static Function BuildFromXML(XDocument document) {
            try {
                Function function;
                var root = document.Root;
                preComplieDependences(root);
                function = new Function(getName(root));
                readConstants(root, function);
                readParaneters(root, function);
                readCounters(root, function);
                readBody(root, function);
                return function;
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
        internal static Function BuildFromXML(string xmlPath) {
            try {
                Function function;
                var root = getRoot(xmlPath);
                preComplieDependences(root);
                function = new Function(getName(root));
                readConstants(root, function);
                readParaneters(root, function);
                readCounters(root, function);
                readBody(root, function);
                return function;
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
        static XElement getRoot(string path) {
            XDocument xml;
            using (StreamReader SR = new StreamReader(path)) {
                xml = XDocument.Load(SR);
            }
            return xml.Root;
        }
        static string getName(XElement root) {
            if (root.Element("name") == null) {
                throw new Exception("入力エラー：関数に<name>要素がありません");
            }
            string name = root.Element("name").Value;
            if (string.IsNullOrWhiteSpace(name)) {
                throw new Exception("入力エラー：関数の名前がありません。<name>要素を確認してください。");
            }
            return name;
        }
        static void preComplieDependences(XElement root) {
            if (root.Element("dependences") != null) {//依存ファイルがある
                var dependences = root.Element("dependences").Elements("dependence");
                if (dependences != null) {
                    foreach (var dep in dependences) {
                        if (dep.Element("path") == null) {
                            throw new Exception("依存ファイルのpathが記されていません。");
                        }
                        string path = dep.Element("path").Value;
                        if (dep.Element("type") == null) {
                            throw new Exception("依存関数の型が指定されていません。引数の数を数字で記入してください。");
                        }
                        double type;
                        if (!double.TryParse(dep.Element("type").Value, out type)) {
                            throw new Exception("依存関数の型に誤りがあります。。引数の数を数字で記入してください。");
                        } else {
                            switch (type) {
                                case 1:
                                    FunctionGenerator.GenerateFunc<Func<double, double>>(path);
                                    break;
                                case 2:
                                    FunctionGenerator.GenerateFunc<Func<double, double, double>>(path);
                                    break;
                                case 3:
                                    FunctionGenerator.GenerateFunc<Func<double, double, double, double>>(path);
                                    break;
                                case 4:
                                    FunctionGenerator.GenerateFunc<Func<double, double, double, double, double>>(path);
                                    break;
                                case 5:
                                    FunctionGenerator.GenerateFunc<Func<double, double, double, double, double, double>>(path);
                                    break;
                                case 6:
                                    FunctionGenerator.GenerateFunc<Func<double, double, double, double, double, double, double>>(path);
                                    break;
                                default:
                                    throw new Exception("関数の引数が不正です。1個以上6個以下にしてください");
                            }
                        }
                    }
                }
            }
        }
        static void readConstants(XElement root, Function function) {
            foreach (var item in root.Elements("const")) {
                if (item.Element("name") == null || item.Element("value") == null) {
                    throw new Exception("定数の記法に誤りがあります。<name>要素と<value>要素があることを確認してください");
                } else if (string.IsNullOrWhiteSpace(item.Element("name").Value)) {
                    throw new Exception("定数に名前がありません。<name>要素を確認してください");
                } else if (string.IsNullOrWhiteSpace(item.Element("value").Value)) {
                    throw new Exception("定数に値がありません。<value>要素を確認してください");
                } else {
                    double d;
                    var b = double.TryParse(item.Element("value").Value, out d);
                    if (!b) {
                        throw new Exception("定数の値を解釈できませんでした。<value>要素を確認してください");
                    } else {
                        function.AddConstant(item.Element("name").Value, d);
                    }
                }
            }

        }
        static void readParaneters(XElement root, Function function) {
            foreach (var item in root.Elements("param")) {
                if (string.IsNullOrWhiteSpace(item.Value)) {
                    throw new Exception("変数に名前がありません。<param>要素を確認してください");
                }
                function.AddParameter(item.Value);
            }
        }
        static void readCounters(XElement root, Function function) {
            foreach (var item in root.Elements("counter")) {
                if (string.IsNullOrWhiteSpace(item.Value)) {
                    throw new Exception("カウンタ変数に名前がありません。<counter>要素を確認してください");
                }
                function.AddCounter(item.Value);
            }
        }
        static void readBody(XElement root, Function function) {
            if (root.Element("body") == null) {
                throw new Exception("入力エラー：関数に<body>要素がありません。");
            }
            if (string.IsNullOrWhiteSpace(root.Element("body").Value)) {
                throw new Exception("入力エラー：関数の定義式がありません。<body>を確認してください。");
            }
            function.Body = root.Element("body").Value;
        }


        internal string ToXml() {
            StringBuilder SB = new StringBuilder();
            SB.AppendLine("<function>");
            var names = Constants.Keys.ToArray();
            var values = Constants.Values.ToArray();
            for (int i = 0; i < Constants.Count; i++) {
                SB.AppendLine("<Constant>");
                SB.Append("<name>");
                SB.Append(names[i]);
                SB.AppendLine("</name>");
                SB.Append("<value>");
                SB.Append(values[i].ToString());
                SB.AppendLine("</value>");
                SB.AppendLine("</Constant>");
            }
            for (int i = 0; i < Parameters.Count; i++) {
                SB.Append("<param>");
                SB.Append(Parameters[i]);
                SB.AppendLine("</param>");
            }
            for (int i = 0; i < Counters.Count; i++) {
                SB.Append("<counter>");
                SB.Append(Counters[i]);
                SB.AppendLine("</counter>");
            }
            SB.Append("<body>");
            SB.Append(Body);
            SB.AppendLine("</body>");
            SB.AppendLine("</function>");
            return SB.ToString();
        }
        public override string ToString() {
            StringBuilder SB = new StringBuilder();
            SB.Append("Name : ");
            SB.AppendLine(Name);
            var names = Constants.Keys.ToArray();
            var values = Constants.Values.ToArray();
            for (int i = 0; i < Constants.Count; i++) {
                SB.Append("Constant : ");
                SB.Append(names[i]);
                SB.Append(" = ");
                SB.AppendLine(values[i].ToString());
            }
            for (int i = 0; i < Parameters.Count; i++) {
                SB.Append("Parameter : ");
                SB.AppendLine(Parameters[i]);
            }
            for (int i = 0; i < Counters.Count; i++) {
                SB.Append("Counter : ");
                SB.AppendLine(Counters[i]);
            }
            SB.Append(Name);
            SB.Append("(");
            SB.Append(Parameters[0]);
            foreach (var item in Parameters.Skip(1)) {
                SB.Append(",");
                SB.Append(item);
            }
            SB.Append(") = ");
            SB.Append(Body);
            return SB.ToString();
        }
    }
}
