void main() { 
  // int intVal=97;
    // int intVal=933333333337;
    int intVal=100000001;
    // ERROR: Value was either too large or too small for an Int32. in file example\castNotPrintf.c near line 3, column 27
  char charVal=(char)intVal;
  // float flVal=4131.13;
  //  float flVal=100000001;
  //  float flVal=2147483648;
  //  定义不了 太大了 
  //  ERROR: Value was either too large or too small for an Int32. 
  //  最大的int 
  //  float flVal=2147483647;
  //  float flVal=2147483646.2;
  //  -2147483648
    float flVal=2147483644.2;
    // -2147483648  

    
    // 1325400064 他最多只能存 大概这个量级的 所以大于的要当作float 来 计算

  // 1324618814
  //  float flVal=2047483646.2;
  // 2047483648 
   printf(" \n print Float to int  val :\n");
   int flToInt=(int)flVal;
  //  printf("printFloat %f \n",flToInt);
  print(flToInt);
  // printFloat((int)flVal);
  printf(" \n int val :\n");
  print(intVal);
   printf("\n char val:\n");
   printCh(charVal);
  //  如果 printf 走的就是 printf 的逻辑 ，而不是cast 了
  // printf("int val : %d, char val: %c\n", intVal,(char)intVal);
}