
namespace Boo.Log4Net

import System
import System.Globalization
import log4net.Appender from log4net
import log4net.Util from log4net
import Boo.XmlObject

class AcgLogConfig:
	
	public static final AcgLogDatabaseAppenderName = "AcgLogDatabaseAppender" 


class AppenderConfig:
	
	def constructor():
		pass
	
	property Name as string
		
class FileAppenderConfig(AppenderConfig):
	
	_max_file_size as long
	
	_rolling_style_enum as RollingFileAppender.RollingMode = RollingFileAppender.RollingMode.Size
	
	def constructor():
		super()
		MaxBackups = 5
		MaxFileSize = "512KB"
		StaticName = true
		Append = true
		LayoutType = "log4net.Layout.PatternLayout"
		LayoutPattern = "%date [%thread] %-5level %logger [%property{NDC}] - %message%newline"
	
	property FilePath as string
	
	property Append as bool
	
	RollingStyle as string:
		get:
			return _rolling_style_enum.ToString()
		set:
			try:
				_rolling_style_enum = Enum.Parse(RollingFileAppender.RollingMode, value)
			except:
				_rolling_style_enum = RollingFileAppender.RollingMode.Size
	
	property MaxBackups as int
	
	MaxFileSize as string:
		get:
			return _max_file_size.ToString(CultureInfo.InvariantCulture)
		set:
			_max_file_size = OptionConverter.ToFileSize(value, (_max_file_size if _max_file_size > 0 else 524288))
	
	property StaticName as bool
	
	property LayoutType as string
	
	property LayoutPattern as string
	
class LoggerConfig:
	
	def constructor([Required]name as string):
		Name = name
		Level = "DEBUG"
		Additivity = true
	
	property Name as string
	
	property Additivity as bool
	
	property Level as string

def ensure_log4net_configsection(configuration as XmlObject, log as callable(string)):
	config_sections = configuration.Ensure("configSections")[0]
	
	all_config_sections = config_sections.section
	
	existing_log4net_section = [sec for sec as XmlObject in all_config_sections if sec["name"] == "log4net"]
	
	if not len(existing_log4net_section):
		log("log4net configSection is missing, adding it...")
		config_sections.Append("<section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler, log4net\" />")
	else:
		log("log4net configSection exists")

def ensure_logger(configuration as XmlObject, name as string, log as callable(string)) as XmlObject:
	ensure_log4net_configsection(configuration, log)
	log4net_root = configuration.Ensure("log4net")[0]
	existing_logger as List = null
	if name != "root":
		existing_logger = [logger for logger as XmlObject in log4net_root.logger if logger["name"] == name]
	else:
		existing_logger = [logger for logger as XmlObject in log4net_root.root]
	logger as XmlObject = null
	if not len(existing_logger):
		log("Adding logger ${name} to log4net config")
		if(name == "root"):
			logger = log4net_root.Append("<root />")
		else:
			logger = log4net_root.Append("<logger name=\"${name}\" />")
	else:
		logger = existing_logger[0]
	
	return logger

def add_acglog_appender(configuration as XmlObject, connection_string_name as string, log as callable(string)):
	cs_name = (connection_string_name if connection_string_name is not null else "default")
	ensure_log4net_configsection(configuration, log)
	log4net_root = configuration.Ensure("log4net")[0]
	existing_acglog_appender = [app for app as XmlObject in log4net_root.appender if app["name"] == AcgLogConfig.AcgLogDatabaseAppenderName]
	acglog_appender as XmlObject = null
	if not len(existing_acglog_appender):
		log("Adding AcgLogDatabaseAppender to log4net config...")
		acglog_appender = log4net_root.Append("<appender name=\"${AcgLogConfig.AcgLogDatabaseAppenderName}\" type=\"AmadeusConsulting.Simplex.Logging.Log4Net.AcgLogDatabaseAppender, Amadeus.Simplex.Base\" />")
	else:
		log("AcgLogDatabaseAppender already exists")
		acglog_appender = existing_acglog_appender[0]
	
	connection_string_name_setting = acglog_appender.Ensure("connectionStringName")[0]
	connection_string_name_setting["value"] = cs_name

def add_rolling_file_appender(configuration as XmlObject, config as FileAppenderConfig, log as callable(string)):
	ensure_log4net_configsection(configuration, log)
	log4net_root = configuration.Ensure("log4net")[0]
	existing_rolling_file_appender = [appender for appender as XmlObject in log4net_root.appender if appender["name"] == config.Name]
	rolling_file_appender as XmlObject = null
	if not len(existing_rolling_file_appender):
		log("Adding Rolling File Appender ${config.Name}...")
		rolling_file_appender = log4net_root.Append("<appender name=\"${config.Name}\" type=\"log4net.Appender.RollingFileAppender\" />")
	else:
		log("Rolling File Appender ${config.Name} already exists.")
		rolling_file_appender = existing_rolling_file_appender[0]

	log("Applying configuration settings for Rolling File Appender ${config.Name}...")
	rolling_file_appender.Ensure("file")[0]["value"] = config.FilePath
	rolling_file_appender.Ensure("appendToFile")[0]["value"] = config.Append.ToString().ToLowerInvariant()
	rolling_file_appender.Ensure("rollingStyle")[0]["value"] = config.RollingStyle
	rolling_file_appender.Ensure("maxSizeRollBackups")[0]["value"] = config.MaxBackups
	rolling_file_appender.Ensure("maximumFileSize")[0]["value"] = config.MaxFileSize
	rolling_file_appender.Ensure("staticLogFileName")[0]["value"] = config.StaticName.ToString().ToLowerInvariant()
	layout = rolling_file_appender.Ensure("layout")[0]
	layout["type"] = config.LayoutType
	layout.Ensure("conversionPattern")[0]["value"] = config.LayoutPattern

def apply_logger_settings(configuration as XmlObject, config as LoggerConfig, log as callable(string)):
	logger = ensure_logger(configuration, config.Name, log)
	
	if config.Name != "root":
		log("setting additivity for ${config.Name} to ${config.Additivity}")
		logger["additivity"] = config.Additivity
	
	log("setting logger level for ${config.Name} to ${config.Level}...")
	level = logger.Ensure("level")[0]
	level["value"] = config.Level

def add_appender_ref_to_logger(configuration as XmlObject, appender_ref as string, logger_name as string, log as callable(string)):
	logger = ensure_logger(configuration, logger_name, log)
	
	existing_appender_refs as List = logger.appender_D_ref
	
	matching_appender_ref = [ar for ar as XmlObject in existing_appender_refs if not string.IsNullOrEmpty(ar["ref"]) and ar["ref"] == appender_ref]
	
	if not len(matching_appender_ref):
		log("Adding new appender-ref for ${appender_ref}...")
		logger.Append("<appender-ref ref=\"${appender_ref}\" />")
	else:
		log("appender-ref ref=${appender_ref} already exists for logger ${logger_name}")