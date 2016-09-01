#region license
// Copyright (c) 2003, 2004, Rodrigo B. de Oliveira (rbo@acm.org)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Rodrigo B. de Oliveira nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion


"""
Adapted from Example located at
https://github.com/bamboo/boo/blob/master/examples/duck-typing/XmlObject.boo
"""

namespace Boo.XmlObject

import System
import System.Xml from System.Xml
import System.Reflection
import System.IO
import System.Collections

[DefaultMember("Item")]
class XmlObject(IQuackFu):
		
		_element as XmlElement
		
		def constructor(element as XmlElement):
				_element = element
				
		def constructor(text as string):
				doc = XmlDocument()
				doc.LoadXml(text)
				_element = doc.DocumentElement
		
		public static def read_xml_file(xml_file) as XmlObject:
			using filestream = File.Open(xml_file, FileMode.Open, FileAccess.Read):
				reader = StreamReader(filestream)
				return XmlObject(reader.ReadToEnd())

		public static def edit_xml_file(xml_file, edit_action as callable(XmlObject), log_action as callable(string)):
			edit_xml_file(xml_file, edit_action as callable(XmlObject), log_action as callable(string), FileMode.Open)

		public static def edit_xml_file(xml_file, edit_action as callable(XmlObject), log_action as callable(string), file_mode as FileMode):
			log_action("Opening file ${xml_file} for editing") if log_action is not null
			using filestream = File.Open(xml_file, file_mode, FileAccess.ReadWrite):
				reader = StreamReader(filestream)
				writer = StreamWriter(filestream)
				xmlObj = XmlObject(reader.ReadToEnd())
				
				//call the edit action
				edit_action.Invoke(xmlObj)
				
				log_action("Saving file ...") if log_action is not null
				filestream.Seek(0, SeekOrigin.Begin)
				filestream.SetLength(0) //truncate the file
				xmlObj.WriteTo(writer)
				
		def QuackSet(name as string, parameters as (object), value) as object:
			if name is null or name == string.Empty:
				assert len(parameters) == 1
				_element.SetAttribute(FixName(parameters[0]), (value.ToString() if value is not null else string.Empty))
				return self
			else:
				strValue as string = (value.ToString() if value is not null else null)
				fixedName = FixName(name)
				if len(self.QuackGet(fixedName, null)) == 0:
					self.Append("<${fixedName} />")
				child as XmlObject = (self.QuackGet(fixedName, null) as IList)[0]
				childEl as XmlElement = child._element
				if parameters is null or len(parameters) == 0:
					childEl.InnerXml = string.Empty
					childEl.InnerText = string.Empty
					if strValue is not null and strValue.Trim().StartsWith("<"):
						childEl.InnerXml = strValue
					else:
						childEl.InnerText = (strValue if strValue is not null else string.Empty)
				else:
					assert len(parameters) == 1
					childEl.SetAttribute(parameters[0], strValue)
				return child
				
		def QuackGet(name as string, parameters as (object)) as object:
				if name == string.Empty:
						assert len(parameters) == 1
						return GetAttribute(parameters[0])
				elements = _element.SelectNodes(FixName(name))
				if elements is not null and elements.Count > 0:
						list = [ XmlObject(e) for e as XmlElement in elements ] 
						return (list[parameters[0]] if parameters is not null and len(parameters) == 1 and parameters[0] isa int else list)
				else:
					return []
					
		def QuackInvoke(name as string, args as (object)) as object:
			if name == "op_Addition" or name == "Append":
				doc as XmlDocument = _element.OwnerDocument
				docFrag = doc.CreateDocumentFragment()
				toAppend = (args[1] if name == "op_Addition" else args[0])
				if toAppend isa XmlObject:
					docFrag.InnerXml += (toAppend as XmlObject).ToString(true)
				else:
					docFrag.InnerXml += toAppend
				appended_node = _element.AppendChild(docFrag)
				return (XmlObject(appended_node) if name == "Append" else self)
			elif name == "Remove":
				assert len(args) == 1
				for arg in args[0]:
					if arg isa XmlObject:
						_element.RemoveChild((arg as XmlObject)._element)
					elif arg isa XmlElement:
						_element.RemoveChild(arg)
					else:
						raise ArgumentException("Invalid argument type {0} passed to Remove" % (arg.GetType().Name,))							
			elif name == "Empty":
				self._element.InnerText = string.Empty
				self._element.InnerXml = string.Empty
				return self
			elif name == "Ensure":
				assert len(args) == 1
				if len(self.QuackGet(args[0], null)) == 0:
					assert args[0] isa string
					self.Append("<${args[0]} />")
				return self.QuackGet(args[0], null)
			elif name == "XPath":
				assert len(args) == 1
				elements = _element.SelectNodes(args[0])
				if elements is not null:
					return XmlObject(elements[0]) if elements.Count == 1
					return XmlObject(e) for e as XmlElement in elements
				else:
					return []
			elif name == "WriteTo":
				writer as StreamWriter = args[0]
				xmlWriter = XmlTextWriter(writer)
				xmlWriter.Formatting = Formatting.Indented
				odoc as XmlDocument = _element.OwnerDocument
				odoc.WriteTo(xmlWriter)
				xmlWriter.Flush()
			else:
				raise System.InvalidOperationException("Method ${name} not found in class ${self.GetType()}")		


		def GetAttribute(name as string):
			item = _element.Attributes.GetNamedItem(name)
			return item.InnerText if item
		
		[Extension]
		def op_Implicit(o as XmlObject) as string:
			return (o.ToString() if o is not null else null)
			
		def FixName(name as string):
			//replace single underscores with a dot (.) - so long as they don't match other underscore patterns
			name = /(?<!(_|_D))_(?!(_|D_))/.Replace(name, ".")
			//replace the pattern _D_ with a dash (-)
			name = /_D_/.Replace(name, "-")
			//replace double underscores with a single underscore
			name = /__/.Replace(name, "_")
			return name
				
		override def ToString():
				return (_element.InnerXml if _element.HasChildNodes else _element.InnerText)
		
		def ToString(includeOuterXml as bool):
			return (_element.OuterXml if includeOuterXml else self.ToString())