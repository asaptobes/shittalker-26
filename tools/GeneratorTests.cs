using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

class EdgeRandom : Random {
    readonly bool last;
    public EdgeRandom(bool last) {this.last=last;}
    public override int Next(int max) {return last?max-1:0;}
}
class GeneratorTests {
    static void Check(bool value,string message) {if(!value)throw new Exception(message);}
    static void Main(string[] args) {
        var c=new JavaScriptSerializer().Deserialize<Catalog>(File.ReadAllText(args[0]));
        Check(c.insult_groups.Select(g=>g.Count).SequenceEqual(new[]{6,59,68,69}),"Insult table counts");
        Check(c.insult_groups[0].Count(s=>s=="you are a")==2,"Repeated lead-in must retain its weighting");
        Check(c.noise_groups.All(g=>g.Count==6),"Six choices in each of four noise tables");
        Check(c.noise_groups[1][2]=="aaaa" && c.noise_groups[2][3]=="aea" && c.noise_groups[3][5]=="oni","Short literals must not be lost by strings extraction");
        Check(Generators.Insult(c,new EdgeRandom(false))=="you are a stupid muppet loving chicken","First insult choices and spacing");
        Check(Generators.Insult(c,new EdgeRandom(true))=="your brother is a angry smart janitor with a stillbee","Last insult choices");
        Check(Generators.SickoNoise(c,new EdgeRandom(false))=="aeeuuiooueuieeiiuuiloioolllllaallloooaa","Noise is concatenated without spaces");
        Check(Generators.SickoNoise(c,new EdgeRandom(true))=="aailaaiiiieuyuueaeiiioni","Last noise choices");
        var r=new Random(26);
        for(int i=0;i<1000;i++) {
            Check(!string.IsNullOrWhiteSpace(Generators.Insult(c,r)),"Empty insult");
            Check(Generators.SickoNoise(c,r).All(ch=>"aeioulyn".Contains(ch)),"Unexpected noise character");
        }
        Console.WriteLine("PASS: recovered table counts, duplicates, short strings, first/last choices, spacing, and 1000 generations.");
    }
}
