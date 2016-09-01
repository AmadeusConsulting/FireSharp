namespace Boo.SqlServer

import System
import System.Linq
import System.Linq.Enumerable
import System.Collections.Generic
import System.Data.SqlClient
import Microsoft.SqlServer.Management.Common from Microsoft.SqlServer.ConnectionInfo
import Microsoft.SqlServer.Management.Smo from Microsoft.SqlServer.Smo

class SqlServerManagement:
	
	_log as callable(string)
	
	def constructor([Required]log as callable(string)):
		_log = log
		
	def add_users_to_roles([Required]connection_string as string, [Required]user_role_list as string):
		connection_string_info = SqlConnectionStringBuilder(connection_string)
		database_name = connection_string_info.InitialCatalog
		
		_log.Invoke("Adding to database \"${database_name}\" user-role list: \"${user_role_list}\"")
		
		using connection = SqlConnection(connection_string):
			server_connection = ServerConnection(connection)
			server = Server(server_connection)
			
			database = server.Databases[database_name]
			users as IEnumerable[of User] = database.Users.Cast[of User]()
			
			user_roles as IEnumerable[of (string)] = user_role_list.Split(char(',')).Select({ur as string | ur.Split(char('='))})
			
			for name,role in user_roles.Select({ a as (string) | (a if len(a) == 2 else (a[0],string.Empty)) }):
				existing_user as User = users.Where({u as User | u.Name == name }).SingleOrDefault()
				if not existing_user:
					_log.Invoke("Adding user \"${name}\" to database \"${database.Name}\"")
					existing_user = User(database, name)
					existing_user.Create()
				
				if not string.IsNullOrEmpty(role):
					existing_roles = [r for r as string in existing_user.EnumRoles().Cast[of string]() if r == role]
				
					if not len(existing_roles):
						_log.Invoke("Adding user \"${name}\" to role \"${role}\" in database \"${database.Name}\"")
						existing_user.AddToRole(role)
						existing_user.Alter()
		
		

