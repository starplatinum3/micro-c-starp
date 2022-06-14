module interpc.top.starp.util.PrintUtil

//需要写成public 
//let public printMap (map:Map<int,string>) =
//    for key in map.Keys do
//        printfn "======"
//        printf "key: %d\n" key
//        printf "val: %s" map.[key]
//        printfn ""
//
////https://www.runoob.com/java/java-override-overload.html
////可以参数不同吗 这个叫重载还是。。重写
////重载吧
////https://www.coder.work/article/7597663
////不能重载 怎么办
let public printMap (map:Map<int,int>) =
    for key in map.Keys do
        printfn "======"
//        Format string can be replaced with an interpolated string
        printf "key: %d\n" key
        printf "val: %d" map.[key]
        printfn ""
//
//参数类型必须给 而且还没有重载，这怎么办，动态语言的动态性没有 重载也没有 这写着玩呢
//The type 'Microsoft.FSharp.Collections.Map<_,_>' expects 2 type argument(s) but is given 0    
//let public printMap (map:Map) =
//    for key in map.Keys do
//        printfn "======"
//        printf "key: %d\n" key
//        printf "val: %d" map.[key]
//        printfn ""