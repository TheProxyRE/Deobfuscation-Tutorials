# Tutorial 1 - Strings Decryption

## Intro
String encryption is common protection in obfuscators and it encrypts strings to some gibberish code so we cant dechiper what was original text.

![image](https://user-images.githubusercontent.com/12687236/27516110-f72fb222-59b2-11e7-88cd-b73a35cd994a.png)

Structure of string decryption routine is
  1.Decryption Method
  2.Parameters 

![image](https://user-images.githubusercontent.com/12687236/27516142-dacb002c-59b3-11e7-860c-ce5c217ced15.png)

## How to decrypt
There are 2 most common ways of creating string decryption:

1.Static decryption - Static decryption is when we copy decompiled method from obfsucated assembly and paste it in our code and use as it is.

2.Dynamic decryption - Dynamic decryption is when we invoke method from obfuscated assembly which is not 100% since it can also invoke some malware 

Ofcourse there could be some preventions for boath to work such as using name of method from where is called decryption method (with Stack Trace) or something like assigning some field outside of decryption method (mostly in Module.Cctor or EntryPoint) which is used in decryption method (All those can be fixed for static method but cannot for dynamic)

## Static Decryption  
In this example i will use files protected by Yano Obfsucator.
So as i sad in static decryption we try to decompile Decryption method and use it in our decrypter code.

### 1.Decompiling decryption method
First thing what we will do is to find decryption method and try to decompile it.
![image](https://user-images.githubusercontent.com/12687236/27516344-8302beb6-59b8-11e7-8792-9b3df12cc1ee.png)
As we see on this picture we secssesfuly decompiled our decryption method.

### 2.Checking is decompiled method compilabe, optimizing it and testing it
What we have to do now is to open our IDE and to paste decompiled method in it and if errors are present we will try to fix them
![image](https://user-images.githubusercontent.com/12687236/27516522-fcc0975c-59bb-11e7-9008-96a814617acd.png)

And then we fix method and optimize it like this:
![image](https://user-images.githubusercontent.com/12687236/27516545-705e3002-59bc-11e7-8d25-09f9660d8459.png)

And after that we can test if decryption method working
![image](https://user-images.githubusercontent.com/12687236/27516562-a3aaeedc-59bc-11e7-8fa5-24c9814a2a0e.png)

### 3.Checking for decryption keys
Next thing to do is to search for Keys (constants like integer/strings etc..) that are unique for each obfscuated file.
Best way to detect this is to compare decryption methods of 2 or more protected files
![image](https://user-images.githubusercontent.com/12687236/27516420-96d462ea-59b9-11e7-88a1-0be57857b637.png)
> int num2 = key + num;

And when we compare first decryption method and this one we can see that 1 integer constant is changed and we can assume that will be different each time, so that means we have find  this key when we are detecting if decryption method is applied

Also we shouldnt forget to add key parameter in our decryption method
![image](https://user-images.githubusercontent.com/12687236/27517266-edfc4e9c-59c9-11e7-96b4-ceb006d20e56.png)
```
 internal static string Decrypt(string text, int num,int key)
        {
            int num2 = key + num;
            char[] array = text.ToCharArray();
            for (int i = 0; i < array.Length; i++)
                array[i] = (char)(((array[i] & 'ÿ') ^ num2++) << 8 | ((byte)((array[i] >> 8) ^ num2++)));
            return string.Intern(new string(array));
        }
```

### 4.Static dcryption method detecting and key's parsing
Ok now we are starting to code our string decryption and first thing we have to do is to create function that will find decryption method in obfsuacted assembly
First what we will do is to create method that will loop through all methods in assembly
 ```
            MethodDef decryptionMethod = null;                   //Creating locals for our decrpytion method and key for furter assigning 
            int? decryptionKey = null;

            foreach (var type in module.GetTypes())               //Looping through all types (classes)
                foreach (var method in type.Methods)              //Looping through all methods in that type
                {
                    if (!method.HasBody)                          //Checking if method have body (instructions)
                        continue;
                    //Now we will check if method is matching needed signature ( signature is written like ReturnType(ParameterType,SecondParameterType...)  )
                    if (method.Signature.ToString() != "System.String (System.String,System.Int32)")  //that signature is for current case when it return string and have 2 parameters (string,int)  
                        continue;                                         //Best way to get valid signature is with debugging
                    var instructions = method.Body.Instructions;  //Parsing method instructions as Array of instructions
                   
                     //Here we are checking if instructions corresponds to decrpytion instructions
                     //if we find method and key we should assign them on decryptionMethod and decryptionKey
                    
                }

            if (decryptionMethod == null || decryptionKey == null)   if decryptionMethod or key arent assigned that means detecting failed
                return;
 ```
![image](https://user-images.githubusercontent.com/12687236/27517058-698fb01c-59c5-11e7-8f6a-b080f19fd5fc.png)

Than next thing to do is to check if method il body. Best way is to check first 10-15 instructions if are matching original like this
![image](https://user-images.githubusercontent.com/12687236/27517105-aa6c0742-59c6-11e7-8141-23d40faad28f.png)
 ```
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
```
Also we shouldnt forget that we have to save key so we should determine where it is located
![image](https://user-images.githubusercontent.com/12687236/27517142-7d520486-59c7-11e7-9465-b7cbbb106e12.png)

After we get location we should assign our decryptionMethod and decryptionKey fields
```
                    decryptionKey = instructions[0].GetLdcI4Value();
                    decryptionMethod = method;
                    break;
```

### 5.Decrypting strings
So finaly we came to the end of journey where there there is left only to replace encrypted methods with decrypted strings.
First thing we should do is to see how decryption method is called
![image](https://user-images.githubusercontent.com/12687236/27517215-579e420c-59c9-11e7-9c2e-ab2603d7a346.png)

So we know that there are 3 Instructions for each call that we should change (1 call and 2 parameters) so what is left to do is to loop through each instruction in evry method and try to find those 3 instructions and to replace them with decrypted string 
```
int decrypted = 0;
            foreach (var type in module.GetTypes())
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody)
                        continue;
                    var instructions = method.Body.Instructions;
                    for (int i = 2; i < instructions.Count; i++)  //We are starting from i=2 because call needs 2 parameters so it cant start from 0 or 1
                    {
                        if (instructions[i].OpCode != OpCodes.Call) //checking if instruction is call
                            continue;
                        var calledMethod = instructions[i].Operand as MethodDef;
                        if (calledMethod == null || calledMethod != decryptionMethod)  //checking if decryptionMethod is called
                            continue;
                        if (!instructions[i - 1].IsLdcI4() || instructions[i - 2].OpCode != OpCodes.Ldstr) //checking if parameters are valid
                            continue;
                        var strParameter = instructions[i - 2].Operand.ToString();  //parsing value of first parameter
                        var intParameter = instructions[i - 1].GetLdcI4Value();     //parsing value of second parameter  
                        var decryptedString = Decrypt(strParameter, intParameter,decryptionKey.Value);

                        Console.WriteLine(decryptedString);  //Logging decryptedString

                        instructions[i].OpCode = OpCodes.Ldstr;   
                        instructions[i].Operand = decryptedString;   //replacing call with ldstr (load string) and assigning to return decrypted string
                        instructions[i - 1].OpCode = OpCodes.Nop;
                        instructions[i - 2].OpCode = OpCodes.Nop;  //nopping parameters

                        decrypted++; //Logging how much calls got decrypted
                    }
                }
            Console.WriteLine("Decrypted {0} strings", decrypted);
```

### 6.Removing junk and saving assembly
Now we can remove decryption method and also because its only method in Class (Type) we can remove whole Class 
![image](https://user-images.githubusercontent.com/12687236/27517390-0d7ca36e-59cc-11e7-8593-c9d098a38e6c.png)
```
 module.Types.Remove(decryptionMethod.DeclaringType);
```
Now we want to create new path save path and for that we can use simple method i created
```
 static string FormatPath(string path, string sufix)
        {
            var extension = Path.GetExtension(path);
            return path.Substring(0,path.Length - extension.Length) + sufix + extension;
        }
```
And finaly we save deobfsucated assembly
```
 var newPath = FormatPath(modulePath, "_strDecrypted");

            var moduleWriterOptions = new ModuleWriterOptions(module);  //this is just to prevent write methods from throwing error
            moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            module.Write(newPath, moduleWriterOptions); //saving assemby                  

            Console.WriteLine("Saved {0}", newPath);  //logging where its saved
```

![image](https://user-images.githubusercontent.com/12687236/27517582-4bb1ec86-59cf-11e7-90b2-333e98a51a31.png)
