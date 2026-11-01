// Same-field bridge for the publicized-reference provider's DiceFormula
// initializer. Standalone CLR enforces private access; the game uses Mono.
using System;
using System.Reflection;
public static class NativeValueFieldBridge01454
{
    public static void Write<TStruct,TField>(ref TStruct instance,TField value,string owner,string name)where TStruct:struct
    {
        if(typeof(TStruct)!=typeof(Kingmaker.RuleSystem.DiceFormula)||owner!=typeof(TStruct).FullName||(name!="m_Dice"&&name!="m_Rolls"))throw new InvalidOperationException("Unaudited provider value-field access");
        var field=typeof(TStruct).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly);
        if(field==null||field.IsStatic||field.IsPublic||field.FieldType!=typeof(TField))throw new MissingFieldException(owner,name);
        object boxed=instance;field.SetValue(boxed,value);instance=(TStruct)boxed;
    }
}
public static class NativeEnumFieldBridge01454
{
    static FieldInfo Field(object instance,string owner,string name)
    {
        if(owner!="Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff"||name!="m_Flags")throw new InvalidOperationException("Unaudited provider enum access");
        var t=typeof(Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff);var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
        if(!t.IsInstanceOfType(instance)||f==null||f.IsStatic||f.IsPublic||!f.FieldType.IsEnum||Enum.GetUnderlyingType(f.FieldType)!=typeof(int))throw new MissingFieldException(owner,name);return f;
    }
    public static int Read(object instance,string owner,string name)=>Convert.ToInt32(Field(instance,owner,name).GetValue(instance));
    public static void Write(object instance,int value,string owner,string name){var f=Field(instance,owner,name);f.SetValue(instance,Enum.ToObject(f.FieldType,value));}
}
public static class NativeProviderAccessBridge01454
{
    static readonly MethodInfo Getter=typeof(Kingmaker.Blueprints.BlueprintReferenceBase).GetProperty("Cached",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly)?.GetGetMethod(true)??throw new MissingMemberException("BlueprintReferenceBase.Cached");
    public static Kingmaker.Blueprints.SimpleBlueprint Cached(Kingmaker.Blueprints.BlueprintReferenceBase reference)
    {if(reference==null)throw new NullReferenceException();return (Kingmaker.Blueprints.SimpleBlueprint)Getter.Invoke(reference,null);}
    static readonly MethodInfo Setter=typeof(Kingmaker.Blueprints.BlueprintReferenceBase).GetProperty("Cached",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly)?.GetSetMethod(true)??throw new MissingMemberException("BlueprintReferenceBase.Cached setter");
    public static void SetCached(Kingmaker.Blueprints.BlueprintReferenceBase reference,Kingmaker.Blueprints.SimpleBlueprint value)
    {if(reference==null)throw new NullReferenceException();Setter.Invoke(reference,new object[]{value});}
}
