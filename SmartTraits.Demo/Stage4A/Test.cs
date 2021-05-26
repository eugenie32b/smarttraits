using System;

namespace SmartTraits.Tests.Stage4A
{
    static class Test
    {
        public static void Example()
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("### Test 4A");
            Console.Out.WriteLine("");

            var a = new ExampleA() { FirstName = "Jim", LastName = "Smith" };

            Console.Out.WriteLine(a.GetType().FullName);
            Console.Out.WriteLine(a.GetFullName());

            Console.Out.WriteLine("");

            var b = new ExampleB() { FirstName = "Jane", MiddleName = "J.", LastName = "Silver", Address = "123 Main st.", City = "New York", State = "NY", ZipCode = "10012" };

            Console.Out.WriteLine(b.GetType().FullName);
            Console.Out.WriteLine(b.GetFullName());
            Console.Out.WriteLine(b.GetAddress());

            Console.Out.WriteLine("");

            var c = new ExampleC() { FirstName = "Joe", MiddleName = "M.", LastName = "Delecroix", Address = "456 Short st.", City = "Los Angeles", State = "CA", ZipCode = "90124" };
            Console.Out.WriteLine(c.GetType().FullName);
            Console.Out.WriteLine(c.GetFullAddress());
        }
    }
}
