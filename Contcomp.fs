(* File MicroC/Contcomp.fs
   A continuation-based (backwards) compiler from micro-C, a fraction of
   the C language, to an abstract machine.  
   sestoft@itu.dk * 2011-11-10

   The abstract machine code is generated backwards, so that jumps to
   jumps can be eliminated, so that tail-calls (calls immediately
   followed by return) can be recognized, dead code can be eliminated, 
   etc.

   The compilation of a block, which may contain a mixture of
   declarations and statements, proceeds in two passes:

   Pass 1: elaborate declarations to find the environment in which
           each statement must be compiled; also translate
           declarations into allocation instructions, of type
           bstmtordec.
  
   Pass 2: compile the statements in the given environments.
 *)

(*文件micro/Contcomp.fs）
micro-C的一个基于延续的（向后）编译器
将C语言转换为抽象机器。
sestoft@itu.dk * 2011-11-10
抽象机器代码是向后生成的，因此可以跳到
跳跃可以被消除，这样尾巴就可以立即呼叫
然后返回）可以被识别，死代码可以被消除，
等
一个块的汇编，其中可能包含
声明和声明，分两步进行：
过程1：详细声明，以找到
每一项声明都必须经过汇编；也翻译
将声明转换为分配指令，类型为
bstmtordec。
过程2：在给定的环境中编译语句。
*)
module Contcomp

open System.IO
open Absyn
open Machine


(* The intermediate representation between passes 1 and 2 above:  *)

type bstmtordec =
     | BDec of instr list                  (* Declaration of local variable  *)
     | BStmt of stmt                       (* A statement                    *)

(* ------------------------------------------------------------------- *)

(* Code-generating functions that perform local optimizations *)

let rec addINCSP m1 C : instr list =
    match C with
    | INCSP m2            :: C1 -> addINCSP (m1+m2) C1
    | RET m2              :: C1 -> RET (m2-m1) :: C1
    | Label lab :: RET m2 :: _  -> RET (m2-m1) :: C
    | _                         -> if m1=0 then C else INCSP m1 :: C

let addLabel C : label * instr list =          (* Conditional jump to C *)
    match C with
    | Label lab :: _ -> (lab, C)
    | GOTO lab :: _  -> (lab, C)
    | _              -> let lab = newLabel() 
                        (lab, Label lab :: C)

let makeJump C : instr * instr list =          (* Unconditional jump to C *)
    match C with
    | RET m              :: _ -> (RET m, C)
    | Label lab :: RET m :: _ -> (RET m, C)
    | Label lab          :: _ -> (GOTO lab, C)
    | GOTO lab           :: _ -> (GOTO lab, C)
    | _                       -> let lab = newLabel() 
                                 (GOTO lab, Label lab :: C)

let makeCall m lab C : instr list =
    match C with
    | RET n            :: C1 -> TCALL(m, n, lab) :: C1
    | Label _ :: RET n :: _  -> TCALL(m, n, lab) :: C
    | _                      -> CALL(m, lab) :: C

//作用应该是去除C中的第一个Label及之前的所有的代码
let rec deadcode C =
    match C with
    | []              -> []
    | Label lab :: _  -> 
      printfn "作用应该是去除C中的第一个Label及之前的所有的代码"
      printfn $"Label {Label}  lab {lab} _   "
      printfn $"C {C}"
      C
    | _         :: C1 -> deadcode C1

let addNOT C =
    match C with
    | NOT        :: C1 -> C1
    | IFZERO lab :: C1 -> IFNZRO lab :: C1 
    | IFNZRO lab :: C1 -> IFZERO lab :: C1 
    | _                -> NOT :: C

let addJump jump C =                    (* jump is GOTO or RET *)
    let C1 = deadcode C
    match (jump, C1) with
    | (GOTO lab1, Label lab2 :: _) -> if lab1=lab2 then C1 
                                      else GOTO lab1 :: C1
    | _                            -> jump :: C1
    
//去那个label 然后跟着后面的语言?
let addGOTO label restCode =
    addJump (GOTO label) restCode

// 头的 label 
let rec headlab labs = 
    match labs with
        | headLab :: rest -> headLab
        // 拿到头
        | []        -> failwith "Error: unknown break"

let rec addCST i C =
    match (i, C) with
    | (0, ADD        :: C1) -> C1
    | (0, SUB        :: C1) -> C1
    | (0, NOT        :: C1) -> addCST 1 C1
    | (_, NOT        :: C1) -> addCST 0 C1
    | (1, MUL        :: C1) -> C1
    | (1, DIV        :: C1) -> C1
    | (0, EQ         :: C1) -> addNOT C1
    | (_, INCSP m    :: C1) -> if m < 0 then addINCSP (m+1) C1
                               else CSTI i :: C
    | (0, IFZERO lab :: C1) -> addGOTO lab C1
    | (_, IFZERO lab :: C1) -> C1
    | (0, IFNZRO lab :: C1) -> C1
    | (_, IFNZRO lab :: C1) -> addGOTO lab C1
    | _                     -> CSTI i :: C
            
(* ------------------------------------------------------------------- *)

(* Simple environment operations *)

type 'data Env = (string * 'data) list


let rec lookup env x = 
    match env with 
    | []         -> failwith (x + " not found")
    | (y, v)::yr -> if x=y then v else lookup yr x

(* A global variable has an absolute address, a local one has an offset: *)

type Var = 
    | Glovar of int                   (* absolute address in stack           *)
    | Locvar of int                   (* address relative to bottom of frame *)
    | StructMemberLoc of int

type StructTypeEnv = (string * (Var * typ) Env * int) list 

(* The variable environment keeps track of global and local variables, and 
   keeps track of next available offset for local variables *)

type VarEnv = (Var * typ) Env * int

(* The function environment maps a function name to the function's label, 
   its return type, and its parameter declarations *)

