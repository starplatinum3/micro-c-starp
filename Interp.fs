(* File MicroC/Interp.c
   Interpreter for micro-C, a fraction of the C language
   sestoft@itu.dk * 2010-01-07, 2014-10-18

   A value is an integer; it may represent an integer or a pointer,
   where a pointer is just an address in the store (of a variable or
   pointer or the base address of an array).  The environment maps a
   variable to an address (location), and the store maps a location to
   an integer.  This freely permits pointer arithmetics, as in real C.
   Expressions can have side effects.  A function takes a list of
   typed arguments and may optionally return a result.

   For now, arrays can be one-dimensional only.  For simplicity, we
   represent an array as a variable which holds the address of the
   first array element.  This is consistent with the way array-type
   parameters are handled in C (and the way that array-type variables
   were handled in the B language), but not with the way array-type
   variables are handled in C.

   The store behaves as a stack, so all data are stack allocated:
   variables, function parameters and arrays.

   The return statement is not implemented (for simplicity), so all
   functions should have return type void.  But there is as yet no
   typecheck, so be careful.
 *)

 (*文件micro/Interp.c）
micro-C的解释器，C语言的一小部分
sestoft@itu.dk * 2010-01-07, 2014-10-18
值是一个整数；它可以表示整数或指针，
其中指针只是存储区中的地址（变量或
指针或数组的基址）。环境映射了一个
变量映射到一个地址（位置），而商店将一个位置映射到
一个整数。这可以自由地使用指针算法，就像在实C中一样。
表情可能有副作用。函数获取
类型化参数，并可以选择返回结果。
目前，数组只能是一维的。为了简单起见，我们
将数组表示为一个变量，该变量保存
第一个数组元素。这与数组类型的方式一致
参数在C中处理（以及数组类型变量的处理方式）
是用B语言处理的），但不是用数组类型
变量是用C语言处理的。
存储的行为类似于堆栈，因此所有数据都是堆栈分配的：
变量、函数参数和数组。
return语句没有实现（为了简单起见），所以
函数应具有返回类型void。但目前还没有
打字检查，所以要小心。
*)

module Interp

open Absyn
open Debug
open System
open interpc.top.starp.util
//open interpc.top.starp.util
//open interpc.top.starp.util.PrintUtil

(* Simple environment operations *)
// 多态类型 env
// 环境 env 是 元组 ("name",data) 的列表 ，名称是字符串 string 值 'data 可以是任意类型
//  名称 ---> 数据 名称与数据绑定关系的 键-值 对  key-value pairs
// [("x",9);("y",8)]: int env

//str 和data的对应 元组，而不是个map
type 'data env = (string * 'data) list

///环境查找函数
///在环境 env上查找名称为 x 的值
let rec lookup env wantFoundName =
    match env with
    | [] -> failwith (wantFoundName + " not found")
//    https://blog.csdn.net/Vermont_/article/details/84557065
//    找x
//    他的数据结构是元组的数组，所以他的head 是一个元组
//    修改了变量名 自认为代码可以更容易被看懂了
//    递归是一件好事吗？ 递归不是要用内存的？ stack 虽然不会爆栈 。。 但是递归一般不是不好吗，为什么不能迭代呢
//不过这应该就是所谓尾递归吧 我也不太懂 总之可能不会占 栈空间
//    虽然递归会显得比较高级吧。。 我懂了。。 因为可能f# 根本没办法写循环里return东西，因为这是个没有return的语言
//    https://zhuanlan.zhihu.com/p/48777030
//真是一门可怕的语言 没有return  很多简单的事情都做不了，都得靠递归，但是我是废物，递归又看不懂
//    找到了变量 str 的val 
    | (headName, headValue) :: rest -> if wantFoundName = headName then headValue else lookup rest wantFoundName
//    | (headName,head) :: rest -> if wantFound = y then value else lookup rest wantFound

//let lookUp env wantFoundName =
////    https://docs.microsoft.com/zh-cn/dotnet/fsharp/language-reference/conditional-expressions-if-then-else
//    for (name,value) in env do
//        if wantFoundName = name then
//            return value
////            `return` may only be used within computation expressions
////    `return`只能在计算表达式中使用
//            
//            
//            
//    match env with
//    | [] -> failwith (wantFoundName + " not found")
////    找x
////    他的数据结构是元组的数组，所以他的head 是一个元组
////    修改了变量名 自认为代码可以更容易被看懂了
////    递归是一件好事吗？ 递归不是要用内存的？ stack 虽然不会爆栈 。。 但是递归一般不是不好吗，为什么不能迭代呢
////    虽然递归会显得比较高级吧。。
//    | (headName, headValue) :: rest -> if wantFoundName = headName then headValue else lookup rest wantFoundName

(* A local variable environment also knows the next unused store location *)

// ([("x",9);("y",8)],10)
// x 在位置9,y在位置8,10--->下一个空闲空间位置10
type locEnv = int env * int
//env 是一个map ？

(* A function environment maps a function name to parameter list and body *)
//函数参数例子:
//void func (int a , int *p)
// 参数声明列表为: [(TypI,"a");(TypP(TypI) ,"p")]
type paramdecs = (typ * string) list

(* 函数环境列表
  [("函数名", ([参数元组(类型,"名称")的列表],函数体AST)),....]

  //main (i){
  //  int r;
  //    fac (i, &r);
  //    print r;
  // }
  [ ("main",
   ([(TypI, "i")],
    Block
      [Dec (TypI,"r");
       Stmt (Expr (Call ("fac",[Access (AccVar "i"); Addr (AccVar "r")])));
       Stmt (Expr (Prim1 ("printi",Access (AccVar "r"))))]))]

函数环境 是 多态类型  'data env ---(string * 'data ) list 的一个 具体类型 ⭐⭐⭐
    类型变量 'data  具体化为  (paramdecs * stmt)
    (string * (paramdecs * stmt)) list
*)

type funEnv = (paramdecs * stmt) env

(* A global environment consists of a global variable environment
   and a global function environment
 *)

// 全局环境是 变量声明环境 和 函数声明环境的元组
// 两个列表的元组
// ([var declares...],[fun declares..])
// ( [ ("x" ,1); ("y",2) ], [("main",mainAST);("fac",facAST)] )
// mainAST,facAST 分别是main 与fac 的抽象语法树

type gloEnv = int env * funEnv

(* The store maps addresses (ints) to values (ints): *)

//地址是store上的的索引值
type address = int

// store 是一个 地址到值的映射，是对内存的抽象 ⭐⭐⭐
// store 是可更改的数据结构，特定位置的值可以修改，注意与环境的区别
// map{(0,3);(1,8) }
// 位置 0 保存了值 3
// 位置 1 保存了值 8

type store = Map<address, int>

