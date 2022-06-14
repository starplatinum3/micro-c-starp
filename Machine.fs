(* File MicroC/Machine.fs 

   Instructions and code emission for a stack-based
   abstract machine * sestoft@itu.dk 2009-09-23

   Implementations of the machine are found in file MicroC/Machine.java 
   and MicroC/machine.c.
   
   Must precede Comp.fs and Contcomp.fs in the VS Solution Explorer.
   
   基于堆栈的指令和代码输出
抽象机器*sestoft@itu.dk 2009-09-23
机器的实现可以在 MicroC/Machine.java 和 MicroC/machine.c.文件中找到
必须在Comp.fs和  Contcomp.fs 之前，在VS解决方案资源管理器中。
 *)

module Machine

type label = string
// 汇编指令
type instr =
  | Label of label                     (* symbolic label; pseudo-instruc.符号标签；伪指令。 *)
  (*符号标签；伪指令。*)
  | FLabel of int * label                     (* symbolic label; pseudo-instruc. 符号标签；伪指令。 *)
  | CSTI of int                        (* constant                        *)
//   | CST_CHAR of char
  | CST_CHAR of int
  | CST_FLOAT of int32
  | OFFSET of int                        (* constant     偏移地址  x86     *) 
  | GVAR of int                        (* global var     全局变量  x86     *) 
  | ADD                                (* addition                        *)
  | SUB                                (* subtraction                     *)
  | MUL                                (* multiplication                  *)
  | DIV                                (* division                        *)
  | MOD                                (* modulus                         *)
  | EQ                                 (* equality: s[sp-1] == s[sp]      *)
  | LT                                 (* less than: s[sp-1] < s[sp]      *)
  | NOT                                (* logical negation:  s[sp] != 0   *)
  | DUP                                (* duplicate stack top    重复堆栈顶部         *)
  | SWAP                               (* swap s[sp-1] and s[sp]          *)
  | LDI                                (* get s[s[sp]]    load？ int？                *)
  | STI                                (* set s[s[sp-1]]    设置栈顶？              *)
  | GETBP                              (* get bp                          *)
  | GETSP                              (* get sp                          *)
  | INCSP of int                       (* increase stack top by m         *)
  | GOTO of label                      (* go to label                     *)
  | IFZERO of label                    (* go to label if s[sp] == 0       *)
  | IFNZRO of label                    (* go to label if s[sp] != 0       *)
  | CALL of int * label                (* move m args up 1, push pc, jump *)
  | TCALL of int * int * label         (* move m args down n, jump        *)
  | RET of int                         (* pop m and return to s[sp]       *)
  | PRINTI                             (* print s[sp] as integer          *)
  | PRINTC                             (* print s[sp] as character        *)
  | LDARGS of int                             (* load command line args on stack , load args（*在堆栈上加载命令行参数*） *)
  | STOP                               (* halt the abstract machine       *)
  | PRINT_FLOAT
  | BITAND                             (* bit operation AND               *)
  | BITOR                              (* bit operation OR                *)
  | BITXOR                             (* bit operation XOR               *)
  | BITLEFT                            (* bit operation LEFT SHIFT        *)
  | BITRIGHT                           (* bit operation LEFT SHIFT        *)
  | BITNOT                             (* bit operation BITNOT            *)
  | THROW of int
  | PUSHHDLR of int * label
//  push head label
  | POPHDLR

(* Generate new distinct labels *)

// 返回两个函数 resetLabels , newLabel
let (resetLabels, newLabel) = 
    let lastlab = ref -1
    ((fun () -> lastlab := 0), (fun () -> (lastlab := 1 + !lastlab; "L" + (!lastlab).ToString())))

(* Simple environment operations *)

type 'data env = (string * 'data) list

let rec lookup env x = 
    match env with 
    | []         -> failwith (x + " not found")
    | (y, v)::yr -> if x=y then v else lookup yr x

(* An instruction list is emitted in two phases:
   * pass 1 builds an environment labenv mapping labels to addresses 
   * pass 2 emits the code to file, using the environment labenv to 
     resolve labels
 *)

(*指令列表分两个阶段发出：
*pass 1构建一个环境labenv，将标签映射到地址
*pass 2使用环境labenv将代码发送到文件
解析标签
*)

(* These numeric instruction codes must agree with Machine.java: *)
(*这些数字指令代码必须与Machine.java:*一致*)


//机器码

//[<Literal>] 属性可以让 
//该变量在模式匹配时候被匹配,否则匹配时只能用数值.不能用变量名