type Paramdecs = (typ * string) list
type FunEnv = (label * typ option * Paramdecs) Env

type LabEnv = label list

let rec structLookup env x =
    match env with
    | []                            -> failwith(x + " not found")
    | (name, arglist, size)::rhs    -> if x = name then (name, arglist, size) else structLookup rhs x

let rec structLookupVar env x lastloc =
    match env with
    | []                            -> failwith(x + " not found")
    | (name, (loc, typ))::rhs         -> 
        if x = name then 
            match typ with
            | TypA (_, _)  -> StructMemberLoc (lastloc+1)
            | _                 -> loc 
        else
        match loc with
        | StructMemberLoc lastloc1 -> structLookupVar rhs x lastloc1


let rec dellab labs =
    match labs with
        | lab :: tr ->   tr
        | []        ->   []

(* Bind declared variable in varEnv and generate code to allocate it: *)

// let allocate (kind : int -> Var) (typ, x) (varEnv : VarEnv) : VarEnv * instr list =
//     let (env, fdepth) = varEnv 
//     match typ with
//     | TypA (TypA _, _) -> failwith "allocate: arrays of arrays not permitted"
//     | TypA (t, Some i) ->
//       let newEnv = ((x, (kind (fdepth+i), typ)) :: env, fdepth+i+1)
//       let code = [INCSP i; GETSP; CSTI (i-1); SUB]
//       (newEnv, code)
//     | _ -> 
//       let newEnv = ((x, (kind (fdepth), typ)) :: env, fdepth+1)
//       let code = [INCSP 1]
//       (newEnv, code)

let allocate (kind : int -> Var) (typ, x) (varEnv : VarEnv) (structEnv : StructTypeEnv) : VarEnv * instr list =
    let (env, fdepth) = varEnv 
    match typ with
    | TypA (TypA _, _) -> failwith "allocate: arrays of arrays not permitted"
    | TypA (t, Some i) ->
      let newEnv = ((x, (kind (fdepth+i), typ)) :: env, fdepth+i+1)
      let code = [INCSP i; GETSP; CSTI (i-1); SUB]
      (newEnv, code)
    | TypStruct structName     ->
      let (name, argslist, size) = structLookup structEnv structName
      let code = [INCSP (size + 1); GETSP; CSTI (size); SUB]
      let newEnvr = ((x, (kind (fdepth + size + 1), typ)) :: env, fdepth+size+1+1)
      (newEnvr, code)
    | _ -> 
      let newEnv = ((x, (kind (fdepth), typ)) :: env, fdepth+1)
      let code = [INCSP 1]
      (newEnv, code)

(* Bind declared parameter in env: *)

let bindParam (env, fdepth) (typ, x) : VarEnv = 
    ((x, (Locvar fdepth, typ)) :: env, fdepth+1);

let bindParams paras (env, fdepth) : VarEnv = 
    List.fold bindParam (env, fdepth) paras;
    // 不断的去 绑定参数

(* ------------------------------------------------------------------- *)
(*为全局变量和全局函数构建环境*)
(* Build environments for global variables and global functions *)

// let makeGlobalEnvs(topdecs : topdec list) : VarEnv * FunEnv * instr list = 
//     let rec addv decs varEnv funEnv = 
//         match decs with 
//         | [] -> (varEnv, funEnv, [])
//         | dec::decr -> 
//           match dec with
//           | Vardec (typ, x) ->
//             let (varEnv1, code1) = allocate Glovar (typ, x) varEnv
//             let (varEnvr, funEnvr, coder) = addv decr varEnv1 funEnv
//             (varEnvr, funEnvr, code1 @ coder)
//           | Fundec (tyOpt, f, xs, body) ->
//             addv decr varEnv ((f, (newLabel(), tyOpt, xs)) :: funEnv)
//     addv topdecs ([], 0) []


let makeGlobalEnvs(topdecs : topdec list) : VarEnv * FunEnv * StructTypeEnv * instr list =
    let rec addv decs varEnv funEnv structTypEnv =
        match decs with
        | [] -> (varEnv, funEnv, structTypEnv, [])
        | dec::decr ->
            match dec with
            | Vardec (typ, x) -> 
                let (varEnv1, code1) = allocate Glovar (typ, x) varEnv structTypEnv
                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv1 funEnv structTypEnv
                (varEnvr, funEnvr, structTypEnvr, code1 @ coder)
            | VardecAndAssign (typ, x, e) -> 
                let (varEnv1, code1) = allocate Glovar (typ, x) varEnv structTypEnv
                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv1 funEnv structTypEnv
                (varEnvr, funEnvr, structTypEnvr, code1 @ (cAccess (AccVar(x)) varEnvr funEnvr [] structTypEnv (cExpr e varEnvr funEnvr [] structTypEnv (STI :: (addINCSP -1 coder)))))
            | Fundec (tyOpt, f, xs, body) ->
                addv decr varEnv ((f, (newLabel(), tyOpt, xs)) :: funEnv) structTypEnv
            | Structdec (typName, typEntry) -> 
                let structTypEnv1 = makeStructEnvs typName typEntry structTypEnv
                let (varEnvr, funEnvr, structTypEnvr, coder) = addv decr varEnv funEnv structTypEnv1
                (varEnvr, funEnvr, structTypEnvr, coder)
                
    addv topdecs ([], 0) [] []

(* ------------------------------------------------------------------- *)

(* Compiling micro-C statements:

   * stmt    is the statement to compile
   * varenv  is the local and global variable environment 
   * funEnv  is the global function environment
   * C       is the code that follows the code for stmt
*)

(*编译micro-C语句：
*stmt是要编译的语句
*varenv是局部和全局变量环境
*funEnv是全球功能环境
*C是stmt代码后面的代码
*)

