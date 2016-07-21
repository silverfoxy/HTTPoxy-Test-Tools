#!/usr/bin/python

import os, urllib2, argparse

class ApacheConfigParser :
	CGI_CONFIG_PATTERN = 'ScriptAlias /cgi-bin/'
	def __init__(self, filename) :
		self.config_file = filename
	def get_cgi_dir(self) :
		with open(self.config_file) as conf :
			for line in conf :
				if self.CGI_CONFIG_PATTERN in line :
					return line.split()[2].replace('"', '')

class TestSuite :
	def __init__(self, cgi_dir) :
		self.cgi_dir = cgi_dir
	def create_test_file(self, filename='httpoxy-test-file.py') :
		self.filename = filename
		test_file = open(self.cgi_dir + self.filename, 'w+')
		test_file.write('#!/usr/bin/python\n')
		test_file.write('import os\n')
		content_type = 'print "Content-Type: text/html\n"'
		test_file.write(repr(content_type).replace('\'', '') + '\n')
		test_file.write('print os.environ.get(\'HTTP_PROXY\')\n')
	def set_permissions(self) :
		os.chmod(self.cgi_dir + self.filename, 0755)
	def run_test(self) :
		vprint('[+] Sending Get Request to http://127.0.0.1/cgi-bin/' + self.filename + ' with proxy header set to 10.10.10.10')
		request = urllib2.Reqeust('http://127.0.0.1/cgi-bin/' + self.filename, headers = {'proxy': '10.10.10.10'})
		response = urllib2.urlopen(request).read()
		vprint('[+] Testing proxy in response')
		if '10.10.10.10' in response :
			vprint('[+] Proxy was set in response')
			vprint('[-] ===== Server Vulnerable =====')
			bprint(1)
		else :
			vprint('[-] Proxy was not set in response')
			vprint('[+] ===== Server Not Vulnerable =====')
			bprint(0)
	def clean_up(self) :
		os.remove(self.cgi_dir + self.filename)

if __name__ == '__main__' :
	verbose = True
	boolean_output = False
	parser = argparse.ArgumentParser()
	parser.add_argument('-b', '--boolean', action='store_true', help='Script returns 1 if server is vulnerable, 0 if server is not vulnerable')
	parser.add_argument('-c', '--config', action='store', help='Enter httpd.conf address', dest='conf')
	args = parser.parse_args()

	if args.boolean == True :
		boolean_output = True
		verbose = False

	def vprint(obj) :
		if verbose :
			print(obj)
		return
	def bprint(obj) :
		if boolean_output :
			print(obj)
		return

	vprint('[+] Initiating Test')
	if args.conf == None :
		httpdconf_addr = raw_input('[?] Enter httpd.conf address: [Default: /etc/httpd/conf/httpd.conf] ') or '/etc/httpd/conf/httpd.conf'
	else :
		httpdconf_addr = args.conf
	config_parser = ApacheConfigParser(httpdconf_addr)
	vprint('[+] httpd.conf address was set to ' + httpdconf_addr)
	vprint('[+] Reading CGI-Directory Address from httpd.conf')
	cgi_dir = config_parser.get_cgi_dir()
	vprint('[+] CGI-Directory was set to ' + cgi_dir)
	vprint('[+] Initiating TestSuite')
	test_suite = TestSuite(cgi_dir)
	vprint('[+] Creating CGI File')
	test_suite.create_test_file()
	vprint('[+] Setting Permissions')
	test_suite.set_permissions()
	vprint('[+] Running Tests')
	test_suite.run_test()
	vprint('[+] Cleaning up')
	test_suite.clean_up()
	vprint('[+] Done')