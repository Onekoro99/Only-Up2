using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace GirlsFall.InspectorHelp.Editor {
 public static class TooltipAudit {
  static object Get(object o,string p){return o.GetType().GetProperty(p).GetValue(o,null);}
  static IEnumerable<object> Items(object o,string p){return ((IEnumerable)Get(o,p)).Cast<object>();}
  static string Operand(object value){
   if(value==null)return "";
   if(value is string)return (string)value;
   if(value is IEnumerable)return string.Join("|",((IEnumerable)value).Cast<object>().Select(Operand));
   var formatted=value as IFormattable;return formatted!=null?formatted.ToString(null,System.Globalization.CultureInfo.InvariantCulture):value.ToString();
  }
  static object Attributes(object field){
   return Items(field,"CustomAttributes").Where(a=>!new[]{"UnityEngine.TooltipAttribute","UnityEngine.HeaderAttribute","UnityEngine.SpaceAttribute"}.Contains(Get(Get(a,"AttributeType"),"FullName").ToString())).Select(a=>new{
    type=Get(Get(a,"AttributeType"),"FullName").ToString(),
    args=Items(a,"ConstructorArguments").Select(arg=>Operand(Get(arg,"Value"))).ToArray()
   }).ToArray();
  }
  public static string Behavior(string dll){
   var cecil=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Unity.Cecil");
   var moduleType=cecil.GetType("Mono.Cecil.ModuleDefinition");
   var module=moduleType.GetMethod("ReadModule",new[]{typeof(string)}).Invoke(null,new object[]{dll});
   try{
    var records=new List<object>();Action<object> visit=null;
    visit=type=>{
     records.Add(new{type=Get(type,"FullName").ToString(),fields=Items(type,"Fields").Select(f=>new{name=Get(f,"Name").ToString(),fieldType=Get(Get(f,"FieldType"),"FullName").ToString(),flags=Get(f,"Attributes").ToString(),constant=Operand(Get(f,"Constant")),attributes=Attributes(f)}).ToArray()});
     foreach(var method in Items(type,"Methods")){
      var body=Get(method,"Body");
      records.Add(new{method=Get(method,"FullName").ToString(),flags=Get(method,"Attributes").ToString(),
       instructions=body==null?new string[0]:Items(body,"Instructions").Select(i=>Get(i,"OpCode").ToString()+":"+Operand(Get(i,"Operand"))).ToArray(),
       locals=body==null?new string[0]:Items(body,"Variables").Select(v=>Get(Get(v,"VariableType"),"FullName").ToString()).ToArray(),
       exceptions=body==null?new string[0]:Items(body,"ExceptionHandlers").Select(e=>string.Join("|",new[]{"HandlerType","TryStart","TryEnd","HandlerStart","HandlerEnd","FilterStart","CatchType"}.Select(p=>Operand(Get(e,p))))).ToArray()});
     }
     foreach(var nested in Items(type,"NestedTypes"))visit(nested);
    };
    foreach(var t in Items(module,"Types"))visit(t);return JsonConvert.SerializeObject(records);
   }finally{((IDisposable)module).Dispose();}
  }
  public static object Run(){
   var inventory=JArray.Parse(File.ReadAllText("Library/InspectorTooltip/Inventory.json"));var missing=new List<string>();
   var rows=new List<object>();
   foreach(var row in inventory){
    var type=typeof(GirlsFall.Dialogue.DialogueManager).Assembly.GetType((string)row["type"]);
    var field=type.GetField((string)row["field"],BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    var tip=(TooltipAttribute)Attribute.GetCustomAttribute(field,typeof(TooltipAttribute));
    bool valid=tip!=null&&System.Text.RegularExpressions.Regex.IsMatch(tip.tooltip,@"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]");
    if(!valid)missing.Add(type.FullName+"."+field.Name);
    rows.Add(new{type=type.FullName,field=field.Name,file=(string)row["file"],tooltip=tip==null?"":tip.tooltip,valid=valid,newlyAdded=string.IsNullOrEmpty((string)row["tooltip"])});
   }
   string before=Behavior("Library/InspectorTooltip/BeforeAssembly.dll"),after=Behavior("Library/ScriptAssemblies/Assembly-CSharp.dll");
   File.WriteAllText("Library/InspectorTooltip/BeforeBehavior.json",before);File.WriteAllText("Library/InspectorTooltip/AfterBehavior.json",after);
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab");
   int inspectorProperties=0;var inspectorMissing=new List<string>();
   foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true)){
    if(component.GetType().Assembly!=typeof(GirlsFall.Dialogue.DialogueManager).Assembly)continue;
    var so=new SerializedObject(component);
    foreach(var row in inventory.Where(r=>(string)r["type"]==component.GetType().FullName)){
     var p=so.FindProperty((string)row["field"]);if(p==null)continue;inspectorProperties++;
     if(string.IsNullOrEmpty(p.tooltip))inspectorMissing.Add(component.GetType().Name+"."+p.propertyPath);
    }
   }
   var data=AssetDatabase.LoadAssetAtPath<GirlsFall.Dialogue.DialogueData>("Assets/GirlsFall/Dialogue/Data/OpeningDialogue.asset");var serialized=new SerializedObject(data);
   foreach(string p in new[]{"defaultCharacterName","defaultCharactersPerSecond","lines","lines.Array.data[0].characterName","lines.Array.data[0].text","lines.Array.data[0].expression","lines.Array.data[0].charactersPerSecond","lines.Array.data[0].delayBefore"}){
    var property=serialized.FindProperty(p);inspectorProperties++;if(property==null||string.IsNullOrEmpty(property.tooltip))inspectorMissing.Add("DialogueData."+p);
   }
   var hashes=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText("Assets/GirlsFall/InspectorHelp/Backups/Protected.json"));var changed=new List<string>();
   using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var h in hashes)if(!File.Exists(h.Key)||BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(h.Key)))!=h.Value)changed.Add(h.Key);
   File.WriteAllText("Library/InspectorTooltip/Coverage.json",JsonConvert.SerializeObject(rows,Formatting.Indented));
   var report=new{fields=inventory.Count,newlyAdded=rows.Count()-inventory.Count(r=>!string.IsNullOrEmpty((string)r["tooltip"])),missing=missing,behaviorAndFieldDefinitionsUnchanged=before==after,serializedTooltipChecks=inspectorProperties,inspectorMissing=inspectorMissing,protectedFiles=hashes.Count,changedProtectedFiles=changed};
   File.WriteAllText("Library/InspectorTooltip/Audit.json",JsonConvert.SerializeObject(report,Formatting.Indented));return report;
  }
 }
}