// let rec cStmt stmt (varEnv : VarEnv) (funEnv : FunEnv)  (C : instr list) (lablist : LabEnv)  (structEnv : StructTypeEnv) : instr list = 
//     match stmt with
//     | If(e, stmt1, stmt2) -> 
//       let (jumpend, C1) = makeJump C
//       let (labelse, C2) = addLabel (cStmt stmt2 varEnv funEnv C1 lablist) 
//       cExpr e varEnv funEnv (IFZERO labelse 
//        :: cStmt stmt1 varEnv funEnv (addJump jumpend C2 ) lablist)
//     | While(e, body) ->
//       let labbegin = newLabel()
//       let (jumptest, C1) = 
//            makeJump (cExpr e varEnv funEnv (IFNZRO labbegin :: C))
//       addJump jumptest (Label labbegin :: cStmt body varEnv funEnv C1 lablist)
//     | Expr e -> 
//       cExpr e varEnv funEnv (addINCSP -1 C) 
//     | Block stmts -> 
//       let rec pass1 stmts ((_, fdepth) as varEnv) =
//           match stmts with 
//           | []     -> ([], fdepth)
//           | s1::sr ->
//             let (_, varEnv1) as res1 = bStmtordec s1 varEnv
//             let (resr, fdepthr) = pass1 sr varEnv1 
//             (res1 :: resr, fdepthr) 
//       let (stmtsback, fdepthend) = pass1 stmts varEnv
//       let rec pass2 pairs C = 
//           match pairs with 
//           | [] -> C
//           | (BDec code,  varEnv) :: sr -> code @ pass2 sr C
//           | (BStmt stmt, varEnv) :: sr -> cStmt stmt varEnv funEnv (pass2 sr C ) lablist
//       pass2 stmtsback (addINCSP(snd varEnv - fdepthend) C)
//     | Break ->
//         // 貌似没有走到这里啊
//         let labend = headlab lablist
//         printf $"labend {labend }"
//         addGOTO labend C
//     | Return None -> 
//       RET (snd varEnv - 1) :: deadcode C
//     | Return (Some e) -> 
//       cExpr e varEnv funEnv (RET (snd varEnv) :: deadcode C)
//     | Try(stmt, catchs)  ->
//         let exns = [Exception "ArithmeticalExcption"]
//         let rec lookupExn e1 (es:excep list) exdepth=
//             match es with
//             | hd :: tail -> if e1 = hd then exdepth else lookupExn e1 tail exdepth+1
//             | []-> -1
//         let (labend, C1) = addLabel C
//         let lablist = labend :: lablist
//         let (env, fdepth) = varEnv
//         let varEnv = (env, fdepth+3*catchs.Length)
//         let (tryins, varEnv) = tryStmt stmt varEnv funEnv lablist structEnv []
//         let rec everycatch c  = 
//             match c with
//             | [Catch(exn, body)] -> 
//                 let exnum = lookupExn exn exns 1
//                 let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv [])
//                 let Ctry = PUSHHDLR (exnum ,label) :: tryins @ [POPHDLR]
//                 (Ccatch,Ctry)
//             | Catch(exn, body) :: tr->
//                 let exnum = lookupExn exn exns 1
//                 let (C2, C3) = everycatch tr
//                 let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv C2)
//                 let Ctry = PUSHHDLR (exnum,label) :: C3 @ [POPHDLR]
//                 (Ccatch, Ctry)
//             | [] -> ([],tryins)
//         let (Ccatch, Ctry) = everycatch catchs
//         Ctry @ Ccatch @ C1


