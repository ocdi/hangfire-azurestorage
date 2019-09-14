We'll store some data in table storage, job data will be serialized to JSON and stored in blob storage. 
Queuing will be handled in Azure Queues.

Jobs are the most complex piece of data that needs to be stored.
The job and its arguments can be quite large, in addition to the history of it. We'll use blob storage for this information
with table storage containing the last known state for a job.


/{jobID}/job.json
	- contains all the job invocation data required
/{jobID}/state_{statename}_{ts}.json
	- contains the latest state information

Jobs Table
| PartitionKey | Key | StateFile
| -----| ---- |
| Jobs | {jobID} | ...  |

These can be stored in blob storage.

| Partition | Key | Columns |
| --------- | --- | ------- |
| SCHEMA | Version | "1" |
| Sets_{Key} | Value |
| Hash | Id | DataJson |





| JOB_DATA | Id | InvocationData, Arguments, CreatedAt, ExpireAt, (StateId, StateName) |
| JOB_{state} | Id | |
| JOB_HISTORY | Id | Name, Reason, CreatedAt, Data |