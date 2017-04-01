using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Xml;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            ////////////////////////////////FILE EXTRACTION SECTION //////////////////////////////////////////////////////////////////////

            var OdxeFilepath = @"C:\Users\rsaru\Downloads\Work\PDX\A-IVI_CD_DEFconfig_Scope2_Sample_V03.2.2.0.odx-e";
            string[] dumpRead  = { "", "", "", "", "", "", "", "", "" };
            string filename = Path.GetFileNameWithoutExtension(OdxeFilepath);
            XDocument xml = XDocument.Load(OdxeFilepath);
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(OdxeFilepath);
            //foreach (XmlNode node in xmldoc.SelectNodes("descendant::CONFIG-DATA[SHORT-NAME='751075084601']"))
            //{
                //is there some method to check an attribute like
                //bool isCapital = node.Attributes.Exist("IsCapital");
                //Console.WriteLine("Value: {0}", node.LastChild.InnerText);
                //some code here...
            //}
            //XmlNodeList list = xml.SelectNodes("X/Y");
            //foreach (XElement xx in xml.Descendants("CONFIG-DATA"))
            //{

            //  if (xx.Element("SHORT-NAME").Value == "750075080001")
            //{
            var RDBIs =  xml.Descendants("CONFIG-DATA")
                         .Where(x => (string)x.Element("SHORT-NAME") == "751075084601")
                         .Descendants("SDG")
                         .Elements("SD")
                         .Select(RDBI => new
                         {
                             Header = (RDBI.Attribute("SI").Value == "READ-DIAG-COMM") ? (RDBI.Value) : null,
                             //Header = (string)RDBI.Value
                         });
            /*var RDBIs = (from RDBI in xml.Descendants("DATA-RECORD")
                                 //on RDBI.Attribute("ID-REF").Value equals "SDG_TYPE_MG002"
                                 .Where(x => (string)x.Element("SHORT-NAME").Value == "751075084601")
                                 select new
                                 {
                                     Header = (RDBI.Attribute("SI").Value == "READ-DIAG-COMM") ? (RDBI.Element("SD").Value) : string.Empty,
                                     //Header = RDBI.Element("SD").Value
                                 }).ToList(); */
                     //var readDIDs = RDBIs.Where(x => x.Header.Any(r => r.RegionName == "West")).ToList();
                     //Loop through results
                     int i = 0;
                    foreach (var RDBI in RDBIs)
                    {
                        if(RDBI.Header != null)
                        {
                            dumpRead[i] = RDBI.Header;
                            Console.WriteLine("dumpRead values are: {0}", dumpRead[i]);
                         }                            
                    }
            //}            
            //}            
        }
    }
}