//空存储
let emptyStore = Map.empty<address, int>

//保存value到存储store
let setSto (store: store) addr value = store.Add(addr, value)

//输入addr 返回存储的值value
// 不是很看得懂 这返回的是什么。。
// 不过好在有注释，虽然这语言看不懂 至少我知道他在干嘛
///一个不易读的函数，其实是 map.get(key)
let getSto (store: store) addr = store.Item addr

// store上从loc开始分配n个值的空间
// 用于数组分配
let rec initSto loc n store =
    if n = 0 then
        store
    else // 默认值 0
        initSto (loc + 1) (n - 1) (setSto store loc 0)

(* Combined environment and store operations *)

(* Extend local variable environment so it maps x to nextloc
   (the next store location) and set store[nextloc] = v.

locEnv结构是元组 : (绑定环境env,下一个空闲地址nextloc)
store结构是Map<string,int>
为什么是str ,addr 不是int吗.. 不懂
也就是str 地址，保存了一个int ，那str就没有可能实现了吧。。。

扩展环境 (x nextloc) :: env ====> 新环境 (env1,nextloc+1)
变更store (nextloc) = v
 *)

// 绑定一个值 x,v 到环境
// 环境是非更改数据结构，只添加新的绑定（变量名称，存储位置），注意与store 的区别⭐⭐⭐
// 返回新环境 locEnv,更新store,
// nextloc是store上下一个空闲位置
(*

// variable.c
int g ;
int h[3];
void main (int n){
n = 8;
}
上面c程序的解释环境如下：

 环境：locEnv:
    ([(n, 5); (n, 4); (g, 0)], 6)

存储：store:
    (0, 0)  (1, 0)(2, 0)(3, 0)(4, 1)  (5, 8)
     ^^^^    ^^^^^^^^^^^^^^^^^^^^^^    ^^^^
       g               h                n

   变量 地址 值
   n--->5--->8
   h--->4--->1
   g--->0--->0

   下一个待分配位置是 6
*)

//将多个值 xs vs绑定到环境
//遍历 xs vs 列表,然后调用 bindVar实现单个值的绑定
let store2str store =
    String.concat "" (List.map string (Map.toList store))

let bindVar x v (env, nextloc) store : locEnv * store =
    let env1 = (x, nextloc) :: env
    msg $"bindVar:\n%A{env1}\n"

    //返回新环境，新的待分配位置+1，设置当前存储位置为值 v
    let ret = ((env1, nextloc + 1), setSto store nextloc v)
    
    msg $"locEnv:\n {fst ret}\n"
    msg $"Store:\n {store2str (snd ret)}\n"

    ret 


let rec bindVars xs vs locEnv store : locEnv * store =
    let res =
        match (xs, vs) with
        | ([], []) -> (locEnv, store)
        | (xHead :: xRest, vHead :: vRest) ->
            let (locEnv1, sto1) = bindVar xHead vHead locEnv store
            bindVars xRest vRest locEnv1 sto1
        | _ -> failwith "parameter/argument mismatch"

    msg "\nbindVars:\n"
    msg $"\nlocEnv:\n{locEnv}\n"
    msg $"\nStore:\n"
    store2str store |> msg
    res
(* Allocate variable (int or pointer or array): extend environment so
   that it maps variable to next available store location, and
   initialize store location(s).
 *)

(*分配变量（int或指针或数组）：扩展环境以便
将变量映射到下一个可用的存储位置，以及
初始化存储位置。
*)

//

let rec allocate (typ, x) (env0, nextloc) sto0 : locEnv * store =

    let (nextloc1, v, sto1) =
        match typ with
        //数组 调用 initSto 分配 i 个空间
        // 长度是怎么样的。。 int 的长度和char 的长度有区别吗
        | TypA (t, Some i) -> (nextloc + i, nextloc, initSto nextloc i sto0)
        // 常规变量默认值是 0
        // 第二个是值
        // | TypFloat   -> (nextloc, (FLOAT 0.0),sto0)
        // | TypC   -> (nextloc,  (CHAR (char 0)),sto0)
        | _ -> (nextloc, 0, sto0)
        // 如果是char 呢

    msg $"\nalloc:\n {((typ, x), (env0, nextloc), sto0)}\n"
    bindVar x v (env0, nextloc1) sto1

(* Build global environment of variables and functions.  For global
   variables, store locations are reserved; for global functions, just
   add to global function environment.
*)

(*构建变量和函数的全局环境。对于全局
变量，存储位置被保留；对于全局函数，只需
添加到全局功能环境中。
*)

let typSize typ = 
    match typ with
    |  TypA (t, Some i) -> i
    |  TypString ->  128
    |  _ -> 1

type structEnv = (string *  paramdecs * int ) list

//初始化 解释器环境和store
let initEnvAndStore (topDecs: topdec list) : locEnv * funEnv * structEnv * store =

    //包括全局函数和全局变量
    // 这一步也没有运行
    // ERROR: parse error in file example\testDecAndAssign.c near line 4, column 13
    // 编译失败 解析失败 所以没有运行起来
    msg $"\ntopdecs:\n{topDecs}\n"
    // add var?
    let rec addv decs locEnv funEnv structEnv store =
        match decs with
        | [] -> (locEnv, funEnv, structEnv,store)

        // 全局变量声明  调用allocate 在store上给变量分配空间
        | Vardec (typ, x) :: decr ->
            let (locEnv1, sto1) = allocate (typ, x) locEnv store
            addv decr locEnv1 funEnv  structEnv sto1
        | VardecAndAssign (typ, assignMark, expr) :: decr ->
            let (locEnvAllocated, stoAlloced) = allocate (typ, assignMark) locEnv store
            printfn $"locEnvAllocated {locEnvAllocated}"
            printfn $"stoAlloced {stoAlloced}"
            addv decr locEnvAllocated funEnv structEnv stoAlloced
        //全局函数 将声明(f,(xs,body))添加到全局函数环境 funEnv
        | Fundec (_, f, xs, body) :: decr -> 
            addv decr locEnv ((f, (xs, body)) :: funEnv) structEnv store
        | Structdec (structName, memberList) :: decr ->
            printfn "Structdec"
            let rec getSize list structSize = 
                match list with
                | [] -> structSize
                | ( typ, memberName ):: tail -> getSize tail ((typSize (typ)) + structSize)
            let size = getSize memberList 0
            printfn $"size {size}"
            // structEnv 里面增加了
            addv decr locEnv funEnv ((structName, memberList, size) :: structEnv) store
    // ([], 0) []  默认全局环境
    // locEnv ([],0) 变量环境 ，变量定义为空列表[],下一个空闲地址为0
    // ([("n", 1); ("r", 0)], 2)  表示定义了 变量 n , r 下一个可以用的变量索引是 2
    // funEnv []   函数环境，函数定义为空列表[]
    addv topDecs ([], 0) [] [] emptyStore

