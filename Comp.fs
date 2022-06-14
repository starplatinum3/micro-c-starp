(* File MicroC/Comp.fs
   A compiler from micro-C, a sublanguage of the C language, to an
   abstract machine.  Direct (forwards) compilation without
   optimization of jumps to jumps, tail-calls etc.
   sestoft@itu.dk * 2009-09-23, 2011-11-10

   A value is an integer; it may represent an integer or a pointer,
   where a pointer is just an address in the store (of a variable or
   pointer or the base address of an array).

   The compile-time environment maps a global variable to a fixed
   store address, and maps a local variable to an offset into the
   current stack frame, relative to its bottom.  The run-time store
   maps a location to an integer.  This freely permits pointer
   arithmetics, as in real C.  A compile-time function environment
   maps a function name to a code label.  In the generated code,
   labels are replaced by absolute code addresses.

   Expressions can have side effects.  A function takes a list of
   typed arguments and may optionally return a result.

   Arrays can be one-dimensional and constant-size only.  For
   simplicity, we represent an array as a variable which holds the
   address of the first array element.  This is consistent with the
   way array-type parameters are handled in C, but not with the way
   array-type variables are handled.  Actually, this was how B (the
   predecessor of C) represented array variables.

   The store behaves as a stack, so all data except global variables
   are stack allocated: variables, function parameters and arrays.
*)

(*文件micro/Comp.fs）
从C语言的子语言micro-C到
抽象机器。直接（转发）编译，无需
跳转到跳转、尾部呼叫等的优化。
sestoft@itu.dk * 2009-09-23, 2011-11-10
值是一个整数；它可以表示整数或指针，
其中指针只是存储区中的地址（变量或
指针或数组的基址）。
编译时环境将全局变量映射到固定变量
存储地址，并将局部变量映射到
当前堆栈帧，相对于其底部。运行时商店
将位置映射为整数。这完全允许指针
算术，如在real C中。编译时函数环境
将函数名映射到代码标签。在生成的代码中，
标签被绝对代码地址取代。
表情可能有副作用。函数获取
类型化参数，并可以选择返回结果。
数组只能是一维且大小不变。对于
简单来说，我们将数组表示为一个变量，该变量包含
第一个数组元素的地址。这与
在C中处理数组类型参数的方式，但不使用
处理数组类型的变量。实际上，这就是
C）的前身表示数组变量。
存储的行为就像一个堆栈，所以除了全局变量之外，所有数据
是堆栈分配的：变量、函数参数和数组。
*)

module Comp

open System.IO
open Absyn
open Machine
open Debug
open Backend

(* ------------------------------------------------------------------- *)

(* Simple environment operations *)

//https://docs.microsoft.com/zh-cn/dotnet/fsharp/language-reference/generics/automatic-generalization
//	指示泛型类型参数。
type 'data Env = (string * 'data) list

let rec lookup env x =
    match env with
    | [] -> failwith (x + " not found")
    | (headName, v) :: yRest -> if x = headName then v else lookup yRest x

(* A global variable has an absolute address, a local one has an offset: 
全局变量具有绝对地址，局部变量具有偏移量：*)

type Var =
//    他是一个int 
    | Glovar of int (* absolute address in stack  堆栈中的绝对地址         *)
    | Locvar of int (* address relative to bottom of frame 相对于帧底部的地址 *)
    | StructMemberLoc of int


let rec structLookupVar env x lastloc =
    match env with
    | []                            -> failwith(x + " not found")
    | (name, (loc, typ))::rest         -> 
        if x = name then 
            match typ with
            | TypA (_, _)  -> StructMemberLoc (lastloc+1)
            | _                 -> loc 
        else
        match loc with
//        去别的里面找
        | StructMemberLoc lastLocOther -> structLookupVar rest x lastLocOther

type StructTypeEnv = (string * (Var * typ) Env * int) list 

(* The variable environment keeps track of global and local variables, and
   keeps track of next available offset for local variables 
   
ex1.c下面的的全局声明

int g ;
int h[3] 

构造的环境如下：

h 是整型数组，长度为 3，g是整数，下一个空闲位置是 5

([("h", (Glovar 4, TypA (TypI, Some 3)));
 ("g", (Glovar 0, TypI))], 5)  

实际存储布局如下：
 (0,0)(1,0)(2,0)(3,0) (4,1) ...... 
*)

type VarEnv = (Var * typ) Env * int

//代码指令列表
type LabEnv = label list

(* The function environment maps function name to label and parameter decs
函数环境将函数名映射到标签和参数decs *)

type Paramdecs = (typ * string) list

type FunEnv = (label * typ option * Paramdecs) Env

let isX86Instr = ref false

//在环境env上查找名称为x的结构体
let rec structLookup env x =
    match env with
    | []                            -> failwith(x + " not found")
    | (name, argList, size)::rest    -> if x = name then (name, argList, size) else structLookup rest x


(* Bind declared parameters in env: *)
let x86patch code =
    if !isX86Instr then
        code @ [ CSTI -8; MUL ] // x86 偏移地址*8
    else
        code 
let bindParam (env, newloc) (typ, x) : VarEnv =
    ((x, (Locvar newloc, typ)) :: env, newloc + 1)

let bindParams paras ((env, newLoc): VarEnv) : VarEnv = List.fold bindParam (env, newLoc) paras

(* Bind declared variable in env and generate code to allocate it: *)
// kind : Glovar / Locvar
let rec allocateWithMsg (kind: int -> Var) (typ, x) (varEnv: VarEnv)(structEnv : StructTypeEnv) =
    let varEnv, instrs =
        allocate (kind: int -> Var) (typ, x) (varEnv: VarEnv)  (structEnv : StructTypeEnv)

    msg
    <| "\nalloc\n"
       + sprintf "%A\n" varEnv
       + sprintf "%A\n" instrs

    (varEnv, instrs)


