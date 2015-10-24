using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

/*
var atomicTypes = ['byte', 'int', 'uint', 'int16', 'uint16', 'int32', 'uint32', 'float32', 'float64', 'string', 'url', 'alias'];
var structures = ['Sequence', 'Structure', 'Dataset'];

var IDENTIFIER_REGEX = '[\\w-/]'

    */
namespace sharpdap
{
    public class Loader
    {
        private string pURL = "";
        private string pRetValue = "";
        private CallbackDelegate pcallback;

        public delegate void CallbackDelegate(string RetValue);

        public string loadDataset(string url)
        {
            string retvalue;
            pURL = url;
            using (WebClient client = new WebClient())
            {
                retvalue = client.DownloadString(new Uri(pURL + ".dds"));
                pRetValue = ddsParser.parse(retvalue);
                retvalue = client.DownloadString(new Uri(pURL + ".das"));
                pRetValue += dasParser.parse(retvalue);
            }
            return pRetValue;
        }

        public void loadDatasetAsync(string url, CallbackDelegate callback)
        {
            pURL = url;
            pcallback = callback;
            using (WebClient client = new WebClient())
            {
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadDDSCompleted);
                client.DownloadStringAsync(new Uri(pURL + ".dds"));
            }
        }

        private void client_DownloadDDSCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string text = e.Result;
            pRetValue = ddsParser.parse(text);
            using (WebClient client = new WebClient())
            {
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadDASCompleted);
                client.DownloadStringAsync(new Uri(pURL + ".das"));
            }
        }

        private void client_DownloadDASCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string text = e.Result;

            pRetValue += dasParser.parse(text);
            pcallback(pRetValue);
        }
    }

    static class ddsParser
    {
        public static dynamic parse(string value)
        {
            string token = "";
            var vDataset = new Dataset();
            //dynamic dataset = new ETTExpandoObject();
            vDataset.type = "Dataset";
            dynamic dataset = vDataset;
            value = value.consume("dataset");
            value = value.consume("{");
            while (value.peek("}") != "}")
            {
                dynamic declaration = null; 
                value = value._declaration(ref declaration);
                dataset[declaration.name] = declaration;
            }
            value = value.consume("}");
            value= value.consume("[^;]+", ref token);
            dataset.id = token;
            dataset.name = token;
            value = value.consume(";");

            ////// *** Should serialize both static properties dynamic properties
            var json = JsonConvert.SerializeObject(dataset, Formatting.Indented);
            Console.WriteLine("*** Serialized Native object:");
            Console.WriteLine(json);

            ////string xml;
            ////SerializationUtils.SerializeObject(dataset, out xml);
            ////Console.WriteLine("*** Serialized Dynamic object:");
            ////Console.WriteLine(xml);

            walk(dataset, false);
            return dataset;
        }

        // Set id.
        private static void walk(dynamic dapvar, bool includeParent)
        {
            foreach (var attr in dapvar.Properties)
            {
                var child = dapvar[attr];
                if (child.type)
                {
                    child.id = child.name;
                    if (includeParent)
                    {
                        child.id = dapvar.id + '.' + child.id;
                    }
                    walk(child, true);
                }
            }
        }        
    }

    static class dasParser
    {
        public static string parse(string value)
        {

            return "";
        }


    }

    public class Dataset : ETTExpandoObject
    {
        public string type { get; set; }       
        public Dataset() : base()
        { }

    }

    public static class sharpdapStringExtension
    {
        // This is the extension method.
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined.
        static string IDENTIFIER_REGEX = "[\\w-/]";

        public static string consume(this string value, string expr, ref string token)
        {
            string retValue = _consume(value, expr);
            if (value != "")
            {
                value = value.Substring(retValue.Length).TrimStart();
                token = retValue;
                return value; // matches[0].ToString();
            }
            else
            {
                throw new SystemException("Unable to parse stream: " + value.Substring(0, 10));
            }            
        }

        public static string consume(this string value, string expr)
        {
            string retValue = _consume(value, expr);
            if (value!="")
            {
                value = value.Substring(retValue.Length).TrimStart();
                return value; // matches[0].ToString();
            }
            else
            {
                throw new SystemException("Unable to parse stream: " + value.Substring(0, 10));
            }
        }

        private static string _consume(string value, string expr)
        {
            Regex regExp = new Regex("^" + expr, RegexOptions.IgnoreCase);
            MatchCollection matches = regExp.Matches(value);
            if (matches.Count > 0)
            {
                return matches[0].ToString();
            }
            else
            {
                return "";
            }
        }

        public static string peek(this string value, string expr)
        {
            Regex regExp = new Regex("^" + expr, RegexOptions.IgnoreCase);
            MatchCollection matches = regExp.Matches(value);
            if (matches.Count > 0)
            {
                return matches[0].ToString();
            }
            return "";
        }

        public static dynamic _declaration(this string value, ref dynamic declaration)
        {
            var type = value.peek(IDENTIFIER_REGEX + '+').ToLower();
            switch (type)
            {
                case "grid": return value._grid(ref declaration);
                case "structure": return value._structure(ref declaration);
                case "sequence": return value._sequence(ref declaration);
                default: return value._base_declaration(ref declaration);
            }
        }

        private static dynamic _grid(this string value, ref dynamic rdeclaration)
        {
            string token = "";
            var Grid = new Dataset();            
            Grid.type = "Grid";
            dynamic grid = Grid;

            //dynamic dataset = new ETTExpandoObject();
            //dynamic grid = new ExpandoObject();
            //grid.type="Grid";

            value = value.consume("grid");
            value = value.consume("{");

            value = value.consume("array");
            value = value.consume(":");
            grid.array = null;
            value = value._base_declaration(ref grid.array);

            value = value.consume("maps");
            value = value.consume(":");
            grid.maps = new Dataset();
            while (value.peek("}")!="}")
            {
                dynamic map_ = null;
                value = value._base_declaration(ref map_);
                grid.maps[map_.name] = map_;
            }
            value = value.consume("}");

            value = value.consume(IDENTIFIER_REGEX + '+', ref token);
            grid.name = token;
            value = value.consume(";");

            rdeclaration = grid;
            return value;
        }

        private static dynamic _structure(this string value, ref dynamic rdeclaration)
        {
            string token = "";
            var Structure = new Dataset();
            Structure.type = "Structure";
            dynamic structure = Structure;

            //dynamic structure = new ExpandoObject();
            //structure.type = "Structure";            

            value = value.consume("structure");
            value = value.consume("{");
            while (value.peek("}")!="}")
            {
                dynamic declaration = null;
                value = value._declaration(ref declaration);
                structure[declaration.name] = declaration;
            }
            value = value.consume("}");

            value = value.consume(IDENTIFIER_REGEX + "+",ref token);
            structure.name = token;
            structure.dimensions = new List<string>();            
            structure.shape = new List<int>();
            while (value.peek(";")!=";")
            {
                value = value.consume("\\[");
                value = value.consume(IDENTIFIER_REGEX + "+",ref token);

                if (value.peek("=")=="=")
                {
                    structure.dimensions.Add(token);
                    value = value.consume("=");
                    value = value.consume("\\d+", ref token);
                }
                structure.shape.Add(Convert.ToInt32(token));
                value = value.consume("\\]");
            }
            value = value.consume(";");
            
            rdeclaration = structure;
            return value;
        }

        private static dynamic _sequence(this string value, ref dynamic rdeclaration)
        {
            string token = "";
            var Sequence = new Dataset();
            Sequence.type = "Sequence";
            dynamic sequence = Sequence;

            //dynamic sequence = new ExpandoObject();
            //sequence.type = "Sequence";

            value = value.consume("sequence");
            value = value.consume("{");
            while (value.peek("}")!="}")
            {
                dynamic declaration=null;
                value = value._declaration(ref declaration);
                sequence[declaration.name] = declaration;
            }
            value = value.consume("}");

            value = value.consume(IDENTIFIER_REGEX + '+', ref token);
            sequence.name = token;
            value = value.consume(";");

            rdeclaration = sequence;
            return value;
        }

        private static string _base_declaration(this string value, ref dynamic rdeclaration)
        {
            string token = "";
            var BaseType = new Dataset();
            //Sequence.type = "Sequence";
            dynamic baseType = BaseType;
            //dynamic baseType = new ExpandoObject();
            value = value.consume(IDENTIFIER_REGEX + '+', ref token);
            baseType.type = token;
            value = value.consume(IDENTIFIER_REGEX + '+', ref token);
            baseType.name = token;
            baseType.dimensions = new List<string>();
            baseType.shape = new List<int>();
            while (value.peek(";")!=";")
            {
                value=value.consume("\\[");                
                value = value.consume(IDENTIFIER_REGEX + '+', ref token);
                if (value.peek("=")=="=")
                {
                    baseType.dimensions.Add(token);
                    value=value.consume("=");
                    value = value.consume("\\d+", ref token);
                }
                baseType.shape.Add(Convert.ToInt32(token));
                value=value.consume("\\]");
            }
            value=value.consume(";");
            rdeclaration = baseType;
            return value;
        }
    }
}
