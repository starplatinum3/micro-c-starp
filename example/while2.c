
int main()
{
    int i = 1;
    int sum = 0;
    while (sum < 10)
    {
        print(sum);
        sum = sum + i;
        i = i + 1;
    }

    print(i);
    print(sum);
}
