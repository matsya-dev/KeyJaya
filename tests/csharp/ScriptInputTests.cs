using System;namespace KeyJaya {
public static class ScriptInputTests {
 static int passed,failed;static void Equal(string expected,string actual,string name){if(expected==actual)passed++;else{failed++;Console.WriteLine("FAIL "+name+" expected="+expected+" actual="+actual);}}
 public static int Main(string[] args){using(Core c=new Core(args[0])){
 string[,] examples={{"sa","स","স"},{"na","न","ন"},{"hi","हि","হি"},{"HI","हि","হি"},{"ka","क","ক"},{"kala","काल","কাল"},{"aapka","आपका","আপকা"},{"āpkā","आपका","আপকা"},{"āpakā","आपका","আপকা"},{"hihihi","हिहिहि","হিহিহি"},{"aapkee","आपकी","আপকী"},{"kṛṣṇa","कृष्ण","কৃষ্ণ"},{"śakti","शक्ति","শক্তি"},{"çakti","शक्ति","শক্তি"},{"kari'","करिऽ","করি’"}};
 for(int i=0;i<examples.GetLength(0);i++)foreach(string mode in new[]{"devanagari","bengali"})Equal(examples[i,mode=="devanagari"?1:2],c.ConvertCopied(examples[i,0],mode),"requested/common syllable "+examples[i,0]+" "+mode);
 string[] sounds={"k","kh","g","gh","ṅ","c","ch","j","jh","ñ","ṭ","ṭh","ḍ","ḍh","ṇ","t","th","d","dh","n","p","ph","b","bh","m","y","r","l","v","ś","ṣ","s","h","kṣ","jñ","tr","śr"};string[] devanagari={"क","ख","ग","घ","ङ","च","छ","ज","झ","ञ","ट","ठ","ड","ढ","ण","त","थ","द","ध","न","प","फ","ब","भ","म","य","र","ल","व","श","ष","स","ह","क्ष","ज्ञ","त्र","श्र"};string[] bengaliSounds={"ক","খ","গ","ঘ","ঙ","চ","ছ","জ","ঝ","ঞ","ট","ঠ","ড","ঢ","ণ","ত","থ","দ","ধ","ন","প","ফ","ব","ভ","ম","য","র","ল","ব","শ","ষ","স","হ","ক্ষ","জ্ঞ","ত্র","শ্র"};string[] vowels={"a","ā","i","ī","u","ū","ṛ","ṝ","ḷ","ḹ","e","ai","o","au"},devMatra={"","ा","ि","ी","ु","ू","ृ","ॄ","ॢ","ॣ","े","ै","ो","ौ"},benMatra={"","া","ি","ী","ু","ূ","ৃ","ৄ","ৢ","ৣ","ে","ৈ","ো","ৌ"};
 for(int n=0;n<sounds.Length;n++){Equal(devanagari[n]+"्",c.ConvertCopied(sounds[n],"devanagari"),"bare consonant "+sounds[n]);Equal(bengaliSounds[n]+"্",c.ConvertCopied(sounds[n],"bengali"),"bare Bengali consonant "+sounds[n]);for(int v=0;v<vowels.Length;v++){Equal(devanagari[n]+devMatra[v],c.ConvertCopied(sounds[n]+vowels[v],"devanagari"),"all vowel signs "+sounds[n]+vowels[v]);Equal(bengaliSounds[n]+benMatra[v],c.ConvertCopied(sounds[n]+vowels[v],"bengali"),"all Bengali vowel signs "+sounds[n]+vowels[v]);}}
 string[] devVowels={"अ","आ","इ","ई","उ","ऊ","ऋ","ॠ","ऌ","ॡ","ए","ऐ","ओ","औ"},benVowels={"অ","আ","ই","ঈ","উ","ঊ","ঋ","ৠ","ঌ","ৡ","এ","ঐ","ও","ঔ"};for(int v=0;v<vowels.Length;v++){Equal(devVowels[v],c.ConvertCopied(vowels[v],"devanagari"),"independent vowel "+vowels[v]);Equal(benVowels[v],c.ConvertCopied(vowels[v],"bengali"),"independent Bengali vowel "+vowels[v]);}
 string[,] hindi={{"mera","मेरा"},{"meri","मेरी"},{"tumhara","तुम्हारा"},{"namaste","नमस्ते"},{"hai","है"},{"nahi","नहीं"},{"achha","अच्छा"},{"bada","बड़ा"},{"ladki","लड़की"},{"kya","क्या"},{"paani","पानी"},{"kahan","कहाँ"},{"ghar","घर"},{"zindagi","ज़िंदगी"}};
 for(int i=0;i<hindi.GetLength(0);i++)Equal(hindi[i,1],c.ConvertCopied(hindi[i,0],"devanagari"),"common Hindi "+hindi[i,0]);
 string[,] bengali={{"ami","আমি"},{"tumi","তুমি"},{"apni","আপনি"},{"amar","আমার"},{"bhalo","ভালো"},{"ache","আছে"},{"boro","বড়"},{"chhoto","ছোট"},{"kothay","কোথায়"},{"dhonnobad","ধন্যবাদ"},{"bangla","বাংলা"}};
 for(int i=0;i<bengali.GetLength(0);i++)Equal(bengali[i,1],c.ConvertCopied(bengali[i,0],"bengali"),"common Bengali "+bengali[i,0]);
 Equal("बड़ा लड़की आदमी",c.ConvertCopied("baṛā laṛkī ādmī","devanagari"),"modern IAST flap and written schwa");
 Equal("বড় বড়ো",c.ConvertCopied("baṛa baṛo","bengali"),"modern Bengali flap");
 Equal("स्कूल कंप्यूटर फ़ोन",c.ConvertCopied("school computer phone","devanagari"),"common modern Hindi loans");
 Equal("স্কুল কম্পিউটার ফোন",c.ConvertCopied("school computer phone","bengali"),"common modern Bengali loans");
 foreach(string mode in new[]{"unicode","balaram","plain","itrans"})Equal("school computer phone",c.ConvertCopied("school computer phone",mode),"modern defaults never affect English in ordinary converters");
 foreach(string mode in new[]{"devanagari","bengali"}){
 Equal(mode=="devanagari"?"न हि (आपका), काल!":"ন হি (আপকা), কাল!",c.ConvertCopied("na hi (aapka), kala!",mode),"spacing/punctuation unchanged");
 foreach(string literal in new[]{"https://example.com/hi","name@example.com","C:\\Users\\hi\\file","Xylophone","xyzzy"})Equal(literal,c.ConvertCopied(literal,mode),"protected/unsupported token atomic");
 }
 foreach(string mode in new[]{"unicode","balaram","plain","itrans"})Equal("an age since hi man",c.ConvertCopied("an age since hi man",mode),"English protection outside explicit script tools");
 Equal(null,c.Trigger("hi>"),"script-only syllable change does not authorize automatic trigger");
 Equal(null,c.Trigger("na>"),"script-only syllable change does not authorize automatic trigger");
 Equal("ज़ फ़ क़ ड़ ढ़",IndicScripts.FromIast("za fa qa r̤a r̤ha","devanagari"),"modern nukta consonants");
 Equal("qe k͟hā ġi zu fo",c.ConvertCopied("ক়ে খ়া গ়ি জ়ু ফ়ো","unicode"),"modern Bengali nukta consumed with vowel signs");
 Equal("अं ऽ क्ष",c.ConvertCopied("a.m .a kSha","devanagari"),"copied ITRANS markers retain script conversion");
 Equal("অং ’ ক্ষ",c.ConvertCopied("a.m .a kSha","bengali"),"copied ITRANS markers retain Bengali conversion");
 }
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;}
}
}
