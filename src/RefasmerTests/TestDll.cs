namespace RefasmerTests
{
#pragma warning disable 649
#pragma warning disable 169
// ReSharper disable InconsistentNaming
    public interface IPublicInterface
    {
        int PublicInterfaceMethod();
    }

    internal interface IInternalInterface
    {
        int InternalInterfaceMethod();
    }
    
    
    public class PublicClass : IInternalInterface, IPublicInterface
    {
        public int PublicInt;
        protected int ProtectedInt;
        private int PrivateInt;

        public string PublicString;
        protected string ProtectedString;
        private string PrivateString;

        public static int PublicStaticInt;
        protected static int ProtectedStaticInt;
        private static int PrivateStaticInt;

        public static string PublicStaticString;
        protected static string ProtectedStaticString;
        private static string PrivateStaticString;

        public int InternalInterfaceMethod()
        {
            throw new System.NotImplementedException();
        }

        public int PublicInterfaceMethod()
        {
            throw new System.NotImplementedException();
        }
        
        public string PublicMethod( int a )
        {
            return $"public {a}";
        }

        protected string ProtectedMethod( int a )
        {
            return $"protected {a}";
        }
        
        private string PrivateMethod( int a )
        {
            return $"private {a}";
        }

        public static string PublicStaticMethod( int a )
        {
            return $"public static {a}";
        }

        protected static string ProtectedStaticMethod( int a )
        {
            return $"protected static {a}";
        }
        
        private static string PrivateStaticMethod( int a )
        {
            return $"private static {a}";
        }

        public class PublicNestedClass
        {
            public int Int;
        }

        protected class ProtectedNestedClass
        {
            public int Int;
        }
        
        private class PrivateNestedClass
        {
            public int Int;
        }
    }

    public struct PublicStruct
    {
        public int PublicInt;
        private string PrivateString;
    }

    internal struct InternalStruct
    {
        public int PublicInt;
        private string PrivateString;
    }
// ReSharper restore InconsistentNaming
#pragma warning restore 169
#pragma warning restore 649
}