//结构体要传入结构体的环境 所有函数都要改签名
and allocate (kind: int -> Var) (typ, x) (varEnv: VarEnv) (structEnv : StructTypeEnv): VarEnv * instr list =
//https://docs.microsoft.com/zh-cn/dotnet/fsharp/language-reference/functions/
//    说明kind 是个函数 他的参数是 int 返回是var
//    kind(int){
//    return Var;
//    } 
    msg $"allocate called!{(x, typ)}"

    let (env, newloc) = varEnv

    match typ with
    | TypA (TypA _, _) -> raise (Failure "allocate: array of arrays not permitted")
    | TypA (t, Some index) ->
//        | TypA of typ * int option
//int 是不一定有的 int 是他的下标
//        let nextVar=kind (newloc + i)
        let varOfIdxI=kind (newloc + index)
        let newEnv =
//            ((x, (kind (newloc + i), typ)) :: env, newloc + i + 1) //数组内容占用 i个位置,数组变量占用1个位置
            ((x, (varOfIdxI, typ)) :: env, newloc + index + 1) //数组内容占用 i个位置,数组变量占用1个位置

        let code = [ INCSP index; GETSP; OFFSET(index - 1); SUB ]
//        https://blog.csdn.net/cherish_xmm/article/details/50119737
//        偏移地址  x86 
//        获取栈顶
        // info (fun () -> printf "new varEnv: %A\n" newEnv)
        (newEnv, code)
    | TypString ->
    //  int -> Var
        let value=kind (newloc+128)
//        let newEnv = ((x, (kind (newloc+128), typ)) :: env, newloc+128+1)
        let newEnv = ((x, (value, typ)) :: env, newloc+128+1)
        let code = [INCSP 128; GETSP; CSTI (128-1); SUB]
//        开辟空间  开栈
        (newEnv, code)
    | TypStruct structName ->
        let (name, argslist, size) = structLookup structEnv structName
        let code = [INCSP (size + 1); GETSP; CSTI (size); SUB]
        let newEnvr = ((x, (kind (newloc + size + 1), typ)) :: env, newloc+size+1+1)
        (newEnvr, code)
    | _ ->
        let newEnv =
            ((x, (kind (newloc), typ)) :: env, newloc + 1)

        let code = [ INCSP 1 ]

        // info (fun () -> printf "new varEnv: %A\n" newEnv) // 调试 显示分配后环境变化
        (newEnv, code)

//
//and allocate (kind: int -> Var) (typ, x) (varEnv: VarEnv) : VarEnv * instr list =
//
//    msg $"allocate called!{(x, typ)}"
// 
//    // newloc 下个空闲存储位置
//    let (env, newloc) = varEnv
//
//    match typ with
//    | TypA (TypA _, _) -> raise (Failure "allocate: array of arrays not permitted")
//    | TypA (t, Some i) ->
//        let newEnv =
//        // 空闲到+i的位置  某种类型的 
//            ((x, (kind (newloc + i), typ)) :: env, newloc + i + 1) //数组内容占用 i个位置,数组变量占用1个位置
//
//        let code = [ INCSP i; GETSP; OFFSET(i - 1); SUB ]
//        // info (fun () -> printf "new varEnv: %A\n" newEnv)
//        (newEnv, code)
//    | _ ->
//        let newEnv =
//            ((x, (kind (newloc), typ)) :: env, newloc + 1)
//
//        let code = [ INCSP 1 ]
//
//        // info (fun () -> printf "new varEnv: %A\n" newEnv) // 调试 显示分配后环境变化
//        
//        (newEnv, code)


(* ------------------------------------------------------------------- *)

(* Build environments for global variables and functions *)

// let makeGlobalEnvs (topdecs: topdec list) : VarEnv * FunEnv * instr list =
//     let rec addv decs varEnv funEnv =

//         msg $"\nGlobal funEnv:\n{funEnv}\n"

//         match decs with
//         | [] -> (varEnv, funEnv, [])
//         | dec :: decr ->
//             match dec with
//             | Vardec (typ, var) ->
//                 let (varEnv1, code1) = allocateWithMsg Glovar (typ, var) varEnv
//                 let (varEnvr, funEnvr, coder) = addv decr varEnv1 funEnv
//                 (varEnvr, funEnvr, code1 @ coder)
//             | Fundec (tyOpt, f, xs, body) -> addv decr varEnv ((f, ($"{newLabel ()}_{f}", tyOpt, xs)) :: funEnv)
//         | Structdec (typName, typEntry) -> 
//                 let structTypEnv1 = makeStructEnvs typName typEntry structTypEnv
//                 let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv funEnv structTypEnv1
//                 (varEnvr, funEnvr, structTypEnvr, coder)
//     addv topdecs ([], 0) []

//let makeGlobalEnvs (topdecs: topdec list) : VarEnv * FunEnv * StructTypeEnv * instr list =
//    let rec addv decs varEnv funEnv structTypEnv =
//
//        msg $"\nGlobal funEnv:\n{funEnv}\n"
//
//        match decs with
//        | [] -> (varEnv, funEnv,structTypEnv, [])
//        | dec :: decr ->
//            match dec with
//            | Vardec (typ, var) ->
//                // let (varEnv1, code1) = allocateWithMsg Glovar (typ, var) varEnv structTypEnv
//                // let (varEnvr, funEnvr, coder) = addv decr varEnv1 funEnv
//                // (varEnvr, funEnvr,structTypEnv, code1 @ coder)
//                let (varEnv1, code1) = allocate Glovar (typ, x) varEnv structTypEnv
//                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv1 funEnv structTypEnv
//                (varEnvr, funEnvr, structTypEnvr, code1 @ coder)
//           
//            | Fundec (tyOpt, f, xs, body) -> addv decr varEnv ((f, ($"{newLabel ()}_{f}", tyOpt, xs)) :: funEnv)
//        | Structdec (typName, typEntry) -> 
//                // let structTypEnv1 = makeStructEnvs typName typEntry structTypEnv
//                // let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv funEnv structTypEnv1
//                // (varEnvr, funEnvr, structTypEnvr, coder)
//
//                let structTypEnv1 = makeStructEnvs typName typEntry structTypEnv
//                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv funEnv structTypEnv1
//                (varEnvr, funEnvr, structTypEnvr, coder)
//    addv topdecs ([], 0) [] []




