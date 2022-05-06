// See https://aka.ms/new-console-template for more information

namespace LLVMSharp.Samples;

using System.Runtime.InteropServices;
using LLVMSharp.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void Print(string d);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate string? Read();

public static class Program
{
    private static void Print(string s)
    {
        Console.WriteLine(s);
    }

    private static string? Read()
    {
        return Console.ReadLine();
    }

    public static void Main()
    {
        LLVM.LinkInMCJIT();
        LLVM.InitializeX86TargetMC();
        LLVM.InitializeX86Target();
        LLVM.InitializeX86TargetInfo();
        LLVM.InitializeX86AsmParser();
        LLVM.InitializeX86AsmPrinter();

        var module = LLVMModuleRef.CreateWithName("Sample module");
        var builder = module.Context.CreateBuilder();
        var engine = module.CreateMCJITCompiler();

        // adding function "print" to module
        var printFunctionType =
            LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, new[] {LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)});
        var printFunction = module.AddFunction("print", printFunctionType);
        printFunction.Linkage = LLVMLinkage.LLVMExternalLinkage;
        Delegate printDelegate = new Print(Print);
        var pointerToCSharpPrintFunction = Marshal.GetFunctionPointerForDelegate(printDelegate);
        engine.AddGlobalMapping(printFunction, pointerToCSharpPrintFunction);

        // adding function "read" to module
        var readFunctionType =
            LLVMTypeRef.CreateFunction(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), new[] {LLVMTypeRef.Void});
        var readFunction = module.AddFunction("read", readFunctionType);
        readFunction.Linkage = LLVMLinkage.LLVMExternalLinkage;
        Delegate readDelegate = new Read(Read);
        var pointerToCSharpReadFunction = Marshal.GetFunctionPointerForDelegate(readDelegate);
        engine.AddGlobalMapping(readFunction, pointerToCSharpReadFunction);


        // adding function "main" to module
        var mainFunctionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, Array.Empty<LLVMTypeRef>());
        var mainFunction = module.AddFunction("main", mainFunctionType);
        mainFunction.Linkage = LLVMLinkage.LLVMExternalLinkage;
        var bb = mainFunction.AppendBasicBlock("entry");
        builder.PositionAtEnd(bb);
        builder.BuildCall(printFunction, new[] {builder.BuildGlobalStringPtr("Please enter a value:")});
        var enteredValue = builder.BuildCall(readFunction, Array.Empty<LLVMValueRef>());
        builder.BuildCall(printFunction, new[] {builder.BuildGlobalStringPtr("You have entered:")});
        builder.BuildCall(printFunction, new[] {enteredValue});
        builder.BuildRetVoid();

        var main = module.GetNamedFunction("main");
        engine.RunFunction(main, Array.Empty<LLVMGenericValueRef>());
    }
}