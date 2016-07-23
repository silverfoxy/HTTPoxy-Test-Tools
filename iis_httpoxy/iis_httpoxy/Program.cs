using System;
using Microsoft.Win32;
using System.IO;
using System.Net;
using System.Text;
using CommandLine;

namespace iis_httpoxy
{
    class Program
    {
        static string WebApplicationDirectory = "C:\\inetpub\\iis-cgi-test";
        static string CGIFilecs = "iis-cgi-test.cs";
        static string CGIFileexe = "iis-cgi-test.exe";
        static string winpath = Environment.GetEnvironmentVariable("windir");

        private static bool Verbose = true;
        private static bool BooleanOutput = false;

        private static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                BooleanOutput = options.Boolean;
                Verbose = !BooleanOutput;
            }
            else if (args.Length > 0)
            {
                return;
            }

            //[+] Testing for CGI module presence
            bool CGIEnabled = GetCGIStatus();
            //[+] CGI is enabled
            //[-] CGI not enabled
            //[-] === Server Not Vulnerable ===
            if (!CGIEnabled)
            {
                VPrint("[-] CGI Not Enabled");
                VPrint("[-] Server Not Vulnerable");
                BPrint("0");
                return;
            }
            //Create folders
            VPrint("[+] Creating CGI Directory");
            Directory.CreateDirectory(WebApplicationDirectory);

            VPrint("[+] Creating CGI Files");
            CreateCGIFile(WebApplicationDirectory, CGIFilecs, CGIFileexe);

            VPrint("[+] Setting Up CGI Application");
            SetupCGIFile(WebApplicationDirectory, CGIFileexe);

            bool vulnerable;
            try
            {
                VPrint("[+] Sending Request");
                vulnerable = TestProxyHeader();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("404"))
                {
                    vulnerable = false;
                    goto Blocked;
                }
                VPrint("[-] Couldn't connect to application");
                VPrint("[-] Test Failed");
                Cleanup();
                BPrint("-1");
                VPrint("[+] Done");
                return;
            }

            Blocked:

            VPrint(vulnerable ? "[+] Proxy was set in response" : "[-] Proxy was not set in response");
            VPrint(vulnerable ? "[-] ===== Server Vulnerable =====" : "[+] ===== Server Not Vulnerable =====");
            Cleanup();
            BPrint(vulnerable ? "1" : "0");
            VPrint("[+] Done");
        }

        private static void VPrint(string text)
        {
            if (Verbose)
            {
                Console.WriteLine(text);
            }
        }

        private static void BPrint(string text)
        {
            if (BooleanOutput)
            {
                Console.WriteLine(text);
            }
        }

        private static bool GetCGIStatus()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\InetStp\\Components"))
            {
                var o = key?.GetValue("CGI");
                if (o != null)
                {
                    return (o.ToString() != "0");
                }
                return false;
            }
        }

        private static void CreateCGIFile(string address, string sourceFilename, string destinationFilename)
        {
            //Create .cs file
            const string content = @"using System; 
                          using System.Collections;

                          class SimpleCGI
                          {
                                static void Main(string[] args)
                                {
                                    Console.WriteLine(""\r\n\r\n"");
                                    Console.WriteLine(""<h1>IIS HTTPoxy Test</h1>"");
                                    foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
                                        if (var.Key as string == ""HTTP_PROXY"")
                                        {
                                            Console.WriteLine(""<hr><b>{0}</b>: {1}"", var.Key, var.Value);
                                        }
                                }
                          }";
            var file = new StreamWriter(address + "\\" + sourceFilename);
            file.WriteLine(content);
            file.Close();

            //Compile .cs
            var sourceFile = address + "\\" + sourceFilename;
            var destinationFile = address + "\\" + destinationFilename;
            RunCommand(winpath + @"\Microsoft.NET\Framework\v2.0.50727\csc.exe", "/out:" + destinationFile + " " + sourceFile);
            RunCommand(winpath + @"\Microsoft.NET\Framework\v2.0.50727\csc.exe", string.Format("/out:{0} {1}", destinationFile, sourceFile));
        }

        private static void SetupCGIFile(string address, string filename)
        {
            //Add website
            VPrint("[+] Creating Web Application");
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", string.Format(@"add site /name:""IIS-CGI-Test"" /bindings:http://:12345 /physicalPath:""{0}""", WebApplicationDirectory));
            //Add Application Pool
            VPrint("[+] Creating Application Pool");
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", @"add apppool /name:IIS-CGI-Test /managedRuntimeVersion:v2.0 /managedPipelineMode:Classic");
            //Set Application Pool Permissions
            VPrint("[+] Setting Up Application Pool Permissions");
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", @"set config /section:applicationPools /[name='IIS-CGI-Test'].processModel.identityType:LocalService ");
            //Add website to Application Pool
            VPrint("[+] Adding Web Application to Pool");
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", @"set app /app.name:IIS-CGI-Test/ /applicationPool:IIS-CGI-Test");
            //Add CGI Rules
            VPrint("[+] Setting Up CGI Rules");
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", string.Format(@"set config -section:isapiCgiRestriction /+[path='{0}\{1}',allowed='true',description='iis-cgi-test']", WebApplicationDirectory, CGIFileexe));
            //Add Permissions
            VPrint("[+] Setting Up CGI Permissions");
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", @"set config ""IIS-CGI-Test"" /section:handlers -accessPolicy:""Read, Script, Execute""");

        }

        private static void RunCommand(string processAddress, string arguments)
        {
            var process = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = processAddress,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo = startInfo;
            process.Start();
            System.Threading.Thread.Sleep(1000);
        }

        private static bool TestProxyHeader()
        {
            string html;
            var url = @"http://127.0.0.1:12345/" + CGIFileexe;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers["proxy"] = "10.10.10.10";

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            return html.Contains("10.10.10.10");
        }

        private static void Cleanup()
        {
            VPrint("[+] Cleaning Up");
            //Remove Application
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", @"delete app /appname:IIS-CGI-Test");
            //Remove Application Pool
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", @"delete apppool / apppool.name:IIS-CGI-Test");
            //Remove CGI Rules
            RunCommand(winpath + "\\system32\\inetsrv\\appcmd.exe", string.Format(@"set config -section:isapiCgiRestriction /-[path='{0}\{1}',allowed='true',description='iis-cgi-test']", WebApplicationDirectory, CGIFileexe));
            //Remove Directory
            RemoveDirectory(WebApplicationDirectory);
        }

        private static void RemoveDirectory(string address)
        {
            DirectoryInfo di = new DirectoryInfo(address);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }

    class Options
    {
        [Option('b', "boolean", Required = false,
          HelpText = "-b, --boolean Script returns 1 if server is vulnerable, 0 if server is not vulnerable")]
        public bool Boolean { get; set; }

        [HelpOption(HelpText = "-b, --boolean Script returns 1 if server is vulnerable, 0 if server is not vulnerable")]
        public string GetUsage()
        {
            var help = new StringBuilder();
            help.AppendLine("-b, --boolean Script returns 1 if server is vulnerable, 0 if server is not vulnerable");
            return help.ToString();
        }
    }
}