(*
    生成 x86 代码，局部地址偏移 *8 ，因为 x86栈上 8个字节表示一个 堆栈的 slot槽位
    栈式虚拟机 无须考虑，每个栈位保存一个变量
*)

(* ------------------------------------------------------------------- *)

(* Compiling micro-C statements:
   * stmt    is the statement to compile
   * varenv  is the local and global variable environment
   * funEnv  is the global function environment

   编译micro-C语句：
*stmt是要编译的语句
*varenv是局部和全局变量环境
*FUNEV是全球功能环境
*)

let mutable lablist : label list = []

let rec headlab labs = 
    match labs with
        | head :: rest -> head
        // 头上的 
        // 如果是空的列表 是报错
        // | []        -> failwith "Error: No Label,你需要先定义标志位置 我才可以跳转"
        | []        -> failwith "Error: No Label,列表是空的 拿不出第一个标志"

// 删除第一个标志 也就是弹栈
// f# 不是原地tp 需要返回剩下的列表
let rec popFirst labs =
    match labs with
        | lab :: restLabels ->   restLabels
        // 获得tail的 列表
        | []        ->   []


// continue 跳转 while 循环 每次都储存一个label ，continue的时候就跳过现在的lab
let rec cStmt stmt (varEnv: VarEnv) (funEnv: FunEnv) (structEnv : StructTypeEnv) : instr list =
    match stmt with
    | If (e, stmt1, stmt2) ->
        let labelse = newLabel ()
        let labend = newLabel ()

        cExpr e varEnv funEnv structEnv
//        if 条件不满足  就是else
//        go to label if s[sp] == 0
//        他是 goto了 
        @ [ IFZERO labelse ]
//          如果else 是0 那么就是不是else 那么就是执行stmt
          @ cStmt stmt1 varEnv funEnv structEnv
            @ [ GOTO labend ]
              @ [ Label labelse ]
//                这块的标记是走的 else
                @ cStmt stmt2 varEnv funEnv  structEnv
                  @ [ Label labend ] 
    | Switch (e, body) ->
        let eValueInstr = cExpr e varEnv funEnv structEnv
        let endLab = newLabel()
        let rec getCode e body = 
            match body with
            | []          -> [INCSP 0]
            | Case (e1, body1):: tail -> 
              let e1ValueInstr = cExpr e1 varEnv funEnv structEnv
              let sublab = newLabel()
            
              eValueInstr @ e1ValueInstr @ [SUB] 
              //   如果是的 跳转 sub 
              @ [IFNZRO sublab] 
              @ cStmt body1 varEnv funEnv structEnv 
              //   结束
              @ [GOTO endLab] 
            
              @ [Label sublab] @ getCode e tail
            | Default body1 :: tail->
                cStmt body1 varEnv funEnv structEnv
      
        let result = getCode e body @ [Label endLab]
        result
    | While (e, body) ->
        let labBegin = newLabel ()
        // 创造了 lab 
        let labTest = newLabel ()
        let labEnd = newLabel ()
        // 约定把 labEnd 放在数组的开头 因为break的时候 需要弹栈  弹出的第一个就是labEnd
        lablist <- [labEnd; labTest; labBegin] @ lablist
        // 他会打印好多的 5 为什么 因为要记录结束的lab才行啊 如果是break的话
        // 要跳到 结束的lab ，如果他没有记录结束的lab 只是记录了循环中的lab
        // 他就会一直在循环里面了
        //  @ 就是贴贴 列表合并
        let instrList=
            [ GOTO labTest; Label labBegin ]
            @ cStmt body varEnv funEnv structEnv
            @ [ Label labTest ]
            // 去测试 表达式是不是真的
            //   记录当前 (标签, 地址) ==> 到 labenv中
                @ cExpr e varEnv funEnv  structEnv
                // 计算表达式 ===0 说明是真的 !=0 说明是假的 就是会继续
                // 跳到开头 继续循环
                // 不然的话 就不会有跳到开头的指令 就会顺序下去 就是结束了循环
                @ [ IFNZRO labBegin;Label labEnd ]
                // 标记一下 这里是循环结束的 标志了
                //   如果不是0 跳转 begin 
                // while(true){
    //              if(i==0){

    //                }
                // }
        lablist <- popFirst lablist
        lablist <- popFirst lablist
        lablist <- popFirst lablist
        // 删除了 开头的lab  退出这个环境
        instrList

    | Expr e -> cExpr e varEnv funEnv structEnv
                @ [ INCSP -1 ]
    | Block stmts ->

        let rec loop stmts varEnv =
            match stmts with
            | [] -> (snd varEnv, [])
            | s1 :: sr ->
                let (varEnv1, code1) = cStmtOrDec s1 varEnv funEnv structEnv
                let (fdepthr, coder) = loop sr varEnv1
                (fdepthr, code1 @ coder)

        let (fdepthend, code) = loop stmts varEnv

        code @ [ INCSP(snd varEnv - fdepthend) ]
    | Try(stmt,catches)  ->
        let ExceptionRes= Exception "ArithmeticalException"
//        let exceptions = [Exception "ArithmeticalException"]
        printfn $" ExceptionRes {ExceptionRes}"
        let exceptions = [ExceptionRes]
        let rec lookupException ex (exs:excep list) exDepth=
            match exs with
            | head :: rest -> if ex = head then exDepth else lookupException ex rest exDepth+1
            | []-> -1
        // let (labend, C1) = addLabel C
        let labEnd = newLabel ()
        // let lablist = labend :: lablist
        // lablist <- [ labend ] @ lablist
        let (env, fDepth) = varEnv
        let varEnv = (env, fDepth+3*catches.Length)
        let (tryInstr,varEnv) = tryStmt stmt varEnv funEnv structEnv
        let rec everyCatch catch  = 
            match catch with
            | [Catch(exp, body)] -> 
                let exNum = lookupException exp exceptions 1
                let catchcode = cStmt body varEnv funEnv structEnv
                let labcatch = 
                    // addLabel( cStmt body varEnv funEnv lablist structEnv [])
                    newLabel()
                // let lablist = label :: lablist
                
                let trycode = PUSHHDLR (exNum, labcatch) :: tryInstr @ [POPHDLR; Label labcatch]
                (catchcode, trycode)
            | Catch(exn,body) :: tr->
                let exnum = lookupException exn exceptions 1
                let (C2, C3) = everyCatch tr
                // let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv C2)
                let catchCode = C2 @ cStmt body varEnv funEnv structEnv
                let labcatch = newLabel()
                // let trycode = PUSHHDLR (exnum, labcatch) :: C3 @ [POPHDLR]