let rec cStmt stmt (varEnv : VarEnv) (funEnv : FunEnv) (lablist : LabEnv) (structEnv : StructTypeEnv) (C : instr list) : instr list = 
    match stmt with
    | If(e, stmt1, stmt2) -> 
      let (jumpend, C1) = makeJump C
      let (labelse, C2) = addLabel (cStmt stmt2 varEnv funEnv lablist structEnv C1)
      cExpr e varEnv funEnv lablist structEnv (IFZERO labelse 
       :: cStmt stmt1 varEnv funEnv lablist structEnv (addJump jumpend C2))
    | Switch (e, body) ->
        // let eValueInstr = cExpr e varEnv funEnv structEnv
        // let lab = newLabel()
        // let rec getcode e body = 
        //     match body with
        //     | []          -> [INCSP 0]
        //     | Case (e1, body1):: tail -> 
        //       let e1ValueInstr = cExpr e1 varEnv funEnv structEnv
        //       let sublab = newLabel()
        //       eValueInstr @ e1ValueInstr @ [SUB] @ [IFNZRO sublab] @ cStmt body1 varEnv funEnv structEnv @ [GOTO lab] @ [Label sublab] 
        //            @ getcode e tail
        //     | Default body1 :: tail->
        //         cStmt body1 varEnv funEnv structEnv
      
        // let result = getcode e body @ [Label lab]
        // result
        let (labend, C1) = addLabel C
        let lablist = labend :: lablist
        let rec getcode c  = 
            match c with
            | Case(cond,body) :: tr ->
                let (labnextbody, labnext, C2) = getcode tr
                let (label, C3) = addLabel(cStmt body varEnv funEnv lablist structEnv (addGOTO labend C2))
                let (label2, C4) = addLabel( cExpr (Prim2 ("==", e, cond)) varEnv funEnv lablist structEnv (IFZERO labnext :: C3))
                (label,label2, C4)
            | Default( body ) :: tr -> 
                let (labnextbody,labnext,C2) = getcode tr
                let (label, C3) = addLabel(cStmt body varEnv funEnv lablist structEnv (addGOTO labend C2))
                let (label2, C4) = addLabel(cExpr (Prim2 ("==", e, e)) varEnv funEnv lablist structEnv (IFZERO labnext :: C3))
                (label,label2,C4)
            | [] -> (labend, labend, C1)
        let (label, label2, C2) = getcode body
        C2
    | While(e, body) ->
    //   let labbegin = newLabel()
    //   let (jumptest, C1) = 
    //        makeJump (cExpr e varEnv funEnv lablist structEnv (IFNZRO labbegin :: C))
    //   addJump jumptest (Label labbegin :: cStmt body varEnv funEnv lablist structEnv C1)
        let labbegin = newLabel()
        let (labend,Cend)   = addLabel C
        let lablist = labend :: labbegin :: lablist
        let (jumptest, C1) = 
            makeJump (cExpr e varEnv funEnv lablist structEnv (IFNZRO labbegin :: Cend))
        addJump jumptest (Label labbegin :: cStmt body varEnv funEnv lablist structEnv C1)
    | For(dec, e, opera,body) ->
        let labend   = newLabel()
        let labbegin = newLabel()
        let labope   = newLabel()
        let lablist = labend :: labope :: lablist
        let Cend = Label labend :: C
        let (jumptest, C2) =                                                
            makeJump (cExpr e varEnv funEnv lablist structEnv (IFNZRO labbegin :: Cend)) 
        let C3 = Label labope :: cExpr opera varEnv funEnv lablist structEnv (addINCSP -1 C2)
        let C4 = cStmt body varEnv funEnv lablist structEnv C3    
        cExpr dec varEnv funEnv lablist structEnv (addINCSP -1 (addJump jumptest  (Label labbegin :: C4) ) ) //dec Label: body  opera  testjumpToBegin 指令的顺序  
    | DoWhile(body, e) ->
        let labbegin = newLabel()
        let C1 = cExpr e varEnv funEnv lablist structEnv (IFNZRO labbegin :: C)
        Label labbegin :: cStmt body varEnv funEnv lablist structEnv C1
    | DoUntil(body, e) ->
        let labbegin = newLabel()
        let C1 = cExpr e varEnv funEnv lablist structEnv (IFZERO labbegin :: C)
        Label labbegin :: cStmt body varEnv funEnv lablist structEnv C1
    | Expr e -> 
      cExpr e varEnv funEnv lablist structEnv (addINCSP -1 C) 
    | Block stmts -> 
      let rec pass1 stmts ((_, fdepth) as varEnv) =
          match stmts with 
          | []     -> ([], fdepth)
          | s1::sr ->
            let (_, varEnv1) as res1 = bStmtordec s1 varEnv structEnv
            let (resr, fdepthr) = pass1 sr varEnv1 
            (res1 :: resr, fdepthr) 
      let (stmtsback, fdepthend) = pass1 stmts varEnv
      let rec pass2 pairs C = 
          match pairs with 
          | [] -> C
          | (BDec code,  varEnv) :: sr -> code @ pass2 sr C
          | (BStmt stmt, varEnv) :: sr -> cStmt stmt varEnv funEnv lablist structEnv (pass2 sr C)
      pass2 stmtsback (addINCSP(snd varEnv - fdepthend) C)
    | Return None -> 
      RET (snd varEnv - 1) :: deadcode C
    | Return (Some e) -> 
      cExpr e varEnv funEnv lablist structEnv (RET (snd varEnv) :: deadcode C)
    | Break ->
        let labend = headlab lablist
        addGOTO labend C
     //如果要编译的stmt是Continue
    | Continue ->
        let lablist   = dellab lablist
        let labbegin = headlab lablist
        addGOTO labbegin C
    | Try(stmt, catchs)  ->
        let exns = [Exception "ArithmeticalExcption"]
        let rec lookupExn e1 (es:excep list) exdepth=
            match es with
            | hd :: tail -> if e1 = hd then exdepth else lookupExn e1 tail exdepth+1
            | []-> -1
        let (labend, C1) = addLabel C
        let lablist = labend :: lablist
        let (env, fdepth) = varEnv
        let varEnv = (env, fdepth+3*catchs.Length)
        let (tryins, varEnv) = tryStmt stmt varEnv funEnv lablist structEnv []
        let rec everycatch c  = 
            match c with
            | [Catch(exn, body)] -> 
                let exnum = lookupExn exn exns 1
                let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv [])
                let Ctry = PUSHHDLR (exnum ,label) :: tryins @ [POPHDLR]
                (Ccatch,Ctry)
            | Catch(exn, body) :: tr->
                let exnum = lookupExn exn exns 1
                let (C2, C3) = everycatch tr
                let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv C2)
                let Ctry = PUSHHDLR (exnum,label) :: C3 @ [POPHDLR]
                (Ccatch, Ctry)
            | [] -> ([],tryins)
        let (Ccatch, Ctry) = everycatch catchs
        Ctry @ Ccatch @ C1



