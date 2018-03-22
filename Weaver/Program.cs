using Engine.Wpf;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Weaver
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(DateTime.Now + " start code weaving");

                if (args == null) return;
                if (args.Length == 0) return;
                if (string.IsNullOrEmpty(args[0])) return;

                Console.WriteLine("target: " + args[0]);

                var assemblyPath = args[0];
                var readerParameters = new ReaderParameters() { ReadSymbols = true };
                var assemblyReference = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);

                assemblyReference.MainModule.Import(typeof(NotifyPropertyChangedAttribute));
                var propertyChangedEventArgs = assemblyReference.MainModule.Import(typeof(PropertyChangedEventArgs));
                var propertyChangedEventArgsCtor = assemblyReference.MainModule.Import(propertyChangedEventArgs.Resolve().Methods.First(m => m.Name == ".ctor"));
                var propertyChangedEventHandler = assemblyReference.MainModule.Import(typeof(PropertyChangedEventHandler));
                var propertyChangedEventHandlerInvoke = assemblyReference.MainModule.Import(propertyChangedEventHandler.Resolve().Methods.First(m => m.Name == "Invoke"));

                var assemblyTypes = assemblyReference
                    .Modules
                    .SelectMany(m => m.GetTypes());

                var properties = assemblyTypes
                    .SelectMany(t => t.Properties)
                    .Where(p => p.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(NotifyPropertyChangedAttribute).FullName));

                foreach(var property in properties)
                {
                    Console.WriteLine("Injecting in " + property.FullName);

                    var backingField = property.DeclaringType.Fields.First(f => f.Name == "<" + property.Name + ">k__BackingField");
                    var propChangedEventHandlerField = property.DeclaringType.Fields.First(f => f.Name == "PropertyChanged");
                    var setter = property.SetMethod;

                    MethodReference getDefault;
                    MethodReference equals;

                    var comparer = (TypeDefinition)property.CustomAttributes
                        .First(a => a.AttributeType.FullName == typeof(NotifyPropertyChangedAttribute).FullName)
                        .ConstructorArguments[0].Value;
                
                    if (comparer == null)
                    {
                        var type = assemblyReference.MainModule.Import(typeof(EqualityComparer<>)).Resolve();
                        var typeReference = (TypeReference)type;
                        typeReference = typeReference.MakeGenericInstanceType(property.PropertyType);
                        getDefault = assemblyReference.MainModule.Import(type.Properties.First(m => m.Name == "Default").GetMethod.MakeHostInstanceGeneric(property.PropertyType));
                        equals = assemblyReference.MainModule.Import(type.Methods.First(m => m.Name == "Equals").MakeHostInstanceGeneric(property.PropertyType));
                    }
                    else
                    {
                        getDefault = comparer.Methods.First(m => m.Name == "get_Default");
                        equals = comparer.Methods.First(m => m.Name == "Equals");
                    }

                    setter.Body.Instructions.Clear();
                    setter.Body.Variables.Add(new VariableDefinition(propertyChangedEventHandler));
                    setter.Body.Variables.Add(new VariableDefinition(propertyChangedEventHandler));
                    setter.Body.Variables.Add(new VariableDefinition(propertyChangedEventHandler));

                    var ilGenerator = setter.Body.GetILProcessor();
                    var start = ilGenerator.Create(OpCodes.Nop);
                    var ret = ilGenerator.Create(OpCodes.Ret);
                    var ldarg_0_18 = ilGenerator.Create(OpCodes.Ldarg_0);
                    var ldarg_0_2b = ilGenerator.Create(OpCodes.Ldarg_0);
                
                    setter.Body.Instructions.Add(start);
                    ilGenerator.InsertAfter(start, ret);
                
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Call, getDefault));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldarg_0));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldfld, backingField));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldarg_1));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Callvirt, equals));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Stloc_0));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldloc_0));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Brfalse_S, ldarg_0_18));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Br_S, ret));
                    ilGenerator.InsertBefore(ret, ldarg_0_18);
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldarg_1));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Stfld, backingField));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldarg_0));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldfld, propChangedEventHandlerField));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Dup));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Brtrue_S, ldarg_0_2b));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Pop));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Br_S, ret));
                    ilGenerator.InsertBefore(ret, ldarg_0_2b);
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Ldstr, property.Name));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Newobj, propertyChangedEventArgsCtor));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Callvirt, propertyChangedEventHandlerInvoke));
                    ilGenerator.InsertBefore(ret, ilGenerator.Create(OpCodes.Nop));

                    var attributesToRemove = new List<CustomAttribute>();

                    foreach (var attribute in property.CustomAttributes.Where(c => c.AttributeType.FullName == typeof(NotifyPropertyChangedAttribute).FullName))
                        attributesToRemove.Add(attribute);

                    foreach (var attribute in attributesToRemove)
                        property.CustomAttributes.Remove(attribute);
                }

                var writerParameters = new WriterParameters() { WriteSymbols = true };

                Console.WriteLine(DateTime.Now + " writing to assembly");
                assemblyReference.Write(assemblyPath, writerParameters);

                Console.WriteLine(DateTime.Now + " code weaving finished");
            }
            catch(Exception ex)
            {
                Console.WriteLine(DateTime.Now + " code weaving failed");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
