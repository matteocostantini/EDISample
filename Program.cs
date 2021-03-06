﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CsvHelper;
using EDI;
using Newtonsoft.Json;

namespace EDISample
{
    class Program
    {
        static void Import()
        {
            var message = Helpers.LoadMessage("KLI_Articoli");
            var process = Helpers.LoadProcess("BRESSANA");

            using (Interpreter interpreter = new Interpreter(message, "item-data.csv")) 
            {
                while (interpreter.MoreMessages())
                {
                    var composer = new XMLComposer("Items", process, interpreter);
                    var items = composer.CreateNode("Items");
                    composer.AddField(items,"Item");
                    composer.AddField(items,"SaleBarCode");
                    composer.AddField(items,"Description");
                    composer.AddField(items,"UseSerialNo");
                    composer.AddNode(items);

                    var goods = composer.CreateNode("GoodsData");
                    composer.AddField(goods,"NetWeight");
                    composer.AddField(goods,"GrossWeight");
                    composer.AddField(goods,"GrossVolume");
                    composer.AddNode(goods);

                    var uom = composer.CreateNode("AlternativeUoM");
                    int child = 1;
                    while (composer.HasMappings(uom,"AlternativeUoMRow", child))
                    {
                        var row = composer.CreateNode("AlternativeUoMRow");
                        composer.AddField(uom, row, child, "ComparableUoM");
                        composer.AddField(uom, row, child, "BaseUoMQty");
                        composer.AddField(uom, row, child, "CompUoMQty");
                        uom.Add(row);
                        child++;
                    }
                    composer.AddNode(uom);

                    var doc = composer.GetDocument();

                    string fname = Path.GetTempFileName();
                    doc.Save(fname);
                    Console.WriteLine(File.ReadAllText(fname)); 
                }
            }
        }

        static string custom(string target, string value)
        {
            if (target == "KLI_Articoli.Descrizione1")
                return value.Substring(0, Math.Min(value.Length,35));
            if (target == "KLI_Articoli.Descrizione2")
                return value.Length > 35 ? value.Substring(35, Math.Min(value.Length - 35, 35)) : "";

            return null;
        }

        static void Export()
        {
            var process = Helpers.LoadProcess("BRESSANA-ARTICOLI");
            var message = Helpers.LoadMessage(process.message);

            XMLExtractor extractor = new XMLExtractor(process, custom);
            using (Composer composer = new Composer(message, process, "out.txt", extractor))  
            {
                composer.Compose();                
            };
        }

        static void Main(string[] args)
        {
            // Import();
            Export();
        }
    }
}
