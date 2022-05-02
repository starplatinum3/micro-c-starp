// micro-C example 23 -- exponentially slow Fibonacci

void main(int n) {
  int i;
  i = 0;
  while (i < n) {
    print i;
    print fib(i);
    println;
    // 只能打印一个ln 不能 打印字符
    i = i + 1;
  }
}

int fib(int n) {
  if (n < 2)
    return 1;
  else 
    return fib(n-2) + fib(n-1);
}