//                把 ex push 进去
                let trycode = PUSHHDLR (exnum, labcatch) :: C3 @ [POPHDLR; Label labcatch]
//                catch的 pop出来
                (catchCode, trycode @ [Label labcatch])
            | [] -> ([], tryInstr)
        let (catchCode, tryCode) = everyCatch catches
        printfn $"catchCode {catchCode}"
        printfn $"tryCode {tryCode}"
        tryCode @ catchCode @ [Label labEnd]
    | Return None -> [ RET(snd varEnv - 1) ]
    | Return (Some e) ->
                         cExpr e varEnv funEnv structEnv
                         @ [ RET(snd varEnv) ]
    | Break -> 
    //     let labend = newLabel ()
        let labend = headlab lablist
        printfn $"lablist {lablist}"
        printfn $"labend {labend}"
        // 获得头 去头
        [GOTO labend]
 // | Continue -> 
    // //     // let labbegin = newLabel ()
    //     let lablist   = dellab lablist
    //     let labbegin = headlab lablist
    //     [GOTO labbegin]
    | Continue -> 
    //     // let labbegin = newLabel ()
        let lablist   = popFirst lablist
        // 不要lab End 
        // 去掉头 这次的不要
        let labTest = headlab lablist
        // 继续去计算 表达式 ，测试表达式是否符合
        printfn  "do continue"
        printfn $"lablist {lablist}"
        printfn $"labTest {labTest}"
        [GOTO labTest]
        // 继续 如果过了测试 就循环继续做



    | DoUntil (body, e) ->
        let labBegin = newLabel ()
        let labTest = newLabel ()
        let labEnd = newLabel ()
        lablist <- [labEnd; labTest; labBegin] @ lablist

        let instrStack = 
            cStmt body varEnv funEnv structEnv
            // 不管三七二十一 先do一下
            @[ GOTO labTest; Label labBegin ]
              @ cStmt body varEnv funEnv structEnv
                @ [ Label labTest ]
                // 这块地方是检查表达式是不是true了
                  @ cExpr e varEnv funEnv structEnv 
                    @ [ IFZERO labBegin; Label labEnd ]
                    // if ==0 回去 就是false 回去，是true的话 就退出了 因为是until
                    // 标志一下 最终位置

        lablist <- popFirst lablist
        lablist <- popFirst lablist
        lablist <- popFirst lablist
        // 退出环境
        // 返回指令
        instrStack

//and的话 定义再下面也行
and makeGlobalEnvs(topdecs : topdec list) : VarEnv * FunEnv * StructTypeEnv * instr list =
    printfn $"topdecs {topdecs}"
    let rec addv decs varEnv funEnv structTypEnv =
        match decs with
        | [] -> (varEnv, funEnv, structTypEnv, [])
        | dec::decRest ->
//            拿出第一个定义
            match dec with
            | Vardec (typ, x) ->
//                他是int 但是可以映射成为var 吗 Glovar
                let (varEnvAfterAloc, codeAfterAloc) = allocate Glovar (typ, x) varEnv structTypEnv
//                申请了一块内存 给这个变量定义
//                r 就是rest
//                去定义剩下的 
                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decRest varEnvAfterAloc funEnv structTypEnv
                (varEnvr, funEnvr, structTypEnvr, codeAfterAloc @ coder)
            | VardecAndAssign (typ, name, e) -> 
                // let (varEnv1, code1) = allocate Glovar (typ, x) varEnv structTypEnv
                // let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv1 funEnv structTypEnv
                // // let code2 = cAccess (AccVar x) varEnvr funEnvr structTypEnv 
                // // let code3 = cExpr e varEnvr funEnvr [] structTypEnv (STI :: (addINCSP -1 coder))
                // (varEnvr, funEnvr, structTypEnvr, code1 @ (cAccess (AccVar(x)) varEnvr funEnvr [] structTypEnv (cExpr e varEnvr funEnvr [] structTypEnv (STI :: (addINCSP -1 coder)))))
                let (varEnv1, codeAfterAloc) = allocateWithMsg Glovar (typ, name) varEnv structTypEnv
                let (varEnvr, funEnvr, structTypEnvr, codeRest) = addv decRest varEnv1 funEnv structTypEnv
                let code2 = cAccess (AccVar name) varEnvr funEnvr structTypEnv
                (varEnvr, funEnvr, structTypEnvr, codeAfterAloc @ codeRest @ code2)
            | Fundec (tyOpt, f, xs, body) ->
            // add Var 
                addv decRest varEnv ((f, (newLabel(), tyOpt, xs)) :: funEnv) structTypEnv
            | Structdec (typName, typEntry) -> 
                let structTypEnv1 = makeStructEnvs typName typEntry structTypEnv
                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decRest varEnv funEnv structTypEnv1
                (varEnvr, funEnvr, structTypEnvr, coder)
                
                
    addv topdecs ([], 0) [] []

and makeStructEnvs(structName : string) (structEntry :(typ * string) list ) (structTypEnv : StructTypeEnv) : StructTypeEnv = 
    let rec addm structName structEntry structTypEnv = 
        match structEntry with
        | [] -> structTypEnv
        | lhs::rhs ->
            match lhs with
            | (typ, name)   -> 
                let structTypEnv1 = structAllocateDef StructMemberLoc structName typ name structTypEnv
                let structTypEnvr = addm structName rhs structTypEnv1
                // add member 
                structTypEnvr

    addm structName structEntry structTypEnv


