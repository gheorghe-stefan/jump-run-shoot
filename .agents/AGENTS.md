# Project Guidelines for jump-run-shoot

The following guidelines apply to all workspace interactions within this project.

## Versioning & Commit Protocol
1. **Mandatory Version Bump**:
   - With **each submit (commit)**, increment the version number in both:
     - [`GameVersion.cs`](file:///c:/work/jump-run-shoot/GameVersion.cs) (`Version` constant and relevant `BuildTag`)
     - [`JumpRunShoot.csproj`](file:///c:/work/jump-run-shoot/JumpRunShoot.csproj) (`<Version>X.Y.Z</Version>`)
2. **Commit Message Format**:
   - Always put the new version number at the start of the commit message.
   - Example: `v0.3.7: <description>` or `v0.3.7 - feat: <description>`.

## Development & Testing (TDD)
- **Bug Fixes**: For each bug found, write a failing unit test first to prove the defect, then implement the fix to turn the test green (`dotnet test`).
- Keep all unit tests passing prior to commit and deployment.

## Visual & Art Style Guidelines
- **Solid 16 EGA Palette**: Strictly authentic DOS / Duke Nukem 1 aesthetics.
- **No Anti-Aliasing**: Disable canvas smoothing, maintain clean 1px black silhouettes, avoid checkerboard dithering or blur filters.
- **Integer Aspect Ratio**: Maintain native 16:10 aspect ratio matching 320x200 retro virtual resolution.

## Deployment
- Automated GitHub Actions Pages deployment using `actions/upload-pages-artifact@v3` and `actions/deploy-pages@v4` on push to `main`.
