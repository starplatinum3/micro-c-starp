# dotnet build -v n interpc.fsproj

import os

# path=r"example\testDecAndAssign2"
# 没有编译成功
# 虽然报错 但是 其实是有的吗 不是啊 out之前就有了
# Unhandled exception. System.FormatException: Input string was not in a correct format.
#    at System.Number.ThrowOverflowOrFormatException(ParsingStatus status, TypeCode type)
#    at System.Int32.Parse(String s)
#    at Machine.readfile(String filename) in D:\school\compile\plzoofs\microc\Machine.cs:line 232
#    at Machine.execute(String[] arglist, Boolean trace) in D:\school\compile\plzoofs\microc\Machine.cs:line 59
#    at Machine.Main(String[] args) in D:\school\compile\plzoofs\microc\Machine.cs:line 36
# path=r"D:\school\compile\plzoofs\microc\example\try"
# path=r"example\try"
# path=r"example\struct"
# path=r"example\structPrim"
# example\structPrim.c
# path=r"example\abs"
# path=r"example\try"
# path=r"example\ex1"
# path=r"example\testPrim3"
# path=r"example\testBitOpCompNoCmt"
# path=r"example\switchComp"
# path=r"example\testPreInc"
# args="5"
# path=r"example\testPreDec"
# args="5"

# path=r"example\opAssignPrint"
# args="7"

# path=r"example\return"
# args="7"

# path=r"example\ex3"
# args="7"

# path=r"example\try"
# args="7"

# path=r"example\structPrim"
# args=""

# path=r"example\testPrim3"
# args="2"

# path=r"example\switchComp"
# # args="2"
# # args="3"
# args="0"

# path=r"example\switchComp"
# args="0"

path=r"example\cmt"
args="0"


# example\return.c


# path=r"example\testBitOpComp"
# path=r"example\opAssign"
# path=r"example\opAssignPrint"
# path=r"example\continue"
# path=r"example\try"
# path=r"example\testDoUntil"
# path=r"example\bool"
# 我定义了
# D:\school\compile\plzoofs\microc\example\testDoUntil.c
# example\opAssignPrint.c
# D:\school\compile\plzoofs\microc\example\ex1.c
# example\abs.c
# 
# example\struct.c
# D:\school\compile\plzoofs\microc\example\try.c
os.system("dotnet clean  microc.fsproj   ")
os.system("dotnet build  microc.fsproj ")

# 如果编译失败了 之后的一次要clean 不然会出错
# Unhandled exception. System.FormatException: Input string was not in a correct format.

# args="3"
# args="7"
# args="2"
# args="7"
# args="2"
# args="1"
# args="5"
# args="2"
# 编译过一次就寄了
# powershell   ./bin/Debug/net6.0/microc.exe -g  example\testDecAndAssign2.c 1 
# os.system(f"powershell ./bin/Debug/net6.0/microc.exe -g {path}.c 1")
# os.system(f"powershell ./bin/Debug/net6.0/microc.exe  {path}.c 1")
# compileCmd=f"powershell ./bin/Debug/net6.0/microc.exe  {path}.c "
# compileCmd=f"powershell ./bin/Debug/net6.0/microc.exe  -g {path}.c "
compileCmd=f"dotnet run --project microc.fsproj  {path}.c "
# 没有出 out 
# dotnet run --project microc.fsproj example/ex1.c 
print("compileCmd",compileCmd)
os.system(compileCmd)
print(f"运行 {path}.out")
# 大小写
# -t 会输出 堆栈
# dotnet clean  microc.fsproj
# dotnet build  microc.fsproj 
# dotnet run --project microc.fsproj  example\switchComp.c 1
# powershell dotnet run -t --project machine.csproj example\switchComp.out 1
machineCmd=f"powershell dotnet run -t --project machine.csproj   {path}.out {args}"
# machineCmd=f"powershell dotnet run  --project machine.csproj   {path}.out {args}"
# machineCmd=f"powershell dotnet run  --project machine.csproj   {path}.out {args}"
print("machineCmd",machineCmd)
os.system(machineCmd)

# os.system("machineCmd",machineCmd)
# @REM set path=example\testDecAndAssign2
# @REM set src=path.c
# powershell dotnet run --project machine.csproj example\testDecAndAssign2.out 3

# dotnet clean  microc.fsproj
# dotnet build  microc.fsproj 
# dotnet run --project microc.fsproj  example\switchComp.c 1
# powershell dotnet run -t --project machine.csproj example\switchComp.out 1