using System;

namespace RefasmerTestDll
{
    public class Class1
    {
        public static readonly int[] Data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

        public static void Foo()
        {
            Console.Out.WriteLine(Data);
            Console.Out.WriteLine("Hello world!");
        }
    }
}