#!/usr/bin/python

import os, requests

class ApacheConfigParser :
	CGI_CONFIG_PATTERN = 'ScriptAlias "/cgi-bin/"'
	def __init__(self, filename) :
		self.config_file = filename
	def get_cgi_dir(self) :
		with open(open(self.config_file)) as file :
			for line, in file :
				if CGI_CONFIG_PATTERN in line :
					return line.split()[2]

class TestSuite :
	def __init__(self, cgi_dir) :
		self.cgi_dir = cgi_dir
	def create_test_file(self, filename='httpoxy-test-file.py') :
		self.filename = filename
		test_file = open(self.cgi_dir + '/' + self.filename, 'w+')
		test_file.write('#!/usr/bin/python\n')
		test_file.write('import os')
		test_file.write('print "Content-Type: test/html\n"')
		test_file.write('print os.environ.get(\'HTTP_PROXY\')')
	def set_permissions(self) :
		os.chmod(self.cgi_dir + '/' + self.filename, 755)
	def run_test(self) :
		response = requests.get('127.0.0.1/cgi-bin/' + self.filename, headers = {'proxy': '1.2.3.4'})
		if '1.2.3.4' in response.text :
			print 'Server Vulnerable'
		else :
			print 'Not Vulnerable'
	def clean_up(self) :
		os.remove(self.cgi_dir + '/' + self.filename)

if __name__ == '__main__' :
	config_parser = ApacheConfigParser('/etc/httpd/conf/httpd.conf')
	cgi_dir = config_parser.get_cgi_dir()

	test_suite = TestSuite(cgi_dir)
	test_suite.create_test_file()
	test_suite.set_permissions()
	test_file.run_test()
	test_suite.clean_up()
