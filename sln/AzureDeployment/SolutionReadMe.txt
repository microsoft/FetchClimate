IMPORTANT:
1. Editing of FetchConfigurationDB requires following manual steps
	a. regenerating FetchConfigurationDataClasses.dbml in FetchConfigProvider project
	b. Coping of FetchConfigurationDB_Create.sql into FetchCnfigurator/database. Removing all "create database" and "use" commands, as well as all SQLCMD commands starting with ':' (e.g. :setvar)


Solution testing procedure

1. Rebuild the solution
2. Set FetchConfigurationDB as start up project and press Ctrl+F5 to deploy it locally.
3. Start Azure storage emulator.
4. Ensure that Test -> Test Settings -> Default Processor Architecture is set to x64
5. Run all tests from TestExplorer