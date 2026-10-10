# Converting elements

> Copied on 2026-10-10 from Mike's ListFunctions repository. Names such as `New-HashSet` and `ListFunctions.Engine` are ListFunctions' own, given where a measurement was taken; the facts are general engine behavior. The last section says what they mean for PoshDotNet.

## What `$null` converts to

`LanguagePrimitives.ConvertTo($null, T)`, measured in both editions on 2026-09-30, with the structs, `[bool]`, `[char]`, and `[hashtable]` added on 2026-10-04:

| Element type | Result |
|---|---|
| `[int]` | `0` |
| `[string]` | `''` |
| `[bool]` | `$false` |
| `[char]` | `[char]0` |
| `Nullable[int]`, and most classes, such as `[version]`, `[System.IO.FileInfo]`, and `[hashtable]` | `$null` |
| `[ref]` | a `PSReference` that wraps `$null` |
| `[datetime]`, `[guid]`, `[timespan]`, enums such as `[ConsoleColor]`, and other structs such as `KeyValuePair[string, int]` | the conversion throws |

`$list.Add($null)` stores the same value as `ConvertTo` for `[int]`, `[string]`, `Nullable[int]`, `[version]`, `[System.IO.FileInfo]`, and `[ref]` (measured 2026-09-30). So a `$null` in a typed collection of a struct that throws is a conversion error, not `default(T)`.

Dictionary values follow the same rule (measured 2026-10-03): `Add('b', $null)` and the indexer store `''` in a `Dictionary[string, string]`, `0` for `[int]` values, and `$null` for `[object]` and `[version]` values.

## In PoshDotNet

No rule depends on these facts yet. Generated dynamic-parameter code will: a `RuntimeDefinedParameter.Value` is an `object` that stays `null` while the parameter is unbound, and converting `null` throws for structs such as `[datetime]`. A generated getter has to handle the unbound case before it converts.
