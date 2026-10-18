using System;
using System.Linq;
using Mono.Cecil;
class Check {
 static int Main(string[] args){
  using(var asm=AssemblyDefinition.ReadAssembly(args[0])){
   var t=asm.MainModule.Types.Single(x=>x.FullName=="LighFight.Runtime.FighterView");
   var names=new[]{"FindNativeClip","FindContactPose","HasNativeMotion","NativeDuration","NativeHeading","NativeStrikeSide"};
   foreach(var name in names){var m=t.Methods.Single(x=>x.Name==name);var bad=m.Body.Instructions.Where(x=>x.OpCode.Code==Mono.Cecil.Cil.Code.Newobj||((x.Operand as MethodReference)?.DeclaringType.FullName=="System.Array"));foreach(var b in bad){Console.WriteLine("REJECT "+name+" "+b);return 1;}Console.WriteLine("PASS "+name+" compiled IL: no newobj or System.Array predicate call");}
  }
  Console.WriteLine("Scope: inspected lookup/control methods only; not whole-player GC or phone thermal proof.");return 0;
 }
}
