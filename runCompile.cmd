dotnet build  microc.fsproj 
@REM powershell   ./bin/Debug/net6.0/microc.exe -g example\try.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testChar.c 1 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testDecAndAssign.c 1 
@REM powershell   ./bin/Debug/net6.0/microc.exe -g  example\testDecAndAssign.c 1 
@REM powershell   ./bin/Debug/net6.0/microc.exe -g example\testDecAndAssign2.c 1 
@REM 运行过了解释器之后就行了 是不是文件会改变啊
powershell   ./bin/Debug/net6.0/microc.exe example\testDecAndAssign2.c 1 
@REM powershell   ./bin/Debug/net6.0/microc.exe example\testDecAndAssign2.c 1 
@REM powershell dotnet run --project machine.csproj ex9.out 3
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPreInc.c 8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testPreDec.c 8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\cast.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\struct.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\structPrim.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testBitLeftShift.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testBitOp.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\bool.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\opAssign.c  8
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\switch.c 1
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

