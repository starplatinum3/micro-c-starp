struct StructTest {
  int intVal;
  char charVal;
  int a[3];
};

void main() { 

  struct StructTest t;
  struct StructTest t2;
  t.intVal = 341;
  t.charVal = 'c';
  t2.intVal = 5141;
  printf("%d\n", t.intVal);
  printf("%c\n", t.charVal);
  printf("%d\n", t2.intVal);

}
