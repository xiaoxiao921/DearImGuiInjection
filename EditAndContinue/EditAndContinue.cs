using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace EditAndContinue;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class EditAndContinue : BaseUnityPlugin
{
    const BindingFlags allFlags = (BindingFlags)(-1);

    private static List<Hook> Hooks = new();

    private static ConfigEntry<string> ReloadKey;

    // needed because otherwise we can't copy over old assembly data state to the new one
    private static MethodInfo GetValueInternal = typeof(FieldInfo).Assembly.GetType("System.Reflection.MonoField").GetMethod("GetValueInternal", allFlags);
    private static MethodInfo SetValueInternal = typeof(FieldInfo).Assembly.GetType("System.Reflection.MonoField").GetMethod("SetValueInternal", allFlags);

    private void Awake()
    {
        ReloadKey = Config.Bind("General", "ReloadKey", "f2", "Press this key to reload all the plugins from the scripts folder");
    }

    private void Update()
    {
        StaticUpdate();
    }

    private static void StaticUpdate()
    {
        if (Input.GetKey(ReloadKey.Value))
        {
            string scriptDirectory = Path.Combine(Paths.BepInExRootPath, "scripts");
            var files = Directory.GetFiles(scriptDirectory, "*.dll", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(scriptDirectory);
                resolver.AddSearchDirectory(Paths.ManagedPath);
                resolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);

                foreach (string path in Directory.GetFiles(scriptDirectory, "*.dll", SearchOption.AllDirectories))
                {

                    using (var dll = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = resolver }))
                    {
                        var originalDllName = dll.Name.Name;

                        Assembly oldAssembly = null;
                        int newIndex = 0;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            for (int i = 0; i < 1000; i++)
                            {
                                var indexedOriginalDllName = originalDllName + "-" + i.ToString();
                                if (assembly.GetName().Name == originalDllName ||
                                    assembly.GetName().Name == indexedOriginalDllName)
                                {
                                    oldAssembly = assembly;
                                    newIndex = i + 1;
                                    break;
                                }
                            }
                        }
                        if (oldAssembly == null)
                        {
                            Log.Error("oldAssembly == null");
                            return;
                        }
                        else
                        {
                            Log.Info("found old assembly " + oldAssembly.FullName);
                        }

                        var oldTypes = oldAssembly.GetTypes();

                        dll.Name.Name = $"{dll.Name.Name}-{newIndex}";

                        using (var ms = new MemoryStream())
                        {
                            dll.Write(ms);
                            var ass = Assembly.Load(ms.ToArray());

                            Log.Info("Assembly.Load " + ass.FullName);

                            var newTypes = ass.GetTypes();

                            var allOldMethods = oldTypes.SelectMany(t => t.GetMethods(allFlags)).ToArray();
                            var allNewMethods = newTypes.SelectMany(t => t.GetMethods(allFlags)).ToArray();
                            var methodCount = Math.Min(allOldMethods.Length, allNewMethods.Length);

                            HashSet<string> restoredTypesStates = new();

                            for (int i = 0; i < methodCount; i++)
                            {
                                var oldMethod = allOldMethods[i];
                                for (int j = 0; j < methodCount; j++)
                                {
                                    var newMethod = allNewMethods[j];
                                    try
                                    {
                                        if (oldMethod.DeclaringType.FullName == newMethod.DeclaringType.FullName && oldMethod.Name == newMethod.Name)
                                        {
                                            if (!restoredTypesStates.Contains(oldMethod.DeclaringType.FullName))
                                            {
                                                restoredTypesStates.Add(oldMethod.DeclaringType.FullName);

                                                try
                                                {
                                                    foreach (var oldTypeField in oldMethod.DeclaringType.GetFields(allFlags))
                                                    {
                                                        foreach (var newTypeField in newMethod.DeclaringType.GetFields(allFlags))
                                                        {
                                                            if (oldTypeField.IsStatic && oldTypeField.Name == newTypeField.Name)
                                                            {
                                                                object staticField = null;
                                                                var oldTypeFieldValue = GetValueInternal.Invoke(oldTypeField, new object[] { staticField });
                                                                SetValueInternal.Invoke(staticField, new object[] { newTypeField, staticField, oldTypeFieldValue });
                                                                // equivalent to
                                                                //newTypeField.SetValue(null, oldTypeField.GetValue(null));
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.Warning(e);
                                                }
                                            }

                                            static void FixUpRefs(ILContext il)
                                            {
                                                var c = new ILCursor(il);

                                                foreach (var instruction in c.Instrs)
                                                {
                                                    if (instruction.Operand == null)
                                                    {
                                                        continue;
                                                    }

                                                    if (instruction.Operand.GetType() == typeof(GenericInstanceMethod))
                                                    {
                                                        var methodRef = (MethodReference)instruction.Operand;

                                                        for (int i = 0; i < methodRef.GenericParameters.Count; i++)
                                                        {
                                                            var oldGenericParam = methodRef.GenericParameters[i];
                                                            //oldGenericParam.decl
                                                            //var newGenericParam = new GenericParameter()
                                                        }

                                                        //var m = typeof(GameObject).GetMethods(allFlags).First(m => m.IsGenericMethod && m.Name == nameof(GetComponent)).MakeGenericMethod(typeof(TestComp));
                                                        //var gm = il.Import(m);
                                                        //gm.GenericParameters.Add(new GenericParameter("T", gm));

                                                        instruction.Operand = methodRef;
                                                    }
                                                }
                                            }

                                            // todo: fix up type refs so that things like GetComponent<TypeFromOldAssembly>()
                                            // still work even after hot reloading
                                            //new ILHook(newMethod, FixUpRefs);

                                            Hooks.Add(new Hook(oldMethod, newMethod));
                                            break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Error(e);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}