[<Literal>]
let CODECSTI   = 0 


[<Literal>]
let CODEADD    = 1 


[<Literal>]
let CODESUB    = 2 


[<Literal>]
let CODEMUL    = 3 

[<Literal>]
let CODEDIV    = 4 

[<Literal>]
let CODEMOD    = 5 

[<Literal>]
let CODEEQ     = 6 

[<Literal>]
let CODELT     = 7 

[<Literal>]
let CODENOT    = 8 

[<Literal>]
let CODEDUP    = 9 

[<Literal>]
let CODESWAP   = 10 

[<Literal>]
let CODELDI    = 11 


[<Literal>]
let CODESTI    = 12 


[<Literal>]
let CODEGETBP  = 13 


[<Literal>]
let CODEGETSP  = 14 


[<Literal>]
let CODEINCSP  = 15 


[<Literal>]
let CODEGOTO   = 16


[<Literal>]
let CODEIFZERO = 17

[<Literal>]
let CODEIFNZRO = 18 



[<Literal>]
let CODECALL   = 19


[<Literal>]
let CODETCALL  = 20

[<Literal>]
let CODERET    = 21


[<Literal>]
let CODEPRINTI = 22 

[<Literal>]
let CODEPRINTC = 23


[<Literal>]
let CODELDARGS = 24

[<Literal>]
let CODESTOP   = 25;

// 变量类型的话 是不是这种汇编也要写。。 看不懂 就跟着造吧。。
[<Literal>]
let CODE_CST_CHAR    = 27;

[<Literal>]
let CODE_PRINT_FLOAT    = 28;

[<Literal>]
let CODE_CST_FLOAT    = 29;


[<Literal>]
let CODEBITAND   = 32;

[<Literal>]
let CODEBITOR   = 33;

[<Literal>]
let CODEBITXOR   = 34;

[<Literal>]
let CODEBITLEFT   = 35;

[<Literal>]
let CODEBITRIGHT  = 30;

[<Literal>]
let CODEBITNOT  = 31;

[<Literal>]
let CODETHROW   = 36;

[<Literal>]
let CODEPUSHHR  = 37;

[<Literal>]
let CODEPOPHR   = 38;

(* Bytecode emission, first pass: build environment that maps 
   each label to an integer address in the bytecode.
 *)
//获得标签在机器码中的地址
let makelabenv (addr, labenv) instr = 
    match instr with
    // 记录当前 (标签, 地址) ==> 到 labenv中
    | Label lab      -> (addr, (lab, addr) :: labenv)
    | FLabel (m,lab)      -> (addr, (lab, addr) :: labenv)
    | CSTI i         -> (addr+2, labenv)
    | CST_CHAR i         -> (addr+2, labenv)
    | CST_FLOAT i            -> (addr+2, labenv)
    | GVAR i         -> (addr+2, labenv)
    | OFFSET i       -> (addr+2, labenv)
    // 一个参数就+2 
    // 0个参数就是+1 
    // 一个参数占一个地址？
    | ADD            -> (addr+1, labenv)
    | SUB            -> (addr+1, labenv)
    | MUL            -> (addr+1, labenv)
    | DIV            -> (addr+1, labenv)
    | MOD            -> (addr+1, labenv)
    | EQ             -> (addr+1, labenv)
    | LT             -> (addr+1, labenv)
    | NOT            -> (addr+1, labenv)
    | DUP            -> (addr+1, labenv)
    | SWAP           -> (addr+1, labenv)
    | LDI            -> (addr+1, labenv)
    | STI            -> (addr+1, labenv)
    | GETBP          -> (addr+1, labenv)
    | GETSP          -> (addr+1, labenv)
    | INCSP m        -> (addr+2, labenv)
    | GOTO lab       -> (addr+2, labenv)
    | IFZERO lab     -> (addr+2, labenv)
    | IFNZRO lab     -> (addr+2, labenv)
    | CALL(m,lab)    -> (addr+3, labenv)
    | TCALL(m,n,lab) -> (addr+4, labenv)
    | RET m          -> (addr+2, labenv)
    | PRINTI         -> (addr+1, labenv)
    | PRINTC         -> (addr+1, labenv)
    | PRINT_FLOAT         -> (addr+1, labenv)
    | LDARGS  m       -> (addr+1, labenv)
    | STOP           -> (addr+1, labenv)
    | BITAND         -> (addr+1, labenv)
    | BITOR          -> (addr+1, labenv)
    | BITXOR         -> (addr+1, labenv)
    | BITLEFT        -> (addr+1, labenv)
    | BITRIGHT       -> (addr+1, labenv)
    | BITNOT         -> (addr+1, labenv)
    | THROW i           -> (addr+2, labenv)
    | PUSHHDLR (exn ,lab) -> (addr+3, labenv)
    | POPHDLR           -> (addr+1, labenv)
    

