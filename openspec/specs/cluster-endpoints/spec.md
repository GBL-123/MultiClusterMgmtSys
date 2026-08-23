# cluster-endpoints

## Purpose

Define the data and behavior contract for administrator-defined cluster endpoints (`ClusterEndpoint`): app-owned metadata records (Kind = VIP/Domain, Value ≤ 256 chars, optional Note ≤ 64 chars, SortOrder) persisted on `ClusterInfo`, updated exclusively through a full-replacement operation validated by the service, cascaded away on cluster delete, readable by all roles on the detail page but mutable only by Admins.

## Requirements

### Requirement: Clusters carry zero or more administrator-defined endpoints

A cluster SHALL own a list of `ClusterEndpoint` records persisted in the app database. Each endpoint has a `Kind` (`Vip` or `Domain`), a non-empty `Value` (≤ 256 chars), an optional `Note` (≤ 64 chars), and a `SortOrder` integer. Endpoints are app-owned metadata — they do not require cluster reachability and SHALL remain available for read when the cluster's `Status` is `Offline`. New kinds are added in the future by extending the `ClusterEndpointKind` enum; no schema change is required beyond that enum addition.

#### Scenario: Endpoint set is optional

- **WHEN** a cluster is created without any endpoints submitted in the create VM
- **THEN** the service stores the cluster with an empty endpoints collection and returns success

#### Scenario: Endpoints survive offline state

- **WHEN** a cluster whose `Status` is `Offline` is loaded by `ClusterService.GetClusterDetailAsync(id)`
- **THEN** the returned `ClusterDetailViewModel.Endpoints` contains the previously saved endpoints unchanged (the k8s API is never contacted for endpoints)

#### Scenario: Kind is typed at the entity boundary

- **WHEN** a `ClusterEndpoint` row is materialized from the database
- **THEN** its `Kind` field is a `ClusterEndpointKind` enum value (`Vip` or `Domain`), not a string, not an integer exposed in the public VM API

### Requirement: Endpoint persistence uses a full-replacement strategy

`ClusterService` SHALL expose endpoint updates as a single full-replacement operation: the client submits the complete intended list of endpoints for a cluster, the service validates it, clears the existing `ClusterInfo.Endpoints` collection, and re-inserts the submitted rows. Partial diffing (per-row patches) is intentionally NOT supported.

#### Scenario: Update clears and repopulates

- **WHEN** `UpdateClusterEndpointsAsync(clusterId, items)` is called with N items
- **THEN** after the call the cluster has exactly N endpoints whose fields match `items` (existing rows not in `items` are removed, new rows in `items` are inserted, surviving rows are recreated)

#### Scenario: Removed endpoints disappear

- **WHEN** the editor marks a previously-saved row as `IsDeleted == true` and submits
- **THEN** the resulting cluster endpoints collection contains no row with that endpoint's original `Id`

#### Scenario: Sort order honored

- **WHEN** the submitted items carry distinct `SortOrder` values
- **THEN** the persisted rows preserve those `SortOrder` values, and `ClusterService.GetClusterDetailAsync` returns them sorted ascending by `SortOrder` then by `Id`

### Requirement: Endpoint value formatting and validation

Each endpoint `Value` MUST be non-empty after trimming and at most 256 characters long; `Note` is optional and at most 64 characters long. Both constraints SHALL be enforced by the service before persistence.

#### Scenario: Empty value rejected

- **WHEN** `UpdateClusterEndpointsAsync` is called with an item whose `Value` is null, empty, or whitespace-only after trimming
- **THEN** the service throws `ArgumentException` with a message indicating the invalid value and does NOT mutate any persisted endpoint row

#### Scenario: Oversized value rejected

- **WHEN** `UpdateClusterEndpointsAsync` is called with an item whose `Value` is longer than 256 characters or whose `Note` is longer than 64 characters
- **THEN** the service throws `ArgumentException` and does NOT mutate any persisted endpoint row

### Requirement: Deleting a cluster cascades to its endpoints

When a `ClusterInfo` row is deleted, its associated `ClusterEndpoint` rows SHALL be removed in the same operation. Endpoint rows MUST NOT be orphaned and MUST NOT be reassigned to a different cluster.

#### Scenario: Cluster delete removes endpoints

- **WHEN** `ClusterService.DeleteClusterAsync(id)` completes successfully
- **THEN** no `ClusterEndpoint` row with `ClusterId == id` exists in the database afterwards

#### Scenario: Endpoints cannot be reparented

- **WHEN** any mutation path is invoked with an `Items` list containing a row whose `Id` belongs to an endpoint of a different cluster
- **THEN** the service either ignores the `Id` (treats the row as a new insertion) or throws `ArgumentException`; it MUST NOT silently relocate the endpoint to a different cluster

### Requirement: Endpoint list is read-only for non-admin members

Members (role `Member`) SHALL be able to read the endpoints list on the cluster detail page. They MUST NOT be able to open the endpoints management dialog, mutate endpoints, nor see any "管理" / "添加" / "删除" affordance for endpoints.

#### Scenario: Member sees endpoints read-only

- **WHEN** a user in role `Member` opens `/clusters/{id}` and the cluster has endpoints
- **THEN** the Endpoints card renders the rows sorted by `SortOrder` but renders no "管理" button and no per-row delete control

#### Scenario: Member cannot reach the management dialog

- **WHEN** a user in role `Member` attempts to open the endpoints management dialog (by any client-side path)
- **THEN** the dialog's content is gated behind `AuthorizeView Roles="Admin"` and so the action surface is empty for that user
