# SIMIS binary token IDs (proposal)

Binary blocks start with a little-endian `uint32` token ID, followed by a
`uint32` body length and the existing label/payload framing. The ID is
conventionally `(namespace << 16) | localId`, with 16 bits for each component.
The namespace is not a separate flags field. Payload flags read by `ReadFlags()`
are unrelated and remain unchanged.

`TokenID` represents the complete ID in both binary and Unicode readers. No
file-dependent offset is applied. Unicode still resolves the same symbolic
names; enum values need not be consecutive across namespaces.

## Namespace allocation

| Namespace | Owner | File-token allocation |
| --- | --- | --- |
| 0 | Kuju Core, including terrain and shapes | Existing IDs unchanged |
| 1-3 | Previously assigned Kuju applications | Not reassigned |
| 4 | MSTS Train, including world objects | Existing native IDs |
| 5 | ORTS (proposed) | Local IDs 2048-65535 |
| 6 | TSRE (proposed) | Local IDs 2048-65535 |

Namespaces 5 and 6 are a proposed interoperability agreement, not an existing
MSTS assignment or a claim of upstream approval. Each has 63,488 file-token
slots. Local IDs 0-2047 are reserved against **file-token allocation**, not
forbidden to the binary reader. An application's internal/server messages may
use that range in its own namespace, but must not be emitted as route-file
extensions. Server protocol negotiation and message handling are outside this
change.

The reader preserves unknown IDs, including namespaces other than those listed.
It does not enforce this allocation policy or reject reserved IDs. The consuming
format decides whether an unfamiliar block can be skipped.

Existing ORTS extension names are assigned explicit IDs starting with
`ORTSListName = 0x00050800`. The existing TSRE name in this master baseline is
`Ruler = 0x00060800`. The allocation `ShapeTemplate = 0x00060801` is reserved
for compatibility with TSRE and the separate unstable extension; this patch
does not introduce that absent enum member or its consumer into master.
Keep each assigned value stable when adding future members. Do not insert implicitly numbered
members into an established run. Avoid duplicate-value enum aliases: Unicode
lookup is built from `Enum.GetValues()` and member names.

## Compatibility

- Standard MSTS binary IDs are unchanged on disk. Train enum values now match
  those native IDs instead of using the old `localId + 300` convention.
- The former arbitrary `CarSpawner`, `Siding`, `Dyntrack`, `Transfer`, `Gantry`,
  `Pickup`, `Wagon`, and `Engine` enum values become their canonical Train IDs.
  World dispatch consequently needs only the symbolic cases.
- The supplied `loadstr.hdr` and former enum misplaced
  `EngineBrakesControllerGraduatedSelfLapLimitedHoldingStart` in the train-brake
  run. Moving it after `EngineBrakesControllerGraduatedSelfLapLimitedStart`
  matches its native ID `0x0004020E` and corrects 33 intervening IDs. This is an
  existing table-ordering discrepancy, not a namespace offset. No brake physics
  or Unicode spelling changes are included.
- Unicode token names do not change. New namespace support does not implement
  the feature represented by a token: an unknown TSRE object may still be skipped.
- ORTS does not provide a route-file binary writer in this change. The TSRE
  author reports no existing binary TSRE extension files requiring migration.
  No speculative aliases for the former arbitrary extension numbers are added.
  Any third-party binary format relying on those numbers would need a separately
  documented migration. Nonstandard files with incorrect/zeroed namespaces are
  not silently reinterpreted through the former per-file offsets.
- Numeric enum values change, so assemblies consuming them must be rebuilt
  together. External callers of the former three-argument `BinaryFileReader`
  constructor must use the two-argument constructor. The former header `Flags`
  field is removed; actual payload-flag methods are retained.

Older ORTS readers discard the namespace and interpret local IDs directly in
terrain/shape files, or add 300 in world files. Starting file extensions at 2048
avoids collisions with the enum in the reviewed master baseline (maximum 1563), but
does not guarantee all older/future forks will skip them safely. In particular,
the existing `terrain_samples` reader throws on unknown child IDs; that policy
is deliberately not changed here. Counted collections and positional records
also have their own rules. Add extension blocks only at compatible child-block
boundaries, retaining required legacy fields and correct counts.

Two new namespaces may intentionally use the same local ID: old readers cannot
distinguish them, but namespace-aware readers can. The low-ID reservation helps
older readers **ignore** extensions; it cannot make them understand extensions.

## Tests

`SbrTokenNamespaceTests` uses generated fixtures to cover complete IDs in
terrain, world and shape headers; compressed and uncompressed streams; mixed
namespaces; high-bit IDs; Unicode/binary name agreement; unique enum values;
the extension reservation; labels, lengths and payload flags; canonical world
object dispatch; and unchanged unknown-child policies in terrain.

No proprietary route, MSTS executable, graphics device, or Windows registry is
needed by these tests. A complete simulator build and representative route-load
checks remain appropriate before release.
