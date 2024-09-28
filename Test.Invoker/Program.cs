using Test.Utils;

namespace Test.Invoker
{
    public class WithRunValue
    {
        public WithRunValue(int value)
        {
            Value = value;
        }
        public int run ()
        {
            return 456;
        }
        public int Value;
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World! {0}", Util.gv());
            var hv = new HasValue(123);
            Func<object, int> i = (object v) =>
            {
                try
                {
                    return Util.getValue<HasValue, int>(v as HasValue);
                }
                catch
                {
                    return int.Parse(v.ToString());
                }
            };
            Console.WriteLine("Hello, World! {0}", Util.getRun<WithRunValue, int>(new WithRunValue(123)));
            //Console.WriteLine("Hello, World! {0}", i(hv));
            //Console.WriteLine("Hello, World! {0}", i(new WithRunValue(123)));
        }
    }
}
