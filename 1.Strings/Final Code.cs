using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
            return;
        string modulePath = args[0];
        ModuleDef module;
        try
        {
            module = ModuleDefMD.Load(modulePath);
        }
        catch { return; }

        MethodDef decryptionMethod = null;
        int? decryptionKey = null;

        foreach (var type in module.GetTypes())
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;
                if (method.Signature.ToString() != "System.String (System.String,System.Int32)")
                    continue;
                var instructions = method.Body.Instructions;
                if (instructions.Count < 15)
                    continue;
                if (!instructions[0].IsLdcI4())
                    continue;
                if (!instructions[1].IsLdarg())
                    continue;
                if (instructions[2].OpCode != OpCodes.Add)
                    continue;
                if (!instructions[3].IsStloc())
                    continue;
                if (!instructions[4].IsLdarg())
                    continue;
                if (instructions[5].OpCode != OpCodes.Call)
                    continue;
                if (instructions[5].Operand.ToString() != "System.Char[] System.String::ToCharArray()")
                    continue;
                if (!instructions[6].IsStloc())
                    continue;
                if (!instructions[7].IsLdcI4() || instructions[7].GetLdcI4Value() != 0)
                    continue;
                if (!instructions[8].IsStloc())
                    continue;
                if (!instructions[9].IsLdloc())
                    continue;
                if (!instructions[10].IsLdloc())
                    continue;
                if (instructions[11].OpCode != OpCodes.Ldlen)
                    continue;
                if (instructions[12].OpCode != OpCodes.Conv_I4)
                    continue;
                if (instructions[13].OpCode != OpCodes.Clt)
                    continue;
                decryptionKey = instructions[0].GetLdcI4Value();
                decryptionMethod = method;
                break;
            }

        if (decryptionMethod == null || decryptionKey == null)
            return;

        int decrypted = 0;
        foreach (var type in module.GetTypes())
            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;
                var instructions = method.Body.Instructions;
                for (int i = 2; i < instructions.Count; i++)
                {
                    if (instructions[i].OpCode != OpCodes.Call)
                        continue;
                    var calledMethod = instructions[i].Operand as MethodDef;
                    if (calledMethod == null || calledMethod != decryptionMethod)
                        continue;
                    if (!instructions[i - 1].IsLdcI4() || instructions[i - 2].OpCode != OpCodes.Ldstr)
                        continue;
                    var strParameter = instructions[i - 2].Operand.ToString();
                    var intParameter = instructions[i - 1].GetLdcI4Value();
                    var decryptedString = Decrypt(strParameter, intParameter, decryptionKey.Value);

                    Console.WriteLine(decryptedString);

                    instructions[i].OpCode = OpCodes.Ldstr;
                    instructions[i].Operand = decryptedString;
                    instructions[i - 1].OpCode = OpCodes.Nop;
                    instructions[i - 2].OpCode = OpCodes.Nop;

                    decrypted++;
                }
            }
        Console.WriteLine("Decrypted {0} strings", decrypted);

        module.Types.Remove(decryptionMethod.DeclaringType);

        var newPath = FormatPath(modulePath, "_strDecrypted");

        var moduleWriterOptions = new ModuleWriterOptions(module);
        moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
        module.Write(newPath, moduleWriterOptions);

        Console.WriteLine("Saved {0}", newPath);
        Console.ReadKey();
    }
    static string FormatPath(string path, string sufix)
    {
        var extension = Path.GetExtension(path);
        return path.Substring(0, path.Length - extension.Length) + sufix + extension;
    }
    internal static string Decrypt(string text, int num, int key)
    {
        int num2 = key + num;
        char[] array = text.ToCharArray();
        for (int i = 0; i < array.Length; i++)
            array[i] = (char)(((array[i] & 'ÿ') ^ num2++) << 8 | ((byte)((array[i] >> 8) ^ num2++)));
        return string.Intern(new string(array));
    }
}

