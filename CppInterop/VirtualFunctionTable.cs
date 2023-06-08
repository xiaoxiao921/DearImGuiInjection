using System;
using System.Collections.Generic;

namespace CppInterop;

/// <summary>
/// A structure type which an individual virtual function table entry.
/// </summary>
public struct TableEntry
{
    /// <summary>
    /// The address in process memory where the VTable entry has been found.
    /// </summary>
    public IntPtr EntryAddress;

    /// <summary>
    /// The value of the individual entry in process memory for the VTable entry pointing to a function.
    /// </summary>
    public IntPtr FunctionPointer;
}

/// <summary>
/// Allows for easy storage of data about a virtual function table.
/// </summary>
public class VirtualFunctionTable
{
    /// <inheritdoc />
    public List<TableEntry> TableEntries { get; set; }

    /// <inheritdoc />
    public TableEntry this[int i]
    {
        get => TableEntries[i];
        set => TableEntries[i] = value;
    }

    private VirtualFunctionTable() { }

    /// <summary>
    /// Initiates a virtual function table from an object address in memory.
    /// An assumption is made that the virtual function table pointer is the first parameter.
    /// </summary>
    /// <param name="objectAddress">
    ///     The memory address at which the object is stored.
    ///     The function will assume that the first entry is a pointer to the virtual function
    ///     table, as standard with C++ code.
    /// </param>
    /// <param name="numberOfMethods">
    ///     The number of methods contained in the virtual function table.
    ///     For enumerables, you may obtain this value as such: Enum.GetNames(typeof(MyEnum)).Length; where
    ///     MyEnum is the name of your enumerable.
    /// </param>
    public static VirtualFunctionTable FromObject(IntPtr objectAddress, int numberOfMethods)
    {
        var table = new VirtualFunctionTable
        {
            TableEntries = GetObjectVTableAddresses(objectAddress, numberOfMethods)
        };

        return table;
    }

    /// <summary>
    /// Initiates a virtual function table given the address of the first function in memory.
    /// </summary>
    /// <param name="tableAddress">
    ///     The memory address of the first entry (function pointer) of the virtual function table.
    /// </param>
    /// <param name="numberOfMethods">
    ///     The number of methods contained in the virtual function table.
    ///     For enumerables, you may obtain this value as such: Enum.GetNames(typeof(MyEnum)).Length; where
    ///     MyEnum is the name of your enumerable.
    /// </param>
    public static VirtualFunctionTable FromAddress(IntPtr tableAddress, int numberOfMethods)
    {
        var table = new VirtualFunctionTable
        {
            TableEntries = GetAddresses(tableAddress, numberOfMethods)
        };

        return table;
    }

    private static unsafe List<TableEntry> GetObjectVTableAddresses(IntPtr objectAddress, int numberOfMethods)
    {
        var virtualFunctionTableAddress = *(ulong*)objectAddress;

        return GetAddresses((IntPtr)virtualFunctionTableAddress, numberOfMethods);
    }

    private static unsafe List<TableEntry> GetAddresses(IntPtr tablePointer, int numberOfMethods)
    {
        // Stores the addresses of the virtual function table.
        var tablePointers = new List<TableEntry>();

        // Append the table pointers onto the tablePointers list.
        // Using the size of the IntPtr allows for both x64 and x86 support.
        for (var i = 0; i < numberOfMethods; i++)
        {
            IntPtr targetAddress = tablePointer + IntPtr.Size * i;

            var functionPtr = *(ulong*)targetAddress;
            tablePointers.Add(new TableEntry
            {
                EntryAddress = targetAddress,
                FunctionPointer = (IntPtr)functionPtr
            });
        }

        return tablePointers;
    }
}