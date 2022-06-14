dotnet build  microc.fsproj 
@REM powershell   ./bin/Debug/net6.0/microc.exe -g example\try.c 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testChar.c 1 
@REM powershell  ./bin/Debug/net6.0/interpc.exe example\testDecAndAssign.c 1 
@REM powershell   ./bin/Debug/net6.0/microc.exe -g  example\testDecAndAssign.c 1 
@REM powershell   ./bin/Debug/net6.0/microc.exe -g  example\testDecAndAssign2.c 1 
@REM cmd 变量
@REM set path=example\testDecAndAssign2
@REM set src=path.c
powershell dotnet run --project machine.csproj example\testDecAndAssign2.out 3
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

