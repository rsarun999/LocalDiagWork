using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Text;
using System.Diagnostics;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            ////////////////////////////////GLOBAL INITIALIZATION SECTION //////////////////////////////////////////////////////////////////////
            string fileFilter = null;
            int NOofBlock = 9;
            List<List<byte>> diagRequests = new List<List<byte>>();

            ////////////////////////////////FILE EXTRACTION SECTION //////////////////////////////////////////////////////////////////////

            var zipFilepath = @"C:\Users\rsaru\Downloads\Work\PDX\refa010.pdx";

            string filename = Path.GetFileNameWithoutExtension(zipFilepath);

            //FileInfo fileInfo = new FileInfo(zipFilepath);
            string directoryFullPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);//fileInfo.DirectoryName; // contains directory path: "C:\MyDirectory"
            var myUniqueFileName = string.Format(@"{0}", DateTime.Now.Ticks);  // generates a random name for the extracted folder to merge with the existing directory path
            /*var directoryFullPath = new DirectoryInfo(
            null ?? Directory.GetCurrentDirectory());
            while (directoryFullPath != null && !directoryFullPath.GetFiles("*.sln").Any())
            {
                directoryFullPath = directoryFullPath.Parent;
            }*/
            //combines pdx file path to the random folder name
            string combinedPath = Path.Combine(directoryFullPath, filename);

            Console.WriteLine("When you combine '{0}' and '{1}', the result is: {2}'{3}'",
                        directoryFullPath, myUniqueFileName, Environment.NewLine, combinedPath);

            FastZip fastZip = new FastZip();

            // Will always overwrite if target filenames already exist, but files are extracted always to a random folder and hence not a problem
            fastZip.ExtractZip(zipFilepath, combinedPath, fileFilter);

            //////////////////////////////// PDX PARSER SECTION //////////////////////////////////////////////////////////////////////

            string index_file = Directory.GetFiles(combinedPath, "index.xml", SearchOption.AllDirectories)
                    .FirstOrDefault();           
            
            //////////////////////////////// LOCAL INIT SECTION //////////////////////////////////////////////////////////////////////

            string[] binfilesExtracted = { "", "", "", "", "", "", "", "", "" }; 
            string[] binfilename = { "", "", "", "", "", "", "", "", "" };
            //string[] dataRecordStr = { "", "", "", "", "", "", "", "", "" };
            string[] binfilesPDX = { "", "", "", "", "", "", "", "", "" };
            string[] index_filelist = { "", "", "", "", "", "", "", "", "" };
            byte[][] dataRecordStr = new byte[8][];
            string[] LBindex = { "", "", "", "", "", "", "", "", "" };
            string[] digest = { "", "", "", "", "", "", "", "", "" };

            string[] srcAddr = { "", "", "", "", "", "", "", "", "" };
            string[] dataLen = { "", "", "", "", "", "", "", "", "" };
            string[] maxBlockLen = { "", "", "", "", "", "", "", "", "" };
            string odxffiles = "";

            XDocument index_xml = XDocument.Load(index_file);            
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var indexfiles = index_xml.Descendants("FILES")
                             .First()
                             .Descendants();

            //Loop through results
            int index_loop = 0;
            int bin_loop = 0;
            foreach (XElement elem in indexfiles)
            {                
                index_filelist[index_loop] = elem.Value.ToString();
                Console.WriteLine("{0}", index_filelist[index_loop]);
                if(Path.GetExtension(index_filelist[index_loop]) != ".bin")
                    odxffiles = System.IO.Directory.GetFiles(combinedPath, index_filelist[index_loop], SearchOption.AllDirectories).FirstOrDefault();
                else {
                    binfilesExtracted[bin_loop] = System.IO.Directory.GetFiles(combinedPath, index_filelist[index_loop], SearchOption.AllDirectories).FirstOrDefault();
                    bin_loop++;
                }                    
                index_loop++;
            }                            
            bool has_indexFile = index_filelist.Contains(Path.GetFileName(odxffiles));

            XDocument xml = XDocument.Load(odxffiles);
            var result = xml.Descendants("SESSION")
                         .Where(x => (string)x.Element("SHORT-NAME") == "TUNING")
                         .Descendants("IDENT-VALUES")
                         .Elements("IDENT-VALUE")
                         .Select(refA => new
                         {
                             Header = (string)refA.Value
                         });

            //Loop through results
            foreach (var refA in result)
            {
                Console.WriteLine("{0}", refA.Header);
            }

            //Iterate every DATABLOCK Element
            var DBs = (from DB in xml.Descendants("DATABLOCK")
                       select new
                       {
                           Header = DB.Element("LOGICAL-BLOCK-INDEX").Value,
                           Children1 = DB.Descendants("SEGMENT"),
                           Children2 = DB.Descendants("OWN-IDENT"),
                           Children3 = DB.Descendants("SDG")
                       }).ToList();
            
            //Loop through results
            int i = 0;
            foreach (var DB in DBs)
            {
                LBindex[i] = DB.Header;
                foreach (var seg in DB.Children1)
                {
                    srcAddr[i] = seg.Element("SOURCE-START-ADDRESS").Value;
                    dataLen[i] = seg.Element("UNCOMPRESSED-SIZE").Value;
                }
                foreach (var ident in DB.Children2)
                {
                    digest[i] = ident.Element("IDENT-VALUE").Value;
                }
                foreach (var maxLen in DB.Children3)
                {
                    maxBlockLen[i] = maxLen.Element("SD").Value;
                }
                i++;
            }

            //Iterate every FlashData Element
            var FDs = (from FD in xml.Descendants("FLASHDATA")
                        select new
                        {
                            Header = (FD.Attribute(xsi + "type").Value == "INTERN-FLASHDATA") ? (FD.Element("DATA").Value) : (FD.Element("DATAFILE").Value),
                            //Header = FD.Element("DATA").Value,
                            //Children = FD.Descendants("FLASHDATA")
                            //Header2 = FD.Element("DATAFILE").Value
                        }).ToList();
            //Loop through results
            int j = 0;
            foreach (var FD in FDs)
            {
                binfilesPDX[j] = FD.Header;
                j++;
            }
            int DRlistindex = 0;
            foreach (string binf in binfilesExtracted)
            {
                if(binfilesPDX.Contains(Path.GetFileName(binf)))
                {
                    dataRecordStr[DRlistindex] = File.ReadAllBytes(binf);
                    binfilename[DRlistindex] = Path.GetFileName(binf);
                    binfilename[DRlistindex] = binfilename[DRlistindex].Trim();
                    System.Console.WriteLine("Total dataRecordStr len: {0} for file:  {1}", dataRecordStr[DRlistindex].Length, binfilename[DRlistindex]);
                    DRlistindex++;
                }                    
            }
            bool subset_result = index_filelist.Any(binfilename.Contains);
            bool isbinFilesEqual = binfilesPDX.SequenceEqual(binfilename);
            if (isbinFilesEqual)
                Console.WriteLine("All files in folder are matching with PDX spec");
            else
                Console.WriteLine("some files are missing in PDX spec or in PDX file");
            for (int test = 0; test < NOofBlock; test++)
            {
                List<byte> WriteDigestByteArray = new List<byte>();
                WriteDigestByteArray.Add(0x2E);
                WriteDigestByteArray.AddRange(Helper.stringToByteArray(LBindex[test]));
                WriteDigestByteArray.AddRange(Helper.stringToByteArray(digest[test]));
                diagRequests.Add(WriteDigestByteArray);
            }

            ////////////////////////////////TRANSFER DATA SECTION//////////////////////////////////////////////////////////////////////
            //part2.1- send request download z.B: 34 00 33 xx xx xx yy yy yy and adjust the block size for 2 bytes header and tail bytes to be added
            //part2.2- send transfer data/transfer exit with Sq.Ctr based on maxNumberOfBlockLength from PDX file and binary data.

            for (int iy = 0; iy < NOofBlock; iy++)
            {
                List<byte> ReqDownloadByteArray = new List<byte>();
                uint blockCounterSize = Convert.ToUInt32(maxBlockLen[iy], 16);
                uint totalSize = 0;
                if (iy == 0)
                    totalSize = 0xA;
                else
                    totalSize = Convert.ToUInt32(dataRecordStr[iy-1].Length);//Convert.ToUInt32(dataLen[i]);
                int totalBlocks = 0;
                
                if (blockCounterSize != 0)
                {
                    totalBlocks = (int)Math.Ceiling((float)totalSize / (float)blockCounterSize);
                    int remainder = (int)totalSize % (int)blockCounterSize;
                    //special case where the 2bytes of header cannot accomodate the existing record size
                    if ((uint)(remainder + (totalBlocks * 2)) > blockCounterSize)
                    {
                        totalBlocks++;
                    }
                }

                ReqDownloadByteArray.Add(0x34);
                ReqDownloadByteArray.Add(0x00);
                ReqDownloadByteArray.Add(0x33);
                ReqDownloadByteArray.AddRange(Helper.stringToByteArray(srcAddr[iy]));
                byte[] tmp = new byte[3];
                string tempLen = dataRecordStr[iy].Length.ToString();
                Array.Copy(BitConverter.GetBytes(Int32.Parse(tempLen)), tmp, 3);
                ReqDownloadByteArray.AddRange(tmp.Reverse());
                diagRequests.Add(ReqDownloadByteArray);

                List<byte> stopDownloadRequestByteArray = new List<byte> { 0x37 };

                List<byte> dataRecordBytes = new List<byte>();

                System.Console.WriteLine("req download block counter size: {0:x} , total size:  {1:X} , total available blocks: {2:X}", blockCounterSize, totalSize, totalBlocks);
                dataRecordBytes = File.ReadAllBytes(binfilesExtracted[iy]).ToList();
                int transferredDataSize = 0;
                for (byte bNr = 1; bNr <= totalBlocks; bNr++)   
                {
                    List<byte> TransDataByteArray = new List<byte>();
                    TransDataByteArray.Add(0x36);
                    TransDataByteArray.Add(bNr);
                    if (bNr == (byte)totalBlocks)
                    {
                        if (iy == 0)
                        {
                            TransDataByteArray.AddRange(dataRecordBytes);
                        }
                        else
                        {
                            TransDataByteArray.AddRange(dataRecordBytes.GetRange((int)transferredDataSize, (int)(totalSize - transferredDataSize)));
                        }
                        transferredDataSize += (int)(totalSize - transferredDataSize);
                    }
                    else
                    {
                        TransDataByteArray.AddRange(dataRecordBytes.GetRange((int)transferredDataSize, (int)(blockCounterSize - 2)));
                        transferredDataSize += (int)(blockCounterSize - 2);
                    }
                    diagRequests.Add(TransDataByteArray);

                    if (transferredDataSize == totalSize)
                    {
                        diagRequests.Add(stopDownloadRequestByteArray);
                    }
                    System.Console.WriteLine("transferred data size: {0} ,bNr:  {1}", transferredDataSize, bNr);
                }
            }

            //StreamWriter file = new System.IO.StreamWriter(Path.Combine(combinedPath, "dumpFile.txt"));
            //diagRequests.ForEach(file.WriteLine); 
            int save2File = 0;
            StreamWriter sw = new StreamWriter(Path.Combine(combinedPath, "dumpFile.txt"));
            for (save2File = 0; save2File <= (diagRequests.Count - 1); save2File++)
            {
                // Writer raw data      
                string hex = BitConverter.ToString(diagRequests[save2File].ToArray()).Replace("-", "");
                sw.WriteLine(hex);
            }
            sw.Flush();
            sw.Close();
            int read2File = 0;
            StreamReader sr = new StreamReader(Path.Combine(combinedPath, "dumpFile.txt"));
            string str = "";           
            while ((str = sr.ReadLine()) != null)
            {
                // Writer raw data                                    
                List<byte> DigestByteArray = new List<byte>();
                DigestByteArray.AddRange(Helper.stringToByteArray(str));
                diagRequests.Add(DigestByteArray);
                read2File++;
            }
            Console.WriteLine("Total lines in file: {0}", read2File);
            sr.Close();
            Process.Start("notepad++.exe", combinedPath + "\\dumpFile.txt");

            //////////////////////////////// BIN FILE COPY SECTION //////////////////////////////////////////////////////////////////////

            /*  XmlTextReader reader = new XmlTextReader(file);
                reader.Read();

                int filereadix = 0;
                while (reader.Read())
                {
                    if (reader.Name == "DATA")
                    {
                        binfilesPDX[filereadix] = reader.ReadString();
                        binfilesPDX[filereadix] = binfilesPDX[filereadix].Trim();
                        System.Console.WriteLine("datafile value:  {0}", binfilesPDX[filereadix]);
                        filereadix++;
                    }

                    if (reader.Name == "DATAFILE")
                    {
                        binfilesPDX[filereadix] = reader.ReadString();
                        binfilesPDX[filereadix] = binfilesPDX[filereadix].Trim();
                        System.Console.WriteLine("datafile list:  {0}", binfilesPDX[filereadix]);
                        filereadix++;
                    }
                }

                //for LB0, copy the file contents same as the PDX file
                int DRlistindex = 0;
                dataRecordStr[DRlistindex] = binfilesPDX[DRlistindex];
                binfilename[DRlistindex] = binfilesPDX[DRlistindex];
                DRlistindex++;
                foreach (string binf in binfilesExtracted)
                {
                    dataRecordStr[DRlistindex] = File.ReadAllText(binf);
                    binfilename[DRlistindex] = Path.GetFileName(binf);
                    binfilename[DRlistindex] = binfilename[DRlistindex].Trim();
                    System.Console.WriteLine("Total dataRecordStr len: {0} for file:  {1}", dataRecordStr[DRlistindex].Length, binfilename[DRlistindex]);
                    DRlistindex++;
                }

                bool isEqual = binfilesPDX.SequenceEqual(binfilename); ;

                if (isEqual)
                    Console.WriteLine("All files in folder are matching with PDX spec");
                else
                    Console.WriteLine("some files are missing in PDX spec or in PDX file");

                ////////////////////////////////WRITE DIGEST SECTION//////////////////////////////////////////////////////////////////////
                //part1- extracts digest using xmltextreader and index to send write digest command z.B: 2E FD xx

                XmlTextReader reader1 = new XmlTextReader(file);
                reader1.Read();
              
                   int LB_index = 0;
                   int digest_index = 0;
                   while (reader1.Read())
                   {
                       if (reader1.Name == "LOGICAL-BLOCK-INDEX")
                       {
                           LBindex[LB_index] = reader1.ReadString();
                           LB_index++;
                       }

                       if ((reader1.Name == "IDENT-VALUE") && (reader1.GetAttribute("TYPE") == "A_BYTEFIELD"))
                       {
                           digest[digest_index] = reader1.ReadString();
                           digest_index++;
                       }
                   }

                   for (int i = 0; i < NOofBlock; i++)
                   {
                       List<byte> WriteDigestByteArray = new List<byte>();
                       WriteDigestByteArray.Add(0x2E);
                       WriteDigestByteArray.AddRange(Helper.stringToByteArray(LBindex[i]));
                       WriteDigestByteArray.AddRange(Helper.stringToByteArray(digest[i]));
                       diagRequests.Add(WriteDigestByteArray);
               }


               ////////////////////////////////TRANSFER DATA SECTION//////////////////////////////////////////////////////////////////////
               //part2.1- extracts SrcAddr and size to send request download z.B: 34 00 33 xx xx xx yy yy yy
               //part2.2- send transfer data/transfer exit with Sq.Ctr based on maxNumberOfBlockLength from PDX file and binary data.

               XmlTextReader reader2 = new XmlTextReader(file);
               reader2.Read();

               int addr_index = 0;
               int len_index = 0;
               int maxBlockLen_index = 0;

               while (reader2.Read())
               {
                   if (reader2.Name == "SOURCE-START-ADDRESS")
                   {
                       // Value is in hexadecimal format
                       srcAddr[addr_index] = reader2.ReadString();
                       addr_index++;
                   }

                   if (reader2.Name == "UNCOMPRESSED-SIZE")
                   {
                       // Value is in decimal format
                       dataLen[len_index] = reader2.ReadString();
                       len_index++;
                   }

                   if ((reader2.Name == "SD") && (reader2.GetAttribute("SI") == "maxNumberOfBlockLength"))
                   {
                       // Value is in hexadecimal format
                       maxBlockLen[maxBlockLen_index] = reader2.ReadString();
                       maxBlockLen_index++;
                   }
               }

               for (int i = 0; i < NOofBlock; i++)
               {
                   List<byte> ReqDownloadByteArray = new List<byte>();
                   uint blockCounterSize = Convert.ToUInt32(maxBlockLen[i], 16); ;
                   uint totalSize = Convert.ToUInt32(dataRecordStr[i].Length);//Convert.ToUInt32(dataLen[i]);
                   int totalBlocks = 0;

                   if ((blockCounterSize != 0) && (i != 0))
                   {
                       totalBlocks = (int)Math.Ceiling((float)totalSize / (float)blockCounterSize);
                       int remainder = (int)totalSize % (int)blockCounterSize;
                       //special case where the 2bytes of header cannot accomodate the existing record size
                       if ((uint)(remainder + (totalBlocks * 2)) > blockCounterSize)
                       {
                           totalBlocks++;
                       }
                   }

                   ReqDownloadByteArray.Add(0x34);
                   ReqDownloadByteArray.Add(0x00);
                   ReqDownloadByteArray.Add(0x33);
                   ReqDownloadByteArray.AddRange(Helper.stringToByteArray(srcAddr[i]));
                   ReqDownloadByteArray.AddRange(BitConverter.GetBytes(Int32.Parse(dataLen[i])).Reverse().ToArray());
                   diagRequests.Add(ReqDownloadByteArray);

                   List<byte> stopDownloadRequestByteArray = new List<byte> { 0x37 };

                   System.Console.WriteLine("req download block counter size: {0:x} , total size:  {1:X} , total available blocks: {2:X}", blockCounterSize, totalSize, totalBlocks);

                   if (i == 0)
                   {
                       List<byte> TransDataByteArray = new List<byte>();
                       List<byte> dataRecordBytes = Helper.stringToByteArray(dataRecordStr[i]);
                       TransDataByteArray.Add(0x36);
                       TransDataByteArray.Add(0x01);
                       TransDataByteArray.AddRange(dataRecordBytes);
                       diagRequests.Add(TransDataByteArray);
                       diagRequests.Add(stopDownloadRequestByteArray);
                   }
                   else
                   {
                       List<byte> dataRecordBytes = File.ReadAllBytes(binfilesExtracted[i - 1]).ToList();
                       int transferredDataSize = 0;
                       for (byte bNr = 1; bNr <= totalBlocks; bNr++)
                       {
                           List<byte> TransDataByteArray = new List<byte>();
                           TransDataByteArray.Add(0x36);
                           TransDataByteArray.Add(bNr);
                           if (bNr == (byte)totalBlocks)
                           {
                               TransDataByteArray.AddRange(dataRecordBytes.GetRange((int)transferredDataSize, (int)(totalSize - transferredDataSize)));
                               transferredDataSize += (int)(totalSize - transferredDataSize);
                           }
                           else
                           {
                               TransDataByteArray.AddRange(dataRecordBytes.GetRange((int)transferredDataSize, (int)(blockCounterSize - 2)));
                               transferredDataSize += (int)(blockCounterSize - 2);
                           }
                           diagRequests.Add(TransDataByteArray);

                           if (transferredDataSize == totalSize)
                           {
                               diagRequests.Add(stopDownloadRequestByteArray);
                           }
                           System.Console.WriteLine("transferred data size: {0} ,bNr:  {1}", transferredDataSize, bNr);
                       }
                   }
               }*/
        }
    }

    public class Helper
    {
        public static List<byte> stringToByteArray(string hexArrayStr) // Expected format: "2E 31 00 ...."
        {
            byte[] byteArray = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            try
            {
                byteArray = Enumerable.Range(0, hexArrayStr.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexArrayStr.Substring(x, 2), 16))
                             .ToArray();
                Console.WriteLine("{0} converts to bytearray.", hexArrayStr);
            }
            catch (OverflowException)
            {
                Console.WriteLine("Unable to convert '0x{0:X}' to a byte.", hexArrayStr);
            }

            List<byte> list = new List<Byte>(byteArray);
            list = byteArray.ToList();
            foreach (var b in list)
            {
                Console.WriteLine("list element: '0x{0:X}' as a byte.", b);
            }
            return list;
        }

        /*public static List<byte> stringToByteArray(string hexArrayStr) // Expected format: "2E 31 00 ...."
        {
            string[] singleByteStrings = hexArrayStr.Split(' ');
            List<byte> list = new List<byte>(singleByteStrings.Length);

            foreach (string numb in singleByteStrings)
            {
                list.Add(Convert.ToByte(numb, 16));
            }

            return list;
        }*/
    }
}