(* ------------------------------------------------------------------- *)
//let printMap (map:Map<int,char>)=
let printMap (map:Map<int,int>)=
//    他保存的就是这个。。
//    但是 强转化吧
    for addr in map.Keys do
//        printfn $"({addr},{char map.[addr]})"
        printf $"({addr},{char map.[addr]}),"
(* Interpreting micro-C statements *)

let rec exec stmt (locEnv: locEnv) (gloEnv: gloEnv) structEnv (store: store)  : store =
    match stmt with
    | If (e, stmt1, stmt2) ->
        let (v, store1) = eval e locEnv gloEnv structEnv store

        if v <> 0 then
            exec stmt1 locEnv gloEnv  structEnv store1 //True分支
        else
            exec stmt2 locEnv gloEnv structEnv store1 //False分支
            // 不能写在这里吧
//     | Prim3(e, stmt1, stmt2) ->
//     // 他不应该返回 表达式 而是返回stmt 
//     // https://www.codenong.com/34315299/
// //     此表达式应具有类型
// //     “stmt”    
// // 而此处具有类型
// //     “expr”  
      
//         let (v, store1) = eval e locEnv gloEnv store

//         // if v <> 0 then
//         //     exec stmt1 locEnv gloEnv store1 //True分支
//         // else
//         //     exec stmt2 locEnv gloEnv store1 //False分支

//         // let (v, store1) = eval e locEnv gloEnv store

//         if v <> 0 then
//             // exec stmt1 locEnv gloEnv store1 //True分支
//             let (v, store2) = eval stmt1 locEnv gloEnv store
//             store2
//         else
//             // exec stmt2 locEnv gloEnv store1 //False分支
//             let (v, store3) = eval stmt2 locEnv gloEnv store
//             store3
// 这里是stmt 不是写在这
    // | PreInc acc -> 
    //     let (loc, storeAcced) = access acc locEnv gloEnv store
    //     let num = getSto storeAcced loc
    //     (num + 1, setSto storeAcced loc (num + 1)) 
    | While (e, body) ->

        //定义 While循环辅助函数 loop
        let rec loop store1 =
            //求值 循环条件,注意变更环境 store
            let (resCmped, store2) = eval e locEnv gloEnv structEnv store1
            // 求值  就是在更新变量，比如 while(i++) 就是i++ 的更新操作
            // 虽然看不懂 但是盲猜是在干这个

            // 继续循环
            // 0 就停止
            if resCmped <> 0 then
            // 返回的值 去做loop
                loop (exec body locEnv gloEnv structEnv store2)
                // 用更新的变量去做body 里的事情
                // 这里是有return 的
                // 没有retrun 的语言 看起来还是让人摸不着头脑
            else
                store2 //退出循环返回 环境store2

        loop store
    | Switch (expr, body) ->
        let (needVal, storeGot) = eval expr locEnv gloEnv structEnv store
        // 定义辅助函数 pickCase
        let rec pickCase caseList =
            match caseList with
            | Case (exprHead, headBody):: rest -> 
                let (caseV, caseStore) = eval exprHead locEnv gloEnv structEnv storeGot
                // 获取第一个值 不是的话 还是去后面找
                if caseV <> needVal then
                    pickCase rest
                else  // 执行case的body
                    exec headBody locEnv gloEnv structEnv caseStore
            | Default defaultBody :: rest->
                exec defaultBody locEnv gloEnv structEnv storeGot
            | [] -> storeGot
            // | _ -> failwith ("unknown grammar")
            
        pickCase body
    | Break -> store 
    | Continue -> store 
    
    | Expr e ->
        // _ 表示丢弃e的值,返回 变更后的环境store1
        let (_, store1) = eval e locEnv gloEnv  structEnv store
        store1

    | Block stmts ->

        // 语句块 解释辅助函数 loop
        let rec loop ss (locEnv, store) =
            match ss with
            | [] -> store
            //语句块,解释 第1条语句s1
            // 调用loop 用变更后的环境 解释后面的语句 sr.
            | s1 :: sr -> loop sr (stmtOrDec s1 locEnv gloEnv structEnv store)

        loop stmts (locEnv, store)

    // | Return _ -> failwith "return not implemented" // 解释器没有实现 return
    | Return e -> 
        // failwith "return not implemented" // 解释器没有实现 return
        match e with
        | Some e1 -> 
            // 把这个值放在key 为-1 的位置 之后好拿 -1 一般不会有东西 所以只有return 会用到
            // 变化之后的store 都是要返回的 因为和java不一样 他的store 不会再原地改 需要返回
            let (res, storeEvaled) = eval e1 locEnv gloEnv structEnv store;
            
            let storeAddedRetVal = storeEvaled.Add(-1, res);
            storeAddedRetVal
        | None -> store
    // 编译过了之后 这里才会没有黄色的  
    | For(assignedStmt,cmpStmt,updateStmt,body) -> 
          let (resAssigned ,storeAssigned) = eval assignedStmt locEnv gloEnv structEnv store
        //   获得初始值
          let rec loop storeOrigin =
                //求值 循环条件,注意变更环境 store
                // 这里是做判断 是for 的第二个参数  i<n
                let (resCmped, storeCmped) = eval cmpStmt locEnv gloEnv structEnv storeOrigin
                // 继续循环
                // 不是0 就不停止
                // body 里面可能也会改变变量的 比如 
                // for(i=0;i<n;i++){
                //     i++
                // }
                // 所以要返回body里面改变过的变量 
                // 去做一个更新操作 
                // 为什么只有他写成v 后面let 才不会 爆红啊
                // if resCmped<>0 then  这样就有问题
                // 必须要对齐
                if resCmped<>0 then 
                    let (updatedRes ,updatedStore) = eval updateStmt locEnv gloEnv structEnv (exec body locEnv gloEnv structEnv storeCmped)
                //   这里做了第三个参数的i++ 
                // 然后这个值可以放到loop里去做循环
                //    用更新的变量去做body 里的事情
                    loop updatedStore
                
                else storeCmped  
          loop storeAssigned
    | DoWhile(body,cmpStmt) -> 
        // 他要先做一个body的事情
      
        let rec loop storeOrigin =
                    //求值 循环条件,注意变更环境 store
                let (cmpedRes, cmpedStore) = eval cmpStmt locEnv gloEnv structEnv storeOrigin
                    // 继续循环 如果是 是 的话
                if cmpedRes<>0 then loop (exec body locEnv gloEnv structEnv cmpedStore)
                        else cmpedStore  //退出循环返回 环境cmpedStore
        loop (exec body locEnv gloEnv  structEnv store)
    | DoUntil(body,cmpStmt) -> 
      
      let rec loop storeOrigin =
                //求值 循环条件,注意变更环境 store
              let (cmpedRes, cmpedStore) = eval cmpStmt locEnv gloEnv structEnv storeOrigin
                // 继续循环
              if cmpedRes=0 then loop (exec body locEnv gloEnv structEnv cmpedStore)
                      else cmpedStore  //退出循环返回 环境cmpedStore
      loop (exec body locEnv gloEnv structEnv store)
    | Break -> failwith("break not done")
    // 利用报错来break 
    //   不管三七二十一 先做一下

    // 这是不对的
