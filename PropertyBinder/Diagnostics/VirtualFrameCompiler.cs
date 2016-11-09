using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using PropertyBinder.Engine;

namespace PropertyBinder.Diagnostics
{
    internal static class VirtualFrameCompiler
    {
        private static readonly AssemblyBuilder Assembly;
        private static readonly ModuleBuilder Module;
        private static long _globalIndex;

        static VirtualFrameCompiler()
        {
            var assemblyName = new AssemblyName("BINDING ");
            Assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            Module = Assembly.DefineDynamicModule("PropertyBinder.VirtualFrames.dll", true);
        }

        public static Action<Binding[], int> CreateMethodFrame(string description, StackFrame frame)
        {
            lock (Module)
            {
                const string methodName = " ";
                var type = Module.DefineType(string.Format(" {0}\0{1}", description, _globalIndex++), TypeAttributes.Class | TypeAttributes.Public);
                var method = type.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(Binding[]), typeof(int) });
                method.SetImplementationFlags(MethodImplAttributes.NoOptimization);

                var il = method.GetILGenerator();

                var binding = il.DeclareLocal(typeof(Binding));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldelem_I4);
                il.Emit(OpCodes.Stloc_S, binding);

                var fileName = frame?.GetFileName();
                if (!string.IsNullOrEmpty(fileName))
                {
                    var symbolDocument = Module.DefineDocument(fileName, SymDocumentType.Text, SymLanguageType.CSharp, SymLanguageVendor.Microsoft);
                    il.MarkSequencePoint(symbolDocument, frame.GetFileLineNumber(), frame.GetFileColumnNumber(), frame.GetFileLineNumber(), frame.GetFileColumnNumber() + 2);

                    method.DefineParameter(1, ParameterAttributes.None, "bindings");
                    method.DefineParameter(2, ParameterAttributes.None, "index");
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Binding[]).GetProperty("Length").GetGetMethod());
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);

                var lblFin = il.DefineLabel();
                il.Emit(OpCodes.Bge_S, lblFin);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldelem_I4);
                il.Emit(OpCodes.Ldfld, typeof(Binding).GetField("DebugContext"));
                il.Emit(OpCodes.Callvirt, typeof(DebugContext).GetProperty("VirtualFrame").GetGetMethod());
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Call, typeof(Action<Binding[], int>).GetMethod("Invoke"));
                il.Emit(OpCodes.Ret);
                il.MarkLabel(lblFin);
                il.Emit(OpCodes.Ldloc, binding);
                il.Emit(OpCodes.Callvirt, typeof(Binding).GetMethod("Execute"));
                il.Emit(OpCodes.Ret);

                if (!string.IsNullOrEmpty(fileName))
                {
                    binding.SetLocalSymInfo("binding", 0, il.ILOffset);
                }

                var actualType = type.CreateType();
                return (Action<Binding[], int>)actualType.GetMethod(methodName).CreateDelegate(typeof(Action<Binding[], int>));
            }
        }
    }
}
