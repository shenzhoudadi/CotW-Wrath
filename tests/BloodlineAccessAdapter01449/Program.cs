using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
using System.Text.Json;
var input=Path.GetFullPath(args[0]);var host=Path.GetFullPath(args[1]);var output=Path.GetFullPath(args[2]);
using var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(args[3]);resolver.AddSearchDirectory(Path.GetDirectoryName(host)!);resolver.AddSearchDirectory(Path.GetDirectoryName(input)!);
using var a=AssemblyDefinition.ReadAssembly(input,new ReaderParameters{AssemblyResolver=resolver});using var h=AssemblyDefinition.ReadAssembly(host);
var bridge=h.MainModule.Types.Single(t=>t.Name=="NativeFieldBridge01449");
var rows=new List<object>();var unadapted=new List<object>();
IEnumerable<TypeDefinition> All(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var x in All(n))yield return x;}
foreach(var t in a.MainModule.Types.SelectMany(All))foreach(var m in t.Methods.Where(m=>m.HasBody))foreach(var ins in m.Body.Instructions.ToArray()) {
 if(ins.Operand is MethodReference getter&&getter.DeclaringType.Scope.Name=="Assembly-CSharp"&&getter.Name=="get_HasRanks"&&getter.Parameters.Count==0&&getter.HasThis&&getter.ReturnType.FullName=="System.Boolean"&&(ins.OpCode==OpCodes.Call||ins.OpCode==OpCodes.Callvirt)){
  var getterCall=a.MainModule.ImportReference(bridge.Methods.Single(x=>x.Name=="HasRanks"));rows.Add(new{method=m.FullName,offset=ins.Offset,opcode=ins.OpCode.Name,getter=getter.FullName});ins.OpCode=OpCodes.Ldstr;ins.Operand=getter.DeclaringType.FullName;var getterIL=m.Body.GetILProcessor();getterIL.InsertAfter(ins,Instruction.Create(OpCodes.Call,getterCall));continue;
 }
 if(ins.Operand is not FieldReference f||f.DeclaringType.Scope.Name!="Assembly-CSharp")continue;
 var definition=f.Resolve();if(definition==null){unadapted.Add(new{method=m.FullName,operation=ins.ToString(),reason="field absent from current game; original unused external code retained"});continue;}if(definition.IsPublic)continue;
 if(f.DeclaringType.Resolve().IsValueType||f.DeclaringType.ContainsGenericParameter||f.DeclaringType is GenericInstanceType||f.FieldType.IsPointer||f.FieldType.IsByReference){unadapted.Add(new{method=m.FullName,operation=ins.ToString(),reason="address/value-type/generic-owner access remains original"});continue;}
 if(ins.OpCode!=OpCodes.Ldfld&&ins.OpCode!=OpCodes.Stfld){unadapted.Add(new{method=m.FullName,operation=ins.ToString()});continue;}
 bool read=ins.OpCode==OpCodes.Ldfld;var method=bridge.Methods.Single(x=>x.Name==(read?"Read":"Write"));
 var call=new GenericInstanceMethod(a.MainModule.ImportReference(method));call.GenericArguments.Add(a.MainModule.ImportReference(f.FieldType));
 rows.Add(new{method=m.FullName,offset=ins.Offset,opcode=ins.OpCode.Name,field=f.FullName});
 ins.OpCode=OpCodes.Ldstr;ins.Operand=f.DeclaringType.FullName;var il=m.Body.GetILProcessor();var name=Instruction.Create(OpCodes.Ldstr,f.Name);il.InsertAfter(ins,name);il.InsertAfter(name,Instruction.Create(OpCodes.Call,call));
}
if(rows.Count==0)throw new Exception("No audited private accesses found");
var opcodes=typeof(OpCodes).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>o.Name);
foreach(var m in a.MainModule.Types.SelectMany(All).SelectMany(t=>t.Methods).Where(m=>m.HasBody))foreach(var ins in m.Body.Instructions)if(ins.OpCode.OperandType==OperandType.ShortInlineBrTarget)ins.OpCode=opcodes[ins.OpCode.Name[..^2]];
Directory.CreateDirectory(Path.GetDirectoryName(output)!);a.Write(output);
string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
File.WriteAllText(output+".adapter.json",JsonSerializer.Serialize(new{purpose="Standalone CLR field-access adaptation for the publicized-reference external mod. Same declaring type/field/type and read/write, no rule/branch replacement. Never installed into the game or delivered.",input,inputSHA256=Hash(input),host,hostSHA256=Hash(host),nativeAssemblySHA256=Hash(Path.Combine(args[3],"Assembly-CSharp.dll")),output,outputSHA256=Hash(output),changes=rows,unadapted},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Adapted field operations "+rows.Count+"; unadapted "+unadapted.Count);
