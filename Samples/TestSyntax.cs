using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TestSyntax
{
    class SomeClass
    {
        public int field1;
        public float field2;
        public string FirstName;
    }
    class Program
    {
        public static void SwitchPattern(object o)
        {
            switch (o)
            {
            case null:
                Console.WriteLine("it's a constant pattern");
                break;
            case int i:
                Console.WriteLine("it's an int");
                break;
            case SomeClass p when p.FirstName.StartsWith("No"):
                Console.WriteLine($"a No person {p.FirstName}");
                break;
            case SomeClass p:
                Console.WriteLine($"any other person {p.FirstName}");
                break;
            case var x:
                Console.WriteLine($"it's a var pattern with the type {x?.GetType().Name} ");
                break;
            default:
                break;
            }
        }
        //private void AddNumericOption<T>(Func<string> name, Func<string> tooltip, Func<T> getValue, Action<T> setValue, T? min, T? max, T? interval, string fieldId)
        //    where T : struct
        //{
        //    name ??= () => fieldId;
        //}

        static void Main(string[] args)
        {
            int? dataNull;
            dataNull = null;
            dataNull = 23;
            Console.WriteLine($"dataNull = {dataNull}");

            SomeClass data;
            data = new SomeClass();
            data.field1 = 23;
            data.FirstName = "Norman";

            if (data?.field1 == 23)
                Console.WriteLine("Text");

            int foo = 23;
            int foo1 = 32;
            switch (foo)
            {
            case 0 when (foo1 == 32):
                Console.WriteLine("zero");
                break;
            default:
                Console.WriteLine("default");
                break;
            }

            SwitchPattern(data);
            SwitchPattern(foo);
            SwitchPattern(data.field2);
        }
    }
}
