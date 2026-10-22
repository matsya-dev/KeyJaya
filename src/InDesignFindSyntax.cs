using System;using System.Text;
namespace KeyJaya {
// Convert literal search text without rewriting GREP operators or InDesign codes.
public static class InDesignFindSyntax {
 static int ProtectedEnd(string text,int at){
  char c=text[at];int n=text.Length;
  if(c=='\\'){
   if(at+1>=n)return n;char code=text[at+1];int end=at+2;
   if(code=='Q'){int closing=text.IndexOf("\\E",end,StringComparison.Ordinal);return closing<0?n:closing+2;}
   if((code=='p'||code=='P'||code=='x'||code=='g'||code=='k')&&end<n&&(text[end]=='{'||text[end]=='<')){char close=text[end]=='{'?'}':'>';int stop=text.IndexOf(close,end+1);return stop<0?n:stop+1;}
   if(code=='u'||code=='x'){int limit=code=='u'?4:2;while(end<n&&end<at+2+limit&&Uri.IsHexDigit(text[end]))end++;}
   if(char.IsDigit(code))while(end<n&&char.IsDigit(text[end]))end++;
   return end;
  }
  if(c=='['){int end=at+1;if(end<n&&text[end]=='^')end++;if(end<n&&text[end]==']')end++;while(end<n){if(text[end]=='\\'){end=Math.Min(n,end+2);continue;}if(text[end++]==']')return end;}return n;}
  if(c=='~'&&at+1<n)return at+2;
  if(c=='$'&&at+1<n){int end=at+1;if(char.IsDigit(text[end])){while(end<n&&char.IsDigit(text[end]))end++;return end;}if(text[end]=='{'){int stop=text.IndexOf('}',end+1);return stop<0?n:stop+1;}}
  if(c=='('&&at+2<n&&text[at+1]=='?'){
   if(text[at+2]=='#'){int stop=text.IndexOf(')',at+3);return stop<0?n:stop+1;}
   if(text[at+2]=='<'&&at+3<n&&text[at+3]!='='&&text[at+3]!='!'){int stop=text.IndexOf('>',at+3);return stop<0?n:stop+1;}
   int end=at+2;while(end<n&&(char.IsLetter(text[end])||text[end]=='-'))end++;
   if(end>at+2&&end<n&&(text[end]==':'||text[end]==')'))return end+1;
  }
  return at;
 }
 public static bool IsLiteralRange(string text,int start,int end){
  if(text==null||start<0||end<start||end>text.Length)return false;
  for(int at=0;at<text.Length;){int stop=ProtectedEnd(text,at);if(stop==at){at++;continue;}if(start<stop&&end>at)return false;at=stop;}return true;
 }
 public static string ConvertRange(string text,int start,int end,Func<string,string> convert){
  if(text==null||convert==null||start<0||end<start||end>text.Length)throw new ArgumentException();
  var result=new StringBuilder();int literal=start,at=0;
  while(at<end){int stop=ProtectedEnd(text,at);if(stop==at){at++;continue;}
   if(stop>start){int begin=Math.Max(start,at),finish=Math.Min(end,stop);if(begin>literal)result.Append(convert(text.Substring(literal,begin-literal)));result.Append(text,begin,finish-begin);literal=finish;}at=stop;
  }
  if(literal<end)result.Append(convert(text.Substring(literal,end-literal)));return result.ToString();
 }
 public static string ConvertLiterals(string text,Func<string,string> convert){
  if(text==null||convert==null)throw new ArgumentNullException();var result=new StringBuilder();int literal=0,at=0;
  while(at<text.Length){int end=ProtectedEnd(text,at);if(end==at){at++;continue;}if(at>literal)result.Append(convert(text.Substring(literal,at-literal)));result.Append(text,at,end-at);at=literal=end;}
  if(literal<text.Length)result.Append(convert(text.Substring(literal)));return result.ToString();
 }
}
}
