using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TagLib;

namespace MusicLabeler
{
    class Program
    {
        static string _SourceDirectory = null;
        static string _FileExtension = null;
        static int _Success = 0;
        static int _Failure = 0;
        static List<string> _FailedFiles = new List<string>();

        static void Main(string[] args)
        {
            Console.WriteLine("");

            while (String.IsNullOrEmpty(_SourceDirectory))
            {
                Console.Write("Source directory       : ");
                _SourceDirectory = Console.ReadLine();
            }
             
            while (String.IsNullOrEmpty(_FileExtension))
            {
                Console.Write("File extension (mp3)   : ");
                _FileExtension = Console.ReadLine();
            }

            bool recurse = false;
            Console.Write("Recursive (true|false) : ");
            string recurseStr = Console.ReadLine();
            if (!String.IsNullOrEmpty(recurseStr))
            {
                try
                {
                    recurse = Convert.ToBoolean(recurseStr);
                }
                catch (Exception)
                {

                }
            }

            _SourceDirectory = _SourceDirectory.Replace("\\", "/"); 

            if (!_SourceDirectory.EndsWith("/")) _SourceDirectory += "/"; 

            // List<string> files = Directory.EnumerateFiles(_SourceDirectory).ToList();
            List<string> files = GetAllFiles(_SourceDirectory, recurse);

            Console.WriteLine("");
            Console.WriteLine("Found information for " + files.Count + " file(s)");
            Console.WriteLine("");

            foreach (string file in files)
            {
                string filenameAndPath = file;
                string filename = Path.GetFileName(filenameAndPath);
                string artist = null;
                string titleAndMix = null;
                string title = null;
                string mix = null;

                if (filename.StartsWith("._"))
                {
                    // MacOS extended attribute file
                    continue;
                }
                else
                {
                    Console.Write("  " + filename + ": ");
                }

                #region Rename

                if (filenameAndPath.Contains("–"))
                {
                    Console.Write("[rename] " + filename + " ");
                    string newName = filenameAndPath.Replace("–", "-");
                    byte[] fileData = System.IO.File.ReadAllBytes(filenameAndPath);
                    System.IO.File.Copy(filenameAndPath, filenameAndPath + ".bak");
                    System.IO.File.Delete(filenameAndPath);
                    System.IO.File.WriteAllBytes(newName, fileData);
                }

                #endregion

                #region Parse

                if (!filename.Contains("-"))
                {
                    Console.WriteLine("[failed] no '-' found");
                    _Failure++;
                    _FailedFiles.Add(filename);
                    continue;
                }

                string[] parts = filename.Split(new[] { '-' }, 2);
                artist = parts[0].Trim();
                titleAndMix = parts[1].Trim();
                titleAndMix = titleAndMix.Replace("." + _FileExtension, "");

                if (titleAndMix.Contains("(") && titleAndMix.Contains(")"))
                {
                    string[] titleAndMixParts = titleAndMix.Split(new[] { '(' }, 2);
                    title = titleAndMixParts[0].Trim();
                    mix = titleAndMixParts[1].Trim().Replace("(", "").Replace(")", "");
                }
                else
                {
                    title = titleAndMix;
                }

                #endregion

                #region Validate

                if (String.IsNullOrEmpty(artist))
                {
                    Console.WriteLine("[failed] unable to find artist, skipping");
                    _Failure++;
                    _FailedFiles.Add(filename);
                    continue;
                }

                if (String.IsNullOrEmpty(title))
                {
                    Console.WriteLine("[failed] unable to find title, skipping");
                    _Failure++;
                    _FailedFiles.Add(filename);
                    continue;
                }

                #endregion

                #region Apply-Tags

                TagLib.File tlFile = TagLib.File.Create(file);
                tlFile.Tag.Artists = new string[] { artist };
                tlFile.Tag.Performers = new string[] { artist };
                tlFile.Tag.AlbumArtists = new string[] { artist };

                if (!String.IsNullOrEmpty(mix))
                {
                    tlFile.Tag.Title = title + " (" + mix + ")";
                }
                else
                {
                    tlFile.Tag.Title = titleAndMix;
                }

                tlFile.Save();
                tlFile.Dispose();
                _Success++;

                #endregion

                Console.WriteLine("[success]");
            }

            Console.WriteLine("");
            Console.WriteLine("Finished");
            Console.WriteLine("  Success: " + _Success);
            Console.WriteLine("  Failure: " + _Failure);
            Console.WriteLine("");

            if (_FailedFiles != null && _FailedFiles.Count > 0)
            {
                Console.WriteLine("Failed Files:");
                foreach (string file in _FailedFiles) Console.WriteLine("  " + file);
                Console.WriteLine("");
            }
        }

        static List<string> GetAllFiles(string root, bool recurse)
        {
            List<string> ret = new List<string>();
            string pattern = "*." + _FileExtension;

            try
            {
                foreach (string f in Directory.GetFiles(root, pattern))
                {
                    if (!String.IsNullOrEmpty(f))
                    {
                        if (f.StartsWith(root + "._"))
                        {
                            // MacOS extended attribute file
                            continue;
                        }

                        ret.Add(f);
                    }
                }

                foreach (string d in Directory.GetDirectories(root))
                { 
                    if (recurse) ret.AddRange(GetAllFiles(d, recurse));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return ret;
        }
    }
}