(* Bytecode emission, second pass: output bytecode as integers *)
// 字节码发射，第二遍：将字节码输出为整数

//getlab 是得到标签所在地址的函数
//let getlab lab = lookup labenv lab

// 传递了一个函数的参数 getlab
let rec emitints getlab instr ints = 
    match instr with
    | Label lab      -> ints
    | FLabel (m,lab) -> ints
    | CSTI i         -> CODECSTI   :: i :: ints
    | CST_CHAR i         -> CODE_CST_CHAR   :: i :: ints
    | CST_FLOAT i            -> CODE_CST_FLOAT     :: i            :: ints
    // ints int 的列表
    // 需要int 类型。。 不过char 也是int 。。
    | GVAR i         -> CODECSTI   :: i :: ints
    | OFFSET i       -> CODECSTI   :: i :: ints
    // 把 code 和值 加入到int 的列表
    | ADD            -> CODEADD    :: ints
    | SUB            -> CODESUB    :: ints
    | MUL            -> CODEMUL    :: ints
    | DIV            -> CODEDIV    :: ints
    | MOD            -> CODEMOD    :: ints
    | EQ             -> CODEEQ     :: ints
    | LT             -> CODELT     :: ints
    | NOT            -> CODENOT    :: ints
    | DUP            -> CODEDUP    :: ints
    | SWAP           -> CODESWAP   :: ints
    | LDI            -> CODELDI    :: ints
    | STI            -> CODESTI    :: ints
    | GETBP          -> CODEGETBP  :: ints
    | GETSP          -> CODEGETSP  :: ints
    | INCSP m        -> CODEINCSP  :: m :: ints
    | GOTO lab       -> CODEGOTO   :: getlab lab :: ints
    | IFZERO lab     -> CODEIFZERO :: getlab lab :: ints
    | IFNZRO lab     -> CODEIFNZRO :: getlab lab :: ints
    | CALL(m,lab)    -> CODECALL   :: m :: getlab lab :: ints
    | TCALL(m,n,lab) -> CODETCALL  :: m :: n :: getlab lab :: ints
    | RET m          -> CODERET    :: m :: ints
    | PRINTI         -> CODEPRINTI :: ints
    | PRINTC         -> CODEPRINTC :: ints
    | PRINT_FLOAT -> CODE_PRINT_FLOAT :: ints
    | LDARGS m        -> CODELDARGS :: ints
    | STOP           -> CODESTOP   :: ints
    | BITAND         -> CODEBITAND :: ints
    | BITOR          -> CODEBITOR  :: ints
    | BITXOR         -> CODEBITXOR :: ints
    | BITLEFT        -> CODEBITLEFT :: ints
    | BITRIGHT       -> CODEBITRIGHT :: ints
    | BITNOT         -> CODEBITNOT :: ints
    | THROW i           -> CODETHROW    :: i            :: ints
    | PUSHHDLR (exn, lab) -> 
      printfn "我定义了 啊  PUSHHDLR"
      printfn $"exn {exn} lab {lab}"
      CODEPUSHHR :: exn          :: getlab lab   :: ints
    | POPHDLR           -> CODEPOPHR    :: ints
    


(* Convert instruction list to int list in two passes:
   Pass 1: build label environment
   Pass 2: output instructions using label environment

   分两次将指令列表转换为int list：
过程1：构建标签环境
过程2：使用标签环境输出指令
 *)
 
//通过对 code 的两次遍历,完成汇编指令到机器指令的转换
let code2ints (code : instr list) : int list =
    
    //从前往后遍历 `汇编指令序列 code: instr list`
    //得到 标签对应的地址,记录到 labenv中
    let (_, labenv) = List.fold makelabenv (0, []) code
    printfn $"标签环境 {labenv}"
    // 运行堆栈
    printfn $"code {code}"
    //getlab 是得到标签所在地址的函数
    let getlab lab = lookup labenv lab
    
    //从后往前 遍历 `汇编指令序列 code: instr list`
    List.foldBack (emitints getlab) code []
                    


