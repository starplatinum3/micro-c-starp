let z = (let x = 4 in z + x) in z * 2  

// test.fs(2,22): error FS0010: 意外的 关键字“in” 在交互中。应为 此点或之前的结构化构造不完整、符号“;;”、符号“;” 或其他标记。

// Using a verbatim string
//使用逐字字符串
let xmlFragment1 = @"<book author=""Milton, John"" title=""Paradise Lost"">"
// 这"Milton, John" 有引号吗
// 上面是有引号的  这是整体 ""Milton, John""
// let xmlFragment1 = @"<book author=" "Milton, John" " title=" "Paradise Lost" ">"
// test.fs(3,20): error FS0003: 此值不是一个函数，无法应用。
// Using a triple-quoted string
let xmlFragment2 = """<book author="Milton, John" title="Paradise Lost">"""


// 注意  add has two arguments!
// let add x = let f y = x+y in f  
let add x = 
    let f y = x+y in f  
//     Description
// 用于序列表达式，并且在详细语法中，用于分隔绑定中的表达式。
let addtwo = add 2                       
let x = 77  
addtwo 5 

// val add: x: int -> (int -> int)
// val addtwo: (int -> int)       
// val x: int = 77
// val it: int = 7

let z=x in z+x  
// let 出来了一个z+x 在这个情况下 给z 赋值 x 
// 我们让 z=x 在z+x的情况下
// 77+77=154 
// z 2
// val it: int = 154
// x 是自由的 因为 z 是x赋值来的  
// x是外面传进来的 z 是被x绑死的 

let z=x 
    // println $" z+x { z+x  }"
    in z+x  

let z=x 
    // println $" z+x { z+x  }"
    in 
    z+x  


let z=x 
    // println $" z+x { z+x  }"
    in 
    printfn "1"
    // printfn $" z+x { z+x  }"
    z+x  



let z=x 
    // println $" z+x { z+x  }"
    printfn "1"
    in 
    
    // printfn $" z+x { z+x  }"
    z+x  

// let x=2;;
let z=22;;

// 需要全局变量 
// 自由变量意味着该变量没有被申明 ... because they would be undeclared
let z=22
// test.fs(53,23): error FS0039: 未定义值或构造函数“z”。
let z = (let x = 4 in z + x) in z * 2  ;;
// 正常来看 z 确实没有被声明 在这个函数里 所以他会去全局找
// 所以这个自由的意思是这样理解吗 他不一定在函数作用域 可能在全局作用域 他会一级级的去找？
// val z: int = 22
// val it: int = 52
// 先求 (let x = 4 in z + x)  提供了 x 但是z没有
// 所以会需要全局变量 
// z = (let x = 4 in z + x) z被重新赋值
// 返回 z*2   

let z = (let x = 4 in z【自由】 + x) in z【被赋值 不自由】 * 2  ;;


let 
1==2

1=2

let 18 let 17 in var.0 + var.1  


let r = ref 177       // `ref` 创建引用  Create int reference  
!r                    // `!` 解引用/取值  Dereference  
r := !r+1; 
(r := !r+1; !r)       // `:=` 赋值 Assign to reference  
// 赋值  然后返回值吗？
!r  
let a = 1    // 符号绑定用let
r:=1         // 引用赋值 用 :=
    //**不用let**  



//Type for f : 'a -> int
//函数f 的参数可以是任意类型，用类型变量'a表示
let f x = 1
  in f 2 + f true
//   x 是 2 或者 true 不管传了 什么都是 返回1 吗
// 是 f x  的整体是1 吗

//   int -> int     bool -> int  
// test.fs(99,6): error FS0003: 此值不是一个函数，无法应用。


let f x = 1
  in f 2 +( f true)

// > let f x = 1 in f;;
val it : ('a -> int)

> let f x = 1 in f 3;;
val it : int = 1

> let f x = 1 in f true;;
val it : int = 1

> let f (x:int) = 1 in f;;
val it : (int -> int) = <fun:it@13-5>

let rec h x =             // Ill-typed: h not  polymorphic in its  own body  
      if true then 22                  
      else h 7 + h false      

//         else h 7 + h false      
//   -------------------^^^^^      

// stdin(4,20): error FS0001: 此表达式应具有类型
//     “int”
// 而此处具有类型
//     “bool”

//  dotnet  fsi


// 小练习 Joint exercises
// Which of these are well-typed, and why/not?
// 请判断 哪些表达式的类型是正确的？
let f x = 1
  in f f  
// val it: int = 1

let f g = g g  
//   let f g = g g
//   ------------^

// stdin(8,13): error FS0001: 类型不匹配。应为
//     “'a”
// 而给定的是
//     “'a -> 'b”
// 类型“'a”和“'a -> 'b”无法联合。

let f x =
  let g y = y  
  in g false  
in f 42  
// val f: x: 'a -> bool
// val it: bool = false
let f x =
  let g y = if true then y else x  
  in g false  
in f 42  


5+7  
let f x = x + 7 in f 2 end  // 函数声明 函数调用 放在一起
// val it: int = 9
let fac x = if x=0 then 1 else x * fac(x - 1)  
in fac 10 end  