and structAllocateDef(kind : int -> Var) (structName : string) (typ : typ) (varName : string) (structTypEnv : StructTypeEnv) : StructTypeEnv = 
    match structTypEnv with
    | lhs :: rhs ->
        let (name, env, depth) = lhs
        if name = structName 
        then 
            match typ with
            | TypA (TypA _, _)    -> failwith "Warning: allocate-arrays of arrays not permitted" 
            // 嵌套的 arr 不行
            | TypA (t, Some i)         ->
                let newEnv = env @ [(varName, (kind (depth+i), typ))]
                // 类型
                // arr idx 的是什么类型
                (name, newEnv, depth + i) :: rhs
            | _ ->
                let newEnv = env @ [(varName, (kind (depth+1), typ))] 
                (name, newEnv, depth + 1) :: rhs
        else structAllocateDef kind structName typ varName rhs
    | [] -> 
        match typ with
            | TypA (TypA _, _)    -> failwith "Warning: allocate-arrays of arrays not permitted" 
            | TypA (t, Some i)         ->
                let kindOfI= kind (i)
                printfn "最底层 他是 arr"
                printfn $"kindOfI {kindOfI}"
                printfn $"typ {typ}"
                printfn $"varName {varName}"
                // let newEnv = [(varName, (kind (i), typ))]
                let newEnv = [(varName, (kindOfI, typ))]
                (structName, newEnv, i) :: structTypEnv
            | _ ->
                let kind0= kind (0)
                printfn "====="
                printfn "最底层 他是 除了arr以外的"
                printfn $"kind0 {kind0}"
                printfn $"typ {typ}"
                printfn $"varName {varName}"
                // 最底层 他是 除了arr以外的
                // kind0 StructMemberLoc 0
                // typ TypI
                // varName intVal
                let newEnv = [(varName, (kind0, typ))]
                (structName, newEnv, 0) :: structTypEnv


and tryStmt tryBlock (varEnv : VarEnv) (funEnv : FunEnv) (structEnv : StructTypeEnv) : instr list * VarEnv = 
    match tryBlock with
    | Block stmts ->

        let rec loop stmts varEnv =
            match stmts with
            | [] -> 
                let sndOfVarEnv = snd varEnv
                printfn $"sndOfVarEnv {sndOfVarEnv}"
                (snd varEnv, [], varEnv)
            | stmtHead :: stmtRest ->
                let (varEnvHead, codeHead) = cStmtOrDec stmtHead varEnv funEnv structEnv
                let (fDepthRest, codeRest, varEnvRest) = loop stmtRest varEnvHead
                printfn  $"fDepthRest {fDepthRest}"
                (fDepthRest, codeHead @ codeRest, varEnvRest)
//                剩下的深度
                // 连接

        let (fDepthEnd, code, varEnvEnd) = loop stmts varEnv

        code @ [ INCSP(snd varEnv - fDepthEnd) ], varEnvEnd
        // | INCSP of int            (* increase stack top by m     *)
        // 将堆栈顶部增加m 
        // sp 是某个寄存器  增加 和栈有关的


//and cStmtOrDec stmtOrDec (varEnv: VarEnv) (funEnv: FunEnv) : VarEnv * instr list =
//    match stmtOrDec with
//    | Stmt stmt -> (varEnv, cStmt stmt varEnv funEnv)
//    | Dec (typ, x) -> allocateWithMsg Locvar (typ, x) varEnv


and cStmtOrDec stmtOrDec (varEnv: VarEnv) (funEnv: FunEnv) (structEnv : StructTypeEnv) : VarEnv * instr list =
    match stmtOrDec with
    | Stmt stmt -> (varEnv, cStmt stmt varEnv funEnv structEnv)
    | Dec (typ, x) -> allocateWithMsg Locvar (typ, x) varEnv structEnv
    | DecAndAssign (typ, name, expr) ->
        let (varEnv1, code) = allocateWithMsg Locvar (typ, name) varEnv structEnv
        printfn $"DecAndAssign"
        printfn $"varEnv1 {varEnv1}"
        printfn $"code {code}"
        // let code2 = cAccess (AccVar x) varEnv1 funEnv
        // let code1 = cExpr (Access x)  varEnv1 funEnv  //求值
        let code2 = cExpr(Assign (AccVar name, expr))  varEnv1 funEnv structEnv
        (varEnv1, code @ code2 @ [INCSP -1])



(* Compiling micro-C expressions:

   * e       is the expression to compile
   * varEnv  is the local and gloval variable environment
   * funEnv  is the global function environment

   Net effect principle: if the compilation (cExpr e varEnv funEnv) of
   expression e returns the instruction sequence instrs, then the
   execution of instrs will leave the rvalue of expression e on the
   stack top (and thus extend the current stack frame with one element).
*)

(*编译micro-C表达式：
*e是要编译的表达式
*varEnv是局部和全局变量环境
*funEnv是全局功能环境
净效果原则：如果
表达式e返回指令序列instrs，然后
instrs的执行将使表达式e的右值保留在
堆栈顶部（从而用一个元素扩展当前堆栈框架）。
*)


and cExpr (e: expr) (varEnv: VarEnv) (funEnv: FunEnv) (structEnv : StructTypeEnv) : instr list =
    match e with
    // ++
    | PreInc acc -> 
        cAccess acc varEnv funEnv structEnv 
            @ [DUP] @ [LDI] @ [CSTI 1] @ [ADD] @ [STI]
    | PreDec acc -> 
        cAccess acc varEnv funEnv structEnv 
            @ [DUP] @ [LDI] @ [CSTI 1] @ [SUB] @ [STI] 
            // 复制栈顶 加载数据 -1  设置
            // 栈顶的数 大概就是现在的变量

    | Access acc -> cAccess acc varEnv funEnv structEnv @ [ LDI ]
    | Assign (acc, e) ->
        cAccess acc varEnv funEnv structEnv
        @ cExpr e varEnv funEnv structEnv @ [ STI ]
    | OpAssign (op, acc, expr) ->
        cAccess acc varEnv funEnv structEnv
        // load int 
        // duplicate stack top    重复堆栈顶部  
        // get s[s[sp]]    load？ int？
