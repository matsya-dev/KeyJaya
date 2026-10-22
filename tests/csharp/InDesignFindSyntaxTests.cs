using System;namespace KeyJaya {public static class InDesignFindSyntaxTests {
 public static int Main(){int passed=0;Func<string,string> convert=s=>s.Replace("varna","varṇa").Replace("raja","rāja");
  string[] inputs={@"\braja\b",@"[a-z] varna $1",@"\p{Letter}varna\x{00E4}",@"(?<varna>raja)",@"(?#varna)raja",@"(?i:raja)",@"\Qvarna\E raja",@"~svarna",@"${varna} raja",@"[varna] raja",@"[varna",@"varna\"};
  string[] outputs={@"\brāja\b",@"[a-z] varṇa $1",@"\p{Letter}varṇa\x{00E4}",@"(?<varna>rāja)",@"(?#varna)rāja",@"(?i:rāja)",@"\Qvarna\E rāja",@"~svarṇa",@"${varna} rāja",@"[varna] rāja",@"[varna",@"varṇa\"};
  for(int i=0;i<inputs.Length;i++){if(InDesignFindSyntax.ConvertLiterals(inputs[i],convert)!=outputs[i])throw new Exception("Find syntax fixture "+i);passed++;}
  if(InDesignFindSyntax.IsLiteralRange(@"\s",1,2))throw new Exception("Escaped letter cannot cycle");passed++;
  if(InDesignFindSyntax.IsLiteralRange(@"[raja]",1,5))throw new Exception("Character class cannot cycle");passed++;
  if(!InDesignFindSyntax.IsLiteralRange(@"\braja\b",2,6))throw new Exception("Literal word may cycle");passed++;
  if(InDesignFindSyntax.ConvertRange(@"[raja] raja",1,11,convert)!="raja] rāja")throw new Exception("Partial character class selection");passed++;
  if(InDesignFindSyntax.ConvertRange(@"\braja\b",2,6,convert)!="rāja")throw new Exception("Selected literal range");passed++;
  if(InDesignFindSyntax.ConvertRange(@"${varna} raja",2,7,convert)!="varna")throw new Exception("Partial replacement reference");passed++;
  Console.WriteLine("PASS: "+passed+" FAIL: 0");return 0;
}}}
