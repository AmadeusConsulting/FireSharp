namespace AmadeusConsulting.BI.NAnt.Tasks

import System
import System.IO
import NAnt.Core from NAnt.Core
import NAnt.Core.Tasks from NAnt.Core
import NAnt.Core.Attributes from NAnt.Core

[TaskName("ssasXmlaGenerate")]
class SsasXmlaGenerationTask(ExternalProgramBase):
	_outfile as string
	_ssasDeployToolPath as string
	_ssasProjFile as string
	_binFilePath as string
	
	override ProgramArguments as string:
		get:
			return "\"${BinFilePath}\" /d /o:\"${OutFile}\""
			
	override def ExecuteTask() as void:
		if string.IsNullOrEmpty(_ssasDeployToolPath):
			raise BuildException("You must specify the path to the Microsoft Analysis Services Deploy Tool")
		if string.IsNullOrEmpty(_ssasProjFile) and string.IsNullOrEmpty(_binFilePath) and string.IsNullOrEmpty(_outfile):
			raise BuildException("You must specify either the ssasProj file OR both binPath and outfile")
		
		self.ExeName = SsasDeployToolPath
		super()
	
	[TaskAttribute("ssasDeployTool")]
	SsasDeployToolPath as string:
		get:
			return _ssasDeployToolPath
		set:
			_ssasDeployToolPath = value
		
	[TaskAttribute("outfile")]
	OutFile as string:
		get:
			if string.IsNullOrEmpty(_outfile):
				_outfile = Path.Combine(Path.GetDirectoryName(SsasProjFile), "bin\\{0}.xmla" % (Path.GetFileNameWithoutExtension(SsasProjFile),))
			return _outfile
		set:
			_outfile = value
		
	[TaskAttribute("ssasProj")]
	SsasProjFile as string:
		get:
			return _ssasProjFile
		set:
			_ssasProjFile = value
			
	[TaskAttribute("binPath")]
	BinFilePath as string:
		get:
			if string.IsNullOrEmpty(_binFilePath) and not string.IsNullOrEmpty(_ssasProjFile):
				_binFilePath = Path.Combine(Path.GetDirectoryName(_ssasProjFile), "bin\\{0}.asdatabase" % (Path.GetFileNameWithoutExtension(_ssasProjFile),))
			return _binFilePath
		set:
			_binFilePath = value