//            复制 栈顶 ，获取栈顶，计算表达式 根据不同的规则,算出来的放入栈顶
            @ [DUP] @ [LDI] @ cExpr expr varEnv funEnv structEnv
                @ (match op with
                    | "+" -> [ADD]
                    | "-" -> [SUB]
                    | "*" -> [MUL]
                    | "/" -> [DIV]
                    | "%" -> [MOD]
                    | _ -> failwith "could not find this operation"
                )  @ [STI]
//                set s[s[sp-1]]    设置栈顶？
//    | Printf(ope, e1)  ->
//         cExpr e1.[0] varEnv funEnv  structEnv
//         @(match ope with
//                | "%d"  -> [PRINTI :: C]
//                | "%c"  -> PRINTC :: C
//                | "%f"  -> PRINT_FLOAT :: C
//            )
    | CstI i -> [ CSTI i ]
    | CstChar c ->
//        传进来的char 
        let c = (int c)
        [ CSTI c ]
//        指令只能是int 
    | CstFloat floatStr ->
//        传进来的str 
        let bytes = System.BitConverter.GetBytes(float32(floatStr))
        let intSetVal = System.BitConverter.ToInt32(bytes, 0)
        [ CSTI intSetVal ]
//        指令里只能是int 形式储存 但是补码都是一样的 之后只要float形式取出就行
  
    | Addr acc -> cAccess acc varEnv funEnv structEnv
    | Prim1 (ope, e1) ->
    // 错误的时候的 行号 是要传参的时候带着这个信息吗。。 有点难搞 不会。。
    // 或者也可以知道这个字符串 之后去源代码里找？ 虽然会找到好几个一样的 但是至少 缩小了范围
    // 而且确实有实现的可能 而带着行列的参 我就不懂了。。
        cExpr e1 varEnv funEnv structEnv
        @ (match ope with
           | "!" -> [ NOT ]
           | "printi" -> [ PRINTI ]
//           print的值是谁
           | "printc" -> [ PRINTC ]
           | "~" -> [ BITNOT ]
           | _ -> raise (Failure $"unknown primitive 1 {ope}"))
    | Prim2 (ope, e1, e2) ->
        // 往栈里放两个值 然后比较 或者计算两个
        cExpr e1 varEnv funEnv structEnv
        @ cExpr e2 varEnv funEnv structEnv
          @ (match ope with
             | "*" -> [ MUL ]
             | "+" -> [ ADD ]
             | "-" -> [ SUB ]
             | "/" -> [ DIV ]
             | "%" -> [ MOD ]
             | "==" -> [ EQ ]
             | "!=" -> [ EQ; NOT ]
             | "<" -> [ LT ]
             | ">=" -> [ LT; NOT ]
             | ">" -> [ SWAP; LT ]
//             转换两个数字 然后less than 
             | "<=" -> [ SWAP; LT; NOT ]
             | "&" -> [ BITAND ]
             | "|" -> [ BITOR ]
             | "^" -> [ BITXOR ]
             | "<<" -> [ BITLEFT ]
             | ">>" -> [ BITRIGHT ]
             | _ -> raise (Failure $"unknown primitive 2 : {ope}"))
    | Prim3 (ques, doExpr,elseExpr) ->
        // ques? do:not 
        // 获取的ques 不对啊
        printfn $"ques {ques}"
        printfn $"doExpr {elseExpr}"
        printfn $"notDoExpr {doExpr}"
        // e1 CstI 5
        // e2 CstI 1
        // e3 CstI 0
        let labElse = newLabel ()
        let labEnd = newLabel ()

        // cExpr e1 varEnv funEnv structEnv
        // @ [ IFZERO labElse ]
        //   @ cExpr e2 varEnv funEnv structEnv
        // //   做第二个
        //     @ [ GOTO labEnd ]
        //     // 结束
        //       @ [ Label labElse ]
        //     //   做另外的
        //         @ cExpr e3 varEnv funEnv structEnv 
        //             @ [ Label labEnd ]

        cExpr ques varEnv funEnv structEnv
        @ [ IFZERO labElse ]
//          如果不符合 就走else
          @ cExpr doExpr varEnv funEnv structEnv
        //   做第二个
            @ [ GOTO labEnd ]
            // 结束
              @ [ Label labElse ]
            //   做另外的
                @ cExpr elseExpr varEnv funEnv structEnv 
                    @ [ Label labEnd ]
    
    | Andalso (e1, e2) ->
        let labEnd = newLabel ()
        let labFalse = newLabel ()
        // &&

        cExpr e1 varEnv funEnv structEnv
        // 得到一个值
        @ [ IFZERO labFalse ]
        // 如果错的 就去 false
          @ cExpr e2 varEnv funEnv structEnv
            @ [ GOTO labEnd
                Label labFalse
                CSTI 0
                Label labEnd ]
    | Orelse (e1, e2) ->
        let labEnd = newLabel ()
        let labTrue = newLabel ()

        cExpr e1 varEnv funEnv structEnv
        @ [ IFNZRO labTrue ]
          @ cExpr e2 varEnv funEnv structEnv
            @ [ GOTO labEnd
                Label labTrue
                CSTI 1
                Label labEnd ]
    | Call (f, es) -> 
        printfn $"start call "
        printfn $"f {f}"
        printfn $"es {es}"
        callfun f es varEnv funEnv structEnv
    


