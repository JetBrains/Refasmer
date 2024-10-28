using System.Collections.Generic;
using System.Linq;

namespace RefasmerTestAssembly;

public class ClassWithLambda
{
    public int Method()
    {
        return new List<int>{1,2,3}.Select(x => x * 2).Sum();
    }
}