// let rec cStmt stmt (varEnv : VarEnv) (funEnv : FunEnv)  (C : instr list) (lablist : LabEnv)  (structEnv : StructTypeEnv) : instr list = 
//     match stmt with
//     | If(e, stmt1, stmt2) -> 
//       let (jumpend, C1) = makeJump C
//       let (labelse, C2) = addLabel (cStmt stmt2 varEnv funEnv C1 lablist structEnv) 
//       cExpr e varEnv funEnv (IFZERO labelse 
//        :: cStmt stmt1 varEnv funEnv (addJump jumpend C2 ) lablist structEnv)
//     | While(e, body) ->
//       let labbegin = newLabel()
//       let (jumptest, C1) = 
//            makeJump (cExpr e varEnv funEnv (IFNZRO labbegin :: C))
//       addJump jumptest (Label labbegin :: cStmt body varEnv funEnv C1 lablist structEnv)
//     | Expr e -> 
//       cExpr e varEnv funEnv (addINCSP -1 C) 
//     | Block stmts -> 
//       let rec pass1 stmts ((_, fdepth) as varEnv) =
//           match stmts with 
//           | []     -> ([], fdepth)
//           | s1::sr ->
//             let (_, varEnv1) as res1 = bStmtordec s1 varEnv
//             let (resr, fdepthr) = pass1 sr varEnv1 
//             (res1 :: resr, fdepthr) 
//       let (stmtsback, fdepthend) = pass1 stmts varEnv
//       let rec pass2 pairs C = 
//           match pairs with 
//           | [] -> C
//           | (BDec code,  varEnv) :: sr -> code @ pass2 sr C
//           | (BStmt stmt, varEnv) :: sr -> cStmt stmt varEnv funEnv (pass2 sr C ) lablist structEnv
//       pass2 stmtsback (addINCSP(snd varEnv - fdepthend) C)
//     | Break ->
//         // 貌似没有走到这里啊
//         let labend = headlab lablist
//         printf $"labend {labend }"
//         addGOTO labend C
//     | Return None -> 
//       RET (snd varEnv - 1) :: deadcode C
//     | Return (Some e) -> 
//       cExpr e varEnv funEnv (RET (snd varEnv) :: deadcode C)
//     | Try(stmt, catchs)  ->
//         let exns = [Exception "ArithmeticalExcption"]
//         let rec lookupExn e1 (es:excep list) exdepth=
//             match es with
//             | hd :: tail -> if e1 = hd then exdepth else lookupExn e1 tail exdepth+1
//             | []-> -1
//         let (labend, C1) = addLabel C
//         let lablist = labend :: lablist
//         let (env, fdepth) = varEnv
//         let varEnv = (env, fdepth+3*catchs.Length)
//         let (tryins, varEnv) = tryStmt stmt varEnv funEnv lablist structEnv []
//         let rec everycatch c  = 
//             match c with
//             | [Catch(exn, body)] -> 
//                 let exnum = lookupExn exn exns 1
//                 let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv [])
//                 let Ctry = PUSHHDLR (exnum ,label) :: tryins @ [POPHDLR]
//                 (Ccatch,Ctry)
//             | Catch(exn, body) :: tr->
//                 let exnum = lookupExn exn exns 1
//                 let (C2, C3) = everycatch tr
//                 let (label, Ccatch) = addLabel( cStmt body varEnv funEnv lablist structEnv C2)
//                 let Ctry = PUSHHDLR (exnum,label) :: C3 @ [POPHDLR]
//                 (Ccatch, Ctry)
//             | [] -> ([],tryins)
//         let (Ccatch, Ctry) = everycatch catchs
//         Ctry @ Ccatch @ C1


and tryStmt tryBlock (varEnv : VarEnv) (funEnv : FunEnv) (lablist : LabEnv) (structEnv : StructTypeEnv) (C : instr list) : instr list * VarEnv = 
    match tryBlock with
    | Block stmts ->
        let rec pass1 stmts ((_, fdepth) as varEnv) = 
            match stmts with
            | []        -> ([], fdepth,varEnv)
            | s1::sr    ->
                let (_, varEnv1) as res1 = bStmtordec s1 varEnv structEnv
                let (resr, fdepthr,varEnv2) = pass1 sr varEnv1
                (res1 :: resr, fdepthr,varEnv2)
        let (stmtsback, fdepthend,varEnv1) = pass1 stmts varEnv
        let rec pass2 pairs C =
            match pairs with
            | [] -> C            
            | (BDec code, varEnv)  :: sr -> code @ pass2 sr C
            | (BStmt stmt, varEnv) :: sr -> cStmt stmt varEnv funEnv lablist structEnv (pass2 sr C)
        (pass2 stmtsback (addINCSP(snd varEnv - fdepthend) C),varEnv1)

and bStmtordec stmtOrDec varEnv (structEnv : StructTypeEnv) : bstmtordec * VarEnv =
    match stmtOrDec with 
    | Stmt stmt    ->
      (BStmt stmt, varEnv) 
    | Dec (typ, x) ->
      let (varEnv1, code) = allocate Locvar (typ, x) varEnv structEnv
      (BDec code, varEnv1)
    | DecAndAssign (typ, x, e) ->
        let (varEnv1, code) = allocate Locvar (typ, x) varEnv structEnv
        (BDec (cAccess (AccVar(x)) varEnv1 [] [] structEnv (cExpr e varEnv1 [] [] structEnv (STI :: (addINCSP -1 code)))), varEnv1)


// and bStmtordec stmtOrDec varEnv : bstmtordec * VarEnv =
//     match stmtOrDec with 
//     | Stmt stmt    ->
//       (BStmt stmt, varEnv) 
//     | Dec (typ, x) ->
//       let (varEnv1, code) = allocate Locvar (typ, x) varEnv 
//       (BDec code, varEnv1)

(* Compiling micro-C expressions: 

   * e       is the expression to compile
   * varEnv  is the compile-time variable environment 
   * funEnv  is the compile-time environment 
   * C       is the code following the code for this expression

   Net effect principle: if the compilation (cExpr e varEnv funEnv C) of
   expression e returns the instruction sequence instrs, then the
   execution of instrs will have the same effect as an instruction
   sequence that first computes the value of expression e on the stack
   top and then executes C, but because of optimizations instrs may
   actually achieve this in a different way.
 *)

// compile 
(*编译micro-C表达式：
*e是要编译的表达式
*varEnv是编译时变量环境
*funEnv是编译时环境
*C是这个表达式的代码后面的代码
净效应原理：如果
表达式e返回指令序列instrs，然后
指令的执行与指令具有相同的效力
首先计算堆栈上表达式e的值的序列
top然后执行C，但由于优化，instrs可能会
实际上，要以不同的方式实现这一点。
*)


