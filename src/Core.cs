using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace KeyJaya {
public static class Version { public const string Current = "1.9.4"; }
public static class EncodingMap {
    const string Indic = "āīūṛṝḷḹṅñṭḍṇśṣḥṁṃĀĪŪṚṜḶḸṄÑṬḌṆŚṢḤṀṂ";
    const string Legacy = "äéüåèÿýìïöòëçñùààÄÉÜÅÈßÝÌÏÖÒËÇÑÙÀÀ";
    public static string Balaram(string value) {
        StringBuilder b = new StringBuilder(); foreach(char c in value.Normalize(NormalizationForm.FormC)) { int i=Indic.IndexOf(c); b.Append(i<0?c:Legacy[i]); } return b.ToString();
    }
    public static string FromBalaram(string value) {
        StringBuilder b=new StringBuilder(); foreach(char c in value) { int i=Legacy.IndexOf(c); b.Append(i<0?c:Indic[i]); } return b.ToString();
    }
    public static bool IsIndic(string value) { return value.Any(c=>Indic.IndexOf(c)>=0); }
    public static bool UnambiguousIndic(string value){return value.Any(c=>Indic.IndexOf(c)>=0&&Legacy.IndexOf(c)<0);}
    public static bool IsLegacy(string value) { return value.Any(c=>Legacy.IndexOf(c)>=0); }
    public static string Ascii(string value) {
        string n=value.Normalize(NormalizationForm.FormD); StringBuilder b=new StringBuilder();
        foreach(char c in n) if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark) b.Append(c); return b.ToString();
    }
    public static string Case(string original, string output) {
        if(original.All(c=>!char.IsLetter(c)||char.IsUpper(c))) return output.ToUpperInvariant();
        if(original.Length>0 && char.IsUpper(original[0])) return char.ToUpperInvariant(output[0])+output.Substring(1);
        return output;
    }
}
public static class CompoundForms {
 public static readonly Regex Pattern=new Regex(@"^[\p{L}\p{M}]+(?:[-\u2011\u2013][\p{L}\p{M}]+)+(?:['’]s)?$",RegexOptions.Compiled);
 public static readonly Regex Separator=new Regex(@"[-\u2011\u2013]",RegexOptions.Compiled);
 public static readonly string[] Spellings=("Vyāsa-pūjā nīti-śāstra guru-bhakti founder-ācārya bhakti-yoga karma-yoga jñāna-yoga aṣṭāṅga-yoga prema-bhakti śuddha-bhakti vaidhī-bhakti rāgānugā-bhakti kṛṣṇa-prema kṛṣṇa-bhakti kṛṣṇa-kathā hari-kathā hari-nāma nāma-saṅkīrtana hari-nāma-saṅkīrtana nāma-aparādha sādhu-saṅga guru-paramparā guru-dakṣiṇā guru-sevā sac-cid-ānanda sac-cid-ānanda-vigraha mahā-mantra mahā-prasāda mahā-bhāgavata bhagavat-tattva brahma-jñāna ātma-jñāna karma-phala puṇya-karma pāpa-karma varṇāśrama-dharma sanātana-dharma sādhana-bhakti māyā-śakti Gaura-Nitāi Gaura-candra Gaura-hari Gaurāṅga-Mahāprabhu Śacī-nandana Nityānanda-Prabhu vraja-vāsī vraja-gopī gopī-jana gopī-jana-vallabha kṛṣṇa-nāma nāma-kīrtana prema-saṅkīrtana bhakta-gaṇa bhakta-vṛnda vaiṣṇava-gaṇa vaiṣṇava-sevā guru-pādāśraya guru-caraṇa caraṇa-kamala pāda-padma prema-dhana prema-rasa nitya-līlā gaura-līlā kṛṣṇa-līlā Navadvīpa-dhāma Vṛndāvana-dhāma").Split(' ');
 static readonly Dictionary<string,string> aliases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"krishna","krsna"},{"pooja","puja"},{"shuddha","suddha"},{"shakti","sakti"},{"vrindavana","vrndavana"},{"navadwipa","navadvipa"},{"gyana","jnana"}};
 static readonly Dictionary<string,string> forms=Spellings.ToDictionary(Key,x=>x,StringComparer.OrdinalIgnoreCase);
 static readonly HashSet<string> roman=new HashSet<string>(new[]{"vyasa-puja","gaura-nitai","gaura-candra","gaura-hari","gauranga-mahaprabhu","saci-nandana","nityananda-prabhu","navadvipa-dhama","vrndavana-dhama"},StringComparer.OrdinalIgnoreCase);
 public static string Key(string text){return string.Join("-",Separator.Split(EncodingMap.Ascii(text).ToLowerInvariant()).Select(part=>{string alias;return aliases.TryGetValue(part,out alias)?alias:part;}));}
 public static bool IsRoman(string key){return roman.Contains(Key(key));}
 public static bool TryKnown(string input,out string canonical){if(forms.TryGetValue(Key(input),out canonical))return true;return !EncodingMap.UnambiguousIndic(input)&&forms.TryGetValue(Key(EncodingMap.FromBalaram(input)),out canonical);}
 public static string PreserveSeparators(string source,string canonical){string[] parts=Separator.Split(canonical);MatchCollection separators=Separator.Matches(source);if(parts.Length!=separators.Count+1)return canonical;var result=new StringBuilder(parts[0]);for(int n=0;n<separators.Count;n++)result.Append(separators[n].Value).Append(parts[n+1]);return result.ToString();}
}
public sealed class IndexedStore : IDisposable {
    readonly FileStream index, data; readonly BinaryReader reader; readonly object sync=new object();
    readonly Dictionary<string,string> cache=new Dictionary<string,string>(StringComparer.Ordinal);
    readonly ulong[] hashes;
    readonly long recordCount;
    public IndexedStore(string root,string stem,bool keepHashes=false) {
        try {
            index=new FileStream(Path.Combine(root,stem+".idx"),FileMode.Open,FileAccess.Read,FileShare.Read,4096,FileOptions.RandomAccess);
            reader=new BinaryReader(index);
            if(index.Length%20!=0)throw new InvalidDataException("Invalid index: "+stem);
            recordCount=index.Length/20;
            data=new FileStream(Path.Combine(root,stem+".dat"),FileMode.Open,FileAccess.Read,FileShare.Read,4096,FileOptions.RandomAccess);
            if(keepHashes){
                hashes=new ulong[recordCount];byte[] block=new byte[20*1024];
                for(int first=0;first<hashes.Length;){
                    int count=Math.Min(1024,hashes.Length-first),bytes=count*20,read=0;
                    while(read<bytes){int n=index.Read(block,read,bytes-read);if(n==0)throw new EndOfStreamException();read+=n;}
                    // The index is little-endian, as are all supported Windows x64 builds.
                    for(int i=0;i<count;i++)hashes[first+i]=BitConverter.ToUInt64(block,i*20);
                    first+=count;
                }
            }
        } catch {Dispose();throw;}
    }
    public bool MayContain(string key){return hashes==null||Array.BinarySearch(hashes,Hash(key))>=0;}
    public static ulong Hash(string key) {
        if(key==null)throw new ArgumentNullException("key");
        ulong h=14695981039346656037UL;
        foreach(char c in key){
            // Most dictionary probes are ASCII. Keep UTF-8 fallback semantics,
            // including replacement of invalid surrogate sequences, unchanged.
            if(c>127){h=14695981039346656037UL;foreach(byte b in Encoding.UTF8.GetBytes(key))h=unchecked((h^b)*1099511628211UL);return h;}
            h=unchecked((h^c)*1099511628211UL);
        }
        return h;
    }
    public string Get(string key) {
        lock(sync) {
            string cached; if(cache.TryGetValue(key,out cached))return cached;
            ulong h=Hash(key); long low=0,high=recordCount;
            while(low<high) { long mid=(low+high)/2;ulong x;if(hashes!=null)x=hashes[mid];else{index.Position=mid*20;x=reader.ReadUInt64();}if(x<h)low=mid+1;else high=mid; }
            if(hashes!=null&&(low==recordCount||hashes[low]!=h))return null;
            string result=null;
            for(long j=low;j<recordCount;j++) {
                index.Position=j*20; if(reader.ReadUInt64()!=h)break;
                long offset=reader.ReadInt64(); int length=reader.ReadInt32();
                if(offset<0||length<1||length>4000000||offset>data.Length-length)throw new InvalidDataException("Invalid record");
                byte[] record=new byte[length]; data.Position=offset; int done=0;
                while(done<length) { int n=data.Read(record,done,length-done); if(n==0)throw new EndOfStreamException(); done+=n; }
                int split=Array.IndexOf(record,(byte)0);
                if(split>=0 && Encoding.UTF8.GetString(record,0,split)==key) { result=Encoding.UTF8.GetString(record,split+1,length-split-1); break; }
            }
            if(cache.Count>=512)cache.Clear(); cache[key]=result; return result;
        }
    }
    public void Dispose() {if(reader!=null)reader.Dispose();else if(index!=null)index.Dispose();if(data!=null)data.Dispose();}
}
public sealed partial class Core : IDisposable {
    public readonly CorrectionSettings AutomaticCorrections;
    readonly ulong[] contractionHashes;
    public readonly Dictionary<string,string> Short=new Dictionary<string,string>(StringComparer.Ordinal);
    readonly HashSet<string> exactShortKeys=new HashSet<string>(StringComparer.Ordinal);
    readonly Dictionary<string,string> snippets=new Dictionary<string,string>(StringComparer.Ordinal);
    readonly Dictionary<string,string> italicOverrides=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> bbtRomanNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,string> wordSpellings=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> neverConvert=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,string> personal=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,string> aliases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    readonly List<KeyValuePair<string,string>> rules=new List<KeyValuePair<string,string>>();
    readonly Dictionary<string,string> preferred=new Dictionary<string,string>(StringComparer.Ordinal);readonly Dictionary<string,string> votes=new Dictionary<string,string>(StringComparer.Ordinal);readonly Dictionary<string,int> voteCounts=new Dictionary<string,int>(StringComparer.Ordinal);readonly object preferenceSync=new object();readonly string preferencePath;System.Threading.Timer preferenceTimer;bool preferencesDirty;string pendingKey,pendingSpelling,pendingOccurrence;readonly HashSet<string> votedOccurrences=new HashSet<string>();readonly Queue<string> occurrenceOrder=new Queue<string>();
    readonly HashSet<string> attestedCompounds=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly ulong[] guard; readonly IndexedStore lexicon, scripture, cycles, corrections;readonly Lazy<IndexedStore> hindiDictionary,sanskritMorphology;
    public static readonly HashSet<string> IndicLoanwords=new HashSet<string>(new[]{"ahimsa","atman","chandra","guru","chatra","jagannath","hanuman","ayurveda","sakti","shakti","varuna","sruti","maharani","devi","siva","shiva","varna","puja","mathura","brahmin","brahmins","pandit","pundit","pooja","poornima","shreela","shree","raj","niti","saiva"},StringComparer.OrdinalIgnoreCase);
    // Recognized Indic terms may use explicit suffix conversion; automatic English prose remains guarded.
    public static readonly HashSet<string> ExplicitIndicTriggers=new HashSet<string>(new[]{"prana","kshatriya","brahmani","godavari","varanasi","dharana","lakshmi","mandala","mantra","mantras","mathura","saraswati","sarasvati","shiva","siva","upanishad","upanisad","purana","yoga","yogi","asana","pranayama","tantra","dharma","karma","samsara","samadhi","nirvana","moksha","bhakti","vedanta","veda","vedas","avatar","avatara","brahma","vishnu","visnu","rama","ramayana","mahabharata","gita","ganesha","ganesa","ganesh","kali","sannyasi","sannyasin","brahman","brahmana","pandit","pundit","kirtan","kirtana","puja","pujari","acharya","acarya","vaishnava","vaisnava","shastra","sastra","shudra","sudra","rishi","rsi","hari","radha","vrindavan","vrindavana","caitanya","chaitanya","purusha","sankara","shankara","sankhya","narmada","parvati","lingam","nagari","prajna","sadhu","sadhus","shakta","varuna","kaveri","yamuna","gatha","sakha","surya","varna","yogis","kama","sita","sati","agama","asoka","brahmi","devi","dhyana","durga","ganesa","raj","niti","saiva","himalaya","himalayas","kalidasa","kamala","lanka","mahatma","mahatmas","mahavira","mudra","nyaya","om","raga","samhita","siddhartha","sruti","tripura","yogini","annapurna","dhoti","dhotis","jataka","karttikeya","mimamsa","nyasa","ramachandra","upanishads","vaisya"},StringComparer.OrdinalIgnoreCase);
    static readonly Regex Words=new Regex(@"[\p{L}\p{M}]+(?:['’]s)?",RegexOptions.Compiled);
    static readonly Regex Compounds=new Regex(@"[\p{L}\p{M}]+(?:[-\u2011\u2013'’][\p{L}\p{M}]+)*",RegexOptions.Compiled);
    static readonly Regex Protected=new Regex(@"/\*[\s\S]*?\*/|(?:https?://|www\.)[^\s]+|[\w.+-]+@[\w.-]+\.[\w-]+|[A-Za-z]:[\\/][^\s]+|\\\\[^\s]+",RegexOptions.Compiled|RegexOptions.IgnoreCase);
    static readonly Regex ScripturePattern=new Regex(@"^(BG|SB|CC|CB|ISO|BS|NOI)(\d+(?:\.\d+){0,2}|INV)(VT|V|T)?(R)?([><?])$",RegexOptions.IgnoreCase|RegexOptions.Compiled);
    public Core(string root,string spellingPreferencePath=null) {
        try {
        AutomaticCorrections=new CorrectionSettings(Path.Combine(Configuration.Folder(root),"settings.ini"));
        using(BinaryReader b=new BinaryReader(File.OpenRead(Path.Combine(root,"data","contractions.hash")))){contractionHashes=new ulong[b.BaseStream.Length/8];for(int i=0;i<contractionHashes.Length;i++)contractionHashes[i]=b.ReadUInt64();}
        if(Directory.Exists(Configuration.Folder(root))&&!File.Exists(Configuration.Personal(root)))throw new InvalidDataException("Missing short-triggers.yml (or legacy personal-triggers.yml) in the configuration folder.");
        ScriptOptions=new ScriptSettings(Path.Combine(Configuration.Folder(root),"settings.ini"));ReadScriptSpellings(Path.Combine(Configuration.Folder(root),"script-spellings.tsv"));ReadScriptPhrases(Path.Combine(Configuration.Folder(root),"script-phrases.tsv"));ReadWordSpellings(Path.Combine(Configuration.Folder(root),"word-spellings.tsv"));
        ReadItalicOverrides(Path.Combine(Configuration.Folder(root),"italics-exceptions.tsv"));
        var configuredSnippets=Directory.Exists(Configuration.Folder(root))?Configuration.Read(Configuration.Personal(root),false,false,exactShortKeys):new Dictionary<string,string>();
        string data=Path.Combine(root,"data");string nameTable=Path.Combine(data,"bbt-roman-names.tsv");if(File.Exists(nameTable))foreach(string row in File.ReadLines(nameTable)){if(row.StartsWith("#")||row.Trim()=="")continue;if(!Regex.IsMatch(row,@"^[a-z]+(?:-[a-z]+)*$")||row.Length>80||bbtRomanNames.Count>=10000)throw new InvalidDataException("Invalid BBT name metadata");bbtRomanNames.Add(row);}
        hindiDictionary=new Lazy<IndexedStore>(()=>File.Exists(Path.Combine(data,"hindi.idx"))?new IndexedStore(data,"hindi"):null);sanskritMorphology=new Lazy<IndexedStore>(()=>File.Exists(Path.Combine(data,"sanskrit-morphology.idx"))?new IndexedStore(data,"sanskrit-morphology"):null);verseNormalization=new Lazy<IndexedStore>(()=>File.Exists(Path.Combine(data,"verse-normalization.idx"))?new IndexedStore(data,"verse-normalization"):null); lexicon=new IndexedStore(data,"conversion"); scripture=new IndexedStore(data,"scripture");cycles=new IndexedStore(data,"cycle");corrections=new IndexedStore(data,"english-corrections",true);
        using(BinaryReader b=new BinaryReader(File.OpenRead(Path.Combine(data,"english.hash")))) { guard=new ulong[b.BaseStream.Length/8]; for(int i=0;i<guard.Length;i++)guard[i]=b.ReadUInt64(); }
        string compoundTable=Path.Combine(data,"book-compounds.tsv");if(File.Exists(compoundTable))foreach(string line in File.ReadLines(compoundTable)){if(line.StartsWith("#")||line.Trim()=="")continue;if(line.Length>160||!CompoundForms.Pattern.IsMatch(line)||line.Any(c=>c!='-'&&(c<'a'||c>'z')))throw new InvalidDataException("Invalid book compound metadata");attestedCompounds.Add(line);}
        preferencePath=spellingPreferencePath??Path.Combine(root,Directory.Exists(Configuration.Folder(root))?"state":"personal","preferred-spellings.tsv");
        if(File.Exists(preferencePath))foreach(string line in File.ReadLines(preferencePath)){string[] cells=line.Split('\t');int count;if(cells.Length==2&&Regex.IsMatch(cells[0],@"^[a-z]+$")&&Regex.IsMatch(cells[1],@"^[\p{L}\p{M}]+$")&&(!English(cells[0])||IndicLoanwords.Contains(cells[0])||ExplicitIndicTriggers.Contains(cells[0])))preferred[cells[0]]=cells[1];else if(cells.Length==4&&cells[0]=="#vote"&&Regex.IsMatch(cells[1],@"^[a-z]+$")&&Regex.IsMatch(cells[2],@"^[\p{L}\p{M}]+$")&&int.TryParse(cells[3],out count)&&count>=1&&count<=3){votes[cells[1]]=cells[2];voteCounts[cells[1]]=count;}}
        if(File.Exists(Configuration.Short(root)))Short=snippets;else foreach(string line in File.ReadLines(Path.Combine(data,"short-triggers.tsv"))) { string[] c=line.Split('\t');if(c.Length>=2 && !line.StartsWith("#"))Short[Configuration.PlainTriggerKey(c[0])]=c[1]; }
        foreach(string line in File.ReadLines(Path.Combine(data,"common-aliases.tsv"))) { string[] c=line.Split('\t'); if(c.Length>=3&&!line.StartsWith("#"))aliases[c[0]]=c[2]; }
        if(Directory.Exists(Configuration.Folder(root))){ReadOverrides(Path.Combine(Configuration.Folder(root),"diacritics-overrides.tsv"));ReadOverrides(Path.Combine(Configuration.Folder(root),"overrides.tsv"));ReadRules(Path.Combine(Configuration.Folder(root),"conversion-rules.tsv"));foreach(var row in configuredSnippets)snippets[row.Key]=row.Value;}else{
        ReadOverrides(Path.Combine(root,"diacritics-overrides.tsv")); ReadOverrides(Path.Combine(root,"personal","diacritics-overrides.tsv"));
        ReadOverrides(Path.Combine(root,"overrides.tsv"));ReadOverrides(Path.Combine(root,"personal","overrides.tsv"));
        ReadRules(Path.Combine(root,"conversion-rules.tsv"));ReadRules(Path.Combine(root,"personal","conversion-rules.tsv"));
        // Personal static snippets are prepared by the installer, with exact personal values taking precedence.
        string snippet=Path.Combine(root,"personal","short-triggers.tsv");
        if(File.Exists(snippet))foreach(string line in File.ReadLines(snippet)) { string[] c=line.Split('\t');if(c.Length==2&&!line.StartsWith("#"))Short[Configuration.PlainTriggerKey(c[0])]=c[1]; }
        }
        foreach(var spelling in wordSpellings)personal[spelling.Key]=spelling.Value;
        } catch {
            // A failed configuration reload must release any dictionaries it
            // already opened; the existing live Core remains available.
            DisposeStores();throw;
        }
    }
    static readonly Regex CtrlShortPattern=new Regex(@"^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$",RegexOptions.Compiled|RegexOptions.CultureInvariant);
    public static bool CtrlOnlyShort(string trigger){return trigger!=null&&CtrlShortPattern.IsMatch(trigger);}
    public bool HasShortAbbreviation(string token){return token!=null&&(Short.ContainsKey(token)||Short.ContainsKey(token+">")||Short.ContainsKey(token+"<")||Short.ContainsKey(token+"?"));}
    public string CtrlShort(string token,string mode){string exact=CtrlShortExact(token,mode);if(exact!=null)return exact;if(token==null||!CompoundForms.Pattern.IsMatch(token))return null;bool found=false;string result=Regex.Replace(token,@"[\p{L}\p{M}\p{N}]+",m=>{string value=CtrlShortExact(m.Value,mode);if(value==null)return Word(m.Value,mode);found=true;return value;});return found?result:null;}
    string CtrlShortExact(string token,string mode){
        if(!CtrlOnlyShort(token)||mode!="unicode"&&mode!="balaram"&&mode!="plain")return null;
        string value;
        // Exact configured output wins. Do not infer dictionary spellings inside it.
        string suffix=mode=="balaram"?"<":mode=="plain"?"?":">";
        if(Short.TryGetValue(token+suffix,out value))return exactShortKeys.Contains(token+suffix)?Configuration.Expand(value,DateTime.Now):RomanNameExceptions(Configuration.Expand(value,DateTime.Now));
        if(Short.TryGetValue(token,out value))return exactShortKeys.Contains(token)?Configuration.Expand(value,DateTime.Now):Target(Configuration.Expand(value,DateTime.Now),mode);
        // Missing encoding variants remain disabled; deletion in YAML is authoritative.
        return null;
    }
    public bool IsDirectTrigger(string trigger){return trigger!=null&&!CtrlOnlyShort(trigger)&&(snippets.ContainsKey(trigger)||Short.ContainsKey(trigger));}
    public Dictionary<string,string> ActiveShortTriggers(){var result=new Dictionary<string,string>(Short,StringComparer.Ordinal);foreach(var row in snippets)result[row.Key]=row.Value;return result;}
    public static Core Reload(Core current,string root){if(!File.Exists(Configuration.Personal(root)))throw new InvalidDataException("Missing trigger YAML; the current configuration remains active.");Configuration.Read(Configuration.Personal(root));current.FinishSpellingChoice();Core replacement=new Core(root);current.Dispose();return replacement;}
    void ReadWordSpellings(string path){if(!File.Exists(path))return;var keys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);int row=0;foreach(string line in File.ReadLines(path)){row++;if(string.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#"))continue;string[] cells=line.Split('\t');if(cells.Length!=2||!Regex.IsMatch(cells[0],@"^[\p{L}\p{M}]+(?:[-\u2011\u2013'’][\p{L}\p{M}]+)*$")||!keys.Add(cells[0])||cells[1]!="!keep"&&!Regex.IsMatch(cells[1],@"^[\p{L}\p{M}]+(?:[-\u2011\u2013'’][\p{L}\p{M}]+)*$"))throw new InvalidDataException("Invalid or duplicate word-spellings.tsv row "+row);if(cells[1]=="!keep")neverConvert.Add(cells[0]);else wordSpellings[cells[0]]=cells[1].Normalize(NormalizationForm.FormC);}}
    string WithKeptWords(string input,Func<string,string> convert){if(neverConvert.Count==0)return convert(input);var output=new StringBuilder();int at=0;foreach(Match word in Compounds.Matches(input)){if(!KeepWord(word.Value))continue;output.Append(convert(input.Substring(at,word.Index-at)));output.Append(word.Value);at=word.Index+word.Length;}output.Append(convert(input.Substring(at)));return output.ToString();}
    bool KeepWord(string word){if(neverConvert.Contains(word))return true;if(word.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||word.EndsWith("’s",StringComparison.OrdinalIgnoreCase))return neverConvert.Contains(word.Substring(0,word.Length-2));return false;}
    void ReadOverrides(string path) { if(!File.Exists(path))return;foreach(string line in File.ReadLines(path)){string[] c=line.Split('\t');if(c.Length>=2&&!line.StartsWith("#")&&c[0].Length>0)personal[c[0]]=c[1];} }
    void ReadRules(string path) { if(!File.Exists(path))return;foreach(string line in File.ReadLines(path)){string[] c=line.Split('\t');if(c.Length>=2&&!line.StartsWith("#")&&c[0].Length>0)rules.Add(new KeyValuePair<string,string>(c[0],c[1]));} }
    public bool English(string word) { return Array.BinarySearch(guard,IndexedStore.Hash(word.ToLowerInvariant()))>=0; }
    static readonly HashSet<string> AmbiguousContractions=new HashSet<string>("well were ill hell shell its wed shed".Split(' '),StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string,string> ExtraContractions=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"lets","let’s"},{"wont","won’t"},{"id","I’d"}};
    string CorrectionValue(string word){if(AmbiguousContractions.Contains(word))return null;string value;return ExtraContractions.TryGetValue(word,out value)?value:corrections.Get(word);}
    bool HasCorrection(string word){if(AmbiguousContractions.Contains(word))return false;return ExtraContractions.ContainsKey(word)||corrections.MayContain(word);}
    public bool MayCorrectCompletedWord(string input){
        if(string.IsNullOrEmpty(input))return false;
        if(input.Trim(' ','\t','\r','\n','(',')','[',']','"','“','”',',','.',';',':','!','?')=="ID")return false;
        if(!AutomaticCorrections.Spelling&&!AutomaticCorrections.Contractions)return false;
        string raw=input.TrimEnd(' ','\t','\r','\n').ToLowerInvariant().Replace('’','\'');
        string direct=raw.TrimEnd(',', '.', ';', ':', '!', '?');if(HasCorrection(direct)&&CorrectionEnabled(direct))return true;
        string wrapped=raw.Trim('(', '"', '\'', '“', '‘', '[', ')', '”', ']', ',', '.', ';', ':', '!', '?');return HasCorrection(wrapped)&&CorrectionEnabled(wrapped);
    }
    bool CorrectionEnabled(string word){string key=word.ToLowerInvariant().Replace('’','\'');bool contraction=ExtraContractions.ContainsKey(key)||Array.BinarySearch(contractionHashes,IndexedStore.Hash(key))>=0;return contraction?AutomaticCorrections.Contractions:AutomaticCorrections.Spelling;}
    public static bool CompletionDelimiter(char c){return c==' '||c=='\t'||c=='\r'||c=='\n'||",.;:!?".IndexOf(c)>=0;}
    public string CorrectCompletedWord(string input){
        if(input==null||input.Length<2||!CompletionDelimiter(input[input.Length-1]))return null;
        string raw=input.Substring(0,input.Length-1),whole=raw.TrimEnd(',', '.', ';', ':', '!', '?'),direct;if(KeepWord(whole)||whole=="ID")return null;if(CorrectionEnabled(whole)&&Regex.IsMatch(whole,@"^['’]?[\p{L}\p{M}]+(?:[-\u2011\u2013'’][\p{L}\p{M}]+)*['’]?$")&&(direct=CorrectionValue(whole.ToLowerInvariant().Replace('’','\'')))!=null){string exact=EncodingMap.Case(whole,direct)+raw.Substring(whole.Length)+input[input.Length-1];return exact==input?null:exact;}
        Match m=Regex.Match(input,@"^([(""'“‘\[]*)([\p{L}\p{M}]+(?:[-\u2011\u2013'’][\p{L}\p{M}]+)*)([)""'”’\],.;:!?]*)([ \t\r\n,.;:!?])$");if(!m.Success)return null;
        string word=m.Groups[2].Value;if(KeepWord(word)||word=="ID")return null;string value=CorrectionValue(word.ToLowerInvariant().Replace('’','\''));if(value==null||!CorrectionEnabled(word))return null;
        string result=m.Groups[1].Value+EncodingMap.Case(word,value)+m.Groups[3].Value+m.Groups[4].Value;return result==input?null:result;
    }
    string MorphologyForms(string key){var store=sanskritMorphology==null?null:sanskritMorphology.Value;return store==null?null:store.Get(key.ToLowerInvariant());}
    string BookWord(string word){string x;if(Vocabulary.Terms.TryGetValue(word,out x))return x;if(aliases.TryGetValue(word,out x))return x;return lexicon.Get(word.ToLowerInvariant());}
    string ExactWord(string word){string x=BookWord(word);if(x!=null)return x;string forms=MorphologyForms(word);return forms==null?null:forms.Split('\x1f')[0];}
    // A grammatical possibility alone is weak evidence for an unfamiliar name
    // in English prose. Explicit single-word actions still expose those forms.
    static bool SingleProseWord(string text){return Words.Matches(text).Count<=1;}
    string ProseWord(string word,string mode,bool singleWord){if(!singleWord&&word.Length>1&&word[0]>='A'&&word[0]<='Z'&&word.Skip(1).All(c=>c>='a'&&c<='z')&&!personal.ContainsKey(word)&&BookWord(word)==null)return word;return Word(word,mode);}
    string PluralStem(string word){if(word.Length<4||!word.EndsWith("s",StringComparison.OrdinalIgnoreCase))return null;string stem=word.Substring(0,word.Length-1);if(English(stem)&&!IndicLoanwords.Contains(stem))return null;string canonical=ExactWord(stem);return canonical!=null&&"aāiīuūṛṝ".IndexOf(char.ToLowerInvariant(canonical[canonical.Length-1]))>=0?stem:null;}
    string Find(string word) { string x=ExactWord(word);if(x==null)x=LooseWord(word);if(x==null){string stem=PluralStem(word);if(stem!=null)x=ExactWord(stem)+"s";}return x==null?null:Preferred(x); }
    string Preferred(string word){string value;lock(preferenceSync)return preferred.TryGetValue(EncodingMap.Ascii(word).ToLowerInvariant(),out value)?EncodingMap.Case(word,value):word;}
    public bool RememberSpelling(string input,string output,string mode,string source,string occurrence=null){
        string unicode=output;if(mode=="balaram")unicode=EncodingMap.FromBalaram(output);else if(mode=="itrans"){if(!Itrans.TryWord(output,out unicode))return false;}else if(mode=="plain")return false;
        bool plainChoice=unicode==EncodingMap.Ascii(unicode);
        if(!ClipboardChoices(input,mode,source).Contains(output)&&!CaretChoices(input,mode).Contains(output)){if(plainChoice)CancelSpellingChoice();return false;}
        if(!Regex.IsMatch(unicode,@"^[\p{L}\p{M}]+$"))return false;
        string key=EncodingMap.Ascii(unicode).ToLowerInvariant();if(plainChoice&&!RecognizedIndic(input,source=="balaram"?"balaram":"unicode"))return false;
        if(English(key)&&!IndicLoanwords.Contains(key)&&!ExplicitIndicTriggers.Contains(key))return false;
        lock(preferenceSync){if(pendingKey!=null&&pendingKey!=key)FinishSpellingChoice();pendingKey=key;pendingSpelling=unicode.ToLowerInvariant();pendingOccurrence=occurrence;if(preferenceTimer==null)preferenceTimer=new System.Threading.Timer(x=>FinishSpellingChoice(),null,System.Threading.Timeout.Infinite,System.Threading.Timeout.Infinite);preferenceTimer.Change(10000,System.Threading.Timeout.Infinite);}return true;
    }
    public void CancelSpellingChoice(){lock(preferenceSync){pendingKey=pendingSpelling=pendingOccurrence=null;if(preferenceTimer!=null)preferenceTimer.Change(System.Threading.Timeout.Infinite,System.Threading.Timeout.Infinite);}}
    public void FinishOtherOccurrence(string occurrence){lock(preferenceSync){if(occurrence==null||pendingOccurrence!=occurrence)FinishSpellingChoice();}}
    public void FinishSpellingChoice(){lock(preferenceSync){if(pendingKey!=null){string occurrence=pendingOccurrence==null?null:pendingKey+":"+pendingOccurrence;if(occurrence!=null&&!votedOccurrences.Add(occurrence)){pendingKey=pendingSpelling=pendingOccurrence=null;SavePreferences();return;}if(occurrence!=null){occurrenceOrder.Enqueue(occurrence);if(occurrenceOrder.Count>256)votedOccurrences.Remove(occurrenceOrder.Dequeue());}string old;int count=votes.TryGetValue(pendingKey,out old)&&old==pendingSpelling?Math.Min(3,voteCounts[pendingKey]+1):1;votes[pendingKey]=pendingSpelling;voteCounts[pendingKey]=count;if(count>=3)preferred[pendingKey]=pendingSpelling;pendingKey=pendingSpelling=pendingOccurrence=null;preferencesDirty=true;}SavePreferences();}}
    void SavePreferences(){lock(preferenceSync){if(!preferencesDirty)return;try{Directory.CreateDirectory(Path.GetDirectoryName(preferencePath));string temporary=preferencePath+".tmp";File.WriteAllLines(temporary,preferred.OrderBy(x=>x.Key).Select(x=>x.Key+"\t"+x.Value).Concat(votes.OrderBy(x=>x.Key).Select(x=>"#vote\t"+x.Key+"\t"+x.Value+"\t"+voteCounts[x.Key])),new UTF8Encoding(false));SettingsFile.Commit(temporary,preferencePath);preferencesDirty=false;}catch(IOException){}catch(UnauthorizedAccessException){}}}
    string HindiLookup(string word){if(hindiDictionary.Value==null)return null;string value=hindiDictionary.Value.Get(Hindi.Alias(word));if(value==null)return null;string[] choices=value.Split('\x1f');string normalized=word.Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace('ṃ','ṁ');foreach(string candidate in choices)if(Hindi.ToIast(candidate).Replace('ṃ','ṁ')==normalized||IndicScripts.ToIast(candidate).Replace('ṃ','ṁ')==normalized)return candidate;return choices[0];}
    bool KnownSanskritScript(string word){string latin=IndicScripts.ToIast(word).Replace('ṃ','ṁ');string known=Find(EncodingMap.Ascii(latin));if(known!=null&&known.Replace('ṃ','ṁ').Equals(latin,StringComparison.OrdinalIgnoreCase))return true;string alternatives=cycles.Get(EncodingMap.Ascii(latin).ToLowerInvariant());return alternatives!=null&&alternatives.Split('\0')[0].Split('\x1f').Any(x=>x.Replace('ṃ','ṁ').Equals(latin,StringComparison.OrdinalIgnoreCase));}
    bool KnownHindiScript(string word){if(Hindi.Common.Values.Contains(word))return true;if(hindiDictionary.Value==null)return false;foreach(string latin in new[]{IndicScripts.ToIast(word),Hindi.ToIast(word)}){string values=hindiDictionary.Value.Get(Hindi.Alias(latin));if(values!=null&&values.Split('\x1f').Any(x=>x.Normalize(NormalizationForm.FormC)==word.Normalize(NormalizationForm.FormC)))return true;}return false;}
    string LooseWord(string word){if(!Regex.IsMatch(word,@"^[A-Za-z]+$"))return null;string key=Hindi.Alias(word);if(key==word.ToLowerInvariant())return null;string value=ExactWord(key);if(value!=null)return value;return null;}
    bool KnownUnicode(string word){if(!EncodingMap.IsIndic(word))return false;string key=EncodingMap.Ascii(word).ToLowerInvariant();if(string.Equals(Find(key),word,StringComparison.OrdinalIgnoreCase))return true;string variants=cycles.Get(key);return variants!=null&&variants.Split('\0')[0].Split('\x1f').Any(x=>string.Equals(x,word,StringComparison.OrdinalIgnoreCase));}
    public string Word(string word,string mode) {
        if(KeepWord(word))return word;
        string custom;if(personal.TryGetValue(word,out custom))return Target(EncodingMap.Case(word,custom),mode);
        string compound;if(TryCompound(word,mode,out compound))return compound;
        string titleKey=word.ToLowerInvariant();if(titleKey=="gosvami"||titleKey=="gosvamipada")return Target(titleKey=="gosvami"?"Gosvāmī":"Gosvāmīpāda",mode);
        // Capitalized Brahman denotes the Absolute, not the priest brāhmaṇa.
        if(word=="Brahman"||word=="Brahman's"||word=="Brahman’s")return Target(word,mode);
        string baseWord=word,possessive="";
        if(word.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||word.EndsWith("’s",StringComparison.OrdinalIgnoreCase)){possessive=word.Substring(word.Length-2);baseWord=word.Substring(0,word.Length-2);}
        if(English(baseWord)&&!IndicLoanwords.Contains(baseWord))return word;
        string normalized=baseWord.Normalize(NormalizationForm.FormC),unicode;
        // A lone legacy-looking accent is also an ordinary foreign word/letter
        // (notably French à). Automatic word lookup must not infer its encoding.
        // Explicit Balarama and single-letter conversions already know the source.
        if(normalized.Length==1&&EncodingMap.IsLegacy(normalized)&&!EncodingMap.UnambiguousIndic(normalized)&&!KnownUnicode(normalized))return word;
        string decoded=EncodingMap.FromBalaram(normalized);
        if(EncodingMap.UnambiguousIndic(normalized))unicode=normalized;
        else if(KnownUnicode(normalized))unicode=normalized;
        else if(EncodingMap.IsLegacy(normalized)&&decoded!=normalized&&Find(EncodingMap.Ascii(decoded))!=null)unicode=decoded;
        else if(EncodingMap.IsIndic(normalized))unicode=normalized;
        else if(EncodingMap.IsLegacy(normalized))return word;
        else { unicode=Find(baseWord);if(unicode==null)return word;unicode=EncodingMap.Case(baseWord,unicode); }
        return Target(Preferred(unicode),mode)+possessive;
    }
    public static string RomanNameExceptions(string text){
        if(text==null||text.IndexOf("bhaktived",StringComparison.OrdinalIgnoreCase)<0)return text;
        var output=new StringBuilder();int at=0;foreach(Match literal in Protected.Matches(text)){output.Append(RomanNamePart(text.Substring(at,literal.Index-at)));output.Append(literal.Value);at=literal.Index+literal.Length;}output.Append(RomanNamePart(text.Substring(at)));return output.ToString();
    }
    static string RomanNamePart(string text){return Words.Replace(text,m=>{string key=EncodingMap.Ascii(EncodingMap.FromBalaram(m.Value)).ToLowerInvariant(),possessive="";if(key.EndsWith("'s",StringComparison.Ordinal)||key.EndsWith("’s",StringComparison.Ordinal)){possessive=m.Value.Substring(m.Value.Length-2);key=key.Substring(0,key.Length-2);}return key=="bhaktivedanta"||key=="bhaktivedantas"?EncodingMap.Case(m.Value,key)+possessive:m.Value;});}
    static string RomanWordSpelling(string word){if(word==null)return null;string stem=word,possessive="";if(stem.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||stem.EndsWith("’s",StringComparison.OrdinalIgnoreCase)){possessive=stem.Substring(stem.Length-2);stem=stem.Substring(0,stem.Length-2);}if(EncodingMap.Ascii(stem).Equals("maharani",StringComparison.OrdinalIgnoreCase))return EncodingMap.Case(stem,"mahārāṇī")+possessive;if(stem.Equals("mathura",StringComparison.OrdinalIgnoreCase)||stem.Equals("mathurā",StringComparison.OrdinalIgnoreCase))return (stem.All(c=>!char.IsLetter(c)||char.IsUpper(c))?"MATHURĀ":"Mathurā")+possessive;return word;}
    public static string Target(string unicode,string mode) { if(mode=="hindi")mode="devanagari";if(mode!="devanagari"&&mode!="bengali")unicode=RomanNameExceptions(unicode);if(mode=="unicode"||mode=="balaram"||mode=="plain")unicode=RomanWordSpelling(unicode);return mode=="devanagari"||mode=="bengali"?IndicScripts.FromIast(unicode,mode):mode=="itrans"?Itrans.Encode(unicode):mode=="balaram"?EncodingMap.Balaram(unicode):mode=="plain"?EncodingMap.Ascii(unicode):unicode.Normalize(NormalizationForm.FormC); }
    public string Convert(string input,string mode) {
        StringBuilder result=new StringBuilder();int position=0;
        foreach(Match m in Protected.Matches(input)) { result.Append(ConvertSegment(input.Substring(position,m.Index-position),mode));result.Append(m.Value);position=m.Index+m.Length; }
        result.Append(ConvertSegment(input.Substring(position),mode));
        string value=result.ToString();return WithKeptWords(value,text=>{foreach(var rule in rules)text=text.Replace(rule.Key,rule.Value);return text;});
    }
    public string ConvertItrans(string input,string mode){
        StringBuilder result=new StringBuilder();int position=0;
        foreach(Match m in Protected.Matches(input)){result.Append(WithKeptWords(input.Substring(position,m.Index-position),text=>NormalizeItrans(text,mode)));result.Append(m.Value);position=m.Index+m.Length;}
        result.Append(WithKeptWords(input.Substring(position),text=>NormalizeItrans(text,mode)));return result.ToString();
    }
    public string ConvertBalaram(string input,string mode){
        StringBuilder result=new StringBuilder();int position=0;
        foreach(Match m in Protected.Matches(input)){result.Append(WithKeptWords(input.Substring(position,m.Index-position),text=>BalaramSegment(text,mode)));result.Append(m.Value);position=m.Index+m.Length;}
        result.Append(WithKeptWords(input.Substring(position),text=>BalaramSegment(text,mode)));return result.ToString();
    }
    string BalaramSegment(string input,string mode){return Compounds.Replace(input,m=>{string word=m.Value,compound;if(TryCompound(word,mode,out compound))return compound;string stem=word;if(stem.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||stem.EndsWith("’s",StringComparison.OrdinalIgnoreCase))stem=stem.Substring(0,stem.Length-2);if(English(stem)||English(stem.Replace("'","").Replace("’","")))return word;return Target(EncodingMap.FromBalaram(word),mode);});}
    public string[] ClipboardChoices(string input,string mode,string source){if(mode=="hindi")return new string[0];
        if(KeepWord(input))return new string[0];if(CompoundForms.Pattern.IsMatch(input))return CompoundChoices(input,mode);
        if(source=="auto"&&English(input)&&!IndicLoanwords.Contains(input)&&!personal.ContainsKey(input))return new string[0];
        string word=input,decoded;
        string stem=input;if(stem.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||stem.EndsWith("’s",StringComparison.OrdinalIgnoreCase))stem=stem.Substring(0,stem.Length-2);
        if(source=="auto"&&English(stem)&&!IndicLoanwords.Contains(stem)&&!personal.ContainsKey(input)&&!personal.ContainsKey(stem))return new string[0];
        if(source=="itrans"){if(!Itrans.TryWord(input,out decoded))return new string[0];word=decoded;}
        else if(source=="balaram")word=EncodingMap.FromBalaram(input);
        else if(source=="auto"){if(IndicScripts.Contains(input))word=DecodeBengali(input);else if(DecodeItrans(input,out decoded))word=decoded;}
        if(!Regex.IsMatch(word,@"^[\p{L}\p{M}]+(?:['’]s)?$"))return new string[0];
        bool bookNasal=source=="auto"&&word.IndexOf('ṁ')>=0;
        return Cycle(word,'>').Select(x=>Target(bookNasal?x.Replace('ṃ','ṁ'):x,mode)).Distinct().ToArray();
    }
    bool KnownCompound(string input,out string canonical){
        if(CompoundForms.TryKnown(input,out canonical))return true;
        string key=CompoundForms.Key(input),rawKey=EncodingMap.Ascii(input).ToLowerInvariant().Replace('\u2011','-').Replace('\u2013','-');
        int parts=CompoundForms.Separator.Split(input).Length;
        canonical=ExactWord(key)??ExactWord(rawKey);
        if(canonical==null&&!EncodingMap.UnambiguousIndic(input))canonical=ExactWord(CompoundForms.Key(EncodingMap.FromBalaram(input)));
        string recorded=null;
        // Older entries sometimes replace a compound's hyphens with spaces.
        // Prefer an attested whole-compound form rather than changing separators.
        if(canonical==null||!CompoundForms.Pattern.IsMatch(canonical)||CompoundForms.Separator.Split(canonical).Length!=parts){
            recorded=cycles.Get(rawKey)??cycles.Get(key);
            canonical=recorded==null?null:recorded.Split('\0')[0].Split('\x1f').FirstOrDefault(form=>CompoundForms.Pattern.IsMatch(form)&&CompoundForms.Separator.Split(form).Length==parts);
        }
        if(canonical==null)return false;
        if(!attestedCompounds.Contains(key)&&!attestedCompounds.Contains(rawKey))foreach(string part in CompoundForms.Separator.Split(input))if(English(part)&&!IndicLoanwords.Contains(part)&&!ExplicitIndicTriggers.Contains(part))return false;
        if(!EncodingMap.IsIndic(canonical)){
            recorded=recorded??cycles.Get(rawKey)??cycles.Get(key);
            if(recorded==null||!EncodingMap.IsIndic(recorded.Split('\0')[0]))return false;
        }
        canonical=EncodingMap.Case(input,canonical);return true;
    }
    public bool TryCompound(string input,string mode,out string output){
        output=null;if(!CompoundForms.Pattern.IsMatch(input)||KeepWord(input))return false;string custom;if(personal.TryGetValue(input,out custom)){output=Target(EncodingMap.Case(input,custom),mode);return true;}if(IndicScripts.Contains(input)){string decoded=CopiedFallback(input,"unicode");return decoded!=input&&TryCompound(decoded,mode,out output);}
        string stem=input,possessive="";if(stem.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||stem.EndsWith("’s",StringComparison.OrdinalIgnoreCase)){possessive=stem.Substring(stem.Length-2);stem=stem.Substring(0,stem.Length-2);}
        var sourceParts=Words.Matches(stem).Cast<Match>().ToArray();bool itrans=sourceParts.Any(part=>{string decoded;return DecodeItrans(part.Value,out decoded);});if(itrans){output=Words.Replace(stem,part=>{string decoded;if(DecodeItrans(part.Value,out decoded))return Target(decoded,mode);if((RecognizedIndic(part.Value,"unicode",true)||ExplicitIndicTriggers.Contains(part.Value))&&Itrans.TryWord(part.Value,out decoded))return Target(decoded.Replace('ṃ','ṁ'),mode);return part.Value;})+possessive;return true;}
        string canonical;if(KnownCompound(stem,out canonical)){string[] source=CompoundForms.Separator.Split(stem),parts=CompoundForms.Separator.Split(canonical);for(int n=0;n<parts.Length;n++)if(parts[n]=="founder")parts[n]=source[n];output=Target(CompoundForms.PreserveSeparators(stem,string.Join("-",parts)),mode)+possessive;return true;}
        if(mode=="devanagari"||mode=="bengali"||mode=="hindi")return false;
        bool indic=false;string converted=Words.Replace(stem,m=>{if(KeepWord(m.Value))return m.Value;string decoded;if(DecodeItrans(m.Value,out decoded)){indic=true;return Target(decoded,mode);}string value=Word(m.Value,mode);if(value!=m.Value||RecognizedIndic(m.Value,mode))indic=true;return value;});
        if(!indic)return false;output=converted+possessive;return true;
    }
    public string[] CompoundChoices(string input,string mode){string value;if(!TryCompound(input,mode,out value))return new string[0];bool[] protect=CompoundProtectedMask(input);int offset=0;string plain=Words.Replace(input,m=>{offset=m.Index;bool kept=protect!=null&&protect.Skip(offset).Take(m.Length).All(x=>x);if(kept)return m.Value;string decoded=m.Value,itrans;if(DecodeItrans(decoded,out itrans))decoded=itrans;else if(EncodingMap.IsLegacy(decoded)&&!EncodingMap.UnambiguousIndic(decoded))decoded=EncodingMap.FromBalaram(decoded);return EncodingMap.Ascii(decoded);});var variants=new List<string>{value};{string dictionaryInput=!EncodingMap.UnambiguousIndic(input)&&EncodingMap.IsLegacy(input)&&!KnownUnicode(input)?EncodingMap.FromBalaram(input):input;string rawKey=EncodingMap.Ascii(dictionaryInput).ToLowerInvariant().Replace('\u2011','-').Replace('\u2013','-');string recorded=string.Join("\x1f",new[]{cycles.Get(rawKey),cycles.Get(CompoundForms.Key(dictionaryInput))}.Where(x=>x!=null).Select(x=>x.Split('\0')[0]));if(recorded.Length>0)foreach(string form in recorded.Split('\x1f'))if(CompoundForms.Pattern.IsMatch(form)&&CompoundForms.Separator.Split(form).Length==CompoundForms.Separator.Split(input).Length)variants.Add(Target(EncodingMap.Case(input,CompoundForms.PreserveSeparators(input,form)),mode));}return variants.Where(x=>x!=plain).Concat(new[]{plain}).Distinct().ToArray();}
    public bool[] CompoundProtectedMask(string input){if(!CompoundForms.Pattern.IsMatch(input))return null;string stem=input;if(stem.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||stem.EndsWith("’s",StringComparison.OrdinalIgnoreCase))stem=stem.Substring(0,stem.Length-2);string canonical;bool known=KnownCompound(stem,out canonical);string[] parts=known?CompoundForms.Separator.Split(canonical):null;var mask=Enumerable.Repeat(true,input.Length).ToArray();int n=0;foreach(Match part in Words.Matches(stem)){string converted=Word(part.Value,"unicode");bool indic=known?parts[n]!="founder":converted!=part.Value||RecognizedIndic(part.Value,"unicode");if(indic)for(int j=part.Index;j<part.Index+part.Length;j++)mask[j]=false;n++;}return mask;}
    public ItalicSpan[] CompoundFontSpans(string input,string output,string mode){bool[] mask=CompoundProtectedMask(input);if(mask==null)return null;var source=Words.Matches(input).Cast<Match>().ToArray();var target=Words.Matches(output).Cast<Match>().ToArray();if(source.Length!=target.Length)return new ItalicSpan[0];var spans=new List<ItalicSpan>();for(int n=0;n<source.Length;n++)if(mask.Skip(source[n].Index).Take(source[n].Length).Any(x=>!x)){int length=target[n].Length;if(target[n].Value.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||target[n].Value.EndsWith("’s",StringComparison.OrdinalIgnoreCase))length-=2;spans.Add(new ItalicSpan(target[n].Index,length));}return spans.ToArray();}
    public string ContextualGoswami(string word,string context,string mode){string key=IndicStyle.Key(word,"balaram");if(key!="goswami"&&key!="gosvami")return null;context=context??word;var match=Regex.Matches(context,@"[\p{L}\p{M}]+").Cast<Match>().LastOrDefault(m=>m.Index+m.Length==context.Length);if(match==null)return Target(EncodingMap.FromBalaram(word),mode);return Target(EncodingMap.FromBalaram(IndicStyle.GoswamiTitle(context,match)),mode);}
    public string[] CaretChoices(string word,string mode){
        string number=Numbers.Single(word);if(number!=null&&(mode=="unicode"||mode=="balaram"||mode=="plain"))return new[]{number};
        if(KeepWord(word))return new string[0];if(CompoundForms.Pattern.IsMatch(word))return CompoundChoices(word,mode);string title=ContextualGoswami(word,null,mode);if(title!=null)return new[]{title,EncodingMap.Ascii(title)}.Distinct().ToArray();
        string stem=word;if(stem.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||stem.EndsWith("’s",StringComparison.OrdinalIgnoreCase))stem=stem.Substring(0,stem.Length-2);
        if(English(stem)&&!IndicLoanwords.Contains(stem)&&!personal.ContainsKey(word)&&!personal.ContainsKey(stem))return new string[0];
        return ClipboardChoices(word,mode,mode=="balaram"?"balaram":"auto");
    }
    public bool RecognizedIndic(string word,string mode="unicode",bool bookContext=false){if(KeepWord(word)||English(word)&&!IndicLoanwords.Contains(word)&&!personal.ContainsKey(word)&&!(bookContext&&(ExplicitIndicTriggers.Contains(word)||Vocabulary.Terms.ContainsKey(word))))return false;string decoded=mode=="balaram"?EncodingMap.FromBalaram(word):word;if(mode=="itrans"){string latin;if(Itrans.TryWord(word,out latin))decoded=latin;}if(IndicScripts.Contains(decoded))return true;if(personal.ContainsKey(word))return true;if(decoded.Any(c=>char.IsLetter(c)&&c>127&&!EncodingMap.IsIndic(c.ToString())))return false;return Find(EncodingMap.Ascii(decoded).ToLowerInvariant())!=null||EncodingMap.UnambiguousIndic(decoded);}
    public static bool AutomaticItalicMode(string mode){return mode=="unicode"||mode=="balaram"||mode=="plain";}
    public ItalicSpan[] ItalicWords(string text,string mode){var spans=new List<ItalicSpan>();if(!AutomaticItalicMode(mode))return spans.ToArray();int at=0;foreach(Match literal in Protected.Matches(text)){AddItalicWords(text.Substring(at,literal.Index-at),mode,at,spans);at=literal.Index+literal.Length;}AddItalicWords(text.Substring(at),mode,at,spans);return spans.ToArray();}
    void ReadItalicOverrides(string path){if(!File.Exists(path))return;foreach(string row in File.ReadLines(path)){if(row.Trim()==""||row.TrimStart().StartsWith("#"))continue;string[] cells=row.Split('\t');if(cells.Length!=2||!Regex.IsMatch(cells[0],@"^[a-z]+(?:-[a-z]+)*$",RegexOptions.IgnoreCase)||cells[1]!="roman"&&cells[1]!="italic"&&cells[1]!="name"||italicOverrides.ContainsKey(cells[0])||italicOverrides.Count>=1000)throw new InvalidDataException("Invalid italic exception");italicOverrides.Add(cells[0],cells[1]);}}
    bool RomanStyle(string word,string mode){string rule;if(italicOverrides.TryGetValue(IndicStyle.Key(word,mode),out rule))return rule=="roman"||rule=="name"&&char.IsUpper(word[0]);return IndicStyle.RomanWord(word,mode,bbtRomanNames);}
    bool ForceStyle(string word,string mode){string rule;return italicOverrides.TryGetValue(IndicStyle.Key(word,mode),out rule)&&rule=="italic";}
    string CapitalizeContextNames(string text,string mode){if(mode!="unicode"&&mode!="balaram"&&mode!="plain")return text;var names=IndicStyle.NameContext(text,mode,word=>RecognizedIndic(word,mode));if(names.Count==0)return text;char[] chars=text.ToCharArray();foreach(int index in names)chars[index]=char.ToUpperInvariant(chars[index]);return new string(chars);}
    string ProseNameCase(string text,string mode){return WithKeptWords(text,value=>{var result=new StringBuilder();int at=0;foreach(Match literal in Protected.Matches(value)){result.Append(CapitalizeContextNames(value.Substring(at,literal.Index-at),mode));result.Append(literal.Value);at=literal.Index+literal.Length;}result.Append(CapitalizeContextNames(value.Substring(at),mode));return result.ToString();});}
    void AddItalicWords(string text,string mode,int offset,List<ItalicSpan> spans){var compounds=Compounds.Matches(text);var contextualNames=IndicStyle.NameContext(text,mode,word=>RecognizedIndic(word,mode));for(int n=0;n<compounds.Count;n++){
        Match compound=compounds[n];string key=IndicStyle.Key(compound.Value,mode);
        if(n+1<compounds.Count&&(key=="sri"||key=="srimad")&&IndicStyle.Titles.Contains(IndicStyle.Key(compounds[n+1].Value,mode))&&Regex.IsMatch(text.Substring(compound.Index+compound.Length,compounds[n+1].Index-compound.Index-compound.Length),@"^ +$")){Match title=compounds[++n];spans.Add(new ItalicSpan(offset+compound.Index,title.Index+title.Length-compound.Index));continue;}
        if(contextualNames.Contains(compound.Index)&&!ForceStyle(compound.Value,mode))continue;
        if(n>0&&(key=="acarya"||key=="thakura"||key=="dasa"||key=="devi"||key=="dasi")&&char.IsUpper(compound.Value[0])&&char.IsUpper(compounds[n-1].Value[0])&&RomanStyle(compounds[n-1].Value,mode))continue;
        bool contrast=key=="karma"&&Regex.IsMatch(text.Substring(Math.Max(0,compound.Index-100),Math.Min(text.Length,compound.Index+100)-Math.Max(0,compound.Index-100)),@"\bakarma\b[\s\S]*?\bvikarma\b|\bvikarma\b[\s\S]*?\bakarma\b",RegexOptions.IgnoreCase);
        bool asWord=n>0&&(compounds[n-1].Value.Equals("word",StringComparison.OrdinalIgnoreCase)||compounds[n-1].Value.Equals("term",StringComparison.OrdinalIgnoreCase));
        if(!contrast&&!asWord&&!ForceStyle(compound.Value,mode)&&RomanStyle(compound.Value,mode))continue;MatchCollection parts=Words.Matches(compound.Value);
        bool allIndic=parts.Count>1&&parts.Cast<Match>().All(p=>RecognizedIndic(p.Value,mode)||RomanStyle(p.Value,mode));
        if(allIndic||contrast||asWord&&RecognizedIndic(compound.Value,mode,true)||ForceStyle(compound.Value,mode)){spans.Add(new ItalicSpan(offset+compound.Index,compound.Length));continue;}
        foreach(Match part in parts)if(!RomanStyle(part.Value,mode)&&(RecognizedIndic(part.Value,mode)||ForceStyle(part.Value,mode)))spans.Add(new ItalicSpan(offset+compound.Index+part.Index,part.Length));
    }}
    public ItalicSpan[] TriggerItalics(string trigger,string output,string mode){if(!AutomaticItalicMode(mode))return new ItalicSpan[0];var spans=new Dictionary<int,ItalicSpan>();foreach(var span in ItalicWords(output,mode))spans[span.Start]=span;string normalized=Regex.Replace(trigger,@"^(?i:(CC|CB)(Adi|Madhya|Antya))(?=\d)",m=>m.Groups[1].Value.ToUpperInvariant()+(m.Groups[2].Value.Equals("Adi",StringComparison.OrdinalIgnoreCase)?"1":m.Groups[2].Value.Equals("Madhya",StringComparison.OrdinalIgnoreCase)?"2":"3")+".");Match scriptureMatch=ScripturePattern.Match(normalized);if(scriptureMatch.Success){int verseLength;string expected=Scripture(scriptureMatch,out verseLength);if(expected==output){foreach(Match word in Words.Matches(output.Substring(0,verseLength)))spans[word.Index]=new ItalicSpan(word.Index,word.Length,true);}}return spans.Values.OrderBy(x=>x.Start).ToArray();}

    public static string ReencodeCurrent(string input,string mode){if(mode!="unicode"&&mode!="balaram")return null;string value=EncodingMap.UnambiguousIndic(input)?input:EncodingMap.IsLegacy(input)?EncodingMap.FromBalaram(input):null;return value==null?null:Target(value,mode);} public string ConvertHighlighted(string input,string mode,bool allowNumber=true){if(!allowNumber&&Numbers.Single(input)!=null)return input;string shortValue=CtrlShort(input,mode);if(shortValue!=null)return shortValue;string[] options=CaretChoices(input,mode);if(options.Length==0)return ConvertCopied(input,mode);int index=Array.IndexOf(options,input);return options[index<0?0:(index+1)%options.Length];}
    public bool RememberCaretSpelling(string input,string output,string mode,bool repeat,string occurrence=null){string[] options=CaretChoices(input,mode);if(output==input||!options.Contains(output)){if(output==EncodingMap.Ascii(output))CancelSpellingChoice();return false;}if(!repeat&&(input==EncodingMap.Ascii(input)||!options.Contains(input)))return false;return RememberSpelling(input,output,mode,mode=="balaram"?"balaram":"auto",occurrence);}
    string ConvertSegment(string text,string mode){bool singleWord=SingleProseWord(text);return Compounds.Replace(text,x=>{if(KeepWord(x.Value))return x.Value;string compound;if(TryCompound(x.Value,mode,out compound))return compound;return personal.ContainsKey(x.Value)?Word(x.Value,mode):English(x.Value)&&!IndicLoanwords.Contains(x.Value)?x.Value:Words.Replace(x.Value,w=>ProseWord(w.Value,mode,singleWord));});}
    public string TriggerWithObservedCase(string typed,string observed){if(!EditGuard.CaseEquivalent(typed,observed))return null;string original=Trigger(typed);if(original==null||IsDirectTrigger(typed))return original;return Trigger(observed)??original;}
    public string Trigger(string trigger) {
        if(trigger==null||CtrlOnlyShort(trigger))return null;string configured;if(snippets.TryGetValue(trigger,out configured))return exactShortKeys.Contains(trigger)?Configuration.Expand(configured,DateTime.Now):RomanNameExceptions(Configuration.Expand(configured,DateTime.Now));
        string value; if(Short.TryGetValue(trigger,out value))return RomanNameExceptions(value);
        string normalized=Regex.Replace(trigger,@"^(?i:(CC|CB)(Adi|Madhya|Antya))(?=\d)",x=>x.Groups[1].Value.ToUpperInvariant()+(x.Groups[2].Value.Equals("Adi",StringComparison.OrdinalIgnoreCase)?"1":x.Groups[2].Value.Equals("Madhya",StringComparison.OrdinalIgnoreCase)?"2":"3")+".");
        Match m=ScripturePattern.Match(normalized); if(m.Success)return Scripture(m);
        if(trigger.Length<2 || (trigger[trigger.Length-1]!='>'&&trigger[trigger.Length-1]!='<'&&trigger[trigger.Length-1]!='?'))return null;
        string word=trigger.Substring(0,trigger.Length-1);if(KeepWord(word))return null;
        if(!Regex.IsMatch(word,@"^[\p{L}\p{M}]+(?:[-\u2011\u2013][\p{L}\p{M}]+)*(?:['’]s)?$"))return null;
        string mode=trigger.EndsWith("<")?"balaram":trigger.EndsWith("?")?"plain":"unicode";
        string compoundOutput;if(TryCompound(word,mode,out compoundOutput))return compoundOutput;
        string stem=word,possessive="";if(word.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||word.EndsWith("’s",StringComparison.OrdinalIgnoreCase)){stem=word.Substring(0,word.Length-2);possessive=word.Substring(word.Length-2);}
        if(English(stem)&&!IndicLoanwords.Contains(stem)&&!ExplicitIndicTriggers.Contains(stem)&&!personal.ContainsKey(stem))return null;
        if(ExplicitIndicTriggers.Contains(stem)&&!personal.ContainsKey(stem)){string explicitForm=Find(stem);return explicitForm==null?null:Target(EncodingMap.Case(stem,explicitForm),mode)+possessive;}
        string canonical=EncodingMap.Ascii(EncodingMap.IsLegacy(stem)?EncodingMap.FromBalaram(stem):stem).ToLowerInvariant();
        if(Find(stem)==null&&Find(canonical)==null&&Find(EncodingMap.Ascii(stem).ToLowerInvariant())==null&&!personal.ContainsKey(word)&&!personal.ContainsKey(stem))return null;
        return Word(word,mode);
    }
    string Scripture(Match m){int length;return Scripture(m,out length);}
    string Scripture(Match m,out int verseLength) {
        verseLength=0;
        string book=m.Groups[1].Value.ToUpperInvariant(),reference=m.Groups[2].Value.ToUpperInvariant(),kind=m.Groups[3].Value.ToUpperInvariant(),suffix=m.Groups[5].Value=="?"?":":m.Groups[5].Value;
        if(kind=="")kind="V";
        string key=book+reference;
        if(book=="CB"&&reference.Contains(".")){int dot=reference.IndexOf('.');string part=reference.Substring(0,dot);if(part=="1"||part=="2"||part=="3")key="CB"+(part=="1"?"Adi":part=="2"?"Madhya":"Antya")+reference.Substring(dot+1);}
        string verse=kind.Contains("V")?ScriptureRecord(key,suffix):null;
        string translation=kind.Contains("T")?ScriptureRecord(key,"T"+suffix):null;
        if((kind.Contains("V")&&verse==null)||(kind.Contains("T")&&translation==null))return null;
        if(verse!=null){ string[] lines=verse.Replace("\r\n","\n").Split('\n');string first=lines[0];if(first.EndsWith("uvāca")||first.EndsWith("uvaca")||first.EndsWith("uväca"))verse=string.Join("\n",lines.Skip(1)); }
        verseLength=verse==null?0:verse.Length;
        string output=verse==null?translation:translation==null?verse:verse+"\n\n"+translation;
        if(m.Groups[4].Success){string title=book=="BG"?"Bhagavad-gītā":book=="SB"?"Śrīmad-Bhāgavatam":book=="CC"?"Śrī Caitanya-caritāmṛta":book=="CB"?"Śrī Caitanya-bhāgavata":book=="ISO"?"Śrī Īśopaniṣad":book=="BS"?"Brahma-saṁhitā":"Nectar of Instruction";output+="\n\n"+Target(title,suffix=="<"?"balaram":suffix==":"?"plain":"unicode")+" "+reference;}
        return RomanNameExceptions(output.Replace("Bhagavad-Gītā","Bhagavad-gītā"));
    }
    string ScriptureRecord(string key,string suffix){string value=scripture.Get(key+suffix);if(value!=null||!key.StartsWith("CB"))return value;int dot=key.LastIndexOf('.');if(dot<0)return null;string number=key.Substring(dot+1).TrimStart('0');if(number=="")number="0";for(int width=1;width<=4;width++){value=scripture.Get(key.Substring(0,dot+1)+number.PadLeft(width,'0')+suffix);if(value!=null)return value;}return null;}
    public string[] Cycle(string word,char suffix) {
        if(KeepWord(word))return new string[0];if(CompoundForms.Pattern.IsMatch(word))return CompoundChoices(word,suffix=='<'?"balaram":suffix=='?'?"plain":"unicode");
        string custom;if(personal.TryGetValue(word,out custom)){string customMode=suffix=='<'?"balaram":suffix=='?'?"plain":"unicode";return new[]{Target(EncodingMap.Case(word,custom),customMode),EncodingMap.Ascii(word)}.Distinct().ToArray();}
        if(word=="Brahman"||word=="Brahman's"||word=="Brahman’s")return new[]{Target(word,suffix=='<'?"balaram":suffix=='?'?"plain":"unicode")};
        string possessive="";
        if(word.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||word.EndsWith("’s",StringComparison.OrdinalIgnoreCase)){possessive=word.Substring(word.Length-2);word=word.Substring(0,word.Length-2);}
        if(English(word)&&!IndicLoanwords.Contains(word)&&!ExplicitIndicTriggers.Contains(word)&&!personal.ContainsKey(word))return new string[0];
        string preferred=Word(word,"unicode");
        if(ExplicitIndicTriggers.Contains(word)&&!personal.ContainsKey(word)){string explicitForm=Find(word);if(explicitForm!=null)preferred=EncodingMap.Case(word,explicitForm);}
        string key=EncodingMap.Ascii(EncodingMap.IsLegacy(word)&&!KnownUnicode(word)?EncodingMap.FromBalaram(word):word).ToLowerInvariant();
        string data=cycles.Get(key)??cycles.Get(word)??cycles.Get(EncodingMap.Ascii(preferred).ToLowerInvariant());
        if(data==null){string singular=PluralStem(key);if(singular==null){string canonicalWord=Find(key);if(canonicalWord==null)return new string[0];data=canonicalWord;}else{string singularData=cycles.Get(singular);string canonical=ExactWord(singular);data=singularData==null?canonical+"s":string.Join("\x1f",singularData.Split('\0')[0].Split('\x1f').Select(x=>x+"s"));}}
        // The conversion table carries the corpus-frequency winner. Keep recovered alternatives after it.
        string[] values=data.Split('\0')[0].Split('\x1f');string morphology=MorphologyForms(key)??MorphologyForms(EncodingMap.Ascii(preferred).ToLowerInvariant());if(morphology!=null)values=values.Concat(morphology.Split('\x1f')).Distinct().ToArray();
        string mode=suffix=='<'?"balaram":suffix=='?'?"plain":"unicode";
        string plain=EncodingMap.Ascii(EncodingMap.IsLegacy(word)&&!KnownUnicode(word)?EncodingMap.FromBalaram(word):word)+possessive;
        return new[]{preferred}.Concat(values.Select(x=>EncodingMap.Case(word,x))).Select(x=>Target(x,mode)+possessive).Where(x=>x!=plain).Concat(new[]{plain}).Distinct().ToArray();
    }
    void DisposeStores(){
        if(verseNormalization!=null&&verseNormalization.IsValueCreated&&verseNormalization.Value!=null)verseNormalization.Value.Dispose();
        if(hindiDictionary!=null&&hindiDictionary.IsValueCreated&&hindiDictionary.Value!=null)hindiDictionary.Value.Dispose();
        if(sanskritMorphology!=null&&sanskritMorphology.IsValueCreated&&sanskritMorphology.Value!=null)sanskritMorphology.Value.Dispose();
        if(lexicon!=null)lexicon.Dispose();if(scripture!=null)scripture.Dispose();if(cycles!=null)cycles.Dispose();if(corrections!=null)corrections.Dispose();
    }
    public void Dispose(){try{lock(preferenceSync){if(preferenceTimer!=null){preferenceTimer.Dispose();preferenceTimer=null;}FinishSpellingChoice();}}finally{DisposeStores();}}
}
public static class EditGuard {
    public static bool CaseEquivalent(string typed,string observed){if(typed==null||observed==null||typed.Length!=observed.Length)return false;for(int i=0;i<typed.Length;i++){char a=typed[i],b=observed[i];if(a==b)continue;if(!((a>='a'&&a<='z')||(a>='A'&&a<='Z'))||!((b>='a'&&b<='z')||(b>='A'&&b<='Z'))||char.ToUpperInvariant(a)!=char.ToUpperInvariant(b))return false;}return true;}
    static bool TokenCharacter(char c){return char.IsLetterOrDigit(c)||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.SpacingCombiningMark||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.EnclosingMark||c=='-'||c=='\u2011'||c=='\u2013'||c=='_'||c=='\''||c=='’'||c=='.'||c=='/'||c=='\\'||c=='@';}
    public static bool LeftBoundary(string text,int start,string expected){if(start<0||start>text.Length||expected.Length==0)return false;return start==0||!TokenCharacter(expected[0])||!TokenCharacter(text[start-1]);}
    public static bool RightBoundary(string text,int end,string expected){if(end<0||end>text.Length||expected.Length==0)return false;return end==text.Length||char.IsWhiteSpace(expected[expected.Length-1])||!TokenCharacter(text[end]);}
    public static bool SameFocus(int[] a,int[] b){
        if(a==null||b==null||a.Length==0||a.Length!=b.Length)return false;
        for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;
        return true;
    }
    public static bool Locate(string text,int start,int end,string expected,out int begin){
        begin=-1;if(text==null||string.IsNullOrEmpty(expected)||start!=end||start<expected.Length||start>text.Length)return false;
        int candidate=start-expected.Length;if(string.CompareOrdinal(text,candidate,expected,0,expected.Length)!=0)return false;
        if(!LeftBoundary(text,candidate,expected)||!RightBoundary(text,end,expected))return false;
        begin=candidate;return true;
    }
}
// Bounded ownership: a token never includes earlier whitespace-delimited text.
public sealed class EllipsisTyping {
    string tail="";
    public void Clear(){tail="";}
    public void Type(char c){tail+=c;if(tail.Length>96)tail=tail.Substring(tail.Length-96);}
    public static bool Delimiter(char c){return c==' '||",;:!?)]}\"'’”".IndexOf(c)>=0;}
    public static string Replacement(string input){if(string.IsNullOrEmpty(input)||!Delimiter(input[input.Length-1]))return null;string body=input.Substring(0,input.Length-1);if(body.StartsWith(" ",StringComparison.Ordinal)||body.StartsWith("\u00a0",StringComparison.Ordinal))body=body.Substring(1);return body=="..."||body==". . ."?"\u00a0.\u00a0.\u00a0."+input[input.Length-1]:null;}
    public string Completed(char delimiter){
        if(!Delimiter(delimiter))return null;var match=Regex.Match(tail,@"[ \u00a0]?(?:\.\.\.|\. \. \.)$");if(!match.Success)return null;
        string before=tail.Substring(0,match.Index);if(before.EndsWith(".",StringComparison.Ordinal)||before.EndsWith(". ",StringComparison.Ordinal))return null;
        int boundary=before.LastIndexOf(' ');string token=before.Substring(boundary+1);if(token.IndexOfAny(new[]{'/', '\\', '@', '`'})>=0)return null;
        return match.Value+delimiter;
    }
}
public sealed class InputState {
    readonly EllipsisTyping ellipsis=new EllipsisTyping();
    public string Token=""; public long Generation; public bool Paused; bool overflow;string whitespaceToken="";bool whitespaceOverflow;
    public string UndoOriginal,UndoOutput; public IntPtr Owner; public DateTime CycleDeadline;public string[] Candidates;public int CycleIndex;public int PendingCycles;public string CycleMode="unicode";
    public void Clear(){Token="";overflow=false;whitespaceToken="";whitespaceOverflow=false;ellipsis.Clear();Generation++;UndoOriginal=null;UndoOutput=null;Candidates=null;PendingCycles=0;}
    public string CompletedEllipsis(char c){return Paused?null:ellipsis.Completed(c);}
    public string CurrentLiteral {get{return whitespaceOverflow?null:whitespaceToken;}}
    public void RecordReplacement(string original,string replacement,IntPtr owner,bool automaticCorrection=false){Owner=owner;UndoOriginal=automaticCorrection?null:original;UndoOutput=automaticCorrection?null:replacement;if(automaticCorrection)Candidates=null;}
    public bool ReplaceLastLetter(char expected,char replacement){
        if(overflow||whitespaceOverflow||Token.Length==0||whitespaceToken.Length==0||Token[Token.Length-1]!=expected||whitespaceToken[whitespaceToken.Length-1]!=expected)return false;
        Token=Token.Substring(0,Token.Length-1)+replacement;whitespaceToken=whitespaceToken.Substring(0,whitespaceToken.Length-1)+replacement;UndoOriginal=UndoOutput=null;Candidates=null;PendingCycles=0;return true;
    }
    public void CancelManualConversion(){Generation++;UndoOriginal=UndoOutput=null;Candidates=null;PendingCycles=0;}
    public string Type(char c,Core core){string candidate=Type(c),literal=CurrentLiteral;if(core.IsDirectTrigger(literal))return literal;if((c==':'||c=='#')&&literal!=null&&literal.Length>1&&char.IsDigit(literal[0]))return null;return candidate;}
    public bool BackspaceTyped(){
        if(Paused||overflow||whitespaceOverflow||Token.Length==0||whitespaceToken.Length==0||Token[Token.Length-1]!=whitespaceToken[whitespaceToken.Length-1])return false;
        Token=Token.Substring(0,Token.Length-1);whitespaceToken=whitespaceToken.Substring(0,whitespaceToken.Length-1);Generation++;UndoOriginal=UndoOutput=null;Candidates=null;PendingCycles=0;ellipsis.Clear();return true;
    }
    public string CompletedEnglish(char c){return !Paused&&Core.CompletionDelimiter(c)&&!whitespaceOverflow&&whitespaceToken.Length>0?whitespaceToken+c:null;}
    public bool QueueCycle(char c,IntPtr owner,DateTime now){
        if(Paused||Token!=""||Candidates==null||Candidates.Length<2||Owner!=owner||now>=CycleDeadline||(c!='>'&&c!='<'&&c!='?'))return false;
        // Coalesce bursts while an edit is in flight; no timer or background polling.
        if(PendingCycles<64)PendingCycles++;
        CycleDeadline=now.AddSeconds(10);return true;
    }
    public string Type(char c) {
        Generation++;PendingCycles=0;
        if(Paused){Clear();return null;}
        ellipsis.Type(c);
        if(char.IsWhiteSpace(c)){whitespaceToken="";whitespaceOverflow=false;}else if(!whitespaceOverflow&&whitespaceToken.Length<160)whitespaceToken+=c;else whitespaceOverflow=true;
        if(c=='>'||c=='<'||c=='?'||c=='#'||c==':'){string candidate=overflow?null:Token+c;Token="";overflow=false;UndoOriginal=null;UndoOutput=null;Candidates=null;return candidate;}
        UndoOriginal=null;UndoOutput=null;Candidates=null;
        if(c=='.'&&!Regex.IsMatch(Token,@"^(?i:BG|SB|CC|CB|ISO|BS|NOI)(?i:Adi|Madhya|Antya)?\d+(?:\.\d+){0,1}$")){Token="";overflow=false;return null;}
        if(char.IsLetterOrDigit(c)||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.SpacingCombiningMark||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.EnclosingMark||c=='-'||c=='\u2011'||c=='\u2013'||c=='.'||c=='\''||c=='’') { if(!overflow&&Token.Length<160)Token+=c;else overflow=true; }
        else {Token="";overflow=false;}
        return null;
    }
    public static int IconPixels(int dpi){return Math.Max(16,(int)Math.Round(16.0*dpi/96));}
}
// Clipboard identity distinguishes a repeated shortcut from a newly copied, identical word.
public sealed class ClipboardCycle {
    string mode,source,last,clipboardExpected;uint sequence;IntPtr owner;int[] focus;DateTime deadline;string[] choices;int index;
    public string Current {get{return last;}}
    public void Clear(){choices=null;focus=null;last=null;}
    public void Start(string output,string[] variants,string format,string inputSource,uint clipboardSequence,IntPtr window,int[] field,DateTime now){
        Clear();if(variants==null||variants.Length<2||field==null||field.Length==0)return;
        int found=Array.IndexOf(variants,output);if(found<0)return;
        choices=(string[])variants.Clone();index=found;last=clipboardExpected=output;mode=format;source=inputSource;sequence=clipboardSequence;owner=window;focus=(int[])field.Clone();deadline=now.AddSeconds(10);
    }
    public bool Next(string format,string inputSource,string clipboard,uint clipboardSequence,IntPtr window,int[] field,DateTime now,out string output){
        output=null;if(choices==null)return false;
        if(format!=mode||inputSource!=source||clipboard!=clipboardExpected||clipboardSequence!=sequence||owner!=window||!EditGuard.SameFocus(focus,field)||now>=deadline){Clear();return false;}
        output=choices[(index+1)%choices.Length];return true;
    }
    public bool NextSelected(string format,string inputSource,string selected,uint clipboardSequence,IntPtr window,int[] field,DateTime now,out string output){output=null;if(choices==null)return false;if(format!=mode||inputSource!=source||selected!=last||clipboardSequence!=sequence||owner!=window||!EditGuard.SameFocus(focus,field)||now>=deadline){Clear();return false;}output=choices[(index+1)%choices.Length];return true;}
    public void Commit(string output,uint clipboardSequence,DateTime now){if(choices==null)return;index=(index+1)%choices.Length;last=clipboardExpected=output;sequence=clipboardSequence;deadline=now.AddSeconds(10);}
    public void Preserved(string clipboard,uint clipboardSequence){if(choices==null)return;clipboardExpected=clipboard;sequence=clipboardSequence;}
    public void Released(uint clipboardSequence){if(choices==null)return;clipboardExpected="";sequence=clipboardSequence;}
    public static bool MayClearClipboard(uint written,uint current,bool pasted,bool sameContext){return written==current&&pasted&&sameContext;}
}
}






