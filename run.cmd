
dotnet clean  interpc.fsproj 
dotnet build -v n interpc.fsproj
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testChar.c 1 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testDecAndAssign.c 1 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPreInc.c 8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPreDec.c 8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\cast.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\castNotPrintf.c 
@REM D:\school\compile\plzoofs\microc\example\continue.c
@REM 没实现  continue
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\continue.c 8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\struct.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\structPrim.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testContinue.c 8 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\doWhileTest.c 8 
@REM D:\school\compile\plzoofs2\microc\example\doWhileTest.c
@REM 不行
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\structPrim.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testBitLeftShift.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testBitOp.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\bool.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\opAssign.c  8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\switch.c 1
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\cmt.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\return.c 3
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\abs.c -3
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\minusNum.c -3
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\abs.c -3
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrim3.c -3
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrim3.c 1
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\char.c 1
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrintf.c 1
powershell  ./bin/Debug/net6.0/interpc.exe example\testDoUntil.c 1
@REM example\testDoUntil.c
@REM example\testPrintf.c
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrim3.c 2
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrim3.c 1
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrim3.c 6
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPrim3.c 2
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\while2.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\continue.c 6
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testContinue.c 6
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\try.c 
@REM 没有实现

@REM 需要括号 ，优先级还没搞定
@REM  Prim2 (">", Access (AccVar "n"), Prim3 (CstI 5, CstI 1, CstI 0)))));
@REM inter |prim3 放前面 没有用处
@REM 先 n>5 这种 优先
@REM example\minusNum.c
@REM example\abs.c
@REM example\return.c
@REM example\cmt.c
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\try.c 
@REM example\try.c
@REM example\switch.c
@REM example\opAssign.c
@REM example\bool.c
@REM example\testBitOp.c
@REM example\testBitLeftShift.c
@REM example\structPrim.c
@REM example\cast.c
@REM example\testPreDec.c
@REM run code 可以运行