and cExpr (e : expr) (varEnv : VarEnv) (funEnv : FunEnv) (lablist : LabEnv) (structEnv : StructTypeEnv) (C : instr list) : instr list =
    match e with

    | Access acc     -> cAccess acc varEnv funEnv lablist structEnv (LDI :: C)
    | Assign(acc, e) -> cAccess acc varEnv funEnv lablist structEnv (cExpr e varEnv funEnv lablist structEnv (STI :: C))
    | OpAssign (op, acc, e) ->
    // cAccess acc varEnv funEnv structEnv
    //     @ [DUP] @ [LDI] @ cExpr e varEnv funEnv structEnv
    //         @ (match op with
    //             | "+" -> [ADD]
    //             | "-" -> [SUB]
    //             | "*" -> [MUL]
    //             | "/" -> [DIV]
    //             | "%" -> [MOD]
    //             | _ -> failwith "could not fine operation"
    //         )  @ [STI]
        cExpr e varEnv funEnv lablist structEnv
            (match op with
            | "+" -> 
                let ass = Assign (acc,Prim2("+",Access acc, e))
                cExpr ass varEnv funEnv lablist structEnv (addINCSP -1 C)
            | "-" ->
                let ass = Assign (acc,Prim2("-",Access acc, e))
                cExpr ass varEnv funEnv lablist structEnv (addINCSP -1 C)
            | "*" -> 
                let ass = Assign (acc,Prim2("*",Access acc, e))
                cExpr ass varEnv funEnv lablist structEnv (addINCSP -1 C)
            | "/" ->
                let ass = Assign (acc,Prim2("/",Access acc, e))
                cExpr ass varEnv funEnv lablist structEnv (addINCSP -1 C)
            | "%" ->
                let ass = Assign (acc,Prim2("%",Access acc, e))
                cExpr ass varEnv funEnv lablist structEnv (addINCSP -1 C)
            | _         -> failwith "Error: unknown unary operator")

    | CstI i         -> addCST i C
 
    | Addr acc       -> cAccess acc varEnv funEnv lablist structEnv C
    | Printf(ope, e1)  ->
         printf "调用printf"
         cExpr e1.[0] varEnv funEnv lablist structEnv
            (match ope with
            | "%d"  -> PRINTI :: C
            | "%c"  -> PRINTC :: C
      
            )
    | Prim1(ope, e1) ->
      cExpr e1 varEnv funEnv lablist structEnv
          (match ope with
           | "!"      -> addNOT C
           | "printi" -> PRINTI :: C
           | "printc" -> PRINTC :: C
           | "~" ->  BITNOT :: C
           | _        -> failwith "unknown primitive 1")
    | Prim2(ope, e1, e2) ->
      cExpr e1 varEnv funEnv lablist structEnv
        (cExpr e2 varEnv funEnv lablist structEnv
           (match ope with
            | "*"   -> MUL  :: C
            | "+"   -> ADD  :: C
            | "-"   -> SUB  :: C
            | "/"   -> DIV  :: C
            | "%"   -> MOD  :: C
            | "=="  -> EQ   :: C
            | "!="  -> EQ   :: addNOT C
            | "<"   -> LT   :: C
            | ">="  -> LT   :: addNOT C
            | ">"   -> SWAP :: LT :: C
            | "<="  -> SWAP :: LT :: addNOT C
            | "&" ->  BITAND :: C
            | "|" ->  BITOR :: C
            | "^" ->  BITXOR :: C
            | "<<" -> BITLEFT :: C
            | ">>" -> BITRIGHT :: C
            | _     -> failwith "unknown primitive 2"))
    | Prim3(e1, e2, e3)    ->
        let (jumpend, C1) = makeJump C  // 最后加labend
        let (labelse, C2) = addLabel (cExpr e3 varEnv funEnv lablist structEnv C1)  // 前面加labelse
        cExpr e1 varEnv funEnv lablist structEnv 
            (IFZERO labelse :: cExpr e2 varEnv funEnv lablist structEnv 
                (addJump jumpend C2))  // addjump: 前面加goto
   
    | Andalso(e1, e2) ->
      match C with
      | IFZERO lab :: _ ->
         cExpr e1 varEnv funEnv lablist structEnv (IFZERO lab :: cExpr e2 varEnv funEnv lablist structEnv C)
      | IFNZRO labthen :: C1 -> 
        let (labelse, C2) = addLabel C1
        cExpr e1 varEnv funEnv lablist structEnv
           (IFZERO labelse 
              :: cExpr e2 varEnv funEnv lablist structEnv (IFNZRO labthen :: C2))
      | _ ->
        let (jumpend,  C1) = makeJump C
        let (labfalse, C2) = addLabel (addCST 0 C1)
        cExpr e1 varEnv funEnv lablist structEnv
          (IFZERO labfalse 
             :: cExpr e2 varEnv funEnv lablist structEnv (addJump jumpend C2))
    | Orelse(e1, e2) -> 
      match C with
      | IFNZRO lab :: _ -> 
        cExpr e1 varEnv funEnv lablist structEnv (IFNZRO lab :: cExpr e2 varEnv funEnv lablist structEnv C)
      | IFZERO labthen :: C1 ->
        let(labelse, C2) = addLabel C1
        cExpr e1 varEnv funEnv lablist structEnv
           (IFNZRO labelse :: cExpr e2 varEnv funEnv lablist structEnv
             (IFZERO labthen :: C2))
      | _ ->
        let (jumpend, C1) = makeJump C
        let (labtrue, C2) = addLabel(addCST 1 C1)
        cExpr e1 varEnv funEnv lablist structEnv
           (IFNZRO labtrue 
             :: cExpr e2 varEnv funEnv lablist structEnv (addJump jumpend C2))
    | Call(f, es) -> callfun f es varEnv funEnv lablist (structEnv : StructTypeEnv) C


// and cExpr (e : expr) (varEnv : VarEnv) (funEnv : FunEnv) (C : instr list) : instr list =
//     match e with
//     | Access acc     -> cAccess acc varEnv funEnv (LDI :: C)
//     | Assign(acc, e) -> cAccess acc varEnv funEnv (cExpr e varEnv funEnv (STI :: C))
//     | CstI i         -> addCST i C
//     | Addr acc       -> cAccess acc varEnv funEnv C
    
