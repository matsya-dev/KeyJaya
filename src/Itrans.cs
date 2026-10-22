using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace KeyJaya {
public static class Itrans {
    static readonly Dictionary<string,string> Map=new Dictionary<string,string>(StringComparer.Ordinal){
        {"R^I","ṝ"},{"R^i","ṛ"},{"RRi","ṛ"},{"Rri","ṛ"},{"RRI","ṝ"},{"RR","ṝ"},{"Ri","ṛ"},{"R","ṛ"},{"L^I","ḹ"},{"L^i","ḷ"},{"LLI","ḹ"},{"Lli","ḷ"},{"LLi","ḷ"},{"LL","ḹ"},{"Li","ḷ"},{"L","ḷ"},
        {"aa","ā"},{"A","ā"},{"ii","ī"},{"I","ī"},{"uu","ū"},{"U","ū"},
        {"JN","ñ"},{"dny","jñ"},{"jny","jñ"},{"jnj","jñ"},{"ld","ḷ"},{"ee","e"},{"oo","o"},{"N^","ṅ"},{"~N","ṅ"},{"~n","ñ"},{"j~n","jñ"},{"GY","jñ"},
        {"chh","ch"},{"CH","ch"},{"Ch","ch"},{"ch","c"},{"Th","ṭh"},{"Dh","ḍh"},{"T","ṭ"},{"D","ḍ"},{"N","ṇ"},
        {"z","ś"},{"S","ṣ"},{"shh","ṣ"},{"Sh","ṣ"},{"sh","ś"},{"w","v"},{"M","ṃ"},{".m","ṃ"},{".n","ṃ"},{".N","m̐"},{".N.h","m̐"},{".c","̆"},{".s","́"},{".ds","᳚"},{".au","̀"},{".p","᳒"},{".h",""},{".D","r̤"},{".Dh","r̤h"},{".k","ḵh"},{".g","ġ"},{".j","z"},{".y","ẏ"},{"q","q"},{"f","f"},{"zh","ḻ"},{"SRI","śrī"},{"OM","oṃ"},{"AUM","auṃ"},{"H","ḥ"},{".a","’"},
        {"a","a"},{"i","i"},{"u","u"},{"e","e"},{"o","o"},{"k","k"},{"g","g"},{"j","j"},{"c","c"},
        {"t","t"},{"d","d"},{"n","n"},{"p","p"},{"b","b"},{"m","m"},{"y","y"},{"r","r"},{"l","l"},{"v","v"},{"s","s"},{"h","h"},
        {"K","k"},{"G","g"},{"B","b"},{"P","p"},{"J","j"},{"V","v"},{"W","v"},{"Y","y"},{"C","c"},{"E","e"},{"O","o"}
    };
    static readonly string[] Keys=Map.Keys.OrderByDescending(x=>x.Length).ThenBy(x=>x,StringComparer.Ordinal).ToArray();
    static readonly Regex Tokens=new Regex(@"(?:[\p{L}\p{M}~^{}_]|\.(?:N\.h|Dh|ds|au|[mnNahDkgjycsp]))+",RegexOptions.Compiled);
    public static bool TryWord(string input,out string output){
        if(input.IndexOf('-')>=0){var parts=new List<string>();foreach(string piece in input.Split('-')){string decoded;if(!TryWord(piece,out decoded)){output=input;return false;}parts.Add(decoded);}output=string.Join("-",parts.ToArray());return true;}
        if(input=="x"){output="kṣa";return true;}
        StringBuilder text=new StringBuilder();for(int i=0;i<input.Length;){
            if(input[i]=='_'){i++;continue;}
            if(i+1<input.Length&&input[i]=='{'&&input[i+1]=='}'){i+=2;continue;}
            if(input[i]=='x'){text.Append("kṣ");i++;continue;}
            string key=null;foreach(string candidate in Keys)if(i+candidate.Length<=input.Length&&string.CompareOrdinal(input,i,candidate,0,candidate.Length)==0){key=candidate;break;}
            if(key==null){output=input;return false;}text.Append(Map[key]);i+=key.Length;
        }
        output=text.ToString();return input.Length>0;
    }
    public static string Convert(string input,string mode){return Tokens.Replace(input,m=>{string output;return TryWord(m.Value,out output)?Core.Target(output,mode):m.Value;});}
    public static string Encode(string input){return Regex.Replace(input.Normalize(NormalizationForm.FormC),@"[\p{L}\p{M}]+",m=>EncodeWord(m.Value)).Replace("m\u0310",".N").Replace("’",".a").Replace("'",".a");}
    static string EncodeWord(string input){
        const string letters="āīūṛṝḷḹṅñṭḍṇśṣḥṁṃĀĪŪṚṜḶḸṄÑṬḌṆŚṢḤṀṂ";
        string[] values={"A","I","U","R","RR","L","LL","~N","~n","T","D","N","sh","Sh","H","M","M","A","I","U","R","RR","L","LL","~N","~n","T","D","N","sh","Sh","H","M","M"};
        string normalized=input.Normalize(NormalizationForm.FormC);List<string> tokens=new List<string>();StringBuilder expected=new StringBuilder();
        for(int at=0;at<normalized.Length;at++){
            char c=normalized[at];int i=letters.IndexOf(c);
            if(c=='c'&&at+1<normalized.Length&&normalized[at+1]=='h'){tokens.Add("Ch");expected.Append("ch");at++;}
            else if(i>=0){tokens.Add(values[i]);expected.Append(char.ToLowerInvariant(c)=='ṁ'?'ṃ':char.ToLowerInvariant(c));}
            else if(c=='’'||c=='\''){tokens.Add(".a");expected.Append('’');}
            else{char canonical=char.ToLowerInvariant(c);tokens.Add(canonical.ToString());expected.Append(canonical);}
        }
        string result=string.Concat(tokens),decoded;
        // Token barriers are only needed where adjacent phonemes would form a different ITRANS token.
        if(TryWord(result,out decoded)&&decoded!=expected.ToString())result=string.Join("_",tokens.ToArray());
        return result;
    }
}
}
