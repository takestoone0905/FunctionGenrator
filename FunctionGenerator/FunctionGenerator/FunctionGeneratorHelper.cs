using System;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionGenerator {
    /// <summary>
    /// Xmlを書いたtxtファイルを毎回用意するのは面倒なので、
    /// 文字列からXmlを起こすためのHelperクラスを実装。
    /// </summary>
    internal class FunctionGeneratorHelper {

        /// <summary>
        /// 定数と定義式を受け取って、XmlにParseする。
        /// そこからFunctionを生成する。
        /// </summary>
        /// <param name="constants">「g=9.8」のような定数宣言を要素とする。ここで宣言されていない文字は変数として扱われる。</param>
        /// <param name="definition">「  f(x,y)=x+Sum[i,0,3,x+i*y]  」のような形で書く。</param>
        /// <returns></returns>
        internal static Function Convert(string[] constants, string definition) {
            string built = BuildXml(constants == null ? null : constants, definition);
            XDocument xml = XDocument.Parse(built);
            return Function.BuildFromXML(xml);
        }

        //counter変数が重複しないようにメモをしておく。
        static List<string> counter;
        //入力を元にXmlを表すstringを組む。
        static string BuildXml(string[] constants, string definition) {
            counter = new List<string>();
            StringBuilder SB = new StringBuilder();
            SB.AppendLine("<function>");
            getName(definition, SB);
            if (constants != null) getConst(constants, SB);            
            getParam(definition, SB);
            getCounter("Sum", definition);
            getCounter("Pi", definition);
            addCounter(SB);
            getBody(definition, SB);
            SB.AppendLine("</function>");
            return SB.ToString();
        }
        static void getName(string definition, StringBuilder SB) {
            SB.Append("<name>");
            string name = definition.Substring(0, definition.IndexOf('(')).Trim();
            SB.Append(name);
            SB.AppendLine("</name>");
        }
        static void getConst(string[] constants, StringBuilder SB) {
            foreach (var item in constants) {
                var k = item.Split('=');
                SB.AppendLine("<const>");
                SB.Append("<name>");
                SB.Append(k[0].Trim());
                SB.AppendLine("</name>");
                SB.Append("<value>");
                SB.Append(k[1].Trim());
                SB.AppendLine("</value>");
                SB.AppendLine("</const>");
            }
        }
        static void getParam(string definition, StringBuilder SB) {
            string param = definition.Substring(definition.IndexOf('(') + 1, definition.IndexOf(')') - definition.IndexOf('(') - 1).Trim();
            foreach (var item in param.Split(',')) {
                SB.Append("<param>");
                SB.Append(item.Trim());
                SB.AppendLine("</param>");
            }
        }
        static void getCounter(string funcName, string definition) {
            //?を使った最短マッチで入れ子のSumなどにも対応可能になった。
            Regex regex = new Regex(funcName + "\\[.*?,");
            foreach (var item in regex.Matches(definition)) {
                string s = item.ToString().Substring(funcName.Length + 1);
                string name = s.Substring(0, s.Length - 1);
                if (!counter.Contains(name)) {
                    counter.Add(name);
                }
            }
        }
        static void getBody(string definition, StringBuilder SB) {
            SB.Append("<body>");
            SB.Append(definition.Split('=')[1]);
            SB.AppendLine("</body>");
        }
        static void addCounter(StringBuilder SB) {
            foreach (var item in counter) {
                SB.Append("<counter>");
                SB.Append(item);
                SB.AppendLine("</counter>");
            }
        }
    }
}
