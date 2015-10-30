DbSync Verify
===

This will be a verification tool that will verify that an import will run successfully.

This tool will verify the following things:

1. That the target has all of the fields that the source has 
2. That all of the required fields in the target are defined in the source
3. That there are no foreign keys that depend on records that will be deleted
4. That there are no unique constraints that will be violated when records are added or updated