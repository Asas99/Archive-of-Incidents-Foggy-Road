using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class DrawCallFrameAudit {
 static BindingFlags flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static Type utility,dataType; static MethodInfo getData,getInfo;static Array events;static int index,misses;static double start;static object data;static StringBuilder csv;static string prefix;
 static object Field(string name){return dataType.GetField(name,flags).GetValue(data);}
 static string Clean(object value){return Convert.ToString(value).Replace("\t"," ").Replace("\n"," ").Replace("\r","");}
 public static object Run(){return Start("Road");}
 public static object Start(string label){utility=typeof(Editor).Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");dataType=typeof(Editor).Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData");getData=utility.GetMethod("GetFrameEventData",flags);getInfo=utility.GetMethod("GetFrameEventInfoName",flags);events=(Array)utility.GetMethod("GetFrameEvents",flags).Invoke(null,null);if(events.Length==0)return "NO EVENTS";prefix="Recovery/DrawCalls_20261107/"+label;csv=new StringBuilder("index\ttype\tscope\tshader\tpass\tdraws\tinstances\tmesh\tcomponent\tbreak\n");index=misses=0;Prepare();EditorApplication.update-=Tick;EditorApplication.update+=Tick;return "Started detail capture "+events.Length;}
 static void Prepare(){data=Activator.CreateInstance(dataType);start=EditorApplication.timeSinceStartup;utility.GetProperty("limit",flags).SetValue(null,index+1);EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();getData.Invoke(null,new object[]{index,data});}
 static void Tick(){try{bool ok=(bool)getData.Invoke(null,new object[]{index,data});if(!ok&&EditorApplication.timeSinceStartup-start<.35)return;if(!ok)misses++;var e=events.GetValue(index);var c=EditorUtility.InstanceIDToObject((int)Field("m_ComponentInstanceID"));string path=c?c.name:"";var comp=c as Component;if(comp){path=comp.name;for(var t=comp.transform.parent;t;t=t.parent)path=t.name+"/"+path;}csv.AppendLine(String.Join("\t",index,Clean(e.GetType().GetField("m_Type",flags).GetValue(e)),Clean(getInfo.Invoke(null,new object[]{index})),Clean(Field("m_RealShaderName")),Clean(Field("m_PassName")),Field("m_DrawCallCount"),Field("m_InstanceCount"),Clean(Field("m_Mesh")),Clean(path),Field("m_BatchBreakCause")));index++;if(index%50==0)File.WriteAllText(prefix+"Details.status",index+"/"+events.Length+" missing="+misses);if(index>=events.Length){EditorApplication.update-=Tick;File.WriteAllText(prefix+"Details.tsv",csv.ToString());File.WriteAllText(prefix+"Details.status","COMPLETE "+index+" missing="+misses);return;}Prepare();}catch(Exception e){EditorApplication.update-=Tick;File.WriteAllText(prefix+"Details.status","ERROR "+e);}}
}

