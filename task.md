## 可选改进列表

### 词法
- 手动实现词法分析

### 语法

- [x] do while/until/for 语法
- [x] 三目运算  ? :
- 变量初始化  有
- 字符串支持 有
  - 模板字符串
  - 单引号，双引号嵌套
  
  - [三引号字符串字面量](https://cn.julialang.org/JuliaZH.jl/latest/manual/strings/#三引号字符串字面量)
- python语法，无须"{}" 无须";"
- 给出 递归下降分析的实现

### 语义
- 类型检查
- 类型推理
- 多态类型
- 安全数组，越界检查

### 特性
- [位运算](https://cn.julialang.org/JuliaZH.jl/latest/manual/mathematical-operations/#位运算符) 有
- 布尔类型， 有
- 逻辑运算 有
- [复数，有理数](https://cn.julialang.org/JuliaZH.jl/latest/manual/complex-and-rational-numbers/)
- [链式比较](https://cn.julialang.org/JuliaZH.jl/latest/manual/mathematical-operations/#链式比较)
- [正则表达式](https://cn.julialang.org/JuliaZH.jl/latest/manual/strings/#正则表达式)
- 复合数据类型
  
  - 列表 list cons ，记录 record，元组 tuple
- 模式匹配
- 宏，[元编程](https://cn.julialang.org/JuliaZH.jl/latest/manual/metaprogramming/)
- immutable 机制
- 异常
- 高阶函数，lamda表达式
- 协程
- 生成器
- 异步
- 面向对象
- 逻辑式
- 关系式

## 错误处理
- 提示错误行，列

### 运行时
- 垃圾回收
- 语言库

### 代码生成
- x86,llvm,risc-v
- wasm3虚拟机

### 链接
- 多模块机制，多文件编译

### 其他语言
- decaf
- 类 java ,python,lua,v等语言
