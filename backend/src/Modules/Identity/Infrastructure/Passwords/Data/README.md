# Offline password blocklist — HomeMatch T06

Reviewed: 2026-10-28. Purpose: reject known common passwords when creating/changing/resetting a credential, without a network request. This is not a complete list of all compromised passwords.

## Upstream

- Project: [danielmiessler/SecLists](https://github.com/danielmiessler/SecLists).
- Pinned commit: `758fecf4925900dd1bd0a3cba39551d6b4f2da8c`.
- File: `Passwords/Common-Credentials/xato-net-10-million-passwords-1000000.txt`.
- [Pinned source](https://github.com/danielmiessler/SecLists/blob/758fecf4925900dd1bd0a3cba39551d6b4f2da8c/Passwords/Common-Credentials/xato-net-10-million-passwords-1000000.txt).
- [Pinned upstream license](https://github.com/danielmiessler/SecLists/blob/758fecf4925900dd1bd0a3cba39551d6b4f2da8c/LICENSE).
- Upstream bytes: 8,557,632; SHA-256: `424a3e03a17df0a2bc2b3ca749d81b04e79d59cb7aeec8876a5a3f308d0caf51`.
- Upstream contains 1,000,000 entries and a trailing newline.
- MIT license copied unchanged to LICENSE.SecLists.txt, including upstream copyright. Preserve this notice and license with redistribution. Build/publish copies the license to third-party-licenses/SecLists.txt.

The older filename 10-million-password-list-top-1000000.txt has been removed from upstream. Use the pinned xato source above, not a moving branch.

## Derived resource

common-passwords.txt contains **10,908 lines**, 192,161 bytes, UTF-8 without BOM, LF line endings, final newline.

SHA-256: `8b53ba0f86311b36c33cf92dc6b123367f6590bc80fe3d0b37a79577630c964d`.

Derivation used PowerShell 7:

1. Download upstream bytes at the pinned commit and verify upstream SHA-256.
2. Decode UTF-8, split on LF, remove line-ending CR only.
3. Retain lines matching `^[\x20-\x7E]{15,128}$`: printable ASCII with 15–128 characters, relevant to the current minimum length.
4. Sort/deduplicate with `Sort-Object -Unique -CaseSensitive`, then sort the retained string array with StringComparer.Ordinal.
5. Join with LF and append one final LF; write UTF-8 without BOM.
6. Verify the derived SHA-256 and review license before committing an update.

No password request is truncated, trimmed or normalized by this transformation: it is applied only to the upstream dataset during a reviewed update.

Runtime compares entire submitted passwords using OrdinalIgnoreCase, without rewriting the submitted credential. The resource is embedded; startup checks its SHA-256/line count. Missing or modified data fails initialization. No data is downloaded at startup or during registration.

## Coverage and maintenance

- Filtering removes entries shorter than the required 15 scalars. A list dominated by short passwords would add little protection for this policy.
- The packaged corpus is ASCII; Unicode passwords remain supported but known Unicode breaches are not comprehensively covered.
- Case variants of listed entries are rejected. Whitespace and Unicode normalization are not applied to submitted passwords; Identity hashes the exact input.
- Do not call this corpus exhaustive or a breach-check service. Improve coverage through reviewed data updates, with pinned source, license, checksums and tests.
- For an update, change the embedded file, ExpectedSha256/ExpectedCount in OfflinePasswordBlocklist, this notice and the relevant tests together.
- The application policy is for new credentials. Login verifies existing hashes without imposing the new password policy.
