import System
import Boo.XmlObject
import System.IO


def edit_xml_file(xml_file, edit_action as callable(XmlObject), log_action as callable(string)):
	log_action("Opening file ${xml_file} for editing") if log_action is not null
	using filestream = File.Open(xml_file, FileMode.Open, FileAccess.ReadWrite):
		reader = StreamReader(filestream)
		writer = StreamWriter(filestream)
		xmlObj = XmlObject(reader.ReadToEnd())
		
		//call the edit action
		edit_action.Invoke(xmlObj)
		
		log_action("Saving file ...") if log_action is not null
		filestream.Seek(0, SeekOrigin.Begin)
		filestream.SetLength(0) //truncate the file
		xmlObj.WriteTo(writer)