let numToLabel (n:int) :label = 
    string(n)
//    number to label

//反编译
let rec decomp ints : instr list = 

    // printf "%A" ints

    match ints with
    | []                                              ->  []
    | CODEADD :: ints_rest                         ->   ADD           :: decomp ints_rest
    | CODESUB    :: ints_rest                         ->   SUB           :: decomp ints_rest
    | CODEMUL    :: ints_rest                         ->   MUL           :: decomp ints_rest
    | CODEDIV    :: ints_rest                         ->   DIV           :: decomp ints_rest
    | CODEMOD    :: ints_rest                         ->   MOD           :: decomp ints_rest
    | CODEEQ     :: ints_rest                         ->   EQ            :: decomp ints_rest
    | CODELT     :: ints_rest                         ->   LT            :: decomp ints_rest
    | CODENOT    :: ints_rest                         ->   NOT           :: decomp ints_rest
    | CODEDUP    :: ints_rest                         ->   DUP           :: decomp ints_rest
    | CODESWAP   :: ints_rest                         ->   SWAP          :: decomp ints_rest
    | CODELDI    :: ints_rest                         ->   LDI           :: decomp ints_rest
    | CODESTI    :: ints_rest                         ->   STI           :: decomp ints_rest
    | CODEGETBP  :: ints_rest                         ->   GETBP         :: decomp ints_rest
    | CODEGETSP  :: ints_rest                         ->   GETSP         :: decomp ints_rest
    | CODEINCSP  :: m :: ints_rest                    ->   INCSP m       :: decomp ints_rest
    | CODEGOTO   ::  lab :: ints_rest           ->   GOTO (numToLabel lab)      :: decomp ints_rest
    | CODEIFZERO ::  lab :: ints_rest           ->   IFZERO (numToLabel lab)    :: decomp ints_rest
    | CODEIFNZRO ::  lab :: ints_rest           ->   IFNZRO (numToLabel lab)    :: decomp ints_rest
    | CODECALL   :: m ::  lab :: ints_rest      ->   CALL(m, numToLabel lab)   :: decomp ints_rest
    | CODETCALL  :: m :: n ::  lab :: ints_rest ->   TCALL(m,n,numToLabel lab):: decomp ints_rest
    | CODERET    :: m :: ints_rest                    ->   RET m         :: decomp ints_rest
    | CODEPRINTI :: ints_rest                         ->   PRINTI        :: decomp ints_rest
    | CODEPRINTC :: ints_rest                         ->   PRINTC        :: decomp ints_rest
    | CODE_PRINT_FLOAT :: ints_rest                         ->   PRINT_FLOAT :: decomp ints_rest
    | CODELDARGS :: ints_rest                         ->   LDARGS 0       :: decomp ints_rest
    | CODESTOP   :: ints_rest                         ->   STOP             :: decomp ints_rest
    | CODECSTI   :: i :: ints_rest                    ->   CSTI i :: decomp ints_rest       
    | CODE_CST_CHAR   :: i :: ints_rest                    ->   CST_CHAR i :: decomp ints_rest  
    | CODE_CST_FLOAT   :: i :: ints_rest                  ->   CST_FLOAT i         :: decomp ints_rest 
    
    | CODEBITLEFT :: ints_rest                        ->   BITLEFT       :: decomp ints_rest
    | CODEBITNOT :: ints_rest                         ->   BITNOT       :: decomp ints_rest
    | CODEBITRIGHT :: ints_rest                       ->   BITRIGHT      :: decomp ints_rest  
    | CODEBITAND :: ints_rest                         ->   BITAND        :: decomp ints_rest
    | CODEBITOR :: ints_rest                          ->   BITOR         :: decomp ints_rest
    | CODEBITXOR :: ints_rest                         ->   BITXOR        :: decomp ints_rest
    | CODETHROW  :: i :: ints_rest                    ->   THROW i        :: decomp ints_rest
    // | CODEPUSHHR :: exn :: lab :: ints_rest           ->   PUSHHDLR (exn, numToLabel lab)     :: decomp ints_rest
    | CODEPUSHHR :: exn :: lab :: ints_rest           ->   
      printfn $"CODEPUSHHR ints_rest {ints_rest}"
      PUSHHDLR (exn, numToLabel lab)     :: decomp ints_rest
    | CODEPOPHR :: ints_rest                         ->    POPHDLR      :: decomp ints_rest
    | _                                       ->    printf "%A" ints; failwith "unknow code"

