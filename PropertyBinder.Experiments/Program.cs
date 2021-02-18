using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PropertyBinder.Diagnostics;

namespace PropertyBinder.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            //DebugTest();
            //DebugStackFrameTest();
            PerformanceTest();
            //DictionaryPerfTest();
            //CreationPerfTest();
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
            var tracer = new StopwatchTracer();
            Binder.SetTracer(tracer);

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
            Console.WriteLine($"Total: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Bindings: {tracer.Elapsed.TotalMilliseconds}ms");
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
            var user = new UserModel();
            using (Binder.BeginTransaction())
            {
                user.Source = new SourceModel();
                user.Source.DefaultValue = 1;
            }
        }

        private static void DebugStackFrameTest()
        {
            var binder = new Binder<SourceModel>();
            binder.Bind(x => x.Value1).DoNotRunOnAttach().To(x =>
            {
                VirtualFrameCompiler.TakeSnapshot();
            });

            var model = new SourceModel();
            using (binder.Attach(model))
            {
                model.Value1 = 1;
            }
        }

        private static void PerformanceTest()
        {
            Binder.DebugMode = false;
            var tracer = new StopwatchTracer();
            //Binder.SetTracer(tracer);

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
            Console.WriteLine($"Total: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Bindings: {tracer.Elapsed.TotalMilliseconds}ms");

            Console.ReadLine();
        }

        private static void DictionaryPerfTest()
        {
            var sizes = new[] { 1, 4, 8 };
            var comparer = StringComparer.InvariantCultureIgnoreCase;
            foreach (var sz in sizes)
            {
                Console.WriteLine("Dictionary/{0}", sz);
                TestTryGetValueCollection<Dictionary<string, object>>(sz, 100000, comparer);
            }

            foreach (var sz in sizes)
            {
                Console.WriteLine("SortedDictionary/{0}", sz);
                TestTryGetValueCollection<SortedDictionary<string, object>>(sz, 100000, comparer);
            }

            foreach (var sz in sizes)
            {
                Console.WriteLine("SortedList/{0}", sz);
                TestTryGetValueCollection<SortedList<string, object>>(sz, 100000, comparer);
            }

            foreach (var sz in sizes)
            {
                Console.WriteLine("ListDictionary/{0}", sz);
                TestTryGetValueCollection<ListDictionary<string, object>>(sz, 100000, comparer);
            }

            Console.ReadLine();
        }

        private static void CreationPerfTest()
        {
            Binder.DebugMode = false;
            Binder.AllowReuseOfWatchers = true;

            var depth = 5;
            var model = new ExplosiveModel(depth);

            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 100; ++i)
            {
                new ExplosiveModel(depth).Dispose();
            }

            sw.Stop();

            Console.WriteLine($"Creation: {sw.ElapsedMilliseconds}ms");

            sw.Reset();
            sw.Start();

            for (int i = 0; i < 1000; ++i)
            {
                model.ModifyAll();
            }

            sw.Stop();
            Console.WriteLine($"Modification: {sw.ElapsedMilliseconds}ms");
        }

        private static void TestTryGetValueCollection<T>(int size, int rounds, IEqualityComparer<string> comparer)
            where T : IDictionary<string, object>, new()
        {
            var keys = Enumerable.Range(0, size).Select(x => string.Format("LongKey_{0}_{1}", x % 2, x)).ToArray();

            T collection = new T();
            var sw = new Stopwatch();
            sw.Start();

            for (int r = 0; r < rounds; ++r)
            {
                collection = new T();
                for (int i = 0; i < size; ++i)
                {
                    collection.Add(keys[i], keys[i]);
                }
            }

            sw.Stop();
            Console.WriteLine("Populate: " + sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            for (int r = 0; r < rounds; ++r)
            {
                object value;
                for (int i = 0; i < size; ++i)
                {
                    collection.TryGetValue(keys[i], out value);
                }
            }


            sw.Stop();
            Console.WriteLine("Retrieve: " + sw.ElapsedMilliseconds);
        }

    }
}
