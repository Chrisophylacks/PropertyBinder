using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PropertyBinder.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            DebugTest();
            //PerformanceTest();
        }

        private sealed class BindingAction<T>
            where T: class
        {
            public Action<T> Action;
        }

        private sealed class Binding1
        {
            public Action Action;
        }

        private interface IBinding
        {
            void Execute();
        }

        private sealed class Binding2<T> : IBinding
            where T : class
        {
            public BindingAction<T> Action;

            public T Context;

            public void Execute()
            {
                Action.Action(Context);
            }
        }

        private sealed class BindingAction3
        {
            public Action<object> Action;
        }

        private sealed class Binding3
        {
            public BindingAction3 Action;

            public object Context;

            public void Execute()
            {
                Action.Action(Context);
            }
        }

        private static void PerformanceTest2()
        {
            var action = new BindingAction<string>
            {
                Action = TestExecute
            };

            var ctx = "abc";
            var binding = new Binding1
            {
                Action = () => action.Action(ctx)
            };

            IBinding binding2 = new Binding2<string>
            {
                Action = action,
                Context = ctx
            };

            var action3 = new BindingAction3
            {
                Action = x => TestExecute((string)x)
            };

            var binding3 = new Binding3
            {
                Action = action3,
                Context = ctx
            };

            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 100000000; ++i)
            {
                //binding.Action();
                //binding2.Execute();
                binding3.Execute();
            }

            sw.Stop();

            // expected result: 400ms on average workstation
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TestExecute(string obj)
        {

        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TestExecute2(object obj)
        {
            var z = (string) obj;
        }

        private static void DebugTest()
        {
            Binder.SetTracingMethod(x => Console.WriteLine("[binder] " + x));
            var user = new UserModel();
            using (Binder.BeginTransaction())
            {
                user.Source = new SourceModel();
                user.Source.DefaultValue = 1;
            }
        }

        private static void PerformanceTest()
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

            // expected result: 400ms on average workstation
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
        }
    }
}