//     | Prim3(e1, e2 , e3) ->
//     // https://www.codenong.com/34315299/
// //     此表达式应具有类型
// //     “stmt”    
// // 而此处具有类型
// //     “expr”  

//         let (i1, store1) = eval e1 locEnv gloEnv store
//         // let (i2, store2) = eval e2 locEnv gloEnv store1
//         // let (i3, store3) = eval e3 locEnv gloEnv store2
//         if i1 = 0 then e2
//                   else e3


and stmtOrDec stmtOrDec locEnv gloEnv structEnv store =
    match stmtOrDec with
    | Stmt stmt -> (locEnv, exec stmt locEnv gloEnv structEnv store)
    | Dec (typ, x) -> allocate (typ, x) locEnv store
    | DecAndAssign (typ, x, e) ->
        let (locEnv1, store1) = allocate (typ, x) locEnv store
        let (res, store2) = eval (Assign(AccVar x, e)) locEnv1 gloEnv structEnv  store1
        (locEnv1, store2)

// and getTypeVal =
//     match getTypeVal with
(* Evaluating micro-C expressions *)
(*评估micro-C表达式*)
and eval e locEnv gloEnv structEnv  store : int * store =
    match e with
    // 这是定义一个函数还是什么 and
    | Access acc ->
    // 存取(计算机文件); 
        let (loc, store1) = access acc locEnv gloEnv structEnv store
        (getSto store1 loc, store1)
    | PreInc acc -> 
        let (loc, storeAcced) = access acc locEnv gloEnv  structEnv store
        let num = getSto storeAcced loc
        (num + 1, setSto storeAcced loc (num + 1)) 
    | PreDec  acc -> 
        let (loc, storeAcced) = access acc locEnv gloEnv  structEnv store
        // 先获取他的下标 再去栈里找他的值本身
        printfn $"loc {loc} storeAcced {storeAcced}"
        let num = getSto storeAcced loc
        (num - 1, setSto storeAcced loc (num - 1)) 
    | ToInt expr ->
        printfn $"ToInt  expr {expr}"
        // ToInt  expr Access (AccVar "flVal")
        let (codeVal, storeVal) = eval expr locEnv gloEnv structEnv  store
        // 获得是他的机器码对应的int形
        printfn $"ToInt  codeVal {codeVal} storeVal {storeVal}"
        // ToInt  floatVal 1166088458 storeVal map [(0, 10000
        // int 的最大值

        // ToInt  expr Access (AccVar "flVal")
        // ToInt  codeVal 100000001 storeVal map [(0, 100000001); (1, 100000001); (2, 100000001); ... ]
        // 他是 float 形的 v ToSingle 2.3122343E-35
        // 0  
        // int val :
        // let maxInt=int.MaxValue
        // https://blog.csdn.net/weixin_29384119/article/details/114110604
        // let maxInt=System.Int32.MaxValue
        let maxInt=1000000000
        // maxInt 2147483647
        //             1324618814
        // let maxInt= 100000000
        // 如果他超过了 int 的最大值 他就是 float 形式的 
        printfn $"maxInt {maxInt} "
        if abs( codeVal )> maxInt then // float
            let bytes = System.BitConverter.GetBytes(int32(codeVal))
            let v = System.BitConverter.ToSingle(bytes, 0)
            // float 转化 单精度，值太大
            printfn $"他是 float 形的 v ToSingle {v} "
            // 他是 float 形的 v ToSingle 4131.13
            // 如果他是float  先把机器码转成 float 再四舍五入
            let res = int(round(v))
            (res, storeVal)
        else
            (codeVal, storeVal)
    | ToChar expr ->
        // tochar  expr Access (AccVar "intVal")
        printfn $"tochar  expr {expr}"
        //通过 AccVar "intVal" 获取了值
        let (i, storeVal) = eval expr locEnv gloEnv structEnv store
        printfn $"tochar  i {i} storeVal {storeVal}"
        // tochar  i 100000001 storeVal map [(0, 100000001); (1, 0)]
        (i, storeVal)
        // let absI=abs (i)
        // if abs (i) > 100000000 then // float
        //     printfn $"absI {absI}"
        //     // 单精度 消掉 
        //     let bytes = System.BitConverter.GetBytes(int32(i))
        //     let v = System.BitConverter.ToSingle(bytes, 0)
        //     let res = int(round(v))
        //     (res, storeVal)
        // else
        //     (i, storeVal)
    | Assign (acc, e) ->
        let (loc, storeStart) = access acc locEnv gloEnv structEnv store
        // let (res, store2) = eval e locEnv gloEnv store1
        // (res, setSto store2 loc res)
        // printfn "Assign"
        let (res, store3) = 
            match e with
            | CstString s ->
            // | CstS s ->
                let mutable i = 0;
                let arrloc = getSto storeStart loc  // 数组起始地址
                // printf "i am arrayloc %d\n" arrloc
                let mutable storeNow = storeStart;
                while i < s.Length do
                // 下一个 char 
                    storeNow <- setSto storeNow (arrloc+i) (int (s.Chars(i)))
                    // printf "loc %d; " (arrloc+i)
                    // printf "assign %c\n"(s.Chars(i))
                    i <- i+1
                // printf "i am new arrayloc %d\n" (getSto store2 loc)
                // printf "i am new loc%d" loc
                (s.Length, storeNow)
            // | _ ->  eval e locEnv gloEnv structEnv storeStart
            | _ ->  eval e locEnv gloEnv structEnv storeStart
        (loc, setSto store3 loc res) 
    | OpAssign (op, acc, expr) ->
        let (loc, storeAcced) = access acc locEnv gloEnv structEnv store
        let originVal = getSto storeAcced loc
        let (sndVal, storeSnd) = eval expr locEnv gloEnv structEnv storeAcced
        // 列表里放入了 一个code 和 一个值
        let resValue =
            match op with
            | "+" -> originVal + sndVal
            | "-" -> originVal - sndVal
            | "*" -> originVal * sndVal
            | "/" -> originVal / sndVal
            | "%" -> originVal % sndVal
            | _ -> failwith ("unknown primitive " + op)
        (resValue, setSto storeSnd loc resValue)
    | CstI i -> (i, store)
    // | ConstChar c    -> (CHAR c, store)
    // | CstChar c   -> (CHAR c, store)
    // | CstChar c   -> (char c, store)
    // | CstChar c   -> ( c, store)
