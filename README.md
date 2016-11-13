# MAIME - Maintenance Manager for ETL
MAIME started out as a student program to automize the maintenance of ETL processes in the event of database schema changes.
This prototype is designed as a standalone .Net application, which is capable of reading and modifying Microsoft SQL Server Integration Tools (**SSIS**) packages.

MAIME is implemented using C# v6 and SQL Server 2014. Other versions might be supported.

###\*\*Disclaimer\*\*
This is a prototype, which involves that:
 - Not all transformations are supported
 - Errors might occur
 - The GUI is rather crude and only serves as a simple graphical representation of MAIME
   - This also means that configuration of MAIME happens in the generated configuration files
