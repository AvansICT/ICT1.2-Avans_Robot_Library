# Documentation setup

## 1. Use DocFX

DocFX fits this repository because it is a C# library with existing XML code
comments. It creates API pages from the library project and combines them with
Markdown guides. The built-in modern template provides navigation and search.
See the [DocFX quick start](https://dotnet.github.io/docfx/).

The result is a documentation site on GitHub Pages. GitHub's separate **Wiki**
tab is a different service and is not updated by this workflow.

## 2. Understand the files

| File or folder | Purpose |
| --- | --- |
| `.config/dotnet-tools.json` | Pins the local DocFX tool version. |
| `global.json` | Selects the .NET 8 SDK, allowing installed .NET 8 feature and patch updates. |
| `StatisticalRobot-Lib/*.cs` | Source for API signatures and XML comments. |
| `docs/docfx.json` | Selects the library project and configures the site. |
| `docs/index.md` | Site home page. |
| `docs/guide/` | Handwritten general use and maintenance guides. |
| `docs/toc.yml`, `docs/guide/toc.yml` | Navigation menus. |
| `docs/api/` | Generated API metadata; ignored by Git. |
| `docs/_site/` | Generated website; ignored by Git. |
| `.github/workflows/documentation.yml` | Builds documentation and deploys Pages. |

Only the library is selected for API generation. The sample application is not
part of the API reference. DocFX reads the project without running robot code.

## 3. Build and preview locally

Install a .NET 8 SDK. From the repository root, run:

```shell
dotnet tool restore
dotnet restore StatisticalRobot-Lib/StatisticalRobot-Lib.csproj
dotnet build StatisticalRobot-Lib/StatisticalRobot-Lib.csproj -c Release --no-restore -p:GeneratePackageOnBuild=false
dotnet docfx docs/docfx.json --warningsAsErrors
dotnet docfx serve docs/_site
```

Open <http://localhost:8080>. Stop the server with Ctrl+C. After editing source
comments or guides, run the DocFX generation command again and refresh the page.
It runs both API metadata generation and the HTML build.

`GenerateDocumentationFile` is enabled in the library project. This also produces
an XML documentation file beside the DLL for normal builds and package consumers.
Documentation builds disable package creation because they only need the library
and its comments.

## 4. Enable GitHub Actions and Pages once

An administrator must complete these settings in
[AvansICT/ICT1.2-Avans_Robot_Library](https://github.com/AvansICT/ICT1.2-Avans_Robot_Library):

1. Open **Settings > Actions > General**. Enable Actions and allow the official
   GitHub actions used by the workflow (`actions/*`) if your organization restricts
   actions. The default token permission can remain read-only; the deploy job
   requests its specific Pages permissions.
2. Open **Settings > Pages**. Under **Build and deployment**, set **Source** to
   **GitHub Actions**. No publishing branch or `/docs` source folder is needed.
   See [GitHub's publishing source instructions](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site).
3. Commit these source files and merge or push them to `master`. Do not commit
   `docs/api/` or `docs/_site/`. The workflow handles the initial empty Pages site.
4. Open **Actions > Documentation** and check both jobs. You can also select
   **Run workflow** on `master` after the workflow exists there, for example if
   the first deployment ran before you enabled Pages.
5. Open **Settings > Environments > github-pages** after the environment exists.
   Restrict deployment branches to `master`. For fully automatic publication,
   required reviewer approval must not block this environment. Follow your
   organization's policy if approval is required.
6. Open the URL shown by the deployment job. With the current repository name
   and no custom domain, the expected URL is
   <https://avansict.github.io/ICT1.2-Avans_Robot_Library/>.
7. Optionally add that URL as the repository's website in **About**, and protect
   `master` with a rule requiring the **Build documentation** check before merge.

The workflow uses the built-in `GITHUB_TOKEN` and OpenID Connect. No personal
access token, deployment key, or extra repository secret is needed. The separate
NuGet publishing workflow keeps its existing credentials and trigger.

## 5. Understand automatic updates

| Event | Build and validate | Publish the site |
| --- | --- | --- |
| Push to `master` | Yes | Yes |
| Push to another branch | Yes | No; publish after merging into `master`. |
| Pull request | Yes | No |
| Manual run on `master` | Yes | Yes |
| Manual run on another branch | Yes | No |

Every branch push runs a build, without path filters. Public Pages always follows
`master`. A branch with an open pull request can receive both a push build and a
pull request build. Pull request code receives read-only permissions and cannot
run the deployment job.

The build restores the pinned DocFX tool and library dependencies, compiles the
library, generates API metadata and HTML, and checks that key pages and the search
index exist. DocFX warnings fail the build, including unresolved documentation
links. Existing C# compiler warnings remain visible and are not all promoted to
errors. `CS1591` identifies public APIs missing XML comments; keep comments on new
public types and members to avoid introducing this warning again.

Only a successful `master` build uploads the site artifact. A separate job has
`pages: write` and `id-token: write`, configures Pages, and deploys that artifact.
Deployments run one at a time without cancelling an active deployment. Generated
HTML is published directly; no `gh-pages` branch or generated-file commit is used.
See [GitHub's custom workflow guidance](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages).

## 6. Write API comments

Use `///` XML comments directly above public types and members. Normal `//`
comments are implementation notes and do not become API descriptions.

```csharp
/// <summary>Reads the latest available light sample.</summary>
/// <returns>The analog reading, or -1 when no new sample is available.</returns>
/// <remarks>Read frequency is limited by the constructor's interval.</remarks>
public int GetLux()
```

Add a useful `<summary>` to each public class and member. Add `<param>` for every
parameter, `<returns>` for a result, and `<exception>` for errors callers should
handle. Explain units, hardware requirements, return sentinels, and required
update calls. Use `<see cref="Robot"/>` for API links and `<example><code>` for
short examples. Use `<inheritdoc/>` when an interface already documents the same
behavior. Escape XML characters: write `&lt;` for `<` and `&amp;` for `&`.

Start with the most used APIs: `Robot`, device constructors, sensor return values,
and periodic update methods. Public members without comments still appear with
their signatures. Keep descriptions accurate when behavior changes and document
new public members. See [DocFX .NET API documentation](https://dotnet.github.io/docfx/docs/dotnet-api-docs.html).

## 7. Maintain the general use guide

Edit Markdown in `docs/guide/`. Add new pages to `docs/guide/toc.yml`. Keep setup,
wiring, complete examples, deployment, and troubleshooting here. Keep individual
method contracts in C# comments. Use relative links so the site works under the
repository's Pages URL. Do not edit generated API YAML or HTML.

## 8. Maintain tools and troubleshoot builds

DocFX is pinned to version 2.81.0. To update it deliberately, run:

```shell
dotnet tool update docfx --local --version <tested-version>
```

Rebuild and preview before committing the tool manifest. GitHub Actions are pinned
to full commit hashes. Dependabot checks action updates monthly; review and merge
those pull requests after the documentation build passes.

| Problem | Action |
| --- | --- |
| No compatible SDK | Install .NET 8 SDK; a runtime alone is not enough. |
| Metadata fails to load | Restore the library, then use `dotnet docfx metadata docs/docfx.json --logLevel verbose`. |
| DocFX reports invalid XML or a link | Correct the comment or link in the source and rebuild. |
| Pages configuration or deployment fails | Check Pages Source, allowed actions, organization policy, and environment branch rules. |
| Deployment waits for approval | Check required reviewers on the `github-pages` environment. |
| Pages returns 404 | Check the deploy job and its URL; allow initial publication time and check the repository path and filename case. |
| Branch is renamed | Update the workflow's `refs/heads/master` checks, environment rules, and this guide together. |

The first GitHub Actions run is the final check of the runner build and Pages
deployment. Verify navigation, one generated method description, the general use
guide, and search after publication.
