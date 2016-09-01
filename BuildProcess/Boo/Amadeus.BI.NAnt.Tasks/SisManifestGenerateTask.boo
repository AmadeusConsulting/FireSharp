namespace AmadeusConsulting.BI.NAnt.Tasks

import System
import System.IO
import NAnt.Core from NAnt.Core
import NAnt.Core.Tasks from NAnt.Core
import NAnt.Core.Attributes from NAnt.Core

import Boo.XmlObject

[TaskName("ssisManifestGenerate")]
class SsisManifestGenerateTask(NAnt.Core.Task):

	_projectFile as string
	_projectName as string
	_outfile as string
	_allowConfigChanges as bool = true
	_generatedBy as string = "NAnt"
	
	[TaskAttribute("projectFile")]
	ProjectFile as string:
		get:
			return _projectFile
		set:
			_projectFile = value
			
	[TaskAttribute("projectName")]
	ProjectName as string:
		get:
			if string.IsNullOrEmpty(_projectName) and not string.IsNullOrEmpty(ProjectFile):
				_projectName = Path.GetFileNameWithoutExtension(ProjectFile)
			return _projectName
		set:
			_projectName = value
			
	[TaskAttribute("outfile")]
	OutFile as string:
		get: 
			if _outfile == null and not string.IsNullOrEmpty(_projectFile):
				project_name = Path.GetFileNameWithoutExtension(_projectFile)
				_outfile = Path.Combine(Path.GetDirectoryName(_projectFile), "bin\\${project_name}.SSISDeploymentManifest")
			return _outfile
		set:
			_outfile = value
			
	[TaskAttribute("generatedBy")]
	GeneratedBy as string:
		get:
			return _generatedBy
		set:
			_generatedBy = value
	
	[TaskAttribute("allowConfigChanges")]
	AllowConfigChanges as bool:
		get:
			return _allowConfigChanges
		set:
			_allowConfigChanges = value
	

	override def ExecuteTask():
		if string.IsNullOrEmpty(ProjectFile):
			raise BuildException("projectFile must be specified")
			
		if not File.Exists(ProjectFile):
			raise BuildException("projectFile does not exist at path $ProjectFile")
		
		package_list = []
		misc_file_list = []
		
		proj_xml = XmlObject.read_xml_file(ProjectFile)
		
		dts_packages as Boo.Lang.List[of XmlObject] = Boo.Lang.List[of XmlObject](p as XmlObject for p in proj_xml.DTSPackages[0].DtsPackage)
		misc_project_items as Boo.Lang.List[of XmlObject] = Boo.Lang.List[of XmlObject](pi as XmlObject for pi in proj_xml.Miscellaneous[0].ProjectItem)
		
		for p in dts_packages:
			package_list.Add(p.FullPath[0].ToString())
			
		for pi in misc_project_items:
			misc_file_list.Add(pi.FullPath[0].ToString())
			
		create_manifest = do(manifest as XmlObject):
			manifest["GeneratedBy"] = GeneratedBy
			manifest["GeneratedFromProjectName"] = ProjectName;
			manifest["GeneratedDate"] = DateTime.Now.ToString("o") # round-trip datetime format (see http://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx)
			manifest["AllowConfigurationChanges"] = AllowConfigChanges.ToString().ToLowerInvariant()
			for p in package_list:
				manifest.Append("<Package>$p</Package>")
			for misc in misc_file_list:
				manifest.Append("<MiscellaneousFile>$misc</MiscellaneousFile>")
		
		// write the new, blank deploy manifest	
		using filestream = File.Open(OutFile, FileMode.Create, FileAccess.ReadWrite):
			writer = StreamWriter(filestream)
			filestream.Seek(0, SeekOrigin.Begin)
			filestream.SetLength(0) //truncate the file
			writer.Write("""<?xml version="1.0"?>
<DTSDeploymentManifest>
</DTSDeploymentManifest>""")
			writer.Flush()
					
		XmlObject.edit_xml_file(OutFile, create_manifest, null)