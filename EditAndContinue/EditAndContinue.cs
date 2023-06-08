using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace EditAndContinue;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class EditAndContinue : BaseUnityPlugin
{
    private static List<Hook> Hooks = new();

    private static ConfigEntry<string> ReloadKey;

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
                foreach (string path in Directory.GetFiles(scriptDirectory, "*.dll", SearchOption.AllDirectories))
                {
                    var allFlags = (BindingFlags)(-1);

                    var resolver = new DefaultAssemblyResolver();
                    resolver.AddSearchDirectory(scriptDirectory);
                    resolver.AddSearchDirectory(Paths.ManagedPath);
                    resolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);

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
                            var count = Math.Min(allOldMethods.Length, allNewMethods.Length);
                            for (int i = 0; i < count; i++)
                            {
                                var oldMethod = allOldMethods[i];
                                for (int j = 0; j < count; j++)
                                {
                                    var newMethod = allNewMethods[j];
                                    try
                                    {
                                        if (oldMethod.DeclaringType.FullName == newMethod.DeclaringType.FullName && oldMethod.Name == newMethod.Name)
                                        {
                                            Hooks.Add(new Hook(oldMethod, newMethod));
                                            break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        //Log.Error(e);
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