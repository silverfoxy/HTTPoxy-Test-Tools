using System;
using Microsoft.Win32;
using System.IO;
using System.Net;

namespace iis_httpoxy
{
    class Program
    {
        static string WebApplicationDirectory = "C:\\inetpub\\wwwroot\\iis-cgi-test";
        static string CGIFilecs = "iis-cgi-test.cs";
        static string CGIFileexe = "iis-cgi-test.exe";

        static void Main(string[] args)
        {
            //[+] Testing for CGI module presence
            bool CGIEnabled = GetCGIStatus();
            //[+] CGI is enabled
            //[-] CGI not enabled
            //[-] === Server Not Vulnerable ===

            //Create folders
            Directory.CreateDirectory(WebApplicationDirectory);

            CreateCGIFile(WebApplicationDirectory, CGIFilecs);

            SetupCGIFile(WebApplicationDirectory, CGIFileexe);

            bool vulnerable = TestProxyHeader();
            if (vulnerable)
            {
                Console.WriteLine("Vulnerable");
            }
            else
            {
                Console.WriteLine("Not Vulnerable");
            }
            
            //run test
            //send request set proxy
            //read proxy
            //vulnerable or not
            //clean up
            //done


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

        private static void CreateCGIFile(string address, string filename)
        {
            //Create .cs file
            var content = @"using System; 
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
            var file = new StreamWriter(address + "\\" + filename);
            file.WriteLine(content);
            file.Close();

            //Compile .cs
            RunCommand("%windir%\\Microsoft.NET\\Framework\\v2.0.50727\\csc.exe " + filename);
        }

        private static void SetupCGIFile(string address, string filename)
        {
            //Add website
            RunCommand(@"%windir%\system32\inetsrv\appcmd add site /name:""IIS-CGI-Test"" /bindings:http://:12345 /physicalPath:""c:\inetpub\wwwroot\iis-cgi-test""");
            //Add CGI Rules
            RunCommand(@"%windir%\system32\inetsrv\appcmd set config -section:isapiCgiRestriction /+[path='c:\inetpub\wwwroot\iis-cgi-test\iis-cgi-test.exe',allowed='true',description='iis-cgi-test']");
            //Add Permissions
            RunCommand(@"%windir%\system32\inetsrv\appcmd set config ""IIS-CGI-Test"" /section:handlers -accessPolicy:""Read, Script, Execute""");

        }

        private static void RunCommand(string arguments)
        {
            var process = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = arguments
            };
            process.StartInfo = startInfo;
            process.Start();
        }

        private static bool TestProxyHeader()
        {
            string html = string.Empty;
            string url = @"http://127.0.0.1/IIS-CGI-Test/" + CGIFileexe;

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
    }
}