//     | Prim1(ope, e1) ->
//       cExpr e1 varEnv funEnv
//           (match ope with
//            | "!"      -> addNOT C
//            | "printi" -> PRINTI :: C
//            | "printc" -> PRINTC :: C
//            | _        -> failwith "unknown primitive 1")
//     | Prim2(ope, e1, e2) ->
//       cExpr e1 varEnv funEnv
//         (cExpr e2 varEnv funEnv
//         // 什么操作符
//            (match ope with
//             | "*"   -> MUL  :: C
//             | "+"   -> ADD  :: C
//             | "-"   -> SUB  :: C
//             | "/"   -> DIV  :: C
//             | "%"   -> MOD  :: C
//             | "=="  -> EQ   :: C
//             | "!="  -> EQ   :: addNOT C
//             | "<"   -> LT   :: C
//             | ">="  -> LT   :: addNOT C
//             | ">"   -> SWAP :: LT :: C
//             | "<="  -> SWAP :: LT :: addNOT C
//             | "<<" -> BITLEFT :: C
//             | _     -> failwith "unknown primitive 2"))
//             // is?do:not
//     // | Prim3(e1, e2 , e3) ->
//     //   let (i1, store1) = eval e1 locEnv gloEnv store
//     //   let (i2, store2) = eval e2 locEnv gloEnv store1
//     //   let (i3, store3) = eval e3 locEnv gloEnv store2
//     //   if i1 = 0 then (i2,store3) else (i3,store3) 

//     //   let (i1, store1) = eval e1 locEnv gloEnv store
//     //   let (i2, store2) = eval e2 locEnv gloEnv store1
//     //   let (i3, store3) = eval e3 locEnv gloEnv store2
//     //   if i1 = 0 then (i2,store3) else (i3,store3) 
//                 // if 表达式需要具有类型“instr list”才能满足上下文类型要求。当前的类型为“'a * 'b”。F# Compiler1
//     // | Printf(ope, e1)  ->
//     //      cExpr e1.[0] varEnv funEnv  
//     //         (match ope with
//     //         | "%d"  -> PRINTI :: C
//     //         | "%c"  -> PRINTC :: C
//     //         | "%f"  -> PRINT_FLOAT :: C
//     //         // printflot 这种级别的函数 需要在后端定义了
//     //         )
//     | Andalso(e1, e2) ->
//       match C with
//       | IFZERO lab :: _ ->
//          cExpr e1 varEnv funEnv (IFZERO lab :: cExpr e2 varEnv funEnv C)
//       | IFNZRO labthen :: C1 -> 
//         let (labelse, C2) = addLabel C1
//         cExpr e1 varEnv funEnv
//            (IFZERO labelse 
//               :: cExpr e2 varEnv funEnv (IFNZRO labthen :: C2))
//       | _ ->
//         let (jumpend,  C1) = makeJump C
//         let (labfalse, C2) = addLabel (addCST 0 C1)
//         cExpr e1 varEnv funEnv
//           (IFZERO labfalse 
//              :: cExpr e2 varEnv funEnv (addJump jumpend C2))
//     | Orelse(e1, e2) -> 
//       match C with
//       | IFNZRO lab :: _ -> 
//         cExpr e1 varEnv funEnv (IFNZRO lab :: cExpr e2 varEnv funEnv C)
//       | IFZERO labthen :: C1 ->
//         let(labelse, C2) = addLabel C1
//         cExpr e1 varEnv funEnv
//            (IFNZRO labelse :: cExpr e2 varEnv funEnv
//              (IFZERO labthen :: C2))
//       | _ ->
//         let (jumpend, C1) = makeJump C
//         let (labtrue, C2) = addLabel(addCST 1 C1)
//         cExpr e1 varEnv funEnv
//            (IFNZRO labtrue 
//              :: cExpr e2 varEnv funEnv (addJump jumpend C2))
//     | Call(f, es) -> callfun f es varEnv funEnv C

(* Generate code to access variable, dereference pointer or index array: *)

// and cAccess access varEnv funEnv C = 
//     match access with 
//     | AccVar x   ->
//       match lookup (fst varEnv) x with
//       | Glovar addr, _ -> addCST addr C
//       | Locvar addr, _ -> GETBP :: addCST addr (ADD :: C)
//     | AccDeref e ->
//       cExpr e varEnv funEnv C
//     | AccIndex(acc, idx) ->
//       cAccess acc varEnv funEnv (LDI :: cExpr idx varEnv funEnv (ADD :: C))



and cAccess access varEnv funEnv lablist structEnv C = 
    match access with 
    | AccVar x   ->
      match lookup (fst varEnv) x with
      | Glovar addr, _ -> addCST addr C
      | Locvar addr, _ -> GETBP :: addCST addr (ADD :: C)
    | AccDeref e ->
      cExpr e varEnv funEnv lablist structEnv C
    | AccIndex(acc, idx) ->
      cAccess acc varEnv funEnv lablist structEnv (LDI :: cExpr idx varEnv funEnv lablist structEnv (ADD :: C))
    | AccStruct (AccVar stru, AccVar memb) ->
        let (loc, TypStruct structname)   = lookup (fst varEnv) stru
        let (name, argslist, size) = structLookup structEnv structname
        match structLookupVar argslist memb 0 with
        | StructMemberLoc varLocate ->
            match lookup (fst varEnv) stru with
            | Glovar addr, _ -> addCST (addr - (size+1) + varLocate) C
            | Locvar addr, _ -> GETBP :: addCST (addr - (size+1) + varLocate) (ADD ::  C)