//
//and cExpr (e: expr) (varEnv: VarEnv) (funEnv: FunEnv) (structEnv : StructTypeEnv): instr list =
//    match e with
//    | Access acc -> cAccess acc varEnv funEnv @ [ LDI ]
//    | Assign (acc, e) ->
//        cAccess acc varEnv funEnv
//        @ cExpr e varEnv funEnv structEnv
//          @ [ STI ]
//    | CstI i -> [ CSTI i ]
//    | CstChar c -> 
//        let c = (int c)
//        [ CSTI c ]
//    | ConstFloat f -> 
//        // 获取 float 的bytes
//        let bytes = System.BitConverter.GetBytes(float32(f))
//        let v = System.BitConverter.ToInt32(bytes, 0)
//        [ CSTI v ]
//    | Addr acc -> cAccess acc varEnv funEnv
//    | Prim1 (ope, e1) ->
//        cExpr e1 varEnv funEnv structEnv
//        @ (match ope with
//           | "!" -> [ NOT ]
//           | "printi" -> [ PRINTI ]
//           | "printc" -> [ PRINTC ]
//           | _ -> raise (Failure "unknown primitive 1"))
//    | Prim2 (ope, e1, e2) ->
//        cExpr e1 varEnv funEnv structEnv
//        @ cExpr e2 varEnv funEnv structEnv
//          @ (match ope with
//             | "*" -> [ MUL ]
//             | "+" -> [ ADD ]
//             | "-" -> [ SUB ]
//             | "/" -> [ DIV ]
//             | "%" -> [ MOD ]
//             | "==" -> [ EQ ]
//             | "!=" -> [ EQ; NOT ]
//             | "<" -> [ LT ]
//             | ">=" -> [ LT; NOT ]
//             | ">" -> [ SWAP; LT ]
//             | "<=" -> [ SWAP; LT; NOT ]
//             | "<<" -> [ BITLEFT ]
//             | _ -> raise (Failure "unknown primitive 2"))
//    | Andalso (e1, e2) ->
//        let labend = newLabel ()
//        let labfalse = newLabel ()
//
//        cExpr e1 varEnv funEnv structEnv
//        @ [ IFZERO labfalse ]
//          @ cExpr e2 varEnv funEnv structEnv
//            @ [ GOTO labend
//                Label labfalse
//                CSTI 0
//                Label labend ]
//    | Orelse (e1, e2) ->
//        let labend = newLabel ()
//        let labtrue = newLabel ()
//
//        cExpr e1 varEnv funEnv structEnv
//        @ [ IFNZRO labtrue ]
//          @ cExpr e2 varEnv funEnv structEnv
//            @ [ GOTO labend
//                Label labtrue
//                CSTI 1
//                Label labend ]
//    | Call (f, es) -> callfun f es varEnv funEnv
//
//(* Generate code to access variable, dereference pointer or index array.
//   The effect of the compiled code is to leave an lvalue on the stack.   *)
//
////and cAccess access varEnv funEnv : instr list =
////    match access with
////    | AccVar x ->
////        match lookup (fst varEnv) x with
////        // x86 虚拟机指令 需要知道是全局变量 [GVAR addr]
////        // 栈式虚拟机Stack VM 的全局变量的地址是 栈上的偏移 用 [CSTI addr] 表示
////        // F# ! 操作符 取引用类型的值
////        | Glovar addr, _ ->
////            if !isX86Instr then
////                [ GVAR addr ]
////            else
////                [ CSTI addr ]
////        | Locvar addr, _ -> [ GETBP; OFFSET addr; ADD ]
////    | AccDeref e ->
////        match e with
////        | Access _ -> (cExpr e varEnv funEnv)
////        | Addr _ -> (cExpr e varEnv funEnv)
////        | _ ->
////            printfn "WARN: x86 pointer arithmetic not support!"
////            (cExpr e varEnv funEnv)
////    | AccIndex (acc, idx) ->
////        cAccess acc varEnv funEnv
////        @ [ LDI ]
////          @ x86patch (cExpr idx varEnv funEnv) @ [ ADD ]
//
//
//获取
and cAccess access varEnv funEnv structEnv : instr list =
    match access with
    | AccVar x ->
        match lookup (fst varEnv) x with
        // x86 虚拟机指令 需要知道是全局变量 [GVAR addr]
        // 栈式虚拟机Stack VM 的全局变量的地址是 栈上的偏移 用 [CSTI addr] 表示
        | Glovar addr, _ ->
            // if !isX86Instr then
            //     [ GVAR addr ]
            // else
                [ CSTI addr ]
        | Locvar addr, _ -> [ GETBP; OFFSET addr; ADD ]
//        栈底 
    
    | AccStruct (AccVar stru, AccVar memb) ->
        let (loc, TypStruct structname)   = lookup (fst varEnv) stru
        let (name, argsList, size) = structLookup structEnv structname
        (*全局变量有绝对地址，局部变量有偏移：*)
        printfn $"name {name} argsList {argsList} size {size}"
        match structLookupVar argsList memb 0 with
        
        | StructMemberLoc varLocate ->
            match lookup (fst varEnv) stru with
            | Glovar addr, _ -> 
                let a = (addr - (size+1) + varLocate)
                [ CSTI a ]
            | Locvar addr, _ -> 
                // GETBP :: addCST (addr - (size+1) + varLocate) (ADD ::  C)
                let a = addr - (size+1) + varLocate
                // 0
                [ GETBP; OFFSET a; ADD ]
//                偏移 获取栈顶

    
    | AccDeref e ->
        match e with
        | Access _ -> (cExpr e varEnv funEnv structEnv)
        | Addr _ -> (cExpr e varEnv funEnv structEnv)
        | _ ->
            printfn "WARN: x86 pointer arithmetic not support!"
            (cExpr e varEnv funEnv structEnv)
    | AccIndex (acc, idx) ->
        cAccess acc varEnv funEnv structEnv
        @ [ LDI ]
//         get s[s[sp]]
//        获得内存 内存里是个 index？ 根据index 获取 数组的元素？
        //   @ x86patch (cExpr idx varEnv funEnv) @ [ ADD ]

//
//(* Generate code to evaluate a list es of expressions: 生成用于计算表达式列表的代码：*)
//
//and cExprs es varEnv funEnv : instr list =
//    List.concat (List.map (fun e -> cExpr e varEnv funEnv) es)
//

and cExprs es varEnv funEnv structEnv : instr list =
    List.concat (List.map (fun e -> cExpr e varEnv funEnv structEnv) es)

