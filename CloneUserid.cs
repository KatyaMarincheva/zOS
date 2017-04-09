using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace CloneUserid
{
    class CloneUserid
    {
        static void Main(string[] args)
        {
            // get the result from the LISTUSER MODELID TSO OMVS command
            string listModelUser = File.ReadAllText(@"modelid.txt");

            // get the new userid
            Console.WriteLine("Please enter the new userid: ");
            string newUserid = Console.ReadLine();

            MatchCollection matches;

            // create the new commands.txt REXX file
            using (StreamWriter command = new StreamWriter(@"commands.txt"))
            {
                command.WriteLine("/*REXX*/");

                // create command to add the new userid
                string adduPattern = @"USER\=[^\s]+\s+NAME=([^\s]+)\s*OWNER=([^\s]+)\s+.+\s+DEFAULT-GROUP=([^\s]+)";
                matches = ExtractInfo(listModelUser, adduPattern);

                foreach (Match m in matches)
                {
                    command.WriteLine(String.Format("ADDUSER {0} NAME('{1}') OWNER({2}) DFLTGRP({3}) PASSWORD(PASS)",
                        newUserid, m.Groups[1], m.Groups[2], m.Groups[3]));
                }

                // create command to add the TSO segment
                string TSOPattern = @"(?<!NO\s)TSO INFORMATION\s+-+\s+ACCTNUM=\s+(\w+)\s+PROC=\s+(\w+)\s+SIZE=\s+(\w+)\s+MAXSIZE=\s+(\w+)\s+UNIT=\s+(\w+)\s+USERDATA=\s+(\w+)";
                matches = ExtractInfo(listModelUser, TSOPattern);

                foreach (Match m in matches)
                {
                    command.WriteLine(String.Format("TSO(ACCTNUM({0}) PROC({1}) SIZE({2}) MAXSIZE({3}) UNIT({4}) USERDATA({5}))",
                        m.Groups[1], m.Groups[2], m.Groups[3], m.Groups[4], m.Groups[5], m.Groups[6]));
                }

                // create command to add the OMVS segment
                string OMVSPattern = @"(?<!NO\s)OMVS INFORMATION\s+-+\s+.+\s+HOME=\s(\S+)\s+PROGRAM=\s(\S+)\s+CPUTIMEMAX=\s(\S+)\s+ASSIZEMAX=\s(\S+)\s+FILEPROCMAX=\s(\S+)\s+PROCUSERMAX=\s(\S+)\s+THREADSMAX=\s(\S+)\s+MMAPAREAMAX=\s([0-9]+)";
                matches = ExtractInfo(listModelUser, OMVSPattern);

                foreach (Match m in matches)
                {
                    command.WriteLine(String.Format("OMVS(autouid HOME({0}) PROGRAM({1}) CPUTIMEMAX({2}) ASSIZEMAX({3}) FILEPROCMAX({4}) PROCUSERMAX({5}) THREADSMAX({6}) MMAPAREAMAX({7}))",
                        m.Groups[1], m.Groups[2], m.Groups[3], m.Groups[4], m.Groups[5], m.Groups[6], m.Groups[7], m.Groups[8]));
                }

                // create commands to connect the userid to all groups in which the modelid is a member
                string grPattern = @"(?<!DEFAULT-)GROUP=([A-Z]+)\s+AUTH=([A-Z]+)\s+CONNECT-OWNER=([A-Z]+)\s+.+\s+.+UACC=([A-Z]+)\s+.+\s+CONNECT ATTRIBUTES=([A-Z]+)";
                matches = ExtractInfo(listModelUser, grPattern);

                foreach (Match m in matches)
                {
                    command.WriteLine(String.Format("CONNECT {0} GROUP({1}) AUTHORITY({2}) OWNER({3}) UACC({4}) AUTHORITY({5})",
                        newUserid, m.Groups[1], m.Groups[2], m.Groups[3], m.Groups[4], m.Groups[5]));
                }

                command.WriteLine("exit");
            }

            // open the commands.txt REXX file
            Process.Start(@"commands.txt");

            // check modelid for privileged access ATTRIBUTES, and worn that those have to be added separately to the new userid
            string attrPattern = @"ATTRIBUTES=((?!NONE).*)";
            matches = ExtractInfo(listModelUser, attrPattern);

            if (matches.Count > 0)
            {
                Console.WriteLine(@"Please note that the modelid has got the following privileged access ATTRIBUTES,
which have to be added separately: ");
                foreach (Match m in matches)
                {
                    Console.WriteLine(m.Groups[1]);
                }
		Console.ReadLine();
            }
        }

        // extract modelid info
        private static MatchCollection ExtractInfo(string modelInfo, string pattern)
        {
            Regex rgx = new Regex(pattern);
            MatchCollection matches = rgx.Matches(modelInfo);
            return matches;
        }
    }
}
