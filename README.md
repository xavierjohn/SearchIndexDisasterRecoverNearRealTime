# Search Index Disaster Recovery
This repository is about Azure Cognitive Search Geo-disaster recovery with near real time multi region update that achieves availability beyond 99.9% SLA.


Geo replication is achieved by producers pushing data into a Geo Paired event hub which is subscribed by a WebJob in each region. 
The WebJob then updates its respective Search Index via Azure Search Push API.

Event Hub has manual failover. After failover the WebJobs have to be restarted with checkpoints deleted.
Please upvote the following request. [Failover compatible EH receiver](https://feedback.azure.com/d365community/idea/0949cc0e-7626-ec11-b6e6-000d3a4f032c)

[Deployment Steps](docs/Deployment.md)

[Use Azure Bicep to automate deployment](bicep/usage.md)

[Availability and business continuity in Azure Cognitive Search](https://docs.microsoft.com/en-us/azure/search/search-performance-optimization#multiple-services-in-separate-geographic-regions)

[Azure Event Hubs - Geo-disaster recovery](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-geo-dr)

[Use the Azurite emulator for local Azure Storage development](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
