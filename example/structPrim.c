// struct StructTest {
//   int intVal;
//   char charVal;
// };

struct stru {
  int intVal;
  char charVal;
};

void main() { 

  struct stru t;
  struct stru t2;
  t.intVal = 341;
  t.charVal = 'c';
  t2.intVal = 5141;
  // 没有定义printf 
  // printf("%d\n", t.intVal);
  // printf("%c\n", t.charVal);
  // printf("%d\n", t2.intVal);

    print t.intVal;
      // printc t.charVal;
      // print t.charVal;
       printCh t.charVal;
  // printf("%c\n", t.charVal);
  // printf("%d\n", t2.intVal);

}
