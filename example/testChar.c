void main(int n)
{
    char ch;
    // ch='1';
    // 是说单引号不对吗
    // ERROR: Lexer error: illegal symbol in file example\testChar.c near line 4, column 8
    // ch = "1";
    // ch = "c";
// ch="c";
ch='c';
    // print ch;
// println ch;
// printc ch;
printCh ch;
printCh (ch);
char ret;
ret='\n';
// ERROR: Lexer error: illegal symbol in file example\testChar.c near line 17, column 5
// 打印不了回车。。
// printCh ('\n');
// 这个打印不了
printCh (ret);
// 可以带着括号
}
