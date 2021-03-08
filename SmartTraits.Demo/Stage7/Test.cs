using System;

namespace SmartTraits.Tests.Stage7
{
    static class Test
    {
        public static void Example()
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("### Test 7");
            Console.Out.WriteLine("");

            var a = new ExampleA() { FirstName = "Jim", LastName = "Smith" };

            Console.Out.WriteLine(a.GetType().FullName);
            Console.Out.WriteLine(a.GetFullName());
            Console.Out.WriteLine(a.GetT4ExampleA());

            Console.Out.WriteLine("");

            var b = new ExampleB() { FirstName = "Jane", LastName = "Silver", Address = "123 Main st.", City = "New York", State = "NY", ZipCode = "10012" };

            Console.Out.WriteLine(b.GetType().FullName);
            Console.Out.WriteLine(b.GetFullName());
            Console.Out.WriteLine(b.GetAddress());
            Console.Out.WriteLine(b.GetT4ExampleB());
        }
    }
}
