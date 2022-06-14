let list1 = [ 1; 5; 100; 450; 788 ]

// Pattern matching by using the cons pattern and a list
// pattern that tests for an empty list.
let rec printList listx =
    match listx with
    // 头是只有一个头的 最前面一个  tail是除了头以外的所有 递归的去printList
    | head :: tail -> printf "%d " head; printList tail
    | [] -> printfn ""

printList list1
// Pattern matching with multiple alternatives on the same line.
// https://docs.microsoft.com/zh-cn/dotnet/fsharp/language-reference/match-expressions
// Pattern matching with multiple alternatives on the same line.
//在同一条线上有多个备选方案的模式匹配。
let filter123 x =
    match x with
    | 1 | 2 | 3 -> printfn "Found 1, 2, or 3!"
    | a -> printfn "%d" a

// The same function written with the pattern matching
// function syntax.
//使用模式匹配编写相同的函数
//函数语法。
// a 是一个默认的吗
let filterNumbers =
    function | 1 | 2 | 3 -> printfn "Found 1, 2, or 3!"
             | a -> printfn "%d" a
             