//    写了int 但是其实是toint 语义化挺差的 一个函数
//    | CstChar c   -> ( int c, store)
    | CstChar c   -> ( (int) c, store)
    // | CstString c    -> (  c, store)
    | CstString c    -> (  1, store)
    // | ConstFloat f    -> (FLOAT (float f),store)
    // | ConstFloat f    -> ( float (f),store)
    // | ConstFloat f    -> ( f,store)
    | ConstFloat f -> 
        let bytes = System.BitConverter.GetBytes(float32(f))
        let v = System.BitConverter.ToInt32(bytes, 0)
        (v, store)
    // 可以用CPar.fsy 声明的类型吗 但是报错啊
    // 拿进来什么store 出去就是什么store 
    // 那这里也随便返回一个1 可以吗
    // store参数有用还是 第一个参数呢
    // str 咋办 这样返回可以吗。。 正常吗 先试试
    // 返回 int 和store  : int * store
    // 此表达式应具有类型    “int”    而此处具有类型    “char” 
        //  D:\proj\compile\plzoofs\microc\Interp.fs(439,23): error FS0039: 未定义值或构造函数“CHAR”。 你可能需要以下之一:   char   CPar [D:\proj\compile\plzoofs\microc\interpc.fsproj]
    | Addr acc -> access acc locEnv gloEnv structEnv store
    // | Printf(op,e1)   ->
    //     let (i1, store1) = eval e1 locEnv gloEnv store
    //     let res = 
    //         match op with
    //         // 他没有可能打印str的呀 传进来就是int 。。
    //         | "%c"   -> (printf "%c " char i1; i1)
    //         | "%d"   -> (printf "%d " i1 ; i1)  
    //         | "%f"   -> (printf "%f " i1 ;i1 )
    //         | "%s"   -> (printf "%s " i1 ;i1 )
        // (res, store1)  
    | Prim1 (ope, e1) ->
    //  好像没人调用。。？
        let (i1, store1) = eval e1 locEnv gloEnv  structEnv store
        // 单原操作
        //  | NOT Expr                            { Prim1("!", $2)      }
        // 对应的这块代码就是说 参数的第二个 是他的e1  吧 ,应该没错了 可以对应起来，他的第一个是$1 没有$0 


        let res =
            match ope with
            | "!" -> if i1 = 0 then 1 else 0
            // 哦 确实 根据这个返回我应该知道他是个int了 
            // 这是赋值还是比较啊 是0 就是不等于
            | "printi" ->
                (printf "%d " i1
                 i1)
                //  但是这里是什么形式，这为啥是int 返回的是一个tuple吗，f# 有tuple吗
                // 第一个i1 应该是printf 的参数，但是第二个i1 是啥。。
                //  就是打印数字形式的 而且还返回？
            | "printc" ->
            // 这是强转？
                // (printf "%c" (char i1)
                //  i1)
                //  忽然想到，这不会只是个括号吧 就是个单纯的优先级的括号。。
                // 只是个括号 然后来恶心人的 不会吧。。
                // 也就是说这个括号去掉了也没事？
                // 也就是说他其实是。。 像下面这样子的，返回值是这个i1
                // char返回int 的话 也没什么问题 
                // 但是string要返回什么int  这里的int返回了 又给谁去调用
                // 返回的值会有影响吗
                printf "%c" (char i1)
                i1
                // 这样写我就理解了啊， 写成上面这样我以为返回个tuple还是什么。。
                // 而且f# 这个语言 网上资料都查不到 我也不知道他有没有元组
                // 也就是说 回车和缩进对这门语言是有影响的吗？。。 缩进确实，回车也不知道
                // printf "%c" (char i1) i1
                // D:\proj\compile\plzoofs\microc\Interp.fs(508,24): error FS0001: 类型“'a -> int”与类型“unit”不匹配 [D:\proj\compile\plzoofs\microc\interpc.fsproj]
                // 经过测试，printf "%c" (char i1) i1 这样写是不行的，这样确实是返回了个unit了，应该就是类似元祖的东西吧
                // 也就是他写在最后一行的就是返回值，所以他的回车对他的语言是有影响的
                // 也就是说，上面这个写法的结果一样吗。。
                // 因为啊。。第二个i1 如果是参数列表里的某个参数的话，也不是不可能啊，所以就会猜啊
                // 因为c 的printf 不是有很多参数吗，那么一般都会认为他是个参数吧
                // 这语言也没个return 根本看不懂啊,花了好多时间才理解这是个return的值啊
                // 而且拿个括号括起来更加让人以为他们是同一个函数里的东西了啊
                // 虽然说当作同一个函数的参数应该这样括才对啊..
                // printf ("%c" (char i1)
                // i1)
                // 说到底还是因为我太菜的缘故啊
                // 啊 是因为我太菜，没有看完语言的语法就来实战 是我的错。。
                // 不能大放厥词，都是因为我没有学好基础的缘故。。

                //  请问这是什么意思
                //  括号是干嘛。。
                //  printf 的结果是啥啊 为什么后面还有个i1 啊。。
                // f#  printf
                // 我怎么才能打印这个res 啊 ，这个res 是个什么类型我都不知道啊。。
            | "~" -> ~~~i1
            | _ -> failwith ("unknown primitive " + ope)
        // pri 
        //   D:\proj\compile\plzoofs\microc\Interp.fs(503,33): error FS0001: 此表达式应具有类型    “string”    而此处具有类型    “int”
        // 通过报错知道了他是个int 
        // printf "res 是什么 %s" res
        // printf "\n res 是什么 %d\n" res
        (res, store1)
        // 这个是返回元祖了
    | Prim3(e, stmt1, stmt2) ->
    // 是不是把他放前面 就行
    // 他不应该返回 表达式 而是返回stmt 
    // https://www.codenong.com/34315299/
