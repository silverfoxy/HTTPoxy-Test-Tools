# HTTPoxy-Test-Tools

I'm developing tools to test for <a href="https://httpoxy.org/">HTTPoxy vulnerability</a>.
The tool finds CGI directory, adds a temporary file that returns the HTTP_PROXY environment variable. The script then sends a GET request to this CGI file and sets the "proxy" header.
If the environment variable is affected, then you're vulnerable.

<b>apache_httpoxy.py</b>

apache_httpoxy.py Checks for this vulnerability on Apache web servers.

<b>Dependencies:</b>

os, urllib2, argparse

<b>Usage</b>

usage: apache_httpoxy.py [-h] [-b] [-c CONF]

optional arguments:
  -h, --help            show this help message and exit
  -b, --boolean         Script returns 1 if server is vulnerable, 0 if server
                        is not vulnerable
  -c CONF, --config CONF
                        Enter httpd.conf address
                        
<b>Sample Output</b>

$sudo python apache_httpoxy.py

[+] Initiating Test

[?] Enter httpd.conf address: [Default: /etc/httpd/conf/httpd.conf]

[+] httpd.conf address was set to /etc/httpd/conf/httpd.conf

[+] Reading CGI-Directory Address from httpd.conf

[+] CGI-Directory was set to /var/www/cgi-bin/

[+] Initiating TestSuite

[+] Creating CGI File

[+] Setting Permissions

[+] Running Tests

[+] Sending Get Request to http://127.0.0.1/cgi-bin/httpoxy-test-file.py with proxy header set to 10.10.10.10

[+] Testing proxy in response

[+] Proxy was set in response

[-] ===== Server Vulnerable =====

[+] Cleaning up

[+] Done
