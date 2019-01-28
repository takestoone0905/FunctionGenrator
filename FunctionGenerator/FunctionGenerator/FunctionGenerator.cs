using System;

namespace FunctionGenerator {
    public class FunctionGenerator {
        /*
        static void Main() {
            var func = FunctionGenerator.GenerateFunc<Func<double, double, double>>(
                new string[] { "g=9.8" },
                "func(v,t)=v-g*t");
            Console.WriteLine(func(1.0d, 2.0d));//Output:-18.6
        }
        */
        public static TDelegate GenerateFunc<TDelegate>(string FilePath) where TDelegate : Delegate {
            try {
                Function function = Function.BuildFromXML(FilePath);
                if (function == null) {
                    throw new Exception("入力に誤りがあります。Delegateは生成されませんでした。");
                }
                //ここにも例外処理があってしかるべきか。
                RootNode root = ExpressionLexer.BuildTree(function);
                if (root == null) {
                    throw new Exception("定義式の解析に失敗しました。Delegateは生成されませんでした。");
                }
                var func = DelegateBuilder.BuildDelegate<TDelegate>(function, root);
                if (func == null) {
                    throw new Exception("コンパイルに失敗しました。Delegateは生成されませんでした。");
                }
                return func;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        public static TDelegate GenerateFunc<TDelegate>(string[] Constants, string definition) where TDelegate : Delegate {
            try {
                Function function = FunctionGeneratorHelper.Convert(Constants, definition);
                if (function == null) {
                    throw new Exception("入力に誤りがあります。Delegateは生成されませんでした。");
                }
                //ここにも例外処理があってしかるべきか。
                RootNode root = ExpressionLexer.BuildTree(function);
                if (root == null) {
                    throw new Exception("定義式の解析に失敗しました。Delegateは生成されませんでした。");
                }
                var func = DelegateBuilder.BuildDelegate<TDelegate>(function, root);
                if (func == null) {
                    throw new Exception("コンパイルに失敗しました。Delegateは生成されませんでした。");
                }
                return func;
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        #region Test Samples 
        static void Test() {
            var f = GenerateFunc<Func<double, double, double>>("Sample/test.txt");
            if (f != null) {
                Console.WriteLine(f(10, 5));
            } else {
                Console.WriteLine("Try Again!");
            }
        }
        static void Test_dependences() {
            var f = GenerateFunc<Func<double, double, double>>("Sample/MySum.txt");
            if (f != null) {
                Console.WriteLine(f(10, 5));
            } else {
                Console.WriteLine("Try Again!");
            }
        }
        static void Test_Combination() {
            var f = GenerateFunc<Func<double, double, double>>(new string[] { },"f(n,r)=C[n,r]");
            if (f != null) {
                Console.WriteLine(f(10, 5));
            } else {
                Console.WriteLine("Try Again!");
            }
        }
        static void Test_sigma() {
            var f = GenerateFunc<Func<double, double>>("Sample/Sigma.txt");
            if (f != null) {
                Console.WriteLine(f(3));
            } else {
                Console.WriteLine("Try Again!");
            }
        }
        static void Test_pi() {
            var f = GenerateFunc<Func<double, double>>("Sample/Pi.txt");
            if (f != null) {
                Console.WriteLine(f(3));
            } else {
                Console.WriteLine("Try Again!");
            }
        }
        static void Test_paren() {
            var f = GenerateFunc<Func<double, double, double>>("Sample/Paren.txt");
            if (f != null) {
                Console.WriteLine(f(3, 1));
            } else {
                Console.WriteLine("Try Again!");
            }
        }
        static void Test_helper() {
            var f = GenerateFunc<Func<double, double, double>>(new string[] { "a=1", "b=2" }, "f(x,y)=x*y+Sum[i,a,b,2*i]");
            Console.WriteLine(f(10, 3));
            var f2 = GenerateFunc<Func<double, double, double>>(null, "f2(x,y)=f[x,y]");
            Console.WriteLine(f2(10, 3));
        }
        #endregion    
    }
}