//     此表达式应具有类型
//     “stmt”    
// 而此处具有类型
//     “expr”  
      
        let (v, store1) = eval e locEnv gloEnv structEnv store

        // if v <> 0 then
        //     exec stmt1 locEnv gloEnv store1 //True分支
        // else
        //     exec stmt2 locEnv gloEnv store1 //False分支

        // let (v, store1) = eval e locEnv gloEnv store

        if v <> 0 then
            // exec stmt1 locEnv gloEnv store1 //True分支
            let (v2, store2) = eval stmt1 locEnv gloEnv  structEnv store
            // store2
            (v2, store2)
        else
            // exec stmt2 locEnv gloEnv store1 //False分支
            let (v3, store3) = eval stmt2 locEnv gloEnv  structEnv store
            // store3
            (v3, store3)
    | Prim2 (ope, e1, e2) ->
    // 双原子操作
        let (i1, store1) = eval e1 locEnv gloEnv structEnv store
        // 去调用 可能会调用到 CstI 这个函数，获得int 的参数吧
        // 会去对比参数的 ,突然发现为什么要用f# 呢,因为他可以没有类型
        // 这样他就可以match参数的类型 如果java 要怎么写这个呢,强转? 可能比较麻烦吧
        // 虽然说因为我菜,这f# 这么难理解,我比较讨厌他 但是现在发现他确实在写解释器方面有比较好的特性吧
        let (i2, store2) = eval e2 locEnv gloEnv structEnv store1
        // 他getStore 获取了之后是放在元祖的前面还是后面一个
        // res 返回了什么、、
        // 加减乘除的结果
        let res =
            match ope with
            | "*" -> i1 * i2
            | "+" -> i1 + i2
            | "-" -> i1 - i2
            | "/" -> i1 / i2
            | "%" -> i1 % i2
            | "==" -> if i1 = i2 then 1 else 0
            | "!=" -> if i1 <> i2 then 1 else 0
            | "<" -> if i1 < i2 then 1 else 0
            | "<=" -> if i1 <= i2 then 1 else 0
            | ">=" -> if i1 >= i2 then 1 else 0
            | ">" -> if i1 > i2 then 1 else 0
            // 解释器映射到f# 语言的运行
            | "&" -> i1 &&& i2
            | "|" -> i1 ||| i2
            | "^" -> i1 ^^^ i2
            | "<<" -> i1 <<< i2
            | ">>" -> i1 >>> i2
            | _ -> failwith ("unknown primitive " + ope)
       
        (res, store2)
    // | PrintString (ope, e1) ->
    // | PrintString ( strEnv) ->
    // // 双原子操作
    //     // let (i1, store1) = eval strEnv locEnv gloEnv store
    //     // 他调用了什么 拿到的是int 啊
    //     // 对啊挺烦的 调用之后 第一个参数必然是int啊 
    //     // 是不是直接别调用了 传过来的就是str ？
    //     // 但是他又是store了个什么
    //     // 对于str 他第一个参数只能返回int啊 ，但是int 又是他需要打印的东西
    //     // 怎么从int 变成str呢，这不是很奇怪吗
    //     // 操作之后才能获得实际的 str 吗
    //     // let (i2, store2) = eval e2 locEnv gloEnv store1
    //     // 看不懂f# 。。

    //     // printf "%s"  i1
    //     printf "%s"  strEnv
    //     // 要返回int
   
    //     (1, store1)
        // 干脆随便返回个1 代表成功好了
//     | Prim3(e, stmt1, stmt2) ->
//     // 是不是把他放前面 就行
//     // 他不应该返回 表达式 而是返回stmt 
//     // https://www.codenong.com/34315299/
// //     此表达式应具有类型
// //     “stmt”    
// // 而此处具有类型
// //     “expr”  
      
//         let (v, store1) = eval e locEnv gloEnv structEnv store

//         // if v <> 0 then
//         //     exec stmt1 locEnv gloEnv store1 //True分支
//         // else
//         //     exec stmt2 locEnv gloEnv store1 //False分支

//         // let (v, store1) = eval e locEnv gloEnv store

//         if v <> 0 then
//             // exec stmt1 locEnv gloEnv store1 //True分支
//             let (v2, store2) = eval stmt1 locEnv gloEnv  structEnv store
//             // store2
//             (v2, store2)
//         else
//             // exec stmt2 locEnv gloEnv store1 //False分支
//             let (v3, store3) = eval stmt2 locEnv gloEnv  structEnv store
//             // store3
//             (v3, store3)
    // | Printf (s, exprs) ->
    //     let rec evalExprs exprs store1 =  // 循环计算printf后面所有表达式的值
    //         match exprs with
    //         | e :: tail ->  
    //             let (v, store2) = eval e locEnv gloEnv store1 
    //             let (vlist, store3) = evalExprs tail store2
    //             ([v] @ vlist, store3)
    //         | [] -> ([], store1)
    //     let (evals, store1) = evalExprs exprs store


    //     let getPrintString =
    //         let mutable i = 0
    //         let slist = s.Split('%')
    //         let mutable resString = slist.[0]
    //         let mutable i = 1
    //         while i < slist.Length do
    //             resString <- resString + evals.[i-1].ToString() + slist.[i].[1..]
    //             i <- i + 1
    //         printf "%s" resString
    //         1  // 返回1
    //     (getPrintString, store1)
    | Printf (formatStr, exprs) ->
        // let rec evalExprs exprs store1 =  // 循环计算printf后面所有表达式的值
        //     match exprs with
        //     | e :: tail ->  
        //         let (v, store2) = eval e locEnv gloEnv store1 
        //         let (vlist, store3) = evalExprs tail store2
        //         ([v] @ vlist, store3)
        //     | [] -> ([], store1)
        // let (evals, store1) = evalExprs exprs store
        // printf ("%s",s);
        // 只有一个表达式
        let evalOneExpr exprs storeExpr =  
        // 返回计算得到的值, 剩下的exprs, 新的store
            match exprs with
            | onlyOneHeadExpr :: rest ->
//                PrintUtil.printMap(storeExpr)
                // printfn "storeExpr"
                // PrintUtil.printMap(storeExpr)
//                PrintUtil.printMap(store)
            // 因为只有一个 所以取出来的 e 就是一个东西吧 tail 是什么都没有的吧
            // 这里也可以不叫head吗 随便什么名字，但是他拿出来的确实是第一个东西，可以这样理解吧
            // 根据表达式获取值 可能表达式不止是一个 str或者什么 还可能是 加法之类的，所以不能仅仅是转化为一个类型
//                refact 还是jetbrains的舒服  
                let (v, storeGot) = eval onlyOneHeadExpr locEnv gloEnv  structEnv storeExpr 
                // 看他是什么 如果是str 就是str 
                // let (vlist, store3) = evalExprs tail store2
                (v, rest, storeGot)
                // 先获取第一个参数 后面留给别人处理
            | [] -> failwith "few expression"
        
        ///获得第一个参数 比如 printf("%s %s",str0,str1)的str0
        let getOneExpr exprs storeExpr =  // 返回计算得到的值, 剩下的exprs, 新的store
            match exprs with
            | onlyOneHeadExpr :: rest ->  
                // let (loc, store2) = access (Access e) locEnv gloEnv store1
                (onlyOneHeadExpr, rest, storeExpr)
                // store 还是返回原来的store 那就没有必要了。。
//            空的数组
            | [] -> failwith "空的数组"

        // printfn $"传进来什么store {store}"
