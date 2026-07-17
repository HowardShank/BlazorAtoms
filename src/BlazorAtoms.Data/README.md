# BlazorAtoms.Data

Data-utility components for Blazor. Standalone, no runtime dependencies beyond
`Microsoft.AspNetCore.Components.Web`, no JavaScript, works in every render mode.

## Components

### `AtomDataHasher`

Live CRC / cryptographic hash panel. Type into the input, pick an algorithm from the
CRC vs Cryptographic groups in the built-in `<select>`, watch the uppercase hex
digest update on every keystroke.

Algorithms — `HashAlgorithmKind`:

| Value    | Engine                                     | Output width |
|----------|--------------------------------------------|--------------|
| `Crc32`  | CRC-32 / IEEE 802.3 (reflected)            | 8 hex chars  |
| `Crc64`  | CRC-64 / ECMA-182 (non-reflected)          | 16 hex chars |
| `Md5`    | `System.Security.Cryptography.MD5`         | 32 hex chars |
| `Sha256` | `System.Security.Cryptography.SHA256`      | 64 hex chars |
| `Sha512` | `System.Security.Cryptography.SHA512`      | 128 hex chars |

CRC engines are implemented in-library (table-driven, byte-at-a-time) so the
package stays framework-only — no `System.IO.Hashing` NuGet dependency.
CRC-64/ECMA-182 output matches `System.IO.Hashing.Crc64`.

### Minimal usage

```razor
<AtomDataHasher @bind-Value="text" />
```

### Full example

```razor
<AtomDataHasher Label="Payload"
                @bind-Value="text"
                @bind-Algorithm="algorithm"
                Multiline="true"
                Rows="6"
                Placeholder="Paste text to hash"
                HelpText="Digest updates on every keystroke."
                ResultColor="#00ff41"
                ResultBackgroundColor="#000" />

@code {
    private string text = "";
    private HashAlgorithmKind algorithm = HashAlgorithmKind.Sha256;
}
```

### Parameters

| Parameter                | Type                     | Default    | Description |
|--------------------------|--------------------------|------------|-------------|
| `Value`                  | `string?`                | `null`     | Bound text (two-way with `@bind-Value`). |
| `Algorithm`              | `HashAlgorithmKind`      | `Crc32`    | Bound algorithm (two-way with `@bind-Algorithm`). |
| `Encoding`               | `System.Text.Encoding`   | `UTF8`     | Turns the string into bytes before hashing. |
| `ShowAlgorithmPicker`    | `bool`                   | `true`     | Toggles the built-in `<select>`. Turn off if the host owns the picker. |
| `AlgorithmLabel`         | `string`                 | `Algorithm`| Label above the picker. |
| `ResultLabel`            | `string`                 | `Result`   | Label above the result panel. |
| `Label` / `LabelCol` / `ControlCol` / `HelpText` / `Placeholder` / `AriaLabel` | strings | — | Standard form-field wiring. |
| `Multiline`              | `bool`                   | `true`     | `<textarea>` vs `<input type="text">`. |
| `Rows`                   | `int`                    | `5`        | Textarea rows (multiline only). |
| `Width`                  | `double?`                | `null`     | Explicit px width → CSS `--hasher-width`. |
| `ResultColor`            | `string?`                | `null`     | Result digest color → `--hasher-result-color`. |
| `ResultBackgroundColor`  | `string?`                | `null`     | Result panel background → `--hasher-result-bg`. |
| `Disabled` / `ReadOnly`  | `bool`                   | `false`    | `ReadOnly` is an alias of `Disabled`. |
| `Visible`                | `bool`                   | `true`     | `false` hides via `display:none` (stays in the DOM). |
| `ValueExpression` / `ValidationFor` | `Expression<Func<string?>>?` | `null` | `EditContext` participation. |

### Direct API

`HashComputer.Compute(HashAlgorithmKind, string?, Encoding?)` returns the same
uppercase-hex digest the component renders. Suitable for tests, background
jobs, or hashing outside a component tree.

```csharp
var digest = HashComputer.Compute(HashAlgorithmKind.Sha256, "hello");
// -> "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824"
```

### Notes

- `MD5` is included for legacy interop / non-security checksum use only. Do not
  rely on it for signatures or password storage.
- CRC digests are integrity-check helpers, not cryptographic guarantees.
- Empty / null input renders an empty result (no digest for "no input" — matches
  the sample UX).
