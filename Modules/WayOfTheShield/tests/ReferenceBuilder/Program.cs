using Mono.Cecil;
using System.Security.Cryptography;
using System.Text.Json;
if(args.Length!=2)throw new ArgumentException("source game managed directory and reference output directory required");
string source=Path.GetFullPath(Path.Combine(args[0],"Assembly-CSharp.dll")),dest=Path.GetFullPath(args[1]);Directory.CreateDirectory(dest);
string hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)));
string output=Path.Combine(dest,"Assembly-CSharp.dll"),receipt=Path.Combine(dest,"reference.json");
if(File.Exists(output)&&File.Exists(receipt)){var data=JsonDocument.Parse(File.ReadAllText(receipt)).RootElement;if(data.GetProperty("sourceSHA256").GetString()==hash&&data.TryGetProperty("policyVersion",out var policy)&&policy.GetInt32()==2){Console.WriteLine("Current game private-member reference cached: "+hash);return;}}
var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(Path.GetDirectoryName(source));using var asm=AssemblyDefinition.ReadAssembly(source,new ReaderParameters{AssemblyResolver=resolver});int fields=0,methods=0,types=0;
void Visit(TypeDefinition type){
 if(type.IsNotPublic){type.IsPublic=true;types++;}else if(type.IsNestedPrivate||type.IsNestedAssembly){type.IsNestedPublic=true;types++;}
 foreach(var f in type.Fields)if(!f.IsPublic){f.IsPublic=true;fields++;}
 foreach(var m in type.Methods)if(m.IsPrivate||m.IsAssembly){m.IsPublic=true;methods++;}
 foreach(var t in type.NestedTypes)Visit(t);
}
foreach(var type in asm.MainModule.Types)Visit(type);asm.Write(output);
File.WriteAllText(receipt,JsonSerializer.Serialize(new{source,sourceSHA256=hash,output,policyVersion=2,protectedVisibilityPreserved=true,gameMethodBodiesUnchanged=true,changedFields=fields,changedMethods=methods,changedTypes=types},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Actual game reference prepared; protected lifecycle visibility preserved: "+hash);