//        传进来什么store map [(0, 49); (1, 7); (2, 49); ... ]
        let mutable nowStore = store
        let getPrintString =
            let slist = formatStr.Split('%')
            // printfn "slist %A" slist
//         比如这样的函数   printf ("hello %s %s",s,s);
//            前面会有 hello 先拿出这个
            let mutable resString = slist.[0]
            // printfn "resString %s"  resString
            // printf ("%s %s",s,s);
            // slist [|""; "s "; "s"|]
            // 还没有开始解析这里 就是 字符串的 双引号的后面一个 就有问题了
            let mutable i = 1
            let mutable es = exprs
            // let mutable store1 = store
            while i < slist.Length do
                let getOneMarkStr =
                    let mark=slist.[i].[0]
                    match mark with
                    | 'd' -> 
                        let (intVal, tailRestExprs, oneExprGettedStore) = evalOneExpr exprs nowStore
                        // let intv = 1
                        // if e.GetType().IsEquivalentTo((1).GetType()) then  // 检查类型是否是int..但是现在存的都是int ?
                            // evals.[i-1].ToString()
                        es <- tailRestExprs
                        nowStore <- oneExprGettedStore
                        intVal.ToString()
                        // 返回
                    | 'c' -> 
                        let (intVal, tailRestExprs, oneExprGettedStore) = evalOneExpr exprs nowStore
                        // char(evals.[i-1]).ToString()
                        es <- tailRestExprs
                        nowStore <- oneExprGettedStore
                        char(intVal).ToString()
                    | 'f' -> 
                        let (intVal, exprs2, store2) = evalOneExpr exprs nowStore
                        es <- exprs2
                        nowStore <- store2
                        let bytes = System.BitConverter.GetBytes(intVal)
                        // https://docs.microsoft.com/zh-cn/dotnet/api/system.bitconverter.tosingle?view=net-6.0
                        let floatVal = System.BitConverter.ToSingle(bytes, 0)
                        // 转化为 单精度浮点数
                        // startIndex
                        // Int32
                        // value 内的起始位置  从0 开始
                        floatVal.ToString()
                    | 's' ->
                        // let (slen, exprs2, store2) = oneExpr exprs store1
                        // printf "%d" slen
//                        rider 有灰色的 代表没有用到 比较舒服
//                        照理说这里的store应该没有变化
//                        firStrArgName 变量名 比如 定义了 String  str ,那么变量名: 字面量 str
                        // printfn $"处理这个mark开始时候的 nowStore {nowStore}"
                        let (argName, tailRestExprs, charArrSto) = getOneExpr exprs nowStore
//                        获取表达式的时候 sto没有变吧
                        
//                        printfn "%s" e.ToString()
//                        printfn
//                        e expr 不会print 太多类型了
                        // printfn $"charArrSto {charArrSto}"
//                        store2 map [(0, 49); (1, 7); (2, 49); ... ]
//                        PrintUtil.printMap(store2)
                        // printMap(charArrSto)
                        // printfn ""
//                        前面下标 后面的是 值 是char 类型的，因为是str嘛
//                        (0,1),(1,),(2,1),(3,4),(4,1),(5,4),(6,1),
//                        store 里面是这么回事

                        // printfn $"firStrArgName {argName}"
//                        他是个参数名字啊
//                        firStrArg Access (AccVar "s")

                        ///loc 是store的最后一个元素的下标
                        let (arrLenKey, store3) = 
                            match argName with
                            // | Access acc -> access acc locEnv gloEnv structEnv store
                            | Access acc -> 
                                let argVal=access acc locEnv gloEnv structEnv  store
//                                获取s变量的值
//                                可以获取
//                                let  fst accVal
                                let (arrLenKey,argStore) =argVal
//                                因为我定义的str的fst 是个1 永远是1  代表成功 所以都是1 这个fstVal 应该没用。。
//不是啊 这是acc 不是我定义的那个cststring  不一样的。。所以这不是isOK

//                                printfn $"fstVal %d, store : {store}"
//                                printfn $"""fstVal {fstVal}, store : {store}"""
                                // printfn $"arrLenKey {arrLenKey}, argStore : {argStore}"
//                                Invalid interpolated string. Interpolated strings may not use '%' format specifiers unless each is given an expression, e.g. '%d{1+1}'
//                                printfn "可以获取 %s" accVal.ToString()
                                argVal
                            
                            // 获取他的位置
                            | _ -> failwith "not support expression"
                        // printf "i m loc2 %d\n" loc
                        ///数组长度
                        /// 为啥可以得到长度
                        /// 这应该是之前放好的 ，套娃的，所以拿出来可以
                        /// charArrSto 虽然装了char Arr 但是貌似把 数组的长度放在了 idx=1 的地方
                        /// 可能是哪里写错了。。 然而能跑 不是很懂啊
                        /// 还是说 长度放在第二个地方 是正常的 就是他的数据结构？
                        let charArrLen = getSto charArrSto arrLenKey
//                        (0,1),(1,),(2,1),(3,4),(4,1),(5,4),(6,1),
//                        怪不得 1 的val是空的 ，应该是7对应的char 是个控制符还是啥的 不可见
                        // printfn  $"arrLenKey {arrLenKey}"
//                        他可以知道arr的长度
                        // printfn $"charArrLen {charArrLen}"
                        // printf "i am arrayloc2 %d\n" arrloc
                        // let (loc, store1) = access AccVar e locEnv gloEnv store
                        // let arrloc = getSto store1 loc
                        // es <- tailRestExprs
                        nowStore <- charArrSto
                        // printfn $"处理过一个mark之后的sto {nowStore}"
                        // let mutable store2 = store1;
                        let mutable i = 0;
                        let mutable s = ""
                        while i < charArrLen do
                            // s <- s + char(getSto store2 (arrloc-i)).ToString()
                            let charIdx=charArrLen-i-1
                            // printfn  $"charIdx {charIdx}"
//                            printfn $"charIdx {charIdx}"
//                            123456
//                            len-0-1 是len-1  是str的最后一个 所以要-1 ，不-1 的话，len 这个下标是没有字符的
//                            s <- char(getSto store2 (arrLoc-i-1)).ToString() + s
//                            因为这是个栈 他的每个字符都是放到栈里的，先出来的是最后的
//                            ""
//                            6+""
//                            56""
                            s <- char(getSto charArrSto charIdx).ToString() + s
                            i <- i+1
                        // printf "i m s %s\n" s
                        s
                    | _ -> failwith "format mismatch"
                let restStr= slist.[i].[1..]
//                restStr  你好 每一个mark 后面都有跟着的东西
//                printf ("hello %s 你好 %s 你好ok啊",s,s);
//                比如这一串  '  你好 '
//                比如'  你好ok啊' ，也就是 %s 之后的所有，包括空格
//                printfn $"restStr {restStr}"
//                resString <- resString + printOneMark + slist.[i].[1..]
                resString <- resString + getOneMarkStr + restStr
