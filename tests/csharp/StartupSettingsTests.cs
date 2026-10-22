using System;using System.IO;
namespace KeyJaya {public static class StartupSettingsTests {
 public static int Main(){int passed=0;string folder=Path.Combine(Path.GetDirectoryName(typeof(StartupSettingsTests).Assembly.Location),"KeyJaya-startup-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"settings.ini");
 string legacy=Path.Combine(folder,"espanso","keyjaya"),exe=Path.Combine(legacy,"engine","keyjaya-app.exe");foreach(string command in new[]{exe,"\""+exe+"\""}){if(!StartupSettings.IsLegacyCommand(command,legacy))throw new Exception("Obsolete startup alias retained");passed++;}foreach(string command in new[]{"\""+exe+"\" --other",Path.Combine(folder,"other.exe"),"",null}){if(StartupSettings.IsLegacyCommand(command,legacy))throw new Exception("Unrelated startup command removed");passed++;}
 var startup=new StartupSettings(path);if(!startup.Enabled)throw new Exception("Default disabled");passed++;
 File.WriteAllText(path,"encoding_override=balaram\nscript_language=hindi\ncustom=keep\n");startup.Set(false);if(new StartupSettings(path).Enabled)throw new Exception("Disable not persisted");passed++;
 if(!File.ReadAllText(path).Contains("encoding_override=balaram")||!File.ReadAllText(path).Contains("custom=keep"))throw new Exception("Unrelated settings lost");passed++;
 startup.Set(true);if(!new StartupSettings(path).Enabled)throw new Exception("Enable not persisted");passed++;
 File.WriteAllText(path,"start_with_windows=invalid\n");try{new StartupSettings(path);throw new Exception("Invalid setting accepted");}catch(InvalidDataException){passed++;}
 string blocked=Path.Combine(folder,"blocked");File.WriteAllText(blocked,"file");var failed=new StartupSettings(Path.Combine(blocked,"settings.ini"));try{failed.Set(false);throw new Exception("Blocked save succeeded");}catch(IOException){if(!failed.Enabled)throw new Exception("Failed save changed active preference");passed++;}
 Console.WriteLine("PASS: "+passed+" FAIL: 0");return 0;}
}}
