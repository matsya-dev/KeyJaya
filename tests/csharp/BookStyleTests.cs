using System;using System.IO;using System.Linq;using System.Collections.Generic;
namespace KeyJaya {public static class BookStyleTests {
 static int passed,failed;static void Equal(string wanted,string got,string name){if(wanted==got)passed++;else{failed++;Console.WriteLine("FAIL "+name+" wanted="+wanted+" got="+got);}}
 static string Styled(Core c,string input,string mode){return string.Join("|",c.ItalicWords(input,mode).Select(s=>input.Substring(s.Start,s.Length)));}
 public static int Main(string[] args){using(Core c=new Core(args[0])){
 foreach(string mode in new[]{"unicode","balaram"}){
 foreach(string word in new[]{"āyurveda","Āyurveda"})Equal(Core.Target(word,mode),Styled(c,Core.Target(word,mode),mode),"requested Ayurveda italics "+mode);
 Equal(Core.Target("Tamāla Kṛṣṇa Goswami",mode),c.ConvertCopied("tamala krsna Goswami",mode),"capitalize complete name in title context "+mode);
 Equal(Core.Target("Satsvarūpa Dāsa Goswami",mode),c.ConvertCopied("satsvarupa Dasa Goswami",mode),"capitalize personal name before titles "+mode);
 Equal(Core.Target("Tamāla Kṛṣṇa Goswami and śāstra",mode),c.ConvertCopied("tamala krsna Goswami and sastra",mode),"name casing leaves adjacent prose unchanged "+mode);
 Equal("Goswami",c.ContextualGoswami("Gosvāmī","Tamāla Kṛṣṇa Gosvāmī",mode),"caret contemporary title "+mode);
 Equal(Core.Target("Gosvāmī",mode),c.ContextualGoswami("Goswami","Rūpa Goswami",mode),"caret historical title "+mode);
 Equal("Goswami",c.ContextualGoswami("Goswami","Unlisted Goswami",mode),"caret unknown title "+mode);
 Equal(Core.Target("Rūpa Gosvāmī",mode),c.ConvertCopied("Rupa Goswami",mode),"historical title "+mode);
 Equal(Core.Target("Sanātana Gosvāmī",mode),c.ConvertCopied("Sanatana Goswami",mode),"historical title Sanatana "+mode);
 Equal(Core.Target("Tamāla Kṛṣṇa Goswami",mode),c.ConvertCopied(Core.Target("Tamāla Kṛṣṇa Gosvāmī",mode),mode),"modern supplied title repaired "+mode);
 Equal("Unlisted Goswami",c.ConvertCopied("Unlisted Goswami",mode),"unknown identity preserves supplied title "+mode);
 Equal(Core.Target("Unlisted Gosvāmī",mode),c.ConvertCopied(Core.Target("Unlisted Gosvāmī",mode),mode),"unknown accented title preserved "+mode);
 foreach(string name in new[]{"Gaura-Nitāi","Tamāla Kṛṣṇa Goswami","Satsvarūpa Dāsa Goswami","Satsvarūpa dāsa Goswami","Bhakti Devī Dāsī","Tamāla Mahārāja","Swami Tamāla","Sārvabhauma Bhaṭṭācārya"})Equal("",Styled(c,Core.Target(name,mode),mode),"personal name context "+name+" "+mode);
 Equal(Core.Target("tamāla|devī|dāsī",mode),Styled(c,Core.Target("a tamāla tree and devī and dāsī",mode),mode),"generic nouns remain eligible "+mode);
 Equal(Core.Target("śāstra",mode),Styled(c,Core.Target("Tamāla Kṛṣṇa Goswami discusses śāstra",mode),mode),"name context does not suppress following prose "+mode);
 Equal(Core.Target("Tamāla",mode),Styled(c,Core.Target("Tamāla. Mahārāja",mode),mode),"name context does not cross sentence punctuation "+mode);
 Equal(Core.Target("devī",mode),c.Word("devi",mode),"devi vocabulary "+mode);Equal(Core.Target("mahārāṇī",mode),c.Word("maharani",mode),"maharani vocabulary "+mode);
 }
 foreach(string input in new[]{"maharani","Maharani"})foreach(string mode in new[]{"unicode","balaram"}){
 string canonical=input[0]=='M'?"Mahārāṇī":"mahārāṇī",wanted=mode=="unicode"?canonical:EncodingMap.Balaram(canonical);
 Equal(wanted,c.Word(input,mode),"retroflex maharani word "+input+" "+mode);
 Equal(wanted,c.ConvertCopied(input,mode),"retroflex maharani copied "+input+" "+mode);
 Equal(wanted,c.ConvertHighlighted(input,mode),"retroflex maharani highlighted "+input+" "+mode);
 Equal(wanted,c.Trigger(input+(mode=="unicode"?">":"<")),"retroflex maharani trigger "+input+" "+mode);
 Equal("True",c.CaretChoices(input,mode).Contains(wanted).ToString(),"retroflex maharani cycle "+input+" "+mode);
 string old=mode=="unicode"?canonical.Replace('ṇ','n'):EncodingMap.Balaram(canonical.Replace('ṇ','n'));
 Equal("False",c.CaretChoices(input,mode).Contains(old).ToString(),"old dental n not offered "+input+" "+mode);
 }
 foreach(string mode in new[]{"unicode","balaram","plain"}){Equal("Brahman",c.Word("Brahman",mode),"Absolute spelling "+mode);Equal("Brahman",c.ConvertCopied("Brahman",mode),"copied Absolute spelling "+mode);Equal("Brahman",c.ConvertHighlighted("Brahman",mode),"highlighted Absolute spelling "+mode);Equal("guru Brahman and śāstra",c.ConvertCopied("guru Brahman and sastra",mode=="balaram"?"unicode":mode=="plain"?"unicode":mode),"Absolute in prose "+mode);}
 string[,] cases={{"Kṛṣṇa and śāstra","śāstra"},{"Advaita Ācārya and an ācārya’s teachings","ācārya’s"},{"Śrī Caitanya-caritāmṛta with Kṛṣṇa","Śrī Caitanya-caritāmṛta"},{"Śrīmad-Bhāgavatam and guru","Śrīmad-Bhāgavatam"},{"karma, akarma and vikarma","karma|akarma|vikarma"},{"The word guru means teacher","guru"},{"The term śāstra is Sanskrit","śāstra"},{"aṣṭāṅga-yoga and yoga","aṣṭāṅga-yoga"},{"Śāstra and Śikhā","Śāstra|Śikhā"},{"Kurukṣetra, Māyāpur and Vṛndāvana",""},{"Gaura-pūrṇimā Janmāṣṭamī Ratha-yātrā",""},{"Bhagavān Brahman Paramātmā Vedānta Vaiṣṇavas Gosvāmīs",""}};
 for(int n=0;n<cases.GetLength(0);n++)foreach(string mode in new[]{"unicode","balaram"})Equal(Core.Target(cases[n,1],mode),Styled(c,Core.Target(cases[n,0],mode),mode),"book style "+n+" "+mode);
 string trigger="BG1.1VT>",output=c.Trigger(trigger);int split=output.IndexOf("\n\n");var spans=c.TriggerItalics(trigger,output,"unicode");Equal("True",spans.Where(s=>s.Start<split).All(s=>s.Force).ToString(),"complete verse forced italics");foreach(string name in new[]{"Dhṛtarāṣṭra","Sañjaya","Pāṇḍu","Kurukṣetra"})Equal("False",spans.Any(s=>s.Start>=split&&output.Substring(s.Start,s.Length)==name).ToString(),"translation proper name stays roman "+name);
 }
 string root=Path.Combine(Path.GetDirectoryName(typeof(BookStyleTests).Assembly.Location),"book-style-fixture");Directory.CreateDirectory(Path.Combine(root,"data"));Directory.CreateDirectory(Configuration.Folder(root));foreach(string file in Directory.GetFiles(Path.Combine(args[0],"data")))if(!File.Exists(Path.Combine(root,"data",Path.GetFileName(file))))File.Copy(file,Path.Combine(root,"data",Path.GetFileName(file)));Configuration.Write(Configuration.Short(root),new Dictionary<string,string>());string path=Path.Combine(Configuration.Folder(root),"italics-exceptions.tsv");File.WriteAllText(path,"raja\tname\njnana\troman\nmarga\titalic\n");using(Core c=new Core(root))Equal("rāja|mārga",Styled(c,"Rāja rāja jñāna mārga","unicode"),"personal name and style exceptions");File.WriteAllText(path,"raja\tbogus\n");try{using(Core c=new Core(root)){}failed++;Console.WriteLine("FAIL invalid style file accepted");}catch(InvalidDataException){passed++;}
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;}
}}
