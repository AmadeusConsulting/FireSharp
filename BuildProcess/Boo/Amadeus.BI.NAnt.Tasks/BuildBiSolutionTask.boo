namespace AmadeusConsulting.BI.NAnt.Tasks

import System
import NAnt.Core from NAnt.Core
import NAnt.Core.Tasks from NAnt.Core
import NAnt.Core.Attributes from NAnt.Core

[TaskName("bidsSolutionBuild")]
class BuildBiSolutionTask(ExternalProgramBase):
	_buildConfiguration as string
	_solutionFile as string
	_devEnvPath as string
	
	override ProgramArguments as string:
		get:
			return "\"${SolutionFile}\" /ReBuild ${BuildConfiguration}"
			
	override def ExecuteTask() as void:
		self.ExeName = _devEnvPath
		
		if string.IsNullOrEmpty(_solutionFile):
			raise BuildException("SolutionFile must be specified")
		
		super()
	
	[TaskAttribute("devenv")] 
	DevEnvPath as string:
		get:
			return _devEnvPath
		set:
			_devEnvPath = value
		
	[TaskAttribute("sln")] 
	SolutionFile as string:
		get:
			return _solutionFile
		set:
			_solutionFile = value
		
	[TaskAttribute("config")]
	BuildConfiguration as string:
		get:
			if string.IsNullOrEmpty(_buildConfiguration):
				_buildConfiguration = "Release"
			return _buildConfiguration
		set:
			_buildConfiguration = value
		

