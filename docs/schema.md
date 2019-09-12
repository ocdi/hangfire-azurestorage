We'll store some data in table storage, job data will be serialized to JSON and stored in blob storage. Queuing will be handled in Azure Queues.

| Partition | Key | Columns |
| --------- | --- | ------- |
| SCHEMA | Version | "1" |
| Sets_{Key} | Value |
| Hash | Id | DataJson |





| JOB_DATA | Id | InvocationData, Arguments, CreatedAt, ExpireAt, (StateId, StateName) |
| JOB_{state} | Id | |
| JOB_HISTORY | Id | Name, Reason, CreatedAt, Data |