// and cAccess access varEnv funEnv lablist  C = 
//     match access with 
//     | AccVar x   ->
//       match lookup (fst varEnv) x with
//       | Glovar addr, _ -> addCST addr C
//       | Locvar addr, _ -> GETBP :: addCST addr (ADD :: C)
//     | AccDeref e ->
//       cExpr e varEnv funEnv lablist  C
//     | AccIndex(acc, idx) ->
//       cAccess acc varEnv funEnv lablist  (LDI :: cExpr idx varEnv funEnv lablist structEnv (ADD :: C))
//     | AccStruct (AccVar stru, AccVar memb) ->
//         let (loc, TypStruct structname)   = lookup (fst varEnv) stru
//         let (name, argslist, size) = structLookup  structname
//         match structLookupVar argslist memb 0 with
//         | StructMemberLoc varLocate ->
//             match lookup (fst varEnv) stru with
//             | Glovar addr, _ -> addCST (addr - (size+1) + varLocate) C
//             | Locvar addr, _ -> GETBP :: addCST (addr - (size+1) + varLocate) (ADD ::  C)
    



and cAccess access varEnv funEnv lablist structEnv C = 
    match access with 
    | AccVar x   ->
      match lookup (fst varEnv) x with
      | Glovar addr, _ -> addCST addr C
      | Locvar addr, _ -> GETBP :: addCST addr (ADD :: C)
    | AccDeref e ->
      cExpr e varEnv funEnv lablist structEnv C
    | AccIndex(acc, idx) ->
      cAccess acc varEnv funEnv lablist structEnv (LDI :: cExpr idx varEnv funEnv lablist structEnv (ADD :: C))
    | AccStruct (AccVar stru, AccVar memb) ->
        let (loc, TypStruct structname)   = lookup (fst varEnv) stru
        let (name, argslist, size) = structLookup structEnv structname
        match structLookupVar argslist memb 0 with
        | StructMemberLoc varLocate ->
            match lookup (fst varEnv) stru with
            | Glovar addr, _ -> addCST (addr - (size+1) + varLocate) C
            | Locvar addr, _ -> GETBP :: addCST (addr - (size+1) + varLocate) (ADD ::  C)
    

(* Generate code to evaluate a list es of expressions: *)

and cExprs es varEnv funEnv lablist (structEnv : StructTypeEnv) C = 
    match es with 
    | []     -> C
    | e1::er -> cExpr e1 varEnv funEnv lablist structEnv (cExprs er varEnv funEnv lablist structEnv C)


// and cExprs es varEnv funEnv C = 
//     match es with 
//     | []     -> C
//     | e1::er -> cExpr e1 varEnv funEnv (cExprs er varEnv funEnv C)

(* Generate code to evaluate arguments es and then call function f: *)

and callfun f es varEnv funEnv lablist (structEnv : StructTypeEnv) C : instr list =
    let (labf, tyOpt, paramdecs) = lookup funEnv f
    let argc = List.length es
    if argc = List.length paramdecs then
      cExprs es varEnv funEnv lablist structEnv (makeCall argc labf C)
    else
      failwith (f + ": parameter/argument mismatch")

// and callfun f es varEnv funEnv C : instr list =
//     let (labf, tyOpt, paramdecs) = lookup funEnv f
//     let argc = List.length es
//     if argc = List.length paramdecs then
//       cExprs es varEnv funEnv (makeCall argc labf C)
//     else
//       failwith (f + ": parameter/argument mismatch")

(* Compile a complete micro-C program: globals, call to main, functions *)

let cProgram (Prog topdecs) : instr list = 
    let _ = resetLabels ()
    let ((globalVarEnv, _), funEnv, structEnv, globalInit) = makeGlobalEnvs topdecs
    // let ((globalVarEnv, _), funEnv, globalInit) = makeGlobalEnvs topdecs
    let compilefun (tyOpt, f, xs, body) =
        let (labf, _, paras) = lookup funEnv f
        let (envf, fdepthf) = bindParams paras (globalVarEnv, 0)
        let C0 = [RET (List.length paras-1)]
        let code = cStmt body (envf, fdepthf) funEnv [] structEnv C0
        Label labf :: code
    let functions = 
        List.choose (function 
                        | Fundec (rTy, name, argTy, body) 
                                    -> Some (compilefun (rTy, name, argTy, body))
                        | Vardec _ -> None
                        | VardecAndAssign _ -> None
                        | Structdec _ -> None)
                        topdecs
    let (mainlab, _, mainparams) = lookup funEnv "main"
    let argc = List.length mainparams
    globalInit 
    @ [LDARGS argc; CALL(argc, mainlab); STOP] 
    @ List.concat functions


// let cProgram (Prog topdecs) : instr list = 
//     let _ = resetLabels ()
//     let ((globalVarEnv, _), funEnv, globalInit) = makeGlobalEnvs topdecs
//     let compilefun (tyOpt, f, xs, body) =
//         let (labf, _, paras) = lookup funEnv f
//         // 找到这个func 嘛
//         let (envf, fdepthf) = bindParams paras (globalVarEnv, 0)
//         let C0 = [RET (List.length paras-1)]
//         let code = cStmt body (envf, fdepthf) funEnv C0 []
//         Label labf :: code
//     let functions = 
//         List.choose (function 
//                          | Fundec (rTy, name, argTy, body) 
//                                     -> Some (compilefun (rTy, name, argTy, body))
//                          | Vardec _ -> None)
//                          topdecs
//     let (mainlab, _, mainparams) = lookup funEnv "main"
//     let argc = List.length mainparams
//     globalInit 
//     @ [LDARGS argc; CALL(argc, mainlab); STOP] 
//     @ List.concat functions

(* Compile the program (in abstract syntax) and write it to file
   fname; also, return the program as a list of instructions.
 *)
(*编译程序（抽象语法）并将其写入文件
fname；另外，将程序作为指令列表返回。
*)
let intsToFile (inss : int list) (fname : string) = 
    File.WriteAllText(fname, String.concat " " (List.map string inss))

let contCompileToFile program fname = 
    let instrs   = cProgram program 
    let bytecode = code2ints instrs
    intsToFile bytecode fname; instrs

(* Example programs are found in the files ex1.c, ex2.c, etc *)
(*示例程序可在ex1.c、ex2.c等文件中找到*)