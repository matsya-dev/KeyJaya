using System;using System.IO;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace KeyJaya {public static class CopiedConversionTests {
static long passed,failed;static void Check(bool ok,string label){if(ok)passed++;else{if(failed++<40)Console.WriteLine("FAIL "+label);}}
static void Equal(string expected,string actual,string label){Check(expected==actual,label+" expected="+expected+" actual="+actual);}
public static int Main(string[] args){using(Core c=new Core(args[0])){
foreach(string elision in new[]{"kari'","kari’"}){
Equal("करिऽ",c.ConvertCopied(elision,"devanagari"),"Bengali book elision to avagraha");
Equal("করি’",c.ConvertCopied(elision,"bengali"),"Bengali book elision retained");
}
foreach(string spelling in new[]{"sakti","shakti","śakti","çakti"}){
Equal("शक्ति",c.ConvertCopied(spelling,"devanagari"),"Sakti copied Devanagari "+spelling);
Equal("শক্তি",c.ConvertCopied(spelling,"bengali"),"Sakti copied Bengali "+spelling);
Equal("śakti",c.Trigger(spelling+">"),"Sakti explicit trigger "+spelling);
}
foreach(string mode in new[]{"devanagari","bengali"}){
foreach(string unicode in new[]{"nāśa","sāra","gāya","dīna","vidyā-yoga","śakti-prada"})
Equal(Core.Target(unicode,mode),c.ConvertCopied(EncodingMap.Balaram(unicode),mode),"Supplied legacy spelling retained "+unicode);
}
foreach(string mode in new[]{"unicode","balaram","plain","itrans"})foreach(string english in new[]{"well-being","state-of-the-art","doesn’t can't","an age since"})Equal(english,c.ConvertCopied(english,mode),"English punctuation protected outside explicit script tools");
string source="shraddhAvAnllabhate j~nAnaM tatparaH saMyatendriyaH |\nj~nAnaM labdhvA parAM shAntimachireNAdhigachChati ||";
string expected="śraddhāvān labhate jñānaṁ tat-paraḥ saṁyatendriyaḥ |\njñānaṁ labdhvā parāṁ śāntim acireṇādhigacchati ||";
foreach(string mode in new[]{"unicode","balaram","plain","itrans","devanagari","bengali"}){
Equal(Core.Target(expected,mode),c.ConvertCopied(source,mode),"requested verse "+mode);
Equal(Core.Target(expected,mode),c.ConvertItrans(source,mode),"explicit verse "+mode);
if(mode!="devanagari"&&mode!="bengali")Equal("an age since EnglishUnknown",c.ConvertCopied("an age since EnglishUnknown",mode),"English sentence "+mode);
Equal("Hanumān",c.Trigger("Hanuman>"),"Hanuman Unicode");Equal("Hanumän",c.Trigger("Hanuman<"),"Hanuman Balaram");
Equal("brāhmaṇas",c.Trigger("brahmanas>"),"plural primary");Equal("brähmaëas",c.Trigger("brahmanas<"),"plural legacy");
}
string[,] fixtures={{"कृष्ण","kṛṣṇa"},{"श्री","śrī"},{"ज्ञानम्","jñānam"},{"प्राणः","prāṇaḥ"},{"हन्","han"},{"अ आ इ ई उ ऊ ऋ ॠ ऌ ॡ ए ऐ ओ औ","a ā i ī u ū ṛ ṝ ḷ ḹ e ai o au"},{"क ख ग घ ङ च छ ज झ ञ ट ठ ड ढ ण त थ द ध न प फ ब भ म य र ल व श ष स ह","ka kha ga gha ṅa ca cha ja jha ña ṭa ṭha ḍa ḍha ṇa ta tha da dha na pa pha ba bha ma ya ra la va śa ṣa sa ha"},{"কৃষ্ণ","kṛṣṇa"},{"শ্রী","śrī"},{"জ্ঞানম্","jñānam"},{"প্রাণঃ","prāṇaḥ"},{"क्‍ष","kṣa"},{"१२३ ৪৫৬ । ॥","123 456 | ||"}};
for(int n=0;n<fixtures.GetLength(0);n++)Equal(fixtures[n,1],IndicScripts.ToIast(fixtures[n,0]),"published orthography "+n);
Equal("कृष्ण",c.ConvertCopied("kṛṣṇa","devanagari"),"IAST Devanagari");Equal("কৃষ্ণ",c.ConvertCopied("kṛṣṇa","bengali"),"IAST Bengali");
Equal("kṛṣṇa",c.ConvertCopied("कृष्ण","unicode"),"detected Devanagari");Equal("kṛṣṇa",c.ConvertCopied("কৃষ্ণ","unicode"),"detected Bengali");
Equal("bhagavān",c.ConvertCopied("ভগবান্","unicode"),"Bengali dictionary va");
Equal("ya",c.ConvertCopied("য়","unicode"),"reported Bengali yya");
Equal("ye",c.ConvertCopied("য়ে","unicode"),"reported Bengali yya vowel");
string[,] dottedLetters={{"য়","y"},{"ড়","ṛ"},{"ঢ়","ṛh"},{"क़","q"},{"ख़","k͟h"},{"ग़","ġ"},{"ज़","z"},{"ड़","r̤"},{"ढ़","r̤h"},{"फ़","f"},{"य़","ẏ"},{"ऱ","ṟ"},{"ऴ","l̤"},{"ऩ","ṉ"}};
string[] bengaliMarks={"","া","ি","ী","ু","ূ","ৃ","ৄ","ৢ","ৣ","ে","ৈ","ো","ৌ"};
string[] devaMarks={"","ा","ि","ी","ु","ू","ृ","ॄ","ॢ","ॣ","े","ै","ो","ौ"};
string[] markValues={"a","ā","i","ī","u","ū","ṛ","ṝ","ḷ","ḹ","e","ai","o","au"};
for(int n=0;n<dottedLetters.GetLength(0);n++){
bool bengali=n<3;string[] marks=bengali?bengaliMarks:devaMarks;string virama=bengali?"্":"्";
foreach(NormalizationForm form in new[]{NormalizationForm.FormC,NormalizationForm.FormD}){
for(int v=0;v<marks.Length;v++){
string original=(dottedLetters[n,0]+marks[v]).Normalize(form),result=(dottedLetters[n,1]+markValues[v]).Normalize(NormalizationForm.FormC);
Equal(result,IndicScripts.ToIast(original),"dotted direct vowel "+n+" "+v+" "+form);
foreach(string mode in new[]{"unicode","balaram","plain","itrans"})Equal(Core.Target(result,mode),c.ConvertCopied(original,mode),"dotted copied vowel "+n+" "+v+" "+form+" "+mode);
}
Equal(dottedLetters[n,1].Normalize(NormalizationForm.FormC),IndicScripts.ToIast((dottedLetters[n,0]+virama).Normalize(form)),"dotted virama "+n+" "+form);
Equal(("ka"+dottedLetters[n,1]+"e").Normalize(NormalizationForm.FormC),IndicScripts.ToIast(((bengali?"ক":"क")+dottedLetters[n,0]+(bengali?"ে":"े")).Normalize(form)),"dotted preceding consonant "+n+" "+form);
Equal((dottedLetters[n,1]+"e").Normalize(NormalizationForm.FormC),IndicScripts.ToIast((dottedLetters[n,0]+"\u200d"+(bengali?"ে":"े")).Normalize(form)),"dotted joiner "+n+" "+form);
}
}
Equal("rā ḻe",c.ConvertCopied("ৰা ळे","unicode"),"related extended consonant vowels");
Equal("ya yā yi yī yu yū ye yai yo yau y",c.ConvertCopied("য় য়া য়ি য়ী য়ু য়ূ য়ে য়ৈ য়ো য়ৌ য়্","unicode"),"Bengali yya complete common forms");
Equal("haya haye hayā",c.ConvertCopied("হয় হয়ে হয়া","unicode"),"Bengali yya inside words");
Equal("English ye, 12; prajāḥ\r\nṛi ṛhe",c.ConvertCopied("English য়ে, ১২; प्रजा: ॥ ६ ॥\r\nড়ি ঢ়ে","unicode"),"dotted letters mixed script and verse cleanup");
Equal("় ़",IndicScripts.ToIast("় ़"),"orphan nukta preserved");
foreach(string script in new[]{"प्रजा:","প্রজা:"}){
Equal("prajāḥ",IndicScripts.ToIast(script),"colon visarga direct "+script);
foreach(string mode in new[]{"unicode","balaram","plain","itrans","devanagari","bengali"})Equal(Core.Target("prajāḥ",mode),c.ConvertCopied(script,mode),"colon visarga copied "+script+" "+mode);
Equal("Earlier untouched prajāḥ next",c.ConvertCopied("Earlier untouched "+script+" next","unicode"),"colon visarga preserves surroundings");
}
Equal("prajāḥ prajāḥ prajāḥ prajāḥ",c.ConvertCopied("प्रजा: प्रजाः প্রজা: প্রজাঃ","unicode"),"real and ASCII visarga consistent");
Equal("kaḥ kaḥ āḥ āḥ",c.ConvertCopied("क: ক: आ: আ:","unicode"),"inherent and independent vowel visarga");
Equal("prajāḥ,\r\nprajāḥ!",c.ConvertCopied("प्रजा:,\r\nপ্রজা:!","unicode"),"visarga surrounding punctuation and line endings");
foreach(string literal in new[]{"English: IAST: prajā: 12:30 :", "https://example.com/प्रजा:", "C:\\प्रजा:\\file.txt"})Equal(literal,c.ConvertCopied(literal,"unicode"),"ordinary colon protected "+literal);
Equal("12:30 12:30",c.ConvertCopied("१२:३० ১২:৩০","unicode"),"script time digits converted");
string scriptVerse="महर्षय: सप्‍त पूर्वे चत्वारो मनवस्तथा ।\nमद्भ‍ावा मानसा जाता येषां लोक इमा: प्रजा: ॥ ६ ॥";
string verseIast="maharṣayaḥ sapta pūrve catvāro manavas tathā\nmad-bhāvā mānasā jātā yeṣāṁ loka imāḥ prajāḥ";
foreach(string mode in new[]{"unicode","balaram","plain","itrans"}){
Equal(Core.Target(verseIast,mode),c.ConvertCopied(scriptVerse,mode),"user verse clean "+mode);
Equal("English an age since: "+Core.Target("kṛṣṇa",mode)+", "+Core.Target("kṛṣṇa",mode)+"; "+Core.Target("kṛṣṇa",mode)+" and "+Core.Target("jñānaṁ",mode)+" 123.",c.ConvertCopied("English an age since: कृष्ण, কৃষ্ণ; kṛṣṇa and j~nAnaM ১২३.",mode),"mixed four encodings "+mode);
Equal("English"+Core.Target("kṛṣṇa",mode)+Core.Target("kṛṣṇa",mode)+"English",c.ConvertCopied("Englishकृष्णকৃষ্ণEnglish",mode),"adjacent script runs "+mode);
Equal("12 34 56\r\nEnglish | 7 |",c.ConvertCopied("१२ ৩৪ 56\r\nEnglish | 7 |",mode),"ordinary numbers and English bars "+mode);
foreach(string marker in new[]{"।","॥","|","||"})foreach(string digits in new[]{"६","৬","6"})Equal(Core.Target("prajāḥ",mode),c.ConvertCopied("प्रजा: "+marker+" "+digits+" "+marker,mode),"verse markers and number "+mode+marker+digits);
Equal("12 "+Core.Target("prajāḥ",mode),c.ConvertCopied("१२ प्रजा: ॥ ६ ॥",mode),"unrelated heading retained "+mode);
Equal(Core.Target("prajāḥ",mode)+" 12:30 10/11/2026 3.5",c.ConvertCopied("प्रजा: १२:३० ১০/১১/২০২৬ ३.५",mode),"times dates and quantities "+mode);
Equal(Core.Target("prajāḥ",mode)+"\r\n"+Core.Target("prajāḥ",mode),c.ConvertCopied("প্রজা: ॥ (৬) ॥\r\nप्रजा: || [6] ||",mode),"Bengali verse markers and CRLF "+mode);
}
Equal("https://example.com/कृष्ण|६\r\n"+"kṛṣṇa",c.ConvertCopied("https://example.com/कृष्ण|६\r\nकृष्ण ॥ ६ ॥","unicode"),"mixed protected URL");
Equal("  kṛṣṇa\t  \r\n\tkṛṣṇa  ",c.ConvertCopied("  कृष्ण\t  \r\n\tকৃষ্ণ  ","unicode"),"unmarked mixed script whitespace preserved");
foreach(string numbered in new[]{"प्रजा: ६॥","প্রজা: (৬) ||","प्रजा: [6] |","प्रजा: ॥६","প্রজা: |6|"})Equal("prajāḥ",c.ConvertCopied(numbered,"unicode"),"alternate final verse number "+numbered);
foreach(string mode in new[]{"devanagari","bengali"}){string digits=mode=="devanagari"?"१२":"১২";Equal(digits+" "+Core.Target("kṛṣṇa",mode)+" "+Core.Target("kṛṣṇa",mode)+" "+Core.Target("kṛṣṇa",mode)+" "+digits,c.ConvertCopied("12 कृष्ण কৃষ্ণ kṛṣṇa ১২",mode),"mixed script destination "+mode);}
Equal(": prajā :: prajā:: prajā ::",c.ConvertCopied(": प्रजा :: प्रजा:: প্রজা ::","unicode"),"detached and doubled colons preserved");
Equal("āyurveda",c.Trigger("ayurveda>"),"Ayurveda Unicode trigger");Equal("äyurveda",c.Trigger("ayurveda<"),"Ayurveda Balaram trigger");
Equal("Āyurveda",c.ConvertCopied("Ayurveda","unicode"),"Ayurveda copied case");Equal("äyurveda",c.ConvertCopied("ayurveda","balaram"),"Ayurveda copied Balaram");
Equal("Āyurveda",c.Trigger("Ayurveda>"),"Word capitalized Ayurveda Unicode");Equal("Äyurveda",c.Trigger("Ayurveda<"),"Word capitalized Ayurveda Balaram");Equal("Hanumān",c.Trigger("Hanuman>"),"Word capitalized Hanuman");Equal("Brāhmaṇas",c.Trigger("Brahmanas>"),"Word capitalized plural");Equal("Doesn’t ",c.CorrectCompletedWord("Doesnt "),"Word capitalized contraction");
Equal("Āyurveda",c.TriggerWithObservedCase("ayurveda>","Ayurveda>"),"observed case recomputes Sanskrit output");Equal(null,c.TriggerWithObservedCase("ayurveda>","otherword>"),"observed text must differ only in case");Equal(null,c.TriggerWithObservedCase("an>","An>"),"observed case does not authorize English trigger");
foreach(string artifact in new[]{"awchhena","awchena","toxmara","girifriends"}){Check(c.Trigger(artifact+">")==null,"OCR artifact has no trigger "+artifact);Equal(artifact,c.ConvertCopied(artifact,"unicode"),"OCR artifact preserved "+artifact);Check(c.ClipboardChoices(artifact,"balaram","auto").Length==0,"OCR artifact has no cycle "+artifact);}
Equal("brahmañ",c.ConvertCopied("brahmañ","unicode"),"known IAST variant remains IAST");
Equal("vaiśāradyekṣayāsaṅga",c.ConvertItrans("vaishAradyekShayAsa~Nga","unicode"),"word fragment cannot gain verse hyphen");
Check(c.ConvertCopied(source+" 123","unicode").EndsWith(" 123"),"verse annotation digits preserved");
foreach(string form in new[]{"cHarm","cHarms","aGe","aN","sInce","SHould","MARCH","shes"})foreach(string mode in new[]{"unicode","balaram","plain","itrans"})Equal(form,c.ConvertCopied(form,mode),"mixed-case English "+form);
string[,] plurals={{"brahmanas","brāhmaṇas"},{"vaisnavas","vaiṣṇavas"},{"ksatriyas","kṣatriyas"},{"sudras","śūdras"},{"sannyasis","sannyāsīs"},{"rsis","ṛṣis"},{"gosvamis","gosvāmīs"},{"acaryas","ācāryas"},{"grhasthas","gṛhasthas"},{"brahmacaris","brahmacārīs"},{"sastras","śāstras"},{"asramas","āśramas"}};
for(int n=0;n<plurals.GetLength(0);n++){Equal(plurals[n,1],c.Trigger(plurals[n,0]+">"),"Sanskrit plural "+n);Equal(EncodingMap.Balaram(plurals[n,1]),c.Trigger(plurals[n,0]+"<"),"Balaram plural "+n);}
Equal("xylophone",IndicScripts.FromIast("xylophone","devanagari"),"unsupported word atomic");
Equal("jñānaṁ  tatparaḥ\r\nunknownword",c.ConvertCopied("j~nAnaM  tatparaH\r\nunknownword","unicode"),"unknown passage spacing retained");
foreach(string literal in new[]{"https://example.com/j~nAnaM","name@example.com","C:\\some\\shraddhA.txt"})Equal(literal,c.ConvertCopied(literal,"unicode"),"protected literal");
string[] phonemes={"a","ā","i","ī","u","ū","ṛ","ṝ","ḷ","ḹ","e","ai","o","au","ka","kha","ga","gha","ṅa","ca","cha","ja","jha","ña","ṭa","ṭha","ḍa","ḍha","ṇa","ta","tha","da","dha","na","pa","pha","ba","bha","ma","ya","ra","la","va","śa","ṣa","sa","ha","kṣa","jña","tra","śra","aṁ","aḥ","kam","k"};
foreach(string a in phonemes)foreach(string b in phonemes){string word=a+b;foreach(string mode in new[]{"devanagari","bengali"})Equal(mode=="bengali"?word.Replace('v','b'):word,IndicScripts.ToIast(IndicScripts.FromIast(word,mode)),"phoneme roundtrip "+mode+" "+word);}
string[] words=args.Contains("--quick")?new string[0]:File.ReadLines(args[1]).Skip(1).Select(x=>x.Split('/')[0]).Where(x=>Regex.IsMatch(x,@"^[A-Za-z]+$")&&!Core.IndicLoanwords.Contains(x)).Distinct().ToArray();
foreach(string word in words)foreach(string form in new[]{word,char.ToUpperInvariant(word[0])+word.Substring(1),word.ToUpperInvariant()})foreach(string mode in new[]{"unicode","balaram","plain","itrans"})Equal(form,c.ConvertCopied(form,mode),"copied English "+form+" "+mode);
foreach(string word in words.Take(1000))foreach(string mode in new[]{"unicode","balaram","plain","itrans"})Equal(word+Core.Target("kṛṣṇa",mode)+word+Core.Target("kṛṣṇa",mode)+word,c.ConvertCopied(word+"कृष्ण"+word+"কৃষ্ণ"+word,mode),"English beside mixed scripts "+word+" "+mode);
Console.WriteLine("Copied English headwords: "+words.Length);}
Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;
}}}
