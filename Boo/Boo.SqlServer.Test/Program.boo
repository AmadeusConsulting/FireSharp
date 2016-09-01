namespace Boo.SqlServer.Test

import System
import Boo.SqlServer

mgr = SqlServerManagement(print)

mgr.add_users_to_roles("server=.;database=Parallax.Asclepius;Integrated Security=SSPI;", "IIS APPPOOL\\Parallax - Asclepius=db_owner,IIS APPPOOL\\Asclepius WCF")

print "Press any key to continue . . . "
Console.ReadKey(true)
