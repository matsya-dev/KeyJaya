using System;using System.IO;using System.Collections.Generic;using System.Linq;using System.Threading;
namespace KeyJaya {
 // Keep the installation directory stable: editors and other processes may hold it open.
 public sealed class InstallTransaction {
  readonly string root,stage;readonly List<string> changed=new List<string>(),created=new List<string>();
  public string Backup {get;private set;}
  public InstallTransaction(string root,string stage){this.root=Path.GetFullPath(root);this.stage=Path.GetFullPath(stage);}
  static void NoLink(string path){if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("Installation links are unsupported: "+path);}
  static bool Same(string a,string b){if(!File.Exists(a)||!File.Exists(b))return false;if(new FileInfo(a).Length!=new FileInfo(b).Length)return false;using(var x=File.OpenRead(a))using(var y=File.OpenRead(b)){byte[] xb=new byte[65536],yb=new byte[65536];int n;while((n=x.Read(xb,0,xb.Length))>0){int got=0;while(got<n){int m=y.Read(yb,got,n-got);if(m==0)return false;got+=m;}for(int i=0;i<n;i++)if(xb[i]!=yb[i])return false;}return true;}}
  static void CopyTree(string source,string target){NoLink(source);Directory.CreateDirectory(target);foreach(string file in Directory.GetFiles(source)){NoLink(file);File.Copy(file,Path.Combine(target,Path.GetFileName(file)));}foreach(string dir in Directory.GetDirectories(source))CopyTree(dir,Path.Combine(target,Path.GetFileName(dir)));}
  void EnsureDirectory(string path){if(Directory.Exists(path)){NoLink(path);return;}string parent=Path.GetDirectoryName(path);if(!Directory.Exists(parent))EnsureDirectory(parent);Directory.CreateDirectory(path);created.Add(path);}
  static void Retry(Action action,string path){for(int attempt=0;;attempt++){try{action();return;}catch(IOException){if(attempt>=9)throw;Thread.Sleep(100);}catch(UnauthorizedAccessException ex){if(attempt>=9)throw new IOException("Cannot replace file "+path+". Close apps using that file and retry setup.",ex);Thread.Sleep(100);}}}
  public void Apply(Action<int,int> progress=null){
   // Complete the backup before changing anything; never rename the existing root.
   if(Directory.Exists(root)){Backup=root+".backup-"+Guid.NewGuid().ToString("N");CopyTree(root,Backup);}
   EnsureDirectory(root);
   foreach(string directory in Directory.GetDirectories(stage,"*",SearchOption.AllDirectories).OrderBy(x=>x.Length)){NoLink(directory);EnsureDirectory(Path.Combine(root,directory.Substring(stage.Length+1)));}
   string[] sources=Directory.GetFiles(stage,"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.Ordinal).ToArray();int count=0;foreach(string source in sources){
    NoLink(source);string relative=source.Substring(stage.Length+1),target=Path.Combine(root,relative);EnsureDirectory(Path.GetDirectoryName(target));
    if(File.Exists(target)){NoLink(target);if(Same(source,target)){if(progress!=null)progress(++count,sources.Length);continue;}Retry(()=>File.Replace(source,target,null),target);}
    else Retry(()=>File.Move(source,target),target);
    changed.Add(relative);if(progress!=null)progress(++count,sources.Length);
   }
  }
  public void Rollback(){
   // Only touch successfully committed files, so a locked, unchanged file does not
   // prevent restoring earlier changes. Preserve the complete backup for recovery.
   List<Exception> errors=new List<Exception>();
   for(int i=changed.Count-1;i>=0;i--){string relative=changed[i],target=Path.Combine(root,relative),original=Backup==null?null:Path.Combine(Backup,relative);try{
    if(original!=null&&File.Exists(original)){if(Same(original,target))continue;string temp=Path.Combine(stage,"restore-"+Guid.NewGuid().ToString("N"));File.Copy(original,temp);Retry(()=>{if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);},target);}
    else if(File.Exists(target))Retry(()=>File.Delete(target),target);
   }catch(Exception ex){errors.Add(ex);}}
   for(int i=created.Count-1;i>=0;i--)try{if(Directory.Exists(created[i])&&!Directory.EnumerateFileSystemEntries(created[i]).Any())Directory.Delete(created[i]);}catch(Exception ex){errors.Add(ex);}
   if(errors.Count>0)throw new AggregateException("Some installation files could not be restored. Backup: "+Backup,errors);
  }
  public void Retire(string relative){
   string target=Path.GetFullPath(Path.Combine(root,relative));if(!target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Unsafe retired configuration path");
   if(!File.Exists(target))return;NoLink(target);Retry(()=>File.Delete(target),target);changed.Add(relative);
  }
 }
}
