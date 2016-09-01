namespace Boo.XmlObject.Test

import System
import Boo.XmlObject
import Boo.Log4Net
		
filepath = """D:\temp\web.config""" //prompt("Enter the path to the xml file")
	
def print(message as string):
	print message





#import Boo.XmlObject
								
//web_application_name = Project.Properties["webApplicationProject"]
//file_path = Project.Properties[web_application_name + ".webconfig.path"]
//mode = Project.Properties["customErrorsMode"]
//debug = Project.Properties["compilationDebug"]

file_path = """D:\Projects\eTrial\BriefLynx\SiteRedesign\BriefLynx.Workspace.Web\Web.config"""
mode = 'Off'
debug = 'true'

def set_custom_error_and_compilation_debug(configuration as XmlObject):
	system_web as XmlObject = null
	sysweb_path = @/,/.Split('location,system.web') 
	if true:  //Project.Properties.Contains(web_application_name + ".systemweb.path"):
		last_path = configuration
		for path in sysweb_path: //@/,/.Split(Project.Properties[web_application_name + ".systemweb.path"]):
			last_path = last_path.Ensure(path)[0]
		system_web = last_path
	else:
		system_web = configuration.Ensure("system.web")[0]
	custom_errors as XmlObject = system_web.Ensure("customErrors")[0]
	custom_errors["mode"] = mode
	
	compilation = system_web.Ensure("compilation")[0]
	compilation["debug"] = debug

XmlObject.edit_xml_file(file_path, { x as XmlObject | set_custom_error_and_compilation_debug(x) }, print)













//Main Method

the_list = []

if not the_list:
	print "nope"

XmlObject.edit_xml_file(filepath, { xmlObj as XmlObject | 
	xmlObj.Ensure("system.web")[0].Ensure("compilation");
	compilation = xmlObj.system_web[0].compilation[0];
	compilation["debug"] = "true";
	compilation["targetFramework"] = "4.5";
	
	print "Lets alter the appsettings values";
	xmlObj.appSettings[0].Remove([s for s as XmlObject in xmlObj.appSettings[0].add if s["key"] == "NewKey"]);
	
	print "here's the updated version:";
	print xmlObj.appSettings[0];
}, print)








//LOG4NET APPENDERS

rf_appender_config = FileAppenderConfig()
rf_appender_config.Name = "RollingFileAppender"
rf_appender_config.FilePath = "Testing.log"
rf_appender_config.Append = true
rf_appender_config.RollingStyle = "Size"
rf_appender_config.MaxBackups = 5
rf_appender_config.MaxFileSize = "512KB"
rf_appender_config.StaticName = true
rf_appender_config.LayoutType = "log4net.Layout.PatternLayout"
rf_appender_config.LayoutPattern = "%date [%thread] %-5level %logger [%property{NDC}] - %message%newline"

logger_config = LoggerConfig("Parallax.Common.SyncFramework")
logger_config.Level = "INFO"

root_logger_config = LoggerConfig("root")
root_logger_config.Level = "INFO"
		
XmlObject.edit_xml_file(filepath, { x as XmlObject | 
					add_acglog_appender(x, AcgLogConfig.AcgLogDatabaseAppenderName, print);
					add_appender_ref_to_logger(x, AcgLogConfig.AcgLogDatabaseAppenderName, root_logger_config.Name, print);
				    add_appender_ref_to_logger(x, AcgLogConfig.AcgLogDatabaseAppenderName, logger_config.Name, print);
				    apply_logger_settings(x, logger_config, print);
				    add_rolling_file_appender(x, rf_appender_config, print);
				    add_appender_ref_to_logger(x, rf_appender_config.Name, logger_config.Name, print)
				}, print)

//LOG4NET

XmlObject.edit_xml_file(filepath, { configuration as XmlObject | 
				  	//ensure that xpath /configuration/glimpse/runtimePolicies/ignoredTypes exists
					configuration.Ensure("glimpse")[0].Ensure("runtimePolicies")[0].Ensure("ignoredTypes");
					glimpse = configuration.glimpse[0];
					glimpse["defaultRuntimePolicy"] = "On";
					glimpse["endpointBaseUri"] = "~/Glimpse.axd";
					ignored_runtime_policy_types = glimpse.runtimePolicies[0].ignoredTypes[0];
					ignored_local_policy = [policy for policy as XmlObject in ignored_runtime_policy_types.add if policy["type"] == "Glimpse.AspNet.Policy.LocalPolicy, Glimpse.AspNet"];
					print("Adding Glimpse.AspNet.Policy.LocalPolicy to ignored runtime policy types...") if len(ignored_local_policy) == 0;
					print("Local Policy already exists in Ignored Policy Types") if len(ignored_local_policy) > 0;
					ignored_runtime_policy_types.Append("<add type=\"Glimpse.AspNet.Policy.LocalPolicy, Glimpse.AspNet\" />") if len(ignored_local_policy) == 0;
				}, print)

appsetting_key = "foobar"
appsetting_value as string = "baz1"

def update_appsetting(configuration as XmlObject):
	appsettings = configuration.Ensure("appSettings")[0]
	existing_settings = [val for val as XmlObject in appsettings.add if val["key"] == appsetting_key]
	if not len(existing_settings):
		print("AppSetting ${appsetting_key} does not exist, adding it with value = ${appsetting_value}...")
		appsettings.Append("<add key=\"${appsetting_key}\" value=\"${appsetting_value}\" />")
	else:
		print("Updating existing AppSetting key ${appsetting_key} with value ${appsetting_value}")
		existing_setting as XmlObject = existing_settings[0]
		existing_setting["value"] = appsetting_value

XmlObject.edit_xml_file(filepath, update_appsetting, print)


print "Press any key to continue . . . "
Console.ReadKey()
