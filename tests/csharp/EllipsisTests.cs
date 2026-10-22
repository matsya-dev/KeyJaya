using System;namespace KeyJaya {public static class EllipsisTests {
 static int passed;static void Equal(string a,string b){if(a!=b)throw new Exception("Ellipsis fixture mismatch");passed++;}
 public static int Main(){foreach(string raw in new[]{"...",". . ."," ..."," . . .","\u00a0..."})foreach(char delimiter in " ,;:!?)]}\"'’”"){
  var input=new InputState();foreach(char c in "Earlier unchanged"+raw)input.Type(c);string pending=input.CompletedEllipsis(delimiter);if(pending==null)throw new Exception("Expected ellipsis completion");Equal("\u00a0.\u00a0.\u00a0."+delimiter,EllipsisTyping.Replacement(pending));
 }
 foreach(string text in new[]{"..","....",". . . .","\u00a0.\u00a0.\u00a0.","https://example.com/...","name@example...","`code..."}){var input=new InputState();foreach(char c in text)input.Type(c);Equal(null,input.CompletedEllipsis(' '));}
 foreach(char delimiter in "a0.\n\t") {var input=new InputState();foreach(char c in "...")input.Type(c);Equal(null,input.CompletedEllipsis(delimiter));}
 var reset=new InputState();foreach(char c in "...")reset.Type(c);reset.Clear();Equal(null,reset.CompletedEllipsis(' '));foreach(char c in "...")reset.Type(c);reset.Paused=true;Equal(null,reset.CompletedEllipsis(' '));
 Equal(null,EllipsisTyping.Replacement("earlier... "));Equal(null,EllipsisTyping.Replacement(".... "));Equal(null,EllipsisTyping.Replacement(". . ."));
 Console.WriteLine("PASS: "+passed+" FAIL: 0");return 0;
 }}}
