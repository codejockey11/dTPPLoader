using System;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace dTPPLoader
{
    public class ZipNames
    {
        public String pdf;
        public String zip;

        public ZipNames(String p, String z)
        {
            pdf = p;
            zip = z;
        }
    }

    public class CompareZipNames : IComparer<ZipNames>
    {
        public int Compare(ZipNames a, ZipNames b)
        {
            return String.Compare(a.pdf, b.pdf);
        }
    }

    class Program
    {
        static XmlTextReader textReader;

        static String ident { get; set; }
        static String alnum { get; set; }
        static String chartCode { get; set; }
        static String chartName { get; set; }
        static String pdfName { get; set; }

        static void Main(String[] args)
        {
            String userprofileFolder = Environment.GetEnvironmentVariable("USERPROFILE");
            
            String[] fileEntries = Directory.GetFiles(userprofileFolder + "\\Downloads\\", "DDTPPE_*.zip");

            StreamWriter dTPP = new StreamWriter("dTPP.txt", false);

            StreamWriter compares = new StreamWriter("compares.txt", false);

            ZipArchive archive = ZipFile.OpenRead(fileEntries[0]);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Name.Length > 0)
                {
                    String s = entry.Name.Substring(entry.Name.Length - 3, 3);

                    if (String.Compare(entry.Name, "d-TPP_Metafile.xml") == 0)
                    {
                        entry.ExtractToFile("dTPP.xml", true);
                    }
                }
            }


            CompareZipNames zipNameCompare = new CompareZipNames();

            List<ZipNames> compareNames = new List<ZipNames>();

            archive = ZipFile.OpenRead(fileEntries[0]);

            String[] filenames = fileEntries[0].Split('\\');

            String fn = filenames[filenames.Length - 1];

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Name.Contains(".PDF"))
                {
                    ZipNames z = new ZipNames(entry.Name.Replace("_CMP", ""), fn);

                    compareNames.Add(z);
                }
            }

            compareNames.Sort(zipNameCompare);

            List<ZipNames> zipNames = new List<ZipNames>();

            fileEntries = Directory.GetFiles(userprofileFolder + "\\Downloads\\", "DDTPP*_*.zip");

            foreach (String filename in fileEntries)
            {
                archive = ZipFile.OpenRead(filename);

                filenames = filename.Split('\\');

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    fn = filenames[filenames.Length - 1];

                    if (fn.Contains("DDTPPE"))
                    {
                        // skipping compares
                    }
                    else
                    {
                        ZipNames z = new ZipNames(entry.Name, fn);

                        zipNames.Add(z);
                    }
                }
            }

            zipNames.Sort(zipNameCompare);

            try
            {
                textReader = new XmlTextReader("dTPP.xml");

                Int32 c = 0;

                while (textReader.Read())
                {
                    if (textReader.NodeType == XmlNodeType.Element)
                    {
                        if (String.Compare(textReader.Name, "airport_name") == 0)
                        {
                            c = 0;
                            ident = textReader.GetAttribute("apt_ident");
                            
                            dTPP.Write(ident);
                            dTPP.Write("~");
                            
                            Int32 i = Convert.ToInt32(textReader.GetAttribute("alnum"));
                            
                            alnum = i.ToString("D5");
                            
                            dTPP.Write(alnum);
                            dTPP.Write("~");
                        }

                        if (String.Compare(textReader.Name, "chart_code") == 0)
                        {
                            if (c > 0)
                            {
                                dTPP.Write(ident);
                                dTPP.Write("~");
                                
                                dTPP.Write(alnum);
                                dTPP.Write("~");
                            }

                            chartCode = textReader.ReadElementContentAsString();
                            
                            dTPP.Write(chartCode);
                            dTPP.Write("~");
                        }

                        if (String.Compare(textReader.Name, "chart_name") == 0)
                        {
                            chartName = textReader.ReadElementContentAsString();

                            dTPP.Write(chartName);
                            dTPP.Write("~");
                        }

                        if (String.Compare(textReader.Name, "pdf_name") == 0)
                        {
                            pdfName = textReader.ReadElementContentAsString();
                            
                            dTPP.Write(pdfName);
                            dTPP.Write("~");

                            ZipNames zipName = new ZipNames(pdfName, "");

                            int x = zipNames.BinarySearch(zipName, zipNameCompare);

                            if (x >= 0)
                            {
                                dTPP.Write(zipNames[x].zip);
                            }
                            else
                            {
                                dTPP.Write("X");
                            }
                            
                            dTPP.Write("\r\n");

                            x = compareNames.BinarySearch(zipName, zipNameCompare);
                            
                            if (x >= 0)
                            {
                                compares.Write(ident);
                                compares.Write("~");

                                compares.Write(alnum);
                                compares.Write("~");

                                compares.Write(chartCode);
                                compares.Write("~");

                                compares.Write(chartName);
                                compares.Write("~");

                                compares.Write(pdfName.Replace(".PDF", "_CMP.PDF"));
                                compares.Write("~");

                                compares.Write(compareNames[x].zip);
                                compares.Write("\r\n");
                            }

                            c++;
                        }
                    }
                }

                textReader.Close();
                
                dTPP.Close();

                compares.Close();

            }
            catch (FileNotFoundException se)
            {
                Console.WriteLine(se.Message);
            }

        }
    }
}