//                积累在前面
                i <- i + 1
            printf "%s" resString
            1  // 返回1
        (getPrintString, nowStore)
    | Andalso (e1, e2) ->
    // if (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) 
    // 逻辑and 逻辑或者
        let (i1, store1) as res = eval e1 locEnv gloEnv structEnv store
        // 0 走的else 逻辑
        if i1 <> 0 then
            // 通过了 i1 是的 ,去比较e2 是不是
            eval e2 locEnv gloEnv  structEnv store1
        else
            res
    | Orelse (e1, e2) ->
        let (i1, store1) as res = eval e1 locEnv gloEnv  structEnv store

        if i1 <> 0 then
            res
        else
            eval e2 locEnv gloEnv structEnv store1
    | Call (f, es) -> callfun f es locEnv gloEnv structEnv store

(*
获取值 int ， 或者下一个下标
*)
///获取值 int ， 或者下一个下标
/// https://docs.microsoft.com/zh-cn/dotnet/fsharp/language-reference/xml-documentation
and access acc locEnv gloEnv structEnv store : int * store =
    match acc with
//    这是干嘛 不是强转吧 lookup 需要两个参数 acc返回两个参数
//    这里是返回了元祖  应该没问题，store 没有变化 直接返回了
//    fst 是个啥 看来是个函数，获取元组的第一个
//    f# 他是个函数 还是个造出来的变量，一下子看不出来，甚至你在rider里都看不出来
//    心力憔悴  没事了 rider里给他设置func的颜色就行了。。 默认都是黑色的。。
//    locEnv 是个 元组 fst是名字，snd是值
    ///获取了他的 环境 想要找x
    | AccVar x ->
//        这几个都没调用几次啊。。
//数组里面没有调用他们
        // printfn $"(x {x})"
//        这个是 s 调用了 难道调用了指针。。
//        printfn $"(locEnv {locEnv})"
        
//        let  fstLocEnv= fst locEnv
        let  stackTopLocEnv= fst locEnv
//        printfn $"(stackTopLocEnv {stackTopLocEnv})"
//        (locEnv ([(s, 1); (n, 0)], 2))
//        (fstLocEnv [(s, 1); (n, 0)])
//这个栈的环境 说明我们在main函数里面
//        也就是现在栈顶的 locEnv的意思吧
//确实 之前还以为是列表 其实他是一个个元组的套娃，栈顶的环境会在fst 应该是这个结构才对

//        let value =lookup (fst locEnv) x
        let keyInNextSto =lookup stackTopLocEnv x
//        应该是找到了 str这个字面量  他要去store里面用哪个idx去找
//他的value 是下一个要找的store里面的idx 或者说key 
//        (lookup (fst locEnv) x, store)
//        还是这样写 比较容易看懂
        (keyInNextSto, store)
//    文档：指针解引用.note
//链接：http://note.youdao.com/noteshare?id=dec59092b9a0bde403fd4c3643424b07&sub=3271B2F5EB244B23964DDB2BACBCB819
    | AccDeref e ->
//        printfn $"(e {e})"
        eval e locEnv gloEnv  structEnv store
    | AccIndex (acc, idx) ->
        printfn "AccIndex"
        let (a, store1) = access acc locEnv gloEnv structEnv store
//        这里store1会改变吗 如果之前弄了AccIndex ，确实会改变的吧
        printfn $"({a}, {store1})"
//        没有调用到 竟然
        let aval = getSto store1 a
//        printfn $"(aval {aval})"
        let (i, store2) = eval idx locEnv gloEnv structEnv store1
        (aval + i, store2)

and evals es locEnv gloEnv  structEnv store : int list * store =
    match es with
    | [] -> ([], store)
    | e1 :: er ->
        let (v1, store1) = eval e1 locEnv gloEnv structEnv store
        let (vr, storer) = evals er locEnv gloEnv structEnv store1
        (v1 :: vr, storer)

and callfun f es locEnv gloEnv structEnv store : int * store =

    msg
    <| sprintf "callfun: %A\n" (f, locEnv, gloEnv, store)

    let (_, nextloc) = locEnv
    let (varEnv, funEnv) = gloEnv
    let (paramdecs, fBody) = lookup funEnv f
    let (vs, store1) = evals es locEnv gloEnv structEnv store

    let (fBodyEnv, store2) =
        bindVars (List.map snd paramdecs) vs (varEnv, nextloc) store1

    let store3 = exec fBody fBodyEnv gloEnv structEnv store2
    // (-111, store3)
    let res = store3.TryFind(-1) 
    // 刚才存在-1 了
    // return 的时候 -1 有东西就返回
    let restore = store3.Remove(-1)
    // 一个新的store 因为他删除之后会返回一个 不返回的话 原来的不会变的
    match res with
    | None -> (0,restore)
    | Some i -> (i,restore)

(* Interpret a complete micro-C program by initializing the store
   and global environments, then invoking its `main' function.
 *)

// run 返回的结果是 代表内存更改的 store 类型
// vs 参数列表 [8,2,...]
// 可以为空 []
let run (Prog topDecs) args =
    //
    printfn $"topDecs {topDecs}"
    let ((varEnv, nextloc), funEnv, structEnv,storeInited) = initEnvAndStore topDecs
    printfn $"varEnv {varEnv}"
    printfn $"nextloc {nextloc}"
    // mainParams 是 main 的参数列表
    printfn $"funEnv {funEnv}"
    printfn $"storeInited {storeInited}"
    //
    let (mainParams, mainBody) = lookup funEnv "main"

    let (mainBodyEnv, store1) =
        bindVars (List.map snd mainParams) args (varEnv, nextloc) storeInited


    msg
    <|

    //以ex9.c为例子
    // main的 AST
    sprintf "\nmainBody:\n %A\n" mainBody
    +

    //局部环境
    // 如
    // i 存储在store位置0,store中下个空闲位置是1
    //([("i", 0)], 1)

    sprintf "\nmainBodyEnv:\n %A\n" mainBodyEnv
    +

    //全局环境 (变量,函数定义)
    // fac 的AST
    // main的 AST
    sprintf $"\n varEnv:\n {varEnv} \nfunEnv:\n{funEnv}\n"
    +

    //当前存储
    // store 中 0 号 位置存储值为8
    // map [(0, 8)]
    sprintf "\nstore1:\n %A\n" store1

    let endstore =
        exec mainBody mainBodyEnv (varEnv, funEnv) structEnv  store1

    msg $"\nvarEnv:\n{varEnv}\n"
    msg $"\nStore:\n"
    msg <| store2str endstore

    endstore

(* Example programs are found in the files ex1.c, ex2.c, etc *)
