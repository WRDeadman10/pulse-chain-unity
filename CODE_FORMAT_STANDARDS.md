# Code Format Standards (Reusable)

This document defines the coding and formatting rules that must be followed for this project and can be reused in future projects.

## Primary Rule Source

1. Always read and follow the root `.editorconfig` exactly.
2. Do not introduce a new style when code already has an established pattern.
3. If any style decision is unclear, prefer explicit and verbose code.

## C# Rules (Strict)

1. Never use `var`.
2. Always use explicit types.
3. Never use implicit object creation (`new()`).
4. Never use expression-bodied members (no `=>` members for methods/properties).
5. Use K&R brace style (`{` on the same line).
6. Use explicit access modifiers on non-interface members.

## Naming Rules

1. Public APIs: PascalCase.
2. Private non-serialized fields: `_camelCase` (underscore prefix required).
3. Unity serialized fields (`[SerializeField] private`): `camelCase` without underscore.
4. Local functions: camelCase.
5. Interfaces: PascalCase with `I` prefix.

## Unity-Specific Rules

1. Follow Unity serialization naming conventions strictly:
   - `[SerializeField] private Transform cameraRoot;`
   - `private float _runtimeTimer;`
2. Keep serialized fields private unless a public API is required.
3. Do not rename serialized fields casually in existing scenes/prefabs without migration awareness.

## Formatting Rules

1. Indentation: 4 spaces for `.cs`.
2. Line endings: CRLF.
3. Charset: UTF-8 BOM.
4. Keep using directives ordered and grouped as configured.
5. Do not auto-apply style changes outside requested scope.

## Validation Commands

Use these commands before commit.

### If solution has multiple projects

```powershell
dotnet format Assembly-CSharp.csproj --include "Assets/Games/indoor-navigation-unity"
dotnet format Assembly-CSharp.csproj --verify-no-changes --include "Assets/Games/indoor-navigation-unity"
```

### If formatting entire solution is explicitly requested

```powershell
dotnet format indoor-navigation-unity-base-project.sln --no-restore
dotnet format indoor-navigation-unity-base-project.sln --no-restore --verify-no-changes
```

Note: Full-solution verify can fail on third-party/vendor code naming rules (`IDE1006`) that are not safe to auto-fix.

## Safety and Scope Policy

1. Never mass-format vendor or third-party code unless explicitly requested.
2. Prefer scoped formatting to the feature/module path being modified.
3. If a global format pass causes unrelated file changes, revert unrelated files and keep only target scope changes.

## Fast Checklist

Before writing code:
1. Read `.editorconfig`.
2. Confirm naming and formatting rules from this document.
3. Match existing project patterns.

Before commit:
1. Run `dotnet format` on target scope.
2. Run `dotnet format --verify-no-changes` on target scope.
3. Ensure no unintended file changes outside requested scope.

---

Recommended usage in future projects:
- Share this file at task start.
- State: "Follow CODE_FORMAT_STANDARDS.md + root .editorconfig strictly."
