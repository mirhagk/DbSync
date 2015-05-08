DbSync
===

DbSync is a tool for exporting a database to a set of XML files. They can then be imported to a database, doing appropriate merging.

Why
---

Why would you want to export a database to a set of XML files? A few potential reasons:

1. Source Control - Source control systems work best with text based formats, exporting your data to xml means that you can view history of the files
2. Comparison - You can compare 2 xml files using standard text file comparison tools
3. Data Transfer - You can export data from one database and import into another database

Why XML?
---

With all the anti-XML pro-JSON movements XML might seem like a weird choice. The benefit of XML is that it really isn't a format, but a way to create a format. For instance there are many different ways to encode the exact same information.

For this project the following simple schema was chosen:

~~~
<root>
  <row
    CentreID="1"
    OrganizationID="1041" />
</root>
~~~

This has the benefit of very few repeated characters and very easy to edit by hand. It's actually similar to json (in fact it can be converted with a few simple regular expressions) but it doesn't have the overhead of the repeated quotations for field names.

###Why not csv?

Why not a csv format? CSV is a very simple format with lots of exporters already existing (SQL server has BCP which does it). It works for some scenarios, but it's significantly harder to work with in a text editor (you don't know which field lines up to which column). And once you add proper escaping it loses a lot of it's simplicity.

Installation
===

A chocolatey package is coming soon, but for now you can build the project from source, and add the folder to your path (or copy the files to your desired location).

How to Use
===

DbSync is a command line application. You can use it like the following:

~~~
dbsync -config config.xml -job TestData --export
~~~

The above will load configuration from `config.xml` and export the `TestData` job specified. You can leave job out in order to execute all the jobs.

Each job is a set of tables and rules for merging and exporting with a database connection. You can specify multiple jobs in each configuration file and choose which job to run.

`--export` exports the database tables to files and `--import` imports files into the database.

Configuration
---

A configuration file looks like the following:

~~~
<Settings>
	<Job>
		...
	</Job>
	<Job>
		...
	</Job>
</Settings>
~~~

Each job has a name, a connection string, a set of tables, and some options. As an example:

~~~
<Job>
    <Name>Test Data</Name>
    <ConnectionString>server=.;database=BusTap;Integrated Security=True;</ConnectionString>
    <Tables>
      <Table>dbo.Stops</Table>
    </Tables>
    <Path>.\Data</Path>
    <MergeStrategy>MergeWithDelete</MergeStrategy>
    <AuditColumns>
      <CreatedDate>CreatedDate</CreatedDate>
      <CreatedUser>CreatedUser</CreatedUser>
      <ModifiedDate>ModifiedDate</ModifiedDate>
      <ModifiedUser>ModifiedUser</ModifiedUser>
    </AuditColumns>
    <IgnoreAuditColumnsOnExport>true</IgnoreAuditColumnsOnExport>
    <UseAuditColumnsOnImport>true</UseAuditColumnsOnImport>
  </Job>
~~~

+ Name - The name of the job (you can specify which job to run based on the name)
+ ConnectionString - The connection string to the database you want to use. Make sure to include the database you want in the string (and not just the server)
+ Tables - A list of `<Table>` elements which specify the tables included
+ Path - The path to place the exported data files
+ MergeStrategy - Which strategy the tool uses to merge data with the database
+ AuditColumns - If the tables have columns used only for auditing purposes then this is where you can map those special columns. The tool understands how to properly set these auditing columns (see below for more information)
+ IgnoreAuditColumnsOnExport - True to exclude these columns from the data files so that they are smaller and easier to manage. This will prevent these columns from being synced, and allow each database to show who modified it in that database
+ UseAuditColumnsOnImport - Use the special semantics of audit columns described below when importing  

Audit Columns
---

When using the audit columns for import dbsync understands and will populate certain columns. Simply tell it what the column names are, mapping to one of the options below:

+ CreatedDate - This is the date that the record was created. When inserting the record dbsync will use `GETDATE()` to populate this field.
+ CreatedUser - This is the user that created the record. When inserting dbsync will use `SUSER_NAME()` to populate this record
+ ModifiedDate - This is the data that the record was last modified. When updating the record (only when changes are found) dbsync will use `GETDATE()` to update this field
+ ModifiedUser - This is the last user to modify the record. When updating the record (only when changes are found) dbsync will use `GETDATE()` to update this field

Performance
===

Performance is a key goal of this project. It can handle tables of arbitrary size and does so rather quickly.