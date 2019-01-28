# FunctionGenrator
函数を表す文字列（もしくはXml）からDelegateを生成するためのC#モジュール
![sample](https://user-images.githubusercontent.com/46702789/51844951-dbe83300-2359-11e9-8811-5c27601b1d80.PNG)


English::

This module enables you to generate a delegate from strings which represents numerical function or text file written like a Xml file. 

Install:
 Put all Files or, Copy and Paste the all code to the project where you want to use this.

Usage:
 Function Generator can accept two type of input, raw strings and input file.

1. Generate a delegate from strings

      Use the method “FunctionGenerator.GenerateFunc(string[] Constants, string definition)"
   Constants is an array of strings whose elements means an equality such as “pi=3.14”. Names defined here will be replaced with the 　
   right value in following definition.
     2nd operand represents a numerical function and is written like this: “func(r)=pi*r^2”
   Return Type from GenerateFunc with this example is Func<double,double>.
     After generation, you can use the function “func” in definitions of the following definition. So,
   “func2(r,θ)=func[r]*(θ/360)” is acceptable.(In a definition, use “[operand1,operand2,…]” to call other functions.)

2. Generate a delegate from Files
    Use the method “FunctionGenerator.GenerateFunc(string FilePath)". This overload parse a file with a certain format like Xml and Generate a delegate.Now, Let me show you an example.

![sample2](https://user-images.githubusercontent.com/46702789/51845302-a728ab80-235a-11e9-84ab-f19600a41808.PNG)

 This example contains one constant “a=1.0” and two parameters, x and y. What is more, this example uses a function “double”. It is indicated by <path> to a file which defines the function, and <Type> which indicates how many parameters “double” takes. When the parser find <dependence>, program parse the file and register the function.
 

Built-in Function:
  ・Numerical functions defined in System.Math class are built-in. (ex: f(x) = Abs[x]+5)
  
  ・Combination is available(ex: f(x,n,r) =x+C[n,r] )
 
 ・Σ, Π is registered as Sum and Pi.
     Ex. ∑_(i=0)^10▒〖(x\*i+5)〗 is written as Sum[i,0,10,x\*i+5]
  This “i” can be used without declaration such as constants need. However, in input file, counter parameter have to be written with <counter> tag. (ex. <counter> i </counter>)

Sample
This sample generates a delegate which calculate velocity from initial velocity and time as parameters.
![sample](https://user-images.githubusercontent.com/46702789/51844951-dbe83300-2359-11e9-8811-5c27601b1d80.PNG)
 

 
日本語::

　このモジュールは、数学的な記法で書かれた関数を表す文字列を受けとって、それをデリケートに変換するモノです。入力は文字列と、Xmlのような構造で書かれるファイルの2種類を取ることができます。

導入
　ファイルを全部プロジェクトに突っ込めば、とりあえず動きます。

使い方
1.　文字列からデリケートを生成する
　FunctionGenerator.GenerateFunc(string[] Constants, string definition)という静的メソッドを使います。Constantsは“pi=3.14”などの、定数名とその値を表現する等式の配列です。ここに書いた定数名は、右辺値で置換されます。
   第二引数は “func(r)=pi*r^2”という数学で見るような形式で書かれます。この場合は
Func<double,double>型のデリケートが生成されます。
  一度生成した関数は名前が登録されます。この場合はfuncという名前で、参照できるようになります。例えば、funcを使って計算するfunc2を次のように書き表すことができます。“func2(r, θ) = func[r]*(θ/360)” 。
なお、関数定義の中に他の関数を書くときは、”関数名[引数リスト]” という記法で書かなければなりません。

2. ファイルからデリケートを生成する
   “FunctionGenerator.GenerateFunc(string FilePath)"を使えば、指定したPathのファイルをParseしてデリケートを生成できます。下に例を示します。
   
![sample2](https://user-images.githubusercontent.com/46702789/51845302-a728ab80-235a-11e9-84ab-f19600a41808.PNG)

 この例では、“a=1.0”なる定数1つと、xとyという2つの変数が定義されています。さらに、依存ファイルにdouble.txtというファイルを指定しています。<path> タグでそのファイルへのパスを、<Type>タグではその依存ファイル内で定義された関数の引数の数を指定します。<dependence>タグをParserが見つけると、そのファイルを先にParseし、内部で宣言された関数を登録します。

組み込みの関数:
  ・System.Mathクラスに定義されている関数が利用できます。例: f(x) = Abs[x]+5
  
  ・組み合わせを表すC[n,r]が使えます。例: f(x,n,r) =x+C[n,r] 
  
  ・Σ, ΠはそれぞれSum、Piという名前の関数になっています。
    例. ∑_(i=0)^10▒〖(x\*i+5)〗 は、Sum[i,0,10,x\*i+5]　という文字列に相当します。
  ここでの “i” は定数とは異なり、宣言することなく使えます。しかし、ファイルからの入力でデリケートを生成する場合、<counter> タグでカウンタ変数を宣言する必要があります（ <counter> i </counter>　など）。

サンプル
　このサンプルは、初速と経過時間を引数にとり、その時刻での落体の速度を計算するようなデリケートを生成します。
 
![sample](https://user-images.githubusercontent.com/46702789/51844951-dbe83300-2359-11e9-8811-5c27601b1d80.PNG)
 

