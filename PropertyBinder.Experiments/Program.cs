using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertyBinder.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            var consumer = new Consumer();
            var model1 = new Model
            {
                Data1 = 1.0,
                Data2 = 2.0,
                Data3 = 3.0,
                Data4 = 4.0,
                Data5 = 5.0,
                Data6 = 1.0,
                Data7 = 2.0,
                Data8 = 3.0,
                Data9 = 4.0,
                Data10 = 5.0
            };

            var model2 = new Model
            {
                Data1 = 2.0,
                Data2 = 3.0,
                Data3 = 4.0,
                Data4 = 5.0,
                Data5 = 6.0,
                Data6 = 2.0,
                Data7 = 3.0,
                Data8 = 4.0,
                Data9 = 5.0,
                Data10 = 6.0
            };

            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 50000; ++i)
            {
                consumer.Source.Model = model1;
                if (consumer.FormattedAggregate != "30.0")
                {
                    throw new Exception();
                }
                consumer.Source.Model = model2;
                if (consumer.FormattedAggregate != "40.0")
                {
                    throw new Exception();
                }
            }

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
        }
    }
}