//(* Generate code to evaluate arguments es and then call function f: *)
//
//and callfun f es varEnv funEnv : instr list =
//    let (labf, tyOpt, paramdecs) = lookup funEnv f
//    let argc = List.length es
//
//    if argc = List.length paramdecs then
//        cExprs es varEnv funEnv @ [ CALL(argc, labf) ]
//    else
//        raise (Failure(f + ": parameter/argument mismatch"))

and callfun f es varEnv funEnv structEnv : instr list =
    let (labf, tyOpt, paramDecs) = lookup funEnv f
    let argc = List.length es

    if argc = List.length paramDecs then
        cExprs es varEnv funEnv structEnv @ [ CALL(argc, labf) ]
    else
        raise (Failure(f + ": parameter/argument mismatch"))


(* Compile a complete micro-C program: globals, call to main, functions *)
//编译一个完整的micro-C程序：globals、对main的调用、函数
let argc = ref 0
(*编译一个完整的micro-C程序：globals、对main的调用、函数*)
// 入口
//let cProgram (Prog topdecs) : instr list =
//    let _ = resetLabels ()
//    let ((globalVarEnv, _), funEnv, globalInit) = makeGlobalEnvs topdecs
//
//    let compilefun (tyOpt, f, xs, body) =
//        let (labf, _, paras) = lookup funEnv f
//        let paraNums = List.length paras
//        let (envf, fdepthf) = bindParams paras (globalVarEnv, 0)
//        let code = cStmt body (envf, fdepthf) funEnv
//
//        [ FLabel (paraNums, labf) ]
//        @ code @ [ RET(paraNums - 1) ]
//        // 连接两个列表。
//// 
//
//    let functions =
//        List.choose
//            (function
//            | Fundec (rTy, name, argTy, body) -> Some(compilefun (rTy, name, argTy, body))
//            | Vardec _ -> None)
//            topdecs
//
//    let (mainlab, _, mainparams) = lookup funEnv "main"
//    argc := List.length mainparams
//
//    globalInit
//    @ [ LDARGS !argc
//        CALL(!argc, mainlab)
//        STOP ]
//      @ List.concat functions
//

let cProgram (Prog topdecs) : instr list =
    let _ = resetLabels ()
    let ((globalVarEnv, _), funEnv, structEnv, globalInit) = makeGlobalEnvs topdecs

    let compilefun (tyOpt, f, xs, body) =
        let (labf, _, paras) = lookup funEnv f
        let paraNums = List.length paras
        let (envf, fdepthf) = bindParams paras (globalVarEnv, 0)
        let code = cStmt body (envf, fdepthf) funEnv structEnv

        [ FLabel (paraNums, labf) ]
        @ code @ [ RET(paraNums - 1) ]

    let functions =
        List.choose
            (function
            | Fundec (rTy, name, argTy, body) -> Some(compilefun (rTy, name, argTy, body))
            | Vardec _ -> None
            | VardecAndAssign _ -> None
            | Structdec _ -> None)
            topdecs

    let (mainlab, _, mainParams) = lookup funEnv "main"
    argc := List.length mainParams

    globalInit
    // load args  参数
    @ [ LDARGS !argc
        CALL(!argc, mainlab)
        STOP ]
      @ List.concat functions


(* Compile a complete micro-C and write the resulting instruction list
   to file fname; also, return the program as a list of instructions.
   编译一个完整的micro-C并编写生成的指令列表
归档fname；另外，将程序作为指令列表返回。
 *)

let intsToFile (inss: int list) (fname: string) =
    File.WriteAllText(fname, String.concat " " (List.map string inss))

let writeInstr fname instrs =
    let ins =
        String.concat "\n" (List.map string instrs)

    File.WriteAllText(fname, ins)
    printfn $"VM instructions saved in file:\n\t{fname}"

let gen86 program fname=
    // 面向 x86 的虚拟机指令 略有差异，主要是地址偏移的计算方式不同
    // 单独生成 x86 的指令
    isX86Instr := true
    let x86instrs = cProgram program
    writeInstr (fname + ".insx86") x86instrs
    // 生成失败
    let x86asmlist = List.map emitx86 x86instrs
    // 这里报错
    // printfn 
    printfn $"x86asmlist {x86asmlist}"
    let x86asmbody =
        List.fold (fun asm ins -> asm + ins) "" x86asmlist
    printfn $"x86asmbody {x86asmbody}"
    let x86asm =
        (x86header + beforeinit !argc + x86asmbody)
    printfn $"x86asm {x86asm}"
    printfn $"x86 assembly saved in file:\n\t{fname}.asm"
    File.WriteAllText(fname + ".asm", x86asm)


let compileToFile program fname =

    msg <|sprintf "program:\n %A" program

    let instrs = cProgram program

    msg <| sprintf "\nStack VM instrs:\n %A\n" instrs

    writeInstr (fname + ".ins") instrs

    printfn "开始生成字节码"
    let bytecode = code2ints instrs
    // 应该是这里报错了 不是 是下面 86的 
    msg <| sprintf "Stack VM numeric code:\n %A\n" bytecode


    // gen86 program fname

    // 面向 x86 的虚拟机指令 略有差异，主要是地址偏移的计算方式不同
    // 单独生成 x86 的指令
    // isX86Instr := true
    // let x86instrs = cProgram program
    // writeInstr (fname + ".insx86") x86instrs
    // // 生成失败
    // let x86asmlist = List.map emitx86 x86instrs
    // // 这里报错
    // // printfn 
    // printfn $"x86asmlist {x86asmlist}"
    // let x86asmbody =
    //     List.fold (fun asm ins -> asm + ins) "" x86asmlist
    // printfn $"x86asmbody {x86asmbody}"
    // let x86asm =
    //     (x86header + beforeinit !argc + x86asmbody)
    // printfn $"x86asm {x86asm}"
    // printfn $"x86 assembly saved in file:\n\t{fname}.asm"
    // File.WriteAllText(fname + ".asm", x86asm)

    // let deinstrs = decomp bytecode
    // printf "deinstrs: %A\n" deinstrs
    intsToFile bytecode (fname + ".out")

    instrs

(* Example programs are found in the files ex1.c, ex2.c, etc *)
