(* File MicroC/Absyn.fs
   Abstract syntax of micro-C, an imperative language.
   sestoft@itu.dk 2009-09-25

   Must precede Interp.fs, Comp.fs and Contcomp.fs in Solution Explorer
 *)

module Absyn

// 基本类型
// 注意，数组、指针是递归类型
// 这里没有函数类型，注意与上次课的 MicroML 对比
type typ =
  | TypFloat
  | TypI                             (* Type int                    *)
  | TypC                             (* Type char                   *)
  | TypA of typ * int option         (* Array type   option 有或者没有       *)
  | TypP of typ                      (* Pointer type                *)
  | TypString
  | TypBool
  | TypCharArr of typ * char option 
  | TypStruct of string
                                                                   
and expr =                           // 表达式，右值
//  类型太多了
  | Access of access                 (* x    or  *p    or  a[e]     *) //访问左值（右值）
  | Assign of access * expr          (* x=e  or  *p=e  or  a[e]=e   *)
  | Addr of access                   (* &x   or  &*p   or  &a[e]    *)
  | CstI of int                      (* Constant                    *)
  // | CstChar of char 
  | CstChar of char
  | CstString of string       
  | CstFloat of string
//  先从代码里的字符串解析出来 之后再解析成float 
  | Prim1 of string * expr           (* Unary primitive operator    *)
  | Prim2 of string * expr * expr    (* Binary primitive operator   *)
  | Prim3 of expr * expr * expr    
  | OpAssign of string * access * expr
  // b += 1  之后是个表达是 是可以继续赋值的 
  // a+= b+=1  就是 b+=1 之后 继续被 a+= (b) 
  | ToChar of expr 
  | ToInt of expr
  // (int) a 之后的值 可以赋值 b= (int) a
  | ConstFloat of float32 
  // | Prim3 of stmt * expr * expr      
  // | Prim3 of expr * stmt * stmt      
  | Andalso of expr * expr           (* Sequential and    顺序和          *)
  | Orelse of expr * expr            (* Sequential or               *)
  | Call of string * expr list       (* Function call f(...)        *)
  | PreInc of access                  (* ++x  *)
  | PreDec of access                  (* --x *)
  | Printf of string * expr list
  // | PrintString of string * expr 
  | PrintString of expr 
  // | PrintString of string 
  // | Printf of string * expr 
  // 这里做里面的操作
                                                                   
and access =                         //左值，存储的位置                                            
  | AccVar of string                 (* Variable access        x  （*可变访问x*）  *) 
  | AccDeref of expr                 (* Pointer dereferencing  *p  （*指针解引用*p*） *)
  | AccIndex of access * expr        (* Array indexing         a[e] *)
  // | Break
  | AccStruct of access * access
                                                                   
and stmt =                                                         
  | If of expr * stmt * stmt         (* Conditional                 *)
  | While of expr * stmt             (* While loop                  *)
  | Expr of expr                     (* Expression statement   e;   *)
  | Return of expr option            (* Return from method          *)
  | Block of stmtordec list          (* Block: grouping and scope   *)
  // 语句块内部，可以是变量声明 或语句的列表                       
  | For of expr * expr  * expr * stmt   
  | Throw of excep
  | Switch of expr * caseStmt list      
  | DoWhile of  stmt * expr     
  | DoUntil of stmt * expr    
  | Try of stmt * stmt list
  | Catch of excep * stmt
  // | Prim3 of expr * stmt * stmt       
  | Break     
  // | Continue                
  | Continue                            (*continue功能*)


and excep = 
  | Exception of string

and caseStmt = 
  | Case of expr * stmt
  | Default of stmt


// 定义一个变量是在这里的 要是一开始要赋值  是不是也是定义在这里
// 是一种新的语句？
and stmtordec =                                                    
  | Dec of typ * string              (* Local variable declaration  *)
  | Stmt of stmt                     (* A statement                 *)
  | DecAndAssign of typ * string * expr

// 顶级声明 可以是函数声明或变量声明
and topdec = 
  | Fundec of typ option * string * (typ * string) list * stmt
  | Vardec of typ * string
  | VardecAndAssign of typ * string * expr
  | Structdec of string * (typ * string) list
  // struct name { int i =1 ; int b =1 ;}
// 程序是顶级声明的列表
and program = 
  | Prog of topdec list
