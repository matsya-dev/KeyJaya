using System;using System.IO;using System.Text;using System.Security.Cryptography;using System.Runtime.InteropServices;
namespace KeyJaya {
public static class LaunchRequests {
 public const string CheatSheetArgument="--open-cheat-sheet";
 public static readonly uint Message=RegisterWindowMessage("KeyJaya.OpenCheatSheet.v1");
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern uint RegisterWindowMessage(string name);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string kind,string caption);
 [DllImport("user32.dll",EntryPoint="SendMessageTimeoutW")]static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr first,IntPtr second,uint flags,uint timeout,out UIntPtr result);
 public static string Caption(string root){string normalized=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).ToUpperInvariant();using(var hash=SHA256.Create())return "KeyJaya resident "+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(normalized))).Replace("-","");}
 public static bool Request(string root){return Request(()=>FindWindow(null,Caption(root)),window=>{UIntPtr result;return Message!=0&&SendMessageTimeout(window,Message,IntPtr.Zero,IntPtr.Zero,2,100,out result)!=IntPtr.Zero&&result.ToUInt64()==1;},System.Threading.Thread.Sleep);}
 public static bool Request(Func<IntPtr> find,Func<IntPtr,bool> send,Action<int> wait){for(int attempt=0;attempt<40;attempt++){IntPtr target=find();if(target!=IntPtr.Zero&&send(target))return true;if(attempt<39)wait(25);}return false;}
}
}
